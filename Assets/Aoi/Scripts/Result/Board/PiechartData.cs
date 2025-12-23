using TMPro;
using UnityEngine;

public class PiechartData : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI m_name;
    [SerializeField] ResultPoint m_point;
    [SerializeField] TextMeshProUGUI m_ratio;



    public void SetData(string name,int point,int ratio)
    {
        if(m_name)m_name.text = name;
        if(m_point)m_point.SetScore(point);
        if(m_ratio)m_ratio.text = $"{ratio}%";
    }

    public void SetData(int point,int ratio)
    {
        if (m_point) m_point.SetScore(point);
        if (m_ratio) m_ratio.text = $"{ratio}%";
    }
}
