using UnityEngine;

namespace VoxelWorld
{
    /// <summary>
    /// 基本的な攻撃・破壊システム
    /// WorldManagerの破壊機能を呼び出すシンプルなインターフェース
    /// </summary>
    public class BaseAttack : MonoBehaviour
    {
        [Header("攻撃設定")]
        [SerializeField] private float m_attackPower = 2.0f;
        [SerializeField] private float m_attackRadius = 3.0f;
        


        [Header("破壊形状")]
        [SerializeField] private AttackShape m_attackShape = AttackShape.Sphere;
        [SerializeField] private Vector3 m_boxSize = new Vector3(2, 2, 2);

        [Header("攻撃対象")]
        [SerializeField] private bool m_targetChunks = true;
        [SerializeField] private bool m_targetSeparatedObjects = true;

        [Header("デバッグ")]
        [SerializeField] private bool m_enableLogging = false;

        private VoxelDestructionManager m_destructionManager;
        public enum AttackShape
        {
            Point,   // 単一ボクセル
            Sphere,  // 球体範囲
            Box      // 矩形範囲
        }

        // 攻撃力
        public float AttackPower 
        { 
            get => m_attackPower; 
            set => m_attackPower = Mathf.Max(0, value); 
        }
        
        // 攻撃半径
        public float AttackRadius 
        { 
            get => m_attackRadius; 
            set => m_attackRadius = Mathf.Max(0, value); 
        }




        /// <summary>
        /// 指定位置への攻撃を実行
        /// </summary>
        /// <param name="worldPosition">攻撃位置（ワールド座標）</param>
        public void AttackAtPosition(Vector3 worldPosition,Vector3 directio = default)
        {
            ExecuteAttack(worldPosition,directio);
        }

        /// <summary>
        /// 攻撃を実行する
        /// DestructionCoordinatorに全処理を委譲
        /// </summary>
        /// <param name="attackPosition">攻撃位置</param>
        private void ExecuteAttack(Vector3 attackPosition,Vector3 effectDirection = default)
        {
            IDestructionShape destructionShape = CreateDestructionShape(attackPosition);

            if (destructionShape == null)
            {
                Debug.LogWarning("[BaseAttack] 破壊形状の作成に失敗しました。");
                return;
            }

            // DestructionCoordinatorに全て委譲（統一破壊API使用）
            var coordinator = DestructionCoordinator.GetInstance();
            if (coordinator == null)
            {
                Debug.LogError("[BaseAttack] DestructionCoordinatorが見つかりません。");
                return;
            }

            coordinator.DestroyAllTargets(
                destructionShape,
                attackPosition,
                m_attackPower,
                effectDirection,
                m_targetChunks,
                m_targetSeparatedObjects,
                v => Debug.Log(v)
            );

        }


        /// <summary>
        /// 破壊形状を作成
        /// </summary>
        /// <param name="position">中心位置</param>
        /// <returns>破壊形状</returns>
        private IDestructionShape CreateDestructionShape(Vector3 position)
        {
            switch (m_attackShape)
            {
                case AttackShape.Point:
                    return new PointListDestruction(position);

                case AttackShape.Sphere:
                    return new SphereDestruction(position, m_attackRadius);

                case AttackShape.Box:
                    return new BoxDestruction(position, m_boxSize);

                default:
                    Debug.LogWarning($"[BaseAttack] 未対応の攻撃形状: {m_attackShape}");
                    return new PointListDestruction(position);
            }
        }
    }
}