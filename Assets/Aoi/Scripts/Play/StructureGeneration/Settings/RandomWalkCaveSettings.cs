using UnityEngine;

namespace StructureGeneration
{
    /// <summary>
    /// ランダムウォーク洞窟の生成設定
    /// </summary>
    [CreateAssetMenu(fileName = "RandomWalkCaveSettings", menuName = "StructureGeneration/RandomWalkCaveSettings")]
    public class RandomWalkCaveSettings : StructureSettings
    {
        [Header("ランダムウォーク設定")]
        [Tooltip("ウォークのステップ数の最小値")]
        public int minWalkSteps = 20;

        [Tooltip("ウォークのステップ数の最大値")]
        public int maxWalkSteps = 40;

        [Tooltip("各ステップの距離（メートル）")]
        public float stepDistance = 2f;

        [Header("洞窟のサイズ")]
        [Tooltip("トンネルの半径（メートル、お宝部屋より少し大きめ）")]
        public float tunnelRadius = 7f;

        [Header("方向変更")]
        [Tooltip("方向を変更する確率（0-1、高いほど曲がりくねる）")]
        [Range(0f, 1f)]
        public float directionChangeChance = 0.4f;

        [Tooltip("方向変更時の最大角度（度）")]
        public float maxDirectionAngle = 30f;

        [Header("内部空間")]
        [Tooltip("削除するボクセルID（空気）")]
        public byte innerVoxelId = 0;

        [Header("生成位置")]
        [Tooltip("開始Y座標の最小値")]
        public float minStartYPosition = -20f;

        [Tooltip("開始Y座標の最大値")]
        public float maxStartYPosition = -10f;

        public override StructureType GetStructureType()
        {
            return StructureType.RandomWalkCave;
        }

        public override IStructure CreateStructure(string id, int seed)
        {
            return new RandomWalkCave(id, seed, this);
        }
    }
}
