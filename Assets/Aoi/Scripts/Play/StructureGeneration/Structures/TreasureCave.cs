using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration
{
    /// <summary>
    /// お宝洞窟：硬い岩で囲まれた小さな部屋
    /// </summary>
    public class TreasureCave : BaseStructure
    {
        private readonly TreasureCaveSettings settings;
        private readonly Vector3? fixedPosition;  // 固定位置（nullの場合はランダム）
        private readonly Vector3? targetPosition; // 接続点の向き先（中央洞窟など）
        private float actualRadius;
        private float actualWallThickness;
        private Vector3 treasurePosition;
        private Bounds bounds;

        public override StructureType Type => StructureType.TreasureCave;
        public override int Priority => settings.priority;

        public TreasureCave(string id, int seed, TreasureCaveSettings settings, Vector3? fixedPosition = null, Vector3? targetPosition = null)
            : base(id, seed)
        {
            this.settings = settings;
            this.fixedPosition = fixedPosition;
            this.targetPosition = targetPosition;
        }

        public override async Task<StructureResult> GenerateAsync(int seed, Bounds fieldBounds)
        {
            // シードを初期化
            Random.InitState(seed);

            // パラメータを決定
            actualRadius = Random.Range(settings.minRadius, settings.maxRadius);
            actualWallThickness = Random.Range(settings.minWallThickness, settings.maxWallThickness);

            // 生成位置を決定（固定位置のみサポート）
            if (fixedPosition.HasValue)
            {
                centerPosition = fixedPosition.Value;
            }
            else
            {
                // 固定位置が指定されていない場合はエラー
                Debug.LogError($"TreasureCave {id}: 固定位置が指定されていません。放射状配置のみサポートしています。");
                centerPosition = Vector3.zero;
            }

            // バウンディングボックスを計算
            float totalRadius = actualRadius + actualWallThickness;
            bounds = new Bounds(
                centerPosition,
                new Vector3(totalRadius * 2f, totalRadius * 2f, totalRadius * 2f)
            );

            // 宝の位置を決定（中心から少しずらす）
            Vector3 treasureOffset = new Vector3(
                Random.Range(-settings.treasureOffsetRange, settings.treasureOffsetRange),
                Random.Range(-settings.treasureOffsetRange, settings.treasureOffsetRange),
                Random.Range(-settings.treasureOffsetRange, settings.treasureOffsetRange)
            );
            treasurePosition = centerPosition + treasureOffset;

            // ボクセルデータを生成（半球ドーム）
            var shapeParams = new ShapeParameters(
                centerPosition,
                actualRadius,
                actualRadius,  // 垂直半径も同じ（半球）
                actualWallThickness,
                actualWallThickness,  // 床の厚さ
                settings.wallVoxelId,
                settings.wallVoxelId, // 床も壁と同じ素材
                settings.innerVoxelId,
                seed,
                0.10f,  // 壁のノイズ振幅
                0.08f,  // 床のノイズ振幅
                3,      // オクターブ数
                4f      // 波の数
            );

            var voxelUpdates = VoxelShapeGenerator.GenerateDome(shapeParams);

            // 接続点を生成（ドーム型用：水平な側面のみ）
            // targetPositionがある場合は、その方向を優先的に向ける
            Vector3? targetDirection = null;
            if (targetPosition.HasValue)
            {
                targetDirection = targetPosition.Value - centerPosition;
            }

            connectionPoints = GenerateDomeConnectionPoints(
                centerPosition,
                actualRadius + actualWallThickness,
                settings.maxConnectionPoints,
                targetDirection,
                settings.connectionPointInset,
                2.0f
            ) ;

            // 特殊ポイント（宝の位置）
            var specialPoints = new Dictionary<string, Vector3>
            {
                { "treasure", treasurePosition }
            };

            // フレーム分散のため少し待機
            await Task.Yield();

            return new StructureResult
            {
                VoxelUpdates = voxelUpdates,
                SpecialPoints = specialPoints,
                ConnectionPoints = connectionPoints
            };
        }

        public override bool CanConnectTo(IStructure target)
        {
            // お宝洞窟同士は接続しない
            if (target.Type == StructureType.TreasureCave)
                return false;

            // 他の構造物とは接続可能
            return true;
        }

        public override Bounds GetBounds()
        {
            return bounds;
        }
    }
}
