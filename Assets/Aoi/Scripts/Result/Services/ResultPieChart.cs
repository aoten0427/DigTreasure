using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// リザルト画面の円グラフ表示
/// </summary>
public class ResultPieChart : MonoBehaviour
{
    //円グラフ本体
    [SerializeField] private PieChart m_pieChart;
    //ユーザーデータ
    [SerializeField]List<PiechartData> m_datas = new List<PiechartData>();
    //合計
    [SerializeField] PiechartData m_totalData;

    private List<int> m_pointCounter = new List<int>();
    private int m_totalCounter;

    private void Awake()
    {
        if (m_pieChart) m_pieChart.OnProgressChanged += OnPieChartProgress;
    }

    private void Start()
    {
        //初期状態は非表示
        m_pieChart.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if(m_pieChart)m_pieChart.OnProgressChanged -= OnPieChartProgress;
    }

    /// <summary>
    /// 円グラフ表示を開始
    /// </summary>
    public IEnumerator Initialize(List<ResultData> datas)
    {
        if (m_pieChart == null) yield break;

       

        //円グラフを表示
        m_pieChart.gameObject.SetActive(true);

        //インデックス順でソート
        var sortedDatas = datas.ToList();
        sortedDatas.Sort((a, b) => a.Index.CompareTo(b.Index));

        //それぞれのDigPointを記録
        foreach (var data in sortedDatas)
        {
            m_pointCounter.Add(data.DigPoint);
            m_totalCounter += data.DigPoint;
        }


        foreach(var data in m_datas)data.gameObject.SetActive(false);

        //データの名前初期化
        for(int i = 0; i < sortedDatas.Count; i++)
        {
            if (i >= m_datas.Count) break;
            m_datas[i].SetData(sortedDatas[i].NickName.ToString(), 0, 0);
            m_datas[i].gameObject.SetActive(true);
        }
        m_totalData.SetData( 0, 0);

        //アニメーション完了フラグ
        bool m_isDoing = true;

        //各プレイヤーの掘りスコアを配列に変換
        float[] score = new float[sortedDatas.Count];
        for (int i = 0; i < sortedDatas.Count; i++)
        {
            score[i] = sortedDatas[i].DigPoint;
        }

        yield return new WaitForSeconds(2.0f);

        //円グラフアニメーション開始
        m_pieChart.SetData(score, () => m_isDoing = false);

        //アニメーション完了まで待機
        while (m_isDoing)
        {
            yield return null;
        }

        yield return new WaitForSeconds(3.0f);

        //円グラフを非表示
        m_pieChart.gameObject.SetActive(false);
    }

    private void OnPieChartProgress(int segmentIndex, float segmentProgress, float totalProgress)
    {
        m_datas[segmentIndex].SetData((int)(m_pointCounter[segmentIndex] * segmentProgress), (int)segmentProgress * 100);

        m_totalData.SetData((int)(m_totalCounter * totalProgress), (int)totalProgress * 100);
    }
}
