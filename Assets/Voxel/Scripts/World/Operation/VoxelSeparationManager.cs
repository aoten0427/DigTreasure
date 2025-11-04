using UnityEngine;
using System.Collections.Generic;

namespace VoxelWorld
{
    /// <summary>
    /// 分離検出とSeparatedVoxelObject生成を管理
    /// </summary>
    public class VoxelSeparationManager
    {
        private ChunkManager m_chunkManager;
        private SeparationDetector m_separationDetector;
        private bool m_enableAutoMeshUpdate;
        private SeparatedObjectSpawner m_spawner;

        // 分離オブジェクトコールバック
        private event System.Action<List<SeparatedVoxelObject>> m_onSeparatedObjectsCreated;

        /// <summary>
        /// VoxelSeparationManagerを初期化
        /// </summary>
        /// <param name="chunkManager">チャンク管理クラス</param>
        /// <param name="enableAutoMeshUpdate">自動メッシュ更新を有効化</param>
        /// <param name="enableSeparationDetection">分離検出を有効化</param>
        /// <param name="spawner">分離オブジェクト生成クラス（nullの場合はダミー使用）</param>
        public void Initialize(ChunkManager chunkManager, bool enableAutoMeshUpdate, bool enableSeparationDetection, SeparatedObjectSpawner spawner = null)
        {
            m_chunkManager = chunkManager;
            m_enableAutoMeshUpdate = enableAutoMeshUpdate;
            m_spawner = spawner;

            // 分離検出器を初期化
            if (enableSeparationDetection)
            {
                m_separationDetector = SeparationDetector.CreateDefault();
            }
        }

        /// <summary>
        /// 分離検出器へのアクセス
        /// </summary>
        public SeparationDetector SeparationDetector => m_separationDetector;

        /// <summary>
        /// 分離オブジェクト生成コールバックを登録
        /// </summary>
        public void RegisterSeparatedObjectsCreatedCallback(System.Action<List<SeparatedVoxelObject>> callback)
        {
            m_onSeparatedObjectsCreated += callback;
        }

        /// <summary>
        /// 破壊後の分離検出処理
        /// </summary>
        public List<SeparatedVoxelObject> ProcessSeparationDetection(List<Vector3> destroyedPositions)
        {
            var separatedObjects = new List<SeparatedVoxelObject>();

            if (m_separationDetector == null || destroyedPositions == null || destroyedPositions.Count == 0)
            {
                Debug.LogWarning($"[VoxelSeparationManager] ProcessSeparationDetection終了: detector={m_separationDetector != null}, positions={destroyedPositions?.Count ?? 0}");
                return separatedObjects;
            }

            
            // ChunkManagerVoxelProviderでラップ
            var voxelProvider = new ChunkManagerVoxelProvider(m_chunkManager);

            // 分離検出実行
            var separationResult = m_separationDetector.DetectSeparations(destroyedPositions, voxelProvider);

            // サイズ超過グループをチャンクボクセルとして復元
            if (separationResult.OversizedGroupsCount > 0)
            {
                RestoreOversizedGroupsToChunks(separationResult.OversizedGroups);
            }

            // 分離オブジェクトを作成
            if (separationResult.ValidGroupsCreated > 0)
            {
                separatedObjects = CreateSeparatedObjects(separationResult.SeparatedGroups);

                // コールバック呼び出し
                if (m_onSeparatedObjectsCreated != null && separatedObjects.Count > 0)
                {
                    m_onSeparatedObjectsCreated.Invoke(separatedObjects);
                }
            }
            
            

            return separatedObjects;
        }

        /// <summary>
        /// 分離グループからSeparatedVoxelObjectを作成
        /// </summary>
        private List<SeparatedVoxelObject> CreateSeparatedObjects(List<List<Vector3>> separatedGroups)
        {
            var separatedObjects = new List<SeparatedVoxelObject>();

            for (int i = 0; i < separatedGroups.Count; i++)
            {
                var group = separatedGroups[i];

                if (group == null || group.Count == 0)
                {
                    continue;
                }

                // Spawnerで分離オブジェクトを生成
                var separatedObject = CreateSeparatedObjectFromGroup(group);

                if (separatedObject != null)
                {
                    separatedObjects.Add(separatedObject);
                }
                else
                {
                    Debug.LogWarning($"[VoxelSeparationManager] SeparatedVoxelObject作成失敗: {group.Count}個のボクセル");
                }
                
                
            }

            return separatedObjects;
        }

