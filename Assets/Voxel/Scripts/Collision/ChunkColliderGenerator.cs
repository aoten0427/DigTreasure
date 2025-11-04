using UnityEngine;
using System.Collections.Generic;

namespace VoxelWorld
{

    /// <summary>
    /// BoxCollider情報
    /// </summary>
    public struct OptimizedBoxCollider
    {
        public Vector3 localCenter;    // チャンク内ローカル座標
        public Vector3 size;          // ワールド単位サイズ
        public Vector3Int voxelMin;   // ボクセル最小座標
        public Vector3Int voxelMax;   // ボクセル最大座標
        
        public int VoxelCount => (voxelMax.x - voxelMin.x + 1) * 
                                (voxelMax.y - voxelMin.y + 1) * 
                                (voxelMax.z - voxelMin.z + 1);
    }

    /// <summary>
    /// ボクセルデータからBoxCollider
    /// </summary>
    public static class ChunkColliderGenerator
    {
        /// <summary>
        /// ボクセルデータからBoxCollider群を生成
        /// </summary>
        public static List<OptimizedBoxCollider> GenerateOptimizedBoxColliders(
            Voxel[,,] voxelData, int maxColliders)
        {
            return GenerateOptimizedBoxColliders(voxelData, 
                new Vector3Int(VoxelConstants.CHUNK_WIDTH, VoxelConstants.CHUNK_HEIGHT, VoxelConstants.CHUNK_DEPTH),
                maxColliders);
        }

