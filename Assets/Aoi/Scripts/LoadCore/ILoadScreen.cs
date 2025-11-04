using UnityEngine;

public enum LoadType
{ 
    None,
    Load1,
    Load2,
    Load3
}


public interface ILoadScreen
{
    public void Show();
    public void Hide();
    public void LoadRatio(float ratio);
    LoadType GetLoadType();
}
