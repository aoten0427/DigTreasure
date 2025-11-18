using Fusion;
using System.Globalization;
using UnityEngine;

public class PlayerProto : NetworkBehaviour
{
    //名前
    [Networked] public NetworkString<_16> NickName { get; set; }

    //動作可能か
    bool m_isAction = true;

    //リジッドボディ
    private Rigidbody rb;
    //コライダー
    private Collider playerCollider;
    //移動可能か
    private bool canMove = true;


    [SerializeField] Dig[] m_digs = new Dig[3];
    //掘り可能か
    private bool canDig = true;

    [Header("リジッドボディセッティング")]
    [SerializeField] private float moveSpeed = 5f;//移動速度
    [SerializeField] private float maxSpeed = 10f; //最大速度
    [SerializeField] private float jumpForce = 5f;//ジャンプ力
    [SerializeField] private float groundCheckDistance = 1.5f;//床の当たり判定
    [SerializeField] private LayerMask groundLayer;//地面レイヤー
    [SerializeField] private float drag = 5f;

    private bool isJump = false;//ジャンプフラグ
    private bool isGrounded;//地面についているか
    private Vector2 m_moveInput;//移動量

    //ロックオン
    private bool needLockOnRot = false;
    private Quaternion lockOnRot;

    [SerializeField] PlayerCombat m_combat;
    [SerializeField] SurroundingsDig m_surroundingsDig;

