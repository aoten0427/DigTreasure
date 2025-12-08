using UnityEngine;
using System.Collections;

/// <summary>
/// プレイヤーバリアーロジッククラス
/// </summary>
public class PlayerBarrierLogic : MonoBehaviour
{
    [Header("バリアー設定")]
    [SerializeField] private float m_barrierRadius = 2f;
    [SerializeField] private float m_barrierCooldown = 3f;
    [SerializeField] private float m_barrierBtnHoldDuration = 0.3f;

    //PlayerManager参照
    private PlayerManager m_manager;

    //状態
    private bool m_isBarrierActive;
    private bool m_barrierPressed;
    private bool m_barrierBtnWaiting = true;
    private Coroutine m_barrierCoroutine;
    private bool m_isOnCooldown;

    //プロパティ
    public bool IsBarrierActive => m_isBarrierActive;
    public float BarrierRadius => m_barrierRadius;

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize(PlayerManager manager)
    {
        m_manager = manager;
    }

    private void Update()
    {
        //バリアーボタン長押し処理
        if (m_barrierPressed)
        {
            if (m_barrierBtnWaiting && m_barrierCoroutine == null && !m_isOnCooldown)
            {
                m_barrierCoroutine = StartCoroutine(BufferBarrierButton());
            }
            else if (!m_barrierBtnWaiting && m_barrierCoroutine != null)
            {
                ActivateBarrier();
            }
        }
    }

    /// <summary>
    /// バリアー開始
    /// </summary>
    public void StartBarrier()
    {
        m_barrierPressed = true;
    }

    /// <summary>
    /// バリアー終了
    /// </summary>
    public void EndBarrier()
    {
        m_barrierPressed = false;
        DeactivateBarrier();
    }

    /// <summary>
    /// バリアー発動
    /// </summary>
    private void ActivateBarrier()
    {
        if (m_isBarrierActive) return;

        m_isBarrierActive = true;
        m_barrierBtnWaiting = true;

        //ネットワーク同期
        if (m_manager != null && m_manager.IsOnlineMode && m_manager.NetworkState != null)
        {
            m_manager.NetworkState.SetBarrierActive(true);
        }
        else
        {
            m_manager?.Events?.InvokeBarrierStart();
        }
    }

    /// <summary>
    /// バリアー解除
    /// </summary>
    private void DeactivateBarrier()
    {
        if (!m_isBarrierActive) return;

        m_isBarrierActive = false;

        //ネットワーク同期
        if (m_manager != null && m_manager.IsOnlineMode && m_manager.NetworkState != null)
        {
            m_manager.NetworkState.SetBarrierActive(false);
        }
        else
        {
            m_manager?.Events?.InvokeBarrierEnd();
        }

        //クールダウン開始
        if (m_barrierCoroutine != null)
        {
            StopCoroutine(m_barrierCoroutine);
        }
        m_barrierCoroutine = StartCoroutine(BarrierCooldown());
    }

    /// <summary>
    /// バリアーボタンバッファリング
    /// </summary>
    private IEnumerator BufferBarrierButton()
    {
        m_barrierBtnWaiting = true;
        yield return new WaitForSeconds(m_barrierBtnHoldDuration);
        m_barrierBtnWaiting = false;
    }

    /// <summary>
    /// バリアークールダウン
    /// </summary>
    private IEnumerator BarrierCooldown()
    {
        m_isOnCooldown = true;
        yield return new WaitForSeconds(m_barrierCooldown);
        m_isOnCooldown = false;
        m_barrierCoroutine = null;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, m_barrierRadius);
    }
}
