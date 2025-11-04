using Fusion;
using NetWork;
using UnityEngine;

/// <summary>
/// プレイ時のスコア管理
/// </summary>
public class ScoreManager : NetworkBehaviour
{
    NetWork.GameLauncher m_gameLauncher;
    //ユーザーデータ
    NetWork.NetworkUserData m_userData;

    public override void Spawned()
    {
           
    }

    private void Start()
    {
        m_gameLauncher = GameLauncher.Instance;
    }




    /// <summary>
    /// 宝ポイントを変更
    /// </summary>
    /// <param name="user"></param>
    /// <param name="treasureScore"></param>
    public void ChangeTreasurePoint(int treasurePoint)
    {
        m_userData = m_gameLauncher.UserData;

        //ユーザーを取得
        var user = Runner.LocalPlayer;
        //ユーザーデータのスコア更新
        m_userData.m_treasurePoint = treasurePoint;
        m_gameLauncher.UserData = m_userData;
    }



}


