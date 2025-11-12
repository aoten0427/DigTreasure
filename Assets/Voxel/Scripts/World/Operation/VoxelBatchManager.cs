using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UniRx;

namespace VoxelWorld
{
    /// <summary>
    /// ボクセルの非同期バッチ処理を管理
    /// </summary>
    public class VoxelBatchManager
    {
        private ChunkManager m_chunkManager;
        private MonoBehaviour m_coroutineRunner;

        // イベント発火用
        private Action<List<VoxelUpdate>> m_onVoxelChanged;
        private Func<bool> m_shouldFireEvent = () => true;

        // VoxelMeshManager参照（進捗追跡用）
        private VoxelMeshManager m_meshManager;

        // 自動メッシュ更新フラグ
        private bool m_enableAutoMeshUpdate;

        /// <summary>
        /// VoxelBatchManagerを初期化
        /// </summary>
        public void Initialize(
            ChunkManager chunkManager,
            MonoBehaviour coroutineRunner,
            bool enableAutoMeshUpdate,
            Action<List<VoxelUpdate>> onVoxelChanged,
            VoxelMeshManager meshManager)
        {
            m_chunkManager = chunkManager;
            m_coroutineRunner = coroutineRunner;
            m_enableAutoMeshUpdate = enableAutoMeshUpdate;
            m_onVoxelChanged = onVoxelChanged;
            m_meshManager = meshManager;
        }

        /// <summary>
        /// イベント発火条件を設定
        /// </summary>
        public void SetEventFireCondition(System.Func<bool> condition)
        {
            m_shouldFireEvent = condition ?? (() => true);
        }

        #region ボクセル変更処理
        /// <summary>
        /// 複数のボクセルを非同期で一括設定（チャンク単位でフレーム分割）
        /// </summary>
        public IEnumerator SetVoxelsAsync(
            List<VoxelUpdate> voxelUpdates,
            bool isSender = false,
            int chunksPerFrame = 10,
            ReactiveProperty<float> progressProperty = null,
            Action<int> onComplete = null)
        {
            //データ妥当性チェック
            if (!ValidateVoxelUpdates(voxelUpdates, progressProperty, onComplete))yield break;

            // ボクセルをチャンクごとにグループ化
            var chunkGroups = GroupVoxelUpdatesByChunk(voxelUpdates);

            // ボクセルデータを設定
            var result = new VoxelSetResult();
            yield return ProcessVoxelDataSetting(chunkGroups, chunksPerFrame, progressProperty, result);

            // イベント発火
            FireVoxelChangedEvent(result.AppliedChanges, isSender);

            // メッシュ更新
            if (m_enableAutoMeshUpdate && result.SuccessCount > 0)
            {
                yield return ProcessMeshUpdate(result.AffectedChunks, progressProperty);
            }

            // 完了通知
            CompleteProcessing(progressProperty, onComplete, result.SuccessCount);
        }

        /// <summary>
        /// ボクセル更新リストの検証
        /// </summary>
        private bool ValidateVoxelUpdates(List<VoxelUpdate> voxelUpdates, ReactiveProperty<float> progressProperty, Action<int> onComplete)
        {
            if (voxelUpdates == null || voxelUpdates.Count == 0)
            {
                if (progressProperty != null) progressProperty.Value = 1.0f;
                onComplete?.Invoke(0);
                return false;
            }
            return true;
        }

        /// <summary>
        /// ボクセル更新をチャンクごとにグループ化
        /// </summary>
        private Dictionary<Vector3Int, List<VoxelUpdate>> GroupVoxelUpdatesByChunk(List<VoxelUpdate> voxelUpdates)
        {
            var chunkGroups = new Dictionary<Vector3Int, List<VoxelUpdate>>();

            foreach (var update in voxelUpdates)
            {
                Vector3Int chunkPos = VoxelConstants.WorldToChunkPosition(update.WorldPosition);
                if (!chunkGroups.TryGetValue(chunkPos, out var list))
                {
                    list = new List<VoxelUpdate>();
                    chunkGroups[chunkPos] = list;
                }
                list.Add(update);
            }

            return chunkGroups;
        }

