using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration
{
    /// <summary>
    /// 構造物の基底クラス
    /// </summary>
    public abstract class BaseStructure : IStructure
    {
        protected readonly string id;
        protected readonly int baseSeed;
        protected Vector3 centerPosition;
        protected List<ConnectionPoint> connectionPoints;

        public string Id => id;
        public abstract StructureType Type { get; }
        public abstract int Priority { get; }
        public Vector3 CenterPosition => centerPosition;

        protected BaseStructure(string id, int seed)
        {
            this.id = id;
            this.baseSeed = seed;
            this.connectionPoints = new List<ConnectionPoint>();
        }

        /// <summary>
        /// 構造物を非同期生成
        /// </summary>
        public abstract Task<StructureResult> GenerateAsync(int seed, Bounds fieldBounds);

        /// <summary>
        /// 接続可能かどうかを判定
        /// </summary>
        public abstract bool CanConnectTo(IStructure target);

        /// <summary>
        /// 指定位置に最も近い接続点を取得
        /// </summary>
        public ConnectionPoint GetClosestConnectionPoint(Vector3 targetPosition)
        {
            if (connectionPoints == null || connectionPoints.Count == 0)
                return null;

            ConnectionPoint closest = null;
            float minDistance = float.MaxValue;

            foreach (var point in connectionPoints)
            {
                if (point.IsUsed)
                    continue;

                float distance = Vector3.Distance(point.Position, targetPosition);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = point;
                }
            }

            return closest;
        }

        /// <summary>
        /// バウンディングボックスを取得
        /// </summary>
        public abstract Bounds GetBounds();

        /// <summary>
        /// ドーム型構造用の接続点を生成（水平な側面のみ）
        /// </summary>
        protected List<ConnectionPoint> GenerateDomeConnectionPoints(Vector3 center, float radius, int count)
        {
            var points = new List<ConnectionPoint>();
            if (count <= 0) return points;

            // 水平方向に均等分散
            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle);
                float z = Mathf.Sin(angle);

                // 床より少し上の高さ（中心と同じ高さ）
                Vector3 direction = new Vector3(x, 0, z).normalized;
                Vector3 position = center + direction * radius;

                points.Add(new ConnectionPoint(
                    $"{id}_connection_{i}",
                    position,
                    direction,
                    radius * 0.15f // 接続点の半径
                ));
            }

            return points;
        }

        /// <summary>
        /// 球面上に均等分散した接続点を生成（黄金比を使用）
        /// </summary>
        protected List<ConnectionPoint> GenerateConnectionPoints(Vector3 center, float radius, int count)
        {
            var points = new List<ConnectionPoint>();
            if (count <= 0) return points;

            float goldenRatio = (1f + Mathf.Sqrt(5f)) / 2f;
            float angleIncrement = Mathf.PI * 2f * goldenRatio;

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / count;
                float inclination = Mathf.Acos(1f - 2f * t);
                float azimuth = angleIncrement * i;

                float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
                float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
                float z = Mathf.Cos(inclination);

                Vector3 direction = new Vector3(x, y, z).normalized;
                Vector3 position = center + direction * radius;

                points.Add(new ConnectionPoint(
                    $"{id}_connection_{i}",
                    position,
                    direction,
                    radius * 0.1f // 接続点の半径は構造物の10%
                ));
            }

            return points;
        }

        /// <summary>
        /// ボクセル更新リストを作成するヘルパーメソッド
        /// </summary>
        protected VoxelUpdate CreateVoxelUpdate(Vector3 worldPosition, byte voxelId)
        {
            return new VoxelUpdate(worldPosition, voxelId);
        }

        /// <summary>
        /// 球体領域のボクセル更新リストを生成
        /// </summary>
        protected List<VoxelUpdate> GenerateSphereVoxels(Vector3 center, float radius, byte voxelId)
        {
            var updates = new List<VoxelUpdate>();
            float voxelSize = VoxelConstants.VOXEL_SIZE;

            int range = Mathf.CeilToInt(radius / voxelSize);
            Vector3Int centerVoxel = new Vector3Int(
                Mathf.RoundToInt(center.x / voxelSize),
                Mathf.RoundToInt(center.y / voxelSize),
                Mathf.RoundToInt(center.z / voxelSize)
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

                        float distance = Vector3.Distance(worldPos, center);
                        if (distance <= radius)
                        {
                            updates.Add(CreateVoxelUpdate(worldPos, voxelId));
                        }
                    }
                }
            }

            return updates;
        }

        /// <summary>
        /// 自然な洞窟風ドーム構造を生成（ノイズ付き）
        /// </summary>
        protected List<VoxelUpdate> GenerateNaturalDomeVoxels(
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
            var updates = new List<VoxelUpdate>();
            float voxelSize = VoxelConstants.VOXEL_SIZE;
            float outerRadius = radius + wallThickness;

            int range = Mathf.CeilToInt(outerRadius / voxelSize);
            Vector3Int centerVoxel = new Vector3Int(
                Mathf.RoundToInt(center.x / voxelSize),
                Mathf.RoundToInt(center.y / voxelSize),
                Mathf.RoundToInt(center.z / voxelSize)
            );

            // 床の基準高さ（中心のY座標と同じ）
            float baseFloorY = center.y;

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
                        Vector3 offset = worldPos - center;
                        float horizontalDist = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z);
                        float verticalDist = offset.y;

                        // 3Dノイズで自然な形状を作る
                        float noise3D = Mathf.PerlinNoise(
                            (worldPos.x + noiseSeed) * 0.1f,
                            (worldPos.y + noiseSeed) * 0.1f
                        ) * Mathf.PerlinNoise(
                            (worldPos.z + noiseSeed * 2) * 0.1f,
                            (worldPos.x + noiseSeed * 3) * 0.1f
                        );

                        // ノイズを-0.5~0.5の範囲に正規化
                        noise3D = (noise3D - 0.5f) * 2f;

                        // 床の高さにノイズを適用
                        float floorNoise = Mathf.PerlinNoise(
                            worldPos.x * 0.2f + noiseSeed,
                            worldPos.z * 0.2f + noiseSeed
                        );
                        floorNoise = (floorNoise - 0.5f) * 2f * floorNoiseScale;
                        float floorY = baseFloorY + floorNoise * radius;

                        // 半球の距離計算（Y > center.yの部分のみ）
                        float hemisphereRadius = outerRadius + noise3D * noiseScale * radius;

                        // 床より下の場合
                        if (worldPos.y < floorY)
                        {
                            // 床の範囲内かチェック
                            if (worldPos.y >= floorY - floorThickness && horizontalDist <= radius)
                            {
                                updates.Add(CreateVoxelUpdate(worldPos, floorVoxelId));
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
                            float innerRadius = radius + noise3D * noiseScale * radius * 0.5f;
                            float innerDist = Mathf.Sqrt(
                                horizontalDist * horizontalDist +
                                verticalDist * verticalDist
                            );

                            if (innerDist > innerRadius)
                            {
                                // 壁
                                updates.Add(CreateVoxelUpdate(worldPos, wallVoxelId));
                            }
                            else
                            {
                                // 内部空間
                                updates.Add(CreateVoxelUpdate(worldPos, innerVoxelId));
                            }
                        }
                    }
                }
            }

            return updates;
        }

        /// <summary>
        /// 球殻（空洞のある球）領域のボクセル更新リストを生成
        /// </summary>
        protected List<VoxelUpdate> GenerateSphericalShellVoxels(
            Vector3 center,
            float outerRadius,
            float innerRadius,
            byte outerVoxelId,
            byte innerVoxelId)
        {
            var updates = new List<VoxelUpdate>();
            float voxelSize = VoxelConstants.VOXEL_SIZE;

            int range = Mathf.CeilToInt(outerRadius / voxelSize);
            Vector3Int centerVoxel = new Vector3Int(
                Mathf.RoundToInt(center.x / voxelSize),
                Mathf.RoundToInt(center.y / voxelSize),
                Mathf.RoundToInt(center.z / voxelSize)
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

                        float distance = Vector3.Distance(worldPos, center);

                        if (distance <= outerRadius && distance > innerRadius)
                        {
                            // 外殻部分
                            updates.Add(CreateVoxelUpdate(worldPos, outerVoxelId));
                        }
                        else if (distance <= innerRadius)
                        {
                            // 内部空間
                            updates.Add(CreateVoxelUpdate(worldPos, innerVoxelId));
                        }
                    }
                }
            }

            return updates;
        }
    }
}
