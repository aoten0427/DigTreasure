using UnityEngine;
using System.Collections.Generic;

namespace VoxelWorld
{
    /// <summary>
    /// 分離オブジェクト管理クラス
    /// </summary>
    public class SeparationManager : MonoBehaviour
    {
        // 分離オブジェクト管理
        private HashSet<SeparatedVoxelObject> m_separatedObjects = new HashSet<SeparatedVoxelObject>();

        /// <summary>
        /// 初期化
        /// </summary>
        public void InitializeManager()
        {
            m_separatedObjects.Clear();
        }

        /// <summary>
        /// 分離オブジェクトを即座に登録
        /// </summary>
        /// <param name="separatedObject">登録する分離オブジェクト</param>
        public void RegisterSeparatedObjectImmediate(SeparatedVoxelObject separatedObject)
        {
            if (separatedObject != null)
            {
                m_separatedObjects.Add(separatedObject);
            }
        }

        /// <summary>
        /// 分離オブジェクトの登録を解除
        /// </summary>
        /// <param name="separatedObject">登録解除する分離オブジェクト</param>
        public void UnregisterSeparatedObject(SeparatedVoxelObject separatedObject)
        {
            if (separatedObject != null)
            {
                m_separatedObjects.Remove(separatedObject);
            }
        }

        /// <summary>
        /// 範囲内の分離オブジェクトを検索
        /// </summary>
        /// <param name="center">検索中心位置</param>
        /// <param name="radius">検索半径</param>
        /// <returns>範囲内の分離オブジェクトリスト</returns>
        public List<SeparatedVoxelObject> FindObjectsInRange(Vector3 center, float radius)
        {
            var result = new List<SeparatedVoxelObject>();
            float radiusSqr = radius * radius;

            foreach (var obj in m_separatedObjects)
            {
                if (obj != null && (obj.transform.position - center).sqrMagnitude <= radiusSqr)
                {
                    result.Add(obj);
                }
            }

            return result;
        }

        /// <summary>
        /// 分離オブジェクト数を取得
        /// </summary>
        /// <returns>現在の分離オブジェクト数</returns>
        public int GetSeparatedObjectCount()
        {
            return m_separatedObjects.Count;
        }

        /// <summary>
        /// 全分離オブジェクトを取得
        /// </summary>
        /// <returns>全分離オブジェクトのリスト</returns>
        public List<SeparatedVoxelObject> GetAllSeparatedObjects()
        {
            return new List<SeparatedVoxelObject>(m_separatedObjects);
        }

        /// <summary>
        /// 分離オブジェクトを強制削除
        /// </summary>
        /// <param name="instanceId">削除するオブジェクトのインスタンスID</param>
        public void ForceDestroySeparatedObject(int instanceId)
        {
            SeparatedVoxelObject targetObject = null;
            foreach (var obj in m_separatedObjects)
            {
                if (obj != null && obj.GetInstanceID() == instanceId)
                {
                    targetObject = obj;
                    break;
                }
            }

            if (targetObject != null)
            {
                m_separatedObjects.Remove(targetObject);
                if (Application.isPlaying)
                {
                    Destroy(targetObject.gameObject);
                }
                else
                {
                    DestroyImmediate(targetObject.gameObject);
                }
            }
        }

        /// <summary>
        /// 全分離オブジェクトをクリア
        /// </summary>
        public void ClearAllSeparatedObjects()
        {
            foreach (var obj in m_separatedObjects)
            {
                if (obj != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(obj.gameObject);
                    }
                    else
                    {
                        DestroyImmediate(obj.gameObject);
                    }
                }
            }
            m_separatedObjects.Clear();
        }

    }
}
