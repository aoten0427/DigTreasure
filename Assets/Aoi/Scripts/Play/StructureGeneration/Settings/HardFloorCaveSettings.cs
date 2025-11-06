using UnityEngine;

namespace StructureGeneration
{
    /// <summary>
    /// 硬い床を持つ大きな洞窟の生成設定
    /// </summary>
    [CreateAssetMenu(fileName = "HardFloorCaveSettings", menuName = "StructureGeneration/HardFloorCaveSettings")]
    public class HardFloorCaveSettings : StructureSettings
    {
        [Header("洞窟のサイズ")]
        [Tooltip("洞窟の水平半径の最小値（メートル、x-z方向）")]
        public float minHorizontalRadius = 15f;

        [Tooltip("洞窟の水平半径の最大値（メートル、x-z方向）")]
        public float maxHorizontalRadius = 25f;

        [Tooltip("洞窟の垂直半径の最小値（メートル、y方向）")]
        public float minVerticalRadius = 8f;

        [Tooltip("洞窟の垂直半径の最大値（メートル、y方向）")]
        public float maxVerticalRadius = 15f;

        [Header("床の設定")]
        [Tooltip("床の厚さ（メートル）")]
        public float floorThickness = 2f;

        [Tooltip("床に使用するボクセルID（硬い岩）")]
        public byte floorVoxelId = 3;

        [Header("内部空間")]
        [Tooltip("内部空間のボクセルID（空気）")]
        public byte innerVoxelId = 0;

        [Header("形状")]
        [Tooltip("洞窟の形状のランダム性（0-1、高いほど不規則）")]
        [Range(0f, 1f)]
        public float shapeRandomness = 0.3f;

        [Header("生成位置")]
        [Tooltip("洞窟の中心位置")]
        public Vector3 centerPosition = new Vector3(0, -20, 0);

        public override StructureType GetStructureType()
        {
            return StructureType.HardFloorCave;
        }

        public override IStructure CreateStructure(string id, int seed)
        {
            return new HardFloorCave(id, seed, this);
        }
    }
}
