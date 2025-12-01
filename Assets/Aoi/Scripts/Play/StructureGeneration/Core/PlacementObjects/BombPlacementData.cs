using UnityEngine;

namespace StructureGeneration
{
    /// <summary>
    /// 爆弾配置情報
    /// </summary>
    public class BombPlacementData : PlacementObjectData
    {
        public override PlacementObjectType ObjectType => PlacementObjectType.Bomb;

        public BombPlacementData(
            Vector3 position,
            Vector3 direction,
            string relatedStructureId,
            string relatedConnectionId = null)
            : base(position, direction, relatedStructureId, relatedConnectionId)
        {
        }
    }
}