        /// <summary>
        /// グループから分離オブジェクトを生成（Spawner使用）
        /// </summary>
        private SeparatedVoxelObject CreateSeparatedObjectFromGroup(List<Vector3> group)
        {
            if (group == null || group.Count == 0)
            {
                return null;
            }

            if (m_spawner == null)
            {
                Debug.LogError("[VoxelSeparationManager] Spawnerが初期化されていません");
                return null;
            }

            // バウンディングボックスを計算
            var bounds = CalculateGroupBounds(group);

            // サイズを計算
            Vector3Int size = new Vector3Int(
                Mathf.CeilToInt(bounds.size.x / VoxelConstants.VOXEL_SIZE),
                Mathf.CeilToInt(bounds.size.y / VoxelConstants.VOXEL_SIZE),
                Mathf.CeilToInt(bounds.size.z / VoxelConstants.VOXEL_SIZE)
            );

            // ボクセルデータを作成
            Voxel[,,] voxelData = CreateVoxelDataArray(group, bounds, size, out HashSet<Vector3Int> affectedChunks);

            // Spawnerで分離オブジェクトを生成
            var separatedObject = m_spawner.Spawn(voxelData, size, bounds.min);

            if (separatedObject == null)
            {
                Debug.LogError("[VoxelSeparationManager] 分離オブジェクトの生成に失敗しました");
                return null;
            }

            // メッシュとコライダーを生成
            separatedObject.GenerateMeshAndCollider();

            // 影響を受けたチャンクのメッシュとコライダーを更新
            UpdateAffectedChunks(affectedChunks);

            // コライダーを有効化
            EnableColliders(separatedObject);

            // SeparationManagerに登録
            RegisterToSeparationManager(separatedObject);

            return separatedObject;
        }

        /// <summary>
        /// グループからボクセルデータ配列を作成（チャンクからボクセルを削除）
        /// </summary>
        private Voxel[,,] CreateVoxelDataArray(List<Vector3> group, Bounds bounds, Vector3Int size, out HashSet<Vector3Int> affectedChunks)
        {
            // ボクセルデータ配列を作成
            Voxel[,,] voxelData = new Voxel[size.x, size.y, size.z];
            affectedChunks = new HashSet<Vector3Int>();

            //チャンクごとにグループ化
            var chunkGroups = new Dictionary<Vector3Int, List<Vector3>>();

            foreach (var worldPos in group)
            {
                Vector3Int chunkPos = VoxelConstants.WorldToChunkPosition(worldPos);
                if (!chunkGroups.ContainsKey(chunkPos))
                {
                    chunkGroups[chunkPos] = new List<Vector3>();
                }
                chunkGroups[chunkPos].Add(worldPos);
            }

            // チャンクごとに一括処理
            foreach (var kvp in chunkGroups)
            {
                Chunk chunk = m_chunkManager.GetChunk(kvp.Key);
                if (chunk != null)
                {
                    Vector3 chunkWorldPos = VoxelConstants.ChunkToWorldPosition(kvp.Key.x, kvp.Key.y, kvp.Key.z);

                    foreach (var worldPos in kvp.Value)
                    {
                        // チャンクからボクセルデータを取得
                        Vector3 localWorldPos = worldPos - chunkWorldPos;
                        Vector3Int chunkLocalIndex = new Vector3Int(
                            Mathf.FloorToInt(localWorldPos.x / VoxelConstants.VOXEL_SIZE),
                            Mathf.FloorToInt(localWorldPos.y / VoxelConstants.VOXEL_SIZE),
                            Mathf.FloorToInt(localWorldPos.z / VoxelConstants.VOXEL_SIZE)
                        );
                        Voxel originalVoxel = chunk.GetVoxel(chunkLocalIndex);

                        // SeparatedVoxelObjectのローカル座標に変換
                        Vector3 localPos = worldPos - bounds.min;
                        Vector3Int localIndex = new Vector3Int(
                            Mathf.FloorToInt(localPos.x / VoxelConstants.VOXEL_SIZE),
                            Mathf.FloorToInt(localPos.y / VoxelConstants.VOXEL_SIZE),
                            Mathf.FloorToInt(localPos.z / VoxelConstants.VOXEL_SIZE)
                        );

                        // 範囲チェック
                        if (localIndex.x >= 0 && localIndex.x < size.x &&
                            localIndex.y >= 0 && localIndex.y < size.y &&
                            localIndex.z >= 0 && localIndex.z < size.z)
                        {
                            // ボクセルデータ配列に設定
                            voxelData[localIndex.x, localIndex.y, localIndex.z] = originalVoxel;
                        }

                        // チャンクからボクセルを削除
                        chunk.SetVoxel(chunkLocalIndex, Voxel.Empty);
                    }

                    affectedChunks.Add(kvp.Key);
                }
            }

            return voxelData;
        }

