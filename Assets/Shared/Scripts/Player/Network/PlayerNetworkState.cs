using Fusion;
using UnityEngine;

/// <summary>
/// プレイヤーのネットワーク同期状態管理クラス
/// オンライン時のみアクティブ
/// </summary>
public class PlayerNetworkState : NetworkBehaviour
{
    //プレイヤー名
    [Networked] public NetworkString<_16> NickName { get; set; }

    //状態同期
    [Networked] private NetworkBool m_isStunned { get; set; }
    [Networked] private NetworkBool m_isBarrierActive { get; set; }
    [Networked] private NetworkBool m_isImmune { get; set; }

    //タイマー同期
    [Networked] private float m_stunTimer { get; set; }
    [Networked] private float m_immunityTimer { get; set; }

    //PlayerManager参照
    private PlayerManager m_manager;

    //プロパティ
    public bool IsStunned => m_isStunned;
    public bool IsBarrierActive => m_isBarrierActive;
    public bool IsImmune => m_isImmune;

    public bool NetworkStateAuthority { get {
            return Runner != null && Runner.IsRunning && HasStateAuthority;
        } }

    public override void Spawned()
    {
        base.Spawned();

        //PlayerManagerを取得
        m_manager = GetComponent<PlayerManager>();
        if (m_manager == null)
        {
            Debug.LogError("[PlayerNetworkState] PlayerManagerが見つかりません");
            return;
        }

        //マネージャーを初期化
        m_manager.Initialize(HasStateAuthority);
        
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        //スタンタイマー更新
        if (m_isStunned && m_stunTimer > 0)
        {
            m_stunTimer -= Runner.DeltaTime;
            if (m_stunTimer <= 0)
            {
                m_isStunned = false;
                m_manager?.Events?.InvokeStunEnd();

                //全クライアントにアニメーション同期
                RpcSyncStunAnimation(false);

                //全クライアントにエフェクト同期
                RpcPlayStunEffect(false);
            }
        }

        //免疫タイマー更新
        if (m_isImmune && m_immunityTimer > 0)
        {
            m_immunityTimer -= Runner.DeltaTime;
            if (m_immunityTimer <= 0)
            {
                m_isImmune = false;
                m_manager?.Events?.InvokeImmunityEnd();
            }
        }
    }

    /// <summary>
    /// スタン状態を設定
    /// </summary>
    public void SetStunned(float duration)
    {
        //if (!HasStateAuthority) return;

        m_isStunned = true;
        m_stunTimer = duration;

        //ローカルイベント発火
        m_manager?.Events?.InvokeStunStart();

        //全クライアントにアニメーション同期
        RpcSyncStunAnimation(true);

        //全クライアントにエフェクト同期
        RpcPlayStunEffect(true);
    }


    /// <summary>
    /// バリアー状態を設定
    /// </summary>
    public void SetBarrierActive(bool active)
    {
        ///if (!HasStateAuthority) return;

        m_isBarrierActive = active;

        //ローカルイベント発火
        if (active)
        {
            m_manager?.Events?.InvokeBarrierStart();
        }
        else
        {
            m_manager?.Events?.InvokeBarrierEnd();
        }

        //全クライアントにアニメーション同期
        RpcSyncBarrierAnimation(active);

        //全クライアントにエフェクト同期
        RpcPlayBarrierEffect(active);
    }

    /// <summary>
    /// 免疫状態を設定
    /// </summary>
    public void SetImmune(float duration)
    {
        if (!HasStateAuthority) return;

        m_isImmune = true;
        m_immunityTimer = duration;
        m_manager?.Events?.InvokeImmunityStart();
    }

