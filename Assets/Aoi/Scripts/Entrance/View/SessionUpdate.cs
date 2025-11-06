using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SessionUpdate : MonoBehaviour
{
    [SerializeField] GameObject m_string;
    [SerializeField] EntranceManager m_manager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (m_manager == null) m_manager = FindFirstObjectByType<EntranceManager>();
        m_manager.OnSessionUpdate += UpdateRoomData;
        m_string.SetActive(false);
    }

    public void UpdateSession()
    {
        m_manager.SesstionUpdate();
        m_string.SetActive(true);
    }

    private void UpdateRoomData(Dictionary<string, SessionInfo> data)
    {
        if(m_string != null) m_string.SetActive(false);
    }
}
