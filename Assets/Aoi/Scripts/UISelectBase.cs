using System;
using UnityEngine;

/// <summary>
/// 選択されるUI
/// </summary>
[System.Serializable]
public abstract class UISelecterBase : MonoBehaviour
{
    public enum SelectionDirection
    {
        Up, Down, Left, Right
    }

    //選択された
    public virtual void Select(UISelecterBase back) { }
    //選択から外れた
    public virtual void Deselect(UISelecterBase next) { }
    //次に選択されるもの
    public virtual UISelecterBase Selection(SelectionDirection direction) { return null; }
    //決定された
    public virtual void Decision() { }
}

public abstract class UISelecterBase<TResult> : UISelecterBase
{
    //次に選択されるもの
    public virtual UISelecterBase<TResult> SelectionGenerics(SelectionDirection direction) { return null; }
    //決定
    public virtual TResult DecisionGenerics() { return default; }
}



