using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UniRx;
using System.Diagnostics;

namespace VoxelWorld
{
    /// <summary>
    /// ボクセル操作専用管理クラス（統合マネージャー）
    /// </summary>
    public class VoxelOperationManager
    {
        private ChunkManager m_chunkManager;
        private VoxelEffectManager m_voxelEffectManager;
        private MonoBehaviour m_coroutineRunner;

        // サブマネージャー
        private VoxelBatchManager m_batchManager;
        private VoxelMeshManager m_meshManager;
        private VoxelSeparationManager m_separationManager;

        // 設定フラグ
        private bool m_enableAutoMeshUpdate;
        private bool m_enableSeparationDetection = true;

        // 破壊後フック
        private event Action<List<Vector3>> m_onVoxelsDestroyed;

        // ボクセル変更イベント（ネットワーク同期用）
        public event Action<List<VoxelUpdate>> OnVoxelChanged;

        // イベント発火条件（StateAuthorityチェック用）
        private Func<bool> m_shouldFireEvent = () => true;

        // パフォーマンス計測用
        private bool m_enablePerformanceLogging = false;
        public bool EnablePerformanceLogging
        {
            get => m_enablePerformanceLogging;
            set
            {
                m_enablePerformanceLogging = value;
                if (m_batchManager != null)
                {
                    m_batchManager.EnablePerformanceLogging = value;
                }
            }
        }

        // プロパティ
        public bool EnableAutoMeshUpdate
        {
            get => m_enableAutoMeshUpdate;
            set
            {
                m_enableAutoMeshUpdate = value;
                m_meshManager?.SetAutoMeshUpdate(value);
            }
        }

        public bool EnableSeparationDetection
        {
            get => m_enableSeparationDetection;
            set => m_enableSeparationDetection = value;
        }

        public SeparationDetector SeparationDetector => m_separationManager?.SeparationDetector;

        /// <summary>
        /// イベント発火条件を設定（ネットワーク同期制御用）
        /// </summary>
        public void SetEventFireCondition(System.Func<bool> condition)
        {
            m_shouldFireEvent = condition ?? (() => true);
            m_batchManager?.SetEventFireCondition(condition);
        }

        /// <summary>
        /// VoxelOperationManagerを初期化
        /// </summary>
        public void Initialize(ChunkManager chunkManager,VoxelEffectManager effectManager, MonoBehaviour coroutineRunner, bool enableAutoMeshUpdate = true, bool enableSeparationDetection = false, SeparatedObjectSpawner spawner = null)
        {
            m_chunkManager = chunkManager;
            m_voxelEffectManager = effectManager;
            m_coroutineRunner = coroutineRunner;
            m_enableAutoMeshUpdate = enableAutoMeshUpdate;
            m_enableSeparationDetection = enableSeparationDetection;

            // VoxelMeshManagerを初期化
            m_meshManager = new VoxelMeshManager();
            m_meshManager.Initialize(chunkManager, coroutineRunner, enableAutoMeshUpdate);

            // VoxelBatchManagerを初期化（VoxelMeshManagerへの参照を渡す）
            m_batchManager = new VoxelBatchManager();
            m_batchManager.Initialize(
                chunkManager,
                coroutineRunner,
                enableAutoMeshUpdate,
                (updates) => OnVoxelChanged?.Invoke(updates),
                m_meshManager
            );

            // VoxelSeparationManagerを初期化（Spawnerを渡す）
            m_separationManager = new VoxelSeparationManager();
            m_separationManager.Initialize(chunkManager, enableAutoMeshUpdate, enableSeparationDetection, spawner);
        }

        /// <summary>
        /// 指定されたワールド座標のボクセルを取得
        /// </summary>
        public Voxel GetVoxel(Vector3 worldPosition)
        {
            if (m_chunkManager == null)
            {
                UnityEngine.Debug.LogWarning("[VoxelOperationManager] ChunkManagerが初期化されていません");
                return Voxel.Empty;
            }

            Chunk chunk = m_chunkManager.GetChunkFromWorldPosition(worldPosition);
            return chunk?.GetVoxelFromWorldPosition(worldPosition) ?? Voxel.Empty;
        }

