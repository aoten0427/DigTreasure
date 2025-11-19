using Fusion;
using NetWork;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// プレイシーンでの自身のスコアを管理
/// </summary>
public class PlayScoreCenter : MonoBehaviour
{

    GameLauncher m_gameLauncher;
    //宝石管理用のインベントリ
    PlayerInventory m_inventory;

    public int m_treasurePoint;
    public int m_treasureNum;
    public int m_digPoint;

    private void Start()
    {
         m_inventory = GetComponent<PlayerInventory>();
        if(m_inventory == null )
        {
            Destroy(this);
            return;
        }
        m_inventory.OnUpdateTreasure += UpdateTreasure;

        m_gameLauncher = GameLauncher.Instance;
    }

    private void OnDestroy()
    {
        m_inventory.OnUpdateTreasure -= UpdateTreasure;
    }

    private void UpdateTreasure(int point,int num)
    {
        m_treasurePoint = point;
        m_treasureNum = num;
        UpdateScore();
    }

    /// <summary>
    /// スコアを更新
    /// </summary>
    private void UpdateScore()
    {
        var userdata = m_gameLauncher.UserData;
        userdata.m_treasurePoint = m_treasurePoint;
        userdata.m_treasureCount = m_treasureNum;
        userdata.m_digPoint = m_digPoint;
        m_gameLauncher.UserData = userdata;
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (m_hits.Contains(other))return;

    //    m_hits.Add(other);

    //    if (other.gameObject.CompareTag("Treasure"))
    //    {
    //        Treasure treasure = other.gameObject.GetComponent<Treasure>();
    //        if (treasure == null) return;

    //        // スコアを加算
    //        m_score.treasureScore += treasure.ScorePoint;
    //        m_score.treasureNum++;

    //        if(!m_scoreManager)
    //        {
    //            m_scoreManager = FindFirstObjectByType<ScoreUIData>();
    //        }

    //        m_scoreManager.ChangeTreasurePoint(m_score.treasureScore);

    //        // スコア加算後に削除処理
    //        //treasure.RPC_RequestDespawn();
    //    }
    //}
}
