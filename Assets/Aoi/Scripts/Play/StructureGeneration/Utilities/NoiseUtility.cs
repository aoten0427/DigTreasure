using UnityEngine;

namespace StructureGeneration
{
    /// <summary>
    /// ノイズ生成のユーティリティクラス
    /// </summary>
    public static class NoiseUtility
    {
        /// <summary>
        /// 洞窟サイズに適応したマルチオクターブノイズを生成
        /// </summary>
        /// <param name="position">ワールド座標</param>
        /// <param name="characteristicSize">洞窟の特性サイズ（半径の平均など）</param>
        /// <param name="seed">ノイズシード</param>
        /// <param name="octaves">オクターブ数（デフォルト3）</param>
        /// <param name="wavesPerSize">サイズあたりの波の数（デフォルト4）</param>
        /// <returns>-1~1の範囲の正規化されたノイズ値</returns>
        public static float GetAdaptiveNoise(Vector3 position, float characteristicSize, int seed, int octaves = 3, float wavesPerSize = 4f)
        {
            // 基本周波数：特性サイズに対して指定回数の波
            float baseFrequency = wavesPerSize / characteristicSize;

            float noise = 0f;
            float totalWeight = 0f;
            float frequency = baseFrequency;
            float amplitude = 1f;

            // マルチオクターブノイズ
            for (int octave = 0; octave < octaves; octave++)
            {
                // シードベースのオフセット（0-10の範囲）
                float seedOffsetX = ((seed + octave * 17) % 100) * 0.1f;
                float seedOffsetY = ((seed + octave * 31) % 100) * 0.1f;
                float seedOffsetZ = ((seed + octave * 47) % 100) * 0.1f;

                // 3D的なノイズ（3つの2Dノイズを組み合わせ）
                float noiseXY = Mathf.PerlinNoise(
                    position.x * frequency + seedOffsetX,
                    position.y * frequency + seedOffsetX
                );
                float noiseYZ = Mathf.PerlinNoise(
                    position.y * frequency + seedOffsetY,
                    position.z * frequency + seedOffsetY
                );
                float noiseXZ = Mathf.PerlinNoise(
                    position.x * frequency + seedOffsetZ,
                    position.z * frequency + seedOffsetZ
                );

                // 平均化
                float octaveNoise = (noiseXY + noiseYZ + noiseXZ) / 3f;

                // 累積
                noise += octaveNoise * amplitude;
                totalWeight += amplitude;

                // 次のオクターブ：周波数2倍、振幅半分
                frequency *= 2f;
                amplitude *= 0.5f;
            }

            // 正規化して-1~1の範囲に
            noise /= totalWeight;
            return (noise - 0.5f) * 2f;
        }

        /// <summary>
        /// シンプルな3Dノイズを生成（BaseStructureの旧実装互換）
        /// </summary>
        /// <param name="position">ワールド座標</param>
        /// <param name="seed">ノイズシード</param>
        /// <param name="frequency">周波数（デフォルト0.1f）</param>
        /// <returns>-1~1の範囲の正規化されたノイズ値</returns>
        public static float GetSimple3DNoise(Vector3 position, int seed, float frequency = 0.1f)
        {
            float noise3D = Mathf.PerlinNoise(
                (position.x + seed) * frequency,
                (position.y + seed) * frequency
            ) * Mathf.PerlinNoise(
                (position.z + seed * 2) * frequency,
                (position.x + seed * 3) * frequency
            );

            // ノイズを-1~1の範囲に正規化
            return (noise3D - 0.5f) * 2f;
        }

        /// <summary>
        /// 2Dノイズを生成（床の高さなどに使用）
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="z">Z座標</param>
        /// <param name="seed">ノイズシード</param>
        /// <param name="frequency">周波数（デフォルト0.2f）</param>
        /// <returns>-1~1の範囲の正規化されたノイズ値</returns>
        public static float Get2DNoise(float x, float z, int seed, float frequency = 0.2f)
        {
            float noise = Mathf.PerlinNoise(
                x * frequency + seed,
                z * frequency + seed
            );
            return (noise - 0.5f) * 2f;
        }
    }
}
