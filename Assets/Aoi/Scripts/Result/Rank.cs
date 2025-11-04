using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Rank : MonoBehaviour
{
    [SerializeField] Sprite[] m_rankSprite;
    [SerializeField] Image m_rank;
    [SerializeField] TextMeshProUGUI m_name;
    [SerializeField] AutoFont m_treasureScore;
    [SerializeField] AutoFont m_treasureCount;
    [SerializeField] AutoFont m_digCount;

    private void Start()
    {
        m_rank.enabled = false;
        m_name.enabled = false;
    }

    public void ShowRank(int rank, string name, int treasureScore, int treasureCount, int digCount)
    {
        m_rank.enabled = true;
        m_name.enabled = true;

        m_rank.sprite = m_rankSprite[rank - 1];
        m_name.text = name.ToString();
        m_treasureScore.SetText(treasureScore.ToString() + "pt");
        m_treasureCount.SetText(treasureCount.ToString());
        m_digCount.SetText(digCount.ToString());
    }
}
