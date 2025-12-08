using UnityEngine;
using UnityEngine.UI;

namespace Option
{
    public class ActionDataView : MonoBehaviour
    {
        [SerializeField] private Image m_image;
        [SerializeField] private Sprite m_normalSprite;
        [SerializeField] private Sprite m_selectSprite;

        public void Select()
        {
            m_image.sprite = m_selectSprite;
        }

        public void Deselect()
        {
            m_image.sprite = m_normalSprite;
        }
    } 
}
