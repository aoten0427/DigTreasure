using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// フェード演出を管理
/// </summary>
public class FadeManager : MonoBehaviour
{
    #region Constants

    // シェーダー定数（_timeパラメータの値）
    private const float FADE_MIN_VALUE = 0f;
    private const float FADE_TITLE_VALUE = 0.3f;
    private const float FADE_MAX_VALUE = 5.5f;

    // パワー初期値
    private const float POWER_TITLE_FADEIN = 10f;
    private const float POWER_NORMAL_FADEIN = 1f;
    private const float POWER_TITLE_FADEOUT = 15f;
    private const float POWER_NORMAL_FADEOUT = 5f;

    // パワー変化速度
    private const float POWER_FADEIN_TITLE_SPEED = 4f;
    private const float POWER_FADEIN_NORMAL_SPEED = 2f;
    private const float POWER_FADEOUT_TITLE_SPEED = 7f;
    private const float POWER_FADEOUT_NORMAL_SPEED = 1f;

    #endregion

    #region Singleton

    public static FadeManager instance { get; private set; }

    #endregion

    #region SerializeFields

    [Header("フェード用Transform")]
    [SerializeField] private RectTransform m_fadeInTransform;
    [SerializeField] private RectTransform m_fadeOutTransform;

    [Header("フェード設定")]
    [SerializeField] private float m_fadeSpeed = 2f;
    [SerializeField] private string m_titleSceneName;

    #endregion

    #region Private Fields

    // フェード状態
    private float m_power;
    private bool m_isFadeIn = false;
    private bool m_isFadeOut = false;
    private bool m_isChangeScene = false;
    private string m_nextSceneName;

    // コンポーネント参照
    private Material m_material;
    private RectTransform m_rectTransform;

    #endregion

    #region Events

    public event Action OnFadeInStart;
    public event Action OnFadeInEnd;
    public event Action OnFadeOutStart;
    public event Action OnFadeOutEnd;
    public event Action<float> OnFadeProgress;

    #endregion

    #region Properties

