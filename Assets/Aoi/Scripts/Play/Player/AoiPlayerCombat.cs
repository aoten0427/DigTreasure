using UnityEngine;
using Fusion;
using System.Collections;
using System.Collections.Generic;
using System;

namespace Aoi
{
    public class PlayerCombat : NetworkBehaviour
    {
        //ロックオン
        [Header("ロックオン")]
        [SerializeField] private float _lockOnRange;
        private PlayerCombat _lockTarget = null;
        [SerializeField] private float _lookSpeed;
        private bool _canLockOn = true;

        //ノックバック
        [Header("ノックバック")]
        [SerializeField] private float _knockbackForce = 10f; // AddForce用の力の大きさ

        //スタン
        [Header("スタン")]
        [SerializeField] private float _stunDuration;
        private IEnumerator _stunCoroutine = null;
        private float _stunRemaining = 0f;
        public static event System.Action OnPlayerStunStart;
        public static event System.Action OnPlayerStunEnd;

        //宝落とし
        [Header("宝落とし")]
        [SerializeField][Range(0, 1)] private float _treasureLostPercent;
        [SerializeField] private Vector2 _treasureDropRadiusRange;
        [SerializeField] private Treasure _treasurePrefab;
        private float _treasureHeight;
        private PlayerInventory _inventory;
        private NetworkTreasureSpawner _treasureSpawner;
        private TreasureList _treasureList;

        //バリア
        [Header("バリア")]
        [SerializeField] private float _barrierRadius;
        [SerializeField] private float _barrierCD;
        [HideInInspector] public bool _barrierActive => _barrierCoroutine != null && _barrierBtnWaiting;
        [SerializeField] private float _barrierBtnHoldDuration;
        private bool _barrierBtnWaiting = true;
        private IEnumerator _barrierCoroutine = null;
        [SerializeField] private NetworkObject _barrierModel;
        public static event System.Action OnPlayerBarrierStart;
        public static event System.Action OnPlayerBarrierEnd;

        //他
        private PlayerCombat _attackTarget = null;
        private PlayerProto _playerProto;
        private Rigidbody _rb;
        private bool _isAttacking = false;
        private bool _canAttack = true;

        public event Action OnPlayerAttack;
        public event Action OnPlayerDamage;

        // Input System
        private GameController m_gameController;
        private bool m_guardPressed = false;

