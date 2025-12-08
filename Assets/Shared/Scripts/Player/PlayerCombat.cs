using UnityEngine;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using System;

public class PlayerCombat : NetworkBehaviour
{
    //ロックオン
    [Header("ロックオン")]
    [SerializeField] private float _lockOnRange;
    private float _lockOnRangeMod => _lockTarget && _lockTarget._barrierActive ? _barrierRadius : 0f;
    private PlayerCombat _lockTarget = null;
    [SerializeField] private float _lookSpeed;
    private bool _canLockOn = true;
    [SerializeField] private GameObject _lockOnIcon;

    //ノックバック
    [Header("ノックバック")]
    [SerializeField] private float _knockbackForce = 10f; //AddForce用の力の大きさ

    //スタン
    [Header("スタン")]
    [SerializeField] private float _stunDuration;
    private IEnumerator _stunCoroutine = null;
    private float _stunRemaining = 0f;
    [SerializeField] private StunAnim _stunAnim;
    public static event System.Action OnPlayerStunStart;
    public static event System.Action OnPlayerStunEnd;

    //宝落とす
    [Header("宝落とす")]
    [SerializeField][Range(0, 1)] private float _treasureLostPercent;
    [SerializeField] private Vector2 _treasureDropRadiusRange;
    [SerializeField] private Treasure _treasurePrefab;
    private float _treasureHeight;
    private PlayerInventory _inventory;
    private NetworkTreasureSpawner _treasureSpawner;
    private TreasureList _treasureList;

    //バリアー
    [Header("バリアー")]
    [SerializeField] private float _barrierRadius;
    [SerializeField] private float _barrierCD;
    [HideInInspector] public bool _barrierActive => _barrierModel.isActiveAndEnabled;
    [SerializeField] private float _barrierBtnHoldDuration;
    private bool _barrierBtnWaiting = true;
    private IEnumerator _barrierCoroutine = null;
    private bool _barrierPressed = false;
    [SerializeField] private NetworkObject _barrierModel;
    public static event System.Action OnPlayerBarrierStart;
    public static event System.Action OnPlayerBarrierEnd;

    //ダメージ受けCT
    [Header("ダメージ受けCT")]
    [SerializeField] private float _ImmunityDuration;
    [Networked] private NetworkBool _immune { get; set; } = false;

    //ダメージエフェクト
    [Header("ダメージエフェクト")]
    [SerializeField] private DamageAnim _damageAnimPrefab;
    private CapsuleCollider _collider;
    private Vector3 _playerCenter => transform.TransformPoint(_collider.center);

    //攻撃データ
    PlayerDamage _damageData;

    //他
    private PlayerCombat _attackTarget = null;
    private PlayerProto _playerProto;
    private Rigidbody _rb;
    private bool _isAttacking = false;
    private bool _canAttack = true;

    public event Action OnPlayerAttack;
    public event Action<PlayerCombat, PlayerCombat> OnPlayerDamage;



    private void Awake()
    {
        _treasureList = Resources.Load<TreasureList>("Treasure/TreasureList");
        _barrierModel.transform.localScale *= _barrierRadius;
        _collider = GetComponent<CapsuleCollider>();
        
    }
    private void OnEnable()
    {
        OnPlayerBarrierStart += DisableAttack;
        OnPlayerBarrierStart += DisableLockOn;
        OnPlayerBarrierEnd += EnableAttack;
        OnPlayerBarrierEnd += EnableLockOn;
        OnPlayerStunStart += DisableAttack;
        OnPlayerStunEnd += EnableAttack;
        OnPlayerDamage += UntargetDamagedPlayer;
    }

    private void OnDisable()
    {
        OnPlayerBarrierStart -= DisableAttack;
        OnPlayerBarrierStart -= DisableLockOn;
        OnPlayerBarrierEnd -= EnableAttack;
        OnPlayerBarrierEnd -= EnableLockOn;
        OnPlayerStunStart -= DisableAttack;
        OnPlayerStunEnd -= EnableAttack;
        OnPlayerDamage -= UntargetDamagedPlayer;

        //入力処理削除
        if (HasStateAuthority)
        {
            var inputmanager = GameInputManager.Instance;
            inputmanager.LookOn -= LockOn;
            inputmanager.Attack -= Attack;
            inputmanager.Barrier -= Barrier;
        }
    }

    public override void Spawned()
    {
        base.Spawned();
        if (HasStateAuthority)
        {
            //攻撃データ作成
            _damageData = new PlayerDamage();
            _damageData.SetAttacker(this);
            _damageData.KnockBackForce = _knockbackForce;
            _damageData.StunDuration = _stunDuration;
            _damageData.IsDirect = true;

            _playerProto = GetComponent<PlayerProto>();
            _rb = GetComponent<Rigidbody>();
            _inventory = GetComponent<PlayerInventory>();
            _treasureHeight = _treasurePrefab.GetComponent<Collider>().bounds.extents.y * 2f;
            _treasureSpawner = FindFirstObjectByType<NetworkTreasureSpawner>(FindObjectsInactive.Include);
            RpcToggleBarrierModel(false);

            //入力処理初期化
            if (Object.HasStateAuthority)
            {
                var inputmanager = GameInputManager.Instance;
                if(inputmanager != null)
                {
                    inputmanager.LookOn += LockOn;
                    inputmanager.Attack += Attack;
                    inputmanager.Barrier += Barrier;
                }
                
            }
        }


    }


