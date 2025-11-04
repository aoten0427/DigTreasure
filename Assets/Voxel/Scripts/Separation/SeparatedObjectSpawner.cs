using UnityEngine;
using Fusion;

namespace VoxelWorld
{
    /// <summary>
    /// 分離オブジェクトの生成を管理（ローカル/ネットワーク対応）
    /// </summary>
    public class SeparatedObjectSpawner
    {
        /// <summary>
        /// 生成モード
        /// </summary>
        public enum SpawnMode
        {
            Local,      // ローカル生成（シングルプレイ、テスト用）
            Network     // ネットワーク生成（マルチプレイ）
        }

        private SpawnMode m_spawnMode;
        private NetworkRunner m_runner;
        private NetworkObject m_separatedVoxelPrefab;
        private Material m_voxelMaterial;

        /// <summary>
        /// 現在の生成モード
        /// </summary>
        public SpawnMode CurrentMode => m_spawnMode;

        /// <summary>
        /// 初期化（ローカルモード）
        /// </summary>
        /// <param name="voxelMaterial">ボクセルマテリアル</param>
        public void InitializeLocal(Material voxelMaterial)
        {
            m_spawnMode = SpawnMode.Local;
            m_voxelMaterial = voxelMaterial;
            Debug.Log("[SeparatedObjectSpawner] ローカルモードで初期化");
        }

        /// <summary>
        /// 初期化（ネットワークモード）
        /// </summary>
        /// <param name="runner">NetworkRunner</param>
        /// <param name="prefab">分離オブジェクトのPrefab</param>
        /// <param name="voxelMaterial">ボクセルマテリアル</param>
        public void InitializeNetwork(NetworkRunner runner, NetworkObject prefab, Material voxelMaterial)
        {
            m_spawnMode = SpawnMode.Network;
            m_runner = runner;
            m_separatedVoxelPrefab = prefab;
            m_voxelMaterial = voxelMaterial;
            Debug.Log("[SeparatedObjectSpawner] ネットワークモードで初期化");
        }

        /// <summary>
        /// 分離オブジェクトを生成
        /// </summary>
        /// <param name="voxelData">ボクセルデータ配列</param>
        /// <param name="size">オブジェクトサイズ</param>
        /// <param name="worldPosition">ワールド位置</param>
        /// <returns>生成された分離オブジェクト</returns>
        public SeparatedVoxelObject Spawn(Voxel[,,] voxelData, Vector3Int size, Vector3 worldPosition)
        {
            if (m_spawnMode == SpawnMode.Network)
            {
                return SpawnNetwork(voxelData, size, worldPosition);
            }
            else
            {
                return SpawnLocal(voxelData, size, worldPosition);
            }
        }

        /// <summary>
        /// ローカル生成（既存の方式）
        /// </summary>
        private SeparatedVoxelObject SpawnLocal(Voxel[,,] voxelData, Vector3Int size, Vector3 worldPosition)
        {
            // GameObjectを作成
            var gameObject = new GameObject("SeparatedVoxelObject");

            // 必須コンポーネントを追加
            gameObject.AddComponent<Rigidbody>();
            gameObject.AddComponent<MeshFilter>();
            gameObject.AddComponent<MeshRenderer>();

            // SeparatedVoxelObjectコンポーネントを追加
            var separatedObject = gameObject.AddComponent<SeparatedVoxelObject>();

            // 初期化
            separatedObject.Initialize(voxelData, size, worldPosition, m_voxelMaterial);

            return separatedObject;
        }

        /// <summary>
        /// ネットワーク生成（Fusion使用）
        /// </summary>
        private SeparatedVoxelObject SpawnNetwork(Voxel[,,] voxelData, Vector3Int size, Vector3 worldPosition)
        {
            if (m_runner == null || m_separatedVoxelPrefab == null)
            {
                Debug.LogError("[SeparatedObjectSpawner] ネットワークモードが正しく初期化されていません");
                return null;
            }

            // Runner.Spawn()でネットワークオブジェクト生成
            NetworkObject networkObject = m_runner.Spawn(
                m_separatedVoxelPrefab,
                worldPosition,
                Quaternion.identity,
                m_runner.LocalPlayer  // State Authority = 生成者
            );

            if (networkObject == null)
            {
                Debug.LogError("[SeparatedObjectSpawner] NetworkObjectの生成に失敗しました");
                return null;
            }

            var separatedObject = networkObject.GetComponent<SeparatedVoxelObject>();
            if (separatedObject != null)
            {
                // 初期化はState Authorityのみが実行
                // 他のクライアントはRPCで初期化データを受信する
                if (networkObject.HasStateAuthority)
                {
                    separatedObject.Initialize(voxelData, size, worldPosition, m_voxelMaterial);
                }
            }
            else
            {
                Debug.LogError("[SeparatedObjectSpawner] SeparatedVoxelObjectコンポーネントが見つかりません");
            }

            return separatedObject;
        }

        /// <summary>
        /// 分離オブジェクトを削除
        /// </summary>
        /// <param name="separatedObject">削除する分離オブジェクト</param>
        public void Despawn(SeparatedVoxelObject separatedObject)
        {
            if (separatedObject == null) return;

            if (m_spawnMode == SpawnMode.Network)
            {
                var networkObject = separatedObject.GetComponent<NetworkObject>();
                if (networkObject != null && m_runner != null)
                {
                    m_runner.Despawn(networkObject);
                    return;
                }
            }

            // ローカルモードまたはNetworkObjectがない場合
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(separatedObject.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(separatedObject.gameObject);
            }
        }
    }
}
