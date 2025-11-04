using Fusion;
using NetWork;
using System.Collections.Generic;
using UnityEngine;

public struct PlayScore : INetworkStruct
{
    public int treasureScore;
    public int treasureNum;
    public int digScore;
}

public class LocalScore : MonoBehaviour
{ 
    //���g�̃X�R�A
    PlayScore m_score;
    //
    List<Collider> m_hits = new List<Collider>();
    //�X�R�A�}�l�[�W���[
    ScoreManager m_scoreManager;


    private void OnTriggerEnter(Collider other)
    {
        if (m_hits.Contains(other))return;

        m_hits.Add(other);

        if (other.gameObject.CompareTag("Treasure"))
        {
            Treasure treasure = other.gameObject.GetComponent<Treasure>();
            if (treasure == null) return;

            // スコアを加算
            m_score.treasureScore += treasure.ScorePoint;
            m_score.treasureNum++;

            if(!m_scoreManager)
            {
                m_scoreManager = FindFirstObjectByType<ScoreManager>();
            }

            m_scoreManager.ChangeTreasurePoint(m_score.treasureScore);

            // スコア加算後に削除処理
            //treasure.RPC_RequestDespawn();
        }
    }
}
