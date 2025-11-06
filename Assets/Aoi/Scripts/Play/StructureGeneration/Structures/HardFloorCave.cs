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
                2f, // 壁の厚さ（薄め）
                settings.floorThickness,
                1, // 壁
                settings.floorVoxelId, // 床
                settings.innerVoxelId,
                seed,
                settings.shapeRandomness,
                0.05f // 床のノイズスケール
            );

            // ボクセルデータを生成（扁球ドーム型）
            var voxelUpdates = VoxelShapeGenerator.GenerateEllipsoidDome(shapeParams);

            // 接続点を生成（ドーム型用：水平な側面のみ）
            connectionPoints = GenerateDomeConnectionPoints(
                centerPosition,
                actualHorizontalRadius,
                settings.maxConnectionPoints
            );

            // フレーム分散のため待機
            await Task.Yield();

            return new StructureResult
            {
                VoxelUpdates = voxelUpdates,
                SpecialPoints = new Dictionary<string, Vector3>(),
                ConnectionPoints = connectionPoints
            };
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