        /// <summary>
        /// ボクセルデータ設定の結果
        /// </summary>
        private class VoxelSetResult
        {
            public HashSet<Vector3Int> AffectedChunks = new HashSet<Vector3Int>();
            public List<VoxelUpdate> AppliedChanges = new List<VoxelUpdate>();
            public int SuccessCount = 0;
        }

        /// <summary>
        /// ボクセルデータの設定処理（進捗: 0.0～0.5）
        /// </summary>
        private IEnumerator ProcessVoxelDataSetting(
            Dictionary<Vector3Int, List<VoxelUpdate>> chunkGroups,
            int chunksPerFrame,
            ReactiveProperty<float> progressProperty,
            VoxelSetResult result)
        {
            int totalChunks = chunkGroups.Count;
            int processedChunks = 0;

            foreach (var kvp in chunkGroups)
            {
                Chunk chunk = m_chunkManager.GetChunk(kvp.Key);
                if (chunk != null)
                {
                    ApplyVoxelUpdatesToChunk(chunk, kvp.Key, kvp.Value, result);
                    result.AffectedChunks.Add(kvp.Key);
                }

                processedChunks++;

                // フレーム分割
                if (processedChunks % chunksPerFrame == 0)
                {
                    //進捗具合を更新
                    if (progressProperty != null)
                    {
                        float voxelProgress = (float)processedChunks / totalChunks * 0.5f;
                        progressProperty.Value = voxelProgress;
                    }
                    yield return null;
                }
            }
        }

        /// <summary>
        /// チャンクにボクセル更新を適用
        /// </summary>
        private void ApplyVoxelUpdatesToChunk(
            Chunk chunk,
            Vector3Int chunkPos,
            List<VoxelUpdate> updates,
            VoxelSetResult result)
        {
            Vector3 chunkWorldPos = VoxelConstants.ChunkToWorldPosition(chunkPos.x, chunkPos.y, chunkPos.z);
            bool hasBoundaryVoxel = false;

            foreach (var update in updates)
            {
                Vector3Int localPosition = ConvertWorldToLocalPosition(update.WorldPosition, chunkWorldPos);

                bool success = chunk.SetVoxel(localPosition, update.VoxelID);
                if (success)
                {
                    result.SuccessCount++;
                    result.AppliedChanges.Add(update);

                    // 境界ボクセルかチェック（既に境界ボクセルが見つかっている場合はスキップ）
                    if (!hasBoundaryVoxel && IsChunkBoundaryVoxel(localPosition))
                    {
                        hasBoundaryVoxel = true;
                    }
                }
            }

            // 境界ボクセルが変更された場合、隣接チャンクも更新対象に追加
            if (hasBoundaryVoxel)
            {
                AddNeighborChunksToAffected(chunkPos, result);
            }
        }

        /// <summary>
        /// ボクセルがチャンク境界にあるかチェック
        /// </summary>
        private bool IsChunkBoundaryVoxel(Vector3Int localPosition)
        {
            return localPosition.x == 0 || localPosition.x == VoxelConstants.CHUNK_WIDTH - 1 ||
                   localPosition.y == 0 || localPosition.y == VoxelConstants.CHUNK_HEIGHT - 1 ||
                   localPosition.z == 0 || localPosition.z == VoxelConstants.CHUNK_DEPTH - 1;
        }

