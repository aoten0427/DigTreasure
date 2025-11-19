using UnityEngine;

public class DamageAnim : MonoBehaviour
{

    private Camera m_camera;
    Vector3 m_initialPosition;
    [SerializeField] float m_offset = 2.0f;

    void Start()
    {
        m_camera = Camera.main;
        m_initialPosition = transform.position;
    }

    void Update()
    {
        // ƒJƒƒ‰‚Ì•ûŒü‚ğŒü‚­
        transform.forward = m_camera.transform.forward;

        // ‰ŠúˆÊ’u ¨ ƒJƒƒ‰•ûŒü
        Vector3 dirToCamera = (m_camera.transform.position - m_initialPosition).normalized;

        // ‚»‚Ì•ûŒü‚É offset •ª‚¾‚¯ˆÚ“®
        transform.position = m_initialPosition + dirToCamera * m_offset;
    }

    public void OnDamageAnimEnd()
    {
        Destroy(gameObject);
    }
}
