using System;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Option
{
    /// <summary>
    /// ボタンデータ
    /// </summary>
    public class ButtonData : UISelecterBase<(GameInputManager.ButtonType,GameInputManager.ActionType)>
    {

        GameInputManager m_inputManager;
        //対応したボタン
        [SerializeField] private GameInputManager.ButtonType m_buttonType;
        //使われるアクション
        [SerializeField] private GameInputManager.ActionType m_actionType;

        [SerializeField] private UISelecterBase m_selectUp;
        [SerializeField] private UISelecterBase m_selectDown;
        [SerializeField] private UISelecterBase m_selectLeft;
        [SerializeField] private UISelecterBase m_selectRight;

        [SerializeField] ButtonDataView m_view;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            m_inputManager = GameInputManager.Instance;
            if (m_inputManager == null) Debug.LogError("GameImputManagerがありません");
        }


        public void ActionChange(GameInputManager.ActionType action)
        {
            m_inputManager.ChangeAction(m_buttonType, action);
            m_view.ActionChange(action);
        }


        public override void Select(UISelecterBase back)
        {
            m_view.Select();
        }

        public override void Deselect(UISelecterBase next)
        {
            m_view.DeSelect();
        }

        public override UISelecterBase Selection(SelectionDirection direction)
        {
            return direction switch
            {
                SelectionDirection.Up => m_selectUp,
                SelectionDirection.Down => m_selectDown,
                SelectionDirection.Left => m_selectLeft,
                SelectionDirection.Right => m_selectRight,
                _ => null
            };
        }

        public override (GameInputManager.ButtonType, GameInputManager.ActionType) DecisionGenerics()
        {
            return (m_buttonType, m_actionType);
        }
    } 
}
