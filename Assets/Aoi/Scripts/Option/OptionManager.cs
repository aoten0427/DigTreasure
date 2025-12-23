using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Option
{
    public class OptionManager : MonoBehaviour
    {
        //インプット
        InputGame m_input;
        //ビュー
        [SerializeField] OptionBaseView m_optionBaseView;
        //現在開いているウインド
        private UIWindowBase m_currentWindow;
        //各ウインド
        [SerializeField] List<UIWindowBase> m_windows;
        //アクティブ化
        bool m_isActive = false;

        private void Start()
        {
            //入力生成
            m_input = new InputGame();
            if (m_optionBaseView == null) m_optionBaseView = GetComponent<OptionBaseView>();

            //ウインド初期化
            foreach(UIWindowBase win in m_windows)
            {
                win.Initialize(m_input);
            }

            //ウインド設定
            if(m_windows.Count > 0)
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

        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.Alpha1))
            {
                Open();
            }
            if (Input.GetKeyUp(KeyCode.Alpha2))
            {
                Close();
            }
        }

        /// <summary>
        /// 開く
        /// </summary>
        public void Open()
        {
            m_isActive = true;
            m_optionBaseView.Open();
            if (m_currentWindow) m_currentWindow.Open(null);
        }

        /// <summary>
        /// 閉じる
        /// </summary>
        public void Close()
        {
            m_isActive=false;
            m_optionBaseView.Close();
            if (m_currentWindow) m_currentWindow.Close(null);
        }

        /// <summary>
        /// ウインド変更
        /// </summary>
        /// <param name="direction"></param>
        private void InputDirection(SelectionDirection direction)
        {
            if (!m_isActive) return;
            var next = m_currentWindow.Selection(direction);
            if(next == null||next == m_currentWindow) return;
            m_currentWindow.Close(next);
            next.Open(m_currentWindow);
            m_currentWindow = next;
        }
    } 
}
