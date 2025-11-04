using System.Threading.Tasks;
using UniRx;
using UnityEngine;

public enum InitializationPriority
{ 
    Map = 0,
    PlayerCreate = 100,
    UI = 150,
    TreasureCreate = 200
}


public interface IPlayInitialize
{
    InitializationPriority Priority { get; }

    int PriorityOffset => 0;

    int LoadWeight => 1;

    string Name { get; }

    void SetManager(PlayManager manager);

    Task InitializeAsync(ReactiveProperty<float> task = null);
}
