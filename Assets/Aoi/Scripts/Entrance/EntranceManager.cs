using Fusion;
using NetWork;
using Option;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Unity.Collections.Unicode;

/// <summary>
/// 入室シーンの管理
/// </summary>
public class EntranceManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tmproUGUI;//ユーザー名
    private NetWork.GameLauncher m_gameLauncher;
    bool m_isConnecting = false;
    //接続時イベント
    public event Action<bool> OnConnectAction;
    //セッション更新イベント
    bool m_isSession =false;
    public event Action<Dictionary<string, SessionInfo>> OnSessionUpdate;
    [SerializeField] AudioSource m_bgm;

    [SerializeField] EntranceInput m_input;
    OptionManager m_optionManager;

    [SerializeField] CircleButton m_backButton;

    private void Start()
    {
        //ゲームランチャーを探す
        m_gameLauncher = NetWork.GameLauncher.Instance;
        m_gameLauncher.OnPlayerJoined += MoveToRoom;
        m_bgm.Play();

        m_optionManager = OptionManager.Instance;

        if(m_input)
        {
            m_input.OnPause += OpenOption;
            m_input.OnCancel += CancelPush;
        }

        
    }

    private void OnDestroy()
    {
        m_bgm.Stop();
        if (m_input)
        {
            m_input.OnPause -= OpenOption;
            m_input.OnCancel -= CancelPush;
        }
    }

    /// <summary>
    /// ルームへ参加
    /// </summary>
    /// <param name="roomName"></param>
    public  async void JoinRoom(string roomName)
    {
        if (!(tmproUGUI.text.Length > 1)) return;
        if(m_isConnecting) return;

        m_isConnecting = true;
        Connecting();

        var userdata = m_gameLauncher.UserData;
        userdata.m_name = tmproUGUI.text;
        m_gameLauncher.UserData = userdata;


        m_isConnecting = await m_gameLauncher.JoinRoom(roomName);
        //接続失敗
        if(!m_isConnecting)
        {
            FaielConnect();
        }
        
    }

    /// <summary>
    /// 接続中
    /// </summary>
    void Connecting()
    {
        OnConnectAction?.Invoke(m_isConnecting);
    }

    /// <summary>
    /// 接続失敗用
    /// </summary>
    void FaielConnect()
    {
        OnConnectAction?.Invoke(false);
    }


    /// <summary>
    /// 待機シーンへ移動
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="player"></param>
    private void MoveToRoom(NetworkRunner runner, PlayerRef player)
    {
        if (runner.LocalPlayer != player) return;

        Debug.Log("呼び出し");

        //待機ルームへ移動
        if (runner.IsSceneAuthority)
        {
            runner.LoadScene(SceneRef.FromIndex(Config.ROOM_SCENE_NUMBER), LoadSceneMode.Single);
        }
    }


    /// <summary>
    /// ルームデータ更新
    /// </summary>
    public async void SesstionUpdateAsync()
    {
        //セッションデータを更新
        await m_gameLauncher.UpdateSessions();
        //更新データを取得
        var data = m_gameLauncher.GetSessionInfo();
        //反映
        OnSessionUpdate?.Invoke(data);
    }

    private void OpenOption(bool push)
    {
        if (m_optionManager != null) m_optionManager.Open();
    }

    private void CancelPush(bool push)
    {
        //オプション処理
        if (m_optionManager != null&&IsOpenOption())
        {
            m_optionManager.Close();
            return;
        }

        ChangeScene();
    }

    public bool IsOpenOption()
    {
        if (m_optionManager == null) return false;
        return m_optionManager.IsActive;
    }

    private void ChangeScene()
    {
        var fade = FadeManager.instance;
        fade.ChangeScene("0_Title");
    }
}
