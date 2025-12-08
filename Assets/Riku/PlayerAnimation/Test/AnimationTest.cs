using UnityEngine;

/// <summary>
/// プレイヤーの基本的なアニメーション（歩行、攻撃、防御、スタン）をテストするためのクラスです。
/// キーボードの1-4キーで各アニメーションを制御します。
/// </summary>
public class AnimationTest : MonoBehaviour
{
    private Animator animator;

    private bool isWalk = false;
    private bool isAttack = false;
    private bool isDefence = false;
    private bool isStun = false;
    // isDigについてはDigTest.csを参照
    //private bool isDig = false;

    /// <summary>
    /// 初期化時にAnimatorコンポーネントを取得します。
    /// </summary>
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// 毎フレーム呼び出され、キー入力を監視してアニメーションを更新します。
    /// </summary>
    void Update()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (stateInfo.IsName("Attack"))
        {
            if (stateInfo.normalizedTime > 1f)
            {
                isAttack = false;
                animator.SetBool("isAttack", isAttack);
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            isWalk = !isWalk;
            animator.SetBool("isWalk", isWalk);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            isAttack = true;
            animator.SetBool("isAttack", isAttack);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            isDefence = !isDefence;
            animator.SetBool("isDefense", isDefence);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            isStun = !isStun;
            animator.SetBool("isStun", isStun);
        }
    }
}
