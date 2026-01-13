using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// カウントダウン表示を管理（開始・終了兼用）
/// </summary>
public class CountDown : MonoBehaviour
{
    #region SerializeFields

    [Header("表示設定")]
    [SerializeField] private Image m_image;
    [SerializeField] private float m_maxScale = 1.2f;
    [SerializeField] private float m_finalScale = 2.0f;

    [Header("開始用カウントダウン（3,2,1,GO）")]
    [SerializeField] private Sprite[] m_startSprites = new Sprite[3];
    [SerializeField] private Sprite m_goSprite;

    [Header("終了用カウントダウン（5,4,3,2,1,FINISH）")]
    [SerializeField] private Sprite[] m_endSprites = new Sprite[5];
    [SerializeField] private Sprite m_finishSprite;

    #endregion

    #region Private Fields

    // 開始カウントダウン用
    private bool m_isActive;
    private int m_currentIndex;
    private Action m_completeCallback;
    private bool m_isShowingFinal;
    private Sprite[] m_currentSprites;
    private Sprite m_finalSprite;

    // 終了カウントダウン用（個別呼び出し）
    private int m_lastEndCount = -1;
    private Tween m_endTween;
    private Action m_endCompleteCallback;

    #endregion

    #region Constants

    // アニメーション設定
    private const float ANIMATION_DURATION = 0.2f;
    private const float DISPLAY_DURATION = 1.0f;

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        m_image.enabled = false;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 開始カウントダウン（3,2,1,GO）
    /// </summary>
    public void StartCountDown(Action complete = null)
    {
        PlayCountDown(m_startSprites, m_goSprite, complete);
    }

    /// <summary>
    /// 終了カウントダウン（5,4,3,2,1,FINISH）
    /// </summary>
    public void EndCountDown(Action complete = null)
    {
        PlayCountDown(m_endSprites, m_finishSprite, complete);
    }

    /// <summary>
    /// 終了カウントダウン（個別呼び出し：残り時間を渡して該当の数値を表示）
    /// </summary>
    public void EndCountDown(int count, Action complete = null)
    {
        // 同じ値なら何もしない
        if (count == m_lastEndCount) return;
        m_lastEndCount = count;
        m_endCompleteCallback = complete;

        // 前のアニメーションをキャンセル
        m_endTween?.Kill();

        if (count <= 0)
        {
            // FINISHを表示
            PlayEndSprite(m_finishSprite, true);
        }
        else if (count <= m_endSprites.Length)
        {
            // 数値スプライトを表示
            int index = count - 1;
            PlayEndSprite(m_endSprites[index], false);
        }
    }

    /// <summary>
    /// 終了カウントダウンの状態をリセット
    /// </summary>
    public void ResetEndCountDown()
    {
        m_endTween?.Kill();
        m_lastEndCount = -1;
        m_endCompleteCallback = null;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// カウントダウン再生
    /// </summary>
    private void PlayCountDown(Sprite[] sprites, Sprite finalSprite, Action complete)
    {
        if (m_isActive) return;

        // 状態初期化
        m_isActive = true;
        m_currentSprites = sprites;
        m_finalSprite = finalSprite;
        m_currentIndex = sprites.Length - 1;
        m_completeCallback = complete;
        m_isShowingFinal = false;
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
            m_image.sprite = m_currentSprites[m_currentIndex];
            PlayScaleAnimation(() =>
            {
                m_currentIndex--;
                ShowNextSprite();
            });
        }
        else if (!m_isShowingFinal && m_finalSprite != null)
        {
            // 最後のスプライト表示（GO or FINISH）
            m_isShowingFinal = true;
            m_image.sprite = m_finalSprite;
            PlayScaleAnimation(() =>
            {
                FinishCountDown();
            }, true);
        }
        else
        {
            // 最後のスプライトなしで終了
            FinishCountDown();
        }
    }

    /// <summary>
    /// DOTweenを使ったスケールアニメーション
    /// </summary>
    private void PlayScaleAnimation(Action onComplete, bool isFinal = false)
    {
        // スケール設定（最後のスプライトは大きく表示）
        float targetScale = isFinal ? m_finalScale : 1.0f;
        float startScale = targetScale * m_maxScale;
        m_image.transform.localScale = Vector3.one * startScale;

        // スケールアニメーションを再生
        m_image.transform
            .DOScale(targetScale, ANIMATION_DURATION)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // 表示時間が経過したら次へ
                DOVirtual.DelayedCall(DISPLAY_DURATION, () => onComplete?.Invoke());
            });
    }

    /// <summary>
    /// カウントダウン終了
    /// </summary>
    private void FinishCountDown()
    {
        m_isActive = false;
        m_image.enabled = false;

        // コールバック呼び出し
        m_completeCallback?.Invoke();
        m_completeCallback = null;
    }

    /// <summary>
    /// 終了カウントダウン用の個別スプライト表示
    /// </summary>
    private void PlayEndSprite(Sprite sprite, bool isFinal)
    {
        // スケール設定（最後のスプライトは大きく表示）
        float targetScale = isFinal ? m_finalScale : 1.0f;
        float startScale = targetScale * m_maxScale;

        m_image.enabled = true;
        m_image.sprite = sprite;
        m_image.transform.localScale = Vector3.one * startScale;

        // スケールアニメーション
        m_endTween = m_image.transform
            .DOScale(targetScale, ANIMATION_DURATION)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                if (isFinal)
                {
                    // FINISHの場合は表示時間後に終了
                    m_endTween = DOVirtual.DelayedCall(DISPLAY_DURATION, () =>
                    {
                        m_image.enabled = false;
                        m_image.transform.localScale = Vector3.one;
                        m_lastEndCount = -1;
                        m_endCompleteCallback?.Invoke();
                        m_endCompleteCallback = null;
                    });
                }
            });
    }

    #endregion
}
