using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// プレイヤーインベントリロジッククラス
/// </summary>
public class PlayerInventoryLogic : MonoBehaviour
{
    [Header("宝設定")]
    [SerializeField] private float m_treasureLostPercent = 0.3f;
    [SerializeField] private float m_treasureHeight = 2f;
    [SerializeField] private Vector2 m_treasureDropRadiusRange = new Vector2(1f, 3f);

    //PlayerManager参照
    private PlayerManager m_manager;

    //ネットワーク宝スポナー（オンライン時のみ使用）
    private NetworkTreasureSpawner m_networkTreasureSpawner;

    //宝リスト
    private TreasureList m_treasureList;
    private Dictionary<int, int> m_treasures = new Dictionary<int, int>();
    private List<Treasure> m_pickupBlacklist = new List<Treasure>();

    //掘りポイント
    private int m_digPoints;

    //イベント
    public event Action<int, int> OnUpdateTreasure;

    //プロパティ
    public int TotalPoints { get; private set; }
    public int TreasureCount { get; private set; }
    public int DigPoints => m_digPoints;

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize(PlayerManager manager)
    {
        m_manager = manager;

        //宝リスト読み込み
        m_treasureList = Resources.Load<TreasureList>("Treasure/TreasureList");
        if (m_treasureList != null)
        {
            for (int i = 0; i < m_treasureList.allTreasure.Count; i++)
            {
                m_treasures[i] = 0;
            }
        }

        //オンラインモードの場合、NetworkTreasureSpawnerを検索
        if (m_manager != null && m_manager.IsOnlineMode)
        {
            m_networkTreasureSpawner = FindFirstObjectByType<NetworkTreasureSpawner>();
            if (m_networkTreasureSpawner == null)
            {
                Debug.LogWarning("[PlayerInventoryLogic] NetworkTreasureSpawnerが見つかりません");
            }
        }
    }

    /// <summary>
    /// 宝のキー取得
    /// </summary>
    private int Key(TreasureSO type)
    {
        return m_treasureList.allTreasure.IndexOf(type);
    }

    /// <summary>
    /// 宝を追加
    /// </summary>
    public void AddTreasure(TreasureSO type, int amount)
    {
        int key = Key(type);
        if (m_treasures.ContainsKey(key))
        {
            m_treasures[key] += amount;
        }
        else
        {
            m_treasures[key] = amount;
        }
        CalculatePoints();

        //イベント発火
        m_manager?.Events?.InvokeTreasureChanged(TreasureCount);
    }

    /// <summary>
    /// 宝を削除
    /// </summary>
    public void RemoveTreasure(TreasureSO type, int amount)
    {
        int key = Key(type);
        if (m_treasures.ContainsKey(key))
        {
            m_treasures[key] = Mathf.Max(0, m_treasures[key] - amount);
        }
        CalculatePoints();

        //イベント発火
        m_manager?.Events?.InvokeTreasureChanged(TreasureCount);
    }

    /// <summary>
    /// 指定タイプの宝を持っているか
    /// </summary>
    public bool HasTreasureOfType(TreasureSO type)
    {
        int key = Key(type);
        return m_treasures.ContainsKey(key) && m_treasures[key] > 0;
    }

    /// <summary>
    /// 何か宝を持っているか
    /// </summary>
    public bool HasTreasure()
    {
        foreach (var kvp in m_treasures)
        {
            if (kvp.Value > 0) return true;
        }
        return false;
    }

    /// <summary>
    /// 指定タイプの宝の数
    /// </summary>
    public int AmountOfTreasureType(TreasureSO type)
    {
        int key = Key(type);
        return m_treasures.ContainsKey(key) ? m_treasures[key] : 0;
    }

    /// <summary>
    /// 全宝の数
    /// </summary>
    public int AmountOfTreasure()
    {
        int total = 0;
        foreach (var kvp in m_treasures)
        {
            total += kvp.Value;
        }
        return total;
    }

    /// <summary>
    /// ランダムに宝を取得
    /// </summary>
    public List<TreasureSO> GetRandomTreasures(int amount)
    {
        List<TreasureSO> result = new List<TreasureSO>();
        foreach (var kvp in m_treasures)
        {
            for (int i = 0; i < kvp.Value; i++)
            {
                result.Add(m_treasureList.allTreasure[kvp.Key]);
            }
        }

        //現在の個数以上なら全部返す
        if (amount >= result.Count)
            return result;

        //要求分減らす
        int iterations = result.Count - amount;
        for (int i = 0; i < iterations; i++)
        {
            result.RemoveAt(UnityEngine.Random.Range(0, result.Count));
            if (result.Count == 0) break;
        }
        return result;
    }

    /// <summary>
    /// ポイント計算
    /// </summary>
    private void CalculatePoints()
    {
        int score = 0;
        int num = 0;
        foreach (var kvp in m_treasures)
        {
            if (m_treasureList != null && kvp.Key < m_treasureList.allTreasure.Count)
            {
                int point = m_treasureList.allTreasure[kvp.Key].point;
                score += point * kvp.Value;
                num += kvp.Value;
            }
        }
        TotalPoints = score;
        TreasureCount = num;

        //更新通知
        OnUpdateTreasure?.Invoke(score, num);
    }

    /// <summary>
    /// 宝落とし処理（オンライン時のみ宝をドロップ）
    /// </summary>
    public void LoseTreasure()
    {
        int amountToLose = (int)(m_treasureLostPercent * AmountOfTreasure());
        if (amountToLose <= 0) return;

        List<TreasureSO> treasureLost = GetRandomTreasures(amountToLose);

        for (int i = 0; i < treasureLost.Count; i++)
        {
            //オンラインモードの場合のみ宝をドロップ
            if (m_manager != null && m_manager.IsOnlineMode && m_networkTreasureSpawner != null)
            {
                m_networkTreasureSpawner.DropTreasure(
                    transform.position + new Vector3(0, m_treasureHeight, 0),
                    treasureLost[i].point,
                    m_treasureList.allTreasure.IndexOf(treasureLost[i])
                );
            }

            RemoveTreasure(treasureLost[i], 1);
        }

        //イベント発火
        m_manager?.Events?.InvokeTreasureLost(amountToLose);
    }

    /// <summary>
    /// 掘りポイント追加
    /// </summary>
    public void AddDigPoints(int points)
    {
        m_digPoints += points;
    }

    /// <summary>
    /// トリガー処理（宝拾い）
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Treasure treasure))
        {
            if (treasure.IsInvalid) return;
            if (m_pickupBlacklist.Contains(treasure)) return;

            AddTreasure(m_treasureList.allTreasure[treasure.MeshIndex], 1);
            m_pickupBlacklist.Add(treasure);
            treasure.RPC_RequestDespawn();
            StartCoroutine(PickupBuffer(treasure));
        }
    }

    /// <summary>
    /// 拾いバッファ
    /// </summary>
    private IEnumerator PickupBuffer(Treasure treasure)
    {
        while (treasure != null)
            yield return null;
        if (m_pickupBlacklist.Contains(treasure))
            m_pickupBlacklist.Remove(treasure);
    }
}
