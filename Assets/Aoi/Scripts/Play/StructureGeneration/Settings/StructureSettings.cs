using UnityEngine;

namespace StructureGeneration
{
    /// <summary>
    /// 構造物設定の基底クラス
    /// </summary>
    public abstract class StructureSettings : ScriptableObject
    {
        [Header("生成数")]
        [Tooltip("生成する最小数")]
        public int minCount = 1;

        [Tooltip("生成する最大数")]
        public int maxCount = 3;

        [Header("優先度")]
        [Tooltip("生成優先度（高いほど後に生成され、他の構造物を上書きする）")]
        public int priority = 10;

        [Header("接続")]
        [Tooltip("この構造物が持つ最大接続点数")]
        public int maxConnectionPoints = 2;

        /// <summary>
        /// 構造物のタイプを取得
        /// </summary>
        public abstract StructureType GetStructureType();
    }
}
