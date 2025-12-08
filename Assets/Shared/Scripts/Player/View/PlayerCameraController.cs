using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// プレイヤーカメラ制御クラス
/// </summary>
public class PlayerCameraController : MonoBehaviour
{
    [Header("カメラシェイク")]
    [SerializeField] private CinemachineImpulseSource m_impulseSource;
    [SerializeField] private float m_shakeForce = 1f;

    [Header("掘りシェイク")]
    [SerializeField] private float m_digShakeForce = 0.3f;
    [SerializeField] private int m_digShakeThreshold = 500;

    //PlayerManager参照
    private PlayerManager m_manager;

    //ローカルプレイヤーフラグ
    private bool m_isLocalPlayer;

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize(PlayerManager manager, bool isLocalPlayer)
    {
        m_manager = manager;
        m_isLocalPlayer = isLocalPlayer;

        Debug.Log($"[CameraController] Initialize: isLocalPlayer={isLocalPlayer}, impulseSource={m_impulseSource}");

        //イベント購読
        if (m_manager?.Events != null)
        {
            m_manager.Events.OnHitStopStart += OnHitStopStart;
            Debug.Log("[CameraController] OnHitStopStart イベント購読完了");
        }
    }

    private void OnDestroy()
    {
        //イベント解除
        if (m_manager?.Events != null)
        {
            m_manager.Events.OnHitStopStart -= OnHitStopStart;
        }
    }

    /// <summary>
    /// ヒットストップ開始時のカメラシェイク
    /// </summary>
    private void OnHitStopStart(float duration)
    {
        Debug.Log($"[CameraController] OnHitStopStart: duration={duration}, isLocalPlayer={m_isLocalPlayer}");

        //ローカルプレイヤーのみシェイク
        if (!m_isLocalPlayer)
        {
            Debug.Log("[CameraController] ローカルプレイヤーではないためスキップ");
            return;
        }

        StartCoroutine(Delay());
    }

    IEnumerator Delay(float wait = 0.2f)
    {
        yield return new WaitForSeconds(wait);
        PlayShake(m_shakeForce);
    }

    /// <summary>
    /// カメラシェイク実行
    /// </summary>
    public void PlayShake(float force)
    {
        Debug.Log($"[CameraController] PlayShake: force={force}, impulseSource={m_impulseSource}");

        if (m_impulseSource != null)
        {
            m_impulseSource.GenerateImpulseWithForce(force);
            Debug.Log("[CameraController] GenerateImpulseWithForce 実行完了");
        }
        else
        {
            Debug.LogWarning("[CameraController] m_impulseSource が null です");
        }
    }

    /// <summary>
    /// 掘りシェイク実行
    /// </summary>
    public void PlayDigShake(int digCount)
    {
        if (!m_isLocalPlayer) return;
        if (digCount < m_digShakeThreshold) return;

        //掘り量に応じてシェイク強度を調整
        float force = Mathf.Lerp(m_digShakeForce * 0.5f, m_digShakeForce,
            Mathf.Clamp01((float)(digCount - m_digShakeThreshold) / 1000f));
        PlayShake(force);
    }
}
