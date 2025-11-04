using Fusion;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public struct ResultData : INetworkStruct
{
    public NetworkString<_16> NickName;//名前
    public int TreasureScore;//宝スコあ
    public int TreasureCount; //宝の数
    public int DigScore;//堀スコア
    public bool IsSelf;//このデータが自分自身のか
}

/// <summary>
/// ランキング表示用インターフェース
/// </summary>
public abstract class Ranking:MonoBehaviour
{
    //リザルトを表示
    public abstract void ShowRanking(List<ResultData> resultdata);
}

/// <summary>
/// テスト用ランキング
/// </summary>
public class TestRanking : Ranking
{
    [SerializeField] const int m_maxNumber = 4;
    [SerializeField]Rank[] m_ranks = new Rank[4];
    [SerializeField] List<ResultData> m_tempResult = new();

    public override void ShowRanking(List<ResultData> resultdata)
    {
        //宝ポイントでソート
        resultdata.Sort((a, b) => b.TreasureScore.CompareTo(a.TreasureScore));

        

        for (int i = 0; i < m_maxNumber; i++)
        {
            if (resultdata.Count <= i) break;
            m_ranks[i].ShowRank(i + 1, resultdata[i].NickName.ToString(), resultdata[i].TreasureScore,
                resultdata[i].TreasureCount, resultdata[i].DigScore);
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.T))
        {
            //var data1 = new ResultData();
            //data1.NickName = "player1";
            //data1.TreasureScore = 3;

            //var data2 = new ResultData();
            //data2.NickName = "player2";
            //data2.TreasureScore = 0;

            //var data3 = new ResultData();
            //data3.NickName = "player3";
            //data3.TreasureScore = 1;

            //var data4 = new ResultData();
            //data4.NickName = "player4";
            //data4.TreasureScore = 5;


            //var testdata = new List<ResultData>() { data1,data2, data3, data4 };
            //ShowRanking(testdata);
            ShowRanking(m_tempResult);
        }
    }
}
