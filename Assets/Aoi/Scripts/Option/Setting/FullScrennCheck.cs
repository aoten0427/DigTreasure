using UnityEngine;
using UnityEngine.UI;

public class FullScrennCheck : UISelecterBase
{
    bool m_isFullScrenn = false;
    Vector2Int m_windowSize = new Vector2Int(1920, 1080);

    [SerializeField] Image m_checkMark;
    [SerializeField] OptionSelectButtonView m_view;

    [SerializeField] UISelecterBase m_upSelect;
    [SerializeField] UISelecterBase m_downSelect;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public override UISelecterBase Selection(SelectionDirection direction)
    {
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

    public override void Decision()
    {
        //フルスクリーン状態を反転
        m_isFullScrenn = !m_isFullScrenn;
        if(m_isFullScrenn )
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            
        }else
        {
            Screen.SetResolution(m_windowSize.x,m_windowSize.y, FullScreenMode.Windowed);
        }
        if (m_checkMark) m_checkMark.enabled = m_isFullScrenn;
    }

    public override void Select(UISelecterBase back)
    {
        m_view.Select(true);
    }

    public override void Deselect(UISelecterBase next)
    {
        m_view.Select(false);
    }

    public void SetWindowSize(int width, int height)
    {
        m_windowSize = new Vector2Int(width, height);
        if(!m_isFullScrenn )
        {
            Screen.SetResolution(m_windowSize.x, m_windowSize.y, FullScreenMode.Windowed);
        }
    }
}
