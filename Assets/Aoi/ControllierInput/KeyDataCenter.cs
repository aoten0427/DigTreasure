using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// キー入力を管理
/// </summary>
public class KeyDataCenter : MonoBehaviour
{
    enum KeyState
    {
        Case1,
        Case2
    }

    InputGame m_input;

    // アクティブフラグ
    [SerializeField] bool m_isActive = true;
    // 現在の文字状態
    KeyState m_keyState = KeyState.Case1;
    // キーマネージャー
    [SerializeField] KeyDataManager m_keyDataManager;
    // 選択中のキー
    [SerializeField] KeyData m_selectData;
    // 管理対象キー
    [SerializeField] List<KeyData> m_keydatas = new List<KeyData>();

    // 長押しリピート設定
    [SerializeField] float m_repeatDelay = 0.4f;
    [SerializeField] float m_repeatInterval = 0.1f;

    // リピート用コルーチン
    Coroutine m_repeatCoroutine;

    public bool IsActive { get { return m_isActive; } set { m_isActive = value; } }

    /// <summary>
    /// 初期化処理
    /// </summary>
    private void Awake()
    {
        // 入力システムの初期化
        m_input = new InputGame();

        // 移動イベント登録（押下・解放両方）
        m_input.Normal.Up.started += OnMoveUpStarted;
        m_input.Normal.Up.canceled += OnMoveCanceled;
        m_input.Normal.Down.started += OnMoveDownStarted;
        m_input.Normal.Down.canceled += OnMoveCanceled;
        m_input.Normal.Left.started += OnMoveLeftStarted;
        m_input.Normal.Left.canceled += OnMoveCanceled;
        m_input.Normal.Right.started += OnMoveRightStarted;
        m_input.Normal.Right.canceled += OnMoveCanceled;

        // その他のイベント登録
        m_input.Normal.Select.performed += OnSelect;
        m_input.Normal.Cancel.performed += OnDelete;
        m_input.Normal.X.performed += OnChange;
        m_input.Normal.Y.performed += OnSpace;
        m_input.Normal.RSButton.performed += OnCycle;
        m_input.Normal.LSButton.performed += OnCycle;

        // 入力を有効化
        m_input.Enable();

        // 初期選択
        m_selectData.Select(null);
    }

    /// <summary>
    /// 破棄処理
    /// </summary>
    private void OnDestroy()
    {
        // リピート停止
        StopRepeat();

        if (m_input != null)
        {
            // 移動イベント解除
            m_input.Normal.Up.started -= OnMoveUpStarted;
            m_input.Normal.Up.canceled -= OnMoveCanceled;
            m_input.Normal.Down.started -= OnMoveDownStarted;
            m_input.Normal.Down.canceled -= OnMoveCanceled;
            m_input.Normal.Left.started -= OnMoveLeftStarted;
            m_input.Normal.Left.canceled -= OnMoveCanceled;
            m_input.Normal.Right.started -= OnMoveRightStarted;
            m_input.Normal.Right.canceled -= OnMoveCanceled;

            // その他のイベント解除
            m_input.Normal.Select.performed -= OnSelect;
            m_input.Normal.Cancel.performed -= OnDelete;
            m_input.Normal.X.performed -= OnChange;
            m_input.Normal.Y.performed -= OnSpace;
            m_input.Normal.RSButton.performed -= OnCycle;
            m_input.Normal.LSButton.performed -= OnCycle;

            // 入力システムの破棄
            m_input.Disable();
            m_input.Dispose();
        }
    }

    /// <summary>
    /// Case1に切り替え
    /// </summary>
    public void ChangeCase1()
    {
        m_keyState = KeyState.Case1;
        foreach (KeyData keyData in m_keydatas)
        {
            keyData.SetCase1();
        }
    }

    /// <summary>
    /// Case2に切り替え
    /// </summary>
    public void ChangeCase2()
    {
        m_keyState = KeyState.Case2;
        foreach (KeyData keyData in m_keydatas)
        {
            keyData.SetCase2();
        }
    }

