using UnityEngine;

/// <summary>
/// プレイヤー入力処理クラス
/// GameInputManagerからの入力を各ロジッククラスに委譲
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    //PlayerManager参照
    private PlayerManager m_manager;

    //入力有効フラグ
    private bool m_isInputEnabled = true;

    //現在の移動入力
    private Vector2 m_currentMoveInput;

    //プロパティ
    public bool IsInputEnabled
    {
        get => m_isInputEnabled;
        set => m_isInputEnabled = value;
    }
    public Vector2 CurrentMoveInput => m_currentMoveInput;

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize(PlayerManager manager)
    {
        m_manager = manager;

        //入力イベント購読
        var inputManager = GameInputManager.Instance;
        if (inputManager != null)
        {
            inputManager.Move += OnMove;
            inputManager.Jump += OnJump;
            inputManager.DigUp += OnDigUp;
            inputManager.DigDown += OnDigDown;
            inputManager.Attack += OnAttack;
            inputManager.Barrier += OnBarrier;
            inputManager.LookOn += OnLockOn;
        }

        //イベント購読（スタン/バリアーで入力無効化）
        if (m_manager?.Events != null)
        {
            m_manager.Events.OnStunStart += DisableInput;
            m_manager.Events.OnStunEnd += EnableInput;
        }
    }

    private void OnDestroy()
    {
        //入力イベント解除
        var inputManager = GameInputManager.Instance;
        if (inputManager != null)
        {
            inputManager.Move -= OnMove;
            inputManager.Jump -= OnJump;
            inputManager.DigUp -= OnDigUp;
            inputManager.DigDown -= OnDigDown;
            inputManager.Attack -= OnAttack;
            inputManager.Barrier -= OnBarrier;
            inputManager.LookOn -= OnLockOn;
        }

        //イベント解除
        if (m_manager?.Events != null)
        {
            m_manager.Events.OnStunStart -= DisableInput;
            m_manager.Events.OnStunEnd -= EnableInput;
        }
    }

    /// <summary>
    /// 移動入力
    /// </summary>
    private void OnMove(Vector2 moveInput)
    {
        if (!m_manager.IsActive) return;
        m_currentMoveInput = moveInput;

        if (!m_isInputEnabled) return;

        //移動ロジックに委譲
        if (m_manager?.MovementLogic != null)
        {
            m_manager.MovementLogic.SetMoveInput(moveInput);
        }

        //イベント発火（アニメーション用）
        m_manager?.Events?.InvokeMove(moveInput);
    }

    /// <summary>
    /// ジャンプ入力
    /// </summary>
    private void OnJump(bool isPush)
    {
        if (!m_manager.IsActive) return;
        if (!m_isInputEnabled || !isPush) return;

        //移動ロジックに委譲
        if (m_manager?.MovementLogic != null)
        {
            m_manager.MovementLogic.TryJump();
        }
    }

    /// <summary>
    /// 上掘り入力
    /// </summary>
    private void OnDigUp(bool isPush)
    {
        if (!m_manager.IsActive) return;
        if (!m_isInputEnabled || !isPush) return;

        //掘りロジックに委譲
        if (m_manager?.DigLogic != null)
        {
            m_manager.DigLogic.DigUp();
        }
    }

    /// <summary>
    /// 下掘り入力
    /// </summary>
    private void OnDigDown(bool isPush)
    {
        if (!m_manager.IsActive) return;
        if (!m_isInputEnabled || !isPush) return;

        //掘りロジックに委譲
        if (m_manager?.DigLogic != null)
        {
            m_manager.DigLogic.DigDown();
        }
    }

    /// <summary>
    /// 攻撃入力（攻撃と前方掘りの両方を実行）
    /// </summary>
    private void OnAttack(bool isPush)
    {
        if (!m_manager.IsActive) return;
        if (!m_isInputEnabled || !isPush) return;

        //戦闘ロジックに委譲
        if (m_manager?.CombatLogic != null)
        {
            m_manager.CombatLogic.Attack();
        }

        //掘りロジックに委譲（前方掘り）
        if (m_manager?.DigLogic != null)
        {
            m_manager.DigLogic.DigForward();
        }

        //イベント発火（アニメーション用）
        m_manager?.Events?.InvokeAttack();
    }

    /// <summary>
    /// バリアー入力
    /// </summary>
    private void OnBarrier(bool isPush)
    {
        if (!m_manager.IsActive) return;
        if (!m_isInputEnabled) return;

        //バリアーロジックに委譲
        if (m_manager?.BarrierLogic != null)
        {
            if (isPush)
            {
                m_manager.BarrierLogic.StartBarrier();
            }
            else
            {
                m_manager.BarrierLogic.EndBarrier();
            }
        }
    }

    /// <summary>
    /// ロックオン入力
    /// </summary>
    private void OnLockOn(bool isPush)
    {
        if (!m_manager.IsActive) return;
        if (!m_isInputEnabled || !isPush) return;

        //戦闘ロジックに委譲
        if (m_manager?.CombatLogic != null)
        {
            m_manager.CombatLogic.LockOn();
        }
    }

    /// <summary>
    /// 入力を有効化
    /// </summary>
    public void EnableInput()
    {
        m_isInputEnabled = true;
    }

    /// <summary>
    /// 入力を無効化
    /// </summary>
    public void DisableInput()
    {
        m_isInputEnabled = false;
        m_currentMoveInput = Vector2.zero;

        //移動停止
        if (m_manager?.MovementLogic != null)
        {
            m_manager.MovementLogic.SetMoveInput(Vector2.zero);
        }
    }


}
