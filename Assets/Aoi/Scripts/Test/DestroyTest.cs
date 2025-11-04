using UnityEngine;
using Fusion;
using NetWork;
using System.Linq;

public class DestroyTest : MonoBehaviour
{
    [SerializeField] NetworkPrefabRef prefab;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var la = GameLauncher.Instance;

        la.OnPlayerJoined += Spowan;
    }

    // Update is called once per frame
    void Update()
    {

    }

    void Spowan(NetworkRunner runner,PlayerRef user)
    {
        if(runner.LocalPlayer == user)
        {
            runner.Spawn(prefab,new Vector3(runner.ActivePlayers.Count(),0,0));
        }
    }
}