    public bool IsFadeIn => m_isFadeIn;
    public bool IsFadeOut => m_isFadeOut;
    private bool IsTitle => SceneManager.GetActiveScene().name == m_titleSceneName;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        InitializeSingleton();
    }

    private void Start()
    {
        InitializeFadeState();
    }

    private void OnDestroy()
    {
        CleanupSingleton();
    }

    private void Update()
    {
        // フェード処理
        if (m_isFadeIn || m_isFadeOut)
        {
            UpdateFade();
        }

        // シーン遷移処理
        if (!m_isFadeIn && !m_isFadeOut && m_isChangeScene)
        {
            ExecuteSceneChange();
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// シングルトン初期化
    /// </summary>
    private void InitializeSingleton()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(GetComponentInParent<Canvas>());
            CacheComponents();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// コンポーネント取得
    /// </summary>
    private void CacheComponents()
    {
        m_material = GetComponent<Image>().material;
        m_rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// フェード初期状態を設定
    /// </summary>
    private void InitializeFadeState()
    {
        if (!m_material) return;

        if (IsTitle)
        {
            // タイトルシーン：部分的にフェード表示
            m_rectTransform.SetPositionAndRotation(m_fadeInTransform.position, m_fadeInTransform.rotation);
            SetFadeValue(FADE_TITLE_VALUE);
        }
        else
        {
            // 通常シーン：完全に透明
            SetFadeValue(FADE_MAX_VALUE);
        }
    }

    /// <summary>
    /// シングルトン解除
    /// </summary>
    private void CleanupSingleton()
    {
        // マテリアルをリセット
        if (m_material)
        {
            SetFadeValue(FADE_MAX_VALUE);
        }

        // シングルトン解除
        if (instance == this)
        {
            instance = null;
        }
    }

    #endregion

    #region Fade Update

    /// <summary>
    /// フェード更新処理
    /// </summary>
    private void UpdateFade()
    {
        // 現在の値を取得
        float currentValue = GetFadeValue();

        // 値を更新
        float newValue = CalculateNewFadeValue(currentValue);

        // 完了チェック
        newValue = CheckFadeCompletion(newValue);

        // マテリアルに反映
        SetFadeValue(newValue);

        // 進行状況を通知
        NotifyProgress(newValue);

        // パワー値の更新
        UpdatePower();
    }

    /// <summary>
    /// 新しいフェード値を計算
    /// </summary>
    private float CalculateNewFadeValue(float currentValue)
    {
        // フェードイン: -1, フェードアウト: +1
        float direction = m_isFadeOut ? 1f : -1f;
        float delta = Time.deltaTime / m_power * m_fadeSpeed * direction;
        return currentValue + delta;
    }

    /// <summary>
    /// フェード完了チェック
    /// </summary>
    private float CheckFadeCompletion(float value)
    {
        // フェードイン完了
        if (value <= FADE_MIN_VALUE)
        {
            m_isFadeIn = false;
            OnFadeInEnd?.Invoke();
            OnFadeInEnd = null;
            return FADE_MIN_VALUE;
        }

        // タイトルシーンでのフェードアウト完了
        if (m_isFadeOut && IsTitle && value >= FADE_TITLE_VALUE)
        {
            m_isFadeOut = false;
            OnFadeOutEnd?.Invoke();
            OnFadeInEnd = null;
            return FADE_TITLE_VALUE;
        }

        // 通常のフェードアウト完了
        if (value >= FADE_MAX_VALUE)
        {
            m_isFadeOut = false;
            OnFadeOutEnd?.Invoke();
            OnFadeOutEnd = null;
            return FADE_MAX_VALUE;
        }

        return value;
    }

    /// <summary>
    /// 進行状況を通知（0~1の範囲）
    /// </summary>
    private void NotifyProgress(float value)
    {
        float maxValue = IsTitle ? FADE_TITLE_VALUE : FADE_MAX_VALUE;
        float progress = Mathf.Clamp01(value / maxValue);
        OnFadeProgress?.Invoke(progress);
    }

    /// <summary>
    /// パワー値の更新
    /// </summary>
    private void UpdatePower()
    {
        if (m_isFadeIn)
        {
            // フェードイン中：パワー増加
            float speed = IsTitle ? POWER_FADEIN_TITLE_SPEED : POWER_FADEIN_NORMAL_SPEED;
            m_power += Time.deltaTime * speed;
        }
        else
        {
            // フェードアウト中：パワー減少
            if (IsTitle)
            {
                m_power -= Time.deltaTime * POWER_FADEOUT_TITLE_SPEED;
            }
            m_power -= Time.deltaTime * POWER_FADEOUT_NORMAL_SPEED;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// フェードイン開始（画面を覆う）
    /// </summary>
    public void FadeIn()
    {
        if (m_isFadeIn || m_isFadeOut) return;

        m_isFadeIn = true;
        m_rectTransform.SetPositionAndRotation(m_fadeInTransform.position, m_fadeInTransform.rotation);
        m_power = IsTitle ? POWER_TITLE_FADEIN : POWER_NORMAL_FADEIN;

        OnFadeInStart?.Invoke();
        OnFadeInStart = null;
    }

    /// <summary>
    /// フェードアウト開始（画面を表示）
    /// </summary>
    public void FadeOut(string nextSceneName = "None")
    {
        m_isFadeOut = true;

        bool isNextTitle = nextSceneName == m_titleSceneName;
        var targetTransform = isNextTitle ? m_fadeInTransform : m_fadeOutTransform;
        m_rectTransform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
        m_power = isNextTitle ? POWER_TITLE_FADEOUT : POWER_NORMAL_FADEOUT;

        OnFadeOutStart?.Invoke();
        OnFadeOutStart = null;
    }

    /// <summary>
    /// シーン遷移（フェードイン→シーンロード→フェードアウト）
    /// </summary>
    public void ChangeScene(string sceneName)
    {
        if (m_isFadeIn || m_isFadeOut) return;

        m_nextSceneName = sceneName;
        m_isChangeScene = true;
        FadeIn();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// シーン遷移を実行
    /// </summary>
    private void ExecuteSceneChange()
    {
        SceneManager.LoadScene(m_nextSceneName);
        FadeOut(m_nextSceneName);
        m_isChangeScene = false;
    }

    /// <summary>
    /// シェーダーのフェード値を取得
    /// </summary>
    private float GetFadeValue()
    {
        return m_material.GetFloat("_time");
    }

    /// <summary>
    /// シェーダーのフェード値を設定
    /// </summary>
    private void SetFadeValue(float value)
    {
        m_material.SetFloat("_time", value);
    }

    #endregion
}
