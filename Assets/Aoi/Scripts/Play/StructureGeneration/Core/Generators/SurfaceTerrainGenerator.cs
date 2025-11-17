using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration
{
    /// <summary>
    /// 地表の設定パラメータ
    /// </summary>
    public class SurfaceSettings
    {
        public float baseHeight;           // 地表の基準高さ（Y座標、通常は0）
        public float centerHeight;         // 中心部の地表高さ（メートル）
        public float edgeHeight;           // 境界部の地表高さ（メートル）
        public float flatCenterRatio;      // 中心部の平坦な範囲（0.0-1.0）
        public float boundaryInset;        // 境界をカバーする内側距離（メートル）
        public byte voxelId;               // 地表のボクセルID
        public float noiseAmplitude;       // ノイズの振幅（メートル）
        public float noiseFrequency;       // ノイズの周波数

        public SurfaceSettings(
            float baseHeight = 0f,
            float centerHeight = 2f,
            float edgeHeight = 15f,
            float flatCenterRatio = 0.3f,
            float boundaryInset = 3f,
            byte voxelId = 2,
            float noiseAmplitude = 1.5f,
            float noiseFrequency = 0.08f)
        {
            this.baseHeight = baseHeight;
            this.centerHeight = centerHeight;
            this.edgeHeight = edgeHeight;
            this.flatCenterRatio = flatCenterRatio;
            this.boundaryInset = boundaryInset;
            this.voxelId = voxelId;
            this.noiseAmplitude = noiseAmplitude;
            this.noiseFrequency = noiseFrequency;
        }
    }

    /// <summary>
    /// 地表地形の生成を担当
    /// </summary>
    public class SurfaceTerrainGenerator
    {
        // 定数
        private const float SURFACE_EDGE_BOOST_HEIGHT = 5f;

        /// <summary>
        /// 地表のボクセルリストを生成（端が高く、中心がなだらか）
        /// 並列処理で高速化
        /// </summary>
        /// <param name="minChunk">最小チャンク座標</param>
        /// <param name="maxChunk">最大チャンク座標</param>
        /// <param name="settings">地表設定</param>
        /// <param name="seed">シード値</param>
        /// <returns>地表のボクセルリスト</returns>
        public async Task<List<VoxelUpdate>> GenerateAsync(
            Vector3Int minChunk,
            Vector3Int maxChunk,
            SurfaceSettings settings,
            int seed)
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

            // マップ中心を計算
            Vector3 mapCenter = (minWorld + maxWorld) * 0.5f;
            mapCenter.y = 0f; // 水平方向の中心のみ使用

            // 最大水平距離を計算（中心から端まで）
            float maxHorizontalDist = Mathf.Max(
                Mathf.Abs(maxWorld.x - mapCenter.x),
                Mathf.Abs(maxWorld.z - mapCenter.z)
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

            Debug.Log($"地表生成: 中心={mapCenter}, 最大距離={maxHorizontalDist}m");

            int startY = Mathf.FloorToInt(settings.baseHeight / voxelSize);
            var surfaceVoxels = new List<VoxelUpdate>();
            var lockObj = new object();

            // X軸を並列処理
            await Task.Run(() =>
            {
                System.Threading.Tasks.Parallel.For(minVoxel.x, maxVoxel.x, x =>
                {
                    var localVoxels = new List<VoxelUpdate>();

                    for (int z = minVoxel.z; z < maxVoxel.z; z++)
                    {
                        // ワールド座標
                        float worldX = x * voxelSize;
                        float worldZ = z * voxelSize;

                        // 中心からの水平距離を計算（チェビシェフ距離：四角形に対応）
                        float horizontalDist = Mathf.Max(
                            Mathf.Abs(worldX - mapCenter.x),
                            Mathf.Abs(worldZ - mapCenter.z)
                        );

                        // 距離比率（0 = 中心、1 = 端）
                        float distanceRatio = Mathf.Clamp01(horizontalDist / maxHorizontalDist);

                        // 高さ補間（中心部の平坦範囲を考慮）
                        float baseHeight = CalculateBaseHeight(distanceRatio, settings);

                        // ノイズを適用
                        Vector3 noisePos = new Vector3(worldX, 0, worldZ);
                        float noise = NoiseUtility.Get2DNoise(
                            noisePos.x,
                            noisePos.z,
                            seed,
                            settings.noiseFrequency
                        );
                        float noisedHeight = baseHeight + noise * settings.noiseAmplitude;

                        // 境界近くは確実に高くする（境界壁をカバー）、ノイズも適用
                        float distanceFromEdge = maxHorizontalDist - horizontalDist;
                        if (distanceFromEdge < settings.boundaryInset)
                        {
                            // 境界に近いほど高さを増やす（最低保証高さ）
                            float edgeBoost = (settings.boundaryInset - distanceFromEdge) / settings.boundaryInset;
                            float minHeight = settings.edgeHeight + edgeBoost * SURFACE_EDGE_BOOST_HEIGHT;

                            // ノイズを適用した高さと最低保証高さの大きい方を使用
                            noisedHeight = Mathf.Max(noisedHeight, minHeight + noise * settings.noiseAmplitude);
                        }

                        // 地表の高さまでボクセルを積み上げる
                        int surfaceHeightVoxels = Mathf.CeilToInt((settings.baseHeight + noisedHeight) / voxelSize);

                        for (int y = startY; y < surfaceHeightVoxels; y++)
                        {
                            Vector3 worldPos = new Vector3(x * voxelSize, y * voxelSize, z * voxelSize);
                            localVoxels.Add(new VoxelUpdate(worldPos, settings.voxelId));
                        }
                    }

                    // スレッドセーフにリストに追加
                    lock (lockObj)
                    {
                        surfaceVoxels.AddRange(localVoxels);
                    }
                });
            });

            await Task.Yield(); // Unityメインスレッドに制御を戻す

            return surfaceVoxels;
        }

        /// <summary>
        /// 距離比率から基本高さを計算
        /// </summary>
        private float CalculateBaseHeight(float distanceRatio, SurfaceSettings settings)
        {
            if (distanceRatio <= settings.flatCenterRatio)
            {
                // 平坦範囲内：中心と同じ高さ
                return settings.centerHeight;
            }
            else
            {
                // 平坦範囲外：平坦範囲の端から境界まで線形補間
                float adjustedRatio = (distanceRatio - settings.flatCenterRatio) / (1f - settings.flatCenterRatio);
                return Mathf.Lerp(
                    settings.centerHeight,
                    settings.edgeHeight,
                    adjustedRatio
                );
            }
        }
    }
}
