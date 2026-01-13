using StructureGeneration;
using UnityEngine;

[CreateAssetMenu(fileName = "TreasureCaveSettings", menuName = "StructureGeneration/TreasureBoxCaveSettings")]
public class TreasureBoxCaveSetting : StructureSettings
{
    public override StructureType GetStructureType()
    {
        return StructureType.TreasureBox;
    }

}
