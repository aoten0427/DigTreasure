using System.Collections.Generic;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration
{
    /// <summary>
    /// 形状タイプ
    /// </summary>
    public enum ShapeType
    {
        Dome,           // 半球ドーム
        EllipsoidDome,  // 扁球ドーム
        Sphere,         // 完全な球
        Cylinder        // 円柱
    }

    /// <summary>
    /// 形状生成パラメータ
    /// </summary>
    public struct ShapeParameters
    {
        public Vector3 center;
        public float horizontalRadius;    // 水平方向の半径（x, z）
        public float verticalRadius;      // 垂直方向の半径（y）
        public float wallThickness;
        public float floorThickness;
        public byte wallVoxelId;
        public byte floorVoxelId;
        public byte innerVoxelId;
        public int noiseSeed;
        public float noiseScale;
        public float floorNoiseScale;

        /// <summary>
        /// 球形・半球用のコンストラクタ（horizontalRadius == verticalRadius）
        /// </summary>
        public ShapeParameters(
            Vector3 center,
            float radius,
            float wallThickness,
            float floorThickness,
            byte wallVoxelId,
            byte floorVoxelId,
            byte innerVoxelId,
            int noiseSeed,
            float noiseScale = 0.15f,
            float floorNoiseScale = 0.1f)
        {
            this.center = center;
            this.horizontalRadius = radius;
            this.verticalRadius = radius;
            this.wallThickness = wallThickness;
            this.floorThickness = floorThickness;
            this.wallVoxelId = wallVoxelId;
            this.floorVoxelId = floorVoxelId;
            this.innerVoxelId = innerVoxelId;
            this.noiseSeed = noiseSeed;
            this.noiseScale = noiseScale;
            this.floorNoiseScale = floorNoiseScale;
        }

        /// <summary>
        /// 扁球用のコンストラクタ（horizontalRadius != verticalRadius）
        /// </summary>
        public ShapeParameters(
            Vector3 center,
            float horizontalRadius,
            float verticalRadius,
            float wallThickness,
            float floorThickness,
            byte wallVoxelId,
            byte floorVoxelId,
            byte innerVoxelId,
            int noiseSeed,
            float noiseScale = 0.15f,
            float floorNoiseScale = 0.1f)
        {
            this.center = center;
            this.horizontalRadius = horizontalRadius;
            this.verticalRadius = verticalRadius;
            this.wallThickness = wallThickness;
            this.floorThickness = floorThickness;
            this.wallVoxelId = wallVoxelId;
            this.floorVoxelId = floorVoxelId;
            this.innerVoxelId = innerVoxelId;
            this.noiseSeed = noiseSeed;
            this.noiseScale = noiseScale;
            this.floorNoiseScale = floorNoiseScale;
        }
    }

    /// <summary>
    /// ボクセル形状ジェネレーター
    /// </summary>
    public static class VoxelShapeGenerator
    {
        /// <summary>
        /// 半球ドームを生成（床の上に半球形）
        /// </summary>
        public static List<VoxelUpdate> GenerateDome(ShapeParameters param)
        {
            var updates = new List<VoxelUpdate>();
            float voxelSize = VoxelConstants.VOXEL_SIZE;
            float outerRadius = param.horizontalRadius + param.wallThickness;

            int range = Mathf.CeilToInt(outerRadius / voxelSize);
            Vector3Int centerVoxel = new Vector3Int(
                Mathf.RoundToInt(param.center.x / voxelSize),
                Mathf.RoundToInt(param.center.y / voxelSize),
                Mathf.RoundToInt(param.center.z / voxelSize)
            );

            // 床の基準高さ（中心のY座標と同じ）
            float baseFloorY = param.center.y;

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    for (int z = -range; z <= range; z++)
                    {
                        Vector3Int voxelPos = centerVoxel + new Vector3Int(x, y, z);
                        Vector3 worldPos = new Vector3(
                            voxelPos.x * voxelSize,
                            voxelPos.y * voxelSize,
                            voxelPos.z * voxelSize
                        );

                        // 中心からの水平距離と垂直距離
                        Vector3 offset = worldPos - param.center;
                        float horizontalDist = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z);
                        float verticalDist = offset.y;

                        // 3Dノイズで自然な形状を作る
                        float noise3D = Mathf.PerlinNoise(
                            (worldPos.x + param.noiseSeed) * 0.1f,
                            (worldPos.y + param.noiseSeed) * 0.1f
                        ) * Mathf.PerlinNoise(
                            (worldPos.z + param.noiseSeed * 2) * 0.1f,
                            (worldPos.x + param.noiseSeed * 3) * 0.1f
                        );

                        // ノイズを-0.5~0.5の範囲に正規化
                        noise3D = (noise3D - 0.5f) * 2f;

                        // 床の高さにノイズを適用
                        float floorNoise = Mathf.PerlinNoise(
                            worldPos.x * 0.2f + param.noiseSeed,
                            worldPos.z * 0.2f + param.noiseSeed
                        );
                        floorNoise = (floorNoise - 0.5f) * 2f * param.floorNoiseScale;
                        float floorY = baseFloorY + floorNoise * param.horizontalRadius;

                        // 半球の距離計算（Y > center.yの部分のみ）
                        float hemisphereRadius = outerRadius + noise3D * param.noiseScale * param.horizontalRadius;

                        // 床より下の場合
                        if (worldPos.y < floorY)
                        {
                            // 床の範囲内かチェック
                            if (worldPos.y >= floorY - param.floorThickness && horizontalDist <= param.horizontalRadius)
                            {
                                updates.Add(new VoxelUpdate(worldPos, param.floorVoxelId));
                            }
                            continue;
                        }

                        // 半球の判定（床より上）
                        float distFromCenter = Mathf.Sqrt(
                            horizontalDist * horizontalDist +
                            verticalDist * verticalDist
                        );

                        // 外側の境界
                        if (distFromCenter <= hemisphereRadius && horizontalDist <= outerRadius)
                        {
                            // 内側の境界（ノイズ適用）
                            float innerRadius = param.horizontalRadius + noise3D * param.noiseScale * param.horizontalRadius * 0.5f;
                            float innerDist = Mathf.Sqrt(
                                horizontalDist * horizontalDist +
                                verticalDist * verticalDist
                            );

                            if (innerDist > innerRadius)
                            {
                                // 壁
                                updates.Add(new VoxelUpdate(worldPos, param.wallVoxelId));
                            }
                            else
                            {
                                // 内部空間
                                updates.Add(new VoxelUpdate(worldPos, param.innerVoxelId));
                            }
                        }
                    }
                }
            }

            return updates;
        }

        /// <summary>
        /// 扁球ドームを生成（床の上に扁平な半楕円体）
        /// </summary>
        public static List<VoxelUpdate> GenerateEllipsoidDome(ShapeParameters param)
        {
            var updates = new List<VoxelUpdate>();
            float voxelSize = VoxelConstants.VOXEL_SIZE;

            // 外側の半径（壁の厚さを含む）
            float outerHorizontalRadius = param.horizontalRadius + param.wallThickness;
            float outerVerticalRadius = param.verticalRadius + param.wallThickness;

            // 処理範囲を計算（水平方向の半径を基準）
            int range = Mathf.CeilToInt(outerHorizontalRadius / voxelSize);
            Vector3Int centerVoxel = new Vector3Int(
                Mathf.RoundToInt(param.center.x / voxelSize),
                Mathf.RoundToInt(param.center.y / voxelSize),
                Mathf.RoundToInt(param.center.z / voxelSize)
            );

            // 床の基準高さ（中心のY座標と同じ）
            float baseFloorY = param.center.y;

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    for (int z = -range; z <= range; z++)
                    {
                        Vector3Int voxelPos = centerVoxel + new Vector3Int(x, y, z);
                        Vector3 worldPos = new Vector3(
                            voxelPos.x * voxelSize,
                            voxelPos.y * voxelSize,
                            voxelPos.z * voxelSize
                        );

                        // 中心からのオフセット
                        Vector3 offset = worldPos - param.center;
                        float horizontalDist = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z);
                        float verticalDist = offset.y;

                        // 3Dノイズで自然な形状を作る
                        float noise3D = Mathf.PerlinNoise(
                            (worldPos.x + param.noiseSeed) * 0.1f,
                            (worldPos.y + param.noiseSeed) * 0.1f
                        ) * Mathf.PerlinNoise(
                            (worldPos.z + param.noiseSeed * 2) * 0.1f,
                            (worldPos.x + param.noiseSeed * 3) * 0.1f
                        );

                        // ノイズを-0.5~0.5の範囲に正規化
                        noise3D = (noise3D - 0.5f) * 2f;

                        // 床の高さにノイズを適用
                        float floorNoise = Mathf.PerlinNoise(
                            worldPos.x * 0.2f + param.noiseSeed,
                            worldPos.z * 0.2f + param.noiseSeed
                        );
                        floorNoise = (floorNoise - 0.5f) * 2f * param.floorNoiseScale;
                        float floorY = baseFloorY + floorNoise * param.horizontalRadius;

                        // 床より下の場合
                        if (worldPos.y < floorY)
                        {
                            // 床の範囲内かチェック
                            if (worldPos.y >= floorY - param.floorThickness && horizontalDist <= param.horizontalRadius)
                            {
                                updates.Add(new VoxelUpdate(worldPos, param.floorVoxelId));
                            }
                            continue;
                        }

                        // 扁球の判定（床より上）
                        // 楕円体の距離式: (dx²+dz²)/a² + dy²/b² <= 1
                        // ノイズを適用した半径
                        float noisedHorizontalRadius = outerHorizontalRadius + noise3D * param.noiseScale * param.horizontalRadius;
                        float noisedVerticalRadius = outerVerticalRadius + noise3D * param.noiseScale * param.verticalRadius;

                        // 外側の楕円体判定
                        float outerEllipsoidDist =
                            (horizontalDist * horizontalDist) / (noisedHorizontalRadius * noisedHorizontalRadius) +
                            (verticalDist * verticalDist) / (noisedVerticalRadius * noisedVerticalRadius);

                        if (outerEllipsoidDist <= 1.0f && horizontalDist <= outerHorizontalRadius)
                        {
                            // 内側の楕円体判定（ノイズを弱めに適用）
                            float innerHorizontalRadius = param.horizontalRadius + noise3D * param.noiseScale * param.horizontalRadius * 0.5f;
                            float innerVerticalRadius = param.verticalRadius + noise3D * param.noiseScale * param.verticalRadius * 0.5f;

                            float innerEllipsoidDist =
                                (horizontalDist * horizontalDist) / (innerHorizontalRadius * innerHorizontalRadius) +
                                (verticalDist * verticalDist) / (innerVerticalRadius * innerVerticalRadius);

                            if (innerEllipsoidDist > 1.0f)
                            {
                                // 壁
                                updates.Add(new VoxelUpdate(worldPos, param.wallVoxelId));
                            }
                            else
                            {
                                // 内部空間
                                updates.Add(new VoxelUpdate(worldPos, param.innerVoxelId));
                            }
                        }
                    }
                }
            }

            return updates;
        }

        /// <summary>
        /// 完全な球体を生成
        /// </summary>
        public static List<VoxelUpdate> GenerateSphere(ShapeParameters param)
        {
            var updates = new List<VoxelUpdate>();
            float voxelSize = VoxelConstants.VOXEL_SIZE;
            float outerRadius = param.horizontalRadius + param.wallThickness;

            int range = Mathf.CeilToInt(outerRadius / voxelSize);
            Vector3Int centerVoxel = new Vector3Int(
                Mathf.RoundToInt(param.center.x / voxelSize),
                Mathf.RoundToInt(param.center.y / voxelSize),
                Mathf.RoundToInt(param.center.z / voxelSize)
            );

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    for (int z = -range; z <= range; z++)
                    {
                        Vector3Int voxelPos = centerVoxel + new Vector3Int(x, y, z);
                        Vector3 worldPos = new Vector3(
                            voxelPos.x * voxelSize,
                            voxelPos.y * voxelSize,
                            voxelPos.z * voxelSize
                        );

                        // 3Dノイズで自然な形状を作る
                        float noise3D = Mathf.PerlinNoise(
                            (worldPos.x + param.noiseSeed) * 0.1f,
                            (worldPos.y + param.noiseSeed) * 0.1f
                        ) * Mathf.PerlinNoise(
                            (worldPos.z + param.noiseSeed * 2) * 0.1f,
                            (worldPos.x + param.noiseSeed * 3) * 0.1f
                        );

                        noise3D = (noise3D - 0.5f) * 2f;

                        // ノイズを適用した半径
                        float noisedOuterRadius = outerRadius + noise3D * param.noiseScale * param.horizontalRadius;
                        float noisedInnerRadius = param.horizontalRadius + noise3D * param.noiseScale * param.horizontalRadius * 0.5f;

                        float distance = Vector3.Distance(worldPos, param.center);

                        if (distance <= noisedOuterRadius)
                        {
                            if (distance > noisedInnerRadius)
                            {
                                // 壁
                                updates.Add(new VoxelUpdate(worldPos, param.wallVoxelId));
                            }
                            else
                            {
                                // 内部空間
                                updates.Add(new VoxelUpdate(worldPos, param.innerVoxelId));
                            }
                        }
                    }
                }
            }

            return updates;
        }
    }
}
