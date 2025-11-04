using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TreasureList", menuName = "Scriptable Objects/Treasure/TreasureList")]
public class TreasureList : ScriptableObject
{
    public List<TreasureSO> allTreasure = new();
    public int numTreasure => allTreasure.Count;
}