    private void Update()
    {
        if (!HasStateAuthority)
            return;

        if (_lockTarget
            && Vector3.Distance(transform.position, _lockTarget.transform.position) > _lockOnRange + _lockOnRangeMod)
            ChangeLockTarget(null);

        //バリアー
        if (_barrierPressed)
        {
            if (_barrierBtnWaiting && _barrierCoroutine == null)
            {
                _barrierCoroutine = BufferBarrierButton();
                StartCoroutine(_barrierCoroutine);
            }
            else if (!_barrierBtnWaiting && _barrierCoroutine != null)
                StartBarrier();
        }


        HandleRotation();
    }
    private void LockOn(bool ispush)
    {
        if (!_canLockOn || !ispush) return;

        //距離が優先、次は角
        Collider[] nearPlayers = Physics.OverlapSphere(transform.position, _lockOnRange + _barrierRadius);
        if (nearPlayers.Length == 0)
        {
            ChangeLockTarget(null);
            return;
        }

        float nearestDist = float.MaxValue;
        float smallestAngle = float.MaxValue;
        PlayerCombat newLockTarget = _lockTarget ? _lockTarget : null;
        foreach (Collider coll in nearPlayers)
        {
            if (coll.gameObject == gameObject || (_lockTarget && coll.gameObject == _lockTarget.gameObject)
                || !coll.TryGetComponent(out PlayerCombat p2) || p2._immune)
                continue;

            Vector3 toTarget = p2.transform.position - transform.position;
            float dist = toTarget.magnitude;
            if (dist > _lockOnRange + (p2._barrierActive ? _barrierRadius : 0f))
                continue;
            float angle = Vector3.Angle(transform.forward, toTarget);

            if ((dist < nearestDist)
                || (Mathf.Abs(dist - nearestDist) <= 0.02f
                && angle < smallestAngle))
            {
                newLockTarget = p2;
                nearestDist = dist;
                smallestAngle = angle;
            }
        }
        ChangeLockTarget(newLockTarget);
    }
    private bool ChangeLockTarget(PlayerCombat newTarget)
    {
        if (_lockTarget)
            _lockTarget._lockOnIcon.SetActive(false);
        _lockTarget = newTarget;
        if (_lockTarget)
            _lockTarget._lockOnIcon.SetActive(true);
        return _lockTarget != null;

    }
    private void UntargetDamagedPlayer(PlayerCombat damaged, PlayerCombat attacker)
    {
        if (_lockTarget && _lockTarget == damaged)
            ChangeLockTarget(null);
    }
    private void HandleRotation()
    {
        Quaternion targetRot = transform.rotation;
        bool needRot = false;
        if (_attackTarget)
        {
            targetRot = Quaternion.LookRotation((_attackTarget.transform.position - transform.position).normalized, transform.up);
            needRot = true;
            _attackTarget = null;
        }
        else if (_lockTarget && !_isAttacking)
        {
            targetRot = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation((_lockTarget.transform.position - transform.position).normalized, transform.up), Time.deltaTime * _lookSpeed);
            needRot = true;
        }
        _playerProto.SetRotateTarget(needRot, targetRot);
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RpcTakeDamage(PlayerDamage damage)
    {
        PlayerCombat attacker = damage.GetAttacker(Runner);
        if (!attacker)
        {
            Debug.LogWarning("アタッカーがいません");
            return;
        }
            
        //バリアを貼っていて直接攻撃ならアタッカーをはじく
        if (_barrierActive&&damage.IsDirect)
        {
            attacker.RpcBarrierEffect(this);
            return;
        }
        OnPlayerDamage?.Invoke(this, attacker);
        LoseTreasure();
        StunPlayer();
        DamageAnimation(attacker._playerCenter);
        Knockback(damage.Direction,damage.KnockBackForce);
        StartCoroutine(StartImmunity());
    }

    private void Knockback(Vector3 dir,float force)
    {
        StartCoroutine(Delya(dir, force));
    }

    IEnumerator Delya(Vector3 dir, float force)
    {

        yield return new WaitForSeconds(0.1f);
        if (_rb != null)
        {
            Debug.Log("ノックバック");
            // 瞬間的な力を加える
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            _rb.AddForce(dir * force, ForceMode.Impulse);
        }
    }

