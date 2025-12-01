using Fusion;
using System.Collections;
using UnityEngine;
using static Unity.Collections.Unicode;

public class Treasure : NetworkBehaviour
{
    [Networked] public int ScorePoint { get; set; } = 1;
    [Networked] public int MeshIndex { get; set; } = 0;

    private TreasureList _treasureList;
    private MeshFilter _meshFilter;

    [SerializeField] float m_invalidTime = 1.0f;
    public float InvalidTime { get { return m_invalidTime; } set { m_invalidTime = value; } }
    [Networked]public bool IsInvalid { get; set; }

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

        IsInvalid = true;
        StartCoroutine(Unlock());
    }

    IEnumerator Unlock()
    {
        yield return new WaitForSeconds(m_invalidTime);
        IsInvalid = false;
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

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Field"))
        {
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
        }
        
    }
}
