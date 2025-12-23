using DG.Tweening;
using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SessionUpdate : MonoBehaviour
{

    [SerializeField] EntranceManager m_manager;
    [SerializeField] RectTransform m_recttransform;
    [SerializeField]InfomationText m_infotext;
    Tween m_tween;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (m_manager == null) m_manager = FindFirstObjectByType<EntranceManager>();
        m_manager.OnSessionUpdate += UpdateRoomData;
        
    }

    public void UpdateSession()
    {
        m_manager.SesstionUpdate();
        m_tween = m_recttransform.DORotate(new Vector3(0f, 0f, -360f), 1.0f, RotateMode.FastBeyond360)
            .SetEase(Ease.InOutCubic)
            .SetLoops(-1, LoopType.Restart);
        m_infotext.UpdateText("‚±‚¤‚µ‚ñ‚¿‚ã‚¤",true);
    }

    private void UpdateRoomData(Dictionary<string, SessionInfo> data)
    {
        
        m_tween.Kill();
        m_recttransform.Rotate(Vector3.zero);
        m_infotext.Hide();
    }
}
