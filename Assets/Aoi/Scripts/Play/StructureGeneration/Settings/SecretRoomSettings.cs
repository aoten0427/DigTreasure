using UnityEngine;

namespace StructureGeneration
{
    /// <summary>
    /// 秘密の部屋の生成設定
    /// </summary>
    [CreateAssetMenu(fileName = "SecretRoomSettings", menuName = "StructureGeneration/SecretRoomSettings")]
    public class SecretRoomSettings : StructureSettings
    {
        [Header("部屋のサイズ")]
        [Tooltip("部屋の半径の最小値（メートル）")]
        public float m_minRadius = 4f;

        [Tooltip("部屋の半径の最大値（メートル）")]
        public float m_maxRadius = 8f;

        [Header("壁の設定")]
        [Tooltip("壁の厚さの最小値（メートル）")]
        public float m_minWallThickness = 2f;

        [Tooltip("壁の厚さの最大値（メートル）")]
        public float m_maxWallThickness = 3f;

        [Tooltip("壁に使用するボクセルID")]
        public byte m_wallVoxelId = 4;

        [Header("内部空間")]
        [Tooltip("内部空間のボクセルID（空気）")]
        public byte m_innerVoxelId = 0;

        [Header("ランダム配置設定")]
        [Tooltip("配置可能なY座標の最小値（メートル）")]
        public float m_minY = -30f;

        [Tooltip("配置可能なY座標の最大値（メートル）")]
        public float m_maxY = -10f;

        [Tooltip("他の構造物との最小距離（メートル）")]
        public float m_minDistanceFromOthers = 20f;

        [Tooltip("配置試行回数")]
        public int m_placementAttempts = 50;

        public override StructureType GetStructureType()
        {
            return StructureType.SecretRoom;
        }
    }
}
