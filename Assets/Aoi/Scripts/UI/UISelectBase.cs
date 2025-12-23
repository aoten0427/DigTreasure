using System;
using UnityEngine;

public enum SelectionDirection
{
    Up, Down, Left, Right, None
}

/// <summary>
/// 選択されるUI
/// </summary>
[System.Serializable]
public abstract class UISelecterBase : MonoBehaviour
{
    //選択された
    public virtual void Select(UISelecterBase back) { }
    //選択から外れた
    public virtual void Deselect(UISelecterBase next) { }
    //次に選択されるもの
    public virtual UISelecterBase Selection(SelectionDirection direction) { return null; }
    //決定された
    public virtual void Decision() { }
    //操作
    public virtual void Operation(SelectionDirection direction) { }
    //ロックするか
    public virtual bool IsLock() { return false; }
}

public abstract class UISelecterBase<TResult> : UISelecterBase
{
    //次に選択されるもの
    public virtual UISelecterBase<TResult> SelectionGenerics(SelectionDirection direction) { return null; }
    //決定
    public virtual TResult DecisionGenerics() { return default; }
}



