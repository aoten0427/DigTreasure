using UnityEngine;
using UniRx;
using NUnit.Framework;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// ロード進行状態管理
/// </summary>
public class LoadManager : MonoBehaviour
{
    private static LoadManager s_instance;
    public static LoadManager Instance
    {
        get
        {
            if (s_instance == null) Debug.LogWarning("[LoadManager]インスタンスがありません");
            return s_instance;
        }
        private set { s_instance = value; }
    }

    //登録リスト
    private List<ReactiveProperty<float>> m_tasks = new List<ReactiveProperty<float>>();
    //タスクに対する重み付け
    private Dictionary<ReactiveProperty<float>, float> m_taskWeight = new Dictionary<ReactiveProperty<float>, float>();
    //タスクの所有者情報
    private Dictionary<ReactiveProperty<float>, string> m_taskOwners = new Dictionary<ReactiveProperty<float>, string>();
    //購読
    private IDisposable m_subscription;
    //セットされているロード
    LoadType m_loadtype = LoadType.None;
    ILoadScreen m_loadScreen;
    //ロードパスクリーン
    [SerializeField]List<GameObject> m_screens = new List<GameObject>();
    //登録スクリーン
    Dictionary<ILoadScreen, GameObject> m_registrationScreen = new Dictionary<ILoadScreen, GameObject>();

    //何かロードが設定されているか
    public bool HasLoadScreen() { return m_loadtype != LoadType.None; }


    private void Awake()
    {
        // シングルトンチェック
        if (s_instance != null && s_instance != this)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);

        //スクリーンを登録
        foreach (var obj in m_screens)
        {
            var screenobj = Instantiate(obj);
            ILoadScreen screen = screenobj.GetComponent<ILoadScreen>();
            //スクリーンがない場合は消去
            if (screen == null)
            {
                Destroy(screenobj);
                continue;
            }
            screenobj.transform.parent = transform;
            screenobj.SetActive(false);
            m_registrationScreen.Add(screen, screenobj);
            Debug.Log("スクリーン登録");
        }
    }


    /// <summary>
    /// タスクを登録
    /// </summary>
    /// <param name="weight"></param>
    /// <param name="ownerName">タスクの所有者名（省略時は呼び出し元のクラス名を自動取得）</param>
    /// <returns></returns>
    public ReactiveProperty<float> AddTask(float weight = 1.0f, string ownerName = null)
    {
        ReactiveProperty<float> task = new ReactiveProperty<float>(0f);

        m_tasks.Add(task);
        m_taskWeight.Add(task, weight);

        // 所有者名が指定されていない場合、スタックトレースから呼び出し元を取得
        if (string.IsNullOrEmpty(ownerName))
        {
            var stackTrace = new System.Diagnostics.StackTrace(1, false);
            var frame = stackTrace.GetFrame(0);
            ownerName = frame?.GetMethod()?.DeclaringType?.Name ?? "Unknown";
        }

        Debug.Log($"タスクを追加:{ownerName}");
        m_taskOwners.Add(task, ownerName);

        RebuildSubscription();

        return task;
    }

    /// <summary>
    /// 購読を再構築
    /// </summary>
    private void RebuildSubscription()
    {
        m_subscription?.Dispose();

        m_subscription = Observable.CombineLatest(m_tasks)
            .Select(values =>
            {
                //加重平均計算
                float totalWeight = m_tasks.Sum(t => m_taskWeight[t]);
                float weightedSum = m_tasks.Select((t, i) => values[i] * m_taskWeight[t]).Sum();

                // 終わっていないタスクを表示
                var incompleteTasks = new List<string>();
                for (int i = 0; i < m_tasks.Count; i++)
                {
                    var task = m_tasks[i];
                    if (values[i] < 1.0f)
                    {
                        incompleteTasks.Add(m_taskOwners[task]);
                    }
                }

                if (incompleteTasks.Count > 0)
                {
                    Debug.Log($"[LoadManager] 未完了タスク: {string.Join(", ", incompleteTasks)}");
                }

                return weightedSum / totalWeight;
            })
            .Do(avg =>
            {
                //Debug.Log($"進捗{avg}");
                if(m_loadScreen != null)
                {
                    m_loadScreen.LoadRatio(avg);
                }
            })
            .First(avg => avg >= 1f)
            .Subscribe(values =>
            {
                // タスクの詳細をログ出力
                Debug.Log($"[LoadManager] ロード完了 - タスク数: {m_tasks.Count}");
                for (int i = 0; i < m_tasks.Count; i++)
                {
                    var task = m_tasks[i];
                    Debug.Log($"[LoadManager] Task {i}: Owner={m_taskOwners[task]}, Progress={task.Value}, Weight={m_taskWeight[task]}");
                }

                Hide();
                m_tasks.Clear();
                m_taskWeight.Clear();
                m_taskOwners.Clear();
                m_subscription.Dispose();

                m_loadScreen = null;
            });
    }

    /// <summary>
    /// ロード画面を表示
    /// </summary>
    public void Show()
    {
        if (m_loadScreen == null) return;
        //if (m_tasks.Count == 0) return;
        m_registrationScreen[m_loadScreen].SetActive(true);
        m_loadScreen.Show();
    }

    //ロード画面を削除
    public void Hide()
    {
        if (m_loadScreen == null) return;
        m_loadScreen.Hide();
        m_registrationScreen[m_loadScreen].SetActive(false);
    }

    /// <summary>
    /// ロードをセット
    /// </summary>
    /// <param name="loadType"></param>
    public void SetLoadScreen(LoadType loadType)
    {
        if (loadType == LoadType.None)
        {
            m_loadScreen = null;
            m_loadtype = LoadType.None;
            return;
        }

        var found = m_registrationScreen.FirstOrDefault(t => t.Key.GetLoadType() == loadType);

        if (found.Key == null)
        {
            Debug.LogWarning($"{loadType}に対応するロードがありませんでした");
            return;
        }

        m_loadScreen = found.Key;
        m_loadtype = m_loadScreen.GetLoadType();

        m_tasks.Clear();
    }
}
