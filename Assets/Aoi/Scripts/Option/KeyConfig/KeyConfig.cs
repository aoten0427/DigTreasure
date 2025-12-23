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
        bool m_isActive = false;
        //選択しているボタン
        [SerializeField] private UISelecterBase m_select;

        [SerializeField] ActionWindow m_actionWindow;

        [SerializeField] UIWindowBase m_nextWindow;
        [SerializeField] UIWindowBase m_backWindow;

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
        public override void Initialize(InputGame input)
        {
            Close(null);
            m_input = input;
            if (m_input != null)
            {
                m_input.Normal.Up.started += ctx => InputDirection(SelectionDirection.Up);
                m_input.Normal.Down.started += ctx => InputDirection(SelectionDirection.Down);
                m_input.Normal.Left.started += ctx => InputDirection(SelectionDirection.Left);
                m_input.Normal.Right.started += ctx => InputDirection(SelectionDirection.Right);
                m_input.Normal.Select.started += ctx => ButtonSelect();
                m_input.Normal.Enable();
            }
            m_actionWindow.Initialize(m_input);
            m_actionWindow.OnSelectAction = DecisionButtonActon;
        }



        public override void Open(UIWindowBase backWindow)
        {
            m_isActive = true;
            gameObject.SetActive(true);
            m_select.Select(null);
        }

        public override void Close(UIWindowBase nextWindow)
        {
            m_isActive = false;
            m_actionWindow.Close();
            if (nextWindow) gameObject.SetActive(false);
        }

        /// <summary>
        /// 選択
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public override UIWindowBase Selection(SelectionDirection direction)
        {
            switch (direction)
            {
                case SelectionDirection.Left:
                    if (m_backWindow) return m_backWindow;
                    return this;
                case SelectionDirection.Right:
                    if (m_nextWindow) return m_nextWindow;
                    return this;
                default:
                    return this;

            }
        }

        /// <summary>
        /// 操作ボタン選択
        /// </summary>
        /// <param name="direction"></param>
        private void InputDirection(SelectionDirection direction)
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
