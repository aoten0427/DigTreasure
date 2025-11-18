using System;
using System.Collections.Generic;
using UnityEngine;

public class GameInputManager : MonoBehaviour
{
    public static GameInputManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Initialize();

        DontDestroyOnLoad(gameObject);
    }

    public enum ButtonType
    {
        North,
        South,
        East,
        West,
        LShoulder,
        RShoulder,
        LTrigger,
        RTrigger
    }

    public enum ActionType
    {
        DigDown,
        DigUp,
        Jump,
        Attack,
        Barrier,
        LookOn
    }

    public event Action<Vector2> Move;
    public event Action<bool> DigDown;
    public event Action<bool> DigUp;
    public event Action<bool> Jump;
    public event Action<bool> Attack;
    public event Action<bool> Barrier;
    public event Action<bool> LookOn;



    // ButtonType → ActionType
    Dictionary<ButtonType, ActionType> m_inputActions;

    // ActionType → 対応イベント
    Dictionary<ActionType, Action<bool>> m_actionEvents;

    // 入力システム
    private InputGame m_input;

    private void OnEnable()
    {
        m_input?.Enable();
    }

    private void OnDisable()
    {
        m_input?.Disable();
    }


    void Initialize()
    {
        m_input = new InputGame();

        InitializeEventTable();

        InitializeInputActionTable();

        InitializeInputEvents();
    }

    /// <summary>
    /// アクション設定
    /// </summary>
    void InitializeEventTable()
    {
        m_actionEvents = new Dictionary<ActionType, Action<bool>>
        {
            { ActionType.DigDown, v => DigDown?.Invoke(v) },
            { ActionType.DigUp,   v => DigUp?.Invoke(v) },
            { ActionType.Jump,    v => Jump?.Invoke(v) },
            { ActionType.Attack,  v => Attack?.Invoke(v) },
            { ActionType.Barrier, v => Barrier?.Invoke(v) },
            { ActionType.LookOn,  v => LookOn?.Invoke(v) }
        };
    }

    void InitializeInputActionTable()
    {
        m_inputActions = new Dictionary<ButtonType, ActionType>
        {
            { ButtonType.North,ActionType.DigUp},
            { ButtonType.South,ActionType.DigDown},
            { ButtonType.East,ActionType.Jump},
            { ButtonType.West,ActionType.Attack},
            { ButtonType.RShoulder,ActionType.Barrier},
            { ButtonType.LShoulder,ActionType.LookOn},
        };

    }

    /// <summary>
    /// inputSystemと連携
    /// </summary>
    void InitializeInputEvents()
    {
        // Move（離したとき zero を送る）
        m_input.Play.Move.performed += ctx => Move?.Invoke(ctx.ReadValue<Vector2>());
        m_input.Play.Move.canceled += ctx => Move?.Invoke(Vector2.zero);

        // ボタンの状態登録（押した瞬間・離した瞬間）
        RegisterButton(m_input.Play.ButtonNorth, ButtonType.North);
        RegisterButton(m_input.Play.ButtonSouth, ButtonType.South);
        RegisterButton(m_input.Play.ButtonEast, ButtonType.East);
        RegisterButton(m_input.Play.ButtonWest, ButtonType.West);
        RegisterButton(m_input.Play.LeftShoulder, ButtonType.LShoulder);
        RegisterButton(m_input.Play.RightShoulder, ButtonType.RShoulder);
        RegisterButton(m_input.Play.LeftTrigger, ButtonType.LTrigger);
        RegisterButton(m_input.Play.RightTrigger, ButtonType.RTrigger);
    }

    void RegisterButton(UnityEngine.InputSystem.InputAction action, ButtonType type)
    {
        action.started += ctx => InputAction(type, true);
        action.canceled += ctx => InputAction(type, false);
    }

    /// <summary>
    /// アクション実行
    /// </summary>
    /// <param name="button"></param>
    /// <param name="isPush"></param>
    void InputAction(ButtonType button, bool isPush)
    {
        if (m_inputActions.TryGetValue(button, out var act))
        {
            if (m_actionEvents.TryGetValue(act, out var ev))
            {
                ev.Invoke(isPush);
            }
        }
    }

    /// <summary>
    /// キーバインド変更
    /// </summary>
    /// <param name="button"></param>
    /// <param name="action"></param>
    public void ChangeAction(ButtonType button, ActionType action)
    {
        m_inputActions[button] = action;
    }
}
