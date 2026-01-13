using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class KeyDataManager : MonoBehaviour
{
    [SerializeField] private int m_maxLength = 10;

    string m_text;
    public string Text { get { return m_text; } }
    public int MaxLength { get { return m_maxLength; } set { m_maxLength = value; } }

    public event Action<string> OnChangeString;
    public event Action OnDecision;

    // 濁点・半濁点の対応マップ
    private static readonly Dictionary<char, char> VoicedMap = new()
    {
        ['か'] = 'が',
        ['き'] = 'ぎ',
        ['く'] = 'ぐ',
        ['け'] = 'げ',
        ['こ'] = 'ご',
        ['さ'] = 'ざ',
        ['し'] = 'じ',
        ['す'] = 'ず',
        ['せ'] = 'ぜ',
        ['そ'] = 'ぞ',
        ['た'] = 'だ',
        ['ち'] = 'ぢ',
        ['つ'] = 'づ',
        ['て'] = 'で',
        ['と'] = 'ど',
        ['は'] = 'ば',
        ['ひ'] = 'び',
        ['ふ'] = 'ぶ',
        ['へ'] = 'べ',
        ['ほ'] = 'ぼ',
        ['カ'] = 'ガ',
        ['キ'] = 'ギ',
        ['ク'] = 'グ',
        ['ケ'] = 'ゲ',
        ['コ'] = 'ゴ',
        ['サ'] = 'ザ',
        ['シ'] = 'ジ',
        ['ス'] = 'ズ',
        ['セ'] = 'ゼ',
        ['ソ'] = 'ゾ',
        ['タ'] = 'ダ',
        ['チ'] = 'ヂ',
        ['ツ'] = 'ヅ',
        ['テ'] = 'デ',
        ['ト'] = 'ド',
        ['ハ'] = 'バ',
        ['ヒ'] = 'ビ',
        ['フ'] = 'ブ',
        ['ヘ'] = 'ベ',
        ['ホ'] = 'ボ',

    };

    private static readonly Dictionary<char, char> SemiVoicedMap = new()
    {
        ['は'] = 'ぱ',
        ['ひ'] = 'ぴ',
        ['ふ'] = 'ぷ',
        ['へ'] = 'ぺ',
        ['ほ'] = 'ぽ',
        ['ハ'] = 'パ',
        ['ヒ'] = 'ピ',
        ['フ'] = 'プ',
        ['ヘ'] = 'ペ',
        ['ホ'] = 'ポ',
    };

    // 小文字変換マップ
    private static readonly Dictionary<char, char> SmallSizeMap = new()
    {
        ['あ'] = 'ぁ',
        ['い'] = 'ぃ',
        ['う'] = 'ぅ',
        ['え'] = 'ぇ',
        ['お'] = 'ぉ',
        ['や'] = 'ゃ',
        ['ゆ'] = 'ゅ',
        ['よ'] = 'ょ',
        ['つ'] = 'っ',
        ['わ'] = 'ゎ',
        ['ア'] = 'ァ',
        ['イ'] = 'ィ',
        ['ウ'] = 'ゥ',
        ['エ'] = 'ェ',
        ['オ'] = 'ォ',
        ['ヤ'] = 'ャ',
        ['ユ'] = 'ュ',
        ['ヨ'] = 'ョ',
        ['ツ'] = 'ッ',
        ['ワ'] = 'ヮ',
    };

    // 逆引き用マップ（元の文字に戻すため）
    private static readonly Dictionary<char, char> VoicedReverseMap =
        VoicedMap.ToDictionary(kv => kv.Value, kv => kv.Key);
    private static readonly Dictionary<char, char> SemiVoicedReverseMap =
        SemiVoicedMap.ToDictionary(kv => kv.Value, kv => kv.Key);
    private static readonly Dictionary<char, char> SmallSizeReverseMap =
        SmallSizeMap.ToDictionary(kv => kv.Value, kv => kv.Key);


    /// <summary>
    /// 文字を追加（最大文字数チェックあり）
    /// </summary>
    public void AddText(char character)
    {
        // 最大文字数チェック
        if (!string.IsNullOrEmpty(m_text) && m_text.Length >= m_maxLength)
        {
            return;
        }

        m_text = m_text + character;
        OnChangeString?.Invoke(m_text);
    }

    public void RemoveText()
    {
        if (string.IsNullOrEmpty(m_text)) return;
        m_text = m_text[..^1];
        OnChangeString?.Invoke(m_text);
    }

    /// <summary>
    /// 最後の文字を変換（通常→濁点→半濁点→小文字→通常のサイクル）
    /// </summary>
    public void CycleCharacter()
    {
        // 文字列が空の場合は何もしない
        if (string.IsNullOrEmpty(m_text)) return;

        // 最後の文字を取得
        char lastChar = m_text[^1];

        // 小文字の場合は通常に戻す
        if (SmallSizeReverseMap.TryGetValue(lastChar, out char fromSmallSize))
        {
            m_text = m_text[..^1] + fromSmallSize;
            OnChangeString?.Invoke(m_text);
            return;
        }

        // 半濁点の場合は小文字に変換（可能な場合のみ）
        if (SemiVoicedReverseMap.TryGetValue(lastChar, out char fromSemiVoiced))
        {
            // 小文字に変換できる場合は変換
            if (SmallSizeMap.TryGetValue(fromSemiVoiced, out char smallSizeChar))
            {
                m_text = m_text[..^1] + smallSizeChar;
                OnChangeString?.Invoke(m_text);
            }
            else
            {
                // 小文字に変換できない場合は通常に戻す
                m_text = m_text[..^1] + fromSemiVoiced;
                OnChangeString?.Invoke(m_text);
            }
            return;
        }

        // 濁点の場合は半濁点に変換（可能な場合のみ）
        if (VoicedReverseMap.TryGetValue(lastChar, out char fromVoiced))
        {
            // 半濁点に変換できる場合は変換
            if (SemiVoicedMap.TryGetValue(fromVoiced, out char semiVoicedChar))
            {
                m_text = m_text[..^1] + semiVoicedChar;
                OnChangeString?.Invoke(m_text);
            }
            else
            {
                // 半濁点に変換できない場合は小文字に変換（可能な場合のみ）
                if (SmallSizeMap.TryGetValue(fromVoiced, out char smallSizeChar))
                {
                    m_text = m_text[..^1] + smallSizeChar;
                    OnChangeString?.Invoke(m_text);
                }
                else
                {
                    // 小文字に変換できない場合は通常に戻す
                    m_text = m_text[..^1] + fromVoiced;
                    OnChangeString?.Invoke(m_text);
                }
            }
            return;
        }

        // 通常文字の場合は濁点に変換
        if (VoicedMap.TryGetValue(lastChar, out char voicedChar))
        {
            m_text = m_text[..^1] + voicedChar;
            OnChangeString?.Invoke(m_text);
            return;
        }

        // 濁点に変換できない場合は小文字に変換
        if (SmallSizeMap.TryGetValue(lastChar, out char directSmallSize))
        {
            m_text = m_text[..^1] + directSmallSize;
            OnChangeString?.Invoke(m_text);
        }
    }

    public void Decision()
    {
        OnDecision?.Invoke();
    }
}
