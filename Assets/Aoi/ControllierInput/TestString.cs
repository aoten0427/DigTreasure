using TMPro;
using UnityEngine;

public class TestString : MonoBehaviour
{
    [SerializeField] KeyDataManager m_manager;
    [SerializeField]TextMeshProUGUI m_textMeshProUGUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_manager.OnChangeString += Change;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void Change(string text)
    {
        m_textMeshProUGUI.text = text;
    }
}
