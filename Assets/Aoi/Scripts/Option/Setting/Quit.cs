using UnityEngine;

public class Quit : UISelecterBase
{
    //見た目
    [SerializeField] OptionSelectButtonView m_view;
    //次のボタン
    [SerializeField] UISelecterBase m_upSelect;
    [SerializeField] UISelecterBase m_downSelect;

    //退出画面
    [SerializeField] QuitScrenn m_quitScrenn;

    public override UISelecterBase Selection(SelectionDirection direction)
    {
        //退出画面を開いている間はパス
        if (m_quitScrenn.isOpen) return null;

        switch (direction)
        {
            case SelectionDirection.Up:
                if (m_upSelect) return m_upSelect;
                return this;
            case SelectionDirection.Down:
                if (m_downSelect) return m_downSelect;
                return this;
            default:
                return this;
        }
    }

    public override void Select(UISelecterBase back)
    {
        m_view.Select(true);
    }

    public override void Deselect(UISelecterBase next)
    {
        m_view.Select(false);
        m_quitScrenn.Close();
    }

    /// <summary>
    /// ゲームをやめる画面へ
    /// </summary>
    public override void Decision()
    {
        //退出画面を開いている間はパス
        if (m_quitScrenn.isOpen)
        {
            m_quitScrenn.Decision();
        }else
        {
            m_quitScrenn.Open();
        }
        
    }

    public override void Operation(SelectionDirection direction)
    {
        if(m_quitScrenn.isOpen)
        {
            m_quitScrenn.Operation(direction);
        }
    }

    /// <summary>
    /// 退出画面を開いていたらロックをかける
    /// </summary>
    /// <returns></returns>
    public override bool IsLock()
    {
        return m_quitScrenn.isOpen;
    }
}
