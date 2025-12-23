using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DotLoadingAnimation : MonoBehaviour
{
    [Header("Dots")]
    [SerializeField] private List<RectTransform> m_dots = new List<RectTransform>();

    [Header("Animation Settings")]
    [SerializeField] private float m_moveHeight = 20f;
    [SerializeField] private float m_oneDotDuration = 0.5f;
    [SerializeField] private float m_startInterval = 0.15f;

    [Header("Tween")]
    [SerializeField] private Ease m_easeUp = Ease.OutQuad;
    [SerializeField] private Ease m_easeDown = Ease.InQuad;
    [SerializeField] private bool m_loop = true;

    private Sequence m_sequence;

    void Start()
    {
        Play();
    }

    public void Play()
    {
        m_sequence?.Kill();
        m_sequence = DOTween.Sequence();

        foreach (var dot in m_dots)
        {
            Vector2 basePos = dot.anchoredPosition;

            Sequence dotSeq = DOTween.Sequence();
            dotSeq.Append(
                dot.DOAnchorPosY(basePos.y + m_moveHeight, m_oneDotDuration * 0.5f)
                   .SetEase(m_easeUp)
            );
            dotSeq.Append(
                dot.DOAnchorPosY(basePos.y, m_oneDotDuration * 0.5f)
                   .SetEase(m_easeDown)
            );

            m_sequence.Append(dotSeq);
            m_sequence.AppendInterval(m_startInterval);
        }

        if (m_loop)
        {
            m_sequence.SetLoops(-1);
        }
    }

    private void OnDestroy()
    {
        m_sequence?.Kill();
    }
}
