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
        private const byte DEFAULT_WALL_VOXEL_ID = 1;
        private const int PYRAMID_FRAME_YIELD_INTERVAL = 2;

        private readonly HardFloorCaveSettings m_settings;
        private float m_actualHorizontalRadius;
        private float m_actualVerticalRadius;
        private Bounds m_bounds;

        public override StructureType Type => StructureType.HardFloorCave;
        public override int Priority => m_settings.priority;

        public HardFloorCave(string id, int seed, HardFloorCaveSettings settings)
            : base(id, seed)
        {
            m_settings = settings;
        }

        public override async Task<StructureResult> GenerateAsync(int seed)
        {
            // シードを初期化
            Random.InitState(seed);

            // パラメータを決定
            m_actualHorizontalRadius = Random.Range(m_settings.minHorizontalRadius, m_settings.maxHorizontalRadius);
            m_actualVerticalRadius = Random.Range(m_settings.minVerticalRadius, m_settings.maxVerticalRadius);

            // 生成位置を設定から取得
            centerPosition = m_settings.centerPosition;

            // バウンディングボックスを計算
            m_bounds = new Bounds(
                centerPosition,
                new Vector3(m_actualHorizontalRadius * 2f, m_actualVerticalRadius * 2f, m_actualHorizontalRadius * 2f)
            );

            // 扁球ドームパラメータを作成
            var shapeParams = new ShapeParameters(
                centerPosition,
                m_actualHorizontalRadius,
                m_actualVerticalRadius,
                DEFAULT_WALL_THICKNESS,
                m_settings.floorThickness,
                DEFAULT_WALL_VOXEL_ID,
                m_settings.floorVoxelId,
                m_settings.innerVoxelId,
                seed,
                m_settings.wallNoiseAmplitude,
                m_settings.floorNoiseAmplitude,
                m_settings.noiseOctaves,
                m_settings.wavesPerSize
            );

            // ボクセルデータを生成（扁球ドーム型）
            var voxelUpdates = VoxelShapeGenerator.GenerateEllipsoidDome(shapeParams);

            // 接続点を生成（ドーム型用：水平な側面のみ）
            connectionPoints = GenerateDomeConnectionPoints(
                centerPosition,
                m_actualHorizontalRadius,
                m_settings.maxConnectionPoints,
                null,  // targetDirection: 均等分散
                m_settings.connectionPointInset  // 設定から取得
            );

            // 内部装飾（ピラミッド）を生成
            if (m_settings.generatePyramid)
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

            for (int i = 0; i < m_settings.pyramidCount; i++)
            {
                // ランダムな位置を計算（中心からの距離と角度）
                float distance = Random.Range(m_settings.pyramidMinDistanceFromCenter, m_settings.pyramidMaxDistanceFromCenter);
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0f,
                    Mathf.Sin(angle) * distance
                );

                Vector3 pyramidBase = centerPosition + offset;

                // ランダムなサイズ
                float height = Random.Range(m_settings.pyramidMinHeight, m_settings.pyramidMaxHeight);
                float baseRadius = Random.Range(m_settings.pyramidMinBaseRadius, m_settings.pyramidMaxBaseRadius);

                // ピラミッドを生成
                var pyramid = new PyramidStructure($"{id}_pyramid_{i}", seed + i);
                var pyramidVoxels = await pyramid.GenerateAsync(
                    pyramidBase,
                    height,
                    baseRadius,
                    m_settings.pyramidVoxelId
                );

                allPyramidVoxels.AddRange(pyramidVoxels);

                // フレーム分散
                if (i % PYRAMID_FRAME_YIELD_INTERVAL == 0)
                {
                    await Task.Yield();
                }
            }

            return allPyramidVoxels;
        }

        public override bool CanConnectTo(IStructure target)
        {
            // 全ての構造物と接続可能
            return true;
        }

        public override Bounds GetBounds()
        {
            return m_bounds;
        }
    }
}
