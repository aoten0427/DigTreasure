using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration
{
    /// <summary>
    /// 硬い床の大きな洞窟：ドーム型の空間と硬い床を持つ
    /// </summary>
    public class HardFloorCave : BaseStructure
    {
        private const float DEFAULT_WALL_THICKNESS = 2f;

        private readonly HardFloorCaveSettings settings;
        private float actualHorizontalRadius;
        private float actualVerticalRadius;
        private Bounds bounds;

        public override StructureType Type => StructureType.HardFloorCave;
        public override int Priority => settings.priority;

        public HardFloorCave(string id, int seed, HardFloorCaveSettings settings)
            : base(id, seed)
        {
            this.settings = settings;
        }

        public override async Task<StructureResult> GenerateAsync(int seed, Bounds fieldBounds)
        {
            // シードを初期化
            Random.InitState(seed);

            // パラメータを決定
            actualHorizontalRadius = Random.Range(settings.minHorizontalRadius, settings.maxHorizontalRadius);
            actualVerticalRadius = Random.Range(settings.minVerticalRadius, settings.maxVerticalRadius);

            // 生成位置を設定から取得
            centerPosition = settings.centerPosition;

            // バウンディングボックスを計算
            bounds = new Bounds(
                centerPosition,
                new Vector3(actualHorizontalRadius * 2f, actualVerticalRadius * 2f, actualHorizontalRadius * 2f)
            );

            // 扁球ドームパラメータを作成
            var shapeParams = new ShapeParameters(
                centerPosition,
                actualHorizontalRadius,
                actualVerticalRadius,
                DEFAULT_WALL_THICKNESS,
                settings.floorThickness,
                1, // wallVoxelId
                settings.floorVoxelId,
                settings.innerVoxelId,
                seed,
                settings.wallNoiseAmplitude,
                settings.floorNoiseAmplitude,
                settings.noiseOctaves,
                settings.wavesPerSize
            );

            // ボクセルデータを生成（扁球ドーム型）
            var voxelUpdates = VoxelShapeGenerator.GenerateEllipsoidDome(shapeParams);

            // 接続点を生成（ドーム型用：水平な側面のみ）
            connectionPoints = GenerateDomeConnectionPoints(
                centerPosition,
                actualHorizontalRadius,
                settings.maxConnectionPoints,
                null,  // targetDirection: 均等分散
                settings.connectionPointInset  // 設定から取得
            );

            // 内部装飾（ピラミッド）を生成
            if (settings.generatePyramid)
            {
                var pyramidVoxels = await GeneratePyramidDecorationAsync(seed);
                voxelUpdates.AddRange(pyramidVoxels);
            }

            // フレーム分散のため待機
            await Task.Yield();

            return new StructureResult
            {
                VoxelUpdates = voxelUpdates,
                SpecialPoints = new Dictionary<string, Vector3>(),
                ConnectionPoints = connectionPoints
            };
        }

        /// <summary>
        /// ピラミッド装飾を生成（複数個）
        /// </summary>
        private async Task<List<VoxelUpdate>> GeneratePyramidDecorationAsync(int seed)
        {
            var allPyramidVoxels = new List<VoxelUpdate>();
            Random.InitState(seed);

            for (int i = 0; i < settings.pyramidCount; i++)
            {
                // ランダムな位置を計算（中心からの距離と角度）
                float distance = Random.Range(settings.pyramidMinDistanceFromCenter, settings.pyramidMaxDistanceFromCenter);
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance
                );

                Vector3 pyramidBase = centerPosition + offset;

                // ランダムなサイズ
                float height = Random.Range(settings.pyramidMinHeight, settings.pyramidMaxHeight);
                float baseRadius = Random.Range(settings.pyramidMinBaseRadius, settings.pyramidMaxBaseRadius);

                // ピラミッドを生成
                var pyramid = new PyramidStructure($"{id}_pyramid_{i}", seed + i);
                var pyramidVoxels = await pyramid.GenerateAsync(
                    pyramidBase,
                    height,
                    baseRadius,
                    settings.pyramidVoxelId
                );

                allPyramidVoxels.AddRange(pyramidVoxels);

                // フレーム分散
                if (i % 2 == 0)
                {
                    await Task.Yield();
                }
            }

            Debug.Log($"ピラミッド装飾生成完了: {settings.pyramidCount}個、合計{allPyramidVoxels.Count}ボクセル");

            return allPyramidVoxels;
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
