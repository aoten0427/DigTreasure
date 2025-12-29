using System;
using UnityEngine;
using UnityEngine.Events;

namespace Option
{
    public class ActionData : UISelecterBase<GameInputManager.ActionType>
    {
        //アクションタイプ
        [SerializeField] private GameInputManager.ActionType m_actionType;
        //上の選択肢
        [SerializeField] private ActionData m_selectUp;
        //下の選択肢
        [SerializeField] private ActionData m_selectDown;
        //ビュー
        [SerializeField] private ActionDataView m_view;

        public GameInputManager.ActionType ActionType { get { return m_actionType; } }

        //ボタンが押されたら自身のタイプを返す
        public override GameInputManager.ActionType DecisionGenerics()
        {
            return m_actionType;
        }

        /// <summary>
        /// 選択肢除外
        /// </summary>
        /// <param name="next"></param>
        public override void Deselect(UISelecterBase next)
        {
            m_view.Deselect();
        }

        /// <summary>
        /// 選択された
        /// </summary>
        /// <param name="back"></param>
        public override void Select(UISelecterBase back)
        {
            m_view.Select();
        }

        /// <summary>
        /// 次のボタンを返す
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public override UISelecterBase<GameInputManager.ActionType> SelectionGenericsBase(SelectionDirection direction)
        {
            return direction switch
            {
                SelectionDirection.Up => m_selectUp,
                SelectionDirection.Down => m_selectDown,
                _ => null
            };
        }
    } 
}