    /// <summary>
    /// ダメージを受けるRPC
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcTakeDamage(PlayerDamageData damage)
    {
        if (m_manager == null) return;

        //免疫中はダメージを受けない
        if (m_isImmune) return;

        //バリアー中の直接攻撃は反射
        if (m_isBarrierActive && damage.IsDirect)
        {
            var attacker = damage.GetAttacker(Runner);
            if (attacker != null)
            {
                attacker.RpcBarrierReflect(this);
            }
            return;
        }

        //戦闘ロジックにダメージ処理を委譲
        if (m_manager.CombatLogic != null)
        {
            m_manager.CombatLogic.ProcessDamage(damage);
        }
    }

    /// <summary>
    /// バリアー反射RPC
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcBarrierReflect(PlayerNetworkState barrierPlayer)
    {
        if (m_manager == null || barrierPlayer == null) return;

        //戦闘ロジックにノックバック処理を委譲
        if (m_manager.CombatLogic != null)
        {
            Vector3 direction = (transform.position - barrierPlayer.transform.position).normalized;
            m_manager.CombatLogic.ApplyKnockback(direction, 10f);
        }
    }

    /// <summary>
    /// スタンエフェクト同期RPC
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcPlayStunEffect(bool active)
    {
        if (m_manager?.View != null)
        {
            m_manager.View.SetStunEffect(active);
        }
    }

    /// <summary>
    /// ダメージエフェクト同期RPC
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcPlayDamageEffect(Vector3 position, Quaternion rotation)
    {
        if (m_manager?.View != null)
        {
            m_manager.View.PlayDamageEffect(position, rotation);
        }
    }

    /// <summary>
    /// バリアーエフェクト同期RPC
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcPlayBarrierEffect(bool active)
    {
        if (m_manager?.View != null)
        {
            m_manager.View.SetBarrierEffect(active);
        }
    }

    #region アニメーション同期RPC

    /// <summary>
    /// 歩行アニメーション同期RPC
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcSyncWalkAnimation(bool isWalk)
    {
        //StateAuthorityはローカルイベントで処理済みなのでスキップ
        if (HasStateAuthority) return;

        m_manager?.Animator?.SetBool("isWalk", isWalk);
    }

    /// <summary>
    /// 攻撃アニメーション同期RPC
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcSyncAttackAnimation(bool isAttack)
    {
        //StateAuthorityはローカルイベントで処理済みなのでスキップ
        if (HasStateAuthority) return;

        m_manager?.Animator?.SetBool("isAttack", isAttack);
    }

    /// <summary>
    /// 掘りアニメーション同期RPC
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcSyncDigAnimation(bool isDig)
    {
        //StateAuthorityはローカルイベントで処理済みなのでスキップ
        if (HasStateAuthority) return;

        m_manager?.Animator?.SetBool("isDig", isDig);
    }

    /// <summary>
    /// バリアーアニメーション同期RPC
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcSyncBarrierAnimation(bool isDefense)
    {
        //StateAuthorityはローカルイベントで処理済みなのでスキップ
        Debug.Log($"[RPC] RpcSyncBarrierAnimation received: isDefense={isDefense}, HasStateAuthority={HasStateAuthority}");
        if (HasStateAuthority) return;

        Debug.Log($"[RPC] Setting animator isDefense={isDefense}");
        m_manager?.Animator?.SetBool("isDefense", isDefense);
    }

    /// <summary>
    /// スタンアニメーション同期RPC
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcSyncStunAnimation(bool isStun)
    {
        //StateAuthorityはローカルイベントで処理済みなのでスキップ
        if (HasStateAuthority) return;

        m_manager?.Animator?.SetBool("isStun", isStun);
    }

    #endregion

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_MaterialChange(int id)
    {
        if(HasStateAuthority) return;
        m_manager?.View?.SetMaterial(id);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_NameChange(string name)
    {
        if (HasStateAuthority) return;
        m_manager?.View?.SetPlayerName(name);
    }

    /// <summary>
    /// ヒットストップ同期RPC
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RpcPlayHitStop(float duration)
    {
        //StateAuthorityはローカルイベントで処理済みなのでスキップ
        if (HasStateAuthority) return;

        m_manager?.Events?.InvokeHitStopStart(duration);
    }
}
