using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration
{
    /// <summary>
    /// 埋められたトンネル生成器（柔らかい岩で繋ぐ）
    /// </summary>
    public class FilledTunnelGenerator : IConnectionGenerator
    {
        private readonly float tunnelRadius;
        private readonly float noiseScale;
        private readonly byte fillVoxelId;
        private readonly byte innerVoxelId;
        private readonly PathGenerationMode pathMode;
        private readonly float curveHeight;

        public ConnectionType SupportedType => ConnectionType.FilledTunnel;

        public FilledTunnelGenerator(
            float tunnelRadius = 4f,
            float noiseScale = 0.2f,
            byte fillVoxelId = 2,  // 柔らかい石
            byte innerVoxelId = 0,  // 空気
            PathGenerationMode pathMode = PathGenerationMode.Bezier,
            float curveHeight = 10f)
        {
            this.tunnelRadius = tunnelRadius;
            this.noiseScale = noiseScale;
            this.fillVoxelId = fillVoxelId;
            this.innerVoxelId = innerVoxelId;
            this.pathMode = pathMode;
            this.curveHeight = curveHeight;
        }

        public async Task<List<VoxelUpdate>> GenerateAsync(ConnectionData connection, int seed)
        {
            var updates = new List<VoxelUpdate>();
            float voxelSize = VoxelConstants.VOXEL_SIZE;

            Vector3 start = connection.SourcePoint.Position;
            Vector3 end = connection.TargetPoint.Position;
            float distance = Vector3.Distance(start, end);

            // パスポイントを生成（直線 or ベジェ曲線）
            List<Vector3> pathPoints = GeneratePathPoints(start, end, connection, distance);

            // トンネルのセグメント数を計算（1メートルごと）
            int segments = pathPoints.Count;

            // 各セグメントでトンネルを生成
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

                            // ノイズを追加して自然な形状に
                            float noise = Mathf.PerlinNoise(
                                (worldPos.x + seed) * 0.1f,
                                (worldPos.z + seed) * 0.1f
                            ) * Mathf.PerlinNoise(
                                (worldPos.y + seed * 2) * 0.1f,
                                (worldPos.x + seed * 3) * 0.1f
                            );
                            noise = (noise - 0.5f) * 2f * noiseScale;

                            float effectiveRadius = tunnelRadius + noise * tunnelRadius;

                            // 外側: 柔らかい石で埋める
                            // 内側: 空気（掘れる状態）
                            if (distFromAxis <= effectiveRadius)
                            {
                                // 内側の半径（プレイヤーが掘る部分）
                                float innerRadius = effectiveRadius * 0.6f;

                                if (distFromAxis <= innerRadius)
                                {
                                    // 内部は空気にしない（柔らかい石で埋める）
                                    updates.Add(new VoxelUpdate(worldPos, fillVoxelId));
                                }
                                else
                                {
                                    // 外側も柔らかい石
                                    updates.Add(new VoxelUpdate(worldPos, fillVoxelId));
                                }
                            }
                        }
                    }
                }

                // フレーム分散
                if (i % 10 == 0)
                {
                    await Task.Yield();
                }
            }

            return updates;
        }

        /// <summary>
        /// パスポイントを生成（直線またはベジェ曲線）
        /// </summary>
        private List<Vector3> GeneratePathPoints(Vector3 start, Vector3 end, ConnectionData connection, float distance)
        {
            var pathPoints = new List<Vector3>();

            if (pathMode == PathGenerationMode.Straight)
            {
                // 直線パス
                int segments = Mathf.CeilToInt(distance / 1f);
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    pathPoints.Add(Vector3.Lerp(start, end, t));
                }
            }
            else // PathGenerationMode.Bezier
            {
                // ベジェ曲線パス
                // 制御点を計算
                Vector3 control1, control2;
                CalculateBezierControlPoints(start, end, connection, out control1, out control2);

                // ベジェ曲線に沿ってポイントを生成
                int segments = Mathf.CeilToInt(distance / 1f);
                for (int i = 0; i <= segments; i++)
                {
                    float t = i / (float)segments;
                    pathPoints.Add(CalculateCubicBezierPoint(t, start, control1, control2, end));
                }
            }

            return pathPoints;
        }

        /// <summary>
        /// ベジェ曲線の制御点を計算
        /// </summary>
        private void CalculateBezierControlPoints(Vector3 start, Vector3 end, ConnectionData connection, out Vector3 control1, out Vector3 control2)
        {
            // 中間点
            Vector3 midPoint = (start + end) * 0.5f;

            // 2つの構造物の位置関係を分析
            float horizontalDist = new Vector3(end.x - start.x, 0, end.z - start.z).magnitude;
            float verticalDiff = Mathf.Abs(end.y - start.y);

            // 曲線の高さを決定（Y軸の差が大きい場合は上方向に迂回）
            float actualCurveHeight = curveHeight;
            if (verticalDiff > 15f)
            {
                // 大きな高度差がある場合は、より高く迂回
                actualCurveHeight = curveHeight + verticalDiff * 0.3f;
            }

            // 制御点1: 開始点から少し進んで上方向にオフセット
            Vector3 startDirection = connection.SourcePoint.Direction;
            control1 = start + startDirection * (horizontalDist * 0.25f) + Vector3.up * actualCurveHeight;

            // 制御点2: 終了点から少し戻って上方向にオフセット
            Vector3 endDirection = connection.TargetPoint.Direction;
            control2 = end - endDirection * (horizontalDist * 0.25f) + Vector3.up * actualCurveHeight;
        }

        /// <summary>
        /// 3次ベジェ曲線上の点を計算
        /// </summary>
        private Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 p = uuu * p0; // (1-t)^3 * P0
            p += 3 * uu * t * p1; // 3(1-t)^2 * t * P1
            p += 3 * u * tt * p2; // 3(1-t) * t^2 * P2
            p += ttt * p3;        // t^3 * P3

            return p;
        }
    }
}
