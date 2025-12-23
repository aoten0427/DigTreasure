using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoradData : MonoBehaviour
{
    //ランク画像
    [SerializeField] Sprite[] m_rankSprites = new Sprite[4];
    //ランクImage
    [SerializeField] Image m_rankImage;
    //名前
    [SerializeField] TextMeshProUGUI m_name;
    //トータルスコア
    [SerializeField] ResultPoint m_totalPoint;
    //たからスコア
    [SerializeField] ResultPoint m_treasurePoint;
    //はかいスコア
    [SerializeField] ResultPoint m_destroyPoint;
    
    void Start()
    {
        
    }



    /// <summary>
    /// データセット
    /// </summary>
    /// <param name="rank"></param>
    /// <param name="name"></param>
    /// <param name="totalpoint"></param>
    /// <param name="treasurepoint"></param>
    /// <param name="destroypoint"></param>
    public void SetData(int rank,string name,int totalpoint,int treasurepoint,int destroypoint)
    {
        m_rankImage.sprite = m_rankSprites[rank];
        m_name.text = name;
        m_totalPoint.SetScore(totalpoint);
        m_treasurePoint.SetScore(treasurepoint);
        m_destroyPoint.SetScore(destroypoint);
    }
}