        /// <summary>
        /// 隣接チャンクを更新対象に追加
        /// </summary>
        private void AddNeighborChunksToAffected(Vector3Int chunkPos, VoxelSetResult result)
        {
            // 6方向の隣接チャンクを追加
            result.AffectedChunks.Add(new Vector3Int(chunkPos.x + 1, chunkPos.y, chunkPos.z)); // Right
            result.AffectedChunks.Add(new Vector3Int(chunkPos.x - 1, chunkPos.y, chunkPos.z)); // Left
            result.AffectedChunks.Add(new Vector3Int(chunkPos.x, chunkPos.y + 1, chunkPos.z)); // Up
            result.AffectedChunks.Add(new Vector3Int(chunkPos.x, chunkPos.y - 1, chunkPos.z)); // Down
            result.AffectedChunks.Add(new Vector3Int(chunkPos.x, chunkPos.y, chunkPos.z + 1)); // Forward
            result.AffectedChunks.Add(new Vector3Int(chunkPos.x, chunkPos.y, chunkPos.z - 1)); // Back
        }

        /// <summary>
        /// ワールド座標からローカル座標に変換
        /// </summary>
        private Vector3Int ConvertWorldToLocalPosition(Vector3 worldPosition, Vector3 chunkWorldPos)
        {
            Vector3 localWorldPos = worldPosition - chunkWorldPos;
            return new Vector3Int(
                Mathf.FloorToInt(localWorldPos.x * VoxelConstants.INV_VOXEL_SIZE),
                Mathf.FloorToInt(localWorldPos.y * VoxelConstants.INV_VOXEL_SIZE),
                Mathf.FloorToInt(localWorldPos.z * VoxelConstants.INV_VOXEL_SIZE)
            );
        }

        /// <summary>
        /// ボクセル変更イベントを発火
        /// </summary>
        private void FireVoxelChangedEvent(List<VoxelUpdate> appliedChanges, bool isSender)
        {
            //基本はネットワークデータ送信のために使われる
            if (appliedChanges.Count > 0 && isSender && m_shouldFireEvent())
            {
                m_onVoxelChanged?.Invoke(appliedChanges);
            }
        }

        /// <summary>
        /// メッシュ更新処理（進捗: 0.5～1.0）
        /// </summary>
        private IEnumerator ProcessMeshUpdate(HashSet<Vector3Int> affectedChunks, ReactiveProperty<float> progressProperty)
        {
            Debug.Log($"[VoxelBatchManager] ProcessMeshUpdate started: affectedChunks={affectedChunks.Count}");

            if (m_meshManager != null)
            {
                bool meshUpdateComplete = false;
                var meshProgress = new ReactiveProperty<float>(0f);

                // メッシュ進捗を全体進捗にマッピング（0.5～1.0の範囲）
                var subscription = meshProgress.Subscribe(mp =>
                {
                    if (progressProperty != null)
                    {
                        progressProperty.Value = 0.5f + mp * 0.5f;
                    }
                    //Debug.Log($"[VoxelBatchManager] Mesh progress: {mp * 100:F1}% (overall: {(0.5f + mp * 0.5f) * 100:F1}%)");
                });

                // メッシュ更新を登録
                Debug.Log($"[VoxelBatchManager] Calling MeshUpdate for {affectedChunks.Count} chunks");
                m_meshManager.MeshUpdate(
                    affectedChunks,
                    meshProgress,
                    () => {
                        Debug.Log("[VoxelBatchManager] Mesh update complete callback invoked!");
                        meshUpdateComplete = true;
                    }
                );

                // メッシュ更新の完了を待つ
                int waitFrames = 0;
                while (!meshUpdateComplete)
                {
                    waitFrames++;
                    if (waitFrames % 60 == 0)
                    {
                        Debug.Log($"[VoxelBatchManager] Waiting for mesh update... ({waitFrames} frames)");
                    }
                    yield return null;
                }

                Debug.Log($"[VoxelBatchManager] Mesh update finished after {waitFrames} frames");
                subscription.Dispose();
            }
            else
            {
                Debug.LogWarning("[VoxelBatchManager] VoxelMeshManagerが初期化されていません。メッシュ更新をスキップします。");
            }
        }

