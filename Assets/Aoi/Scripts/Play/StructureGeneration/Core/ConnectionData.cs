using UnityEngine;

namespace StructureGeneration
{

    /// <summary>
    /// 接続データ（2つの構造物間の接続情報）
    /// </summary>
    public class ConnectionData
    {
        public string Id { get; private set; }
        public string SourceStructureId { get; private set; }
        public string TargetStructureId { get; private set; }
        public ConnectionPoint SourcePoint { get; private set; }
        public ConnectionPoint TargetPoint { get; private set; }
        public ConnectionType Type { get; private set; }

        public ConnectionData(
            string id,
            string sourceStructureId,
            string targetStructureId,
            ConnectionPoint sourcePoint,
            ConnectionPoint targetPoint,
            ConnectionType type)
        {
            Id = id;
            SourceStructureId = sourceStructureId;
            TargetStructureId = targetStructureId;
            SourcePoint = sourcePoint;
            TargetPoint = targetPoint;
            Type = type;
        }

        /// <summary>
        /// 接続の長さを取得
        /// </summary>
        public float GetLength()
        {
            return Vector3.Distance(SourcePoint.Position, TargetPoint.Position);
        }
    }
}
