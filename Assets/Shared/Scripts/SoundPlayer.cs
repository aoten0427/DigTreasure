using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Audio;

public class SoundPlayer : MonoBehaviour
{
    public static SoundPlayer Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private SoundData m_soundData;
    [SerializeField] private int m_sePoolSize = 5;

    [Header("Fade Settings")]
    [SerializeField] private float m_defaultFadeTime = 0.5f;

    private AudioSource m_bgmSource;
    private List<AudioSource> m_sePool;
    private int currentSEIndex;

    private Dictionary<BGMType, SoundData.BGMEntry> m_bgmDict;
    private Dictionary<SEType, SoundData.SEEntry> m_seDict;

    private Tween m_bgmFadeTween;
    private BGMType? m_currentBGM;

    [SerializeField] private AudioMixerGroup m_bgmMixerGroup;
    [SerializeField] private AudioMixerGroup m_seMixerGroup;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void Initialize()
    {
        // BGM用AudioSource作成
        m_bgmSource = gameObject.AddComponent<AudioSource>();
        m_bgmSource.playOnAwake = false;
        m_bgmSource.loop = true;
        m_bgmSource.outputAudioMixerGroup = m_bgmMixerGroup;

        // SEプール作成
        m_sePool = new List<AudioSource>(m_sePoolSize);
        for (int i = 0; i < m_sePoolSize; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = m_seMixerGroup;
            m_sePool.Add(source);
        }

        // Dictionary構築
        BuildDictionaries();
    }

    private void BuildDictionaries()
    {
        m_bgmDict = new Dictionary<BGMType, SoundData.BGMEntry>();
        m_seDict = new Dictionary<SEType, SoundData.SEEntry>();

        if (m_soundData == null)
        {
            Debug.LogError("SoundData is not assigned!");
            return;
        }

        foreach (var entry in m_soundData.BGMEntries)
        {
            if (!m_bgmDict.ContainsKey(entry.type))
                m_bgmDict.Add(entry.type, entry);
        }

        foreach (var entry in m_soundData.SEEntries)
        {
            if (!m_seDict.ContainsKey(entry.type))
                m_seDict.Add(entry.type, entry);
        }
    }

    #region BGM

    public void PlayBGM(BGMType type, bool fade = true, float? fadeTime = null)
    {
        if (!m_bgmDict.TryGetValue(type, out var entry) || entry.clip == null)
        {
            Debug.LogWarning($"BGM not found: {type}");
            return;
        }

        // 同じBGMが再生中なら何もしない
        if (m_currentBGM == type && m_bgmSource.isPlaying)
            return;

        float time = fadeTime ?? m_defaultFadeTime;

        // 既存のフェードをキャンセル
        m_bgmFadeTween?.Kill();

        if (fade && m_bgmSource.isPlaying)
        {
            // クロスフェード: 現在のBGMをフェードアウトしてから新しいBGMをフェードイン
            m_bgmFadeTween = m_bgmSource
                .DOFade(0f, time * 0.5f)
                .OnComplete(() => StartNewBGM(entry, time * 0.5f));
        }
        else
        {
            StartNewBGM(entry, fade ? time : 0f);
        }

        m_currentBGM = type;
    }

    private void StartNewBGM(SoundData.BGMEntry entry, float fadeInTime)
    {
        m_bgmSource.clip = entry.clip;
        m_bgmSource.volume = fadeInTime > 0 ? 0f : entry.volume;
        m_bgmSource.Play();

        if (fadeInTime > 0)
        {
            m_bgmFadeTween = m_bgmSource.DOFade(entry.volume, fadeInTime);
        }
    }

    public void StopBGM(bool fade = true, float? fadeTime = null)
    {
        m_bgmFadeTween?.Kill();
        m_currentBGM = null;

        if (!m_bgmSource.isPlaying)
            return;

        float time = fadeTime ?? m_defaultFadeTime;

        if (fade && time > 0)
        {
            m_bgmFadeTween = m_bgmSource
                .DOFade(0f, time)
                .OnComplete(() => m_bgmSource.Stop());
        }
        else
        {
            m_bgmSource.Stop();
        }
    }

    public void PauseBGM(bool fade = true, float? fadeTime = null)
    {
        m_bgmFadeTween?.Kill();
        float time = fadeTime ?? m_defaultFadeTime;

        if (fade && time > 0)
        {
            m_bgmFadeTween = m_bgmSource
                .DOFade(0f, time)
                .OnComplete(() => m_bgmSource.Pause());
        }
        else
        {
            m_bgmSource.Pause();
        }
    }

    public void ResumeBGM(bool fade = true, float? fadeTime = null)
    {
        if (m_currentBGM == null || !m_bgmDict.TryGetValue(m_currentBGM.Value, out var entry))
            return;

        float time = fadeTime ?? m_defaultFadeTime;
        m_bgmSource.UnPause();

        if (fade && time > 0)
        {
            m_bgmFadeTween?.Kill();
            m_bgmFadeTween = m_bgmSource.DOFade(entry.volume, time);
        }
    }

    public void SetBGMVolume(float volume, bool fade = false, float? fadeTime = null)
    {
        m_bgmFadeTween?.Kill();
        float time = fadeTime ?? m_defaultFadeTime;

        if (fade && time > 0)
        {
            m_bgmFadeTween = m_bgmSource.DOFade(volume, time);
        }
        else
        {
            m_bgmSource.volume = volume;
        }
    }

    #endregion

    #region SE

    public void PlaySE(SEType type)
    {
        if (!m_seDict.TryGetValue(type, out var entry) || entry.clip == null)
        {
            Debug.LogWarning($"SE not found: {type}");
            return;
        }

        // ラウンドロビンでAudioSourceを使用
        var source = m_sePool[currentSEIndex];
        source.PlayOneShot(entry.clip, entry.volume);

        currentSEIndex = (currentSEIndex + 1) % m_sePoolSize;
    }

    public void PlaySEWithPitch(SEType type, float pitch)
    {
        if (!m_seDict.TryGetValue(type, out var entry) || entry.clip == null)
            return;

        var source = m_sePool[currentSEIndex];
        source.pitch = pitch;
        source.PlayOneShot(entry.clip, entry.volume);
        source.pitch = 1f; // 戻す

        currentSEIndex = (currentSEIndex + 1) % m_sePoolSize;
    }

    public void StopAllSE()
    {
        foreach (var source in m_sePool)
        {
            source.Stop();
        }
    }

    #endregion

    #region Utility

    public bool IsPlayingBGM() => m_bgmSource.isPlaying;
    public BGMType? CurrentBGM => m_currentBGM;

    #endregion

    private void OnDestroy()
    {
        m_bgmFadeTween?.Kill();
    }
}