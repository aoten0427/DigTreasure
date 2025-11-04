using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class StartCountDown : MonoBehaviour
{
    [SerializeField] Image m_image;
    [SerializeField] Sprite[] m_countdown = new Sprite[3];
    [SerializeField] Sprite m_goSprite;
    [SerializeField] float m_maxScale = 1.2f;

    bool m_isActive;
    int m_currentIndex;
    Action m_completeCallback;
    bool m_isShowingGo;

    // アニメーション設定
    const float ANIMATION_DURATION = 0.2f;
    const float DISPLAY_DURATION = 1.0f;

    private void Start()
    {
        m_image.enabled = false;
    }

    public void CountDownStart(Action complete = null)
    {
        if (m_isActive)
            return;

        m_isActive = true;
        m_currentIndex = 2; // 3,2,1 の順に
        m_completeCallback = complete;
        m_isShowingGo = false;
        m_image.enabled = true;

        ShowNextSprite();
    }

    /// <summary>
    /// 次のスプライトを表示
    /// </summary>
    private void ShowNextSprite()
    {
        if (m_currentIndex >= 0)
        {
            // カウントダウン画像表示
            m_image.sprite = m_countdown[m_currentIndex];
            PlayScaleAnimation(() =>
            {
                m_currentIndex--;
                ShowNextSprite();
            });
        }
        else if (!m_isShowingGo && m_goSprite != null)
        {
            // GO 表示
            m_isShowingGo = true;
            m_image.sprite = m_goSprite;
            PlayScaleAnimation(() =>
            {
                // GO 表示が終わったら終了
                EndCountDown();
            });
        }
        else
        {
            // GOなしで終了
            EndCountDown();
        }
    }

    /// <summary>
    /// DOTweenを使ったスケールアニメーション
    /// </summary>
    private void PlayScaleAnimation(Action onComplete)
    {
        // スケール初期化
        m_image.transform.localScale = Vector3.one * m_maxScale;

        // スケールアニメーションを再生
        m_image.transform
            .DOScale(1.0f, ANIMATION_DURATION)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // 表示時間が経過したら次へ
                DOVirtual.DelayedCall(DISPLAY_DURATION, () => onComplete?.Invoke());
            });
    }

    private void EndCountDown()
    {
        m_isActive = false;
        m_image.enabled = false;
        m_image.transform.localScale = Vector3.one;

        // コールバック呼び出し
        m_completeCallback?.Invoke();
        m_completeCallback = null;
    }
}
