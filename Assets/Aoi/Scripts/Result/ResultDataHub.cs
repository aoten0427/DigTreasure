using Fusion;
using NetWork;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// リザルトデータを集計
/// </summary>
public class ResultDataHub : NetworkBehaviour
{
    //自身のユーザーデータ
    NetWork.NetworkUserData m_uesrData;
    NetWork.GameLauncher m_gameLauncher;

    //全体のリザルトデータ
    [Networked, Capacity(4)]
    private NetworkDictionary<PlayerRef, ResultData> resultData => default;
    //ランキング表示クラス
    [SerializeField] Ranking m_ranking;
    [SerializeField] bool m_islog = false;


    public override void Spawned()
    {
        if(m_ranking == null)
        {
            Debug.LogWarning("表示用ランキングがありません");
        }
        
        m_gameLauncher = GameLauncher.Instance;
        

        //ユーザーデータを取得しリザルトデータに反映
        m_uesrData = m_gameLauncher.UserData;
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
}
