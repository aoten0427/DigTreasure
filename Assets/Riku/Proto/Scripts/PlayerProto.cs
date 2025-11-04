using Fusion;
using System.Globalization;
using UnityEngine;

public class PlayerProto : NetworkBehaviour
{
    //名前
    [Networked] public NetworkString<_16> NickName { get; set; }
    //掘る場所
    [SerializeField] private GameObject digPoint;

    //動作可能か
    bool m_isAction = true;
    
    //リジッドボディ
    private Rigidbody rb;
    //コライダー
    private Collider playerCollider;
    //移動可能か
    private bool canMove = true;
    [SerializeField] private bool testingStun = false; //スタン機能オン・オッフ


    [SerializeField] private Dig m_dig;
    [SerializeField] private GameObject m_digpoint2;

    [Header("リジッドボディセッティング")]
    [SerializeField] private float moveSpeed = 5f;//移動速度
    [SerializeField] private float maxSpeed = 10f; //最大速度
    [SerializeField] private float jumpForce = 5f;//ジャンプ力
    [SerializeField] private float groundCheckDistance = 1.5f;//床の当たり判定
    [SerializeField] private LayerMask groundLayer;//地面レイヤー
    [SerializeField] private float drag = 5f; 

    private bool isJump = false;//ジャンプフラグ

    private bool isGrounded;//地面についているか

    //ロックオン
    private bool needLockOnRot = false;
    private Quaternion lockOnRot;

    [SerializeField]PlayerCombat m_combat;
    [SerializeField]SurroundingsDig m_surroundingsDig;

    private void OnEnable()
    {
        if (testingStun)
        {
            PlayerCombat.OnPlayerStunStart += StartStun;
            PlayerCombat.OnPlayerStunEnd += EndStun;
        }
    }
    private void OnDisable()
    {
        if (testingStun)
        {
            PlayerCombat.OnPlayerStunStart -= StartStun;
            PlayerCombat.OnPlayerStunEnd -= EndStun;
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
        
        if(m_combat != null)
        {
            m_combat.OnPlayerAttack += () =>
            {
                Dig(m_digpoint2.transform.position);
            };
            //m_combat.OnPlayerDamage += () =>
            //{
            //    m_surroundingsDig.Blownaway();
            //};
        }
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

        // TODO: コントローラー対応
        // Jキーで掘る(テスト用にキーボード対応)
        if (Input.GetKeyDown(KeyCode.J))
        {
            Dig(digPoint.transform.position);
        }
        if(Input.GetKeyDown(KeyCode.Space)&&isGrounded&&!isJump)
        {
            isJump = true;
        }
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
            var inputDirection = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
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
                rb.AddForce(Vector3.up * jumpForce,ForceMode.Impulse);
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
        // Colliderの底から判定開始位置を計算
        Vector3 rayStart = transform.position;
        float rayDistance = groundCheckDistance;

        if (playerCollider != null)
        {
            // Colliderの底の位置を取得
            rayStart = playerCollider.bounds.center - new Vector3(0, playerCollider.bounds.extents.y - rayDistance, 0);
            rayDistance = groundCheckDistance; // 底からさらに下へ
        }

        // 足元から少し下に向けてRaycastで地面判定
        RaycastHit hit;
        isGrounded = Physics.Raycast(rayStart, Vector3.down, out hit, rayDistance, groundLayer);

        // デバッグ情報
        if (Input.GetKey(KeyCode.Space))
        {
            Debug.Log($"[CheckGrounded] isGrounded: {isGrounded}, RayStart: {rayStart}, Distance: {(hit.collider ? hit.distance.ToString() : "No Hit")}, Layer: {(hit.collider ? LayerMask.LayerToName(hit.collider.gameObject.layer) : "None")}");
        }
    }

    private void OnDrawGizmos()
    {
        // 地面判定のRayを可視化
        Gizmos.color = isGrounded ? Color.green : Color.red;

        Vector3 startPos = transform.position;
        if (playerCollider != null)
        {
            // Colliderの底から開始
            startPos = playerCollider.bounds.center - new Vector3(0, playerCollider.bounds.extents.y, 0);
        }

        Vector3 endPos = startPos + Vector3.down * groundCheckDistance;
        Gizmos.DrawLine(startPos, endPos);
        Gizmos.DrawWireSphere(endPos, 0.1f);
        Gizmos.DrawWireSphere(startPos, 0.05f); // 開始位置も表示
    }

    private void Dig(Vector3 point)
    {
        if (m_dig == null) return;
        m_dig.DigPoint(point,transform.position - point + new Vector3(0,1,0));

        // pointが掘る位置(プレイヤーの前方、高さは足元)になってる
    }

    //スタン
    private void StartStun()
    {
        canMove = false;
    }
    private void EndStun()
    {
        canMove = true;
    }

    //ロックオン
    public void SetRotateTarget(bool lockOn, Quaternion targetRot)
    {
        needLockOnRot = lockOn;
        lockOnRot = targetRot;
    }
}
