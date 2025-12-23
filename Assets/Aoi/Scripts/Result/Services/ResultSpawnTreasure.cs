using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// リザルト画面でお宝を降らせる演出
/// </summary>
public class ResultSpawnTreasure : MonoBehaviour
{
    //お宝プレハブ
    [SerializeField] GameObject[] m_treasures;
    //お宝生成ポイント
    [SerializeField] GameObject[] m_treasureSpawnPoint = new GameObject[4];

    /// <summary>
    /// お宝生成演出を開始
    /// </summary>
    public IEnumerator Initialize(List<ResultData> datas)
    {
        yield return new WaitForSeconds(1f);

        //宝スコアでソート（元のリストを変更しないようコピー）
        var sortedDatas = datas.ToList();
        sortedDatas.Sort((a, b) => b.TreasureScore.CompareTo(a.TreasureScore));

        //1位は最大数、順位が下がるごとに減少
        int max = 30;
        List<Coroutine> coroutines = new List<Coroutine>();

        //各プレイヤーの生成コルーチンを開始
        foreach (var data in sortedDatas)
        {
            coroutines.Add(StartCoroutine(SpawnTreasure(max, data.Index)));
            max -= 3;
        }

        //全てのコルーチンが終わるのを待つ
        foreach (var c in coroutines)
        {
            yield return c;
        }

        //落ち切るのを待つ
        yield return new WaitForSeconds(3.0f);
    }

    /// <summary>
    /// お宝を生成
    /// </summary>
    IEnumerator SpawnTreasure(int num, int index)
    {
        //生成間隔を計算
        float waittime = 1.0f / num;

        for (int i = 0; i < num; i++)
        {
            //生成位置を計算（上から降らせる）
            Vector3 point = m_treasureSpawnPoint[index].transform.position;
            point.y += 10 + (i + 2);

            //ランダムな回転を生成
            float x = Random.Range(0f, 180f);
            float y = Random.Range(0f, 180f);
            float z = Random.Range(0f, 180f);
            Quaternion rot = Quaternion.Euler(x, y, z);

            //ランダムなお宝を選択して生成
            int treasureIndex = Random.Range(0, m_treasures.Length);
            Instantiate(m_treasures[treasureIndex], point, rot);

            yield return new WaitForSeconds(waittime);
        }
    }
}
