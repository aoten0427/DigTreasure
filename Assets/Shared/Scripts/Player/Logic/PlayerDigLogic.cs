using UnityEngine;

/// <summary>
/// プレイヤー掘りロジッククラス
/// </summary>
public class PlayerDigLogic : MonoBehaviour
{
    [Header("掘りポイント")]
    [SerializeField] private Dig[] m_digs = new Dig[3];

    //PlayerManager参照
    private PlayerManager m_manager;

    //掘り可能フラグ
    private bool m_canDig = true;

    //掘り方向定数
    public const int DIG_DOWN = 0;
    public const int DIG_FORWARD = 1;
    public const int DIG_UP = 2;

    //プロパティ
    public bool CanDig
    {
        get => m_canDig;
        set => m_canDig = value;
    }

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize(PlayerManager manager)
    {
        m_manager = manager;

        //掘り完了コールバック設定
        if (m_digs[0] != null) m_digs[0].OnDestroyAction(OnDigComplete);
        if (m_digs[1] != null) m_digs[1].OnDestroyAction(OnDigComplete);
        if (m_digs[2] != null) m_digs[2].OnDestroyAction(OnDigComplete);

        //イベント購読
        if (m_manager?.Events != null)
        {
            m_manager.Events.OnBarrierStart += DisableDig;
            m_manager.Events.OnBarrierEnd += EnableDig;
        }
    }

    private void OnDestroy()
    {
        //イベント解除
        if (m_manager?.Events != null)
        {
            m_manager.Events.OnBarrierStart -= DisableDig;
            m_manager.Events.OnBarrierEnd -= EnableDig;
        }
    }

    /// <summary>
    /// 上方向に掘る
    /// </summary>
    public void DigUp()
    {
        if (!m_canDig) return;
        Dig(m_digs[DIG_UP], DIG_UP);
    }

    /// <summary>
    /// 前方に掘る
    /// </summary>
    public void DigForward()
    {
        if (!m_canDig) return;
        Dig(m_digs[DIG_FORWARD], DIG_FORWARD);
    }

    /// <summary>
    /// 下方向に掘る
    /// </summary>
    public void DigDown()
    {
        if (!m_canDig) return;
        Dig(m_digs[DIG_DOWN], DIG_DOWN);
    }

    /// <summary>
    /// 掘り実行
    /// </summary>
    private void Dig(Dig dig, int direction)
    {
        if (dig == null) return;

        Vector3 digPoint = dig.transform.position;
        Vector3 digDirection = transform.position - digPoint + new Vector3(0, 1, 0);

        dig.DigPoint(digPoint, digDirection);

        //イベント発火
        m_manager?.Events?.InvokeDig(direction);
    }

    /// <summary>
    /// 掘り完了コールバック
    /// </summary>
    public void OnDigComplete(int digCount)
    {
        //インベントリに掘りポイント追加
        m_manager?.InventoryLogic?.AddDigPoints(digCount);

        //カメラシェイク
        m_manager?.CameraController?.PlayDigShake(digCount);

        //振動処理
        if (digCount >= 1000)
        {
            float low = Mathf.Min(((float)digCount / 1500f), 0.2f);
            float high = Mathf.Min(((float)digCount / 2500f), 0.8f);
            float duration = Mathf.Clamp((float)digCount / 2000f, 0.1f, 0.5f);
            StartCoroutine(RumbleCoroutine(low, high, duration));
        }
    }

    /// <summary>
    /// 振動コルーチン
    /// </summary>
    private System.Collections.IEnumerator RumbleCoroutine(float low, float high, float duration)
    {
        var gamepad = UnityEngine.InputSystem.Gamepad.current as UnityEngine.InputSystem.XInput.XInputController;
        if (gamepad == null) yield break;

        gamepad.SetMotorSpeeds(low, high);
        yield return new WaitForSeconds(duration);
        gamepad.SetMotorSpeeds(0, 0);
    }

    /// <summary>
    /// 掘り有効化
    /// </summary>
    public void EnableDig()
    {
        m_canDig = true;
    }

    /// <summary>
    /// 掘り無効化
    /// </summary>
    public void DisableDig()
    {
        m_canDig = false;
    }
}
