using Unity.VisualScripting;
using UnityEngine;

public class SettingConfig : UIWindowBase
{
    //インプット
    InputGame m_input;
    //アクティブ化
    bool m_isActive = false;
    [SerializeField] UIWindowBase m_nextWindow;
    [SerializeField] UIWindowBase m_backWindow;

    [SerializeField] UISelecterBase m_selectUI;

    public override void Initialize(InputGame input)
    {
        m_input = input;

        m_input.Normal.Up.started += ctx => InputDirection(SelectionDirection.Up);
        m_input.Normal.Down.started += ctx => InputDirection(SelectionDirection.Down);

        m_input.Normal.Left.started += ctx => Operation(SelectionDirection.Left);
        m_input.Normal.Right.started += ctx => Operation(SelectionDirection.Right);

        m_input.Normal.Select.started += ctx => ButtonSelect();

        if (m_selectUI) m_selectUI.Select(null);
    }

    /// <summary>
    /// 開いたとき
    /// </summary>
    /// <param name="backWindow"></param>
    public override void Open(UIWindowBase backWindow)
    {
        m_isActive = true;
        gameObject.SetActive(true);
        if (m_selectUI) m_selectUI.Select(null);
    }

    /// <summary>
    /// 閉じたとき
    /// </summary>
    /// <param name="nextWindow"></param>
    public override void Close(UIWindowBase nextWindow)
    {
        m_isActive = false;
        if (m_selectUI) m_selectUI.Deselect(null);
        if (nextWindow) gameObject.SetActive(false);
    }

    /// <summary>
    /// 選択
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public override UIWindowBase Selection(SelectionDirection direction)
    {
        //選択ボタンがロックをかけた板ラパス
        if(m_selectUI)
        {
            if (m_selectUI.IsLock()) return this;
        }

        switch (direction)
        {
            case SelectionDirection.Left:
                if (m_backWindow) return m_backWindow;
                return this;
            case SelectionDirection.Right:
                if (m_nextWindow) return m_nextWindow;
                return this;
            default:
                return this;

        }
    }


    /// <summary>
    /// 操作ボタン選択
    /// </summary>
    /// <param name="direction"></param>
    private void InputDirection(SelectionDirection direction)
    {
        if (m_selectUI == null | !m_isActive) return;

        var next = m_selectUI.Selection(direction);
        if (next == m_selectUI || next == null) return;
      
        m_selectUI.Deselect(next);
        next.Select(m_selectUI);
        m_selectUI = next;
        
    }

    void Operation(SelectionDirection direction)
    {
        if(m_selectUI)
        {
            m_selectUI.Operation(direction);
        }
    }

    /// <summary>
    /// 決定
    /// </summary>
    private void ButtonSelect()
    {
        if (m_selectUI == null | !m_isActive) return;
        m_selectUI.Decision();
    }
}
