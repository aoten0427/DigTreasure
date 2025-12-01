using DG.Tweening;
using System;
using System.Collections;
using System.Data;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// プレイ時のローディング画面
/// </summary>
public class LoadingScreen : MonoBehaviour,ILoadScreen
{
    [SerializeField] Image m_loadingGage;

    //ピッケル
    [SerializeField] RectTransform m_pickel;
    [SerializeField] float m_digTime = 1.4f;
    Sequence m_pickelTwenn;

    //ブロック
    [SerializeField] RectTransform m_baseBlock;
    Vector2 m_blockSize;
    [SerializeField] Vector2 m_blocsSizeRange;//xがmin,yがmax
    [SerializeField] RectTransform[] m_blocks = new RectTransform[4];

    private void Start()
    {
        m_blockSize = m_baseBlock.localScale;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        LoadStart();
        CreateBlocks();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        m_pickelTwenn.Kill();
        m_pickelTwenn = null;
        for(int i = 0; i < m_blocks.Length; i++)
        {
            m_blocks[i].gameObject.SetActive(false);
        }
    }

    public void LoadRatio(float ratio)
    {
        m_loadingGage.fillAmount = ratio;
    }

    public LoadType GetLoadType()
    {
        return LoadType.Load1;
    }

    private void LoadStart()
    {
        m_pickelTwenn = null;

        Sequence seq = DOTween.Sequence();
        seq.Append(
            m_pickel.DOLocalRotate(new Vector3(0, 0, -100), m_digTime * 0.4f)
                .SetEase(Ease.OutBack)
        );
        seq.OnStepComplete(() => {
            if (seq.CompletedLoops() >= 0)
            {
                DigBlock();
            }
        });
        seq.AppendInterval(0.1f);
        seq.Append(
            m_pickel.DOLocalRotate(new Vector3(0, 0, 0), m_digTime * 0.5f)
                .SetEase(Ease.InSine)
        );
        seq.AppendInterval(0.2f);
        seq.SetLoops(-1);
        m_pickelTwenn = seq;
    }

    private void CreateBlocks()
    {
        Vector2[] sizes = new Vector2[4];

        //左上のブロックを生成
        sizes[0] = new Vector2(UnityEngine.Random.Range(m_blocsSizeRange.x, m_blocsSizeRange.y),
            UnityEngine.Random.Range(m_blocsSizeRange.x, m_blocsSizeRange.y));
        //右上と左下で生成優先度を変更
        if (sizes[0].x > sizes[0].y)
        {
            sizes[1] = new Vector2(1.0f - sizes[0].x, UnityEngine.Random.Range(m_blocsSizeRange.x, m_blocsSizeRange.y));
            sizes[2] = new Vector2(sizes[0].x, 1.0f - sizes[0].y);
            sizes[3] = new Vector2(sizes[1].x, 1.0f - sizes[1].y);
        }
        else
        {
            sizes[2] = new Vector2(UnityEngine.Random.Range(m_blocsSizeRange.x, m_blocsSizeRange.y), 1.0f - sizes[0].y);
            sizes[1] = new Vector2(1.0f - sizes[0].x, 1.0f - sizes[2].y);
            sizes[3] = new Vector2(1.0f - sizes[2].x, 1.0f - sizes[1].y);
        }
        //右下生成
        
        BlockSetting(m_blocks[0], sizes[0]);
        BlockSetting(m_blocks[1], sizes[1]);
        BlockSetting(m_blocks[2], sizes[2]);
        BlockSetting(m_blocks[3], sizes[3]);


    }

    private void BlockSetting(RectTransform block,Vector2 size)
    {
        block.gameObject.SetActive(true);
        var rb = block.gameObject.GetComponent<Rigidbody2D>();
        rb.gravityScale = 0.0f;
        block.localScale = size;
        block.anchoredPosition = Vector2.zero;
    }


    private void DigBlock()
    {
        float random = UnityEngine.Random.Range(0.0f, 1.0f);
        //破壊個数決定
        int num = random <= 0.7f ? 1 : 2;
        int last = 0;

        for(int i = 0;i < m_blocks.Length;i++)
        {
            last = i;
            //すでにないならパス
            if (!m_blocks[i].gameObject.activeSelf)continue;
            num--;
            StartCoroutine(FallBlock(m_blocks[i]));


            if (num == 0) break;
        }

        if(last == m_blocks.Length - 1)StartCoroutine(Reset());
    }

    //落下
    IEnumerator FallBlock(RectTransform block)
    {
        var rb = block.gameObject.GetComponent<Rigidbody2D>();
        rb.gravityScale = 50.0f;
        var force = new Vector2(UnityEngine.Random.Range(-120, 120), UnityEngine.Random.Range(50, 80));
        rb.AddForce(force, ForceMode2D.Impulse);
        float torque = UnityEngine.Random.Range(-6f, 6f);
        rb.AddTorque(torque, ForceMode2D.Impulse);

        yield return new WaitForSeconds(1.0f);
        block.gameObject.SetActive(false);
    }

    IEnumerator Reset()
    {
        float time = 0.0f;
        while (m_blocks[3].gameObject.activeSelf&&time < 1.1f)
        {
            time += Time.deltaTime;
            yield return null;
        }

        for(int i = 0; i < m_blocks.Length;i++)
        {
            var rb = m_blocks[i].GetComponent<Rigidbody2D>();
            rb.gravityScale = 0.0f;
            m_blocks[i].anchoredPosition = Vector2.zero;
            m_blocks[i].localRotation = Quaternion.identity;
            m_blocks[i].gameObject.SetActive(true);
        }

        CreateBlocks();
    }
}
