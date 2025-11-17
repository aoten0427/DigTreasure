using Fusion;
using NetWork;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 参加できるセッション管理
/// </summary>
public class SessionList
{
    private GameLauncher m_gameLauncher;
    private NetworkRunner m_sessionListRunner;
    private Dictionary<string, SessionInfo> m_availableSessions = new Dictionary<string, SessionInfo>();
    private bool m_isUpdatingSession = false;
    private bool m_cancelUpdate = false;

    public Dictionary<string, SessionInfo> AvailableSessions => m_availableSessions;

    public SessionList(GameLauncher gameLauncher)
    {
        m_gameLauncher = gameLauncher;
        m_gameLauncher.OnSessionListUpdated += OnSessionListUpdated;
    }

    ~SessionList()
    {
        if(m_gameLauncher != null) m_gameLauncher.OnSessionListUpdated -= OnSessionListUpdated;
    }

    /// <summary>
    /// セッションを更新
    /// </summary>
    public async Task UpdateSessions()
    {
        if (m_isUpdatingSession) return;

        m_isUpdatingSession = true;
        m_cancelUpdate = false;
        m_availableSessions.Clear();

        // 既存Runnerを安全に破棄
        await SafeShutdownRunner();

        if (m_cancelUpdate)
        {
            m_isUpdatingSession = false;
            return;
        }

        // Runner作成
        GameObject sessionObj = new GameObject("SessionListRunner");
        m_sessionListRunner = sessionObj.AddComponent<NetworkRunner>();
        m_sessionListRunner.AddCallbacks(m_gameLauncher);

        await Task.Delay(100);
        if (m_cancelUpdate) { await SafeShutdownRunner(); m_isUpdatingSession = false; return; }

        // ロビー参加
        var result = await m_sessionListRunner.JoinSessionLobby(SessionLobby.Shared);
        if (m_cancelUpdate) { await SafeShutdownRunner(); m_isUpdatingSession = false; return; }

        // 情報更新のための待機
        int waitMs = 0;
        while (waitMs < 2000 && !m_cancelUpdate)
        {
            await Task.Delay(100);
            waitMs += 100;
        }

        // 閉じる
        await SafeShutdownRunner();

        m_isUpdatingSession = false;
    }

    /// <summary>
    /// セッションリスト更新コールバック
    /// </summary>
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        if (m_cancelUpdate) return;

        m_availableSessions.Clear();
        foreach (var session in sessionList)
        {
            m_availableSessions[session.Name] = session;
        }
    }

    /// <summary>
    /// 特定のセッション情報を取得
    /// </summary>
    public SessionInfo GetSessionInfo(string roomName)
    {
        if (m_availableSessions.ContainsKey(roomName))
        {
            return m_availableSessions[roomName];
        }
        return null;
    }

    /// <summary>
    /// 明示的な切断要求
    /// </summary>
    public async Task Disconnect()
    {
        Debug.Log("切断要求を検知");
        m_cancelUpdate = true;
        await SafeShutdownRunner();
        Debug.Log("切断完了");
    }

    /// <summary>
    /// Runnerを安全に停止・破棄
    /// </summary>
    private async Task SafeShutdownRunner()
    {
        if (m_sessionListRunner != null)
        {
            
            if (m_sessionListRunner.IsRunning)
            {
                await m_sessionListRunner.Shutdown();
            }
            

            Object.Destroy(m_sessionListRunner.gameObject);
            m_sessionListRunner = null;
        }
    }
}
