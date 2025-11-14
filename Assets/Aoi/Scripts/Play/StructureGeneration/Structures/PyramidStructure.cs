using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration
{
    /// <summary>
    /// 細長い三角形（ピラミッド）構造物
    /// 大洞窟の内部装飾として使用
    /// </summary>
    public class PyramidStructure
    {
        // ノイズ生成用定数
        private const float NOISE_AMPLITUDE_RATIO = 0.08f;
        private const float NOISE_FREQUENCY = 0.15f;

        private readonly string m_id;
        private readonly int m_seed;
        private Vector3 m_basePosition;
        private Bounds m_bounds;

        public string Id => m_id;
        public Vector3 BasePosition => m_basePosition;
        public Bounds GetBounds() => m_bounds;

        public PyramidStructure(string id, int seed)
        {
            m_id = id;
            m_seed = seed;
        }

        /// <summary>
        /// ピラミッドを生成
        /// </summary>
        /// <param name="basePos">底面の中心位置</param>
        /// <param name="height">高さ（メートル）</param>
        /// <param name="baseRadius">底面の半径（メートル）</param>
        /// <param name="voxelId">ボクセルID</param>
        public async Task<List<VoxelUpdate>> GenerateAsync(
            Vector3 basePos,
            float height,
            float baseRadius,
            byte voxelId)
        {
            m_basePosition = basePos;


            var voxels = VoxelShapeGenerator.GeneratePyramid(
                basePos,
                height,
                baseRadius,
                voxelId,
                m_seed,
                NOISE_AMPLITUDE_RATIO,
                NOISE_FREQUENCY
            );

            // バウンディングボックスを計算
            m_bounds = new Bounds(
                basePos + Vector3.up * (height / 2f),
                new Vector3(baseRadius * 2f, height, baseRadius * 2f)
            );

            await Task.Yield();
            return voxels;
        }
    }
}
