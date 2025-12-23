using UnityEngine;

public class SoundConfig : UIWindowBase
{
    //インプット
    InputGame m_input;
    //アクティブ化
    bool m_isActive = false;
    [SerializeField]
    private UISelecterBase m_selectUI;

    [SerializeField]UIWindowBase m_nextWindow;
    [SerializeField] UIWindowBase m_backWindow;

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="input"></param>
    public override void Initialize(InputGame input)
    {
        Close(null);
        m_input = input;
        m_input.Normal.Up.started += ctx => InputDirection(SelectionDirection.Up);
        m_input.Normal.Down.started += ctx => InputDirection(SelectionDirection.Down);
        m_input.Normal.Left.started += ctx => InputDirection(SelectionDirection.Left);
        m_input.Normal.Right.started += ctx => InputDirection(SelectionDirection.Right);

        m_input.Normal.Left.performed += ctx => SoundVolumeChange(SelectionDirection.Left);
        m_input.Normal.Right.performed += ctx => SoundVolumeChange(SelectionDirection.Right);
        m_input.Normal.Left.canceled += ctx => SoundVolumeChange(SelectionDirection.None);
        m_input.Normal.Right.canceled += ctx => SoundVolumeChange(SelectionDirection.None);

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
        if(nextWindow)gameObject.SetActive(false);
    }

    /// <summary>
    /// 選択
    /// </summary>
    /// <param name="direction"></param>
    /// <returns></returns>
    public override UIWindowBase Selection(SelectionDirection direction)
    {
        switch (direction)
        {
            case SelectionDirection.Left:
                if(m_backWindow)return m_backWindow;
                return this;
            case SelectionDirection.Right:
                if(m_nextWindow)return m_nextWindow;
                return this;
            default:
                return this;

        }
    }


    /// <summary>
    /// サウンド音量変更
    /// </summary>
    /// <param name="direction"></param>
    private void SoundVolumeChange(SelectionDirection direction)
    {
        if (!m_isActive) return;
        if(m_selectUI)
        {
            m_selectUI.Operation(direction);
        }
    }

    /// <summary>
    /// 選択変更
    /// </summary>
    /// <param name="direction"></param>
    private void InputDirection(SelectionDirection direction)
    {
        if(!m_isActive) return;
       var next = m_selectUI.Selection(direction);
        if (next == null && next == m_selectUI) return;
        m_selectUI.Deselect(next);
        next.Select(m_selectUI);
        m_selectUI = next;
    }
}
