using DG.Tweening;
using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SessionUpdate : MonoBehaviour
{

    [SerializeField] EntranceManager m_manager;
    [SerializeField]InfomationText m_infotext;
    [SerializeField]EntranceInput m_input;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (m_manager == null) m_manager = FindFirstObjectByType<EntranceManager>();
        m_manager.OnSessionUpdate += UpdateRoomData;

        m_input.OnLoad += UpdateSession;
    }

    private void OnDestroy()
    {
        if(m_input)
        {
            m_input.OnLoad -= UpdateSession;
        }
    }

    public void UpdateSession(bool push)
    {
        m_manager.SesstionUpdateAsync();
        m_infotext.UpdateText("‚±‚¤‚µ‚ñ‚¿‚ã‚¤",true);
    }

    private void UpdateRoomData(Dictionary<string, SessionInfo> data)
    {
        m_infotext.Hide();
    }
}