        /// <summary>
        /// 影響を受けたチャンクを更新
        /// </summary>
        private void UpdateAffectedChunks(HashSet<Vector3Int> affectedChunks)
        {
            if (m_chunkManager != null && affectedChunks.Count > 0)
            {
                var colliderManager = m_enableAutoMeshUpdate ? m_chunkManager.GetColliderManager() : null;

                foreach (var chunkPos in affectedChunks)
                {
                    // メッシュ更新
                    m_chunkManager.UpdateChunkMesh(chunkPos);

                    // コライダー更新
                    if (colliderManager != null)
                    {
                        colliderManager.UpdateChunkColliders(chunkPos);
                    }
                }
            }
        }

        /// <summary>
        /// 分離オブジェクトのコライダーを有効化
        /// </summary>
        private void EnableColliders(SeparatedVoxelObject separatedObject)
        {
            var colliders = separatedObject.GetComponentsInChildren<Collider>(true);
            foreach (var collider in colliders)
            {
                collider.enabled = true;
            }
        }

        /// <summary>
        /// SeparationManagerに登録
        /// </summary>
        private void RegisterToSeparationManager(SeparatedVoxelObject separatedObject)
        {
            var worldManager = WorldManager.GetInstance();
            var separationManager = worldManager?.SeparationManager;
            if (separationManager != null)
            {
                separationManager.RegisterSeparatedObjectImmediate(separatedObject);
            }
        }


        /// <summary>
        /// サイズ超過グループをチャンクボクセルとして保持
        /// </summary>
        private void RestoreOversizedGroupsToChunks(List<List<Vector3>> oversizedGroups)
        {
            if (oversizedGroups == null || oversizedGroups.Count == 0)
            {
                return;
            }


            // 影響を受けたチャンクを追跡
            HashSet<Vector3Int> affectedChunkPositions = new HashSet<Vector3Int>();
            int emptyGroupCount = 0;
            int validGroupCount = 0;

            foreach (var group in oversizedGroups)
            {
                // 空のグループはスキップ
                if (group == null || group.Count == 0)
                {
                    emptyGroupCount++;
                    continue;
                }

                validGroupCount++;

                // グループ内のボクセルが所属するチャンクを記録
                foreach (var voxelWorldPos in group)
                {
                    Chunk chunk = m_chunkManager.GetChunkFromWorldPosition(voxelWorldPos);
                    if (chunk != null)
                    {
                        affectedChunkPositions.Add(chunk.ChunkPosition);
                    }
                }
            }

            // 影響を受けたチャンクのメッシュとコライダーを更新
            foreach (var chunkPos in affectedChunkPositions)
            {
                // メッシュ更新
                bool meshUpdated = m_chunkManager.UpdateChunkMesh(chunkPos);

                // コライダー更新
                if (m_enableAutoMeshUpdate)
                {
                    var colliderManager = m_chunkManager.GetColliderManager();
                    if (colliderManager != null)
                    {
                        colliderManager.UpdateChunkColliders(chunkPos);
                    }
                }
            }
        }

        /// <summary>
        /// グループのバウンディングボックスを計算
        /// </summary>
        private Bounds CalculateGroupBounds(List<Vector3> group)
        {
            if (group == null || group.Count == 0)
            {
                return new Bounds();
            }

            // 最初のボクセル座標で初期化
            Vector3 min = group[0];
            Vector3 max = group[0];

            // 全ボクセルの最小・最大座標を計算
            foreach (var pos in group)
            {
                min = Vector3.Min(min, pos);
                max = Vector3.Max(max, pos);
            }

            // maxにVOXEL_SIZEを加算して、ボクセルの右上角を取得
            max += Vector3.one * VoxelConstants.VOXEL_SIZE;

            return new Bounds((min + max) * 0.5f, max - min);
        }
    }
}
