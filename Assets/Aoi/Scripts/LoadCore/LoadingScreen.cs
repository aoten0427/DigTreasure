using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour,ILoadScreen
{
    [SerializeField] Image m_loadingGage;

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void LoadRatio(float ratio)
    {
        m_loadingGage.fillAmount = ratio;
    }

    public LoadType GetLoadType()
    {
        return LoadType.Load1;
    }
}
