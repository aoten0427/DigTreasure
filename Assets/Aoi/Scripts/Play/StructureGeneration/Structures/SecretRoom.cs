using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration
{
    /// <summary>
    /// 秘密の部屋：他の構造物と接続しない孤立した空間
    /// </summary>
    public class SecretRoom : BaseStructure
    {
        // 定数
        private const float DEFAULT_WALL_NOISE_AMPLITUDE = 0.12f;
        private const float DEFAULT_FLOOR_NOISE_AMPLITUDE = 0.10f;
        private const int DEFAULT_NOISE_OCTAVES = 3;
        private const float DEFAULT_WAVES_PER_SIZE = 4f;

        private readonly SecretRoomSettings m_settings;
        private readonly Vector3 m_fixedPosition;
        private float m_actualRadius;
        private float m_actualWallThickness;
        private Bounds m_bounds;

        public override StructureType Type => StructureType.SecretRoom;
        public override int Priority => m_settings.priority;

        public SecretRoom(string id, int seed, SecretRoomSettings settings, Vector3 fixedPosition)
            : base(id, seed)
        {
            m_settings = settings;
            m_fixedPosition = fixedPosition;
        }

        /// <summary>
        /// 秘密の部屋を非同期生成
        /// </summary>
        public override async Task<StructureResult> GenerateAsync(int seed)
        {
            // シードを初期化
            Random.InitState(seed);

            // パラメータを決定
            m_actualRadius = Random.Range(m_settings.m_minRadius, m_settings.m_maxRadius);
            m_actualWallThickness = Random.Range(m_settings.m_minWallThickness, m_settings.m_maxWallThickness);

            centerPosition = m_fixedPosition;

            // バウンディングボックスを計算
            float totalRadius = m_actualRadius + m_actualWallThickness;
            m_bounds = new Bounds(
                centerPosition,
                new Vector3(totalRadius * 2f, totalRadius * 2f, totalRadius * 2f)
            );

            // ボクセルデータを生成（半球ドーム）
            var shapeParams = new ShapeParameters(
                centerPosition,
                m_actualRadius,
                m_actualRadius,
                m_actualWallThickness,
                m_actualWallThickness,
                m_settings.m_wallVoxelId,
                m_settings.m_wallVoxelId,
                m_settings.m_innerVoxelId,
                seed,
                DEFAULT_WALL_NOISE_AMPLITUDE,
                DEFAULT_FLOOR_NOISE_AMPLITUDE,
                DEFAULT_NOISE_OCTAVES,
                DEFAULT_WAVES_PER_SIZE
            );

            var voxelUpdates = VoxelShapeGenerator.GenerateDome(shapeParams);

            // 接続点は生成しない（孤立した構造物）
            connectionPoints = new List<ConnectionPoint>();

            // フレーム分散のため少し待機
            await Task.Yield();

            return new StructureResult
            {
                VoxelUpdates = voxelUpdates,
                SpecialPoints = new Dictionary<string, Vector3>(),
                ConnectionPoints = connectionPoints
            };
        }

        /// <summary>
        /// 接続可能かどうかを判定（秘密の部屋は接続しない）
        /// </summary>
        public override bool CanConnectTo(IStructure target)
        {
            // 秘密の部屋は他の構造物と接続しない
            return false;
        }

        /// <summary>
        /// バウンディングボックスを取得
        /// </summary>
        public override Bounds GetBounds()
        {
            return m_bounds;
        }
    }
}
