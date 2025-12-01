using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    private Camera m_camera;
    [SerializeField] private SpriteRenderer m_spriteRenderer;
    [SerializeField] private List<Sprite> m_sprites = new List<Sprite>();
    [SerializeField] private float m_switchTime = 0.1f;

    private Coroutine m_animationCoroutine;
    private bool m_isPlaying;

    private void Start()
    {
        if (m_spriteRenderer == null)
        {
            m_spriteRenderer = GetComponent<SpriteRenderer>();
        }

        m_camera = Camera.main;
    }


    private void LateUpdate()
    {
        if (m_camera == null || m_spriteRenderer == null) return;

        // ビルボード
        transform.forward = m_camera.transform.forward;
    }

    public void PlayAnimation(Action onComplete = null)
    {
        if (m_isPlaying) return;
        transform.parent = null;

        if (m_sprites == null || m_sprites.Count == 0)
        {
            Debug.LogWarning($"[Explosion] スプライトリストが空です: {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        if (m_spriteRenderer == null)
        {
            Debug.LogError($"[Explosion] SpriteRendererが見つかりません: {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        m_animationCoroutine = StartCoroutine(AnimationCoroutine(onComplete));
    }

    private IEnumerator AnimationCoroutine(Action onComplete)
    {
        m_isPlaying = true;

        var waitTime = new WaitForSeconds(m_switchTime);

        for (int i = 0; i < m_sprites.Count; i++)
        {
            if (m_sprites[i] != null)
            {
                m_spriteRenderer.sprite = m_sprites[i];
            }

            yield return waitTime;
        }

        m_isPlaying = false;
        Destroy(gameObject);
        onComplete?.Invoke();
    }

    public void StopAnimation()
    {
        if (m_animationCoroutine != null)
        {
            StopCoroutine(m_animationCoroutine);
            m_animationCoroutine = null;
        }
        m_isPlaying = false;
    }

    private void OnDestroy()
    {
        StopAnimation();
    }
}