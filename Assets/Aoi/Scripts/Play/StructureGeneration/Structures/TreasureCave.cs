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
        // 定数
        private const float DEFAULT_WALL_NOISE_AMPLITUDE = 0.10f;
        private const float DEFAULT_FLOOR_NOISE_AMPLITUDE = 0.08f;
        private const int DEFAULT_NOISE_OCTAVES = 3;
        private const float DEFAULT_WAVES_PER_SIZE = 4f;
        private const float CONNECTION_POINT_Y_OFFSET = 2.0f;

        private readonly TreasureCaveSettings m_settings;
        private readonly Vector3 m_fixedPosition; 
        private readonly Vector3? m_targetPosition; 
        private float m_actualRadius;
        private float m_actualWallThickness;
        private Vector3 m_treasurePosition;
        private Bounds m_bounds;

        public override StructureType Type => StructureType.TreasureCave;
        public override int Priority => m_settings.priority;

        public TreasureCave(string id, int seed, TreasureCaveSettings settings, Vector3 fixedPosition, Vector3? targetPosition = null)
            : base(id, seed)
        {
            m_settings = settings;
            m_fixedPosition = fixedPosition;
            m_targetPosition = targetPosition;
        }

        public override async Task<StructureResult> GenerateAsync(int seed)
        {
            // シードを初期化
            Random.InitState(seed);

            // パラメータを決定
            m_actualRadius = Random.Range(m_settings.minRadius, m_settings.maxRadius);
            m_actualWallThickness = Random.Range(m_settings.minWallThickness, m_settings.maxWallThickness);

            centerPosition = m_fixedPosition;

            // バウンディングボックスを計算
            float totalRadius = m_actualRadius + m_actualWallThickness;
            m_bounds = new Bounds(
                centerPosition,
                new Vector3(totalRadius * 2f, totalRadius * 2f, totalRadius * 2f)
            );

            // 宝の位置を決定（中心から少しずらす）
            Vector3 treasureOffset = new Vector3(
                Random.Range(-m_settings.treasureOffsetRange, m_settings.treasureOffsetRange),
                Random.Range(-m_settings.treasureOffsetRange, m_settings.treasureOffsetRange),
                Random.Range(-m_settings.treasureOffsetRange, m_settings.treasureOffsetRange)
            );
            m_treasurePosition = centerPosition + treasureOffset;

            // ボクセルデータを生成（半球ドーム）
            var shapeParams = new ShapeParameters(
                centerPosition,
                m_actualRadius,
                m_actualRadius,  // 垂直半径も同じ（半球）
                m_actualWallThickness,
                m_actualWallThickness,  // 床の厚さ
                m_settings.wallVoxelId,
                m_settings.wallVoxelId, // 床も壁と同じ素材
                m_settings.innerVoxelId,
                seed,
                DEFAULT_WALL_NOISE_AMPLITUDE,
                DEFAULT_FLOOR_NOISE_AMPLITUDE,
                DEFAULT_NOISE_OCTAVES,
                DEFAULT_WAVES_PER_SIZE
            );

            var voxelUpdates = VoxelShapeGenerator.GenerateDome(shapeParams);

            // 接続点を生成
            // targetPositionがある場合は、その方向を優先的に向ける
            Vector3? targetDirection = null;
            if (m_targetPosition.HasValue)
            {
                targetDirection = m_targetPosition.Value - centerPosition;
            }

            connectionPoints = GenerateDomeConnectionPoints(
                centerPosition,
                m_actualRadius + m_actualWallThickness,
                m_settings.maxConnectionPoints,
                targetDirection,
                m_settings.connectionPointInset,
                CONNECTION_POINT_Y_OFFSET
            ) ;

            // 特殊ポイント（宝の位置）
            var specialPoints = new Dictionary<string, Vector3>
            {
                { "treasure", m_treasurePosition }
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
            return m_bounds;
        }
    }
}
