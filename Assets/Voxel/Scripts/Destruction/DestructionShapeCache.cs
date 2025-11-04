using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VoxelWorld
{
    /// <summary>
    /// 破壊形状の座標計算結果をキャッシュするシステム
    /// </summary>
    public static class DestructionShapeCache
    {
        // キャッシュストレージ
        private static Dictionary<SphereKey, Vector3[]> s_sphereCache = new Dictionary<SphereKey, Vector3[]>();
        private static Dictionary<BoxKey, Vector3[]> s_boxCache = new Dictionary<BoxKey, Vector3[]>();
        
        // キャッシュ管理設定
        private const int MAX_CACHE_SIZE = 50;
        
        /// <summary>
        /// 球体キャッシュキー
        /// </summary>
        private struct SphereKey
        {
            public Vector3Int gridCenter; // グリッド座標での中心位置
            public float radius;
            
            public SphereKey(Vector3 center, float radius)
            {
                this.gridCenter = new Vector3Int(
                    Mathf.RoundToInt(center.x / VoxelConstants.VOXEL_SIZE),
                    Mathf.RoundToInt(center.y / VoxelConstants.VOXEL_SIZE),
                    Mathf.RoundToInt(center.z / VoxelConstants.VOXEL_SIZE)
                );
                this.radius = radius;
            }
            
            public override bool Equals(object obj)
            {
                return obj is SphereKey other && 
                       gridCenter.Equals(other.gridCenter) && 
                       Mathf.Approximately(radius, other.radius);
            }
            
            public override int GetHashCode()
            {
                return gridCenter.GetHashCode() ^ radius.GetHashCode();
            }
            
            public override string ToString()
            {
                return $"SphereKey(center:{gridCenter}, radius:{radius})";
            }
        }
        
        /// <summary>
        /// 矩形キャッシュキー
        /// </summary>
        private struct BoxKey
        {
            public Vector3Int gridCenter; // グリッド座標での中心位置
            public Vector3 size;
            
            public BoxKey(Vector3 center, Vector3 size)
            {
                this.gridCenter = new Vector3Int(
                    Mathf.RoundToInt(center.x / VoxelConstants.VOXEL_SIZE),
                    Mathf.RoundToInt(center.y / VoxelConstants.VOXEL_SIZE),
                    Mathf.RoundToInt(center.z / VoxelConstants.VOXEL_SIZE)
                );
                this.size = size;
            }
            
            public override bool Equals(object obj)
            {
                return obj is BoxKey other && 
                       gridCenter.Equals(other.gridCenter) && 
                       size.Equals(other.size);
            }
            
            public override int GetHashCode()
            {
                return gridCenter.GetHashCode() ^ size.GetHashCode();
            }
            
            public override string ToString()
            {
                return $"BoxKey(center:{gridCenter}, size:{size})";
            }
        }

        
        /// <summary>
        /// キャッシュされた球体座標を取得、存在しない場合は計算して追加
        /// </summary>
        /// <param name="center">球体の中心座標</param>
        /// <param name="radius">球体の半径</param>
        /// <returns>球体内の座標配列</returns>
        public static Vector3[] GetCachedSpherePositions(Vector3 center, float radius)
        {
            var key = new SphereKey(center, radius);
            
            if (s_sphereCache.TryGetValue(key, out Vector3[] cachedPositions))
            {
                return cachedPositions;
            }
            
            Vector3[] newPositions = CalculateSpherePositions(center, radius);
            
            // キャッシュサイズ管理
            if (s_sphereCache.Count >= MAX_CACHE_SIZE)
            {
                CleanupSphereCache();
            }
            
            s_sphereCache[key] = newPositions;
            
            
            return newPositions;
        }
        
        /// <summary>
        /// 球体内の座標を計算
        /// </summary>
        /// <param name="center">中心座標</param>
        /// <param name="radius">半径</param>
        /// <returns>球体内の座標配列</returns>
        private static Vector3[] CalculateSpherePositions(Vector3 center, float radius)
        {
            var positions = new List<Vector3>();
            
            // ボクセルグリッド単位での範囲計算
            int rangeInVoxels = Mathf.CeilToInt(radius / VoxelConstants.VOXEL_SIZE);
            
            // 中心のボクセル座標を取得
            int centerX = Mathf.RoundToInt(center.x / VoxelConstants.VOXEL_SIZE);
            int centerY = Mathf.RoundToInt(center.y / VoxelConstants.VOXEL_SIZE);
            int centerZ = Mathf.RoundToInt(center.z / VoxelConstants.VOXEL_SIZE);
            
            for (int x = centerX - rangeInVoxels; x <= centerX + rangeInVoxels; x++)
            {
                for (int y = centerY - rangeInVoxels; y <= centerY + rangeInVoxels; y++)
                {
                    for (int z = centerZ - rangeInVoxels; z <= centerZ + rangeInVoxels; z++)
                    {
                        // ボクセルグリッド座標をワールド座標に変換
                        Vector3 worldPos = new Vector3(
                            x * VoxelConstants.VOXEL_SIZE,
                            y * VoxelConstants.VOXEL_SIZE,
                            z * VoxelConstants.VOXEL_SIZE
                        );

                        // 球体内かチェック
                        float distance = Vector3.Distance(worldPos, center);
                        if (distance <= radius)
                        {
                            positions.Add(worldPos);
                        }
                    }
                }
            }

            return positions.ToArray();
        }
        
        /// <summary>
        /// キャッシュされた矩形座標を取得、存在しない場合は計算して追加
        /// </summary>
        /// <param name="center">矩形の中心座標</param>
        /// <param name="size">矩形のサイズ</param>
        /// <returns>矩形内の座標配列</returns>
        public static Vector3[] GetCachedBoxPositions(Vector3 center, Vector3 size)
        {
            var key = new BoxKey(center, size);
            
            if (s_boxCache.TryGetValue(key, out Vector3[] cachedPositions))
            {
                return cachedPositions;
            }
            
            // キャッシュミス: 新規計算
            Vector3[] newPositions = CalculateBoxPositions(center, size);
            
            // キャッシュサイズ管理
            if (s_boxCache.Count >= MAX_CACHE_SIZE)
            {
                CleanupBoxCache();
            }
            
            s_boxCache[key] = newPositions;
            
            return newPositions;
        }
        
        /// <summary>
        /// 矩形内の座標を計算（ボクセルグリッド位置で正確に計算）
        /// </summary>
        /// <param name="center">中心座標</param>
        /// <param name="size">サイズ</param>
        /// <returns>矩形内の座標配列</returns>
        private static Vector3[] CalculateBoxPositions(Vector3 center, Vector3 size)
        {
            var positions = new List<Vector3>();
            
            Vector3 halfSize = size * 0.5f;
            Vector3 min = center - halfSize;
            Vector3 max = center + halfSize;

            // ボクセルグリッド単位で正確に座標範囲を計算
            int minX = Mathf.FloorToInt(min.x / VoxelConstants.VOXEL_SIZE);
            int minY = Mathf.FloorToInt(min.y / VoxelConstants.VOXEL_SIZE);
            int minZ = Mathf.FloorToInt(min.z / VoxelConstants.VOXEL_SIZE);
            
            int maxX = Mathf.CeilToInt(max.x / VoxelConstants.VOXEL_SIZE);
            int maxY = Mathf.CeilToInt(max.y / VoxelConstants.VOXEL_SIZE);
            int maxZ = Mathf.CeilToInt(max.z / VoxelConstants.VOXEL_SIZE);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        // ボクセルグリッド座標をワールド座標に変換
                        Vector3 worldPos = new Vector3(
                            x * VoxelConstants.VOXEL_SIZE,
                            y * VoxelConstants.VOXEL_SIZE,
                            z * VoxelConstants.VOXEL_SIZE
                        );
                        
                        // 矩形範囲内かチェック
                        if (worldPos.x >= min.x && worldPos.x <= max.x &&
                            worldPos.y >= min.y && worldPos.y <= max.y &&
                            worldPos.z >= min.z && worldPos.z <= max.z)
                        {
                            positions.Add(worldPos);
                        }
                    }
                }
            }

            return positions.ToArray();
        }
        
        
        /// <summary>
        /// 球体キャッシュのクリーンアップ
        /// </summary>
        private static void CleanupSphereCache()
        {
            if (s_sphereCache.Count <= MAX_CACHE_SIZE) return;
            
            // 最も古いエントリの半分を削除
            int removeCount = s_sphereCache.Count - MAX_CACHE_SIZE + 10;
            var keysToRemove = s_sphereCache.Keys.Take(removeCount).ToArray();
            
            foreach (var key in keysToRemove)
            {
                s_sphereCache.Remove(key);
            }
            
        }
        
        /// <summary>
        /// 矩形キャッシュのクリーンアップ
        /// </summary>
        private static void CleanupBoxCache()
        {
            if (s_boxCache.Count <= MAX_CACHE_SIZE) return;
            
            // 最も古いエントリの半分を削除
            int removeCount = s_boxCache.Count - MAX_CACHE_SIZE + 10;
            var keysToRemove = s_boxCache.Keys.Take(removeCount).ToArray();
            
            foreach (var key in keysToRemove)
            {
                s_boxCache.Remove(key);
            }
        }
        
        /// <summary>
        /// 全キャッシュクリア
        /// </summary>
        public static void ClearAllCache()
        {
            s_sphereCache.Clear();
            s_boxCache.Clear();
            
            Debug.Log("[DestructionShapeCache] 全キャッシュクリア完了");
        }

    }

}