using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Ú‘±’†A¸”s‚Ì•\¦
/// </summary>
public class Connecting : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI m_obj;
    [SerializeField] EntranceManager m_manager;

    private void Start()
    {
        
        
        if(m_manager == null)m_manager = FindFirstObjectByType<EntranceManager>();
        m_manager.OnConnectAction += Connect;

        m_obj.gameObject.SetActive(false);
    }

    private void Connect(bool result)
    {
        if(result)
        {
            m_obj.gameObject.SetActive(true);
            m_obj.text = "‚¹‚Â‚¼‚­‚¿‚ã‚¤";
        }
    }
}
