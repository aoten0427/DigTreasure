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
        /// m_id
        string Id { get; }

        ///構造物の種類
        StructureType Type { get; }

        ///生成優先度（高いほど後に上書き
        int Priority { get; }

        /// <中心位置
        Vector3 CenterPosition { get; }

        /// <summary>
        /// ボクセルデータを非同期生成
        /// seedとフィールド範囲を受け取り、位置も含めて全て自分で決定する
        /// </summary>
        /// <param name="seed">シード値</param>
        /// <param name="fieldBounds">フィールドの範囲</param>
        Task<StructureResult> GenerateAsync(int seed);

        /// <summary>
        /// 指定された構造物と接続可能かチェック
        /// </summary>
        bool CanConnectTo(IStructure target);

        /// <summary>
        /// 指定された位置に最も近い未使用の接続点を取得
        /// </summary>
        ConnectionPoint GetClosestConnectionPoint(Vector3 targetPosition);

        /// <summary>
        /// すべての接続点を取得
        /// </summary>
        List<ConnectionPoint> GetConnectionPoints();

        /// <summary>
        /// 構造物の占有範囲
        /// </summary>
        Bounds GetBounds();
    }
}
