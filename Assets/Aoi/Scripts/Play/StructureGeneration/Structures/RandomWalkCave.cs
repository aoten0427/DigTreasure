using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration
{
    /// <summary>
    /// ランダムウォーク洞窟：ランダムな経路で生成される自然な洞窟
    /// </summary>
    public class RandomWalkCave : BaseStructure
    {
        private readonly RandomWalkCaveSettings settings;
        private Bounds bounds;
        private List<Vector3> walkPath;

        public override StructureType Type => StructureType.RandomWalkCave;
        public override int Priority => settings.priority;

        public RandomWalkCave(string id, int seed, RandomWalkCaveSettings settings)
            : base(id, seed)
        {
            this.settings = settings;
            this.walkPath = new List<Vector3>();
        }

        public override async Task<StructureResult> GenerateAsync(int seed, Bounds fieldBounds)
        {
            // シードを初期化
            Random.InitState(seed);

            // パラメータを決定
            int walkSteps = Random.Range(settings.minWalkSteps, settings.maxWalkSteps);
            float actualRadius = settings.tunnelRadius;

            // 開始位置を決定
            float startY = Random.Range(settings.minStartYPosition, settings.maxStartYPosition);
            float startX = Random.Range(
                fieldBounds.min.x + actualRadius,
                fieldBounds.max.x - actualRadius
            );
            float startZ = Random.Range(
                fieldBounds.min.z + actualRadius,
                fieldBounds.max.z - actualRadius
            );

            Vector3 currentPos = new Vector3(startX, startY, startZ);
            centerPosition = currentPos;
            walkPath.Add(currentPos);

            // 初期方向をランダムに決定（水平方向主体）
            Vector3 currentDirection = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-0.1f, 0.1f), // Y方向の変化をさらに抑制
                Random.Range(-1f, 1f)
            ).normalized;

            // ボクセルデータを格納
            var voxelUpdates = new List<VoxelUpdate>();
            Vector3 minBounds = currentPos;
            Vector3 maxBounds = currentPos;

            // ランダムウォーク
            for (int step = 0; step < walkSteps; step++)
            {
                // 方向変更の判定
                if (Random.value < settings.directionChangeChance)
                {
                    // 水平方向（Y軸周り）の回転のみ
                    float angleChange = Random.Range(-settings.maxDirectionAngle, settings.maxDirectionAngle);
                    currentDirection = Quaternion.Euler(0, angleChange, 0) * currentDirection;

                    // Y方向のわずかな変化を追加（より控えめに）
                    currentDirection.y += Random.Range(-0.05f, 0.05f);
                    currentDirection = currentDirection.normalized;
                }

                // 次の位置へ移動
                currentPos += currentDirection * settings.stepDistance;

                // フィールド範囲内に制限
                currentPos.x = Mathf.Clamp(currentPos.x, fieldBounds.min.x + actualRadius, fieldBounds.max.x - actualRadius);
                currentPos.y = Mathf.Clamp(currentPos.y, fieldBounds.min.y + actualRadius, fieldBounds.max.y - actualRadius);
                currentPos.z = Mathf.Clamp(currentPos.z, fieldBounds.min.z + actualRadius, fieldBounds.max.z - actualRadius);

                walkPath.Add(currentPos);

                // 境界を更新
                minBounds = Vector3.Min(minBounds, currentPos - Vector3.one * actualRadius);
                maxBounds = Vector3.Max(maxBounds, currentPos + Vector3.one * actualRadius);

                // この位置にドーム型の洞窟を配置
                var domeVoxels = GenerateNaturalDomeVoxels(
                    currentPos,
                    actualRadius,
                    actualRadius * 0.2f, // 壁の厚さ（薄め）
                    actualRadius * 0.3f, // 床の厚さ
                    2, // 壁のボクセルID（柔らかい石）
                    2, // 床のボクセルID
                    settings.innerVoxelId,
                    seed + step, // ステップごとに異なるシード
                    0.2f, // ノイズスケール
                    0.15f // 床のノイズスケール
                );
                voxelUpdates.AddRange(domeVoxels);

                // フレーム分散（10ステップごと）
                if (step % 10 == 0)
                {
                    await Task.Yield();
                }
            }

            // 中心位置を経路の中央に設定
            centerPosition = walkPath[walkPath.Count / 2];

            // バウンディングボックスを計算
            Vector3 size = maxBounds - minBounds;
            Vector3 center = (minBounds + maxBounds) * 0.5f;
            bounds = new Bounds(center, size);

            // 接続点を生成（経路の始点と終点付近）
            connectionPoints = GeneratePathEndConnectionPoints(
                walkPath[0],
                walkPath[walkPath.Count - 1],
                actualRadius,
                settings.maxConnectionPoints
            );

            return new StructureResult
            {
                VoxelUpdates = voxelUpdates,
                SpecialPoints = new Dictionary<string, Vector3>
                {
                    { "start", walkPath[0] },
                    { "end", walkPath[walkPath.Count - 1] }
                },
                ConnectionPoints = connectionPoints
            };
        }

        /// <summary>
        /// 経路の両端に接続点を生成
        /// </summary>
        private List<ConnectionPoint> GeneratePathEndConnectionPoints(
            Vector3 startPos,
            Vector3 endPos,
            float radius,
            int totalCount)
        {
            var points = new List<ConnectionPoint>();
            if (totalCount <= 0) return points;

            int halfCount = Mathf.Max(1, totalCount / 2);

            // 始点の接続点
            Vector3 startDirection = (walkPath[1] - walkPath[0]).normalized;
            for (int i = 0; i < halfCount; i++)
            {
                Vector3 offset = Quaternion.Euler(0, i * (360f / halfCount), 0) * Vector3.right * radius * 0.5f;
                points.Add(new ConnectionPoint(
                    $"{id}_start_{i}",
                    startPos + offset,
                    -startDirection, // 始点は外向き
                    radius * 0.5f
                ));
            }

            // 終点の接続点
            Vector3 endDirection = (walkPath[walkPath.Count - 1] - walkPath[walkPath.Count - 2]).normalized;
            int endCount = totalCount - halfCount;
            for (int i = 0; i < endCount; i++)
            {
                Vector3 offset = Quaternion.Euler(0, i * (360f / endCount), 0) * Vector3.right * radius * 0.5f;
                points.Add(new ConnectionPoint(
                    $"{id}_end_{i}",
                    endPos + offset,
                    endDirection, // 終点は進行方向
                    radius * 0.5f
                ));
            }

            return points;
        }

        public override bool CanConnectTo(IStructure target)
        {
            // 全ての構造物と接続可能
            return true;
        }

        public override Bounds GetBounds()
        {
            return bounds;
        }
    }
}
