using Fusion;
using UnityEngine;

public struct PlayerDamage : INetworkStruct
{
    

    public PlayerDamage(PlayerCombat attaker = null,PlayerCombat target = null,
        float knokbackforce = 0,float starnduration = 0,Vector3 direction = default,bool isdirect = true)
    {
        m_attackerId = attaker ? attaker.Id : default;
        m_targetId = target ? target.Id : default;
        m_knockbackForce = knokbackforce;
        m_stunDuration = starnduration;
        m_direction = direction;
        m_isDirect = isdirect;
    }

    private NetworkBehaviourId m_attackerId;
    private NetworkBehaviourId m_targetId;
    //ノックバック
    private float m_knockbackForce;
    //スタン時間
    private float m_stunDuration;
    //攻撃方向
    private Vector3 m_direction;
    //直接攻撃か
    private bool m_isDirect;

    public void SetAttacker(PlayerCombat attacker)
    {
        m_attackerId = attacker ? attacker.Id : default;
    }

    public void SetTarget(PlayerCombat target)
    {
        m_targetId = target ? target.Id : default;
    }


    public PlayerCombat GetAttacker(NetworkRunner runner)
    {
        Debug.Log($"アタッカーID:{m_attackerId}");
        return runner.TryFindBehaviour(m_attackerId, out PlayerCombat combat) ? combat : null;
    }

    public PlayerCombat GetTarget(NetworkRunner runner)
    {
        return runner.TryFindBehaviour(m_targetId, out PlayerCombat combat) ? combat : null;
    }
    public float KnockBackForce {  get { return m_knockbackForce;}set { m_knockbackForce = value; } }
    public float StunDuration { get { return m_stunDuration; }set { m_stunDuration = value; } }
    public Vector3 Direction { get { return m_direction; } set { m_direction = value; } }
    public bool IsDirect {  get { return m_isDirect; } set { m_isDirect = value; } }
}
