using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour
{
    [SerializeField] PlayManager m_playMaanger;
    [SerializeField] Image m_minute;
    [SerializeField] Image m_secondTen;
    [SerializeField] Image m_secondOne;
    [SerializeField] Sprite[] m_numbers = new Sprite[10];

    void Start()
    {
        // ƒQ[ƒ€‚ÌƒJƒEƒ“ƒgƒ_ƒEƒ“‚É“o˜^
        m_playMaanger.GameTimer
            .Subscribe(value => CountDown(value))
            .AddTo(this);
    }

    void CountDown(double timer)
    {
        // ¬”‚ğØ‚èÌ‚Ä‚Ä®”•b‚É•ÏŠ·
        int totalSeconds = Mathf.Max(0, (int)Math.Floor(timer));

        // •ª‚Æ•b‚ğŒvZ
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        // \‚ÌˆÊ‚Æˆê‚ÌˆÊ‚Ì•b‚ğ•ª‰ğ
        int secTen = seconds / 10;
        int secOne = seconds % 10;

        // ‚»‚ê‚¼‚ê‚ÌImage‚É”š‚ğƒZƒbƒg
        if (m_numbers.Length >= 10)
        {
            m_minute.sprite = m_numbers[Mathf.Clamp(minutes, 0, 9)];
            m_secondTen.sprite = m_numbers[secTen];
            m_secondOne.sprite = m_numbers[secOne];
        }
    }
}
