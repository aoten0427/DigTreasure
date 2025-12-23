using Fusion;
using UnityEngine;

/// <summary>
/// プレイヤーの統括管理クラス（エントリーポイント）
/// 全コンポーネントへの参照保持・初期化を担当
/// オフライン/オンライン両対応
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [Header("モード設定")]
    [SerializeField] private bool m_isOnlineMode = true;

    [Header("ネットワーク")]
    [SerializeField] private PlayerNetworkState m_networkState;

    [Header("入力")]
    [SerializeField] private PlayerInputHandler m_inputHandler;

    [Header("ロジック")]
    [SerializeField] private PlayerMovementLogic m_movementLogic;
    [SerializeField] private PlayerCombatLogic m_combatLogic;
    [SerializeField] private PlayerBarrierLogic m_barrierLogic;
    [SerializeField] private PlayerInventoryLogic m_inventoryLogic;
    [SerializeField] private PlayerDigLogic m_digLogic;

    [Header("表示")]
    [SerializeField] private PlayerView m_view;
    [SerializeField] private PlayerAnimator m_animator;

    [Header("カメラ")]
    [SerializeField] private PlayerCameraController m_cameraController;

    [Header("物理")]
    [SerializeField] private Rigidbody m_rigidbody;
    [SerializeField] private Collider m_collider;

    [SerializeField]private bool m_isActive = false;


    //イベントシステム
    private PlayerEvents m_events;

    //プロパティ
    public bool IsOnlineMode => m_isOnlineMode;
    public PlayerEvents Events => m_events;
    public PlayerNetworkState NetworkState => m_networkState;
    public PlayerInputHandler InputHandler => m_inputHandler;
    public PlayerMovementLogic MovementLogic => m_movementLogic;
    public PlayerCombatLogic CombatLogic => m_combatLogic;
    public PlayerBarrierLogic BarrierLogic => m_barrierLogic;
    public PlayerInventoryLogic InventoryLogic => m_inventoryLogic;
    public PlayerDigLogic DigLogic => m_digLogic;
    public PlayerView View => m_view;
    public PlayerAnimator Animator => m_animator;
    public PlayerCameraController CameraController => m_cameraController;
    public Rigidbody Rigidbody => m_rigidbody;
    public Collider Collider => m_collider;


    public bool NetworkStateAuthority { get {
            return IsOnlineMode && m_networkState != null && m_networkState.NetworkStateAuthority;
        } }

    public bool IsActive {  get { return m_isActive; } set { m_isActive = value; } }

    private void Awake()
    {
        //イベントシステム初期化
        m_events = new PlayerEvents();

        //物理コンポーネント取得
        if (m_rigidbody == null)
        {
            m_rigidbody = GetComponent<Rigidbody>();
        }
        if (m_collider == null)
        {
            m_collider = GetComponent<Collider>();
        }

        //オフライン時はNetworkStateを無効化
        if (!m_isOnlineMode && m_networkState != null)
        {
            m_networkState.enabled = false;
        }
    }

    

    /// <summary>
    /// 初期化（オンラインモードではSpawned後に呼ばれる）
    /// </summary>
    public void Initialize(bool isLocalPlayer)
    {
        //各ロジッククラスを初期化
        if (m_movementLogic != null)
        {
            m_movementLogic.Initialize(this);
        }
        if (m_combatLogic != null)
        {
            m_combatLogic.Initialize(this);
        }
        if (m_barrierLogic != null)
        {
            m_barrierLogic.Initialize(this);
        }
        if (m_inventoryLogic != null)
        {
            m_inventoryLogic.Initialize(this);
        }
        if (m_digLogic != null)
        {
            m_digLogic.Initialize(this);
        }

        //入力ハンドラ初期化（ローカルプレイヤーのみ）
        if (m_inputHandler != null && isLocalPlayer)
        {
            m_inputHandler.Initialize(this);
        }

        //表示初期化
        if (m_view != null)
        {
            m_view.Initialize(this, isLocalPlayer);
        }
        if (m_animator != null)
        {
            m_animator.Initialize(this);
        }

        //カメラ初期化
        if (m_cameraController != null)
        {
            m_cameraController.Initialize(this, isLocalPlayer);
        }
    }

    /// <summary>
    /// オフラインモードで開始
    /// </summary>
    public void StartOffline()
    {
        m_isOnlineMode = false;
        if (m_networkState != null)
        {
            m_networkState.enabled = false;
        }
        Initialize(true);
    }

    /// <summary>
    /// オンラインモードを設定
    /// </summary>
    public void SetOnlineMode(bool isOnline)
    {
        m_isOnlineMode = isOnline;
        if (m_networkState != null)
        {
            m_networkState.enabled = isOnline;
        }
    }

    private void OnDestroy()
    {
        //イベントクリア
        if (m_events != null)
        {
            m_events.ClearAllEvents();
        }
    }
}
