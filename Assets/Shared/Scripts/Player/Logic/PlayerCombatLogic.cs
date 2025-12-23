using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// プレイヤー戦闘ロジッククラス
/// </summary>
public class PlayerCombatLogic : MonoBehaviour
{
    [Header("ロックオン")]
    [SerializeField] private float m_lockOnRange = 5f;
    [SerializeField] private float m_lookSpeed = 5f;
    [SerializeField] private float m_lockOnIConRange = 15f;
    [SerializeField] private GameObject m_lockOnIcon;

    [Header("ノックバック")]
    [SerializeField] private float m_knockbackForce = 10f;

    [Header("スタン")]
    [SerializeField] private float m_stunDuration = 1f;

    [Header("免疫")]
    [SerializeField] private float m_immunityDuration = 2f;

    [Header("ヒットストップ")]
    [SerializeField] private float m_attackerHitStopDuration = 0.1f;
    [SerializeField] private float m_defenderHitStopDuration = 0.15f;

    //PlayerManager参照
    private PlayerManager m_manager;

    //ロックオン
    private PlayerCombatLogic m_lockTarget;
    private bool m_canLockOn = true;

    //攻撃
    private bool m_canAttack = true;
    private bool m_isAttacking;

    //プロパティ
    public bool CanAttack
    {
        get => m_canAttack;
        set => m_canAttack = value;
    }

    public bool IsAttacking { get {  return m_isAttacking; }}

    public PlayerCombatLogic LockTarget => m_lockTarget;

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize(PlayerManager manager)
    {
        m_manager = manager;

        //イベント購読
        if (m_manager?.Events != null)
        {
            m_manager.Events.OnStunStart += DisableAttack;
            m_manager.Events.OnStunEnd += EnableAttack;
            m_manager.Events.OnBarrierStart += DisableAttack;
            m_manager.Events.OnBarrierStart += DisableLockOn;
            m_manager.Events.OnBarrierEnd += EnableAttack;
            m_manager.Events.OnBarrierEnd += EnableLockOn;
        }
    }

    private void OnDestroy()
    {
        //イベント解除
        if (m_manager?.Events != null)
        {
            m_manager.Events.OnStunStart -= DisableAttack;
            m_manager.Events.OnStunEnd -= EnableAttack;
            m_manager.Events.OnBarrierStart -= DisableAttack;
            m_manager.Events.OnBarrierStart -= DisableLockOn;
            m_manager.Events.OnBarrierEnd -= EnableAttack;
            m_manager.Events.OnBarrierEnd -= EnableLockOn;
        }
    }

    private void Update()
    {
        //ロックオン距離チェック
        if (m_lockTarget != null)
        {
            float distance = Vector3.Distance(transform.position, m_lockTarget.transform.position);
            if (distance > m_lockOnIConRange)
            {
                ChangeLockTarget(null);
                m_manager?.MovementLogic?.SetLockOnRotation(false,Quaternion.identity);
            }
        }


        //回転処理
        HandleRotation();
    }



    public void LockOn()
    {
        if (!m_canLockOn) return;
        
        Collider[] nearPlayers = Physics.OverlapSphere(transform.position, m_lockOnIConRange);
        if (nearPlayers.Length == 0)
        {
            ChangeLockTarget(null);
            return;
        }

        float nearestDist = float.MaxValue;
        float smallestAngle = float.MaxValue;
        PlayerCombatLogic newLockTarget = m_lockTarget;

        foreach (Collider coll in nearPlayers)
        {
            if (coll.gameObject == gameObject) continue;
            if (m_lockTarget != null && coll.gameObject == m_lockTarget.gameObject) continue;

            var targetCombat = coll.GetComponent<PlayerCombatLogic>();


            if (targetCombat == null || targetCombat == m_manager.CombatLogic) continue;

            Vector3 toTarget = targetCombat.transform.position - transform.position;
            float dist = toTarget.magnitude;
            if (dist > m_lockOnIConRange) continue;

            float angle = Vector3.Angle(transform.forward, toTarget);

            if (dist < nearestDist || (Mathf.Abs(dist - nearestDist) <= 0.02f && angle < smallestAngle))
            {
                newLockTarget = targetCombat;
                nearestDist = dist;
                smallestAngle = angle;
            }
        }
        if (!newLockTarget) return;
        if(!newLockTarget.m_lockOnIcon.activeSelf)
        {
            ChangeLockTarget(newLockTarget);
        }
        else
        {
            ChangeLockTarget(null);
            m_manager?.MovementLogic?.SetLockOnRotation(false, Quaternion.identity);
        }
        
    }

