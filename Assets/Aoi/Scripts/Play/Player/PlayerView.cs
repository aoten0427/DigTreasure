using Fusion;
using UnityEngine;

public class PlayerView : NetworkBehaviour
{
    [SerializeField]
    MeshFilter m_meshFilter;

    [SerializeField] Mesh[] m_playerMeshs = new Mesh[4];


    public MeshFilter MeshFilter { get { return m_meshFilter; }}
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ChngeMesh(int id)
    {
        m_meshFilter.mesh = m_playerMeshs[id];
    }
}
