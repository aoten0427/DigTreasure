using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class NameInput : MonoBehaviour
{
    //キーボード
    [SerializeField] KeyDataManager m_keyDataManager;
    [SerializeField] KeyDataCenter m_keyDataCenter;
    //名前
    [SerializeField] TextMeshProUGUI m_nameText;
    //
    [SerializeField] GameObject m_hideObject;

    bool m_isOpen = false;
    public bool IsOpen => m_isOpen;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_keyDataCenter.IsActive = false;
        m_keyDataCenter.gameObject.SetActive(false);
        m_keyDataManager.OnChangeString += Change;
        m_keyDataManager.OnDecision += Close;
    }

    private void OnDestroy()
    {
        if(m_keyDataManager != null)
        {
            m_keyDataManager.OnChangeString -= Change;
            m_keyDataManager.OnDecision -= Close;
        }
    }



    public void Open()
    {
        m_isOpen = true;
        m_keyDataCenter.IsActive = true;
        m_keyDataCenter.gameObject.SetActive(true);
    }

    public void Close()
    {
        StartCoroutine(Delay(0.1f,() => m_isOpen = false));
        m_keyDataCenter.IsActive = false;
        m_keyDataCenter.gameObject.SetActive(false);
    }

    IEnumerator Delay(float time,Action action = null)
    {
        yield return new WaitForSeconds(time);
        action?.Invoke();
    }

    void Change(string text)
    {
        if(text.Length <= 0)
        {
            m_nameText.text = "";
            m_hideObject.SetActive(true);
        }
        else{
            m_hideObject.SetActive(false);
            m_nameText.text = text;
        }
        
    }
}