    /// <summary>
    /// ロックオン切り替え
    /// </summary>
    public void ToggleLockOn()
    {

        if (!m_canLockOn) return;

        Collider[] nearPlayers = Physics.OverlapSphere(transform.position, m_lockOnRange);
        

        if (nearPlayers.Length == 0)
        {
            ChangeLockTarget(null);
            return;
        }

        float nearestDist = float.MaxValue;
        float smallestAngle = float.MaxValue;
        PlayerCombatLogic newLockTarget = m_lockTarget;

        foreach (Collider coll in nearPlayers)
        {
            if (coll.gameObject == gameObject) continue;
            if (m_lockTarget != null && coll.gameObject == m_lockTarget.gameObject) continue;

            var targetCombat = coll.GetComponent<PlayerCombatLogic>();
            

            if (targetCombat == null||targetCombat == m_manager.CombatLogic) continue;

            Vector3 toTarget = targetCombat.transform.position - transform.position;
            float dist = toTarget.magnitude;
            if (dist > m_lockOnRange) continue;

            float angle = Vector3.Angle(transform.forward, toTarget);

            if (dist < nearestDist || (Mathf.Abs(dist - nearestDist) <= 0.02f && angle < smallestAngle))
            {
                newLockTarget = targetCombat;
                nearestDist = dist;
                smallestAngle = angle;
            }
        }

        
        ChangeLockTarget(newLockTarget);
    }

    /// <summary>
    /// ロックオンターゲット変更
    /// </summary>
    private bool ChangeLockTarget(PlayerCombatLogic newTarget)
    {
        if (m_lockTarget != null && m_lockTarget.m_lockOnIcon != null)
        {
            m_lockTarget.m_lockOnIcon.SetActive(false);
        }

        m_lockTarget = newTarget;

        if (m_lockTarget != null && m_lockTarget.m_lockOnIcon != null)
        {
            m_lockTarget.m_lockOnIcon.SetActive(true);
        }

        return m_lockTarget != null;
    }

    /// <summary>
    /// 回転処理
    /// </summary>
    private void HandleRotation()
    {
        if (m_lockTarget == null || m_isAttacking) return;

        Vector3 dir = m_lockTarget.transform.position - transform.position;
        dir.y = 0f;

        Quaternion targetRot = Quaternion.Lerp(
            transform.rotation,
            Quaternion.LookRotation(dir.normalized, Vector3.up),
            Time.deltaTime * m_lookSpeed
        );

        m_manager?.MovementLogic?.SetLockOnRotation(true, targetRot);
    }

    /// <summary>
    /// 攻撃
    /// </summary>
    public bool Attack()
    {

        if (!m_canAttack)
        {
            return false;
        }

        //ターゲット決定
        var attackTarget = m_lockTarget != null ? m_lockTarget : GetAttackTarget();
        if(attackTarget == m_manager.CombatLogic)
        {
            return false;
        }
        

        if (attackTarget == null)
        {
            return false;
        }

        m_isAttacking = true;
        StartCoroutine(Delay(() => m_isAttacking = false));
        


        //オンラインモードでダメージ送信
        if (m_manager != null && m_manager.IsOnlineMode && m_manager.NetworkState != null)
        {
            var targetManager = attackTarget.m_manager;
            if (targetManager?.NetworkState != null)
            {
                var damageData = new PlayerDamageData(
                    attacker: m_manager.NetworkState,
                    target: targetManager.NetworkState,
                    knockbackForce: m_knockbackForce,
                    stunDuration: m_stunDuration,
                    direction: (attackTarget.transform.position - transform.position).normalized,
                    isDirect: true
                );
                targetManager.NetworkState.RpcTakeDamage(damageData);

                //攻撃側ヒットストップ
                m_manager.Events?.InvokeHitStopStart(m_attackerHitStopDuration);
            }
        }
        else
        {
            //オフラインモードでダメージ直接処理
            var damageData = new PlayerDamageData(
                knockbackForce: m_knockbackForce,
                stunDuration: m_stunDuration,
                direction: (attackTarget.transform.position - transform.position).normalized,
                isDirect: true
            );
            attackTarget.ProcessDamage(damageData);

            //攻撃側ヒットストップ
            m_manager?.Events?.InvokeHitStopStart(m_attackerHitStopDuration);
        }

        return true;
    }

