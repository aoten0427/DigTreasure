using System.Collections.Generic;
using UnityEngine;
using VoxelWorld;

/// <summary>
/// BaseAttackを継承した統合破壊テストクラス
/// チャンクボクセルと分離オブジェクトの両方に対する統一破壊処理をテスト
/// </summary>
public class TestDestruction : BaseAttack
{
    [Header("テスト設定")]
    [SerializeField] private KeyCode m_testKey = KeyCode.Space;

    void Update()
    {
        if (Input.GetKeyUp(m_testKey))
        {
            //NetWorkDestroy();
            TestUnifiedDestruction();
        }
    }

    /// <summary>
    /// 統合破壊システムをテスト
    /// </summary>
    public void TestUnifiedDestruction()
    {


        // BaseAttackの統合攻撃メソッドを使用
        AttackAtPosition(transform.position);

    }


}
