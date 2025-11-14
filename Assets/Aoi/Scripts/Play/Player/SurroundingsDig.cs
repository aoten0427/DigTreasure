using UnityEngine;
using System.Collections;

public class SurroundingsDig : VoxelWorld.BaseAttack
{
    [SerializeField] float m_blowTime = 1.0f;
    [SerializeField] float m_blowMinVelocity = 1.0f;
    [SerializeField] float m_blowInterval = 0.1f;
    [SerializeField] Vector3 m_offset = Vector3.zero;
    Rigidbody rb;
    Collider m_collider;
    private Coroutine m_blowCoroutine = null;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        m_collider = GetComponent<Collider>();
        if (rb == null)
        {
            Debug.LogWarning("[SurroundingsDig]リジッドボディがないです");
        }
    }

    public void Blownaway()
    {
        // 既に実行中のコルーチンがあれば停止
        if (m_blowCoroutine != null)
        {
            StopCoroutine(m_blowCoroutine);
        }

        // Rigidbodyが存在する場合のみ実行
        if (rb != null)
        {
            m_blowCoroutine = StartCoroutine(BlowCoroutine());
        }
        else
        {
            Debug.LogWarning("[SurroundingsDig]Rigidbodyが存在しないためBlownawayを実行できません");
        }
    }

    private IEnumerator BlowCoroutine()
    {
        Dig();

        float elapsedTime = 0f;

        yield return null;

        Vector3 speed = rb.linearVelocity;
        speed.y = 0f;

        while (speed.magnitude >= m_blowMinVelocity&&elapsedTime < m_blowTime)
        {
            //m_collider.enabled = true;

            speed = rb.linearVelocity;
            speed.y = 0f;

            // インターバル待機
            yield return new WaitForSeconds(m_blowInterval);

            elapsedTime += m_blowInterval;

            
            //m_collider.enabled = false;


            // Digを実行Dig();
            Dig();

        }

        m_collider.enabled = true;
        m_blowCoroutine = null;

    }

    public void Dig()
    {
        Vector3 dire = rb.linearVelocity;
        dire.Normalize();
        //dire *= 2;

        AttackAtPosition(transform.position + (dire));
        AttackAtPosition(transform.position + (dire * 2f));
        AttackAtPosition(transform.position + m_offset);
    }

    // オブジェクト破棄時の安全性確保
    private void OnDestroy()
    {
        if (m_blowCoroutine != null)
        {
            StopCoroutine(m_blowCoroutine);
            m_blowCoroutine = null;
        }
    }
}