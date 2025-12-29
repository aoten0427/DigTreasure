using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// エントランス画面の入力管理
/// </summary>
public class EntranceInput : MonoBehaviour
{
    private InputGame m_input;
    private bool m_isActive = true;

    private event Action<bool> m_onSelect;
    private event Action<bool> m_onCancel;
    private event Action<bool> m_onPause;
    private event Action<bool> m_onLoad;

    public event Action<bool> OnSelect { add => m_onSelect += value; remove => m_onSelect -= value; }
    public event Action<bool> OnCancel { add => m_onCancel += value; remove => m_onCancel -= value; }
    public event Action<bool> OnPause { add => m_onPause += value; remove => m_onPause -= value; }
    public event Action<bool> OnLoad { add => m_onLoad += value;remove => m_onLoad -= value; }

    public bool IsActive
    {
        get => m_isActive;
        set
        {
            m_isActive = value;
            if (value) m_input.Enable();
            else m_input.Disable();
        }
    }

    private void Awake()
    {
        // 入力システムの初期化
        m_input = new InputGame();

        // イベントハンドラの登録
        m_input.Normal.Select.performed += OnSelectPerformed;
        m_input.Normal.Select.canceled += OnSelectCanceled;
        m_input.Normal.Cancel.performed += OnCancelPerformed;
        m_input.Normal.Cancel.canceled += OnCancelCanceled;
        m_input.Normal.Pause.performed += OnPausePerformed;
        m_input.Normal.Pause.canceled += OnPauseCanceled;
        m_input.Normal.Up.performed += OnLoadPerformed;
        m_input.Normal.Up.canceled += OnLoadCanceled;

        // アクティブなら入力を有効化
        if (m_isActive) m_input.Enable();
    }

    private void OnDestroy()
    {
        // イベントハンドラの解除
        m_input.Normal.Select.performed -= OnSelectPerformed;
        m_input.Normal.Select.canceled -= OnSelectCanceled;
        m_input.Normal.Cancel.performed -= OnCancelPerformed;
        m_input.Normal.Cancel.canceled -= OnCancelCanceled;
        m_input.Normal.Pause.performed -= OnPausePerformed;
        m_input.Normal.Pause.canceled -= OnPauseCanceled;
        m_input.Normal.Up.performed -= OnLoadPerformed;
        m_input.Normal.Up.canceled -= OnLoadCanceled;

        // 入力システムの破棄
        m_input.Disable();
        m_input.Dispose();
    }

    private void OnSelectPerformed(InputAction.CallbackContext ctx) => m_onSelect?.Invoke(true);
    private void OnSelectCanceled(InputAction.CallbackContext ctx) => m_onSelect?.Invoke(false);
    private void OnCancelPerformed(InputAction.CallbackContext ctx) => m_onCancel?.Invoke(true);
    private void OnCancelCanceled(InputAction.CallbackContext ctx) => m_onCancel?.Invoke(false);
    private void OnPausePerformed(InputAction.CallbackContext ctx) => m_onPause?.Invoke(true);
    private void OnPauseCanceled(InputAction.CallbackContext ctx) => m_onPause?.Invoke(false);
    private void OnLoadPerformed(InputAction.CallbackContext ctx) => m_onLoad?.Invoke(true);
    private void OnLoadCanceled(InputAction.CallbackContext ctx) => m_onLoad?.Invoke(false);
}
