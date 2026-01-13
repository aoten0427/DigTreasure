using UnityEngine;

namespace StructureGeneration
{
    /// <summary>
    /// 宝箱配置情報
    /// </summary>
    public class TreasurePlacementData : PlacementObjectData
    {
        public override PlacementObjectType ObjectType => PlacementObjectType.Treasure;

        public TreasurePlacementData(
            Vector3 position,
            Vector3 direction,
            string relatedStructureId,
            string relatedConnectionId = null)
            : base(position, direction, relatedStructureId, relatedConnectionId)
        {
        }
    }
}
