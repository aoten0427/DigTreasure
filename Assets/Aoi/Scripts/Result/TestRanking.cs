using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// リザルトデータ
/// </summary>
[System.Serializable]
public struct ResultData : INetworkStruct
{
    //プレイヤー名
    public NetworkString<_16> NickName;
    //宝スコア
    public int TreasureScore;
    //宝の数
    public int TreasureCount;
    //はかいポイント
    public int DigPoint;
    //破壊ポイント
    public int DigScore;
    //プレイヤーインデックス
    public int Index;
    //自分自身のデータか
    public bool IsSelf;
    //最終スコア
    public int TotalScore;
}

/// <summary>
/// ランキング表示用抽象クラス
/// </summary>
public abstract class Ranking : MonoBehaviour
{
    /// <summary>
    /// リザルトを表示
    /// </summary>
    public abstract void ShowRanking(List<ResultData> resultdata, Action oncomplete = null);
}

/// <summary>
/// テスト用ランキング
/// </summary>
public class TestRanking : Ranking
{
    //最大表示人数
    const int m_maxNumber = 4;
    //ランク表示UI
    [SerializeField] Rank[] m_ranks = new Rank[4];
    //テスト用データ
    [SerializeField] List<ResultData> m_tempResult = new();

    /// <summary>
    /// ランキング表示
    /// </summary>
    public override void ShowRanking(List<ResultData> resultdata, Action oncomplete = null)
    {
        //宝スコアでソート（元のリストを変更しないようコピー）
        var sortedData = resultdata.ToList();
        sortedData.Sort((a, b) => b.TreasureScore.CompareTo(a.TreasureScore));

        //順位ごとに表示
        for (int i = 0; i < m_maxNumber; i++)
        {
            if (sortedData.Count <= i) break;
            m_ranks[i].ShowRank(i + 1, sortedData[i].NickName.ToString(), sortedData[i].TreasureScore,
                sortedData[i].TreasureCount, sortedData[i].DigPoint);
        }
    }

    //デバッグ用：Tキーでテストデータを表示
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            ShowRanking(m_tempResult);
        }
    }
}
