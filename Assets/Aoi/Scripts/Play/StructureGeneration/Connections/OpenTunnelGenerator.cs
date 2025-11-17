using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration
{
    /// <summary>
    /// 開いたトンネル生成器（空気で繋ぐ）
    /// </summary>
    public class OpenTunnelGenerator : IConnectionGenerator
    {
        // 定数
        private const int FRAME_YIELD_INTERVAL = 10;
        private const float DEFAULT_TUNNEL_RADIUS = 3f;
        private const float DEFAULT_NOISE_SCALE = 0.2f;
        private const byte DEFAULT_AIR_VOXEL_ID = 0;
        private const float DEFAULT_CURVE_HEIGHT = 10f;

        private readonly float tunnelRadius;
        private readonly float noiseScale;
        private readonly byte airVoxelId;
        private readonly PathGenerationMode pathMode;
        private readonly float curveHeight;

        public ConnectionType SupportedType => ConnectionType.OpenTunnel;

        public OpenTunnelGenerator(
            float tunnelRadius = DEFAULT_TUNNEL_RADIUS,
            float noiseScale = DEFAULT_NOISE_SCALE,
            byte airVoxelId = DEFAULT_AIR_VOXEL_ID,
            PathGenerationMode pathMode = PathGenerationMode.Bezier,
            float curveHeight = DEFAULT_CURVE_HEIGHT)
        {
            this.tunnelRadius = tunnelRadius;
            this.noiseScale = noiseScale;
            this.airVoxelId = airVoxelId;
            this.pathMode = pathMode;
            this.curveHeight = curveHeight;
        }

        public async Task<List<VoxelUpdate>> GenerateAsync(ConnectionData connection, int seed)
        {
            var voxelUpdates = new List<VoxelUpdate>();
            float voxelSize = VoxelConstants.VOXEL_SIZE;

            Vector3 start = connection.SourcePoint.Position;
            Vector3 end = connection.TargetPoint.Position;

            // パスポイントを生成（PathGeneratorを使用）
            List<Vector3> pathPoints = PathGenerator.GeneratePathPoints(
                start,
                end,
                connection.SourcePoint.Direction,
                connection.TargetPoint.Direction,
                pathMode,
                curveHeight
            );

            // 各セグメントでトンネルを生成
            int segments = pathPoints.Count;
            for (int i = 0; i < segments; i++)
            {
                Vector3 currentPos = pathPoints[i];

                // 現在位置を中心に円柱状にボクセルを配置
                int radiusVoxels = Mathf.CeilToInt(tunnelRadius / voxelSize);
                Vector3Int centerVoxel = new Vector3Int(
                    Mathf.RoundToInt(currentPos.x / voxelSize),
                    Mathf.RoundToInt(currentPos.y / voxelSize),
                    Mathf.RoundToInt(currentPos.z / voxelSize)
                );

                // トンネルに垂直な平面で円形に掘る
                for (int x = -radiusVoxels; x <= radiusVoxels; x++)
                {
                    for (int y = -radiusVoxels; y <= radiusVoxels; y++)
                    {
                        for (int z = -radiusVoxels; z <= radiusVoxels; z++)
                        {
                            Vector3Int voxelPos = centerVoxel + new Vector3Int(x, y, z);
                            Vector3 worldPos = new Vector3(
                                voxelPos.x * voxelSize,
                                voxelPos.y * voxelSize,
                                voxelPos.z * voxelSize
                            );

                            // トンネル軸からの距離を計算
                            Vector3 toVoxel = worldPos - currentPos;
                            float distFromAxis = toVoxel.magnitude;

                            // ノイズを追加して自然な形状に（TunnelNoiseHelperを使用）
                            float noise = TunnelNoiseHelper.CalculateTunnelNoise(worldPos, seed, noiseScale);
                            float effectiveRadius = tunnelRadius + noise * tunnelRadius;

                            if (distFromAxis <= effectiveRadius)
                            {
                                voxelUpdates.Add(new VoxelUpdate(worldPos, airVoxelId));
                            }
                        }
                    }
                }

                // フレーム分散
                if (i % FRAME_YIELD_INTERVAL == 0)
                {
                    await Task.Yield();
                }
            }

            return voxelUpdates;
        }
    }
}