        private void Awake()
        {
            _treasureList = Resources.Load<TreasureList>("Treasure/TreasureList");
            _barrierModel.transform.localScale *= _barrierRadius;
        }
        private void OnEnable()
        {
            PlayerCombat.OnPlayerBarrierStart += DisableAttack;
            PlayerCombat.OnPlayerBarrierStart += DisableLockOn;
            PlayerCombat.OnPlayerBarrierEnd += EnableAttack;
            PlayerCombat.OnPlayerBarrierEnd += EnableLockOn;
        }
        private void OnDisable()
        {
            PlayerCombat.OnPlayerBarrierStart -= DisableAttack;
            PlayerCombat.OnPlayerBarrierStart -= DisableLockOn;
            PlayerCombat.OnPlayerBarrierEnd -= EnableAttack;
            PlayerCombat.OnPlayerBarrierEnd -= EnableLockOn;

            // Input Systemのクリーンアップ
            if (m_gameController != null)
            {
                m_gameController.Play.Disable();
                m_gameController.Dispose();
            }
        }
        public override void Spawned()
        {
            base.Spawned();
            if (HasStateAuthority)
            {
                _playerProto = GetComponent<PlayerProto>();
                _rb = GetComponent<Rigidbody>();
                _inventory = GetComponent<PlayerInventory>();
                _treasureHeight = _treasurePrefab.GetComponent<Collider>().bounds.extents.y * 2f;
                _treasureSpawner = FindFirstObjectByType<NetworkTreasureSpawner>(FindObjectsInactive.Include);
                Debug.Log("BarrModel valid: " + _barrierModel.IsValid);
                RpcToggleBarrierModel(false);

                // Input System初期化
                m_gameController = new GameController();

                // バリア入力（Guardボタン）
                m_gameController.Play.Guard.performed += ctx => m_guardPressed = true;
                m_gameController.Play.Guard.canceled += ctx =>
                {
                    m_guardPressed = false;
                    EndBarrier();
                };

                // ロックオン入力
                m_gameController.Play.LookOn.performed += ctx =>
                {
                    if (_canLockOn)
                    {
                        LockOn();
                    }
                };

                // 攻撃入力
                m_gameController.Play.Attack.performed += ctx =>
                {
                    if (_canAttack)
                    {
                        Attack();
                    }
                };

                m_gameController.Play.Enable();
            }
        }
        private void Update()
        {
            if (!HasStateAuthority)
                return;

            if (_lockTarget && Vector3.Distance(transform.position, _lockTarget.transform.position) > _lockOnRange)
                _lockTarget = null;

            // バリア（m_guardPressedフラグを使用）
            if (m_guardPressed)
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
        private void LockOn()
        {
            //距離優先、視野角
            Collider[] nearPlayers = Physics.OverlapSphere(transform.position, _lockOnRange);
            if (nearPlayers.Length == 0)
            {
                _lockTarget = null;
                return;
            }

            float nearestDist = float.MaxValue;
            float smallestAngle = float.MaxValue;
            PlayerCombat newLockTarget = null;
            foreach (Collider coll in nearPlayers)
            {
                if (coll.gameObject == gameObject || (_lockTarget && coll.gameObject == _lockTarget.gameObject)
                    || !coll.TryGetComponent(out PlayerCombat p2))
                    continue;

                Vector3 toTarget = p2.transform.position - transform.position;
                float dist = toTarget.magnitude;
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
            if (newLockTarget)
                _lockTarget = newLockTarget;
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
        public void RpcTakeDamage(PlayerCombat attacker)
        {
            if (!attacker)
                return;
            if (_barrierActive)
            {
                attacker.RpcBarrierEffect(this);
                return;
            }
            OnPlayerDamage?.Invoke();
            LoseTreasure();
            StunPlayer();
            Knockback((transform.position - attacker.transform.position).normalized);
        }

        private void Knockback(Vector3 dir)
        {
            if (_rb != null)
            {
                // 瞬間的な力を加える
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                _rb.AddForce(dir * _knockbackForce, ForceMode.Impulse);
            }
        }
        private void StunPlayer()
        {
            _stunRemaining += _stunDuration;
            if (_stunCoroutine == null)
            {
                _stunCoroutine = ToggleStun();
                StartCoroutine(_stunCoroutine);
            }
        }
        private IEnumerator ToggleStun()
        {
            OnPlayerStunStart?.Invoke();
            while (_stunRemaining >= 0f)
            {
                _stunRemaining -= Time.deltaTime;
                yield return null;
            }
            if (_stunRemaining <= 0f)
            {
                OnPlayerStunEnd?.Invoke();
                _stunCoroutine = null;
            }
        }
        private void LoseTreasure()
        {
            int amtLost = (int)(_treasureLostPercent * (float)(_inventory.AmountOfTreasure()));
            List<TreasureSO> treasureLost = _inventory.GetRandomTreasures(amtLost);
            float spawnHeight = transform.position.y + _treasureHeight;
            for (int i = 0; i < treasureLost.Count; i++)
            {
                Vector3 spawnPos = UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(_treasureDropRadiusRange.x, _treasureDropRadiusRange.y) + transform.position;
                spawnPos.y = spawnHeight;
                _treasureSpawner.SpawnTreasure(spawnPos, treasureLost[i].point, _treasureList.allTreasure.IndexOf(treasureLost[i]));
                _inventory.RemoveTreasure(treasureLost[i], 1);
            }
        }
        //攻撃したかどうかをリターン
        private bool Attack()
        {
            Debug.Log("attack");
            OnPlayerAttack?.Invoke();
            _attackTarget = _lockTarget ? _lockTarget : GetAttackTarget();
            if (!_attackTarget)
                return false;
            //attack
            _isAttacking = true;
            //アニメーション終了後 _isAttacking オフにする
            _isAttacking = false;
            _attackTarget.RpcTakeDamage(this);

            return true;
        }
        private PlayerCombat GetAttackTarget()
        {
            LockOn();
            PlayerCombat returnVal = _lockTarget;
            _lockTarget = null;
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
            Knockback((transform.position - barrierPlayer.transform.position).normalized);
        }
        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RpcToggleBarrierModel(bool visible)
        {
            _barrierModel.gameObject.SetActive(visible);
        }

        //機能オン・オフ
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
}
