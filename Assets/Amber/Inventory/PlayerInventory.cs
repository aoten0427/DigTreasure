using UnityEngine;
using Fusion;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;

public class PlayerInventory : NetworkBehaviour
{
    [Networked, Capacity(300)] private NetworkDictionary<int, int> _treasures => default;
    private TreasureList _treasureList;
    private List<Treasure> _pickupBlacklist = new();

    public override void Spawned()
    {
        base.Spawned();
        if (HasStateAuthority)
        {
            _treasureList = Resources.Load<TreasureList>("Treasure/TreasureList");
            for (int i = 0; i < _treasureList.allTreasure.Count; i++)
                _treasures.Add(i, 0);
        }
    }
    private int Key(TreasureSO type)
    {
        return _treasureList.allTreasure.IndexOf(type);
    }
    public void AddTreasure(TreasureSO type, int amt)
    {
        _treasures.Set(Key(type), _treasures[Key(type)] + amt);
    }
    public void RemoveTreasure(TreasureSO type, int amt)
    {
        _treasures.Set(Key(type), Math.Max(0, _treasures[Key(type)] - 1));
    }
    public bool HasTreasureOfType(TreasureSO type)
    {
        return _treasures[Key(type)] > 0;
    }
    public bool HasTreasure()
    {
        foreach (var kvp in _treasures)
        {
            if (kvp.Value > 0)
                return true;
        }
        return false;
    }
    public int AmountOfTreasureType(TreasureSO type)
    {
        return _treasures[Key(type)];
    }
    public int AmountOfTreasure()
    {
        int returnVal = 0;
        foreach (var kvp in _treasures)
            returnVal += kvp.Value;
        return returnVal;
    }
    public List<TreasureSO> GetRandomTreasures(int amt)
    {
        List<TreasureSO> returnVal = new();
        foreach (var kvp in _treasures)
            returnVal.AddRange(Enumerable.Repeat(_treasureList.allTreasure[kvp.Key], kvp.Value));
        if (amt <= 0)
            return returnVal;
        else if (amt >= _treasures.Count())
            return returnVal;

        int iterations = _treasures.Count() - amt;
        for (int i = 0; i < iterations; i++)
        {
            returnVal.Remove(returnVal[UnityEngine.Random.Range(0, returnVal.Count)]);
            if (returnVal.Count == 0)
                break;
        }
        return returnVal;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (HasStateAuthority && other.TryGetComponent(out Treasure treasure))
        {
            AddTreasure(_treasureList.allTreasure[treasure.MeshIndex], 1);
            _pickupBlacklist.Add(treasure);
            treasure.RPC_RequestDespawn();
            StartCoroutine(PickupBuffer(treasure));
        }
    }

    private IEnumerator PickupBuffer(Treasure treasure)
    {
        while (treasure != null)
            yield return null;
        if (_pickupBlacklist.Contains(treasure))
            _pickupBlacklist.Remove(treasure);
    }
}
