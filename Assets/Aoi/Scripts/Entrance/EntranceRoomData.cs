using Fusion;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EntranceRoomData : MonoBehaviour
{
    //参加するルームの名前
    [SerializeField] string m_roomName;
    //マネジャー
    [SerializeField]EntranceManager m_manager;
    //参加人数表示文字列
    [SerializeField] TextMeshProUGUI m_joindCount;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(m_manager == null)
        {
            m_manager = FindFirstObjectByType<EntranceManager>();
        }
        if (m_manager == null) Debug.LogError("[EntranceRoomData]マネジャーがありません", gameObject);

        m_manager.OnSessionUpdate += UpdateRoomData;
    }

    /// <summary>
    /// ルームへ参加(ボタン用)
    /// </summary>
    public void JoinRoom()
    {
        m_manager.JoinRoom(m_roomName);
    }

    /// <summary>
    /// セッションデータを更新し参加人数を更新
    /// </summary>
    /// <param name="data"></param>
    private void UpdateRoomData(Dictionary<string, SessionInfo> data)
    {
        string text = "";
        if(data.TryGetValue(m_roomName, out SessionInfo info))
        {
            text = $"{info.PlayerCount}/{info.MaxPlayers}";
        }
        else
        {
            text = $"0/4";
        }

        m_joindCount.text = text ;
    }
}
