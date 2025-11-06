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

        [Header("生成位置")]
        [Tooltip("Y座標の最小値")]
        public float minYPosition = -30f;

        [Tooltip("Y座標の最大値")]
        public float maxYPosition = -15f;

        public override StructureType GetStructureType()
        {
            return StructureType.TreasureCave;
        }

        public override IStructure CreateStructure(string id, int seed)
        {
            return new TreasureCave(id, seed, this);
        }
    }
}
