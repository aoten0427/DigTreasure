using UnityEngine;
using System.Collections.Generic;

public class TreasureSpawner : MonoBehaviour
{
    private TreasureList _treasureList;
    [SerializeField] float _minX;
    [SerializeField] float _maxX;
    [SerializeField] float _minZ;
    [SerializeField] float _maxZ;
    [SerializeField] Treasure _treasurePrefab;

    private void Awake()
    {
        _treasureList = Resources.Load<TreasureList>("Treasure/TreasureList");
    }
    //ƒeƒXƒg
    private void Update()
    {
        if(Input.GetKeyUp(KeyCode.T))
            SpawnRandomTreasure();
    }
    public void SpawnRandomTreasure()
    {
        List<TreasureSO> possibleTreasure = new();
        while (possibleTreasure.Count == 0)
        {
            foreach (TreasureSO treasure in _treasureList.allTreasure)
            {
                int randomChance = UnityEngine.Random.Range(0, 101);
                if (randomChance <= treasure.spawnPercent)
                    possibleTreasure.Add(treasure);
            }
        }
        TreasureSO treasureSO = possibleTreasure[Random.Range(0, possibleTreasure.Count)];
        Vector3 spawnPosition = new(Random.Range(_minX, _maxX),
            Random.Range(treasureSO.lowestDepth, treasureSO.highestDepth),
            Random.Range(_minZ, _maxZ));
        Treasure newTreasure = Instantiate(_treasurePrefab, spawnPosition, Quaternion.identity);
        newTreasure.SetScorePoint(treasureSO.point);
        newTreasure.transform.GetChild(0).GetComponent<MeshFilter>().mesh = treasureSO.treasureMesh;
    }
}