    private void StunPlayer()
    {
        _stunRemaining = _stunDuration;
        if (_stunCoroutine == null)
        {
            _stunCoroutine = ToggleStun();
            StartCoroutine(_stunCoroutine);
        }
    }
    private IEnumerator ToggleStun()
    {
        OnPlayerStunStart?.Invoke();
        RpcToggleStunAnim(true);
        while (_stunRemaining >= 0f)
        {
            _stunRemaining -= Time.deltaTime;
            yield return null;
        }
        if (_stunRemaining <= 0f)
        {
            _stunRemaining = 0f;
            OnPlayerStunEnd?.Invoke();
            RpcToggleStunAnim(false);
            _stunCoroutine = null;
        }
    }


    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RpcToggleStunAnim(bool visible)
    {
        _stunAnim.gameObject.SetActive(visible);
    }
    private void DamageAnimation(Vector3 attackerCenter)
    {
        Debug.DrawLine(attackerCenter, _playerCenter);
        Physics.Raycast(attackerCenter, (_playerCenter - attackerCenter).normalized, out RaycastHit hitInfo, float.MaxValue, LayerMask.GetMask("Player"));
        Vector3 spawnPos = hitInfo.point;
        Vector3 spawnDir = (_playerCenter - Camera.main.transform.position).normalized;
        RpcSpawnDamageAnim(spawnPos, Quaternion.LookRotation(spawnDir), this);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RpcSpawnDamageAnim(Vector3 spawnPos, Quaternion spawnDir, PlayerCombat spawnParent)
    {
        Instantiate(_damageAnimPrefab, spawnPos, spawnDir);
    }
    private IEnumerator StartImmunity()
    {
        _immune = true;
        yield return new WaitForSeconds(_ImmunityDuration);
        _immune = false;
    }
    private void LoseTreasure()
    {
        int amtLost = (int)(_treasureLostPercent * (float)(_inventory.AmountOfTreasure()));
        Debug.Log($"パーセント:{_treasureLostPercent},全個数:{_inventory.AmountOfTreasure()},落ちる個数:{amtLost}");
        List<TreasureSO> treasureLost = _inventory.GetRandomTreasures(amtLost);
        float spawnHeight = transform.position.y + _treasureHeight;
        for (int i = 0; i < treasureLost.Count; i++)
        {
            Vector3 spawnPos = UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(_treasureDropRadiusRange.x, _treasureDropRadiusRange.y) + transform.position;
            spawnPos.y = spawnHeight;
            _treasureSpawner.DropTreasure(transform.position + new Vector3(0,2,0), treasureLost[i].point, _treasureList.allTreasure.IndexOf(treasureLost[i]));
            _inventory.RemoveTreasure(treasureLost[i], 1);
        }
    }

    //インプット用
    private void Attack(bool ispush)
    {
        Attack();
    }

    //攻撃したかどうかをリターン
    private bool Attack()
    {
        if (!_canAttack) return false;

        OnPlayerAttack?.Invoke();
        _attackTarget = _lockTarget ? _lockTarget : GetAttackTarget();
        if (!_attackTarget)
            return false;
        //attack
        _isAttacking = true;
        //アニメーション後に _isAttacking オフにする
        _isAttacking = false;

        _damageData.SetTarget(_attackTarget);
        _damageData.Direction = (_attackTarget.transform.position - transform.position).normalized;

        if (!_attackTarget._immune)
        {
            _attackTarget.RpcTakeDamage(_damageData);
        }

        _damageData.SetTarget(null);
        


        return true;
    }

    private void Barrier(bool ispush)
    {
        if (ispush)
        {
            _barrierPressed = true;
        }
        else
        {
            _barrierPressed = false;
            EndBarrier();
        }
    }

    private PlayerCombat GetAttackTarget()
    {
        LockOn(true);
        PlayerCombat returnVal = _lockTarget;
        ChangeLockTarget(null);
        return returnVal;
    }
    private IEnumerator BufferBarrierButton()
    {
        _barrierBtnWaiting = true;
        yield return new WaitForSeconds(_barrierBtnHoldDuration);
        _barrierBtnWaiting = false;
    }
    private IEnumerator BarrierCD()
    {
        yield return new WaitForSeconds(_barrierCD);
        _barrierCoroutine = null;
    }
    private void StartBarrier()
    {
        OnPlayerBarrierStart?.Invoke();
        _barrierBtnWaiting = true;
        RpcToggleBarrierModel(true);
    }
    private void EndBarrier()
    {
        OnPlayerBarrierEnd?.Invoke();
        _barrierCoroutine = BarrierCD();
        StartCoroutine(_barrierCoroutine);
        RpcToggleBarrierModel(false);
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RpcBarrierEffect(PlayerCombat barrierPlayer)
    {
        if (!barrierPlayer)
            return;
        Knockback((transform.position - barrierPlayer.transform.position).normalized,_knockbackForce);
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RpcToggleBarrierModel(bool visible)
    {
        _barrierModel.gameObject.SetActive(visible);
    }

    //機能オン・オッフ
    private void EnableAttack()
    {
        _canAttack = true;
    }
    private void DisableAttack()
    {
        _canAttack = false;
    }
    private void EnableLockOn()
    {
        _canLockOn = true;
    }
    private void DisableLockOn()
    {
        _canLockOn = false;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, _barrierRadius);
    }
}
