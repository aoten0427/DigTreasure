using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.SceneManagement;

public class FadeManager : MonoBehaviour
{
    public static FadeManager instance;

    public RectTransform fadeInTransform;
    public RectTransform fadeOutTransform;

    public float fadeSpeed = 2f;
    public float power;

    public bool isFadeIn = false;
    public bool isFadeOut = false;

    Material material;

    RectTransform rectTransform;

    bool isChange = false;
    string sceneName;

    public event Action OnFeadInStart;
    public event Action OnFeadInEnd;
    public event Action OnFeadOutStart;
    public event Action OnFeadOutEnd;

    public string titleSceneName;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.GetComponentInParent<Canvas>());
            material = this.GetComponent<Image>().material;
            rectTransform = this.GetComponent<RectTransform>();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (SceneManager.GetActiveScene().name == titleSceneName)
        {
            rectTransform.SetPositionAndRotation(fadeInTransform.position, fadeInTransform.rotation);
            material.SetFloat("_time", 0.3f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isFadeIn || isFadeOut)
        {
            float value = material.GetFloat("_time");
            value += Time.deltaTime / power * fadeSpeed *
                (-1 * Convert.ToInt32(isFadeIn) + Convert.ToInt32(isFadeOut));
            if (value <= 0f)
            {
                isFadeIn = false;
                value = 0f;
                OnFeadInEnd?.Invoke();
            }
            else if (value >= 0.3f && SceneManager.GetActiveScene().name == titleSceneName && isFadeOut)
            {
                isFadeOut = false;
                value = 0.3f;
                OnFeadOutEnd?.Invoke();
            }
            else if (value >= 5.5f)
            {
                isFadeOut = false;
                value = 5.5f;
                OnFeadOutEnd?.Invoke();
            }
            material.SetFloat("_time", value);
            if (isFadeIn)
            {
                if (SceneManager.GetActiveScene().name == titleSceneName) power += Time.deltaTime * 4f;
                else power += Time.deltaTime * 2f;
            }
            else
            {
                if (SceneManager.GetActiveScene().name == titleSceneName) power -= Time.deltaTime * 7f;
                power -= Time.deltaTime * 1f;
            }
        }

        if (!isFadeIn && !isFadeOut && isChange)
        {
            SceneManager.LoadScene(sceneName);
            FadeOut(sceneName);
            isChange = false;
        }
    }

    public void FadeIn()
    {
        isFadeIn = true;
        rectTransform.SetPositionAndRotation(fadeInTransform.position, fadeInTransform.rotation);
        if (SceneManager.GetActiveScene().name == titleSceneName) power = 10f;
        else power = 1f;
        OnFeadInStart?.Invoke();
    }

    public void FadeOut(string nextSceneName)
    {
        isFadeOut = true;
        if (nextSceneName == titleSceneName) rectTransform.SetPositionAndRotation(fadeInTransform.position, fadeInTransform.rotation);
        else rectTransform.SetPositionAndRotation(fadeOutTransform.position, fadeOutTransform.rotation);
        if (nextSceneName == titleSceneName) power = 15f;
        else power = 5f;
        OnFeadOutStart?.Invoke();
    }

    public void ChangeScene(string sceneName)
    {
        this.sceneName = sceneName;
        isChange = true;
        FadeIn();
    }
}
