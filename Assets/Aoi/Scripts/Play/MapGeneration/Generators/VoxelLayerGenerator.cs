using UnityEngine;
using MapGeneration;

namespace MapGeneration
{
    /// <summary>
    /// ボクセルレイヤー生成クラス
    /// 深さに応じてボクセルIDを決定し、レイヤー境界でノイズ遷移を適用
    /// </summary>
    public class VoxelLayerGenerator
    {
        private CaveGenerationSettings m_settings;
        private int m_seed;

        public VoxelLayerGenerator(CaveGenerationSettings settings, int seed)
        {
            m_settings = settings;
            m_seed = seed;
        }

        /// <summary>
        /// ワールド座標からボクセルIDを取得（深さベース + ノイズ遷移）
        /// </summary>
        public int GetVoxelIdByHeight(Vector3 worldPosition)
        {
            if (m_settings == null || m_settings.voxelLayers.Length == 0)
            {
                return 4; // デフォルト
            }

            float worldY = worldPosition.y;

            // レイヤーを深い方から順に評価
            for (int i = 0; i < m_settings.voxelLayers.Length; i++)
            {
                var layer = m_settings.voxelLayers[i];

                // このレイヤーの範囲内かチェック
                if (worldY >= layer.minHeight && worldY < layer.maxHeight)
                {
                    // レイヤーの中心部分（遷移範囲外）
                    if (worldY >= layer.minHeight + m_settings.layerTransitionHeight &&
                        worldY < layer.maxHeight - m_settings.layerTransitionHeight)
                    {
                        return layer.voxelId;
                    }

                    // 上境界付近（maxHeight側）の遷移範囲
                    if (worldY >= layer.maxHeight - m_settings.layerTransitionHeight)
                    {
                        // 上のレイヤーを取得
                        if (i < m_settings.voxelLayers.Length - 1)
                        {
                            var upperLayer = m_settings.voxelLayers[i + 1];
                            return GetTransitionVoxel(worldPosition, worldY, layer, upperLayer, layer.maxHeight);
                        }
                        return layer.voxelId;
                    }

                    // 下境界付近（minHeight側）の遷移範囲
                    if (worldY < layer.minHeight + m_settings.layerTransitionHeight)
                    {
                        // 下のレイヤーを取得
                        if (i > 0)
                        {
                            var lowerLayer = m_settings.voxelLayers[i - 1];
                            return GetTransitionVoxel(worldPosition, worldY, lowerLayer, layer, layer.minHeight);
                        }
                        return layer.voxelId;
                    }

                    return layer.voxelId;
                }
            }

            // どのレイヤーにも該当しない場合はデフォルト
            return m_settings.voxelLayers[m_settings.voxelLayers.Length - 1].voxelId;
        }

        /// <summary>
        /// レイヤー境界でノイズを使って遷移
        /// </summary>
        private int GetTransitionVoxel(Vector3 worldPos, float worldY, VoxelLayer lowerLayer, VoxelLayer upperLayer, float boundaryY)
        {
            // 3Dノイズを生成（XZ平面で一貫性を持たせる）
            float noise = Mathf.PerlinNoise(
                worldPos.x * m_settings.layerTransitionNoiseScale + m_seed,
                worldPos.z * m_settings.layerTransitionNoiseScale + m_seed
            );

            // ノイズで境界の高さを調整 (-transitionHeight ~ +transitionHeight)
            float noiseOffset = (noise - 0.5f) * m_settings.layerTransitionHeight * 2f;
            float adjustedBoundary = boundaryY + noiseOffset;

            // 調整後の境界より上なら上のレイヤー、下なら下のレイヤー
            return worldY >= adjustedBoundary ? upperLayer.voxelId : lowerLayer.voxelId;
        }
    }
}
