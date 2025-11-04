using System.Collections.Generic;

namespace VoxelWorld
{
    /// <summary>
    /// 分離検出結果
    /// </summary>
    public class SeparationResult
    {
        /// <summary>
        /// 分離されたグループリスト（各グループは座標のリスト）
        /// </summary>
        public List<List<UnityEngine.Vector3>> SeparatedGroups { get; set; } = new List<List<UnityEngine.Vector3>>();

        /// <summary>
        /// 大きすぎて分離対象外になったグループリスト
        /// </summary>
        public List<List<UnityEngine.Vector3>> OversizedGroups { get; set; } = new List<List<UnityEngine.Vector3>>();

        /// <summary>
        /// 処理時間（秒）
        /// </summary>
        public float ProcessingTime { get; set; }

        /// <summary>
        /// 処理されたボクセル総数
        /// </summary>
        public int TotalProcessedVoxels { get; set; }

        /// <summary>
        /// 検出されたグループ総数（有効+大きすぎるグループの合計）
        /// </summary>
        public int TotalGroupsFound { get; set; }

        /// <summary>
        /// 作成された有効なグループ数
        /// </summary>
        public int ValidGroupsCreated { get; set; }

        /// <summary>
        /// 大きすぎるグループ数
        /// </summary>
        public int OversizedGroupsCount { get; set; }

        /// <summary>
        /// 分離グループ数を取得（互換性のため）
        /// </summary>
        public int SeparatedGroupCount => SeparatedGroups?.Count ?? 0;

        /// <summary>
        /// 大きすぎるグループ数を取得（互換性のため）
        /// </summary>
        public int OversizedGroupCount => OversizedGroups?.Count ?? 0;

        /// <summary>
        /// 分離が検出されたかどうか
        /// </summary>
        public bool HasSeparation => SeparatedGroupCount > 0;
    }
}
