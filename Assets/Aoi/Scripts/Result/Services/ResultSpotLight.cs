using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// リザルト画面のスポットライト演出
/// </summary>
public class ResultSpotLight : MonoBehaviour
{
   

    //プレイヤーオブジェクト
    [SerializeField] GameObject[] m_players;
    //環境光
    [SerializeField] Light m_directionalLight;
    //スポットライト
    [SerializeField] GameObject m_spotLight;

    //ドラムロール時間
    [SerializeField] float m_drumrollTime = 3.0f;

    //カメラオブジェクト
    [SerializeField] GameObject m_cameraObject;
    //カメラの初期位置
    private Vector3 m_initialPosition;
    //カメラの初期回転
    private Quaternion m_initialRotation;
    //カメラの目標位置
    private Vector3 m_targetPosition;
    //カメラの目標回転
    private Quaternion m_targetRotation;
    //ズーム時間
    [SerializeField] float m_zoomTime = 1.5f;
    //カメラ停止距離
    [SerializeField] float m_stopDistance = 3.0f;

    //プレイヤーアニメーター
    [SerializeField] Animator[] m_animator;

    private void Start()
    {
        m_spotLight.SetActive(false);
    }

    /// <summary>
    /// スポットライト演出を開始
    /// </summary>
    public IEnumerator Initialize(List<ResultData> datas)
    {
        if (datas == null || datas.Count == 0) yield break;

        //最終スコアソート
        var sortedDatas = datas.ToList();
        sortedDatas.Sort((a, b) => b.TotalScore.CompareTo(a.TotalScore));

        //環境光を暗くする
        m_directionalLight.DOIntensity(0.0f, 1.0f);

        //ドラムロール演出
        yield return StartCoroutine(Drumroll());

        //1位のプレイヤーにスポットライトを当てる
        GameObject targetPlayer = m_players[sortedDatas[0].Index];
        Vector3 lightPos = targetPlayer.transform.position;
        lightPos.y += 8.0f;
        m_spotLight.transform.position = lightPos;
        m_spotLight.SetActive(true);

        //カメラを1位プレイヤーにズームイン
        Vector3 zoomTarget = targetPlayer.transform.position;
        zoomTarget.y += 1.5f;
        yield return StartCoroutine(ZoomUp(zoomTarget));

        //勝者アニメーション再生
        PlayAnimation(sortedDatas[0].Index);

        //カメラをズームアウト
        yield return StartCoroutine(ZoomBack());
    }

   



    /// <summary>
    /// ドラムロール演出
    /// </summary>
    IEnumerator Drumroll()
    {
        var sound = SoundPlayer.Instance;

        if(sound)
        {
            //ドラムロール開始
            sound.PlaySE(SEType.Drumroll1);
            yield return new WaitForSeconds(m_drumrollTime);

            //ドラムロール停止
            sound.StopAllSE();
            yield return new WaitForSeconds(0.5f);

            //発表音再生
            sound.PlaySE(SEType.Drumroll2);
        }
        else
        {
            yield return new WaitForSeconds(m_drumrollTime + 0.5f);
        }
    }

    /// <summary>
    /// カメラズームイン
    /// </summary>
    IEnumerator ZoomUp(Vector3 target)
    {
        //現在の位置と回転を保存
        m_initialPosition = m_cameraObject.transform.position;
        m_initialRotation = m_cameraObject.transform.rotation;

        //ターゲットへの方向を計算
        Vector3 directionToCamera = (m_cameraObject.transform.position - target).normalized;
        //停止位置を計算
        m_targetPosition = target + (directionToCamera * m_stopDistance);
        //ターゲットを向く回転を計算
        Vector3 lookDirection = target - m_targetPosition;
        m_targetRotation = Quaternion.LookRotation(lookDirection);

        //カメラ移動・回転アニメーション
        m_cameraObject.transform.DOMove(m_targetPosition, m_zoomTime);
        yield return m_cameraObject.transform.DORotateQuaternion(m_targetRotation, m_zoomTime);
    }

    /// <summary>
    /// 勝者アニメーション再生
    /// </summary>
    private void PlayAnimation(int winnerIndex)
    {
        for (int i = 0; i < m_animator.Length; i++)
        {
            if (i == winnerIndex)
            {
                //勝者は喜ぶアニメーション
                m_animator[i].SetBool("isRejoice", true);
            }
            else
            {
                //他プレイヤーは拍手アニメーション
                m_animator[i].SetBool("isApplause", true);
            }
        }
    }

    /// <summary>
    /// カメラズームアウト
    /// </summary>
    IEnumerator ZoomBack()
    {
        //少し待機してから戻る
        yield return new WaitForSeconds(2.0f);

        //カメラを初期位置に戻す
        m_cameraObject.transform.DOMove(m_initialPosition, m_zoomTime);
        m_cameraObject.transform.DORotateQuaternion(m_initialRotation, m_zoomTime);

        //環境光を元に戻す
        m_directionalLight.DOIntensity(1.0f, 1.0f);
    }
}