    /// <summary>
    /// 選択方向を移動
    /// </summary>
    private void SelectDirection(SelectionDirection direction)
    {
        if (!m_isActive) return;

        // 次の選択対象を取得
        var next = m_selectData.SelectionGeneric(direction);
        if (next == null || next == m_selectData) return;

        // 選択を切り替え
        m_selectData.Deselect(next);
        next.Select(m_selectData);
        m_selectData = next;
    }

    /// <summary>
    /// 選択中のキーを実行
    /// </summary>
    private void ExecuteSelect()
    {
        switch (m_selectData.KeyType)
        {
            case KeyData.Type.Character:
                AddCharacter();
                break;
            case KeyData.Type.Delete:
                m_keyDataManager.RemoveText();
                break;
            case KeyData.Type.Space:
                m_keyDataManager.AddText(' ');
                break;
            case KeyData.Type.Change:
                ToggleCase();
                break;
            case KeyData.Type.Cycle:
                m_keyDataManager.CycleCharacter();
                break;
        }
    }

    /// <summary>
    /// 現在のケースに応じて文字を追加
    /// </summary>
    private void AddCharacter()
    {
        if (m_keyState == KeyState.Case1)
        {
            m_keyDataManager.AddText(m_selectData.GetCase1());
        }
        else
        {
            m_keyDataManager.AddText(m_selectData.GetCase2());
        }
    }

    /// <summary>
    /// ケースを切り替え
    /// </summary>
    private void ToggleCase()
    {
        if (m_keyState == KeyState.Case1)
        {
            m_selectData.SetString("あ/a");
            ChangeCase2();
        }
        else
        {
            m_selectData.SetString("ア/A");
            ChangeCase1();
        }
    }

    #region Repeat Control

    /// <summary>
    /// リピート開始
    /// </summary>
    private void StartRepeat(SelectionDirection direction)
    {
        StopRepeat();
        m_repeatCoroutine = StartCoroutine(RepeatCoroutine(direction));
    }

    /// <summary>
    /// リピート停止
    /// </summary>
    private void StopRepeat()
    {
        if (m_repeatCoroutine != null)
        {
            StopCoroutine(m_repeatCoroutine);
            m_repeatCoroutine = null;
        }
    }

    /// <summary>
    /// リピート処理コルーチン
    /// </summary>
    private IEnumerator RepeatCoroutine(SelectionDirection direction)
    {
        // 最初の1回目を即座に実行
        SelectDirection(direction);

        // 初回ディレイ
        yield return new WaitForSeconds(m_repeatDelay);

        // リピート
        while (true)
        {
            SelectDirection(direction);
            yield return new WaitForSeconds(m_repeatInterval);
        }
    }

    #endregion

    #region Input Event Handlers

    private void OnMoveUpStarted(InputAction.CallbackContext ctx)
    {
        if (IsActive) StartRepeat(SelectionDirection.Up);
    }

    private void OnMoveDownStarted(InputAction.CallbackContext ctx)
    {
        if (IsActive) StartRepeat(SelectionDirection.Down);
    }

    private void OnMoveLeftStarted(InputAction.CallbackContext ctx)
    {
        if (IsActive) StartRepeat(SelectionDirection.Left);
    }

    private void OnMoveRightStarted(InputAction.CallbackContext ctx)
    {
        if (IsActive) StartRepeat(SelectionDirection.Right);
    }

    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        StopRepeat();
    }

    private void OnSelect(InputAction.CallbackContext ctx)
    {
        if (IsActive) ExecuteSelect();
    }

    private void OnDelete(InputAction.CallbackContext ctx)
    {
        if (IsActive) m_keyDataManager.RemoveText();
    }

    private void OnSpace(InputAction.CallbackContext ctx)
    {
        if (IsActive) m_keyDataManager.AddText(' ');
    }

    private void OnChange(InputAction.CallbackContext ctx)
    {
        if (IsActive) ToggleCase();
    }

    private void OnCycle(InputAction.CallbackContext ctx)
    {
        if (IsActive) m_keyDataManager.CycleCharacter();
    }

    #endregion
}
