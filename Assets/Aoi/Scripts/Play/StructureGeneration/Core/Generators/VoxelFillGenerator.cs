using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration
{
    /// <summary>
    /// 配置範囲を埋めるボクセルの生成を担当
    /// </summary>
    public class VoxelFillGenerator
    {
        /// <summary>
        /// 配置範囲を埋めるボクセルリストを生成（地下のみ、配置はしない）
        /// 並列処理で高速化
        /// </summary>
        /// <param name="minChunk">最小チャンク座標</param>
        /// <param name="maxChunk">最大チャンク座標</param>
        /// <param name="fillVoxelId">埋めるボクセルID</param>
        /// <param name="maxYHeight">最大Y高さ（これより上は埋めない）</param>
        /// <returns>埋めるボクセルリスト</returns>
        public async Task<List<VoxelUpdate>> GenerateAsync(
            Vector3Int minChunk,
            Vector3Int maxChunk,
            byte fillVoxelId,
            float maxYHeight)
        {
            float voxelSize = VoxelConstants.VOXEL_SIZE;

            // チャンク座標からワールド座標範囲を計算
            Vector3 minWorld = new Vector3(
                minChunk.x * VoxelConstants.CHUNK_WIDTH,
                minChunk.y * VoxelConstants.CHUNK_HEIGHT,
                minChunk.z * VoxelConstants.CHUNK_DEPTH
            );
            Vector3 maxWorld = new Vector3(
                (maxChunk.x + 1) * VoxelConstants.CHUNK_WIDTH,
                (maxChunk.y + 1) * VoxelConstants.CHUNK_HEIGHT,
                (maxChunk.z + 1) * VoxelConstants.CHUNK_DEPTH
            );

            // ボクセル範囲を計算
            Vector3Int minVoxel = new Vector3Int(
                Mathf.FloorToInt(minWorld.x / voxelSize),
                Mathf.FloorToInt(minWorld.y / voxelSize),
                Mathf.FloorToInt(minWorld.z / voxelSize)
            );
            Vector3Int maxVoxel = new Vector3Int(
                Mathf.FloorToInt(maxWorld.x / voxelSize),
                Mathf.FloorToInt(maxWorld.y / voxelSize),
                Mathf.FloorToInt(maxWorld.z / voxelSize)
            );

            // 地表の基準高さまでに制限（地下のみ）
            int maxFillY = Mathf.FloorToInt(maxYHeight / voxelSize);
            int actualMaxY = Mathf.Min(maxVoxel.y, maxFillY);

            Debug.Log($"範囲を埋めるボクセル生成（地下のみ）: {minVoxel} - ({maxVoxel.x}, {actualMaxY}, {maxVoxel.z})");

            // X軸方向に分割して並列処理
            int xRange = maxVoxel.x - minVoxel.x;
            int yRange = actualMaxY - minVoxel.y;
            int zRange = maxVoxel.z - minVoxel.z;
            int totalVoxels = xRange * yRange * zRange;

            var fillVoxels = new List<VoxelUpdate>(totalVoxels);
            var lockObj = new object();

            // 並列処理でX軸を分割
            await Task.Run(() =>
            {
                System.Threading.Tasks.Parallel.For(minVoxel.x, maxVoxel.x, x =>
                {
                    var localVoxels = new List<VoxelUpdate>(yRange * zRange);

                    for (int y = minVoxel.y; y < actualMaxY; y++)
                    {
                        for (int z = minVoxel.z; z < maxVoxel.z; z++)
                        {
                            Vector3 worldPos = new Vector3(x * voxelSize, y * voxelSize, z * voxelSize);
                            localVoxels.Add(new VoxelUpdate(worldPos, fillVoxelId));
                        }
                    }

                    // スレッドセーフにリストに追加
                    lock (lockObj)
                    {
                        fillVoxels.AddRange(localVoxels);
                    }
                });
            });

            await Task.Yield(); // Unityメインスレッドに制御を戻す

            return fillVoxels;
        }
    }
}
