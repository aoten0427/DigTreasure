using UnityEngine;
using VoxelWorld;

namespace MapGeneration
{
    /// <summary>
    /// 洞窟を生成するクラス
    /// </summary>
    public class CaveGenerator
    {
        private CaveGenerationSettings m_settings;

        public CaveGenerator(CaveGenerationSettings settings)
        {
            m_settings = settings;
        }

        /// <summary>
        /// 洞窟システムを生成
        /// </summary>
        public CaveSystem Generate(int seed, Vector3Int fieldMin, Vector3Int fieldMax)
        {
            Random.InitState(seed);
            var caveSystem = new CaveSystem(seed);

            // フィールド境界のワールド座標を計算
            float fieldMinX = fieldMin.x * 16 * VoxelConstants.VOXEL_SIZE;
            float fieldMaxX = fieldMax.x * 16 * VoxelConstants.VOXEL_SIZE;
            float fieldMinY = fieldMin.y * 16 * VoxelConstants.VOXEL_SIZE;
            float fieldMaxY = fieldMax.y * 16 * VoxelConstants.VOXEL_SIZE;
            float fieldMinZ = fieldMin.z * 16 * VoxelConstants.VOXEL_SIZE;
            float fieldMaxZ = fieldMax.z * 16 * VoxelConstants.VOXEL_SIZE;

            // 洞窟数を決定
            int caveCount = Random.Range(m_settings.minCaveCount, m_settings.maxCaveCount + 1);

            // マージンを計算（平均的な洞窟サイズで計算）
            float avgRadius = (m_settings.minCaveRadius + m_settings.maxCaveRadius) * 0.5f;
            float maxRadius = avgRadius * m_settings.horizontalScaleMultiplier;
            float avgWalkSteps = (m_settings.minWalkSteps + m_settings.maxWalkSteps) * 0.5f;
            float estimatedMaxDistance = avgRadius * avgWalkSteps * 0.1f;
            float margin = Mathf.Max(maxRadius * 2, estimatedMaxDistance) * 0.5f;

            // マージン付きの配置範囲
            float marginMinX = fieldMinX + margin;
            float marginMaxX = fieldMaxX - margin;
            float marginMinZ = fieldMinZ + margin;
            float marginMaxZ = fieldMaxZ - margin;

            // 範囲が狭すぎる場合は警告してマージンを縮小
            if (marginMinX >= marginMaxX || marginMinZ >= marginMaxZ)
            {
                margin = Mathf.Min((fieldMaxX - fieldMinX) * 0.1f, (fieldMaxZ - fieldMinZ) * 0.1f);
                marginMinX = fieldMinX + margin;
                marginMaxX = fieldMaxX - margin;
                marginMinZ = fieldMinZ + margin;
                marginMaxZ = fieldMaxZ - margin;
            }

            // Poisson Disk Samplingで洞窟位置を生成
            Vector2 minBounds = new Vector2(marginMinX, marginMinZ);
            Vector2 maxBounds = new Vector2(marginMaxX, marginMaxZ);

            var allCavePositions = PoissonDiskSampling.GeneratePoints(
                minBounds,
                maxBounds,
                m_settings.minCaveDistance,
                30,
                seed
            );

            // 生成された位置からランダムにcaveCount個を選択
            var selectedPositions = PoissonDiskSampling.SelectRandomPoints(allCavePositions, caveCount, seed + 1);

            Debug.Log($"[CaveGenerator] Poisson生成: {allCavePositions.Count}個 → 選択: {selectedPositions.Count}個");

            // 各位置に洞窟を生成
            for (int i = 0; i < selectedPositions.Count; i++)
            {
                Vector2 pos2D = selectedPositions[i];

                // 深度から洞窟パラメータを計算
                float startY = Random.Range(m_settings.minDepth, m_settings.maxDepth);
                float depth = Mathf.Abs(startY);
                float radius = GetCaveRadiusByDepth(depth);
                int walkSteps = GetWalkStepsByDepth(depth);

                // 3D開始位置を作成
                Vector3 startPosition = new Vector3(pos2D.x, startY, pos2D.y);

                // 境界情報を渡して洞窟を生成
                var cave = GenerateRandomWalkCave(
                    startPosition,
                    radius,
                    walkSteps,
                    new Vector3(fieldMinX, fieldMinY, fieldMinZ),
                    new Vector3(fieldMaxX, fieldMaxY, fieldMaxZ),
                    margin
                );
                caveSystem.Caves.Add(cave);
            }

            return caveSystem;
        }

        /// <summary>
        /// 深度に応じた洞窟半径を取得
        /// </summary>
        private float GetCaveRadiusByDepth(float depth)
        {
            float maxDepth = Mathf.Abs(m_settings.maxDepth);
            float t = Mathf.Clamp01(depth / maxDepth);
            return Mathf.Lerp(m_settings.minCaveRadius, m_settings.maxCaveRadius, t);
        }

        /// <summary>
        /// 深度に応じたウォーク歩数を取得
        /// </summary>
        private int GetWalkStepsByDepth(float depth)
        {
            float maxDepth = Mathf.Abs(m_settings.maxDepth);
            float t = Mathf.Clamp01(depth / maxDepth);
            return Mathf.RoundToInt(Mathf.Lerp(m_settings.minWalkSteps, m_settings.maxWalkSteps, t));
        }

        /// <summary>
        /// 水平優先ランダムウォークで洞窟を生成
        /// </summary>
        private CaveData GenerateRandomWalkCave(Vector3 startPosition, float radius, int walkSteps, Vector3 fieldMin, Vector3 fieldMax, float margin)
        {
            var cave = new CaveData(startPosition);
            Vector3 currentPosition = startPosition;

            // 初期方向を水平寄りに設定
            Vector3 currentDirection = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-0.3f, 0.3f) * m_settings.verticalMovementFactor,
                Random.Range(-1f, 1f)
            ).normalized;

            for (int step = 0; step < walkSteps; step++)
            {
                // 現在位置に楕円体を配置
                Vector3 scale = new Vector3(
                    radius * m_settings.horizontalScaleMultiplier,  // X軸（横）
                    radius,                                          // Y軸（縦）
                    radius * m_settings.horizontalScaleMultiplier   // Z軸（横）
                );
                cave.Spheres.Add(new CaveSphere(currentPosition, scale));

                // ランダム方向を生成してY成分を制限
                Vector3 randomDirection = Random.onUnitSphere;
                randomDirection.y *= m_settings.verticalMovementFactor;
                randomDirection.Normalize();

                // 方向を変更
                float angleChange = Random.Range(m_settings.minDirectionChange, m_settings.maxDirectionChange);
                Vector3 randomAxis = Random.onUnitSphere;
                currentDirection = Quaternion.AngleAxis(angleChange, randomAxis) * currentDirection;

                // 水平方向へのバイアスを適用
                Vector3 horizontalDirection = new Vector3(currentDirection.x, 0, currentDirection.z).normalized;
                currentDirection = Vector3.Lerp(currentDirection, horizontalDirection, m_settings.horizontalBias).normalized;

                // 次の位置に移動
                float moveDistance = radius * Random.Range(0.5f, 1.0f);
                currentPosition += currentDirection * moveDistance;

                // 境界内にクランプ
                currentPosition.x = Mathf.Clamp(currentPosition.x, fieldMin.x + margin, fieldMax.x - margin);
                currentPosition.y = Mathf.Clamp(currentPosition.y, fieldMin.y + margin, fieldMax.y - margin);
                currentPosition.z = Mathf.Clamp(currentPosition.z, fieldMin.z + margin, fieldMax.z - margin);
            }

            return cave;
        }
    }
}
