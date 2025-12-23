using UnityEngine;
using UnityEngine.UI;

public class ResultPoint : MonoBehaviour
{
    [SerializeField] private Sprite[] m_numbers = new Sprite[10];
    [SerializeField] private Image[] m_images;

    private int MaxScore => (int)Mathf.Pow(10, m_images.Length) - 1;

    public void SetScore(int score)
    {
        if (m_images == null || m_images.Length == 0)
        {
            Debug.LogWarning("ResultPoint: m_images が設定されていません");
            return;
        }

        score = Mathf.Clamp(score, 0, MaxScore);

        for (int i = 0; i < m_images.Length; i++)
        {
            int divisor = (int)Mathf.Pow(10, i);
            int digit = (score / divisor) % 10;

            // 一の位は常に表示、それ以外はスコアがその桁に達している場合のみ表示
            bool shouldShow = (i == 0) || (score >= divisor);

            m_images[i].enabled = shouldShow;
            if (shouldShow)
            {
                m_images[i].sprite = m_numbers[digit];
            }
        }
    }
}