using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Collections;

namespace Option
{
    /// <summary>
    /// オプション画面を管理
    /// </summary>
    public class OptionManager : MonoBehaviour
    {
        // シングルトンインスタンス
        public static OptionManager Instance { get; private set; }

        // インプット
        InputGame m_input;
        // ビュー
        [SerializeField] OptionBaseView m_optionBaseView;
        // 現在開いているウインド
        private UIWindowBase m_currentWindow;
        // 各ウインド
        [SerializeField] List<UIWindowBase> m_windows;
        // アクティブか
        bool m_isActive = false;

        public bool IsActive { get { return m_isActive; } }

        private void Awake()
        {
            // シングルトン処理
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // 入力生成
            m_input = new InputGame();
            if (m_optionBaseView == null) m_optionBaseView = GetComponent<OptionBaseView>();

            // ウインド初期化
            foreach (UIWindowBase win in m_windows)
            {
                win.Initialize(m_input);
            }

            // ウインド設定
            if (m_windows.Count > 0)
            {
                m_currentWindow = m_windows[0];
            }
            else
            {
                Debug.LogError("[OptionManager]ウインドがありません");
            }

            m_input.Normal.LTrigger.performed += ctx => InputDirection(SelectionDirection.Left);
            m_input.Normal.RTrigger.performed += ctx => InputDirection(SelectionDirection.Right);
        }

        private void OnDestroy()
        {
            // シングルトン解除
            if (Instance == this)
            {
                Instance = null;
            }

            // 入力システムの破棄
            if (m_input != null)
            {
                m_input.Disable();
                m_input.Dispose();
            }
        }

        /// <summary>
        /// 開く
        /// </summary>
        public void Open()
        {
            if (m_isActive) return;
            m_isActive = true;
            m_optionBaseView.Open();
            if (m_currentWindow) m_currentWindow.Open(null);
        }

        /// <summary>
        /// 閉じる
        /// </summary>
        public void Close()
        {
            if (!m_isActive) return;
            m_isActive= false;
            //StartCoroutine(Delay(0.1f, () => m_isActive = false));
            m_optionBaseView.Close();
            if (m_currentWindow) m_currentWindow.Close(null);
        }

        IEnumerator Delay(float time,Action action)
        {
            yield return new WaitForSeconds(time);
            action?.Invoke();
        }

        /// <summary>
        /// ウインド変更
        /// </summary>
        private void InputDirection(SelectionDirection direction)
        {
            if (!m_isActive) return;
            var next = m_currentWindow.Selection(direction);
            if (next == null || next == m_currentWindow) return;
            m_currentWindow.Close(next);
            next.Open(m_currentWindow);
            m_currentWindow = next;
        }
    }
}
