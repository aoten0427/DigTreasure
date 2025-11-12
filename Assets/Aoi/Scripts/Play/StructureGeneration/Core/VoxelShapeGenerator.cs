using System.Collections.Generic;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration
{
    /// <summary>
    /// 形状タイプ
    /// </summary>
    public enum ShapeType
    {
        Dome,           // 半球ドーム
        EllipsoidDome,  // 扁球ドーム
        Sphere,         // 完全な球
        Cylinder        // 円柱
    }

    /// <summary>
    /// 形状生成パラメータ
    /// </summary>
    public struct ShapeParameters
    {
        public Vector3 center;
        public float horizontalRadius;    // 水平方向の半径（x, z）
        public float verticalRadius;      // 垂直方向の半径（y）
        public float wallThickness;
        public float floorThickness;
        public byte wallVoxelId;
        public byte floorVoxelId;
        public byte innerVoxelId;
        public int noiseSeed;
        public float wallNoiseAmplitude;  // 壁のノイズ振幅（特性サイズに対する割合）
        public float floorNoiseAmplitude; // 床のノイズ振幅（特性サイズに対する割合）
        public int noiseOctaves;          // ノイズのオクターブ数
        public float wavesPerSize;        // サイズあたりの波の数

        /// <summary>
        /// 扁球用のコンストラクタ
        /// </summary>
        public ShapeParameters(
            Vector3 center,
            float horizontalRadius,
            float verticalRadius,
            float wallThickness,
            float floorThickness,
            byte wallVoxelId,
            byte floorVoxelId,
            byte innerVoxelId,
            int noiseSeed,
            float wallNoiseAmplitude = 0.10f,
            float floorNoiseAmplitude = 0.10f,
            int noiseOctaves = 3,
            float wavesPerSize = 4f)
        {
            this.center = center;
            this.horizontalRadius = horizontalRadius;
            this.verticalRadius = verticalRadius;
            this.wallThickness = wallThickness;
            this.floorThickness = floorThickness;
            this.wallVoxelId = wallVoxelId;
            this.floorVoxelId = floorVoxelId;
            this.innerVoxelId = innerVoxelId;
            this.noiseSeed = noiseSeed;
            this.wallNoiseAmplitude = wallNoiseAmplitude;
            this.floorNoiseAmplitude = floorNoiseAmplitude;
            this.noiseOctaves = noiseOctaves;
            this.wavesPerSize = wavesPerSize;
        }
    }

    /// <summary>
    /// ボクセル形状ジェネレーター
    /// </summary>
    public static class VoxelShapeGenerator
    {
        /// <summary>
        /// 半球ドームを生成（床の上に半球形）
        /// </summary>
        public static List<VoxelUpdate> GenerateDome(ShapeParameters param)
        {
            var updates = new List<VoxelUpdate>();
            float voxelSize = VoxelConstants.VOXEL_SIZE;
            float outerRadius = param.horizontalRadius + param.wallThickness;

            int range = Mathf.CeilToInt(outerRadius / voxelSize);
            Vector3Int centerVoxel = new Vector3Int(
                Mathf.RoundToInt(param.center.x / voxelSize),
                Mathf.RoundToInt(param.center.y / voxelSize),
                Mathf.RoundToInt(param.center.z / voxelSize)
            );

            // 床の基準高さ（中心のY座標と同じ）
            float baseFloorY = param.center.y;

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    for (int z = -range; z <= range; z++)
                    {
                        Vector3Int voxelPos = centerVoxel + new Vector3Int(x, y, z);
                        Vector3 worldPos = new Vector3(
                            voxelPos.x * voxelSize,
                            voxelPos.y * voxelSize,
                            voxelPos.z * voxelSize
                        );

                        // 中心からの水平距離と垂直距離
                        Vector3 offset = worldPos - param.center;
                        float horizontalDist = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z);
                        float verticalDist = offset.y;

                        // 特性サイズ
                        float characteristicSize = param.horizontalRadius;

                        // 壁・天井用ノイズ
                        float noise3D = NoiseUtility.GetAdaptiveNoise(worldPos, characteristicSize, param.noiseSeed, param.noiseOctaves, param.wavesPerSize);

                        // 床の高さ用ノイズ
                        float floorNoise = NoiseUtility.GetAdaptiveNoise(worldPos, characteristicSize, param.noiseSeed + 1000, param.noiseOctaves, param.wavesPerSize * 0.5f);
                        float floorHeightAmplitude = characteristicSize * param.floorNoiseAmplitude;
                        float floorY = baseFloorY + floorNoise * floorHeightAmplitude;

                        // 半球のノイズ適用
                        float wallAmplitude = characteristicSize * param.wallNoiseAmplitude;
                        float hemisphereRadius = outerRadius + noise3D * wallAmplitude;

                        // 床より下の場合
                        if (worldPos.y < floorY)
                        {
                            // 床の範囲内かチェック
                            if (worldPos.y >= floorY - param.floorThickness && horizontalDist <= param.horizontalRadius)
                            {
                                updates.Add(new VoxelUpdate(worldPos, param.floorVoxelId));
                            }
                            continue;
                        }

                        // 半球の判定（床より上）
                        float distFromCenter = Mathf.Sqrt(
                            horizontalDist * horizontalDist +
                            verticalDist * verticalDist
                        );

                        // 外側の境界
                        if (distFromCenter <= hemisphereRadius && horizontalDist <= outerRadius)
                        {
                            // 内側の境界（ノイズ適用、振幅は外側の半分）
                            float innerRadius = param.horizontalRadius + noise3D * wallAmplitude * 0.5f;
                            float innerDist = Mathf.Sqrt(
                                horizontalDist * horizontalDist +
                                verticalDist * verticalDist
                            );

                            if (innerDist > innerRadius)
                            {
                                // 壁
                                updates.Add(new VoxelUpdate(worldPos, param.wallVoxelId));
                            }
                            else
                            {
                                // 内部空間
                                updates.Add(new VoxelUpdate(worldPos, param.innerVoxelId));
                            }
                        }
                    }
                }
            }

            return updates;
        }

        /// <summary>
        /// 扁球ドームを生成（床の上に扁平な半楕円体）
        /// </summary>
        public static List<VoxelUpdate> GenerateEllipsoidDome(ShapeParameters param)
        {
            var updates = new List<VoxelUpdate>();
            float voxelSize = VoxelConstants.VOXEL_SIZE;

            // 外側の半径（壁の厚さを含む）
            float outerHorizontalRadius = param.horizontalRadius + param.wallThickness;
            float outerVerticalRadius = param.verticalRadius + param.wallThickness;

            // 処理範囲を計算（水平方向の半径を基準）
            int range = Mathf.CeilToInt(outerHorizontalRadius / voxelSize);
            Vector3Int centerVoxel = new Vector3Int(
                Mathf.RoundToInt(param.center.x / voxelSize),
                Mathf.RoundToInt(param.center.y / voxelSize),
                Mathf.RoundToInt(param.center.z / voxelSize)
            );

            // 床の基準高さ（中心のY座標と同じ）
            float baseFloorY = param.center.y;

            for (int x = -range; x <= range; x++)
            {
                for (int y = -range; y <= range; y++)
                {
                    for (int z = -range; z <= range; z++)
                    {
                        Vector3Int voxelPos = centerVoxel + new Vector3Int(x, y, z);
                        Vector3 worldPos = new Vector3(
                            voxelPos.x * voxelSize,
                            voxelPos.y * voxelSize,
                            voxelPos.z * voxelSize
                        );

                        // 中心からのオフセット
                        Vector3 offset = worldPos - param.center;
                        float horizontalDist = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z);
                        float verticalDist = offset.y;

                        // 洞窟の特性サイズ（半径の平均）
                        float characteristicSize = (param.horizontalRadius + param.verticalRadius) / 2f;

                        // 壁・天井用の3Dノイズ
                        float noise3D = NoiseUtility.GetAdaptiveNoise(worldPos, characteristicSize, param.noiseSeed, param.noiseOctaves, param.wavesPerSize);

                        // 床の高さ用ノイズ（より低周波で穏やか）
                        float floorHeightNoise = NoiseUtility.GetAdaptiveNoise(worldPos, characteristicSize, param.noiseSeed + 1000, param.noiseOctaves, param.wavesPerSize * 0.5f);
                        float floorHeightAmplitude = characteristicSize * param.floorNoiseAmplitude;
                        float floorY = baseFloorY + floorHeightNoise * floorHeightAmplitude;

                        // 床の半径用ノイズ（さらに低周波）
                        float floorRadiusNoise = NoiseUtility.GetAdaptiveNoise(worldPos, characteristicSize, param.noiseSeed + 2000, param.noiseOctaves, param.wavesPerSize * 0.375f);
                        float noisedFloorRadius = param.horizontalRadius + floorRadiusNoise * floorHeightAmplitude * 2f;

                        // 床より下の場合
                        if (worldPos.y < floorY)
                        {
                            // 床の範囲内かチェック（ノイズを適用した半径を使用）
                            if (worldPos.y >= floorY - param.floorThickness && horizontalDist <= noisedFloorRadius)
                            {
                                updates.Add(new VoxelUpdate(worldPos, param.floorVoxelId));
                            }
                            continue;
                        }

                        // 扁球の判定（床より上）
                        float wallAmplitude = characteristicSize * param.wallNoiseAmplitude;

                        float noisedHorizontalRadius = outerHorizontalRadius + noise3D * wallAmplitude;
                        float noisedVerticalRadius = outerVerticalRadius + noise3D * wallAmplitude;

                        // 外側の楕円体判定
                        float outerEllipsoidDist =
                            (horizontalDist * horizontalDist) / (noisedHorizontalRadius * noisedHorizontalRadius) +
                            (verticalDist * verticalDist) / (noisedVerticalRadius * noisedVerticalRadius);

                        if (outerEllipsoidDist <= 1.0f)
                        {
                            // 内側の楕円体判定（ノイズ振幅は外側の半分）
                            float innerHorizontalRadius = param.horizontalRadius + noise3D * wallAmplitude * 0.5f;
                            float innerVerticalRadius = param.verticalRadius + noise3D * wallAmplitude * 0.5f;

                            float innerEllipsoidDist =
                                (horizontalDist * horizontalDist) / (innerHorizontalRadius * innerHorizontalRadius) +
                                (verticalDist * verticalDist) / (innerVerticalRadius * innerVerticalRadius);

                            if (innerEllipsoidDist > 1.0f)
                            {
                                // 壁
                                updates.Add(new VoxelUpdate(worldPos, param.wallVoxelId));
                            }
                            else
                            {
                                // 内部空間
                                updates.Add(new VoxelUpdate(worldPos, param.innerVoxelId));
                            }
                        }
                    }
                }
            }

            return updates;
        }

        /// <summary>
        /// 細長い三角形（ピラミッド）を生成
        /// </summary>
        /// <param name="basePosition">底面の中心位置</param>
        /// <param name="height">高さ（メートル）</param>
        /// <param name="baseRadius">底面の半径（メートル）</param>
        /// <param name="voxelId">ボクセルID</param>
        /// <param name="seed">ノイズシード</param>
        /// <param name="noiseAmplitudeRatio">ノイズ振幅の比率（半径に対する割合）</param>
        /// <param name="noiseFrequency">ノイズの周波数</param>
        public static List<VoxelUpdate> GeneratePyramid(
            Vector3 basePosition,
            float height,
            float baseRadius,
            byte voxelId,
            int seed,
            float noiseAmplitudeRatio = 0.08f,
            float noiseFrequency = 0.15f)
        {
            var updates = new List<VoxelUpdate>();
            float voxelSize = VoxelConstants.VOXEL_SIZE;

            // 処理範囲を計算
            int rangeX = Mathf.CeilToInt(baseRadius / voxelSize);
            int rangeY = Mathf.CeilToInt(height / voxelSize);
            int rangeZ = Mathf.CeilToInt(baseRadius / voxelSize);

            Vector3Int baseVoxel = new Vector3Int(
                Mathf.RoundToInt(basePosition.x / voxelSize),
                Mathf.RoundToInt(basePosition.y / voxelSize),
                Mathf.RoundToInt(basePosition.z / voxelSize)
            );

            // ピラミッドの頂点位置
            Vector3 apexPosition = basePosition + Vector3.up * height;

            for (int x = -rangeX; x <= rangeX; x++)
            {
                for (int y = 0; y <= rangeY; y++)
                {
                    for (int z = -rangeZ; z <= rangeZ; z++)
                    {
                        Vector3Int voxelPos = baseVoxel + new Vector3Int(x, y, z);
                        Vector3 worldPos = new Vector3(
                            voxelPos.x * voxelSize,
                            voxelPos.y * voxelSize,
                            voxelPos.z * voxelSize
                        );

                        // 高さに応じた半径を計算（線形補間）
                        float heightRatio = (worldPos.y - basePosition.y) / height;
                        if (heightRatio < 0f || heightRatio > 1f)
                            continue;

                        // 高さに応じて半径を縮小（底面 → 頂点で0に）
                        float currentRadius = baseRadius * (1f - heightRatio);

                        // 中心からの水平距離
                        float horizontalDist = Mathf.Sqrt(
                            Mathf.Pow(worldPos.x - basePosition.x, 2) +
                            Mathf.Pow(worldPos.z - basePosition.z, 2)
                        );

                        // ノイズを適用して表面を粗くする
                        float noiseAmplitude = baseRadius * noiseAmplitudeRatio;
                        float noise = NoiseUtility.GetSimple3DNoise(worldPos, seed, noiseFrequency);
                        float noisedRadius = currentRadius + noise * noiseAmplitude;

                        // ピラミッドの内部判定（ノイズ適用後）
                        if (horizontalDist <= noisedRadius)
                        {
                            updates.Add(new VoxelUpdate(worldPos, voxelId));
                        }
                    }
                }
            }

            return updates;
        }
    }
}
