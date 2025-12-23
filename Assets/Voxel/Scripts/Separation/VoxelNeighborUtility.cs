using UnityEngine;
using System.Collections.Generic;

namespace VoxelWorld
{
    /// <summary>
    /// ボクセル近隣検索ユーティリティ
    /// </summary>
    public static class VoxelNeighborUtility
    {
        // 6方向の近隣オフセット（上下左右前後）
        private static readonly Vector3[] s_sixDirections = {
            Vector3.up,    Vector3.down,
            Vector3.left,  Vector3.right,
            Vector3.forward, Vector3.back
        };

        // 26方向の近隣オフセット（対角線含む）
        private static readonly Vector3[] s_twentySixDirections;

        // キャッシュ済みオフセット配列（VOXEL_SIZEでスケール済み）
        private static readonly Vector3[] s_sixDirectionOffsets;
        private static readonly Vector3[] s_twentySixDirectionOffsets;

        static VoxelNeighborUtility()
        {
            // 26方向の方向ベクトル生成
            var directions = new List<Vector3>();
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && y == 0 && z == 0) continue;
                        directions.Add(new Vector3(x, y, z));
                    }
                }
            }
            s_twentySixDirections = directions.ToArray();

            // スケール済みオフセット配列をキャッシュ（毎回生成を避ける）
            s_sixDirectionOffsets = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                s_sixDirectionOffsets[i] = s_sixDirections[i] * VoxelConstants.VOXEL_SIZE;
            }

            s_twentySixDirectionOffsets = new Vector3[26];
            for (int i = 0; i < 26; i++)
            {
                s_twentySixDirectionOffsets[i] = s_twentySixDirections[i] * VoxelConstants.VOXEL_SIZE;
            }
        }

        /// <summary>
        /// 近隣座標取得（6方向）
        /// </summary>
        public static Vector3[] GetSixDirectionNeighbors(Vector3 position)
        {
            var neighbors = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                neighbors[i] = position + s_sixDirections[i] * VoxelConstants.VOXEL_SIZE;
            }
            return neighbors;
        }

        /// <summary>
        /// 近隣座標取得（26方向）
        /// </summary>
        public static Vector3[] GetTwentySixDirectionNeighbors(Vector3 position)
        {
            var neighbors = new Vector3[26];
            for (int i = 0; i < 26; i++)
            {
                neighbors[i] = position + s_twentySixDirections[i] * VoxelConstants.VOXEL_SIZE;
            }
            return neighbors;
        }

        /// <summary>
        /// 設定に基づく近隣座標取得
        /// </summary>
        public static Vector3[] GetNeighbors(Vector3 position, bool includeDiagonal)
        {
            return includeDiagonal ?
                GetTwentySixDirectionNeighbors(position) :
                GetSixDirectionNeighbors(position);
        }

        /// <summary>
        /// キャッシュ済みオフセット配列を取得（配列の中身を変更しないこと）
        /// </summary>
        public static Vector3[] GetNeighborOffsets(bool includeDiagonal)
        {
            return includeDiagonal ? s_twentySixDirectionOffsets : s_sixDirectionOffsets;
        }

        /// <summary>
        /// 範囲内近隣フィルタリング
        /// </summary>
        public static List<Vector3> FilterValidNeighbors(Vector3[] neighbors, IVoxelProvider provider)
        {
            var validNeighbors = new List<Vector3>();
            foreach (var neighbor in neighbors)
            {
                if (provider.ExistsInWorld(neighbor))
                {
                    validNeighbors.Add(neighbor);
                }
            }
            return validNeighbors;
        }
    }
}
