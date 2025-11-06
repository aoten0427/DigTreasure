using UnityEngine;

public class NameSetting : MonoBehaviour
{
    [SerializeField] Camera m_camera;
    [SerializeField] Transform[] m_playerPositions = new Transform[4];
    [SerializeField] RectTransform[] m_namePositions = new RectTransform[4];
    [SerializeField] Canvas m_canvas;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for(int i = 0;i  < m_playerPositions.Length;i++)
        {
            Vector3 pos = m_camera.WorldToScreenPoint(m_playerPositions[i].position);
            m_namePositions[i].position = pos;
        }
    }
}
