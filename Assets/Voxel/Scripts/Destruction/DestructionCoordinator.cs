using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace VoxelWorld
{
    /// <summary>
    /// 破壊処理統一コーディネーター
    /// </summary>
    public class DestructionCoordinator : MonoBehaviour
    {
        private static DestructionCoordinator s_instance;
        public static DestructionCoordinator Instance => s_instance;
        
        public static DestructionCoordinator GetInstance()
        {
            if (s_instance == null)
            {
                s_instance = FindFirstObjectByType<DestructionCoordinator>();
            }
            return s_instance;
        }
        
        
        [Header("統一破壊設定")]
        [SerializeField] private bool m_enableLogging = false;
        
        
        
        private WorldManager m_worldManager;
        private SeparationManager m_separationManager;
        private VoxelDestructionManager m_destructionManager;
        private bool m_isInitialized = false;
        
        
        private void Awake()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return;
            }
            s_instance = this;
        }
        
        private void Start()
        {
            Initialize();
        }
        
        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }
        
        
        
        /// <summary>
        /// 統一システム初期化
        /// </summary>
        public void Initialize()
        {
            if (m_isInitialized) return;
            
            m_worldManager = WorldManager.GetInstance();
            m_separationManager = m_worldManager?.SeparationManager;
            m_destructionManager = gameObject.AddComponent<VoxelDestructionManager>();
            
            m_isInitialized = true;
        }
        
        
        /// <summary>
        /// チャンクボクセル破壊
        /// </summary>
        /// <param name="shape">破壊形状</param>
        /// <param name="worldCenter">破壊中心位置</param>
        /// <param name="attackPower">攻撃力</param>
        /// <param name="onComplete">完了コールバック（破壊されたボクセル数を受け取る）</param>
        public void DestroyChunkVoxels(IDestructionShape shape, Vector3 worldCenter,
            float attackPower = 1.0f, Vector3 direction = default, System.Action<int> onComplete = null)
        {
            if (!ValidateDestruction(shape))
            {
                onComplete?.Invoke(0);
                return;
            }

            // 破壊実行（破壊数付きコールバック版を使用）
            m_destructionManager.DestroyVoxels(shape, attackPower, direction, onComplete);
        }
        
        /// <summary>
        /// 分離オブジェクト破壊
        /// </summary>
        /// <param name="separatedObject">対象分離オブジェクト</param>
        /// <param name="shape">破壊形状</param>
        /// <param name="worldCenter">破壊中心位置</param>
        /// <param name="attackPower">攻撃力</param>
        /// <returns>破壊されたボクセル数</returns>
        public int DestroySeparatedObject(SeparatedVoxelObject separatedObject,
            IDestructionShape shape, Vector3 worldCenter, float attackPower = 1.0f)
        {
            if (!ValidateSeparatedObjectDestruction(separatedObject, shape)) return 0;

            // SeparatedVoxelObjectの破壊メソッドを直接呼び出し
            return separatedObject.DestroyWithShape(shape, worldCenter);
        }

        /// <summary>
        /// チャンクと分離オブジェクトを破壊
        /// </summary>
        /// <param name="shape">破壊形状</param>
        /// <param name="worldCenter">破壊中心位置</param>
        /// <param name="attackPower">攻撃力</param>
        /// <param name="targetChunks">チャンクを対象にするか</param>
        /// <param name="targetSeparatedObjects">分離オブジェクトを対象にするか</param>
        /// <param name="onComplete">完了コールバック（総破壊ボクセル数を受け取る）</param>
        public void DestroyAllTargets(
            IDestructionShape shape,
            Vector3 worldCenter,
            float attackPower,
            Vector3 direction = default,
            bool targetChunks = true,
            bool targetSeparatedObjects = true,
            Action<int> onComplete = null)
        {
            if (!ValidateDestruction(shape))
            {
                onComplete?.Invoke(0);
                return;
            }

            int totalDestroyed = 0;

            // 分離オブジェクト破壊（同期処理）
            if (targetSeparatedObjects && DestructionTargetFinder.HasValidSeparatedObjectTarget(m_worldManager))
            {
                // DestructionTargetFinderを使用して統一検索
                var separatedObjects = DestructionTargetFinder.FindSeparatedObjectsForShape(
                    shape, worldCenter, m_separationManager);

                foreach (var obj in separatedObjects)
                {
                    if (obj != null && obj.gameObject != null)
                    {
                        int objDestroyed = DestroySeparatedObject(obj, shape, worldCenter, attackPower);
                        totalDestroyed += objDestroyed;
                    }
                }
            }

            // チャンクボクセル破壊（非同期処理、コールバックで結果を受け取る）
            if (targetChunks && DestructionTargetFinder.HasValidChunkTarget(m_worldManager))
            {
                int separatedObjectsDestroyed = totalDestroyed; // 分離オブジェクトの破壊数を保持
                DestroyChunkVoxels(shape, worldCenter, attackPower, direction,(chunkDestroyed) =>
                {
                    int grandTotal = separatedObjectsDestroyed + chunkDestroyed;
                    onComplete?.Invoke(grandTotal);
                });
            }
            else
            {
                // チャンク破壊がない場合は即座にコールバック
                onComplete?.Invoke(totalDestroyed);
            }
        }
        
        
        /// <summary>
        /// 破壊処理の事前検証
        /// </summary>
        private bool ValidateDestruction(IDestructionShape shape)
        {
            if (!m_isInitialized)
            {
                Debug.LogError("[DestructionCoordinator] 初期化されていません");
                return false;
            }
            
            if (shape == null)
            {
                Debug.LogError("[DestructionCoordinator] 破壊形状がnullです");
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 分離オブジェクト破壊の事前検証
        /// </summary>
        private bool ValidateSeparatedObjectDestruction(SeparatedVoxelObject obj, IDestructionShape shape)
        {
            if (!ValidateDestruction(shape)) return false;
            
            if (obj == null || obj.gameObject == null)
            {
                Debug.LogError("[DestructionCoordinator] 分離オブジェクトが無効です");
                return false;
            }
            
            return true;
        }
    }
}