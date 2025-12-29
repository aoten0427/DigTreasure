using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class TitleInput : MonoBehaviour
{
    private InputGame m_input;
    private bool m_isActive = true;

    private event Action<bool> m_onLeft;
    private event Action<bool> m_onRight;
    private event Action<bool> m_onSelect;
    private event Action<bool> m_onPause;
    private event Action<bool> m_onCancel;

    public event Action<bool> OnLeft { add => m_onLeft += value; remove => m_onLeft -= value; }
    public event Action<bool> OnRight { add => m_onRight += value; remove => m_onRight -= value; }
    public event Action<bool> OnSelect { add => m_onSelect += value; remove => m_onSelect -= value; }
    public event Action<bool> OnPause { add => m_onPause += value; remove => m_onPause -= value; }
    public event Action<bool> OnCancel { add => m_onCancel += value;remove => m_onCancel -= value; }

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
        m_input = new InputGame();
        m_input.Normal.Select.performed += OnSelectPerformed;
        m_input.Normal.Select.canceled += OnSelectCanceled;
        m_input.Normal.Left.performed += OnLeftPerformed;
        m_input.Normal.Left.canceled += OnLeftCanceled;
        m_input.Normal.Right.performed += OnRightPerformed;
        m_input.Normal.Right.canceled += OnRightCanceled;
        m_input.Normal.Pause.performed += OnPausePerformed;
        m_input.Normal.Pause.canceled += OnPauseCanceled;
        m_input.Normal.Cancel.performed += OnCancelPerformed;
        m_input.Normal.Cancel.canceled += OnCancelCanceled;

        if (m_isActive) m_input.Enable();
    }

    private void OnDestroy()
    {
        m_input.Normal.Select.performed -= OnSelectPerformed;
        m_input.Normal.Select.canceled -= OnSelectCanceled;
        m_input.Normal.Left.performed -= OnLeftPerformed;
        m_input.Normal.Left.canceled -= OnLeftCanceled;
        m_input.Normal.Right.performed -= OnRightPerformed;
        m_input.Normal.Right.canceled -= OnRightCanceled;
        m_input.Normal.Pause.performed -= OnPausePerformed;
        m_input.Normal.Pause.canceled -= OnPauseCanceled;
        m_input.Normal.Cancel.performed -= OnCancelPerformed;
        m_input.Normal.Cancel.canceled -= OnCancelCanceled;
        m_input.Disable();
        m_input.Dispose();
    }

    private void OnSelectPerformed(InputAction.CallbackContext ctx) => m_onSelect?.Invoke(true);
    private void OnSelectCanceled(InputAction.CallbackContext ctx) => m_onSelect?.Invoke(false);
    private void OnLeftPerformed(InputAction.CallbackContext ctx) => m_onLeft?.Invoke(true);
    private void OnLeftCanceled(InputAction.CallbackContext ctx) => m_onLeft?.Invoke(false);
    private void OnRightPerformed(InputAction.CallbackContext ctx) => m_onRight?.Invoke(true);
    private void OnRightCanceled(InputAction.CallbackContext ctx) => m_onRight?.Invoke(false);
    private void OnPausePerformed(InputAction.CallbackContext ctx) => m_onPause?.Invoke(true);
    private void OnPauseCanceled(InputAction.CallbackContext ctx) => m_onPause?.Invoke(false);
    private void OnCancelPerformed(InputAction.CallbackContext ctx) => m_onCancel?.Invoke(true);
    private void OnCancelCanceled(InputAction.CallbackContext ctx) => m_onCancel?.Invoke(false);
}
