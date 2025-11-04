using UnityEngine;
using System.Collections.Generic;
using VoxelWorld;

namespace MapGeneration
{
    /// <summary>
    /// 範囲外エリア（境界壁・崖）生成クラス
    /// プレイエリアの外側に破壊不可の壁を生成
    /// </summary>
    public class BoundaryGenerator
    {
        private CaveGenerationSettings m_settings;
        private int m_seed;
        private VoxelLayerGenerator m_layerGenerator;
        private Vector3Int m_fieldMin;
        private Vector3Int m_fieldMax;

        public BoundaryGenerator(CaveGenerationSettings settings, int seed, VoxelLayerGenerator layerGenerator)
        {
            m_settings = settings;
            m_seed = seed;
            m_layerGenerator = layerGenerator;
        }

        /// <summary>
        /// 範囲外エリアのボクセルデータを生成
        /// </summary>
        public Dictionary<Vector3Int, List<VoxelUpdate>> GenerateBoundary(
            Vector3Int fieldMin,
            Vector3Int fieldMax)
        {
            // フィールド範囲を保存
            m_fieldMin = fieldMin;
            m_fieldMax = fieldMax;

            var boundaryVoxelData = new Dictionary<Vector3Int, List<VoxelUpdate>>();

            // 境界チャンクのリストを取得
            List<Vector3Int> boundaryChunks = GetBoundaryChunks(fieldMin, fieldMax);

            foreach (var chunkPos in boundaryChunks)
            {
                // チャンクがどの境界に属するか判定
                BoundaryDirection direction = GetBoundaryDirection(chunkPos, fieldMin, fieldMax);

                // 外周境界チャンクかチェック（XZ平面で境界に接している）
                bool isPerimeterChunk = IsPerimeterBoundaryChunk(chunkPos, fieldMin, fieldMax);

                // 境界ボクセルを生成
                var chunkVoxels = GenerateBoundaryChunk(chunkPos, direction, isPerimeterChunk);

                if (chunkVoxels.Count > 0)
                {
                    boundaryVoxelData[chunkPos] = chunkVoxels;
                }
            }

            return boundaryVoxelData;
        }

        /// <summary>
        /// 境界チャンクのリストを取得
        /// </summary>
        private List<Vector3Int> GetBoundaryChunks(Vector3Int fieldMin, Vector3Int fieldMax)
        {
            var boundaryChunks = new List<Vector3Int>();
            int thickness = m_settings.boundaryThickness;

            // 計算範囲を拡張
            Vector3Int expandedMin = fieldMin - Vector3Int.one * thickness;
            Vector3Int expandedMax = fieldMax + Vector3Int.one * thickness;

            for (int x = expandedMin.x; x <= expandedMax.x; x++)
            {
                for (int y = expandedMin.y; y <= expandedMax.y; y++)
                {
                    for (int z = expandedMin.z; z <= expandedMax.z; z++)
                    {
                        Vector3Int chunkPos = new Vector3Int(x, y, z);

                        // プレイエリア内なら除外
                        if (IsInsidePlayArea(chunkPos, fieldMin, fieldMax))
                        {
                            continue;
                        }

                        boundaryChunks.Add(chunkPos);
                    }
                }
            }

            return boundaryChunks;
        }

        /// <summary>
        /// チャンクがプレイエリア内かチェック
        /// </summary>
        private bool IsInsidePlayArea(Vector3Int chunkPos, Vector3Int fieldMin, Vector3Int fieldMax)
        {
            return chunkPos.x >= fieldMin.x && chunkPos.x <= fieldMax.x &&
                   chunkPos.y >= fieldMin.y && chunkPos.y <= fieldMax.y &&
                   chunkPos.z >= fieldMin.z && chunkPos.z <= fieldMax.z;
        }

