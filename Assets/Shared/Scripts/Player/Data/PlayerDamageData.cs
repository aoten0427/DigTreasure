using Fusion;
using UnityEngine;

/// <summary>
/// プレイヤーダメージデータ（ネットワーク送信用）
/// </summary>
public struct PlayerDamageData : INetworkStruct
{
    //アタッカーID
    private NetworkBehaviourId m_attackerId;
    //ターゲットID
    private NetworkBehaviourId m_targetId;
    //ノックバック力
    private float m_knockbackForce;
    //スタン時間
    private float m_stunDuration;
    //攻撃方向
    private Vector3 m_direction;
    //直接攻撃か
    private NetworkBool m_isDirect;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    public PlayerDamageData(
        PlayerNetworkState attacker = null,
        PlayerNetworkState target = null,
        float knockbackForce = 0f,
        float stunDuration = 0f,
        Vector3 direction = default,
        bool isDirect = true)
    {
        m_attackerId = attacker ? attacker.Id : default;
        m_targetId = target ? target.Id : default;
        m_knockbackForce = knockbackForce;
        m_stunDuration = stunDuration;
        m_direction = direction;
        m_isDirect = isDirect;
    }

    /// <summary>
    /// アタッカーを設定
    /// </summary>
    public void SetAttacker(PlayerNetworkState attacker)
    {
        m_attackerId = attacker ? attacker.Id : default;
    }

    /// <summary>
    /// ターゲットを設定
    /// </summary>
    public void SetTarget(PlayerNetworkState target)
    {
        m_targetId = target ? target.Id : default;
    }

    /// <summary>
    /// アタッカーを取得
    /// </summary>
    public PlayerNetworkState GetAttacker(NetworkRunner runner)
    {
        if (runner == null) return null;
        return runner.TryFindBehaviour(m_attackerId, out PlayerNetworkState state) ? state : null;
    }

    /// <summary>
    /// ターゲットを取得
    /// </summary>
    public PlayerNetworkState GetTarget(NetworkRunner runner)
    {
        if (runner == null) return null;
        return runner.TryFindBehaviour(m_targetId, out PlayerNetworkState state) ? state : null;
    }

    //プロパティ
    public float KnockbackForce
    {
        get => m_knockbackForce;
        set => m_knockbackForce = value;
    }

    public float StunDuration
    {
        get => m_stunDuration;
        set => m_stunDuration = value;
    }

    public Vector3 Direction
    {
        get => m_direction;
        set => m_direction = value;
    }

    public bool IsDirect
    {
        get => m_isDirect;
        set => m_isDirect = value;
    }
}
