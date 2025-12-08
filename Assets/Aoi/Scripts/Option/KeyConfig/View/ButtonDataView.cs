using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameInputManager;

namespace Option
{
    /// <summary>
    /// ボタンデータビュー
    /// </summary>
    public class ButtonDataView : MonoBehaviour
    {
        

        [SerializeField] Image m_image;
        [SerializeField] Sprite m_nomalSprite;
        [SerializeField] Sprite m_selectSprite;

        [SerializeField] TextMeshProUGUI m_text;


        public void ActionChange(GameInputManager.ActionType action)
        {
            m_text.text = KeyConfig.ActionName[action];
        }

        public void Select()
        {
            m_text.color = Color.white;
            m_image.sprite = m_selectSprite;
        }

        public void DeSelect()
        {
            m_text.color = Color.black;
            m_image.sprite = m_nomalSprite;
        }
    } 
}
