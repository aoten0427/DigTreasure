using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace StructureGeneration
{
    /// <summary>
    /// 構造物の種類
    /// </summary>
    public enum StructureType
    {
        TreasureCave = 0,
        HardFloorCave = 1,
        RandomWalkCave = 2
    }

    /// <summary>
    /// 構造物の基底インターフェース
    /// </summary>
    public interface IStructure
    {
        /// <summary>一意なID</summary>
        string Id { get; }

        /// <summary>構造物の種類</summary>
        StructureType Type { get; }

        /// <summary>生成優先度（高いほど後に上書き）</summary>
        int Priority { get; }

        /// <summary>中心位置（ワールド座標）</summary>
        Vector3 CenterPosition { get; }

        /// <summary>
        /// ボクセルデータを非同期生成
        /// seedとフィールド範囲を受け取り、位置も含めて全て自分で決定する
        /// </summary>
        /// <param name="seed">シード値（個体ごとにユニーク）</param>
        /// <param name="fieldBounds">フィールドの範囲</param>
        Task<StructureResult> GenerateAsync(int seed, Bounds fieldBounds);

        /// <summary>
        /// 指定された構造物と接続可能かチェック
        /// </summary>
        bool CanConnectTo(IStructure target);

        /// <summary>
        /// 指定された位置に最も近い未使用の接続点を取得
        /// </summary>
        ConnectionPoint GetClosestConnectionPoint(Vector3 targetPosition);

        /// <summary>
        /// 構造物の占有範囲（バウンディングボックス）
        /// </summary>
        Bounds GetBounds();
    }
}