    private void OnEnable()
    {
        PlayerCombat.OnPlayerStunStart += DisableMove;
        PlayerCombat.OnPlayerStunEnd += EnableMove;
        PlayerCombat.OnPlayerBarrierStart += DisableMove;
        PlayerCombat.OnPlayerBarrierStart += DisableDig;
        PlayerCombat.OnPlayerBarrierEnd += EnableMove;
        PlayerCombat.OnPlayerBarrierEnd += EnableDig;
    }
    private void OnDisable()
    {
        PlayerCombat.OnPlayerStunStart -= DisableMove;
        PlayerCombat.OnPlayerStunEnd -= EnableMove;
        PlayerCombat.OnPlayerBarrierStart -= DisableMove;
        PlayerCombat.OnPlayerBarrierStart -= DisableDig;
        PlayerCombat.OnPlayerBarrierEnd -= EnableMove;
        PlayerCombat.OnPlayerBarrierEnd -= EnableDig;

        if (m_combat != null)
        {
            m_combat.OnPlayerDamage -= Blownaway;
        }

        //入力処理削除
        {
            var inputmanager = GameInputManager.Instance;
            inputmanager.Move -= Move;
            inputmanager.Jump -= Jump;
            inputmanager.DigUp -= DigUp;
            inputmanager.DigDown -= DigDown;
            inputmanager.Attack -= DigAttack;
        }
    }
    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true; // プレイヤーが倒れないように
        }

        playerCollider = GetComponent<Collider>();

        var view = GetComponent<PlayerViewProto>();
        view.SetNickName(NickName.Value);
        if (HasStateAuthority)
        {
            view.MakeCameraTarget();
        }

        if (m_combat != null)
        {
            m_combat.OnPlayerDamage += Blownaway;
        }

        //入力処理初期化
        {
            var inputmanager = GameInputManager.Instance;
            inputmanager.Move += Move;
            inputmanager.Jump += Jump;
            inputmanager.DigUp += DigUp;
            inputmanager.DigDown += DigDown;
            inputmanager.Attack += DigAttack;
        }
    }

    public void Move(Vector2 move)
    {
        m_moveInput = move;
    }

    public void Jump(bool ispush)
    {
        if (ispush&&isGrounded && !isJump && m_isAction)
        {
            isJump = true;
        }
    }

    public void DigUp(bool ispush)
    {
        if (ispush) Dig(m_digs[2], m_digs[2].transform.position);
    }

    public void DigAttack(bool ispush)
    {
        if (ispush) Dig(m_digs[1], m_digs[1].transform.position);
    }

    public void DigDown(bool ispush)
    {
        if (ispush) Dig(m_digs[0], m_digs[0].transform.position);
    }

    public void SetPlayManager(PlayManager playManager)
    {
        if (playManager == null) return;
        m_isAction = false;
        playManager.OnGameStartAction += () => m_isAction = true;
        playManager.OnGameEndAction += () => m_isAction = false;
    }

    private void Update()
    {
        if (!Object.HasStateAuthority) return;
        if (!m_isAction) return;

        // 地面判定
        CheckGrounded();

        //// TODO: コントローラー対応
        //// Jキーで掘る(テスト用にキーボード対応)
        //if (Input.GetKeyDown(KeyCode.J) && canDig)
        //{
        //    Dig(m_digs[0], m_digs[0].transform.position);
        //}
        //if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !isJump)
        //{
        //    isJump = true;
        //}
    }

    public override void FixedUpdateNetwork()
    {
        if (rb == null) return;
        if (!HasStateAuthority) return;
        if (!m_isAction) return;

        //Debug.Log("入力受付");



        var cameraRotation = Quaternion.Euler(0f, Camera.main.transform.rotation.eulerAngles.y, 0f);

        if (canMove)
        {
            // 移動入力
            var inputDirection = new Vector3(m_moveInput.x, 0f, m_moveInput.y);
            Vector3 moveDirection = cameraRotation * inputDirection;

            // 現在の水平速度を取得
            Vector3 currentVelocity = rb.linearVelocity;
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);

            // 水平移動
            if (moveDirection.magnitude > 0.1f)
            {
                // 最大速度化
                if (horizontalVelocity.magnitude < maxSpeed)
                {
                    Vector3 force = moveDirection.normalized * moveSpeed;
                    rb.AddForce(force, ForceMode.Force);
                }
            }
            else
            {
                // 摩擦
                Vector3 dragForce = -horizontalVelocity * drag;
                rb.AddForce(dragForce, ForceMode.Force);
            }

            // 回転 - Rigidbodyで制御
            if (needLockOnRot)
            {
                rb.MoveRotation(lockOnRot);
            }
            else if (moveDirection.magnitude > 0.1f)
            {
                // 移動方向を向く
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, Runner.DeltaTime * 10f);
                rb.MoveRotation(newRotation);
            }

            // ジャンプ
            if (isJump)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isJump = false;
            }
        }
        else
        {
            // スタン中は速度を減衰
            Vector3 velocity = rb.linearVelocity;
            velocity.x *= 0.9f;
            velocity.z *= 0.9f;
            rb.linearVelocity = velocity;
            Debug.Log("Stunned");
        }
    }

    private void CheckGrounded()
    {
        if (playerCollider == null)
        {
            isGrounded = false;
            return;
        }

        // Colliderの底の中心位置
        Vector3 boundsBottom = playerCollider.bounds.center - new Vector3(0, playerCollider.bounds.extents.y, 0);
        boundsBottom.y += 0.3f;

        // Colliderのサイズから適切なオフセット距離を計算（端より少し内側）
        float horizontalOffset = playerCollider.bounds.extents.x * 0.8f;
        float forwardOffset = playerCollider.bounds.extents.z * 0.8f;

        // 5箇所のRaycast開始位置
        Vector3[] rayPositions = new Vector3[5]
        {
                boundsBottom,                                              // 中央
                boundsBottom + transform.forward * forwardOffset,          // 前
                boundsBottom - transform.forward * forwardOffset,          // 後
                boundsBottom + transform.right * horizontalOffset,         // 右
                boundsBottom - transform.right * horizontalOffset          // 左
        };

        // いずれか1つでも地面に接していればtrue
        isGrounded = false;
        RaycastHit hit;

        for (int i = 0; i < rayPositions.Length; i++)
        {
            bool hitGround = Physics.Raycast(rayPositions[i], Vector3.down, out hit, groundCheckDistance, groundLayer);

            // デバッグ用の視覚化
            Debug.DrawRay(rayPositions[i], Vector3.down * groundCheckDistance, hitGround ? Color.green : Color.red);

            if (hitGround)
            {
                isGrounded = true;
                break;
            }
        }
    }

    private void Dig(Dig dig,Vector3 point)
    {
        if (dig == null) return;
        dig.DigPoint(point, transform.position - point + new Vector3(0, 1, 0));

        // pointが掘る位置(プレイヤーの前方、高さは足元)になってる
    }

    //機能オン・オッフ
    private void DisableMove()
    {
        canMove = false;
    }
    private void EnableMove()
    {
        canMove = true;
    }
    private void DisableDig()
    {
        canDig = false;
    }
    private void EnableDig()
    {
        canDig = true;
    }

    //ロックオン
    public void SetRotateTarget(bool lockOn, Quaternion targetRot)
    {
        needLockOnRot = lockOn;
        lockOnRot = targetRot;
    }

    private void Blownaway(PlayerCombat damaged,PlayerCombat attacker)
    {
        //m_surroundingsDig.Blownaway(damaged, attacker);
    }
}
