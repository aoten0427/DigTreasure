using UnityEngine;
using DG.Tweening;

public class JoinButtonView : UISelecterBase
{
    [SerializeField] float m_maxSize = 1.1f;
    [SerializeField] float m_animationDuration = 0.5f;
    [SerializeField] Ease m_easeType = Ease.InOutSine;

    Vector3 m_initialScale;
    Tween m_scaleTween;

    [SerializeField]EntranceRoomData m_roomData;

    [SerializeField] UISelecterBase m_upSelect;
    [SerializeField] UISelecterBase m_downSelect;
    [SerializeField] UISelecterBase m_leftSelect;
    [SerializeField] UISelecterBase m_rightSelect;

    private void Awake()
    {
        m_initialScale = transform.localScale;
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Alpha7))
        {
            Select(null);
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            Deselect(null);
        }
    }

    public override void Select(UISelecterBase back)
    {
        base.Select(back);

        KillTween();

        m_scaleTween = transform
            .DOScale(m_initialScale * m_maxSize, m_animationDuration)
            .SetEase(m_easeType)
            .SetLoops(-1, LoopType.Yoyo);
    }

    public override void Deselect(UISelecterBase next)
    {
        base.Deselect(next);

        KillTween();

        m_scaleTween = transform
            .DOScale(m_initialScale, m_animationDuration * 0.5f)
            .SetEase(m_easeType);
    }

    void KillTween()
    {
        if (m_scaleTween != null && m_scaleTween.IsActive())
        {
            m_scaleTween.Kill();
            m_scaleTween = null;
        }
    }

    void OnDestroy()
    {
        KillTween();
    }

    public override void Decision()
    {
        m_roomData.JoinRoom();
    }

    public override UISelecterBase Selection(SelectionDirection direction)
    {
        switch (direction)
        {
            case SelectionDirection.Up:
                if (m_upSelect != null) return m_upSelect;
                return this;
            case SelectionDirection.Down:
                if (m_downSelect != null) return m_downSelect;
                return this;
            case SelectionDirection.Left:
                if (m_leftSelect != null) return m_leftSelect;
                return this;
            case SelectionDirection.Right:
                if (m_rightSelect != null) return m_rightSelect;
                return this;
            default:
                return this;
        }
    }
}