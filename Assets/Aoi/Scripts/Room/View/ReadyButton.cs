using TMPro;
using UnityEngine;

public class ReadyButton : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI m_text;
    [SerializeField] string m_readyText;
    [SerializeField] string m_cancelText;

    bool m_ready = false;

    public void Push()
    {
        m_ready = !m_ready;
        if(m_ready)
        {
            
            m_text.text = m_cancelText;
        }
        else
        {
            m_text.text = m_readyText;
        }
    }
}
