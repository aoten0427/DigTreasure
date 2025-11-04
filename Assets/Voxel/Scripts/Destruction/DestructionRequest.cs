using System;
using System.Numerics;
using UnityEngine;

namespace VoxelWorld
{
    /// <summary>
    /// 破壊要求を表すデータ構造
    /// DestructionManagerで破壊処理をキューイングする際に使用
    /// </summary>
    public class DestructionRequest
    {
        //破壊形状
        public IDestructionShape Shape { get; private set; }
        //攻撃力
        public float AttackPower { get; private set; }
        //エフェクトが出る方向(zeroの場合全方向)
        public UnityEngine.Vector3 EffectDirection { get; private set; }
        //完了時コールバック（破壊数付き）
        public Action<int> OnCompleteWithCount { get; private set; }



        /// <summary>
        /// コンストラクタ（破壊数付きコールバック）
        /// </summary>
        /// <param name="shape">破壊形状</param>
        /// <param name="attackPower">攻撃力</param>
        /// <param name="onCompleteWithCount">完了時コールバック（破壊数を受け取る）</param>
        public DestructionRequest(IDestructionShape shape, float attackPower, UnityEngine.Vector3 direction, Action<int> onCompleteWithCount)
        {
            Shape = shape ?? throw new ArgumentNullException(nameof(shape));
            AttackPower = Math.Max(0, attackPower);
            EffectDirection = direction;
            OnCompleteWithCount = onCompleteWithCount;
        }
    }

}