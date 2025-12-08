using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Unity.Cinemachine;
using System.Xml;

/// <summary>
/// プレイヤー表示クラス
/// カメラ設定、名前表示、エフェクト等を担当
/// </summary>
public class PlayerView : MonoBehaviour
{
    [Header("カメラ")]
    [SerializeField] private Transform m_cameraTarget;
    [SerializeField] private Vector3 m_cameraOffset = new Vector3(0, 2, -5);

    [Header("名前表示")]
    [SerializeField] private TextMeshPro m_nameText;
    private string m_name;

    [Header("エフェクト")]
    [SerializeField] private GameObject m_stunEffect;
    [SerializeField] private GameObject m_barrierEffect;
    [SerializeField] private GameObject m_damageEffectPrefab;
    [SerializeField] private GameObject m_immunityEffect;



    //PlayerManager参照
    private PlayerManager m_manager;

    //ローカルプレイヤーか
    private bool m_isLocalPlayer;

    //メインカメラ
    [SerializeField]private GameObject m_mainCamera;

    int m_colorID = 0;

    [SerializeField]List<MeshRenderer> m_meshRenderers = new List<MeshRenderer>();
    [SerializeField] List<Material> m_colors = new List<Material>();
    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize(PlayerManager manager, bool isLocalPlayer)
    {
        m_manager = manager;
        m_isLocalPlayer = isLocalPlayer;

        //ローカルプレイヤーの場合はカメラ設定
        if (m_isLocalPlayer && m_cameraTarget != null)
        {
            SetupCamera();
        }

        //イベント購読
        if (m_manager?.Events != null)
        {
            m_manager.Events.OnStunStart += ShowStunEffect;
            m_manager.Events.OnStunEnd += HideStunEffect;
            m_manager.Events.OnBarrierStart += ShowBarrierEffect;
            m_manager.Events.OnBarrierEnd += HideBarrierEffect;
            m_manager.Events.OnImmunityStart += ShowImmunityEffect;
            m_manager.Events.OnImmunityEnd += HideImmunityEffect;
        }

        //初期状態
        if (m_stunEffect != null) m_stunEffect.SetActive(false);
        if (m_barrierEffect != null) m_barrierEffect.SetActive(false);
        if (m_immunityEffect != null) m_immunityEffect.SetActive(false);

        //見た目を変更
        SetMaterial(m_colorID);
        if (m_manager.NetworkStateAuthority)
        {
            m_manager?.NetworkState?.RPC_MaterialChange(m_colorID);
            m_manager?.NetworkState?.RPC_NameChange(m_name);
        }
    }

    private void OnDestroy()
    {
        //イベント解除
        if (m_manager?.Events != null)
        {
            m_manager.Events.OnStunStart -= ShowStunEffect;
            m_manager.Events.OnStunEnd -= HideStunEffect;
            m_manager.Events.OnBarrierStart -= ShowBarrierEffect;
            m_manager.Events.OnBarrierEnd -= HideBarrierEffect;
            m_manager.Events.OnImmunityStart -= ShowImmunityEffect;
            m_manager.Events.OnImmunityEnd -= HideImmunityEffect;
        }
    }

    private void LateUpdate()
    {
        m_nameText.transform.rotation = Camera.main.transform.rotation;
        ////名前キャンバスをカメラに向ける
        //if (m_nameCanvas != null && m_mainCamera != null)
        //{
        //    m_nameCanvas.transform.LookAt(
        //        m_nameCanvas.transform.position + m_mainCamera.transform.rotation * Vector3.forward,
        //        m_mainCamera.transform.rotation * Vector3.up
        //    );
        //}
    }

    /// <summary>
    /// カメラ設定
    /// </summary>
    private void SetupCamera()
    {
        m_mainCamera.SetActive(true);
    }

    /// <summary>
    /// 名前設定
    /// </summary>
    public void SetPlayerName(string name)
    {
        m_name = name;
        if (m_nameText != null)
        {
            m_nameText.text = name;
        }
    }

    /// <summary>
    /// スタンエフェクト設定
    /// </summary>
    public void SetStunEffect(bool active)
    {
        
        if (m_stunEffect != null)
        {
            m_stunEffect.SetActive(active);
        }
    }

    /// <summary>
    /// スタンエフェクト表示
    /// </summary>
    private void ShowStunEffect()
    {
        SetStunEffect(true);
    }

    /// <summary>
    /// スタンエフェクト非表示
    /// </summary>
    private void HideStunEffect()
    {
        SetStunEffect(false);
    }

    /// <summary>
    /// バリアーエフェクト設定
    /// </summary>
    public void SetBarrierEffect(bool active)
    {
        if (m_barrierEffect != null)
        {
            m_barrierEffect.SetActive(active);
        }
    }

    /// <summary>
    /// バリアーエフェクト表示
    /// </summary>
    private void ShowBarrierEffect()
    {
        SetBarrierEffect(true);
    }

    /// <summary>
    /// バリアーエフェクト非表示
    /// </summary>
    private void HideBarrierEffect()
    {
        SetBarrierEffect(false);
    }

    /// <summary>
    /// 免疫エフェクト表示
    /// </summary>
    private void ShowImmunityEffect()
    {
        if (m_immunityEffect != null)
        {
            m_immunityEffect.SetActive(true);
        }
    }

    /// <summary>
    /// 免疫エフェクト非表示
    /// </summary>
    private void HideImmunityEffect()
    {
        if (m_immunityEffect != null)
        {
            m_immunityEffect.SetActive(false);
        }
    }

    /// <summary>
    /// ダメージエフェクト再生
    /// </summary>
    public void PlayDamageEffect(Vector3 position, Quaternion rotation)
    {
        Debug.Log("生成");
        if (m_damageEffectPrefab != null)
        {
            Debug.Log("生成成功");
            var effect = Instantiate(m_damageEffectPrefab, position, rotation);
            //Destroy(effect, 2f);
        }
    }



    /// <summary>
    /// マテリアル変更
    /// </summary>
    public void SetMaterialID(int id)
    {
        m_colorID = id;
    }

    public void SetMaterial(int id)
    {
        foreach (var renderer in m_meshRenderers)
        {
            renderer.material = m_colors[id];
        }
    }
}
