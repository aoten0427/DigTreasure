using UnityEngine;
using UnityEngine.UI;

public class SoundBarView : OptionSelectButtonView
{

    //変動するスライド
    [SerializeField]
    Image m_barInside;
    //位置を示すポイント
    [SerializeField]
    RectTransform m_barPoint;
    //ポイントの補正値
    [SerializeField]
    float m_correctionValue = 0.0f;



    public void Change(float value)
    {
        value = Mathf.Clamp(value,0,1);
        m_barInside.fillAmount = value;
        if(m_barPoint != null )
        {
            //補正値割合
            float par = (value - 0.5f) / 0.5f;
            //補正値をかけたpivot位置を計算
            float pivotx = value + (m_correctionValue * par);
            m_barPoint.pivot = new Vector2 (pivotx, m_barPoint.pivot.y);
        }
    }
}
