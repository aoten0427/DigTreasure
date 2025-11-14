using UnityEngine;

namespace StructureGeneration
{
    /// <summary>
    /// お宝洞窟の生成設定
    /// </summary>
    [CreateAssetMenu(fileName = "TreasureCaveSettings", menuName = "StructureGeneration/TreasureCaveSettings")]
    public class TreasureCaveSettings : StructureSettings
    {
        [Header("洞窟のサイズ")]
        [Tooltip("洞窟の半径の最小値（メートル）")]
        public float minRadius = 6f;

        [Tooltip("洞窟の半径の最大値（メートル）")]
        public float maxRadius = 10f;

        [Header("壁の設定")]
        [Tooltip("壁の厚さの最小値（メートル）")]
        public float minWallThickness = 2f;

        [Tooltip("壁の厚さの最大値（メートル）")]
        public float maxWallThickness = 3f;

        [Tooltip("壁に使用するボクセルID（とても硬い岩）")]
        public byte wallVoxelId = 4;

        [Header("内部空間")]
        [Tooltip("内部空間のボクセルID（空気）")]
        public byte innerVoxelId = 0;

        [Header("宝の配置")]
        [Tooltip("宝を中心からずらす最大距離（メートル）")]
        public float treasureOffsetRange = 2f;

        [Header("接続点設定")]
        [Range(0f, 50f)]
        public float connectionPointInset = 5f;

        [Header("放射状配置設定")]
        [Tooltip("中央洞窟からの基本距離（メートル）")]
        public float baseDistance = 60f;

        [Tooltip("距離のランダム変動幅（メートル）")]
        public float distanceVariation = 5f;

        [Tooltip("角度のランダム変動幅（度）")]
        public float angleVariation = 10f;

        [Tooltip("Y座標のランダム変動幅（メートル）")]
        public float yVariation = 5f;

        public override StructureType GetStructureType()
        {
            return StructureType.TreasureCave;
        }
    }
}
