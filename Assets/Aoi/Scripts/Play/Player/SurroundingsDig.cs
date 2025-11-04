using UnityEngine;
using System.Collections;

public class SurroundingsDig : VoxelWorld.BaseAttack
{
    [SerializeField] float m_blowTime = 1.0f;
    [SerializeField] float m_blowMinVelocity = 1.0f;
    [SerializeField] float m_blowInterval = 0.1f;
    [SerializeField] Vector3 m_offset = Vector3.zero;
    Rigidbody rb;
    private Coroutine m_blowCoroutine = null;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
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
        float elapsedTime = 0f;

        while (elapsedTime < m_blowTime)
        {
            //// 速度チェック（nullチェック含む）
            //if (rb == null || rb.linearVelocity.magnitude <= m_blowMinVelocity)
            //{
            //    break;
            //}

            // Digを実行
            Dig();

            // インターバル待機
            yield return new WaitForSeconds(m_blowInterval);

            elapsedTime += m_blowInterval;
        }

        m_blowCoroutine = null;
    }

    public void Dig()
    {
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