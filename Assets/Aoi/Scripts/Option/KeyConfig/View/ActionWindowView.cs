using DG.Tweening;
using Option;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ActionWindowView : MonoBehaviour
{
    [SerializeField] float m_openTime = 0.2f;
    [SerializeField] Ease m_openEase;
    [SerializeField] float m_closeTime = 0.2f;
    [SerializeField] Ease m_closeEase;

    [SerializeField]Transform m_transform;
    [SerializeField] Image m_buttonImage;
    [SerializeField] TextMeshProUGUI m_text;

    [SerializeField]

    [System.Serializable]
    private class ButtoSprite
    { 
        public GameInputManager.ButtonType buttonType;
        public Sprite sprite;
    }

    [SerializeField]private List<ButtoSprite> m_sprites = new List<ButtoSprite>();

    private void Start()
    {
        m_transform.localScale = new Vector3(1, 0, 1);
    }

    

    public void Open(GameInputManager.ButtonType buttonType, GameInputManager.ActionType actionType, bool isleft)
    {
        //テキスト変更
        m_text.text = KeyConfig.ActionName[actionType];
        //画像変更
        var foundSprite = m_sprites.FirstOrDefault(x => x.buttonType == buttonType);
        if(foundSprite != null) { m_buttonImage.sprite = foundSprite.sprite; }
        //大きさ変更
        m_transform.DOScale(Vector3.one, m_openTime).SetEase(m_openEase);
    }

    public void Close()
    {
        //大きさ変更
        m_transform.DOScale(new Vector3(1,0,1), m_closeTime).SetEase(m_closeEase);
    }
    
}
