using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Resolution : UISelecterBase
{
    [SerializeField] FullScrennCheck m_fullscreenChaecker;


    [SerializeField] OptionSelectButtonView m_view;
    [SerializeField] UISelecterBase m_upSelect;
    [SerializeField] UISelecterBase m_downSelect;

    [SerializeField] List<Vector2Int> m_resolutions;
    [SerializeField] List<Image> m_resolutionImages;
    [SerializeField] List<float> m_pivotX;
    int m_currentIndex = 0;
    int m_selectIndex = 0;

    [SerializeField] RectTransform m_point;

    private void Awake()
    {
        Decision();
        SelectResolution(m_currentIndex);
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

    public override void Select(UISelecterBase back)
    {
        m_view.Select(true);
        m_point.gameObject.SetActive(true);
        SelectResolution(m_currentIndex);
    }

    public override void Deselect(UISelecterBase next)
    {
        m_view.Select(false);
        m_point.gameObject.SetActive(false);
    }

    public override void Decision()
    {
        m_resolutionImages[m_currentIndex].enabled = false;
        m_currentIndex = m_selectIndex;
        m_resolutionImages[m_currentIndex].enabled = true;
        var size = m_resolutions[m_currentIndex];
        m_fullscreenChaecker.SetWindowSize(size.x, size.y);
    }

    public override void Operation(SelectionDirection direction)
    {
        if(direction == SelectionDirection.Left)
        {
            SelectResolution(m_selectIndex - 1);
        }else if (direction == SelectionDirection.Right)
        {
            SelectResolution(m_selectIndex + 1);
        }
    }

    void SelectResolution(int index)
    {
        if (index < 0 || index >= m_resolutions.Count) return;
        m_selectIndex = index;
        var pivot = m_point.pivot;
        pivot.x = m_pivotX[m_selectIndex];
        m_point.pivot = pivot;
    }
}
