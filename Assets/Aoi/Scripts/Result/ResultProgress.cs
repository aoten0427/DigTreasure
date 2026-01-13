using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ローカル用リザルト表示管理
/// </summary>
public class ResultProgress : Ranking
{
    [SerializeField] ResultManager m_manager;

    //リザルトデータ
    List<ResultData> m_resultData;

    //プレイヤー配置
    [SerializeField] ResultPlayerSetting m_playerSetting;
    //スコア計算
    [SerializeField]ResultScoreCalculation m_scoreCalculation;
    //お宝生成演出
    [SerializeField] ResultSpawnTreasure m_spawnTreasure;
    //円グラフ演出
    [SerializeField] ResultPieChart m_pieChart;
    //スポットライト演出
    [SerializeField] ResultSpotLight m_spotLight;
    //演出終了後に出すキャンバス
    [SerializeField] GameObject m_finishCanvas;

    bool m_isAction = false;

    private void Start()
    {
        m_finishCanvas.SetActive(false);
        if (m_manager) m_manager.OnClose += Close;
    }

    private void OnDestroy()
    {
        if (m_manager) m_manager.OnClose -= Close;
    }

    /// <summary>
    /// ランキング表示を開始
    /// </summary>
    public override void ShowRanking(List<ResultData> resultdata, Action oncomplete = null)
    {
        if (m_isAction) return;
        m_isAction = true;
        m_resultData = resultdata;
        StartCoroutine(ShowRankingSequence(oncomplete));
    }

    /// <summary>
    /// ランキング表示の順次処理
    /// </summary>
    private IEnumerator ShowRankingSequence(Action oncomplete = null)
    {
        //プレイヤー配置
        m_playerSetting.Initialize(m_resultData);

        //スコア計算
        m_scoreCalculation.Initialize(m_resultData);

        yield return new WaitForSeconds(1.0f);

        //お宝生成演出
        yield return StartCoroutine(m_spawnTreasure.Initialize(m_resultData));

        //円グラフ演出
        yield return StartCoroutine(m_pieChart.Initialize(m_resultData));

        //スポットライト演出
        yield return StartCoroutine(m_spotLight.Initialize(m_resultData));

        //キャンバスを見せる
        m_finishCanvas?.SetActive(true);

        //完了コールバック
        oncomplete?.Invoke();
    }

    //デバッグ用：1キーでテストデータを表示
    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            var data1 = new ResultData();
            data1.NickName = "player1";
            data1.Index = 0;
            data1.TreasureScore = 1000;
            data1.DigPoint = 20000;

            var data2 = new ResultData();
            data2.NickName = "player2";
            data2.Index = 1;
            data2.TreasureScore = 1100;
            data2.DigPoint = 50000;

            //var data3 = new ResultData();
            //data3.NickName = "player3";
            //data3.Index = 2;
            //data3.TreasureScore = 1000;
            //data3.DigPoint = 8000;

            //var data4 = new ResultData();
            //data4.NickName = "player4";
            //data4.Index = 3;
            //data4.TreasureScore = 1300;
            //data4.DigPoint = 2000;

            var testdata = new List<ResultData>() { data1, data2/*, data3, data4*/ };

            ShowRanking(testdata);
        }
    }

    private void Close()
    {
        m_finishCanvas.SetActive(false);
    }
}
