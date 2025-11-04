using UnityEngine;

public interface IInitializable
{
    bool IsInitialized { get; }
    void Initialize();
    void Reset();
}
