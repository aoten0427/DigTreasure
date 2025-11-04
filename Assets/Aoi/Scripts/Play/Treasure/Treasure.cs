using Fusion;
using UnityEngine;
using static Unity.Collections.Unicode;

public class Treasure : NetworkBehaviour
{
    [Networked] public int ScorePoint { get; set; } = 1;
    [Networked] public int MeshIndex { get; set; } = 0;

    private TreasureList _treasureList;
    private MeshFilter _meshFilter;

    private void Awake()
    {
        _treasureList = Resources.Load<TreasureList>("Treasure/TreasureList");
        _meshFilter = transform.GetChild(0).GetComponent<MeshFilter>();
    }

    public override void Spawned()
    {
        // ネットワーク同期されたMeshIndexからメッシュを設定
        if (_treasureList != null && _treasureList.allTreasure.Count > MeshIndex)
        {
            _meshFilter.mesh = _treasureList.allTreasure[MeshIndex].treasureMesh;
        }
    }

    // 他のクライアントからDespawn要求を受け取る
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDespawn()
    {
        if (Object.HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }

    // OnTriggerEnterは削除 - LocalScore側で処理する
    public void SetScorePoint(int score)
    {
        ScorePoint = score;
    }

    public void SetMeshIndex(int index)
    {
        MeshIndex = index;
    }

}
