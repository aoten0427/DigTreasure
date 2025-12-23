using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// リザルト画面のプレイヤー配置
/// </summary>
public class ResultPlayerSetting : MonoBehaviour
{
    //配置範囲の最小位置
    [SerializeField] Vector3 m_minPosition;
    //配置範囲の最大位置
    [SerializeField] Vector3 m_maxPosition;
    //プレイヤーオブジェクト
    [SerializeField] GameObject[] m_players;

    private void Start()
    {
        //初期状態は全員非表示
        foreach (var player in m_players)
        {
            player.SetActive(false);
        }
    }

    /// <summary>
    /// プレイヤーを均等に配置
    /// </summary>
    public void Initialize(List<ResultData> data)
    {
        //参加人数分のプレイヤーを表示
        for (int i = 0; i < data.Count; i++)
        {
            if (i >= m_players.Length) break;
            m_players[i].gameObject.SetActive(true);
        }

        //配置間隔を計算（人数+1で端を除いた場所に配置）
        Vector3 distance = m_maxPosition - m_minPosition;
        Vector3 interval = distance / (data.Count + 1);

        //各プレイヤーを配置
        for (int i = 0; i < data.Count; i++)
        {
            int offset = i + 1;
            Vector3 position = m_minPosition + interval * offset;
            m_players[i].transform.position = position;

            //カメラの方を向かせる
            LookCamera(m_players[i]);
        }
    }

    /// <summary>
    /// カメラのほうを向く
    /// </summary>
    void LookCamera(GameObject go)
    {
        //カメラ位置を取得（Y軸は自身の高さに合わせる）
        Vector3 targetPos = Camera.main.transform.position;
        targetPos.y = go.transform.position.y;
        go.transform.LookAt(targetPos);
    }
}
