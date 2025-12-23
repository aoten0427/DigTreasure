using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 掘り攻撃クラス
/// </summary>
public class Dig : VoxelWorld.BaseAttack,IPlayerAttack
{
    [SerializeField] private GameObject m_holder;
    [SerializeField] private float m_colliderAppearanceTime = 0.5f;
    [SerializeField] private PlayerManager m_manager;
    [SerializeField] private PlayerCombatLogic m_combatLogic;
    [SerializeField]private Collider m_collider;


    public PlayerManager Manager => m_manager;
    public PlayerCombatLogic CombatLogic => m_combatLogic;

    public PlayerManager Player => m_manager;

    /// <summary>
    /// 掘り実行
    /// </summary>
    public void DigPoint(Vector3 position, Vector3 direction,Action<int> OnComplete = null)
    {
        direction.Normalize();

        StartCoroutine(ColliderAppearance());

        AttackAtPosition(position, direction,OnComplete);
    }

    /// <summary>
    /// コライダー表示コルーチン
    /// </summary>
    private IEnumerator ColliderAppearance()
    {
        
        if (m_collider == null) yield break;

        m_collider.enabled = true;

        yield return new WaitForSeconds(m_colliderAppearanceTime);

        m_collider.enabled = false;
    }
}
