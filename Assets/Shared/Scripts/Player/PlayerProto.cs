using Fusion;
using NetWork;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XInput;

public class PlayerProto : NetworkBehaviour
{
    //名前
    [Networked] public NetworkString<_16> NickName { get; set; }

    //動作可能か
    bool m_isAction = true;

    //リジッドボディ
    private Rigidbody rb;

    [SerializeField] Dig[] m_digs = new Dig[3];
    //掘り可能か
    private bool canDig = true;

    private bool isJump = false;//ジャンプフラグ
    private Vector2 m_moveInput;//移動量

    [SerializeField] PlayerMovement m_movement;
    [SerializeField] PlayerCombat m_combat;
    [SerializeField] SurroundingsDig m_surroundingsDig;

    GameLauncher m_gameLauncher;

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
        if (HasStateAuthority)
        {
            var inputmanager = GameInputManager.Instance;
            if(inputmanager != null)
            {
                inputmanager.Move -= Move;
                inputmanager.Jump -= Jump;
                inputmanager.DigUp -= DigUp;
                inputmanager.DigDown -= DigDown;
                inputmanager.Attack -= DigAttack;
            }
           
        }
    }
    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.freezeRotation = true; // プレイヤーが倒れないように
        }

        // PlayerMovement の初期化
        if (m_movement != null)
        {
            m_movement.Initialize(rb, GetComponent<Collider>());
        }

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


        if(Object.HasStateAuthority)
        {
            //入力処理初期化
            var inputmanager = GameInputManager.Instance;
            inputmanager.Move += Move;
            inputmanager.Jump += Jump;
            inputmanager.DigUp += DigUp;
            inputmanager.DigDown += DigDown;
            inputmanager.Attack += DigAttack;


            m_digs[0].OnDestroyAction(DigUpdate);
            m_digs[1].OnDestroyAction(DigUpdate);
            m_digs[2].OnDestroyAction(DigUpdate);
        }




        m_gameLauncher = GameLauncher.Instance;
    }

    public void Move(Vector2 move)
    {
        m_moveInput = move;
    }

    public void Jump(bool ispush)
    {
        if (ispush && m_movement != null && m_movement.IsGrounded && !isJump && m_isAction)
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
        playManager.OnGameStartAction += Active;
        playManager.OnGameEndAction += Inactive;
    }

    private void Active()
    {
        m_isAction = true;
    }

    private void Inactive()
    {
        m_isAction = false;
    }

    private void Update()
    {
        if (!Object.HasStateAuthority) return;
        if (!m_isAction) return;

        // 地面判定
        if (m_movement != null)
        {
            m_movement.CheckGrounded();
        }

        //// TODO: コントローラー対応
        //// Jキーで掘る(テスト用にキーボード対応)
        //if (Input.GetKeyDown(KeyCode.J) && canDig)
        //{
        //    Dig(m_digs[0], m_digs[0].transform.position);
        //}
        //if (Input.GetKeyDown(KeyCode.Space) && m_movement.IsGrounded && !isJump)
        //{
        //    isJump = true;
        //}
    }

    public override void FixedUpdateNetwork()
    {
        if (rb == null) return;
        if (!HasStateAuthority) return;
        if (!m_isAction) return;
        if (m_movement == null) return;

        var cameraRotation = Quaternion.Euler(0f, Camera.main.transform.rotation.eulerAngles.y, 0f);

        // 移動処理を PlayerMovement に委譲
        m_movement.ProcessMovement(m_moveInput, cameraRotation, Runner.DeltaTime);

        // ジャンプ処理を PlayerMovement に委譲
        m_movement.ProcessJump(isJump);
        isJump = false;
    }

    private void Dig(Dig dig,Vector3 point)
    {
        if (dig == null) return;
        dig.DigPoint(point, transform.position - point + new Vector3(0, 1, 0));

    }

    //機能オン・オッフ
    private void DisableMove()
    {
        if (m_movement != null)
        {
            m_movement.DisableMovement();
        }
    }
    private void EnableMove()
    {
        if (m_movement != null)
        {
            m_movement.EnableMovement();
        }
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
        if (m_movement != null)
        {
            m_movement.SetLockOnRotation(lockOn, targetRot);
        }
    }

    private void Blownaway(PlayerCombat damaged,PlayerCombat attacker)
    {
        //m_surroundingsDig.Blownaway(damaged, attacker);
    }

    public void DigUpdate(int digCount)
    {
        var userdata = m_gameLauncher.UserData;
        userdata.m_digPoint += digCount;
        m_gameLauncher.UserData = userdata;

        Debug.Log(userdata.m_digPoint);

        if(digCount >= 1000)
        {
            float low = Mathf.Min(((float)digCount / (float)1500), 0.2f);
            float high = Mathf.Min(((float)digCount / (float)2500), 0.8f);
            float duration = Mathf.Min(0.5f, Mathf.Max((float)digCount / (float)2000), 0.1f);
            StartCoroutine(Rumble(low, high, 0.1f));
        }

       
        

    }

    IEnumerator Rumble(float low,float high,float duration)
    {
        var gamepad = Gamepad.current as XInputController;
        if (gamepad == null) yield break;
       
        gamepad.SetMotorSpeeds(low, high);
        yield return new WaitForSeconds(duration);
        gamepad.SetMotorSpeeds(0, 0);
        
    }
}
