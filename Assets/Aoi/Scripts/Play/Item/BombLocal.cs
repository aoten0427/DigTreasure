using UnityEngine;
using DG.Tweening;

public class BombLocal : VoxelWorld.BaseAttack
{
    [SerializeField] float m_ignitionTime = 3f;//点滅時間
    //制御用Tween
    Tween m_ignitionTween;
    Tween m_blinkTween;
    //ポーズフラグ
    bool m_isPaused;

    bool m_isblink;
    [SerializeField]MeshRenderer m_renderer;
    [SerializeField] Material m_normal;
    [SerializeField] Material m_blink;

    [SerializeField] Explosion m_explosion;

    PlayerCombat m_attacker;
    public PlayerCombat Attacker { get { return m_attacker; } set {  m_attacker = value; } }

    //ボクセルの破壊を行うか
    private bool m_isDestroyd = false;
    public bool IsDestroyd { get {  return m_isDestroyd; } set {  m_isDestroyd = value; } }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.B))
        {
            IgnitionStart();
        }
    }

    /// <summary>
    /// 点火開始
    /// </summary>
    public void IgnitionStart(bool isautoExprosion = false)
    {
        if (m_ignitionTween != null && m_ignitionTween.IsActive())
            return;

        Debug.Log("点火開始！");

        m_isPaused = false;

        m_isblink = false;
        float progress = 0f;

        m_ignitionTween = DOTween.To(() => progress, x => progress = x, 1f, m_ignitionTime)
            .OnUpdate(() =>
            {
                // 点滅速度だけ更新
                float interval = Mathf.Lerp(0.5f, 0.05f, progress);
                m_blinkTween.timeScale = 0.5f / interval;
            })
            .OnStart(() =>
            {
                StartBlink(0.5f); // 初期の点滅間隔で開始
            })
            .OnComplete(() =>
            {
                StopBlink();
                Explosion();
            });

        Rigidbody rb = GetComponent<Rigidbody>();
        if(rb != null)
        {
            rb.AddForce(new Vector3 (0f, 150f, 0f));
        }
    }

    /// <summary>
    /// 点滅
    /// </summary>
    /// <param name="interval"></param>
    void StartBlink(float interval)
    {
        StopBlink();

        m_blinkTween = DOVirtual.DelayedCall(interval, () =>
        {
            BlinkChange();
        })
        .SetLoops(-1, LoopType.Restart)
        .SetEase(Ease.Linear);
    }

    /// <summary>
    /// 点滅の変化
    /// </summary>
    void BlinkChange()
    {
        m_isblink = !m_isblink; ;
        if(m_isblink)
        {
            m_renderer.material = m_blink;
        }
        else
        {
            m_renderer.material = m_normal;
        }
    }

    /// <summary>
    /// 点滅を止める
    /// </summary>
    void StopBlink()
    {
        m_blinkTween?.Kill();
        m_isblink = false;
    }

    /// <summary>
    /// 爆発
    /// </summary>
    public void Explosion()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, AttackRadius,LayerMask.GetMask("Player"));
        foreach (Collider hit in hits)
        {
            var target = hit.GetComponent<PlayerCombat>();
            if(target != null)
            {
                Debug.Log($"{target.gameObject}を見つけました");
                PlayerDamage damage = new PlayerDamage(m_attacker,target,100.0f,10.0f,
                    (target.transform.position - transform.position).normalized,false);
                target.RpcTakeDamage(damage);
            }
        }

        if (m_isDestroyd) AttackAtPosition(transform.position);


        m_renderer.enabled = false;
        m_explosion.PlayAnimation();
        Destroy(gameObject);
    }

    /// <summary>
    /// 中断
    /// </summary>
    public void StopIgnition()
    {
        m_ignitionTween?.Kill();
        StopBlink();
        Debug.Log("点火中断");
    }

    /// <summary>
    /// ポーズ用
    /// </summary>
    /// <param name="pause"></param>
    public void SetPaused(bool pause)
    {
        m_isPaused = pause;

        if (pause)
        {
            m_ignitionTween?.Pause();
            m_blinkTween?.Pause();
        }
        else
        {
            m_ignitionTween?.Play();
            m_blinkTween?.Play();
        }
    }
}
