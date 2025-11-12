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
        // 定数定義
        protected const float CONNECTION_FAN_ANGLE_DEGREES = 120f;
        protected const float CONNECTION_RADIUS_RATIO = 0.15f;
        private const float DEFAULT_NOISE_SCALE = 0.15f;
        private const float INNER_NOISE_SCALE_RATIO = 0.5f;

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
        /// すべての接続点を取得
        /// </summary>
        public List<ConnectionPoint> GetConnectionPoints()
        {
            return connectionPoints ?? new List<ConnectionPoint>();
        }

        /// <summary>
        /// バウンディングボックスを取得
        /// </summary>
        public abstract Bounds GetBounds();

        /// <summary>
        /// ドーム型構造用の接続点を生成（水平な側面のみ）
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="radius">半径</param>
        /// <param name="count">接続点数</param>
        /// <param name="targetDirection">優先方向（nullの場合は均等分散）</param>
        /// <param name="insetDistance">接続点を内側に配置するオフセット距離（メートル）</param>
        protected List<ConnectionPoint> GenerateDomeConnectionPoints(Vector3 center, float radius, int count, Vector3? targetDirection = null, float insetDistance = 3f,float yoffset = 0)
        {
            var points = new List<ConnectionPoint>();
            if (count <= 0) return points;

            if (targetDirection.HasValue)
            {
                // 目標方向がある場合：その方向を中心に扇状に配置
                Vector3 targetDir = new Vector3(targetDirection.Value.x, 0, targetDirection.Value.z).normalized;
                float baseAngle = Mathf.Atan2(targetDir.z, targetDir.x);

                // 扇の角度範囲
                float fanAngle = CONNECTION_FAN_ANGLE_DEGREES * Mathf.Deg2Rad;
                float angleStep = count > 1 ? fanAngle / (count - 1) : 0f;

                for (int i = 0; i < count; i++)
                {
                    float angle = baseAngle + (i - (count - 1) / 2f) * angleStep;
                    float x = Mathf.Cos(angle);
                    float z = Mathf.Sin(angle);

                    Vector3 direction = new Vector3(x, 0, z).normalized;
                    Vector3 position = center + direction * (radius - insetDistance);
                    position.y += yoffset;  // yoffsetを適用

                    points.Add(new ConnectionPoint(
                        $"{id}_connection_{i}",
                        position,
                        direction,
                        radius * CONNECTION_RADIUS_RATIO
                    ));
                }
            }
            else
            {
                // 目標方向がない場合：水平方向に均等分散
                float angleStep = 360f / count;

                for (int i = 0; i < count; i++)
                {
                    float angle = i * angleStep * Mathf.Deg2Rad;
                    float x = Mathf.Cos(angle);
                    float z = Mathf.Sin(angle);

                    Vector3 direction = new Vector3(x, 0, z).normalized;
                    Vector3 position = center + direction * (radius - insetDistance);
                    position.y += yoffset;

                    points.Add(new ConnectionPoint(
                        $"{id}_connection_{i}",
                        position,
                        direction,
                        radius * CONNECTION_RADIUS_RATIO
                    ));
                }
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

            var (range, centerVoxel) = CalculateVoxelRange(center, outerRadius, voxelSize);
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

                        var noiseValues = CalculateNoiseValues(worldPos, center, noiseSeed, noiseScale, floorNoiseScale, radius, baseFloorY);
                        byte? voxelType = ClassifyDomeVoxelType(
                            worldPos, center, noiseValues,
                            radius, outerRadius, wallThickness, floorThickness,
                            wallVoxelId, floorVoxelId, innerVoxelId);

                        if (voxelType.HasValue)
                        {
                            updates.Add(CreateVoxelUpdate(worldPos, voxelType.Value));
                        }
                    }
                }
            }

            return updates;
        }

        /// <summary>
        /// ボクセル範囲を計算
        /// </summary>
        private (int range, Vector3Int centerVoxel) CalculateVoxelRange(Vector3 center, float outerRadius, float voxelSize)
        {
            int range = Mathf.CeilToInt(outerRadius / voxelSize);
            Vector3Int centerVoxel = new Vector3Int(
                Mathf.RoundToInt(center.x / voxelSize),
                Mathf.RoundToInt(center.y / voxelSize),
                Mathf.RoundToInt(center.z / voxelSize)
            );
            return (range, centerVoxel);
        }

        /// <summary>
        /// ノイズ値の構造体
        /// </summary>
        private struct NoiseValues
        {
            public float noise3D;
            public float floorY;
            public float horizontalDist;
            public float verticalDist;
        }

        /// <summary>
        /// ノイズ値を計算
        /// </summary>
        private NoiseValues CalculateNoiseValues(
            Vector3 worldPos, Vector3 center, int noiseSeed,
            float noiseScale, float floorNoiseScale, float radius, float baseFloorY)
        {
            Vector3 offset = worldPos - center;
            float horizontalDist = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z);
            float verticalDist = offset.y;

            // 3Dノイズで自然な形状を作る
            float noise3D = NoiseUtility.GetSimple3DNoise(worldPos, noiseSeed, 0.1f);

            // 床の高さにノイズを適用
            float floorNoise = NoiseUtility.Get2DNoise(worldPos.x, worldPos.z, noiseSeed, 0.2f) * floorNoiseScale;
            float floorY = baseFloorY + floorNoise * radius;

            return new NoiseValues
            {
                noise3D = noise3D,
                floorY = floorY,
                horizontalDist = horizontalDist,
                verticalDist = verticalDist
            };
        }

        /// <summary>
        /// ドーム型のボクセルタイプを分類
        /// </summary>
        private byte? ClassifyDomeVoxelType(
            Vector3 worldPos, Vector3 center, NoiseValues noise,
            float radius, float outerRadius, float wallThickness, float floorThickness,
            byte wallVoxelId, byte floorVoxelId, byte innerVoxelId)
        {
            // 床より下の場合
            if (worldPos.y < noise.floorY)
            {
                if (worldPos.y >= noise.floorY - floorThickness && noise.horizontalDist <= radius)
                {
                    return floorVoxelId;
                }
                return null;
            }

            // 半球の判定（床より上）
            float hemisphereRadius = outerRadius + noise.noise3D * DEFAULT_NOISE_SCALE * radius;
            float distFromCenter = Mathf.Sqrt(
                noise.horizontalDist * noise.horizontalDist +
                noise.verticalDist * noise.verticalDist
            );

            // 外側の境界内かチェック
            if (distFromCenter <= hemisphereRadius && noise.horizontalDist <= outerRadius)
            {
                float innerRadius = radius + noise.noise3D * DEFAULT_NOISE_SCALE * radius * INNER_NOISE_SCALE_RATIO;
                float innerDist = Mathf.Sqrt(
                    noise.horizontalDist * noise.horizontalDist +
                    noise.verticalDist * noise.verticalDist
                );

                return innerDist > innerRadius ? wallVoxelId : innerVoxelId;
            }

            return null;
        }
    }
}
