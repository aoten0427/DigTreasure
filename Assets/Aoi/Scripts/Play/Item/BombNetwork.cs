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

        m_bomb.OnDestroyAction(attaker.Player.DigUpdate);
        //m_rigidbody.AddForce((transform.position - attaker.Attacker.gameObject.transform.position).normalized * 50,ForceMode.Impulse);

        RPC_HolderChange(Runner.LocalPlayer);


        ////å†å¿ñ›Ç…îöî≠ÇàÀóä
        RPC_ExplosionHost(attaker.Attacker);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ExplosionHost(PlayerCombat attacker)
    {
        if (n_isExplosion) return;
        n_isExplosion = true;
        m_bomb.Attacker = attacker;
        m_bomb.IgnitionStart();
        RPC_Explosion();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Explosion()
    {
        Debug.Log("îöî≠");
        m_bomb.IgnitionStart(true);
    }

    public override void HolderChangeAction(bool isholder)
    {
        m_bomb.IsDestroyd = isholder;
        Debug.Log($"îjâÛÇïœçX:{isholder}");
    }
}