        /// <summary>
        /// ワールド座標のボクセルがプレイエリア内かチェック
        /// </summary>
        private bool IsVoxelInsidePlayArea(Vector3 worldPos)
        {
            // ワールド座標をチャンク座標に変換
            Vector3Int chunkPos = VoxelConstants.WorldToChunkPosition(worldPos);

            // チャンク座標でプレイエリア内かチェック
            return chunkPos.x >= m_fieldMin.x && chunkPos.x <= m_fieldMax.x &&
                   chunkPos.y >= m_fieldMin.y && chunkPos.y <= m_fieldMax.y &&
                   chunkPos.z >= m_fieldMin.z && chunkPos.z <= m_fieldMax.z;
        }

        /// <summary>
        /// チャンクがどの境界方向に属するか判定
        /// </summary>
        private BoundaryDirection GetBoundaryDirection(Vector3Int chunkPos, Vector3Int fieldMin, Vector3Int fieldMax)
        {
            BoundaryDirection direction = BoundaryDirection.None;

            if (chunkPos.x < fieldMin.x) direction |= BoundaryDirection.Left;
            if (chunkPos.x > fieldMax.x) direction |= BoundaryDirection.Right;
            if (chunkPos.y < fieldMin.y) direction |= BoundaryDirection.Down;
            if (chunkPos.y > fieldMax.y) direction |= BoundaryDirection.Up;
            if (chunkPos.z < fieldMin.z) direction |= BoundaryDirection.Back;
            if (chunkPos.z > fieldMax.z) direction |= BoundaryDirection.Forward;

            return direction;
        }

        /// <summary>
        /// 外周境界チャンクかチェック（XZ平面でプレイエリアに隣接または境界外）
        /// </summary>
        private bool IsPerimeterBoundaryChunk(Vector3Int chunkPos, Vector3Int fieldMin, Vector3Int fieldMax)
        {
            // XまたはZ方向でプレイエリアの外側にあるかチェック
            bool isOutsideX = (chunkPos.x < fieldMin.x || chunkPos.x > fieldMax.x);
            bool isOutsideZ = (chunkPos.z < fieldMin.z || chunkPos.z > fieldMax.z);

            // Y方向はプレイエリアの範囲内または地上
            bool isWithinOrAboveYRange = chunkPos.y >= fieldMin.y;

            // XまたはZ方向で外側にあり、Y方向が範囲内または地上なら外周境界チャンク
            return (isOutsideX || isOutsideZ) && isWithinOrAboveYRange;
        }

        /// <summary>
        /// 境界チャンクのボクセルを生成（地下 + 外周崖）
        /// </summary>
        private List<VoxelUpdate> GenerateBoundaryChunk(Vector3Int chunkPos, BoundaryDirection direction, bool isPerimeterChunk)
        {
            var chunkVoxels = new List<VoxelUpdate>();
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

                        // このボクセルがプレイエリア内かチェック（ワールド座標ベース）
                        if (IsVoxelInsidePlayArea(worldPos))
                        {
                            continue; // プレイエリア内のボクセルはスキップ
                        }

                        // 外周境界チャンクで地上レベル以上の場合は崖として生成（完全に埋める）
                        if (isPerimeterChunk && worldPos.y >= m_settings.surfaceLevel)
                        {
                            float maxCliffHeight = m_settings.surfaceLevel + (m_settings.cliffHeightInChunks * VoxelConstants.CHUNK_HEIGHT * VoxelConstants.VOXEL_SIZE);

                            if (worldPos.y < maxCliffHeight)
                            {
                                chunkVoxels.Add(new VoxelUpdate(worldPos, new Voxel(m_settings.boundaryVoxelId)));
                            }
                        }
                        // 地下は常に埋める
                        else if (worldPos.y < m_settings.surfaceLevel)
                        {
                            chunkVoxels.Add(new VoxelUpdate(worldPos, new Voxel(m_settings.boundaryVoxelId)));
                        }
                    }
                }
            }

            return chunkVoxels;
        }

        /// <summary>
        /// 境界方向フラグ
        /// </summary>
        [System.Flags]
        private enum BoundaryDirection
        {
            None = 0,
            Left = 1 << 0,
            Right = 1 << 1,
            Down = 1 << 2,
            Up = 1 << 3,
            Back = 1 << 4,
            Forward = 1 << 5
        }
    }
}
