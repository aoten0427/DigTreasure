using System.Collections;
using TMPro;
using UnityEngine;

public class InfomationText : MonoBehaviour
{
    [SerializeField] EntranceManager m_manager;
    [SerializeField] TextMeshProUGUI m_text;
    [SerializeField] GameObject m_dot;
    bool m_isUpdateFlag = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_text.gameObject.SetActive(false);
        m_dot.SetActive(false);
        m_manager.OnConnectAction += Conection;
    }

    public void UpdateText(string text,bool isDotShow = false)
    {
        m_text.text = text;
        m_text.gameObject.SetActive(true);
        m_dot.SetActive(isDotShow);
        m_isUpdateFlag = true;
        StartCoroutine(Delay());
    }

    IEnumerator Delay()
    {
        yield return null;
        m_isUpdateFlag = false;
    }

    public void Hide()
    {
        if (m_isUpdateFlag) return;
        m_text.gameObject.SetActive(false);
        m_dot.SetActive(false);
    }

    void Conection(bool isConect)
    {
        if(isConect)
        {
            UpdateText("‚¹‚Â‚¼‚­‚¿‚ã‚¤",true);
        }
        else
        {
            UpdateText("‚¹‚Â‚¼‚­‚µ‚Á‚Ï‚¢");
        }
    }
}
