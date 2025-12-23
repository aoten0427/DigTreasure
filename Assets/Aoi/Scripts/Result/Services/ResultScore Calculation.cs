using System.Collections.Generic;
using UnityEngine;

public class ResultScoreCalculation : MonoBehaviour
{
    //Digポイントのランクに対応したポイント
    List<int> m_digRankScore = new List<int> { 300, 200, 100, 50 };

    //ボードデータ
    [SerializeField] GameObject m_totalDataObject;
    [SerializeField] List<BoradData> m_bordDatas = new List<BoradData>();

    //円グラフデータ
    [SerializeField] GameObject m_piechartDataObject;
    [SerializeField] PieChart m_boradPieChart;
    [SerializeField] List<PiechartData> m_piechartDatas = new List<PiechartData>();

    [SerializeField] ResultInput m_resultInput;
    //表示フラグ
    private bool m_isShowTotal;

    private void Awake()
    {
        if(m_resultInput)
        {
            m_resultInput.OnRTrigger += SelectRTrigger;
            m_resultInput.OnLTrigger += SelectLTrigger;
        }
        m_piechartDataObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (m_resultInput)
        {
            m_resultInput.OnRTrigger -= SelectRTrigger;
            m_resultInput.OnLTrigger -= SelectLTrigger;
        }
    }

    public void Initialize(List<ResultData> datas)
    {
        Calculation(datas);

        //スコアボード更新
        UpdateBorad(datas);

        UpdatePieChart(datas);
    }



    /// <summary>
    /// 各スコアを計算
    /// </summary>
    void Calculation(List<ResultData> datas)
    {
        //インデックスに紐づいたポイント
        int[] indextToScore = new int[datas.Count];
        //Dipポイントソート
        datas.Sort((a, b) => b.DigPoint.CompareTo(a.DigPoint));

        //最終スコア計算
        for (int i = 0; i < datas.Count; i++)
        {
            if (i > m_digRankScore.Count) break;

            var data = datas[i];

            //Digポイントの順位に基づいたスコアを加算
            data.DigScore += m_digRankScore[i];


            //最終ポイント加算
            data.TotalScore += data.TreasureScore;
            data.TotalScore += data.DigScore;

            datas[i] = data;
        }

        //最終スコアソート
        datas.Sort((a, b) => b.TotalScore.CompareTo(a.TotalScore));

    }

    private void UpdateBorad(List<ResultData> datas)
    {
        datas.Sort((a, b) => b.TotalScore.CompareTo(a.TotalScore));

        for (int i = 0;i < datas.Count;i++)
        {
            //表示するインデックス番号取得
            int index = datas[i].Index;

            if (index >= m_bordDatas.Count) continue;

            int rank = i;
            string name = datas[i].NickName.ToString();
            int totalsocre = datas[i].TotalScore;
            int treasurescore = datas[i].TreasureScore;
            int destroyscore = datas[i].DigScore;

            m_bordDatas[index].SetData(rank,name, totalsocre, treasurescore, destroyscore);

        }
    }

    private void UpdatePieChart(List<ResultData> datas)
    {
        datas.Sort((a, b) => a.Index.CompareTo(b.Index));

        float[] destroyDatas = new float[datas.Count];
        int totalDestroy = 0;
        for (int i = 0; i < datas.Count; i++)
        {
            totalDestroy += datas[i].DigPoint;
            destroyDatas[i] = datas[i].DigPoint;

        }

        m_boradPieChart.SetDataImmediate(destroyDatas);

        for(int i = 0;i < datas.Count;i++)
        {
            //表示するインデックス番号取得
            int index = datas[i].Index;

            if (index >= m_piechartDatas.Count) continue;

            string name = datas[i].NickName.ToString();
            int point = datas[i].DigPoint;
            int ratio = (int)(((float)point / (float)totalDestroy) * 100);

            m_piechartDatas[i].SetData(name, point, ratio); 
        }
    }

    private void SelectRTrigger(bool push)
    {
        m_isShowTotal = false;
        m_totalDataObject.SetActive(m_isShowTotal);
        m_piechartDataObject.SetActive(!m_isShowTotal);
    }

    private void SelectLTrigger(bool push)
    {
        m_isShowTotal = true;
        m_totalDataObject.SetActive(m_isShowTotal);
        m_piechartDataObject.SetActive(!m_isShowTotal);
    }
}
