using UnityEngine;

[CreateAssetMenu(fileName = "TreasureSO", menuName = "Scriptable Objects/Treasure/TreasureData")]
public class TreasureSO : ScriptableObject
{
    [SerializeField] float _lowestDepth;
    public float lowestDepth { get { return _lowestDepth; } }
    [SerializeField] float _highestDepth;
    public float highestDepth { get { return _highestDepth; } }
    [SerializeField] int _point;
    public int point { get { return _point; } }
    [SerializeField][Range(0, 100)] int _spawnPercent;
    public int spawnPercent { get { return _spawnPercent; } }
    [SerializeField] Mesh _treasureMesh;
    public Mesh treasureMesh {  get { return _treasureMesh; } }
}
