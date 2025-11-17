using Fusion;
using NetWork;
using UnityEngine;

public class TManager : MonoBehaviour
{
    [SerializeField]GameLauncher gameLauncher;

    [SerializeField] private NetworkPrefabRef playerPrefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameLauncher.OnPlayerJoined += Spown;
    }

    void Spown(NetworkRunner runner,PlayerRef user)
    {
        if(runner.LocalPlayer == user)
        {
            NetworkObject spawnedPlayer = runner.Spawn(playerPrefab);
        }

        
    }
}
