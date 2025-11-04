using UnityEngine;

namespace VoxelWorld
{
    public class VoxelEffect : MonoBehaviour
    {
        // エフェクトとして生成するオブジェクト
        [SerializeField] private GameObject m_effectObject;

        // 破壊回数に対する生成割合
        [SerializeField, Range(0f, 1f)] private float m_generationRatio = 0.2f;

        // 大きさの範囲
        [SerializeField] private Vector3 m_minSize = new Vector3(0.1f, 0.1f, 0.1f);
        [SerializeField] private Vector3 m_maxSize = new Vector3(0.3f, 0.3f, 0.3f);

        // 速度の調整パラメータ
        [SerializeField]private float m_velocityMultiplier = 1.0f;
        [SerializeField]private float m_spreadAngle = 30f;
        [SerializeField] private float m_velocityRandomness = 0.3f;
        // エフェクトオブジェクトの生存時間
        [SerializeField]private float m_effectLifetime = 3f;



        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.E))
            {
                Rewind(100, Vector3.zero, 1);
            }
        }


        /// <summary>
        /// エフェクトを生成する
        /// </summary>
        /// <param name="originDestroy">破壊された元の数</param>
        /// <param name="direction">飛ばす方向と速度(0,0,0の場合は全方向)</param>
        /// <param name="voxelId">ボクセルID（将来の拡張用）</param>
        public void Rewind(int originDestroy, Vector3 direction, int voxelId)
        {
            // 入力値の検証
            if (!ValidateInput(originDestroy, direction))
            {
                return;
            }

            int generationNum = CalculateGenerationCount(originDestroy);

            for (int i = 0; i < generationNum; i++)
            {
                SpawnEffectObject(direction, voxelId);
            }

            Destroy(gameObject, m_effectLifetime);
        }

        /// <summary>
        /// 入力値の検証
        /// </summary>
        private bool ValidateInput(int originDestroy, Vector3 velocity)
        {
            if (m_effectObject == null) return false;

            if (originDestroy < 0) return false;

            

            return true;
        }

        /// <summary>
        /// 生成数を計算
        /// </summary>
        private int CalculateGenerationCount(int originDestroy)
        {
            int count = Mathf.RoundToInt(originDestroy * m_generationRatio);

            // 最低1つは生成する
            if (originDestroy > 0 && count == 0)
            {
                count = 1;
            }

            return count;
        }

        /// <summary>
        /// エフェクトオブジェクトを生成
        /// </summary>
        private void SpawnEffectObject(Vector3 baseVelocity, int voxelId)
        {
            // 位置は現在のオブジェクトの位置
            Vector3 spawnPosition = transform.position;
            // ランダムな回転を生成
            Quaternion randomRotation = Random.rotation;
            // オブジェクトを生成（親を自分自身に設定）
            GameObject effectInstance = Instantiate(m_effectObject, spawnPosition, randomRotation, transform);
            // ランダムなサイズを設定
            Vector3 randomSize = new Vector3(
                Random.Range(m_minSize.x, m_maxSize.x),
                Random.Range(m_minSize.y, m_maxSize.y),
                Random.Range(m_minSize.z, m_maxSize.z)
            );
            effectInstance.transform.localScale = randomSize;
            // Rigidbodyを取得して速度を設定
            Rigidbody rb = effectInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 finalVelocity = CalculateRandomVelocity(baseVelocity);
                rb.AddForce(finalVelocity);

                // ランダムな回転速度も追加
                rb.angularVelocity = Random.insideUnitSphere * 5f;
            }

            //マテリアル設定
            var color = VoxelDataBase.GetVoxelColorStatic(voxelId);
            var renderer = rb.GetComponent<Renderer>();
            renderer.material.SetColor("_BaseColor", color);

            // 指定時間後に破棄
            Destroy(effectInstance, m_effectLifetime);
        }

        /// <summary>
        /// ランダムなばらつきを加えた速度を計算
        /// </summary>
        private Vector3 CalculateRandomVelocity(Vector3 baseVelocity)
        {
            float baseMagnitude = baseVelocity.magnitude;

            // velocityが0の場合は全方向ランダム
            if (baseMagnitude < 0.001f)
            {
                Vector3 randomDirection = Random.onUnitSphere;
                float randomMagnitude = m_velocityMultiplier * Random.Range(1f - m_velocityRandomness, 1f + m_velocityRandomness);
                return randomDirection * randomMagnitude;
            }

            // 基準方向を正規化
            Vector3 direction = baseVelocity.normalized;

            // ランダムなばらつきを追加
            Vector3 randomOffset = Random.insideUnitSphere * Mathf.Tan(m_spreadAngle * Mathf.Deg2Rad);
            Vector3 spreadDirection = (direction + randomOffset).normalized;

            // 速度の大きさにもランダム性を追加
            float velocityVariation = Random.Range(1f - m_velocityRandomness, 1f + m_velocityRandomness);
            float finalMagnitude = baseMagnitude * m_velocityMultiplier * velocityVariation;

            return spreadDirection * finalMagnitude;
        }
    }
}