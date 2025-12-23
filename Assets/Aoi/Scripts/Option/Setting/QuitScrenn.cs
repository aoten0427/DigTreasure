using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class QuitScrenn : MonoBehaviour
{
    RectTransform m_rectTransform;
    //ŠJ‚¢‚Ä‚¢‚é‚©
    bool m_isOpen = false;
    //ŠJ‚­ŽžŠÔ
    [SerializeField] float m_durationTime = 0.5f;

    //•Â‚¶‚é‚©
    bool m_isQuit = false;
    [SerializeField] Image m_yesImage;
    [SerializeField] Image m_noImage;



    public bool isOpen { get { return m_isOpen;} }

    private void Awake()
    {
        m_rectTransform = GetComponent<RectTransform>();
    }

    public void Open()
    {
        if (m_isOpen) return;
        m_rectTransform.DOScale(Vector3.one, m_durationTime).SetEase(Ease.OutQuad);
        m_isOpen = true;
        m_isQuit = false;
        m_noImage.enabled = true;
    }

    public void Close()
    {
        if(!m_isOpen) return;
        m_rectTransform.DOScale(Vector3.zero, m_durationTime).SetEase(Ease.InQuad);
        m_isOpen = false;
    }

    public void Decision()
    {
        if(!m_isOpen) return;
        if(m_isQuit)
        {
            QuitGame();
        }
        else
        {
            Close();
        }
    }

    public void Operation(SelectionDirection direction)
    {
        if (!m_isOpen) return;
        if(direction == SelectionDirection.Left)
        {
            SelectYes();
        }
        else if(direction == SelectionDirection.Right)
        {
            SelectNo();
        }
    }

    private void SelectYes()
    {
        m_isQuit = true;
        m_yesImage.enabled = true;
        m_noImage.enabled = false;
    }

    private void SelectNo()
    {
        m_isQuit = false;
        m_yesImage.enabled = false;
        m_noImage.enabled = true;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}
