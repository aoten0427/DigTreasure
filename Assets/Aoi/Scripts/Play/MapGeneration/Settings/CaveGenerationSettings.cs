using UnityEngine;

namespace MapGeneration
{
    [CreateAssetMenu(fileName = "CaveGenerationSettings", menuName = "Settings/Cave Generation")]
    public class CaveGenerationSettings : ScriptableObject
    {
        [Header("Cave Count")]
        [Tooltip("生成する洞窟の最小数")]
        public int minCaveCount = 5;
        [Tooltip("生成する洞窟の最大数")]
        public int maxCaveCount = 15;

        [Header("Cave Distribution")]
        [Tooltip("洞窟同士の最小距離（メートル）- Poisson Disk Samplingで使用")]
        public float minCaveDistance = 20f;

        [Header("Depth Range")]
        [Tooltip("洞窟が生成される最小深度")]
        public float minDepth = 0f;
        [Tooltip("洞窟が生成される最大深度")]
        public float maxDepth = -100f;

        [Header("Cave Size")]
        [Tooltip("浅い位置での洞窟の最小半径")]
        public float minCaveRadius = 3f;
        [Tooltip("深い位置での洞窟の最大半径")]
        public float maxCaveRadius = 12f;

        [Header("Random Walk")]
        [Tooltip("浅い位置でのランダムウォーク最小歩数")]
        public int minWalkSteps = 20;
        [Tooltip("深い位置でのランダムウォーク最大歩数")]
        public int maxWalkSteps = 100;
        [Tooltip("方向変化の最小角度")]
        public float minDirectionChange = 30f;
        [Tooltip("方向変化の最大角度")]
        public float maxDirectionChange = 90f;

        [Header("Horizontal Cave Shape")]
        [Tooltip("水平方向の拡張倍率（1.0 = 球形、2.0 = 横に2倍広い）")]
        public float horizontalScaleMultiplier = 2.5f;
        [Tooltip("Y軸方向の移動制限（0.0 = 完全水平、1.0 = 制限なし）")]
        [Range(0f, 1f)]
        public float verticalMovementFactor = 0.3f;
        [Tooltip("洞窟の進行方向を水平に補正する強さ（0.0 = なし、1.0 = 完全水平）")]
        [Range(0f, 1f)]
        public float horizontalBias = 0.7f;

        [Header("Perlin Noise")]
        [Tooltip("パーリンノイズを使用するか")]
        public bool useNoise = true;
        [Tooltip("ノイズのスケール")]
        public float noiseScale = 0.1f;
        [Tooltip("削除する閾値（0.0~1.0）")]
        [Range(0f, 1f)]
        public float noiseThreshold = 0.5f;

        [Header("Voxel Layer Settings")]
        [Tooltip("深さに応じたボクセルレイヤー設定（深い方から順に評価）")]
        public VoxelLayer[] voxelLayers = new VoxelLayer[]
        {
            new VoxelLayer(float.MinValue, -50f, 4, "Bedrock"),
            new VoxelLayer(-50f, -20f, 3, "Deep Stone"),
            new VoxelLayer(-20f, 0f, 2, "Medium Stone"),
            new VoxelLayer(0f, float.MaxValue, 1, "Surface Stone")
        };

        [Header("Layer Transition Settings")]
        [Tooltip("レイヤー境界の遷移範囲（この範囲内で混在する）")]
        public float layerTransitionHeight = 2f;

        [Tooltip("レイヤー遷移用ノイズスケール")]
        public float layerTransitionNoiseScale = 0.15f;

        [Header("Boundary Settings")]
        [Tooltip("範囲外エリアの厚さ（チャンク単位）")]
        public int boundaryThickness = 1;

        [Tooltip("範囲外用ボクセルID（破壊不可の岩壁）")]
        public int boundaryVoxelId = 5;

        [Tooltip("地上境界の基準高さ（この高さ以上は崖、以下は壁）")]
        public float surfaceLevel = 0f;

        [Header("Cliff Settings")]
        [Tooltip("崖の高さ（チャンク単位）")]
        public int cliffHeightInChunks = 3;
    }
}
