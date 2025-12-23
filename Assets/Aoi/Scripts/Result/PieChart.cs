using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 円グラフ表示
/// </summary>
public class PieChart : MonoBehaviour
{
    [SerializeField] private bool m_isActive;
    private float[] m_targetRatios;
    private float[] m_currentRatios;
    [SerializeField] private float m_additionRatio = 0.3125f;
    private float m_totalRatio = 0;
    [SerializeField] private Image m_image;
    private Material m_material;
    private Coroutine m_animationCoroutine;

    /// <summary>
    /// 進行状況を通知するイベント
    /// 引数: (セグメントIndex, セグメント進行率 0-1, 全体進行率 0-1)
    /// </summary>
    public event Action<int, float, float> OnProgressChanged;

    private void Awake()
    {
        m_material = Instantiate(m_image.material);
        m_image.material = m_material;
    }


    /// <summary>
    /// データをセットしてアニメーション開始
    /// </summary>
    public void SetData(float[] values, Action onComplete = null)
    {
        if (m_isActive) return;

        InitializeData(values);
        m_animationCoroutine = StartCoroutine(RatioUpdate(onComplete));
    }

    /// <summary>
    /// データをセットして即座に表示
    /// </summary>
    public void SetDataImmediate(float[] values, Action onComplete = null)
    {
        // 実行中のアニメーションがあれば停止
        StopAnimation();

        InitializeData(values);

        // 全セグメントを即座に目標値に設定
        for (int i = 0; i < m_targetRatios.Length; i++)
        {
            m_currentRatios[i] = i == 0
                ? m_targetRatios[i]
                : m_targetRatios[i] - m_targetRatios[i - 1];
        }
        m_totalRatio = m_targetRatios.Length > 0
            ? m_targetRatios[m_targetRatios.Length - 1]
            : 0f;

        UpdateRotation();

        // 完了を通知
        if (m_targetRatios.Length > 0)
        {
            OnProgressChanged?.Invoke(m_targetRatios.Length - 1, 1f, 1f);
        }

        m_isActive = false;
        onComplete?.Invoke();
    }

    /// <summary>
    /// データ初期化（共通処理）
    /// </summary>
    private void InitializeData(float[] values)
    {
        m_isActive = true;
        m_totalRatio = 0;

        float total = 0f;
        foreach (float value in values)
        {
            total += value;
        }

        m_currentRatios = new float[values.Length];
        m_targetRatios = new float[values.Length];

        float cumulativeRatio = 0f;
        for (int i = 0; i < values.Length; i++)
        {
            float ratio = total > 0 ? values[i] / total : 0f;
            cumulativeRatio += ratio;
            m_targetRatios[i] = cumulativeRatio;
        }
    }

    /// <summary>
    /// アニメーション停止
    /// </summary>
    public void StopAnimation()
    {
        if (m_animationCoroutine != null)
        {
            StopCoroutine(m_animationCoroutine);
            m_animationCoroutine = null;
        }
        m_isActive = false;
    }

    IEnumerator RatioUpdate(Action onComplete = null)
    {
        for (int i = 0; i < m_targetRatios.Length; i++)
        {
            // セグメントの目標値を計算
            float segmentTarget = i == 0
                ? m_targetRatios[i]
                : m_targetRatios[i] - m_targetRatios[i - 1];

            while (m_targetRatios[i] > m_totalRatio)
            {
                // 進行値を更新
                m_totalRatio += m_additionRatio * Time.deltaTime;
                m_currentRatios[i] += m_additionRatio * Time.deltaTime;
                m_currentRatios[i] = Mathf.Clamp(m_currentRatios[i], 0, segmentTarget);
                UpdateRotation();

                // 進行状況を通知
                float segmentProgress = segmentTarget > 0 ? m_currentRatios[i] / segmentTarget : 1f;
                float totalProgress = m_targetRatios[m_targetRatios.Length - 1] > 0
                    ? m_totalRatio / m_targetRatios[m_targetRatios.Length - 1]
                    : 1f;
                OnProgressChanged?.Invoke(i, segmentProgress, Mathf.Clamp01(totalProgress));

                yield return null;
            }

            // セグメント完了を通知（確実に1.0を渡す）
            float completedTotalProgress = m_targetRatios[m_targetRatios.Length - 1] > 0
                ? m_targetRatios[i] / m_targetRatios[m_targetRatios.Length - 1]
                : 1f;
            OnProgressChanged?.Invoke(i, 1f, Mathf.Clamp01(completedTotalProgress));

            yield return new WaitForSeconds(0.2f);
        }

        m_isActive = false;
        m_animationCoroutine = null;
        onComplete?.Invoke();
    }

    private void UpdateRotation()
    {
        float totalRotation = 0f;
        for (int i = 0; i < m_currentRatios.Length; i++)
        {
            float start = totalRotation;
            UpdateShader(i, start, m_currentRatios[i] + start);
            totalRotation += m_currentRatios[i];
        }
    }

    private void UpdateShader(int index, float start, float end)
    {
        if (!m_material)
        {
            m_material = Instantiate(m_image.material);
            m_image.material = m_material;
        }
        m_material.SetVector($"_Segment{index}", new Vector4(start, end, 0f, 0f));
    }
}