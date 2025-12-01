using UnityEngine;

namespace StructureGeneration
{
    /// <summary>
    /// 設置オブジェクト配置情報の基底クラス
    /// プレハブ生成スクリプトがこの情報を参照してオブジェクトを配置する
    /// </summary>
    public abstract class PlacementObjectData
    {
        /// <summary>
        /// 配置位置（ワールド座標）
        /// </summary>
        public Vector3 Position { get; }

        /// <summary>
        /// 向き（通路方向など）
        /// </summary>
        public Vector3 Direction { get; }

        /// <summary>
        /// 関連する構造物のID
        /// </summary>
        public string RelatedStructureId { get; }

        /// <summary>
        /// 関連する接続のID（接続に関連する場合）
        /// </summary>
        public string RelatedConnectionId { get; }

        /// <summary>
        /// オブジェクトの種類
        /// </summary>
        public abstract PlacementObjectType ObjectType { get; }

        protected PlacementObjectData(
            Vector3 position,
            Vector3 direction,
            string relatedStructureId,
            string relatedConnectionId = null)
        {
            Position = position;
            Direction = direction;
            RelatedStructureId = relatedStructureId;
            RelatedConnectionId = relatedConnectionId;
        }
    }
}
