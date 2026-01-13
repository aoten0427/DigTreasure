using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Option;

/// <summary>
/// タイトル画面を管理
/// </summary>
public class TitleManager : MonoBehaviour
{
    // 選択状態（1:スタート, 2:チュートリアル）
    private int m_selectIndex = 1;
    private float m_defaultUISize = 4f;
    private float m_uiSize;
    private bool m_isSelected = false;

    [SerializeField] private TitleInput m_titleInput;
    [SerializeField] private GameObject m_startUI;
    [SerializeField] private GameObject m_tutorialUI;
    [SerializeField] private GameObject m_canvas;

    [SerializeField] private float m_uiSpeed = 2f;
    [SerializeField] private float m_uiDeleteTime = 0.5f;
    [SerializeField] private float m_uiGrowTime = 0.5f;

    OptionManager m_optionManager;
    [SerializeField] Credit m_credit;

    public static bool isTransitioned = false;

    private void Start()
    {
        m_optionManager = OptionManager.Instance;

        // 遷移後の場合はUIをアニメーション表示
        if (isTransitioned)
        {
            GameObject[] gameObjects = GetAllChildren(m_canvas);
            foreach (GameObject gameObject in gameObjects)
            {
                Vector3 originScale = gameObject.transform.localScale;
                gameObject.transform.localScale = Vector3.zero;
                gameObject.transform.DOScale(originScale, m_uiGrowTime);
            }
        }

        // 入力イベントの登録
        m_titleInput.OnLeft += OnLeft;
        m_titleInput.OnRight += OnRight;
        m_titleInput.OnSelect += OnSelect;
        m_titleInput.OnPause += OnPause;
        m_titleInput.OnCancel += OnCancel;

        var sound = SoundPlayer.Instance;
        if (sound) sound.PlayBGM(BGMType.Title);
    }

    private void OnDestroy()
    {
        // DOTweenのアニメーションを停止
        KillAllTweens();

        // 入力イベントの解除
        m_titleInput.OnLeft -= OnLeft;
        m_titleInput.OnRight -= OnRight;
        m_titleInput.OnSelect -= OnSelect;
        m_titleInput.OnPause -= OnPause;
        m_titleInput.OnCancel -= OnCancel;
    }

    /// <summary>
    /// 全てのDOTweenアニメーションを停止
    /// </summary>
    private void KillAllTweens()
    {
        // キャンバスが既に破棄されている場合は何もしない
        if (m_canvas == null) return;

        GameObject[] gameObjects = GetAllChildren(m_canvas);
        foreach (GameObject gameObject in gameObjects)
        {
            // オブジェクトが既に破棄されている場合はスキップ
            if (gameObject == null) continue;
            gameObject.transform.DOKill();
        }
    }

    private void Update()
    {
        // 選択済みの場合は処理しない
        if (m_isSelected) return;
        if (m_optionManager.IsActive) return;

        // 選択中UIのアニメーション
        m_uiSize = Mathf.Sin(Time.time * m_uiSpeed) * 0.5f + 4.5f;
        if (m_selectIndex == 1)
        {
            m_startUI.transform.localScale = new Vector3(m_uiSize, m_uiSize, 1f);
        }
        else if (m_selectIndex == 2)
        {
            m_tutorialUI.transform.localScale = new Vector3(m_uiSize, m_uiSize, 1f);
        }
    }

    #region Input Event Handlers

    /// <summary>
    /// 左入力時の処理
    /// </summary>
    private void OnLeft(bool pressed)
    {
        if (!pressed || m_isSelected||m_optionManager.IsActive) return;
        PushLeft();
    }

    /// <summary>
    /// 右入力時の処理
    /// </summary>
    private void OnRight(bool pressed)
    {
        if (!pressed || m_isSelected || m_optionManager.IsActive) return;
        PushRight();
    }

    /// <summary>
    /// 決定入力時の処理
    /// </summary>
    private void OnSelect(bool pressed)
    {
        if (!pressed || m_isSelected || m_optionManager.IsActive) return;
        OpenSelect();
    }

    /// <summary>
    /// ポーズ入力時の処理
    /// </summary>
    private void OnPause(bool pressed)
    {
        if (!pressed || m_isSelected || m_optionManager.IsActive||m_credit.isOpen) return;
        OpenOption();
    }

    private void OnCancel(bool pressed)
    {
        if(!pressed|| m_isSelected) return;

        if(m_credit.isOpen)
        {
            m_credit.Close();
        }else if(m_optionManager.IsActive)
        {
            CloseOption();
        }
        
    }

    #endregion

    #region UI Control

    /// <summary>
    /// 設定画面を開く
    /// </summary>
    private void OpenOption()
    {
        if (m_optionManager != null) m_optionManager.Open();
    }

    private void CloseOption()
    {
        if (m_optionManager != null) m_optionManager.Close();
    }

    /// <summary>
    /// 選択を実行
    /// </summary>
    private void OpenSelect()
    {
        var sound = SoundPlayer.Instance;
        if (sound) sound.PlaySE(SEType.ButtonClick);
        if (m_selectIndex == 1)
        {
            GameStart();
        }
        else if (m_selectIndex == 2)
        {
            TutorialStart();
        }
        else
        {
            Debug.LogError("想定外のエラーが起きました");
        }
    }

    /// <summary>
    /// ゲームを開始
    /// </summary>
    private void GameStart()
    {
        // UIを縮小アニメーション
        GameObject[] gameObjects = GetAllChildren(m_canvas);
        foreach (GameObject gameObject in gameObjects)
        {
            gameObject.transform.DOScale(Vector3.zero, m_uiDeleteTime);
        }
        m_isSelected = true;

        // シーン遷移
        isTransitioned = true;
        FadeManager.instance.ChangeScene("1_Entrance");
    }

    /// <summary>
    /// チュートリアルを開始
    /// </summary>
    private void TutorialStart()
    {
        m_credit.Open();
    }

    /// <summary>
    /// 左を選択
    /// </summary>
    private void PushLeft()
    {
        m_selectIndex = 1;
        m_tutorialUI.transform.localScale = new Vector3(m_defaultUISize, m_defaultUISize, 1f);

        var sound = SoundPlayer.Instance;
        if (sound) sound.PlaySE(SEType.ButtonMove);
    }

    /// <summary>
    /// 右を選択
    /// </summary>
    private void PushRight()
    {
        m_selectIndex = 2;
        m_startUI.transform.localScale = new Vector3(m_defaultUISize, m_defaultUISize, 1f);

        var sound = SoundPlayer.Instance;
        if (sound) sound.PlaySE(SEType.ButtonMove);
    }

    #endregion

    #region Utility

    /// <summary>
    /// 全ての子オブジェクトを取得
    /// </summary>
    private GameObject[] GetAllChildren(GameObject parent)
    {
        Transform[] allTransforms = parent.GetComponentsInChildren<Transform>(true);
        List<GameObject> objList = new List<GameObject>();

        foreach (Transform child in allTransforms)
        {
            // 親自身を除外
            if (child.gameObject != parent)
            {
                objList.Add(child.gameObject);
            }
        }

        return objList.ToArray();
    }

    #endregion
}
