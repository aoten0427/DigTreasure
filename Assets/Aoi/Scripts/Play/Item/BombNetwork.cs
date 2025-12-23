using Fusion;
using UnityEngine;

public class BombNetwork : NetworkItem
{
    [Networked] private NetworkBool n_isExplosion { get; set; } = false;
    [Networked] private NetworkBool n_isIgnition { get; set; } = false;

    Rigidbody m_rigidbody;

    [SerializeField]
    private BombLocal m_bomb;

    [SerializeField] private float m_exprosionTime = 5.0f;
    [SerializeField] private float m_decisionTime = 4.8f;

    public override void Spawned()
    {
        m_rigidbody = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (n_isExplosion) return;
        var attaker = other.GetComponent<IPlayerAttack>();
        if (attaker == null) return;

        m_bomb.OnDestroyAction(attaker.Player.DigLogic.OnDigComplete);

        ////å†å¿ñ›Ç…îöî≠ÇàÀóä
        RPC_ExplosionHost(Runner.LocalPlayer);
        m_bomb.Attacker = attaker.Player;

    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ExplosionHost(PlayerRef player)
    {
        if (n_isExplosion) return;
        n_isExplosion = true;
        RPC_Explosion(player);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Explosion(PlayerRef player)
    {
        if(player == Runner.LocalPlayer)
        {
            Debug.Log("îöî≠é“Ç≈Ç∑");
            m_bomb.IsDestroyd = true;
        }
        m_bomb.IgnitionStart();
    }

    public override void HolderChangeAction(bool isholder)
    {
        m_bomb.IsDestroyd = isholder;
        Debug.Log($"îjâÛÇïœçX:{isholder}");
    }

    public void Destroy()
    {
        
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_Destroy()
    {
        Destroy(gameObject);
    }
}