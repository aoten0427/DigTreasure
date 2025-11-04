using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VoxelWorld
{
    /// <summary>
    /// 分離されたボクセルオブジェクト
    /// チャンクから分離した独立したボクセル塊を管理
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class SeparatedVoxelObject : MonoBehaviour
    {
        [Header("オブジェクト情報")]
        [SerializeField] private Vector3Int m_size;
        [SerializeField] private int m_voxelCount;
        [SerializeField] private float m_creationTime;

        [Header("物理設定")]
        [SerializeField] private float m_massPerVoxel = 1.0f;
        [SerializeField] private bool m_useGravity = true;
        [SerializeField] private float m_drag = 0.1f;
        [SerializeField] private float m_angularDrag = 0.05f;

        [Header("自動削除設定")]
        [SerializeField] private int m_autoDeleteThreshold = 10;
        [SerializeField] private float m_autoDeleteTime = 5.0f;
        [SerializeField] private bool m_markedForDeletion = false;

        [Header("コライダー設定")]
        [SerializeField] private int m_maxColliders = 64;
        [SerializeField] private bool m_useOptimizedColliders = true;

        [Header("ネットワーク設定")]
        [SerializeField] private bool m_isInitialized = false;

        // データ管理
        private Voxel[,,] m_voxelData;
        private Vector3 m_worldPosition;

        // Unity コンポーネント
        private Rigidbody m_rigidbody;
        private MeshFilter m_meshFilter;
        private MeshRenderer m_meshRenderer;

        // メッシュ管理
        private Mesh m_currentMesh;

        // コライダー管理
        private GameObject m_colliderContainer;
        private List<BoxCollider> m_boxColliders = new List<BoxCollider>();


        //オブジェクトサイズ
        public Vector3Int Size => m_size;
        
        //ボクセル総数
        public int VoxelCount => m_voxelCount;
        
        //オブジェクトのワールド位置
        public Vector3 WorldPosition => m_worldPosition;
        
        //自動削除対象かどうか
        public bool IsMarkedForDeletion => m_markedForDeletion;
        
        //オブジェクトの質量
        public float Mass => m_voxelCount * m_massPerVoxel;

        //ボクセルデータ配列を取得
        public Voxel[,,] GetVoxelData() => m_voxelData;


        /// <summary>
        /// 指定位置のボクセルを設定
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <param name="z">Z座標</param>
        /// <param name="voxel">設定するボクセル</param>
        /// <returns>設定が成功した場合true</returns>
        public bool SetVoxel(int x, int y, int z, Voxel voxel)
        {
            if (!IsValidVoxelPosition(x, y, z))
            {
                return false;
            }

            m_voxelData[x, y, z] = voxel;
            UpdateVoxelCount();
            CheckAutoDelete();
            
            return true;
        }
        
        /// <summary>
        /// 指定位置のボクセルを取得
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <param name="z">Z座標</param>
        /// <returns>ボクセルデータ</returns>
        public Voxel GetVoxel(int x, int y, int z)
        {
            if (!IsValidVoxelPosition(x, y, z))
            {
                return Voxel.Empty;
            }
            
            return m_voxelData[x, y, z];
        }
        
        /// <summary>
        /// Vector3Int形式でボクセルデータを取得
        /// </summary>
        /// <param name="index">ボクセルインデックス</param>
        /// <returns>ボクセルデータ</returns>
        public Voxel GetVoxelData(Vector3Int index)
        {
            return GetVoxel(index.x, index.y, index.z);
        }
        
        /// <summary>
        /// 指定インデックスが有効な範囲内かチェック
        /// </summary>
        /// <param name="index">チェックするインデックス</param>
        /// <returns>有効な範囲内の場合true</returns>
        public bool IsValidIndex(Vector3Int index)
        {
            return IsValidVoxelPosition(index.x, index.y, index.z);
        }

        /// <summary>
        /// 破壊形状による破壊処理
        /// </summary>
        /// <param name="destructionShape">破壊形状</param>
        /// <param name="worldCenter">破壊中心点（ワールド座標）</param>
        /// <returns>破壊されたボクセル数</returns>
        public int DestroyWithShape(IDestructionShape destructionShape, Vector3 worldCenter)
        {
            if (destructionShape == null)
            {
                Debug.LogWarning("[SeparatedVoxelObject] 破壊形状がnullです");
                return 0;
            }

            // ワールド座標からローカル座標に変換
            Vector3 localCenter = worldCenter - transform.position;

            // 破壊対象ボクセルを検索
            var targetVoxels = FindDestructionTargets(destructionShape, localCenter);

            // ボクセルを破壊
            return ExecuteVoxelDestruction(targetVoxels);
        }

        /// <summary>
        /// 複数のボクセルを破壊
        /// </summary>
        /// <param name="voxelPositions">破壊するボクセル座標リスト（ローカル配列インデックス）</param>
        /// <returns>破壊されたボクセル数</returns>
        public int DestroyVoxels(List<Vector3Int> voxelPositions)
        {
            if (voxelPositions == null || voxelPositions.Count == 0)
            {
                return 0;
            }

            // 範囲チェック付きで破壊
            var validPositions = voxelPositions.Where(pos => IsValidIndex(pos)).ToList();
            return ExecuteVoxelDestruction(validPositions);
        }

        /// <summary>
        /// 破壊対象ボクセルを検索
        /// </summary>
        private List<Vector3Int> FindDestructionTargets(IDestructionShape destructionShape, Vector3 localCenter)
        {
            var targets = new List<Vector3Int>();

            if (m_voxelData == null)
            {
                return targets;
            }

            // 破壊形状から破壊対象のワールド座標を取得
            var destructionPositions = destructionShape.GetDestructionPositions();

            // キャッシュ
            var voxelDataBase = VoxelDataBase.Instance;
            var destructibleCache = new Dictionary<int, bool>();

            foreach (var worldPos in destructionPositions)
            {
                // ワールド座標をローカル座標に変換
                Vector3 localPos = worldPos - m_worldPosition;

                // ローカル配列インデックスに変換
                Vector3Int localIndex = new Vector3Int(
                    Mathf.FloorToInt(localPos.x / VoxelConstants.VOXEL_SIZE),
                    Mathf.FloorToInt(localPos.y / VoxelConstants.VOXEL_SIZE),
                    Mathf.FloorToInt(localPos.z / VoxelConstants.VOXEL_SIZE)
                );

                // 範囲チェック
                if (!IsValidIndex(localIndex))
                {
                    continue;
                }

                // 空ボクセルはスキップ
                Voxel voxel = m_voxelData[localIndex.x, localIndex.y, localIndex.z];
                if (voxel.IsEmpty)
                {
                    continue;
                }

                // 破壊可能かチェック（キャッシュ利用）
                if (!destructibleCache.TryGetValue(voxel.VoxelId, out bool isDestructible))
                {
                    isDestructible = voxelDataBase?.GetVoxelData(voxel.VoxelId)?.IsDestructible ?? true;
                    destructibleCache[voxel.VoxelId] = isDestructible;
                }

                if (isDestructible)
                {
                    targets.Add(localIndex);
                }
            }

            return targets;
        }

        /// <summary>
        /// ボクセル破壊を実行
        /// </summary>
        private int ExecuteVoxelDestruction(List<Vector3Int> targetPositions)
        {
            if (targetPositions == null || targetPositions.Count == 0)
            {
                return 0;
            }

            int destroyedCount = 0;

            foreach (var localIndex in targetPositions)
            {
                if (IsValidIndex(localIndex) && !m_voxelData[localIndex.x, localIndex.y, localIndex.z].IsEmpty)
                {
                    m_voxelData[localIndex.x, localIndex.y, localIndex.z] = Voxel.Empty;
                    destroyedCount++;
                }
            }

            // 破壊後の処理
            if (destroyedCount > 0)
            {
                UpdateVoxelCount();
                GenerateMeshAndCollider();
                CheckAutoDelete();
            }

            return destroyedCount;
        }

        /// <summary>
        /// オブジェクトを手動で削除
        /// </summary>
        public void DestroyObject()
        {
            if (m_markedForDeletion)
            {
                return;
            }
            
            m_markedForDeletion = true;
            PerformDestruction();
        }

        /// <summary>
        /// 分離オブジェクトを初期化
        /// </summary>
        /// <param name="voxelData">ボクセルデータ配列</param>
        /// <param name="size">オブジェクトサイズ</param>
        /// <param name="worldPosition">ワールド位置</param>
        /// <param name="voxelMaterial">ボクセルマテリアル</param>
        public void Initialize(Voxel[,,] voxelData, Vector3Int size, Vector3 worldPosition, Material voxelMaterial = null)
        {
            // 重複初期化防止（Fusion対応）
            if (m_isInitialized)
            {
                Debug.LogWarning($"[SeparatedVoxelObject] 既に初期化済み: {GetInstanceID()}");
                return;
            }

            // 基本データ設定
            m_voxelData = voxelData;
            m_size = size;
            m_worldPosition = worldPosition;
            m_creationTime = Time.time;

            // GameObjectの位置設定
            transform.position = worldPosition;
            transform.name = $"SepObj_{GetInstanceID()}";

            // ボクセル数を更新
            UpdateVoxelCount();

            // Unity コンポーネント取得・設定
            InitializeComponents(voxelMaterial);

            // メッシュ・コライダー生成
            GenerateMeshAndCollider();

            // 自動削除チェック
            CheckAutoDelete();

            // 初期化完了フラグ設定
            m_isInitialized = true;
        }

        /// <summary>
        /// Unity コンポーネントを初期化
        /// </summary>
        /// <param name="voxelMaterial">ボクセルマテリアル（nullの場合はResourcesから読み込み）</param>
        private void InitializeComponents(Material voxelMaterial = null)
        {
            // コンポーネント取得
            m_rigidbody = GetComponent<Rigidbody>();
            m_meshFilter = GetComponent<MeshFilter>();
            m_meshRenderer = GetComponent<MeshRenderer>();

            if (m_rigidbody == null || m_meshFilter == null || m_meshRenderer == null)
            {
                Debug.LogError($"[SeparatedVoxelObject] 必須コンポーネントが見つかりません");
                return;
            }

            // マテリアル設定
            if (voxelMaterial == null)
            {
                voxelMaterial = Resources.Load<Material>("VoxelMaterial");
            }

            if (voxelMaterial != null)
            {
                m_meshRenderer.material = voxelMaterial;
            }
            else
            {
                Debug.LogWarning($"[SeparatedVoxelObject] ボクセルマテリアルが設定されていません");
            }

            // 物理設定を適用
            m_rigidbody.mass = Mass;
            m_rigidbody.useGravity = m_useGravity;
            m_rigidbody.linearDamping = m_drag;
            m_rigidbody.angularDamping = m_angularDrag;

            //// Rigidbodyの衝突検出モードを連続に設定（床をすり抜ける問題を防ぐ）
            //m_rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

            //// 補間を有効化（物理演算のスムーズ化）
            //m_rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            //// 制約なし（回転・移動を自由にする）
            //m_rigidbody.constraints = RigidbodyConstraints.None;
        }

        /// <summary>
        /// ボクセル数を再カウントして更新
        /// </summary>
        private void UpdateVoxelCount()
        {
            if (m_voxelData == null)
            {
                m_voxelCount = 0;
                return;
            }

            m_voxelCount = 0;
            for (int x = 0; x < m_size.x; x++)
            {
                for (int y = 0; y < m_size.y; y++)
                {
                    for (int z = 0; z < m_size.z; z++)
                    {
                        if (!m_voxelData[x, y, z].IsEmpty)
                        {
                            m_voxelCount++;
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// ボクセル位置の妥当性をチェック
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <param name="z">Z座標</param>
        /// <returns>有効な位置の場合true</returns>
        private bool IsValidVoxelPosition(int x, int y, int z)
        {
            return m_voxelData != null && 
                   x >= 0 && x < m_size.x && 
                   y >= 0 && y < m_size.y && 
                   z >= 0 && z < m_size.z;
        }

        
        /// <summary>
        /// メッシュとコライダーを生成
        /// </summary>
        public void GenerateMeshAndCollider()
        {
            // メッシュ生成
            GenerateMesh();
            
            // コライダー生成
            if (m_useOptimizedColliders)
            {
                GenerateOptimizedColliders();
            }
            else
            {
                GenerateSingleCollider();
            }
            
            // Rigidbody質量更新
            if (m_rigidbody != null)
            {
                m_rigidbody.mass = Mass;
            }
        }

        /// <summary>
        /// メッシュ生成
        /// </summary>
        private void GenerateMesh()
        {
            if (m_voxelData == null || m_meshFilter == null)
            {
                Debug.LogWarning($"[SeparatedVoxelObject] メッシュ生成前提条件エラー: {GetInstanceID()}");
                return;
            }

            // 既存メッシュを削除
            if (m_currentMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_currentMesh);
                }
                else
                {
                    DestroyImmediate(m_currentMesh);
                }
            }

            // VoxelMeshGeneratorを使用してメッシュ生成
            m_currentMesh = VoxelMeshGenerator.GenerateVoxelMesh(
                m_voxelData,
                m_size.x,
                m_size.y,
                m_size.z,
                Vector3.zero // ローカル座標系で生成
            );

            if (m_currentMesh == null)
            {
                Debug.LogWarning($"[SeparatedVoxelObject] メッシュ生成失敗: {GetInstanceID()}");
                return;
            }

            // MeshFilterに適用
            m_currentMesh.name = $"SepObj_{GetInstanceID()}_Mesh";
            m_meshFilter.mesh = m_currentMesh;
        }

        /// <summary>
        /// メッシュを完全にクリア
        /// </summary>
        private void ClearMesh()
        {
            if (m_currentMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_currentMesh);
                }
                else
                {
                    DestroyImmediate(m_currentMesh);
                }
                m_currentMesh = null;
            }

            if (m_meshFilter != null)
            {
                m_meshFilter.mesh = null;
            }
        }

        /// <summary>
        /// 最適化BoxCollider群を生成
        /// </summary>
        private void GenerateOptimizedColliders()
        {
            // 既存コライダーを削除
            ClearColliders();

            if (m_voxelData == null)
            {
                Debug.LogWarning($"[SeparatedVoxelObject] ボクセルデータがnull: {GetInstanceID()}");
                return;
            }

            // ChunkColliderGeneratorを使用して最適化コライダーを生成
            var optimizedColliders = ChunkColliderGenerator.GenerateOptimizedBoxColliders(
                m_voxelData, m_size, m_maxColliders);

            if (optimizedColliders.Count == 0)
            {
                Debug.Log($"[SeparatedVoxelObject] コライダー生成なし（空オブジェクト）: {GetInstanceID()}");
                return;
            }

            // コライダーコンテナを作成
            if (m_colliderContainer == null)
            {
                m_colliderContainer = new GameObject($"SepObj_{GetInstanceID()}_Colliders");
                m_colliderContainer.transform.SetParent(transform);
                m_colliderContainer.transform.localPosition = Vector3.zero;
                m_colliderContainer.transform.localRotation = Quaternion.identity;
            }

            // BoxColliderを生成（親オブジェクトに直接追加）
            foreach (var optimizedCollider in optimizedColliders)
            {
                BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.center = optimizedCollider.localCenter;
                boxCollider.size = optimizedCollider.size;
                boxCollider.enabled = false; // 初期状態では無効化

                m_boxColliders.Add(boxCollider);
            }

            
        }

        /// <summary>
        /// 単一BoxCollider生成（フォールバック用）
        /// </summary>
        private void GenerateSingleCollider()
        {
            // 既存コライダーを削除
            ClearColliders();

            // バウンディングボックスを計算
            var bounds = CalculateBounds();
            
            // 単一BoxColliderを作成
            if (m_colliderContainer == null)
            {
                m_colliderContainer = new GameObject($"SepObj_{GetInstanceID()}_Colliders");
                m_colliderContainer.transform.SetParent(transform);
                m_colliderContainer.transform.localPosition = Vector3.zero;
                m_colliderContainer.transform.localRotation = Quaternion.identity;
            }

            // BoxColliderを親オブジェクトに直接追加
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.center = bounds.center - transform.position;
            boxCollider.size = bounds.size;

            m_boxColliders.Add(boxCollider);

            Debug.Log($"[SeparatedVoxelObject] 単一コライダー生成完了: {GetInstanceID()}, サイズ={bounds.size}");
        }

        /// <summary>
        /// 既存コライダーを削除
        /// </summary>
        private void ClearColliders()
        {
            m_boxColliders.Clear();

            if (m_colliderContainer != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(m_colliderContainer);
                }
                else
                {
                    DestroyImmediate(m_colliderContainer);
                }
                m_colliderContainer = null;
            }
        }

        /// <summary>
        /// ボクセルデータからバウンディングボックスを計算
        /// </summary>
        /// <returns>バウンディングボックス</returns>
        private Bounds CalculateBounds()
        {
            Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            bool hasVoxels = false;

            for (int x = 0; x < m_size.x; x++)
            {
                for (int y = 0; y < m_size.y; y++)
                {
                    for (int z = 0; z < m_size.z; z++)
                    {
                        if (!m_voxelData[x, y, z].IsEmpty)
                        {
                            Vector3 voxelWorldPos = m_worldPosition + new Vector3(x, y, z) * VoxelConstants.VOXEL_SIZE;
                            
                            min = Vector3.Min(min, voxelWorldPos);
                            max = Vector3.Max(max, voxelWorldPos + Vector3.one * VoxelConstants.VOXEL_SIZE);
                            hasVoxels = true;
                        }
                    }
                }
            }

            if (!hasVoxels)
            {
                // 空の場合はデフォルトサイズ
                return new Bounds(m_worldPosition, Vector3.one * VoxelConstants.VOXEL_SIZE);
            }

            return new Bounds((min + max) * 0.5f, max - min);
        }

        /// <summary>
        /// 自動削除チェック
        /// </summary>
        private void CheckAutoDelete()
        {
            if (m_voxelCount <= 0 && !m_markedForDeletion)
            {
                // ボクセルが全て破壊された場合は即座に削除
                m_markedForDeletion = true;
                DestroyObject();
            }
            else if (m_voxelCount <= m_autoDeleteThreshold && !m_markedForDeletion)
            {
                m_markedForDeletion = true;
                StartCoroutine(AutoDeleteCoroutine());
            }
        }

        /// <summary>
        /// 自動削除コルーチン
        /// </summary>
        private IEnumerator AutoDeleteCoroutine()
        {
            yield return new WaitForSeconds(m_autoDeleteTime);
            
            if (m_markedForDeletion && m_voxelCount <= m_autoDeleteThreshold)
            {
                DestroyObject();
            }
        }

        /// <summary>
        /// オブジェクトの実際の削除処理
        /// </summary>
        private void PerformDestruction()
        {
            // WorldManagerに削除を通知
            var worldManager = WorldManager.GetInstance();
            if (worldManager != null)
            {

            }

            // リソースクリーンアップ
            ClearMesh();
            ClearColliders();

            // GameObject削除
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

    }
}