        /// <summary>
        /// 指定されたワールド座標にボクセルを設定
        /// 注意: 非同期実行のため即座には反映されません
        /// </summary>
        /// <param name="progressProperty">進捗通知用ReactiveProperty（0.0～1.0）</param>
        /// <param name="onComplete">完了コールバック（設定成功数を返す）</param>
        public void SetVoxel(
            Vector3 worldPosition,
            Voxel voxel,
            ReactiveProperty<float> progressProperty = null,
            Action<int> onComplete = null)
        {
            var voxelUpdates = new List<VoxelUpdate>
            {
                new VoxelUpdate { WorldPosition = worldPosition, VoxelID = voxel }
            };

            SetVoxels(voxelUpdates, true, progressProperty, onComplete);
        }

        /// <summary>
        /// 複数のボクセルを一括設定（非同期実行）
        /// </summary>
        /// <param name="voxelUpdates">設定するボクセルのリスト</param>
        /// <param name="isSender">ネットワーク同期でイベントを発火するか</param>
        /// <param name="progressProperty">進捗通知用ReactiveProperty（0.0～1.0、ボクセル設定50%+メッシュ更新50%）</param>
        /// <param name="onComplete">完了コールバック（設定成功数を返す）</param>
        public void SetVoxels(
            List<VoxelUpdate> voxelUpdates,
            bool isSender = false,
            ReactiveProperty<float> progressProperty = null,
            Action<int> onComplete = null)
        {
            if (m_chunkManager == null)
            {
                UnityEngine.Debug.LogWarning("[VoxelOperationManager] ChunkManagerが初期化されていません");
                if (progressProperty != null) progressProperty.Value = 1.0f;
                onComplete?.Invoke(0);
                return;
            }

            if (voxelUpdates == null || voxelUpdates.Count == 0)
            {
                if (progressProperty != null) progressProperty.Value = 1.0f;
                onComplete?.Invoke(0);
                return;
            }

            // coroutineRunnerのチェック
            if (m_coroutineRunner == null)
            {
                UnityEngine.Debug.LogError("[VoxelOperationManager] CoroutineRunnerが設定されていません！VoxelOperationManager.SetManager()でMonoBehaviourを渡してください。");
                if (progressProperty != null) progressProperty.Value = 1.0f;
                onComplete?.Invoke(0);
                return;
            }

            // VoxelBatchManagerに委譲
            m_coroutineRunner.StartCoroutine(
                m_batchManager.SetVoxelsAsync(voxelUpdates, isSender, 10, progressProperty, onComplete)
            );
        }

        /// <summary>
        /// 攻撃力による条件付きボクセル破壊
        /// </summary>
        public int DestroyVoxelsWithPower(List<Vector3> worldPositions, float attackPower,
            Vector3 destractionPoint,Vector3 effectDirection)
        {
            Stopwatch totalStopwatch = null;
            Stopwatch stepStopwatch = null;
            if (EnablePerformanceLogging)
            {
                totalStopwatch = Stopwatch.StartNew();
                stepStopwatch = new Stopwatch();
            }

            if (worldPositions == null || worldPositions.Count == 0)
            {
                return 0;
            }

            var voxelUpdates = new List<VoxelUpdate>();
            var actualDestroyedPositions = new List<Vector3>();
            var affectedChunks = new HashSet<Vector3Int>();

            // チャンクごとにグループ化して破壊判定
            if (EnablePerformanceLogging) stepStopwatch.Restart();

            var chunkGroups = new Dictionary<Vector3Int, List<Vector3>>();

            foreach (var position in worldPositions)
            {
                Vector3Int chunkPos = VoxelConstants.WorldToChunkPosition(position);
                if (!chunkGroups.ContainsKey(chunkPos))
                {
                    chunkGroups[chunkPos] = new List<Vector3>();
                }
                chunkGroups[chunkPos].Add(position);
            }

            if (EnablePerformanceLogging)
            {
                stepStopwatch.Stop();
                UnityEngine.Debug.Log($"[Performance] DestroyVoxelsWithPower - Grouping: {stepStopwatch.ElapsedMilliseconds}ms ({worldPositions.Count} positions, {chunkGroups.Count} chunks)");
            }


            int destroyVoxelID = -1;
            // チャンクごとに一括判定
            if (EnablePerformanceLogging) stepStopwatch.Restart();

            foreach (var kvp in chunkGroups)
            {
                Chunk chunk = m_chunkManager.GetChunk(kvp.Key);
                if (chunk != null)
                {
                    foreach (var position in kvp.Value)
                    {
                        Voxel currentVoxel = chunk.GetVoxelFromWorldPosition(position);

                        if (!currentVoxel.IsEmpty && currentVoxel.CanBeDestroyed(attackPower))
                        {
                            if (destroyVoxelID == -1) destroyVoxelID = currentVoxel.VoxelId;
                            voxelUpdates.Add(new VoxelUpdate { WorldPosition = position, VoxelID = Voxel.Empty });
                            actualDestroyedPositions.Add(position);

                        }
                    }
                    affectedChunks.Add(kvp.Key);
                }
            }

            if (EnablePerformanceLogging)
            {
                stepStopwatch.Stop();
                UnityEngine.Debug.Log($"[Performance] DestroyVoxelsWithPower - Destruction Check: {stepStopwatch.ElapsedMilliseconds}ms ({actualDestroyedPositions.Count} destroyed)");
            }

            // 実際に破壊処理実行
            SetVoxels(voxelUpdates, true);

            if(m_voxelEffectManager != null&&voxelUpdates.Count != 0)
            {
                m_voxelEffectManager.SpownEffect(destractionPoint, voxelUpdates.Count,effectDirection,destroyVoxelID);
            }


            // 破壊後処理実行
            List<SeparatedVoxelObject> generatedSeparatedObjects = new List<SeparatedVoxelObject>();

            //効率化前は処理が重くなりすぎるのでテスト中はパス
            //if (actualDestroyedPositions != null && actualDestroyedPositions.Count > 0)
            //{
            //    // 分離検出実行（VoxelSeparationManagerに委譲）
            //    if (m_enableSeparationDetection && m_separationManager != null)
            //    {
            //        generatedSeparatedObjects = m_separationManager.ProcessSeparationDetection(actualDestroyedPositions);
            //    }

            //    // 破壊後フック実行
            //    if (m_onVoxelsDestroyed != null)
            //    {
            //        m_onVoxelsDestroyed.Invoke(actualDestroyedPositions);
            //    }
            //}

            if (EnablePerformanceLogging && totalStopwatch != null)
            {
                totalStopwatch.Stop();
                UnityEngine.Debug.Log($"[Performance] DestroyVoxelsWithPower - Total: {totalStopwatch.ElapsedMilliseconds}ms");
            }

            return actualDestroyedPositions.Count;
        }

