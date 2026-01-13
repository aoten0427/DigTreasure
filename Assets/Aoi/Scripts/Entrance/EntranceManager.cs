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

    //インプット
    [SerializeField] EntranceInput m_input;

    //オプション
    OptionManager m_optionManager;
    //名前入力
    [SerializeField]NameInput m_nameInput;

    //タイトルバックボタン
    [SerializeField] CircleButton m_backButton;
    bool m_isBack = false;

    //選択ボタン
    [SerializeField] UISelecterBase m_selectButton;



    private void Start()
    {
        //ゲームランチャーを探す
        m_gameLauncher = NetWork.GameLauncher.Instance;
        m_gameLauncher.OnPlayerJoined += MoveToRoom;
       
        //音楽再生
        var soundPlayer = SoundPlayer.Instance;
        soundPlayer.PlayBGM(BGMType.Title);

        m_optionManager = OptionManager.Instance;

        m_selectButton.Select(null);

        if(m_input)
        {
            m_input.OnPause += OpenOption;
            m_input.OnCancel += CancelPush;
            m_input.OnMove += SelectMove;
            m_input.OnNameChange += OpenNameChange;
            m_input.OnSelect += Select;
        }

        m_backButton.OnFillAction = ChangeTitleScene;
    }

    private void OnDestroy()
    {
        if (m_input)
        {
            m_input.OnPause -= OpenOption;
            m_input.OnCancel -= CancelPush;
            m_input.OnMove -= SelectMove;
            m_input.OnNameChange -= OpenNameChange;
            m_input.OnSelect -= Select;
        }

        if(m_backButton)
        {
            m_backButton.OnFillAction = null;
        }
    }

    /// <summary>
    /// ルームへ参加
    /// </summary>
    /// <param name="roomName"></param>
    public  async void JoinRoom(string roomName)
    {
        if (tmproUGUI.text.Length == 0) return;
        if (m_isConnecting) return;

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

    /// <summary>
    /// オプション開く
    /// </summary>
    /// <param name="push"></param>
    private void OpenOption(bool push)
    {
        if (!push) return;
        if (IsOpenNameChange() || m_isBack||IsOpenOption()) return;
        if (m_optionManager != null) m_optionManager.Open();
    }

    /// <summary>
    /// オプション閉じる　タイトルに戻る
    /// </summary>
    /// <param name="push"></param>
    private void CancelPush(bool push)
    {
        if (IsOpenNameChange() || m_isBack) return;
        //オプション処理
        if (m_optionManager != null&&IsOpenOption())
        {
            m_optionManager.Close();
            return;
        }

        m_backButton.OnUpdate(push);

    }

    /// <summary>
    /// オプションが開いているか
    /// </summary>
    /// <returns></returns>
    public bool IsOpenOption()
    {
        if (m_optionManager == null) return false;
        return m_optionManager.IsActive;
    }

    /// <summary>
    /// タイトルに戻る
    /// </summary>
    private void ChangeTitleScene()
    {
        m_isBack = true;
        var fade = FadeManager.instance;
        fade.ChangeScene("0_Title");
    }

    //選択ボタン移動
    private void SelectMove(SelectionDirection direction)
    {
        if (IsOpenOption() || IsOpenNameChange()||m_isBack) return;
        var next = m_selectButton.Selection(direction);
        if(next == null||next == m_selectButton) return;
        //選択肢変更
        m_selectButton.Deselect(next);
        next.Select(m_selectButton);
        m_selectButton = next;
        var sound = SoundPlayer.Instance;
        if (sound) sound.PlaySE(SEType.ButtonMove);
    }

    private void OpenNameChange(bool push)
    {
        if (!push) return;
        if (IsOpenOption() || m_isBack||IsOpenNameChange()) return;
        if(push)
        {
            m_nameInput.Open();
        }
    }

    private bool IsOpenNameChange()
    {
        if(m_nameInput == null) return false;
        return m_nameInput.IsOpen;
    }

    private void Select(bool push)
    {
        if (!push) return;
        if (IsOpenOption() || IsOpenNameChange() || m_isBack) return;
        if (m_selectButton) m_selectButton.Decision();
        var sound = SoundPlayer.Instance;
        if (sound) sound.PlaySE(SEType.ButtonClick);
    }
}