        /// <summary>
        /// ボクセルデータから最適化BoxCollider群を生成
        /// </summary>
        /// <param name="voxelData">ボクセルデータ配列</param>
        /// <param name="dataSize">データサイズ（ボクセル単位）</param>
        /// <param name="maxColliders">最大コライダー数</param>
        /// <returns>最適化されたBoxCollider群</returns>
        public static List<OptimizedBoxCollider> GenerateOptimizedBoxColliders(
            Voxel[,,] voxelData, Vector3Int dataSize, int maxColliders)
        {
            
            var (maxX, maxY, maxZ) = GetMaxCombineSizes(dataSize);
            
            var result = new List<OptimizedBoxCollider>();
            bool[,,] processed = new bool[dataSize.x, dataSize.y, dataSize.z];
            
            // 全ボクセル位置をスキャン
            for (int x = 0; x < dataSize.x; x++)
            {
                for (int y = 0; y < dataSize.y; y++)
                {
                    for (int z = 0; z < dataSize.z; z++)
                    {
                        if (voxelData[x, y, z].VoxelId != VoxelConstants.EMPTY_VOXEL_ID && 
                            !processed[x, y, z])
                        {
                            OptimizedBoxCollider box;
                            
                            // 最大数に達している場合は1x1x1の個別コライダーを作成
                            if (result.Count >= maxColliders)
                            {
                                box = CreateSingleVoxelCollider(x, y, z);
                                processed[x, y, z] = true;
                            }
                            else
                            {
                                // 新しいBoxを成長させる
                                box = GrowOptimalBox(voxelData, processed, 
                                    new Vector3Int(x, y, z), maxX, maxY, maxZ, dataSize);
                            }
                            
                            if (box.VoxelCount > 0)
                            {
                                result.Add(box);
                            }
                        }
                    }
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// 単一ボクセル用のコライダーを作成
        /// </summary>
        private static OptimizedBoxCollider CreateSingleVoxelCollider(int x, int y, int z)
        {
            Vector3 size = new Vector3(
                VoxelConstants.VOXEL_SIZE,
                VoxelConstants.VOXEL_SIZE,
                VoxelConstants.VOXEL_SIZE
            );
            
            Vector3 center = new Vector3(
                (x + 0.5f) * VoxelConstants.VOXEL_SIZE,
                (y + 0.5f) * VoxelConstants.VOXEL_SIZE,
                (z + 0.5f) * VoxelConstants.VOXEL_SIZE
            );
            
            return new OptimizedBoxCollider
            {
                localCenter = center,
                size = size,
                voxelMin = new Vector3Int(x, y, z),
                voxelMax = new Vector3Int(x, y, z)
            };
        }
        
        /// <summary>
        /// 最大結合サイズを取得
        /// </summary>
        /// <param name="quality">品質設定</param>
        /// <param name="dataSize">データサイズ（制限として使用）</param>
        /// <returns>最大結合サイズ</returns>
        private static (int x, int y, int z) GetMaxCombineSizes(Vector3Int dataSize)
        {
            return (
                    Mathf.Min(8, dataSize.x),
                    Mathf.Min(8, dataSize.y),
                    Mathf.Min(8, dataSize.z)
                );
        }
        
        /// <summary>
        /// 指定位置から最適なBoxを成長させる
        /// </summary>
        private static OptimizedBoxCollider GrowOptimalBox(
            Voxel[,,] voxelData, bool[,,] processed, Vector3Int start,
            int maxX, int maxY, int maxZ, Vector3Int dataSize)
        {
            Vector3Int min = start;
            Vector3Int max = start;
            
            // X軸方向に拡張
            max.x = ExpandAlongAxis(voxelData, processed, min, max, 0, 
                Mathf.Min(start.x + maxX - 1, dataSize.x - 1));
            
            // Y軸方向に拡張
            max.y = ExpandAlongAxis(voxelData, processed, min, max, 1,
                Mathf.Min(start.y + maxY - 1, dataSize.y - 1));
            
            // Z軸方向に拡張
            max.z = ExpandAlongAxis(voxelData, processed, min, max, 2,
                Mathf.Min(start.z + maxZ - 1, dataSize.z - 1));
            
            MarkAsProcessed(processed, min, max);
            
            // OptimizedBoxColliderを作成
            Vector3 size = new Vector3(
                (max.x - min.x + 1) * VoxelConstants.VOXEL_SIZE,
                (max.y - min.y + 1) * VoxelConstants.VOXEL_SIZE,
                (max.z - min.z + 1) * VoxelConstants.VOXEL_SIZE
            );
            
            Vector3 center = new Vector3(
                (min.x + max.x + 1) * 0.5f * VoxelConstants.VOXEL_SIZE,
                (min.y + max.y + 1) * 0.5f * VoxelConstants.VOXEL_SIZE,
                (min.z + max.z + 1) * 0.5f * VoxelConstants.VOXEL_SIZE
            );
            
            return new OptimizedBoxCollider
            {
                localCenter = center,
                size = size,
                voxelMin = min,
                voxelMax = max
            };
        }
        
        /// <summary>
        /// 指定軸方向に拡張
        /// </summary>
        private static int ExpandAlongAxis(Voxel[,,] voxelData, bool[,,] processed,
            Vector3Int min, Vector3Int max, int axis, int maxCoord)
        {
            int currentMax = axis == 0 ? max.x : (axis == 1 ? max.y : max.z);
            
            for (int coord = currentMax + 1; coord <= maxCoord; coord++)
            {
                if (!CanExpandToLayer(voxelData, processed, min, max, axis, coord))
                {
                    break;
                }
                currentMax = coord;
            }
            
            return currentMax;
        }
        
        /// <summary>
        /// 指定軸の層に拡張可能かチェック
        /// </summary>
        private static bool CanExpandToLayer(Voxel[,,] voxelData, bool[,,] processed,
            Vector3Int min, Vector3Int max, int axis, int layerCoord)
        {
            switch (axis)
            {
                case 0: // X軸
                    for (int y = min.y; y <= max.y; y++)
                        for (int z = min.z; z <= max.z; z++)
                            if (voxelData[layerCoord, y, z].VoxelId == VoxelConstants.EMPTY_VOXEL_ID || 
                                processed[layerCoord, y, z])
                                return false;
                    break;
                    
                case 1: // Y軸
                    for (int x = min.x; x <= max.x; x++)
                        for (int z = min.z; z <= max.z; z++)
                            if (voxelData[x, layerCoord, z].VoxelId == VoxelConstants.EMPTY_VOXEL_ID || 
                                processed[x, layerCoord, z])
                                return false;
                    break;
                    
                case 2: // Z軸
                    for (int x = min.x; x <= max.x; x++)
                        for (int y = min.y; y <= max.y; y++)
                            if (voxelData[x, y, layerCoord].VoxelId == VoxelConstants.EMPTY_VOXEL_ID || 
                                processed[x, y, layerCoord])
                                return false;
                    break;
            }
            
            return true;
        }
        
        /// <summary>
        /// 指定範囲を処理済みとしてマーク
        /// </summary>
        private static void MarkAsProcessed(bool[,,] processed, Vector3Int min, Vector3Int max)
        {
            for (int x = min.x; x <= max.x; x++)
                for (int y = min.y; y <= max.y; y++)
                    for (int z = min.z; z <= max.z; z++)
                        processed[x, y, z] = true;
        }
    }
}