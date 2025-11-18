using UnityEngine;
using System.Collections;

public class SurroundingsDig : VoxelWorld.BaseAttack
{
    [SerializeField] float m_blowTime = 1.0f;
    [SerializeField] float m_blowMinVelocity = 1.0f;
    [SerializeField] float m_blowInterval = 0.1f;
    [SerializeField] Vector3 m_offset = Vector3.zero;
    [SerializeField] float m_digDistance = 5f; // 掘る距離
    [SerializeField] int m_digSteps = 5; // 掘るステップ数

    Rigidbody rb;
    Collider m_collider;
    private Coroutine m_blowCoroutine = null;
    private Vector3 m_blowDirection; // 吹き飛ばし方向を保存

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        m_collider = GetComponent<Collider>();
        if (rb == null)
        {
            Debug.LogWarning("[SurroundingsDig]���W�b�h�{�f�B���Ȃ��ł�");
        }
    }

    public void Blownaway(PlayerCombat damaged, PlayerCombat attacker)
    {
        // 既に実行中のコルーチンがあれば停止
        if (m_blowCoroutine != null)
        {
            StopCoroutine(m_blowCoroutine);
        }

        // 攻撃者から自分への方向を計算（吹き飛ばし方向）
        Vector3 direction = (damaged.transform.position - attacker.transform.position);
        direction.y = 0f; // Y軸は無視（水平方向のみ）
        m_blowDirection = direction.normalized;

        Debug.Log($"[SurroundingsDig] 吹き飛ばし方向: {m_blowDirection}, 距離: {m_digDistance}m, ステップ: {m_digSteps}");

        // 即座に進行方向を掘る
        DigInDirection(m_blowDirection);

        // Rigidbodyが存在する場合のみ実行
        if (rb != null)
        {
            //m_blowCoroutine = StartCoroutine(BlowCoroutine());
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
            m_collider.enabled = true;

            speed = rb.linearVelocity;
            speed.y = 0f;

            // �C���^�[�o���ҋ@
            yield return new WaitForSeconds(m_blowInterval);

            elapsedTime += m_blowInterval;

            
            m_collider.enabled = false;


            // Dig�����sDig();
            Dig();
        }

        m_collider.enabled = true;
        m_blowCoroutine = null;

    }

    /// <summary>
    /// 指定方向に一定範囲を掘る
    /// </summary>
    private void DigInDirection(Vector3 direction)
    {
        Debug.Log($"[SurroundingsDig] DigInDirection開始: 方向={direction}");

        // プレイヤーの現在位置から、指定方向にm_digStepsステップ分掘る
        for (int i = 1; i <= m_digSteps; i++)
        {
            float distance = (m_digDistance / m_digSteps) * i;
            Vector3 digPosition = transform.position + direction * distance;

            // 即時更新を使用（コライダーが即座に更新される）
            AttackAtPosition(digPosition, default, immediate: true);

            Debug.Log($"[SurroundingsDig] 破壊位置 {i}/{m_digSteps}: {digPosition} (距離: {distance}m)");
        }

        // 中心のオフセット位置も破壊
        AttackAtPosition(transform.position + m_offset, default, immediate: true);
    }

    public void Dig()
    {
        Vector3 dire = rb.linearVelocity;
        dire.Normalize();

        dire = m_blowDirection;

        Debug.Log("破壊");

        // 即時更新を使用（コライダーが即座に更新される）
        AttackAtPosition(transform.position + dire, default, immediate: true);
        AttackAtPosition(transform.position + dire * 2f, default, immediate: true);
        AttackAtPosition(transform.position + dire * 3f, default, immediate: true);
        AttackAtPosition(transform.position + m_offset, default, immediate: true);
    }

    // �I�u�W�F�N�g�j�����̈��S���m��
    private void OnDestroy()
    {
        if (m_blowCoroutine != null)
        {
            StopCoroutine(m_blowCoroutine);
            m_blowCoroutine = null;
        }
    }
}