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
    PlayerInventoryLogic m_inventory;

    public int m_treasurePoint;
    public int m_treasureNum;
    public int m_digPoint;

    private void Start()
    {
         m_inventory = GetComponent<PlayerInventoryLogic>();
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
        if(m_inventory)
        {
            m_inventory.OnUpdateTreasure -= UpdateTreasure;
        }
        
    }

    private void UpdateTreasure(int point,int num,int dig)
    {
        m_treasurePoint = point;
        m_treasureNum = num;
        m_digPoint = dig;
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

}
