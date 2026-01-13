using UnityEngine;
using DG.Tweening;

public class Credit : MonoBehaviour
{
    [SerializeField] private RectTransform m_rectTransform;
    [SerializeField] private float m_duration = 0.3f;
    [SerializeField] private Ease m_openEase = Ease.OutBack;
    [SerializeField] private Ease m_closeEase = Ease.InBack;

    private bool m_isOpen = false;
    public bool isOpen => m_isOpen;

    private Tween m_currentTween;

    private void Awake()
    {
        // ‰Šúó‘Ô‚Í•Â‚¶‚½ó‘Ô
        m_rectTransform.localScale = Vector3.zero;
    }

    public void Open()
    {
        KillCurrentTween();
        m_isOpen = true;
        m_currentTween = m_rectTransform
            .DOScale(Vector3.one, m_duration)
            .SetEase(m_openEase)
            .SetUpdate(true);
    }

    public void Close()
    {
        KillCurrentTween();
        m_isOpen = false;
        m_currentTween = m_rectTransform
            .DOScale(Vector3.zero, m_duration)
            .SetEase(m_closeEase)
            .SetUpdate(true);
    }

    private void KillCurrentTween()
    {
        if (m_currentTween != null && m_currentTween.IsActive())
        {
            m_currentTween.Kill();
        }
    }

    private void OnDestroy()
    {
        KillCurrentTween();
    }
}