    IEnumerator Delay(Action action = null)
    {
        yield return null;
        action?.Invoke();
    }


    /// <summary>
    /// 攻撃ターゲット取得
    /// </summary>
    private PlayerCombatLogic GetAttackTarget()
    {
        ToggleLockOn();
        var returnVal = m_lockTarget;
        ChangeLockTarget(null);
        return returnVal;
    }

    

    /// <summary>
    /// ダメージ処理
    /// </summary>
    public void ProcessDamage(PlayerDamageData damage)
    {
        //被攻撃側ヒットストップ
        m_manager?.Events?.InvokeHitStopStart(m_defenderHitStopDuration);

        //ヒットストップをネットワーク同期
        if (m_manager?.IsOnlineMode == true &&
            m_manager.NetworkState != null &&
            m_manager.NetworkState.HasStateAuthority)
        {
            m_manager.NetworkState.RpcPlayHitStop(m_defenderHitStopDuration);
        }

        //イベント発火
        m_manager?.Events?.InvokeDamageReceived(damage.KnockbackForce);

        //ダメージエフェクト同期
        if (m_manager?.IsOnlineMode == true &&
            m_manager.NetworkState != null &&
            m_manager.NetworkState.HasStateAuthority)
        {
            Vector3 effectPos = transform.position + Vector3.up;
            Quaternion effectRot = Quaternion.LookRotation(-damage.Direction);
            m_manager.NetworkState.RpcPlayDamageEffect(effectPos, effectRot);
        }
        else
        {
            //オフラインモード
            Vector3 effectPos = transform.position + Vector3.up;
            Quaternion effectRot = Quaternion.LookRotation(-damage.Direction);
            m_manager?.View?.PlayDamageEffect(effectPos, effectRot);
        }

        //スタン
        if (m_manager?.NetworkState != null && m_manager.IsOnlineMode)
        {
            m_manager.NetworkState.SetStunned(damage.StunDuration);
            m_manager.NetworkState.SetImmune(m_immunityDuration);
        }
        else
        {
            m_manager?.Events?.InvokeStunStart();
            StartCoroutine(StunCoroutine(damage.StunDuration));
            StartCoroutine(ImmunityCoroutine(m_immunityDuration));
        }

        //ノックバック
        ApplyKnockback(damage.Direction, damage.KnockbackForce);

        //宝落とし
        m_manager?.InventoryLogic?.LoseTreasure();
    }

    /// <summary>
    /// ノックバック適用
    /// </summary>
    public void ApplyKnockback(Vector3 direction, float force)
    {
        var rb = m_manager?.Rigidbody;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(direction * force, ForceMode.Impulse);
        }
        m_manager?.Events?.InvokeKnockback(direction, force);
    }

    private IEnumerator KnockbackCoroutine(Vector3 direction, float force)
    {
        yield return new WaitForSeconds(0.1f);

        
    }

    private IEnumerator StunCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        m_manager?.Events?.InvokeStunEnd();
    }

    private IEnumerator ImmunityCoroutine(float duration)
    {
        m_manager?.Events?.InvokeImmunityStart();
        yield return new WaitForSeconds(duration);
        m_manager?.Events?.InvokeImmunityEnd();
    }

    /// <summary>
    /// 攻撃有効化
    /// </summary>
    public void EnableAttack()
    {
        m_canAttack = true;
    }

    /// <summary>
    /// 攻撃無効化
    /// </summary>
    public void DisableAttack()
    {
        m_canAttack = false;
    }

    /// <summary>
    /// ロックオン有効化
    /// </summary>
    public void EnableLockOn()
    {
        m_canLockOn = true;
    }

    /// <summary>
    /// ロックオン無効化
    /// </summary>
    public void DisableLockOn()
    {
        m_canLockOn = false;
    }
}
