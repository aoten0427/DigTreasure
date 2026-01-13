using Fusion;
using NetWork;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// リザルトデータを集計
/// </summary>
public class ResultManager : NetworkBehaviour
{
    //自身のユーザーデータ
    NetWork.NetworkUserData m_uesrData;
    NetWork.GameLauncher m_gameLauncher;

    RoomManager m_roomManager;

    //全体のリザルトデータ
    [Networked, Capacity(4)]
    private NetworkDictionary<PlayerRef, ResultData> resultData => default;
    //ランキング表示クラス
    [SerializeField] Ranking m_ranking;
    [SerializeField] bool m_islog = false;

    bool m_isSelectButton = false;

    //閉じる際のイベント
    public event Action OnClose;

    private void Start()
    {
        m_gameLauncher = GameLauncher.Instance;
    }

    

    public override void Spawned()
    {
        if(m_ranking == null)
        {
            Debug.LogWarning("表示用ランキングがありません");
        }



        StartCoroutine(LoadRoomSceneAndInitialize());

        //ユーザーデータを取得しリザルトデータに反映
        m_uesrData = m_gameLauncher.UserData;
        ResultData data = new ResultData();
        data.NickName = m_uesrData.m_name;
        data.TreasureScore = m_uesrData.m_treasurePoint;
        data.TreasureCount = m_uesrData.m_treasureCount;
        data.DigPoint = m_uesrData.m_digPoint;
        data.Index = m_uesrData.m_colorID;
        Debug.Log($"堀ポイント{data.DigPoint}");

        //状態権限を持つ人のみイベントを設定
        if(Object.HasStateAuthority)
        {
            m_gameLauncher.AddOnAllUserReady(RPC_SentData);
        }
        

        //リザルトデータを全体更新
        RPC_SetResultData(Runner.LocalPlayer, data);

        
    }

    /// <summary>
    /// ROOMシーンをロードしてコンポーネントを初期化する
    /// </summary>
    private IEnumerator LoadRoomSceneAndInitialize()
    {

        if(Object.HasStateAuthority)
        {
            // アンロード
            Runner.UnloadScene(SceneRef.FromIndex(Config.ROOM_SCENE_NUMBER));
            yield return new WaitForSeconds(0.5f);
            // ロード
            Runner.LoadScene(SceneRef.FromIndex(Config.ROOM_SCENE_NUMBER), LoadSceneMode.Additive);
        }
       

        RoomManager target = null;
        yield return new WaitUntil(() => (target = FindFirstObjectByType<RoomManager>()) != null);
        m_roomManager = target;
        m_roomManager.ActiveOff();

        Debug.Log("見つけました");
    
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        m_gameLauncher.RemoveOnAllUserReady(RPC_SentData);
    }

    private async void Update()
    {
        if(Input.GetKeyUp(KeyCode.Escape))
        {
            await m_gameLauncher.JoinRoom("Room1");
        }
    }

    /// <summary>
    /// リザルトデータを全体更新
    /// </summary>
    /// <param name="user"></param>
    /// <param name="data"></param>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetResultData(PlayerRef user,ResultData data)
    {
        if(m_islog)Debug.Log("データを追加"+ user);
        resultData.Set(user, data);
    }

    /// <summary>
    /// データをローカルに送信
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_SentData()
    {
        if (m_ranking == null) return;
        StartCoroutine(WatiUserData());
    }

    /// <summary>
    /// 全てのユーザーデータが届くまで待つ
    /// </summary>
    /// <returns></returns>
    IEnumerator WatiUserData()
    {
        yield return new WaitUntil(() => resultData.Count >= Runner.ActivePlayers.Count());
        yield return new WaitForSeconds(0.5f);
        SendData();
        
    }

    /// <summary>
    /// データを送信
    /// </summary>
    private void SendData()
    {
        if(m_islog)Debug.Log("データ数:" + resultData.Count);

        List<ResultData> datas = new List<ResultData>();

        //データを移し替え
        foreach (var userdata in resultData)
        {
            ResultData data = new ResultData();
            data.NickName = userdata.Value.NickName;
            data.TreasureScore = userdata.Value.TreasureScore;
            data.DigPoint = userdata.Value.DigPoint;
            data.TreasureCount = userdata.Value.TreasureCount;
            data.Index = userdata.Value.Index;
            data.IsSelf = userdata.Key == Runner.LocalPlayer;
            datas.Add(data);
        }


        //ここでフェードを開く
        var fade = FadeManager.instance;
        if(fade)
        {
            fade.FadeOut();
        }

        m_ranking.ShowRanking(datas, PerformanceEnd);

        var roomcamera = FindFirstObjectByType<RoomCamera>();
        if (roomcamera != null)
        {
            roomcamera.gameObject.SetActive(false);
        }

        
    }


    private void PerformanceEnd()
    {
        m_isSelectButton = true;
    }


    /// <summary>
    /// ルームから抜ける
    /// </summary>
    public async void Exit()
    {
        if (!m_isSelectButton) return;
        m_isSelectButton = false;

        await m_gameLauncher.LeaveRoom();
        SceneManager.LoadScene(Config.ENTRANCE_SCENE_NUMBER, LoadSceneMode.Single);
    }

    /// <summary>
    /// ルームに戻る
    /// </summary>
    public void PlayAgain()
    {
        if (!m_isSelectButton) return;
        m_isSelectButton = false;

        var fade = FadeManager.instance;

        if(fade)
        {
            fade.OnFadeInEnd += CloseResult;
            fade.OnFadeInEnd += () => fade.FadeOut("none");
            fade.FadeIn();
        }
        else
        {
            CloseResult();
        }
        
    }

    private void CloseResult()
    {
        OnClose?.Invoke();
        if (m_roomManager)
        {
            m_roomManager.ActiveOn();
        }
    }
}
