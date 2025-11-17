using UnityEngine;

namespace StructureGeneration
{
    /// <summary>
    /// トンネル生成用のノイズ計算ヘルパー
    /// OpenTunnelGeneratorとFilledTunnelGeneratorで共有
    /// </summary>
    public static class TunnelNoiseHelper
    {
        // ノイズ周波数の定数
        private const float NOISE_FREQUENCY_XZ = 0.1f;
        private const float NOISE_FREQUENCY_Y = 0.1f;
        private const float NOISE_NORMALIZATION_OFFSET = 0.5f;
        private const float NOISE_NORMALIZATION_MULTIPLIER = 2f;

        /// <summary>
        /// トンネル用の3Dノイズを計算
        /// 2つのPerlinNoiseを掛け合わせて自然な変化を作る
        /// </summary>
        /// <param name="worldPos">ワールド座標</param>
        /// <param name="seed">シード値</param>
        /// <param name="noiseScale">ノイズのスケール（0.0～1.0、大きいほど変化が大きい）</param>
        /// <returns>-noiseScale～+noiseScaleの範囲のノイズ値</returns>
        public static float CalculateTunnelNoise(Vector3 worldPos, int seed, float noiseScale)
        {
            // XZ平面のノイズ
            float noiseXZ = Mathf.PerlinNoise(
                (worldPos.x + seed) * NOISE_FREQUENCY_XZ,
                (worldPos.z + seed) * NOISE_FREQUENCY_XZ
            );

            // Y方向のノイズ（異なるオフセットで独立性を確保）
            float noiseY = Mathf.PerlinNoise(
                (worldPos.y + seed * 2) * NOISE_FREQUENCY_Y,
                (worldPos.x + seed * 3) * NOISE_FREQUENCY_Y
            );

            // 2つのノイズを掛け合わせる
            float combinedNoise = noiseXZ * noiseY;

            // 0～1の範囲を-1～1に正規化
            float normalizedNoise = (combinedNoise - NOISE_NORMALIZATION_OFFSET) * NOISE_NORMALIZATION_MULTIPLIER;

            // スケールを適用
            return normalizedNoise * noiseScale;
        }
    }
}
