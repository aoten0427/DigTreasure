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

        [Header("ノイズ設定")]
        [Tooltip("壁・天井のノイズ振幅（特性サイズに対する割合、0.05-0.2推奨）")]
        [Range(0f, 0.3f)]
        public float wallNoiseAmplitude = 0.10f;

        [Tooltip("床の高さのノイズ振幅（特性サイズに対する割合、0.05-0.15推奨）")]
        [Range(0f, 0.3f)]
        public float floorNoiseAmplitude = 0.10f;

        [Tooltip("ノイズのオクターブ数（高いほど細かいディテール、1-4推奨）")]
        [Range(1, 5)]
        public int noiseOctaves = 3;

        [Tooltip("サイズあたりの波の数（3-6推奨）")]
        [Range(1f, 10f)]
        public float wavesPerSize = 4f;

        [Header("生成位置")]
        [Tooltip("洞窟の中心位置")]
        public Vector3 centerPosition = new Vector3(0, -20, 0);

        [Header("接続点設定")]
        [Range(0f, 50f)]
        public float connectionPointInset = 5f;

        [Header("内部装飾：ピラミッド")]
        [Tooltip("ピラミッドを生成する")]
        public bool generatePyramid = true;

        [Tooltip("生成するピラミッドの数")]
        [Range(1, 20)]
        public int pyramidCount = 3;

        [Tooltip("ピラミッドの高さの最小値（メートル）")]
        [Range(5f, 30f)]
        public float pyramidMinHeight = 10f;

        [Tooltip("ピラミッドの高さの最大値（メートル）")]
        [Range(5f, 30f)]
        public float pyramidMaxHeight = 20f;

        [Tooltip("ピラミッドの底面半径の最小値（メートル）")]
        [Range(0f, 15f)]
        public float pyramidMinBaseRadius = 3f;

        [Tooltip("ピラミッドの底面半径の最大値（メートル）")]
        [Range(0f, 15f)]
        public float pyramidMaxBaseRadius = 7f;

        [Tooltip("中心からの配置距離の最小値（メートル）")]
        [Range(0f, 20f)]
        public float pyramidMinDistanceFromCenter = 5f;

        [Tooltip("中心からの配置距離の最大値（メートル）")]
        [Range(0f, 100f)]
        public float pyramidMaxDistanceFromCenter = 15f;

        [Tooltip("ピラミッドのボクセルID")]
        public byte pyramidVoxelId = 3;

        public override StructureType GetStructureType()
        {
            return StructureType.HardFloorCave;
        }
    }
}
