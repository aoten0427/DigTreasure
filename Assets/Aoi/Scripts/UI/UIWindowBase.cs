using UnityEngine;

public abstract class UIWindowBase : MonoBehaviour
{
    public virtual void Initialize(InputGame input) { }

    public virtual void Open(UIWindowBase backWindow) { }

    public virtual void Close(UIWindowBase nextWindow) { }

    public virtual UIWindowBase Selection(SelectionDirection direction) { return null; }
}
