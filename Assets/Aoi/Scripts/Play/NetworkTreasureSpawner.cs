using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class NetworkTreasureSpawner : NetworkBehaviour
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

    public void SpawnTreasure(Vector3 spawnPosition, int scorePoint, int meshIndex)
    {
        Treasure newTreasure = Runner.Spawn(_treasurePrefab, spawnPosition, Quaternion.identity, onBeforeSpawned: (runner, obj) =>
        {
            Treasure treasure = obj.GetComponent<Treasure>();
            treasure.SetScorePoint(scorePoint);
            treasure.SetMeshIndex(meshIndex);
        });
        newTreasure.transform.parent = transform;
    }

    public Vector3 SpawnRandomTreasure()
    {
        List<TreasureSO> possibleTreasure = new();
        List<int> possibleTreasureIndices = new();

        while (possibleTreasure.Count == 0)
        {
            for (int i = 0; i < _treasureList.allTreasure.Count; i++)
            {
                TreasureSO treasure = _treasureList.allTreasure[i];
                int randomChance = UnityEngine.Random.Range(0, 101);
                if (randomChance <= treasure.spawnPercent)
                {
                    possibleTreasure.Add(treasure);
                    possibleTreasureIndices.Add(i);
                }
            }
        }

        int selectedIndex = Random.Range(0, possibleTreasure.Count);
        TreasureSO treasureSO = possibleTreasure[selectedIndex];
        int meshIndex = possibleTreasureIndices[selectedIndex];

        Vector3 spawnPosition = new(Random.Range(_minX, _maxX),
            Random.Range(treasureSO.lowestDepth, treasureSO.highestDepth),
            Random.Range(_minZ, _maxZ));

        Treasure newTreasure = Runner.Spawn(_treasurePrefab, spawnPosition, Quaternion.identity, onBeforeSpawned: (runner, obj) =>
        {
            Treasure treasure = obj.GetComponent<Treasure>();
            treasure.SetScorePoint(treasureSO.point);
            treasure.SetMeshIndex(meshIndex);
        });

        //newTreasure.transform.parent = transform;

        return spawnPosition;
    }
}
