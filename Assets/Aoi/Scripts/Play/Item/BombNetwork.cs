using Fusion;
using UnityEngine;

public class BombNetwork : NetworkBehaviour
{
    [Networked]
    private NetworkBool n_isExplosion { get; set; }

    [SerializeField]
    private BombLocal m_bomb;


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Explosion()
    {
        Debug.Log("”š”­");
        m_bomb.IgnitionStart(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority) return;
        if (n_isExplosion) return;

        n_isExplosion = true;
        m_bomb.IgnitionStart(true);
        RPC_Explosion();
    }
}