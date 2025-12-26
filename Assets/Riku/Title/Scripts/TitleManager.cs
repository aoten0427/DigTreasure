using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class TitleManager : MonoBehaviour
{
    private int isSelect = 1; //1がスタート,2がチュートリアル
    private float defaultUIsize = 4f;
    private float uiSize;
    private bool isSelected = false;

    [SerializeField] private GameObject startUI;
    [SerializeField] private GameObject tutorialUI;
    [SerializeField] private GameObject canvas;

    [SerializeField] private float uiSpeed = 2f;
    [SerializeField] private float uiDeleteTime = .5f;
    [SerializeField] private float uiGrowTime = .5f;

    public static bool isTransitioned = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (isTransitioned)
        {
            GameObject[] gameObjects = GetAllChildren(canvas);
            foreach (GameObject gameObject in gameObjects)
            {
                Vector3 originScale = gameObject.transform.localScale;
                gameObject.transform.localScale = Vector3.zero;
                gameObject.transform.DOScale(originScale, uiGrowTime);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isSelected)
        {
            
            return;
        }

        uiSize = Mathf.Sin(Time.time * uiSpeed) * 0.5f + 4.5f;
        if (isSelect == 1) startUI.transform.localScale = new Vector3(uiSize, uiSize, 1f);
        else if (isSelect == 2) tutorialUI.transform.localScale = new Vector3(uiSize, uiSize, 1f);

        if (Input.GetKeyDown(KeyCode.LeftArrow)) PushLeft();
        if (Input.GetKeyDown(KeyCode.RightArrow)) PushRight();

        if (Input.GetKeyDown(KeyCode.Escape)) OpenSetting();
        if (Input.GetKeyDown(KeyCode.UpArrow)) OpenSelect();
    }

    private void OpenSetting()
    {
        Debug.Log("設定を開いている予定だよ");
    }

    private void OpenSelect()
    {
        if (isSelect == 1) GameStart();
        else if (isSelect == 2) TutorialStart();
        else Debug.LogError("予期せぬエラーが起きました");
    }

    private void GameStart()
    {
        GameObject[] gameObjects = GetAllChildren(canvas);
        foreach (GameObject gameObject in gameObjects)
        {
            gameObject.transform.DOScale(Vector3.zero, uiDeleteTime);
        }
        isSelected = true;

        isTransitioned = true;
        FadeManager.instance.ChangeScene("ToTitleTest");
    }

    private void TutorialStart()
    {
        Debug.Log("チュートリアルが始まる予定だよ");
    }

    private void PushLeft()
    {
        isSelect = 1;
        tutorialUI.transform.localScale = new Vector3(defaultUIsize, defaultUIsize, 1f);
    }

    private void PushRight()
    {
        isSelect = 2;
        startUI.transform.localScale = new Vector3(defaultUIsize, defaultUIsize, 1f);
    }

    private GameObject[] GetAllChildren(GameObject parent)
    {
        // GetComponentsInChildren<Transform>(true) で非アクティブなものも含め全取得
        Transform[] allTransforms = parent.GetComponentsInChildren<Transform>(true);
        List<GameObject> objList = new List<GameObject>();

        foreach (Transform child in allTransforms)
        {
            // GetComponentsInChildrenは親自身(Canvas)も含まれるため、
            // 親自身を除外したい場合は if (child.gameObject != parent) を入れる
            if (child.gameObject != parent)
            {
                objList.Add(child.gameObject);
            }
        }

        return objList.ToArray();
    }
}
