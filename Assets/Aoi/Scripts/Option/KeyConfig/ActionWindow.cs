using NUnit.Framework;
using Option;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using System.Collections;

/// <summary>
/// コンフィグのアクション選択
/// </summary>
public class ActionWindow : UIWindowBase
{
    //インプット
    InputGame m_input;
    //アクティブフラグ
    bool m_isActive = false;

    //ボタン
    [SerializeField]
    List<ActionData> m_actionDatas = new List<ActionData>();
    //選択中のボタン
    private UISelecterBase<GameInputManager.ActionType> m_currentData;

    //選択を決定したときに呼ばれるアクション
    private Action<GameInputManager.ActionType> m_selectAction;
    public Action<GameInputManager.ActionType> OnSelectAction { get { return m_selectAction; } set { m_selectAction = value; } }

    //ビュー
    [SerializeField]ActionWindowView m_view;


    public override void Initialize(InputGame input)
    {
        if (m_actionDatas.Count == 0) return;
        m_currentData = m_actionDatas[0];

        m_input = input;
        if(input != null)
        {
            input.Normal.Up.started += ctx => InputDirection(SelectionDirection.Up);
            input.Normal.Down.started += ctx => InputDirection(SelectionDirection.Down);
            input.Normal.Select.started += ctx => ButtonSelect();
        }
    }

    /// <summary>
    /// 開く
    /// </summary>
    /// <param name="buttonType"></param>
    /// <param name="actionType"></param>
    /// <param name="isleft"></param>
    public void Open(GameInputManager.ButtonType buttonType,GameInputManager.ActionType actionType,bool isleft)
    {
        //データ検索
        var data = m_actionDatas.FirstOrDefault(value => value.ActionType == actionType);
        if (data == null) return;
        m_currentData = data;
        //選択
        m_currentData.Select(null);
        //ビューopen
        m_view.Open(buttonType, actionType, isleft);
        //アクティブ変更(ボタン検知のため1フレーム遅らせる)
        StartCoroutine(ActiveChange(true));
    }

    IEnumerator ActiveChange(bool active)
    {
        yield return null;
        m_isActive = active;
    }

    /// <summary>
    /// 閉じる
    /// </summary>
    public void Close()
    {
        m_isActive = false;
        if(m_currentData)m_currentData.Deselect(null);
        m_view.Close();
    }

    /// <summary>
    /// 選択変更
    /// </summary>
    /// <param name="direction"></param>
    private void InputDirection(SelectionDirection direction)
    {
        if(m_currentData== null||!m_isActive) return;
        var next = m_currentData.SelectionGenericsBase(direction);
        if (m_currentData == next||next == null)
        {
            return;
        }
        else
        {
            m_currentData.Deselect(next);
            var temp = m_currentData;
            m_currentData = next;
            m_currentData.Select(temp);
        }
    }

    /// <summary>
    /// ボタン決定(使うアクションを決定)
    /// </summary>
    private void ButtonSelect()
    {
        if (m_currentData == null || !m_isActive) return;
        var actiontype = m_currentData.DecisionGenerics();
        OnSelectAction?.Invoke(actiontype);
        Close();
    }
}
