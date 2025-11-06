using UnityEngine;
using Fusion;
using System;
using UnityEngine.SceneManagement;
using System.Linq;
using NetWork;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Internal;
using UniRx;

public class PlayManager : NetworkBehaviour
{
    enum GameState
    {
        Preparation,
        StartContDown,
        Play,
        Pause,
        End
    }



    //ランチャー
    NetWork.GameLauncher m_gameLauncher;

    //初期化フラグ
    bool m_isInitialized = false;
    //初期化が終了したユーザーを記録
    [Networked, Capacity(4)]
    private NetworkLinkedList<PlayerRef> m_completePlayer => default;

    //状態
    GameState m_gameState = GameState.Preparation;


    [SerializeField] private double GameTime = 60;
    //開始時間
    [Networked] private double m_startTime { get; set; }
    //ゲームプレイ時間
    [Networked] private double m_gameTimer { get; set; }//ポーズなどで再同期のためのnetworked
    //
    private ReactiveProperty<double> m_ongameTimer = new ReactiveProperty<double>(0);
    public IReadOnlyReactiveProperty<double> GameTimer => m_ongameTimer;

    //カウントダウン
    [SerializeField] StartCountDown m_startCountDown;
    //ゲームが開始したときに呼ばれる処理
    public event Action OnGameStartAction;
    //ゲームが終了した際に呼ばれる処理
    public event Action OnGameEndAction;

    [SerializeField] bool m_isLog = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       m_gameLauncher = GameLauncher.Instance;
    }


    public override void Spawned()
    {
        m_gameTimer = GameTime;
        m_ongameTimer.Value = m_gameTimer;

        StartCoroutine(Wait());
    }


    private void FixedUpdate()
    {
        if (Runner == null) return;

        //ゲームの開始
        if (m_gameState == GameState.Preparation && m_startTime > 0 && Runner.SimulationTime >= m_startTime)
        {
            m_gameState = GameState.StartContDown;
            m_startCountDown.CountDownStart(() =>
            {
                m_gameState = GameState.Play;
                OnGameStartAction?.Invoke();
            });
        }

        if (m_gameState == GameState.Play)
        {
            m_gameTimer -= Time.deltaTime;
            m_ongameTimer.Value = m_gameTimer;
        }

        if (m_gameState == GameState.Play && m_gameTimer <= 0)
        {
            if (Object.HasStateAuthority) Runner.LoadScene(SceneRef.FromIndex(3), LoadSceneMode.Single);
            m_gameState = GameState.End;
        }
    }

    public override void FixedUpdateNetwork()
    {

        if (Runner.IsSharedModeMasterClient)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                Runner.LoadScene(SceneRef.FromIndex(3), LoadSceneMode.Single);
            }
        }
    }


    /// <summary>
    /// ゲームの初期化
    /// </summary>
    private async void InitializeGame()
    {
        if (m_isInitialized) return;

        var initializers = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
            .OfType<IPlayInitialize>()
            .OrderBy(i => (int)i.Priority + i.PriorityOffset)
            .ToList();


        if (initializers.Count == 0)
        {
            if(m_isLog)Debug.LogWarning("[PlayManager] 初期か内容が見つかりませんでした");
            return;
        }

        if (m_isLog) Debug.Log($"[PlayManager] {initializers.Count}個の初期化が見つかりました");

        var maintask = m_gameLauncher.AddLoadingEvent();

        //ロードタスクを生成
        List<ReactiveProperty<float>> tasks = new List<ReactiveProperty<float>>();
        foreach(var initializer in initializers)
        {
            tasks.Add(m_gameLauncher.AddLoadingEvent(initializer.LoadWeight, initializer.Name));
        }



        int totalInitializers = initializers.Count;
        for (int i = 0; i < initializers.Count; i++)
        {

            var initializer = initializers[i];

            initializer.SetManager(this);

            await initializer.InitializeAsync(tasks[i]);

            //必ずタスクを完了させる
            tasks[i].Value = 1.0f;

            if (m_isLog) Debug.Log($"[PlayManager] [{i + 1}/{totalInitializers}] 初期化完了");
        }

        if (m_isLog) Debug.Log("[PlayManager] 全ての初期化完了");

        maintask.Value = 1.0f;

        m_isInitialized = true;

        m_gameLauncher.SetLoadScreen(LoadType.None);

        //初期化が終わったことを通知
        RPC_GenerationCompletionNotification(Runner.LocalPlayer);
    }

    IEnumerator Wait()
    {
        yield return null;

        InitializeGame();
    }

    /// <summary>
    ///全ての初期化を通知
    /// </summary>
    /// <param name="user"></param>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_GenerationCompletionNotification(PlayerRef user)
    {
        m_completePlayer.Add(user);

        if (m_isLog) Debug.Log(user + "PlayManagerの初期化が完了しました" + Time.time);

        if (m_completePlayer.Count >= Runner.ActivePlayers.Count())
        {
            //開始時間を設定
            double now = Runner.SimulationTime;
            m_startTime = now + 3.0;
        }
    }


}
