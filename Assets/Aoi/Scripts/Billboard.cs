using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera m_camera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_camera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        transform.forward = m_camera.transform.forward;
    }
}
