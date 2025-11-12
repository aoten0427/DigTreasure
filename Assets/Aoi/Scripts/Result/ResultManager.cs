using Fusion;
using NetWork;
using NUnit.Framework;
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

    [SerializeField] GameObject m_resultCanvas;

    //全体のリザルトデータ
    [Networked, Capacity(4)]
    private NetworkDictionary<PlayerRef, ResultData> resultData => default;
    //ランキング表示クラス
    [SerializeField] Ranking m_ranking;
    [SerializeField] bool m_islog = false;

    // シーン再ロード処理中フラグ
    private bool isReloadingScene = false;


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

        //状態権限を持つ人のみイベントを設定
        if (Object.HasStateAuthority)
        {
            m_gameLauncher.OnPlayerJoined += OnJoinUser;
            Runner.UnloadScene(SceneRef.FromIndex(Config.ROOM_SCENE_NUMBER));
            Runner.LoadScene(SceneRef.FromIndex(Config.ROOM_SCENE_NUMBER), LoadSceneMode.Additive);

        }

        //ユーザーデータを取得しリザルトデータに反映
        m_uesrData = m_gameLauncher.UserData;
        if (!m_uesrData.m_isPlayData)
        {
            m_resultCanvas.SetActive(false);
            return;
        }
        ResultData data = new ResultData();
        data.NickName = m_uesrData.m_name;
        data.TreasureScore = m_uesrData.m_treasurePoint;
        data.TreasureCount = m_uesrData.m_treasureCount;

        //状態権限を持つ人のみイベントを設定
        if(Object.HasStateAuthority)
        {
            m_gameLauncher.AddOnAllUserReady(RPC_SentData);
        }
        

        //リザルトデータを全体更新
        RPC_SetResultData(Runner.LocalPlayer, data);


    }

    private void OnJoinUser(NetworkRunner runner,PlayerRef user)
    {
       
        //StartCoroutine(WaitForSpawnAndEntry());
        
    }

    private IEnumerator WaitForSpawnAndEntry()
    {
        // Spawnされるまで待つ
        yield return new WaitUntil(() => Runner != null && Object.IsValid);
        if (Object.HasStateAuthority)
        {
            // 既に再ロード処理中なら何もしない
            if (isReloadingScene) yield break;

            StartCoroutine(ReloadRoomScene());
        }
    }

    /// <summary>
    /// ROOMシーンをアンロード→ロードする
    /// </summary>
    private IEnumerator ReloadRoomScene()
    {
        isReloadingScene = true;

        // アンロード開始
        Runner.UnloadScene(SceneRef.FromIndex(Config.ROOM_SCENE_NUMBER));

        // アンロード完了を待つ（1フレーム待機）
        yield return new WaitForSeconds(0.5f);

        // ロード開始
        Runner.LoadScene(SceneRef.FromIndex(Config.ROOM_SCENE_NUMBER), LoadSceneMode.Additive);

        // ロード完了を待つ
        yield return new WaitForSeconds(0.5f);

        isReloadingScene = false;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        m_gameLauncher.OnPlayerJoined -= OnJoinUser;
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
        yield return new WaitUntil(() => resultData.Count >= 2);
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
            data.DigScore = userdata.Value.DigScore;
            data.TreasureCount = userdata.Value.TreasureCount;
            data.IsSelf = userdata.Key == Runner.LocalPlayer;
            datas.Add(data);
        }


        //ランキング表示
        m_ranking.ShowRanking(datas);
    }

    public async void Exit()
    {
        await m_gameLauncher.LeaveRoom();

        SceneManager.LoadScene(Config.ENTRANCE_SCENE_NUMBER, LoadSceneMode.Single);
    }

    public void PlayAgain()
    {
        m_resultCanvas.SetActive(false);
    }
}
