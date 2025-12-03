using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Option
{
    public class KeyConfig : UIWindowBase
    {
        //インプット
        InputGame m_input;

        //アクティブ化
        bool m_isActive = true;
        //選択しているボタン
        [SerializeField] private UISelecterBase m_select;

        [SerializeField] ActionWindow m_actionWindow;

        //アクションと対応した名前
        static Dictionary<GameInputManager.ActionType, string> m_actionName = new Dictionary<GameInputManager.ActionType, string> {
            {GameInputManager.ActionType.None,"なし" },
            {GameInputManager.ActionType.Attack,"こうげき" },
            {GameInputManager.ActionType.DigUp,"うえほり" },
            {GameInputManager.ActionType.DigDown,"したほり" },
             {GameInputManager.ActionType.Jump,"ジャンプ" },
            {GameInputManager.ActionType.Guard,"ガード" },
            {GameInputManager.ActionType.LookOn,"ロックオン" }
        };
        public static IReadOnlyDictionary<GameInputManager.ActionType, string> ActionName { get { return m_actionName; } }

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="input"></param>
        public void Initailize(InputGame input)
        {
            m_input = input;
            if(m_input != null)
            {
                m_input.Normal.Up.started += ctx => InputDirection(UISelecterBase.SelectionDirection.Up);
                m_input.Normal.Down.started += ctx => InputDirection(UISelecterBase.SelectionDirection.Down);
                m_input.Normal.Left.started += ctx => InputDirection(UISelecterBase.SelectionDirection.Left);
                m_input.Normal.Right.started += ctx => InputDirection(UISelecterBase.SelectionDirection.Right);
                m_input.Normal.Select.started += ctx => ButtonSelect();
                m_input.Normal.Enable();
            }
            m_actionWindow.Initialize(m_input);
            m_actionWindow.OnSelectAction = DecisionButtonActon;
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
        }

        public void Open()
        {
            m_isActive = true;
            m_select.Select(null);
        }

        public void Close()
        {
            m_isActive= false;
        }

        /// <summary>
        /// 操作ボタン選択
        /// </summary>
        /// <param name="direction"></param>
        private void InputDirection(UISelecterBase.SelectionDirection direction)
        {
            if (m_select == null|!m_isActive) return;
            var next = m_select.Selection(direction);
            if (next == m_select || next == null)
            {
                return;
            }
            else
            {
                m_select.Deselect(next);
                var temp = m_select;
                m_select = next;
                m_select.Select(temp);
            }
        }

        /// <summary>
        /// ボタン決定(変更するボタンを決定)
        /// </summary>
        private void ButtonSelect()
        {
            if(m_select == null||!m_isActive) return;
            bool isleft = m_select.GetComponent<RectTransform>().anchoredPosition.x < 0;

            if(m_select is UISelecterBase<(GameInputManager.ButtonType,GameInputManager.ActionType)> configbutton)
            {
                var data = configbutton.DecisionGenerics();
                m_actionWindow.Open(this);
                m_actionWindow.Open(data.Item1, data.Item2, isleft);

                m_isActive = false;
            }
            else
            {
                m_select.Decision();
            }
        }

        private void DecisionButtonActon(GameInputManager.ActionType action)
        {
            
            if (m_select == null) return;
            if(m_select is ButtonData  buttonData)
            {
                buttonData.ActionChange(action);
            }
            StartCoroutine(ActiveChange(true));
        }

        /// <summary>
        /// ボタン検知の1フレーム遅延用
        /// </summary>
        /// <param name="active"></param>
        /// <returns></returns>
        IEnumerator ActiveChange(bool active)
        {
            yield return null;
            m_isActive = active;
        }
    } 
}