        /// <summary>
        /// ボクセル変更をチャンクに通知（VoxelMeshManagerに委譲）
        /// </summary>
        public void NotifyVoxelChanged(Vector3Int chunkPosition)
        {
            m_meshManager?.NotifyVoxelChanged(chunkPosition);
        }

        /// <summary>
        /// 指定座標が有効なボクセル座標かチェック
        /// </summary>
        public bool IsValidVoxelPosition(Vector3 worldPosition)
        {
            if (m_chunkManager == null)
            {
                return false;
            }

            Chunk chunk = m_chunkManager.GetChunkFromWorldPosition(worldPosition);
            return chunk != null;
        }

        /// <summary>
        /// 指定チャンクを指定ボクセルで埋める
        /// </summary>
        /// <param name="progressProperty">進捗通知用ReactiveProperty（0.0～1.0）</param>
        /// <param name="onComplete">完了コールバック（設定成功数を返す）</param>
        public int FillChunk(
            Vector3Int chunkPosition,
            Voxel voxel,
            bool isSender = false,
            ReactiveProperty<float> progressProperty = null,
            Action<int> onComplete = null)
        {
            return m_batchManager?.FillChunk(chunkPosition, voxel, SetVoxels, isSender, progressProperty, onComplete) ?? 0;
        }

        /// <summary>
        /// 複数チャンクを指定ボクセルで埋める
        /// </summary>
        /// <param name="progressProperty">進捗通知用ReactiveProperty（0.0～1.0）</param>
        /// <param name="onComplete">完了コールバック（設定成功数を返す）</param>
        public int FillChunks(
            IEnumerable<Vector3Int> chunkPositions,
            Voxel voxel,
            bool isSender = false,
            ReactiveProperty<float> progressProperty = null,
            Action<int> onComplete = null)
        {
            return m_batchManager?.FillChunks(chunkPositions, voxel, SetVoxels, isSender, progressProperty, onComplete) ?? 0;
        }
    }

    /// <summary>
    /// ボクセル更新情報
    /// </summary>
    public struct VoxelUpdate
    {
        public Vector3 WorldPosition;
        public int VoxelID;

        public VoxelUpdate(Vector3 worldPosition, int voxel)
        {
            WorldPosition = worldPosition;
            VoxelID = voxel;
        }
    }
}