        /// <summary>
        /// 処理完了
        /// </summary>
        private void CompleteProcessing(ReactiveProperty<float> progressProperty, Action<int> onComplete, int successCount)
        {
            Debug.Log($"[VoxelBatchManager] CompleteProcessing called: successCount={successCount}, onComplete={(onComplete != null ? "exists" : "null")}");

            if (progressProperty != null)
            {
                progressProperty.Value = 1.0f;
            }

            onComplete?.Invoke(successCount);

            Debug.Log($"[VoxelBatchManager] onComplete invoked");
        } 
        #endregion

        /// <summary>
        /// 指定チャンクを指定ボクセルで埋める
        /// </summary>
        public int FillChunk(
            Vector3Int chunkPosition,
            Voxel voxel,
            System.Action<List<VoxelUpdate>, bool, ReactiveProperty<float>, Action<int>> setVoxelsAction,
            bool isSender = false,
            ReactiveProperty<float> progressProperty = null,
            Action<int> onComplete = null)
        {
            // FillChunks
            return FillChunks(new[] { chunkPosition }, voxel, setVoxelsAction,isSender, progressProperty, onComplete);
        }

        /// <summary>
        /// 複数チャンクを指定ボクセルで埋める
        /// </summary>
        public int FillChunks(
            IEnumerable<Vector3Int> chunkPositions,
            Voxel voxel,
            System.Action<List<VoxelUpdate>, bool, ReactiveProperty<float>, Action<int>> setVoxelsAction,
            bool isSender = false,
            ReactiveProperty<float> progressProperty = null,
            Action<int> onComplete = null)
        {
            if (m_chunkManager == null)
            {
                Debug.LogWarning("[VoxelBatchManager] ChunkManagerが初期化されていません");
                onComplete?.Invoke(0);
                return 0;
            }

            // 全チャンクのボクセル更新リストを作成
            var allVoxelUpdates = CreateVoxelUpdatesForChunks(chunkPositions, voxel);

            if (allVoxelUpdates.Count == 0)
            {
                onComplete?.Invoke(0);
                return 0;
            }

            setVoxelsAction(allVoxelUpdates, isSender, progressProperty, onComplete);
            return allVoxelUpdates.Count;
        }

        /// <summary>
        /// 複数チャンクのボクセル更新リストを作成
        /// </summary>
        private List<VoxelUpdate> CreateVoxelUpdatesForChunks(IEnumerable<Vector3Int> chunkPositions, Voxel voxel)
        {
            var allVoxelUpdates = new List<VoxelUpdate>();

            foreach (var chunkPos in chunkPositions)
            {
                if (m_chunkManager.GetChunk(chunkPos) == null)
                {
                    Debug.LogWarning($"[VoxelBatchManager] チャンク{chunkPos}が見つかりません");
                    continue;
                }

                AddVoxelUpdatesForSingleChunk(allVoxelUpdates, chunkPos, voxel);
            }

            return allVoxelUpdates;
        }

        /// <summary>
        /// 単一チャンクのボクセル更新リストを作成
        /// </summary>
        private void AddVoxelUpdatesForSingleChunk(List<VoxelUpdate> voxelUpdates, Vector3Int chunkPos, Voxel voxel)
        {
            Vector3 chunkWorldPos = VoxelConstants.ChunkToWorldPosition(chunkPos.x, chunkPos.y, chunkPos.z);

            for (int x = 0; x < VoxelConstants.CHUNK_WIDTH; x++)
            {
                for (int y = 0; y < VoxelConstants.CHUNK_HEIGHT; y++)
                {
                    for (int z = 0; z < VoxelConstants.CHUNK_DEPTH; z++)
                    {
                        Vector3 worldPos = chunkWorldPos + new Vector3(
                            x * VoxelConstants.VOXEL_SIZE,
                            y * VoxelConstants.VOXEL_SIZE,
                            z * VoxelConstants.VOXEL_SIZE
                        );

                        voxelUpdates.Add(new VoxelUpdate(worldPos, voxel));
                    }
                }
            }
        }
    }
}
