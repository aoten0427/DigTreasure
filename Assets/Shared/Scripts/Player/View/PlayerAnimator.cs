using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// プレイヤーアニメーション制御クラス
/// </summary>
public class PlayerAnimator : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator m_animator;

    //PlayerManager参照
    private PlayerManager m_manager;

    //スタン状態
    private bool m_isStun;

    //接地状態
    private bool m_isGrounded = false;

    //Dig関連変数
    private bool m_isDig;
    private bool m_digAnimCheck;
    private int m_lastDigLoopCount;

    //ヒットストップ関連
    private Coroutine m_hitStopCoroutine;


    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize(PlayerManager manager)
    {
        m_manager = manager;

        //Animator取得
        if (m_animator == null)
        {
            m_animator = GetComponentInChildren<Animator>();
        }


        //イベント購読
        if (m_manager?.Events != null)
        {
            m_manager.Events.OnMove += OnMove;
            
            m_manager.Events.OnGroundedChanged += OnGroundedChanged;
            m_manager.Events.OnAttack += OnAttack;
            m_manager.Events.OnDig += OnDig;
            m_manager.Events.OnStunStart += OnStunStart;
            m_manager.Events.OnStunEnd += OnStunEnd;
            
            m_manager.Events.OnBarrierStart += OnBarrierStart;
            m_manager.Events.OnBarrierEnd += OnBarrierEnd;
            m_manager.Events.OnHitStopStart += OnHitStopStart;
            m_manager.Events.OnHitStopEnd += OnHitStopEnd;
        }

        //Dig変数初期化
        m_isDig = false;
        m_digAnimCheck = false;
        m_lastDigLoopCount = 0;
    }

    private void OnDestroy()
    {
        //イベント解除
        if (m_manager?.Events != null)
        {
            m_manager.Events.OnMove -= OnMove;
           
            m_manager.Events.OnGroundedChanged -= OnGroundedChanged;
            m_manager.Events.OnAttack -= OnAttack;
            m_manager.Events.OnDig -= OnDig;
            m_manager.Events.OnStunStart -= OnStunStart;
            m_manager.Events.OnStunEnd -= OnStunEnd;
            
            m_manager.Events.OnBarrierStart -= OnBarrierStart;
            m_manager.Events.OnBarrierEnd -= OnBarrierEnd;
            m_manager.Events.OnHitStopStart -= OnHitStopStart;
            m_manager.Events.OnHitStopEnd -= OnHitStopEnd;
        }
    }

    private void Update()
    {
        if (m_animator == null) return;

        //Digアニメーション状態管理
        UpdateDigAnimation();
    }

    /// <summary>
    /// Digアニメーション状態更新
    /// </summary>
    private void UpdateDigAnimation()
    {
        AnimatorStateInfo stateInfo = m_animator.GetCurrentAnimatorStateInfo(0);

        //Dig_Start中の処理
        if (stateInfo.IsName("Dig_Start"))
        {
            //アニメーション終了時、チェック未完了の場合
            if (stateInfo.normalizedTime >= 1f && !m_digAnimCheck)
            {
                //isDigフラグに基づいて継続or終了
                m_animator.SetBool("isDig", m_isDig);
                SyncDigAnimation(m_isDig);
                m_isDig = false;
                m_digAnimCheck = true;
            }
        }
        //Dig_Loop中の処理
        else if (stateInfo.IsName("Dig_Loop"))
        {
            if (m_digAnimCheck)
            {
                m_digAnimCheck = false;
            }
            int currentLoopCount = (int)stateInfo.normalizedTime;
            //ループ回数が増えた場合、チェック未完了なら状態遷移
            if (currentLoopCount > m_lastDigLoopCount && !m_digAnimCheck)
            {
                m_animator.SetBool("isDig", m_isDig);
                SyncDigAnimation(m_isDig);
                m_isDig = false;
                m_digAnimCheck = true;
            }
            m_lastDigLoopCount = currentLoopCount;
        }
        //その他の状態では変数リセット
        else if (!stateInfo.IsName("Dig_End"))
        {
            m_isDig = false;
            m_lastDigLoopCount = 0;
            m_digAnimCheck = false;
        }
    }

    /// <summary>
    /// 移動イベント
    /// </summary>
    private void OnMove(Vector2 moveInput)
    {
        if (m_animator == null) return;
        //移動しているか？
        bool isWalk = moveInput.magnitude > 0.1f;
        bool currentWalk = m_animator.GetBool("isWalk");

        if(currentWalk && !isWalk)
        {
            m_animator.SetBool("isWalk", isWalk);
            SyncWalkAnimation(isWalk);
        }

        if(isWalk && (NowClip() == "Idle" || NextClip() == "Idle"))
        {
            m_animator.SetBool("isWalk", isWalk);
            SyncWalkAnimation(isWalk);
        }
    }

    /// <summary>
    /// 歩行アニメーションをネットワーク同期
    /// </summary>
    private void SyncWalkAnimation(bool isWalk)
    {
        if (m_manager?.IsOnlineMode == true &&
            m_manager.NetworkState != null &&
            m_manager.NetworkState.HasStateAuthority)
        {
            m_manager.NetworkState.RpcSyncWalkAnimation(isWalk);
        }
    }

    /// <summary>
    /// 地面接地状態変更イベント
    /// </summary>
    private void OnGroundedChanged(bool isGrounded)
    {
        m_isGrounded = isGrounded;
        if(m_isGrounded)
        {
            //地上についてスタン中ならスタンアニメーション開始
            StunAnimation();
        }
    }

    /// <summary>
    /// 攻撃イベント
    /// </summary>
    private void OnAttack()
    {
        
        if (m_manager.CombatLogic != null)
        {
            if (!m_manager.CombatLogic.IsAttacking) return;
        }
        if (m_animator != null)
        {
            m_animator.SetBool("isAttack", true);
            SyncAttackAnimation(true);
            StartCoroutine(Delay(() =>
            {
                m_animator.SetBool("isAttack", false);
                SyncAttackAnimation(false);
            }));
        }
        
    }

    /// <summary>
    /// 攻撃アニメーションをネットワーク同期
    /// </summary>
    private void SyncAttackAnimation(bool isAttack)
    {
        if (m_manager?.IsOnlineMode == true &&
            m_manager.NetworkState != null &&
            m_manager.NetworkState.HasStateAuthority)
        {
            m_manager.NetworkState.RpcSyncAttackAnimation(isAttack);
        }
    }

    /// <summary>
    /// 1フレーム遅延実行
    /// </summary>
    private IEnumerator Delay(Action action = null)
    {
        yield return null;
        action?.Invoke();
    }

    /// <summary>
    /// 掘りイベント
    /// </summary>
    private void OnDig(int direction)
    {
        if (m_animator == null) return;
        
        //攻撃中は掘り不可
        if (m_manager.CombatLogic != null && m_manager.CombatLogic.IsAttacking)
        {
            return;
        }

        AnimatorStateInfo stateInfo = m_animator.GetCurrentAnimatorStateInfo(0);

        //Digアニメーション再生中にイベント発火した場合、継続フラグを立てる
        if ((stateInfo.IsName("Dig_Start") || stateInfo.IsName("Dig_Loop")) &&
            stateInfo.normalizedTime % 1f >= 0.25f)
        {
            m_isDig = true;
        }
        //Digアニメーション再生中でない場合、アニメーション開始
        else if (!(stateInfo.IsName("Dig_Start") || stateInfo.IsName("Dig_Loop") || stateInfo.IsName("Dig_End")))
        {
            m_animator.SetBool("isDig", true);
            SyncDigAnimation(true);
        }
        
    }

    /// <summary>
    /// 掘りアニメーションをネットワーク同期
    /// </summary>
    private void SyncDigAnimation(bool isDig)
    {
        if (m_manager?.IsOnlineMode == true &&
            m_manager.NetworkState != null &&
            m_manager.NetworkState.HasStateAuthority)
        {
            m_manager.NetworkState.RpcSyncDigAnimation(isDig);
        }
    }

    /// <summary>
    /// スタンフラグ開始
    /// </summary>
    private void OnStunStart()
    {
        m_isStun = true;
        StartCoroutine(StunAnimation());
    }

    /// <summary>
    /// スタンアニメーション処理
    /// </summary>
    private IEnumerator StunAnimation()
    {
        yield return null;

        //更新後に地面ならすぐにスタン
        if (m_isGrounded)
        {
            m_animator.SetBool("isStun", true);
            SyncStunAnimation(true);
        }

        //地上にいなかつスタン中は待機
        while (!m_isGrounded && m_isStun)
        {
            yield return null;
        }

        //地上についてスタン中ならアニメーション
        if (m_isStun)
        {
            m_animator.SetBool("isStun", true);
            SyncStunAnimation(true);
        }
    }

    /// <summary>
    /// スタン終了イベント
    /// </summary>
    private void OnStunEnd()
    {
        m_isStun = false;
        if (m_animator != null)
        {
            m_animator.SetBool("isStun", false);
            SyncStunAnimation(false);
        }
    }

    /// <summary>
    /// スタンアニメーションをネットワーク同期
    /// </summary>
    private void SyncStunAnimation(bool isStun)
    {
        if (m_manager?.IsOnlineMode == true &&
            m_manager.NetworkState != null &&
            m_manager.NetworkState.HasStateAuthority)
        {
            m_manager.NetworkState.RpcSyncStunAnimation(isStun);
        }
    }

    /// <summary>
    /// バリアー開始イベント
    /// </summary>
    private void OnBarrierStart()
    {
        if (m_animator != null)
        {
            m_animator.SetBool("isDefense", true);
            SyncBarrierAnimation(true);
        }
    }

    /// <summary>
    /// バリアー終了イベント
    /// </summary>
    private void OnBarrierEnd()
    {
        if (m_animator != null)
        {
            m_animator.SetBool("isDefense", false);
            SyncBarrierAnimation(false);
        }
    }

    /// <summary>
    /// バリアーアニメーションをネットワーク同期
    /// </summary>
    private void SyncBarrierAnimation(bool isDefense)
    {
        if (m_manager?.IsOnlineMode == true &&
            m_manager.NetworkState != null &&
            m_manager.NetworkState.HasStateAuthority)
        {
            m_manager.NetworkState.RpcSyncBarrierAnimation(isDefense);
        }
    }

    /// <summary>
    /// ヒットストップ開始イベント
    /// </summary>
    private void OnHitStopStart(float duration)
    {
        if (m_animator == null) return;

        //既存のヒットストップを停止
        if (m_hitStopCoroutine != null)
        {
            StopCoroutine(m_hitStopCoroutine);
        }
        m_hitStopCoroutine = StartCoroutine(HitStopCoroutine(duration));
    }

    /// <summary>
    /// ヒットストップコルーチン
    /// </summary>
    private IEnumerator HitStopCoroutine(float duration)
    {
        m_animator.speed = 1.0f;
        yield return new WaitForSecondsRealtime(duration);
        m_animator.speed = 1f;
        m_manager?.Events?.InvokeHitStopEnd();
        m_hitStopCoroutine = null;
    }

    /// <summary>
    /// ヒットストップ終了イベント
    /// </summary>
    private void OnHitStopEnd()
    {
        if (m_hitStopCoroutine != null)
        {
            StopCoroutine(m_hitStopCoroutine);
            m_hitStopCoroutine = null;
        }
        if (m_animator != null)
        {
            m_animator.speed = 1f;
        }
    }

    /// <summary>
    /// アニメーション再生
    /// </summary>
    public void PlayAnimation(string stateName, int layer = 0)
    {
        if (m_animator != null)
        {
            m_animator.Play(stateName, layer);
        }
    }

    /// <summary>
    /// アニメーション速度設定
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        if (m_animator != null)
        {
            m_animator.speed = speed;
        }
    }

    /// <summary>
    /// トリガー設定
    /// </summary>
    public void SetTrigger(string triggerName)
    {
        if (m_animator != null)
        {
            m_animator.SetTrigger(triggerName);
        }
    }

    /// <summary>
    /// Bool設定
    /// </summary>
    public void SetBool(string paramName, bool value)
    {
        if (m_animator != null)
        {
            m_animator.SetBool(paramName, value);
        }
    }

    /// <summary>
    /// Float設定
    /// </summary>
    public void SetFloat(string paramName, float value)
    {
        if (m_animator != null)
        {
            m_animator.SetFloat(paramName, value);
        }
    }

    /// <summary>
    /// 現在再生中のクリップ名取得
    /// </summary>
    private string NowClip()
    {
        var clipInfo = m_animator.GetCurrentAnimatorClipInfo(0);

        if (clipInfo.Length > 0)
        {
            return clipInfo[0].clip.name;
        }

        return "None";
    }

    /// <summary>
    /// 次に再生されるクリップ名取得
    /// </summary>
    private string NextClip()
    {
        if (m_animator.IsInTransition(0))
        {
            var nextClipInfo = m_animator.GetNextAnimatorClipInfo(0);
            return nextClipInfo[0].clip.name;
        }

        return "None";
    }
}
