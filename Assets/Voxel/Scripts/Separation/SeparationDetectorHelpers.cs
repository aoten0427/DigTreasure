using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VoxelWorld
{
    /// <summary>
    /// SeparationDetector ヘルパーメソッド
    /// </summary>
    public partial class SeparationDetector
    {
        /// <summary>
        /// 2つの座標が近傍関係にあるかチェック
        /// </summary>
        /// <param name="pos1">座標1</param>
        /// <param name="pos2">座標2</param>
        /// <returns>近傍関係にある場合true</returns>
        public bool IsNeighborPosition(Vector3 pos1, Vector3 pos2)
        {
            EnsureInitialized();
            var neighbors = VoxelNeighborUtility.GetNeighbors(pos1, m_settings.EnableDiagonalConnection);
            foreach (var neighbor in neighbors)
            {
                if (Vector3.Distance(neighbor, pos2) < VoxelConstants.VOXEL_SIZE * 0.1f)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 指定座標がボクセル座標として有効かチェック
        /// </summary>
        /// <param name="position">チェックする座標</param>
        /// <returns>有効な場合true</returns>
        public bool IsValidVoxelPosition(Vector3 position)
        {
            // 基本的な座標有効性チェック
            return !float.IsNaN(position.x) && !float.IsNaN(position.y) && !float.IsNaN(position.z) &&
                   !float.IsInfinity(position.x) && !float.IsInfinity(position.y) && !float.IsInfinity(position.z);
        }

        /// <summary>
        /// ワールド座標からチャンク座標を取得
        /// </summary>
        /// <param name="worldPosition">ワールド座標</param>
        /// <returns>チャンク座標</returns>
        private Vector3Int GetChunkPosition(Vector3 worldPosition)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPosition.x / (VoxelConstants.CHUNK_WIDTH * VoxelConstants.VOXEL_SIZE)),
                Mathf.FloorToInt(worldPosition.y / (VoxelConstants.CHUNK_HEIGHT * VoxelConstants.VOXEL_SIZE)),
                Mathf.FloorToInt(worldPosition.z / (VoxelConstants.CHUNK_DEPTH * VoxelConstants.VOXEL_SIZE))
            );
        }

        /// <summary>
        /// 破壊された座標の影響範囲にある非空ボクセルを取得
        /// </summary>
        /// <param name="destroyedPositions">破壊された座標リスト</param>
        /// <param name="voxelProvider">ボクセルプロバイダー</param>
        /// <returns>影響範囲の非空ボクセル座標リスト</returns>
        public List<Vector3> GetAffectedVoxels(List<Vector3> destroyedPositions, IVoxelProvider voxelProvider)
        {
            EnsureInitialized();

            var affectedVoxels = new HashSet<Vector3>();

            // 26方向の近傍オフセットを取得（破壊周辺の影響範囲取得には26方向を使用）
            Vector3[] neighborOffsets = VoxelNeighborUtility.GetNeighborOffsets(true);

            foreach (var destroyedPos in destroyedPositions)
            {
                // 破壊されたボクセルの近傍をチェック
                for (int i = 0; i < neighborOffsets.Length; i++)
                {
                    Vector3 neighbor = destroyedPos + neighborOffsets[i];

                    if (voxelProvider.IsNonEmptyVoxel(neighbor))
                    {
                        affectedVoxels.Add(neighbor);
                    }
                }
            }

            return affectedVoxels.ToList();
        }

        /// <summary>
        /// 破壊された座標の影響範囲にある非空ボクセルを取得
        /// </summary>
        /// <param name="destroyedPositions">破壊された座標リスト</param>
        /// <param name="chunkManager">チャンク管理クラス</param>
        /// <returns>影響範囲の非空ボクセル座標リスト</returns>
        public List<Vector3> GetAffectedVoxels(List<Vector3> destroyedPositions, ChunkManager chunkManager)
        {
            var provider = new ChunkManagerVoxelProvider(chunkManager);
            return GetAffectedVoxels(destroyedPositions, provider);
        }

        /// <summary>
        /// 破壊率による影響範囲フィルタリング
        /// </summary>
        /// <param name="destroyedPositions">破壊された座標リスト</param>
        /// <param name="affectedVoxels">影響範囲ボクセルリスト</param>
        /// <param name="voxelProvider">ボクセルプロバイダー</param>
        /// <returns>フィルタリング後の影響範囲ボクセルリスト</returns>
        private List<Vector3> FilterByDestructionRate(List<Vector3> destroyedPositions, List<Vector3> affectedVoxels, IVoxelProvider voxelProvider)
        {
            // ChunkManagerProviderの場合のみ最適化可能
            if (!(voxelProvider is ChunkManagerVoxelProvider chunkProvider))
            {
                return affectedVoxels;
            }

            var chunkManager = chunkProvider.GetChunkManager();
            if (chunkManager == null)
            {
                return affectedVoxels;
            }

            // 破壊されたチャンクごとに破壊数をカウント
            var destroyedCountByChunk = new Dictionary<Vector3Int, int>();
            foreach (var pos in destroyedPositions)
            {
                var chunkPos = VoxelConstants.WorldToChunkPosition(pos);
                if (!destroyedCountByChunk.ContainsKey(chunkPos))
                    destroyedCountByChunk[chunkPos] = 0;
                destroyedCountByChunk[chunkPos]++;
            }

            // 低破壊率チャンクを検出
            var lowDestructionChunks = new HashSet<Vector3Int>();
            foreach (var kvp in destroyedCountByChunk)
            {
                Vector3Int chunkPos = kvp.Key;
                int destroyedCount = kvp.Value;

                var chunk = chunkManager.GetChunk(chunkPos);
                if (chunk == null) continue;

                int totalVoxels = chunk.GetTotalVoxelCount();
                if (totalVoxels == 0) continue;

                float destructionRate = (float)destroyedCount / totalVoxels;

                // 破壊率が低いチャンクをマーク
                if (destructionRate < m_settings.DestructionRateThreshold)
                {
                    lowDestructionChunks.Add(chunkPos);
                }
            }

            if (lowDestructionChunks.Count == 0)
            {
                return affectedVoxels;
            }

            // 低破壊率チャンクのボクセルをフィルタリング
            var filteredVoxels = affectedVoxels
                .Where(v => !lowDestructionChunks.Contains(VoxelConstants.WorldToChunkPosition(v)))
                .ToList();

            return filteredVoxels;
        }
    }
}
