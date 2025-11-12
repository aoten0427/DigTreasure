using Fusion;
using UnityEngine;
using UniRx;
using System.Collections;

namespace NetWork
{
    /// <summary>
    /// ロード画面の生成・管理を担当するサービス
    /// </summary>
    public class LoadingService : MonoBehaviour
    {
        [Header("Loading Settings")]
        [SerializeField] private GameObject m_loadPrefab;
        [SerializeField] private bool m_isLog = false;

        private LoadManager m_loadManager;
        private ReactiveProperty<float> m_currentLoadEvent;

        /// <summary>
        /// サービスを初期化
        /// </summary>
        public void Initialize(NetworkRunner runner)
        {
            CreateLoadScreen();
        }

        public void DataReset()
        {
            m_loadManager.SetLoadScreen(LoadType.None);
        }

        /// <summary>
        /// シーンロード開始時の処理
        /// </summary>
        public void OnSceneLoadStart(NetworkRunner runner)
        {
            m_loadManager = LoadManager.Instance;
            if (m_loadManager != null)
            {
                //m_currentLoadEvent = AddLoadingEvent(1.0f,"基本ローディング");
                m_loadManager.Show();
                if (m_isLog) Debug.Log("[LoadingService] ロード画面を表示しました。");
            }
        }

        /// <summary>
        /// 現在のロードイベントを完了
        /// </summary>
        public void CompleteLoading()
        {
            //StartCoroutine(LoadEventFinish());
        }

        IEnumerator LoadEventFinish()
        {
            yield return new WaitForSeconds(0.1f);

            if (m_currentLoadEvent != null)
            {
                //m_currentLoadEvent.Value = 1.0f;
                if (m_isLog) Debug.Log("[LoadingService] ロード完了を通知しました。");
            }
        }

        /// <summary>
        /// ロード中に行うイベントを登録
        /// </summary>
        public ReactiveProperty<float> AddLoadingEvent(float weight = 1.0f,string ownerName = null)
        {
            if (m_loadManager == null)
            {
                CreateLoadScreen();
            }
            return m_loadManager?.AddTask(weight,ownerName);
        }

        /// <summary>
        /// ロード画面のタイプを設定
        /// </summary>
        public void SetLoadScreen(LoadType loadType)
        {
            m_loadManager = LoadManager.Instance;
            m_loadManager?.SetLoadScreen(loadType);
        }

        /// <summary>
        /// ロード画面が設定されているか確認
        /// </summary>
        public bool HasLoadScreen()
        {
            return m_loadManager != null && m_loadManager.HasLoadScreen();
        }

        /// <summary>
        /// LoadManagerを取得
        /// </summary>
        public LoadManager GetLoadManager()
        {
            return m_loadManager;
        }

        /// <summary>
        /// ロード画面を生成
        /// </summary>
        private void CreateLoadScreen()
        {
            if (m_loadPrefab == null)
            {
                if (m_isLog) Debug.LogWarning("[LoadingService] ロード画面プレハブが設定されていません。");
                return;
            }
            if (m_loadManager != null) return;

            m_loadManager = LoadManager.Instance;

            if(m_loadManager == null)
            {
                Debug.Log("ロードマネージャー生成");
                var loadObject = Instantiate(m_loadPrefab);
                DontDestroyOnLoad(loadObject);
                m_loadManager = loadObject.GetComponent<LoadManager>();
                if (m_loadManager == null)
                {
                    m_loadManager = loadObject.AddComponent<LoadManager>();
                }
            }
            
        }
    }
}
