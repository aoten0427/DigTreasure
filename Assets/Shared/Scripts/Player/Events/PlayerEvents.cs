using System;
using UnityEngine;

/// <summary>
/// プレイヤーのインスタンスイベント集約クラス
/// 静的イベントを排除し、各プレイヤーごとに独立したイベントを提供
/// </summary>
public class PlayerEvents
{
    //スタン関連
    public event Action OnStunStart;
    public event Action OnStunEnd;

    //バリアー関連
    public event Action OnBarrierStart;
    public event Action OnBarrierEnd;

    //ダメージ関連
    public event Action<float> OnDamageReceived;
    public event Action<float> OnDamageDealt;

    //宝関連
    public event Action<int> OnTreasureChanged;
    public event Action<int> OnTreasureLost;

    //移動関連（アニメーション用）
    public event Action<Vector2> OnMove;
    public event Action OnJump;
    public event Action<bool> OnGroundedChanged;

    //アクション関連（アニメーション用）
    public event Action OnAttack;
    public event Action<int> OnDig;

    //ノックバック関連
    public event Action<Vector3, float> OnKnockback;

    //免疫関連
    public event Action OnImmunityStart;
    public event Action OnImmunityEnd;

    /// <summary>
    /// スタン開始を通知
    /// </summary>
    public void InvokeStunStart()
    {
        OnStunStart?.Invoke();
    }

    /// <summary>
    /// スタン終了を通知
    /// </summary>
    public void InvokeStunEnd()
    {
        OnStunEnd?.Invoke();
    }

    /// <summary>
    /// バリアー開始を通知
    /// </summary>
    public void InvokeBarrierStart()
    {
        OnBarrierStart?.Invoke();
    }

    /// <summary>
    /// バリアー終了を通知
    /// </summary>
    public void InvokeBarrierEnd()
    {
        OnBarrierEnd?.Invoke();
    }

    /// <summary>
    /// ダメージ受けを通知
    /// </summary>
    public void InvokeDamageReceived(float damage)
    {
        OnDamageReceived?.Invoke(damage);
    }

    /// <summary>
    /// ダメージ与えを通知
    /// </summary>
    public void InvokeDamageDealt(float damage)
    {
        OnDamageDealt?.Invoke(damage);
    }

    /// <summary>
    /// 宝変更を通知
    /// </summary>
    public void InvokeTreasureChanged(int newAmount)
    {
        OnTreasureChanged?.Invoke(newAmount);
    }

    /// <summary>
    /// 宝落としを通知
    /// </summary>
    public void InvokeTreasureLost(int lostAmount)
    {
        OnTreasureLost?.Invoke(lostAmount);
    }

    /// <summary>
    /// 移動を通知
    /// </summary>
    public void InvokeMove(Vector2 moveInput)
    {
        OnMove?.Invoke(moveInput);
    }

    /// <summary>
    /// ジャンプを通知
    /// </summary>
    public void InvokeJump()
    {
        OnJump?.Invoke();
    }

    /// <summary>
    /// 地面接地状態変更を通知
    /// </summary>
    public void InvokeGroundedChanged(bool isGrounded)
    {
        OnGroundedChanged?.Invoke(isGrounded);
    }

    /// <summary>
    /// 攻撃を通知
    /// </summary>
    public void InvokeAttack()
    {
        OnAttack?.Invoke();
    }

    /// <summary>
    /// 掘りを通知
    /// </summary>
    public void InvokeDig(int digDirection)
    {
        OnDig?.Invoke(digDirection);
    }

    /// <summary>
    /// ノックバックを通知
    /// </summary>
    public void InvokeKnockback(Vector3 direction, float force)
    {
        OnKnockback?.Invoke(direction, force);
    }

    /// <summary>
    /// 免疫開始を通知
    /// </summary>
    public void InvokeImmunityStart()
    {
        OnImmunityStart?.Invoke();
    }

    /// <summary>
    /// 免疫終了を通知
    /// </summary>
    public void InvokeImmunityEnd()
    {
        OnImmunityEnd?.Invoke();
    }

    /// <summary>
    /// 全イベントをクリア
    /// </summary>
    public void ClearAllEvents()
    {
        OnStunStart = null;
        OnStunEnd = null;
        OnBarrierStart = null;
        OnBarrierEnd = null;
        OnDamageReceived = null;
        OnDamageDealt = null;
        OnTreasureChanged = null;
        OnTreasureLost = null;
        OnMove = null;
        OnJump = null;
        OnGroundedChanged = null;
        OnAttack = null;
        OnDig = null;
        OnKnockback = null;
        OnImmunityStart = null;
        OnImmunityEnd = null;
    }
}
