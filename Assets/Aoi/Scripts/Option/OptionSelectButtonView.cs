using UnityEngine;
using UnityEngine.UI;

public class OptionSelectButtonView : MonoBehaviour
{
    [SerializeField] Image m_selectImage;

    public void Select(bool isSelect)
    {
        if (isSelect)
        {
            m_selectImage.enabled = true;
        }
        else
        {
            m_selectImage.enabled = false;
        }
    }
}
