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
        private float actualRadius;
        private float actualWallThickness;
        private Vector3 treasurePosition;
        private Bounds bounds;

        public override StructureType Type => StructureType.TreasureCave;
        public override int Priority => settings.priority;

        public TreasureCave(string id, int seed, TreasureCaveSettings settings)
            : base(id, seed)
        {
            this.settings = settings;
        }

        public override async Task<StructureResult> GenerateAsync(int seed, Bounds fieldBounds)
        {
            // シードを初期化
            Random.InitState(seed);

            // パラメータを決定
            actualRadius = Random.Range(settings.minRadius, settings.maxRadius);
            actualWallThickness = Random.Range(settings.minWallThickness, settings.maxWallThickness);

            // 生成位置を決定（フィールド範囲内のランダムな位置）
            float yPos = Random.Range(settings.minYPosition, settings.maxYPosition);
            float xPos = Random.Range(
                fieldBounds.min.x + actualRadius + actualWallThickness,
                fieldBounds.max.x - actualRadius - actualWallThickness
            );
            float zPos = Random.Range(
                fieldBounds.min.z + actualRadius + actualWallThickness,
                fieldBounds.max.z - actualRadius - actualWallThickness
            );

            centerPosition = new Vector3(xPos, yPos, zPos);

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

            // ボクセルデータを生成（自然なドーム型）
            var voxelUpdates = GenerateNaturalDomeVoxels(
                centerPosition,
                actualRadius,
                actualWallThickness,
                actualWallThickness, // 床の厚さも壁と同じ
                settings.wallVoxelId,
                settings.wallVoxelId, // 床も壁と同じ素材
                settings.innerVoxelId,
                seed,
                0.5f, // ノイズスケール
                0.1f   // 床のノイズスケール
            );

            // 接続点を生成（ドーム型用：水平な側面のみ）
            connectionPoints = GenerateDomeConnectionPoints(
                centerPosition,
                actualRadius + actualWallThickness,
                settings.maxConnectionPoints
            );

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
