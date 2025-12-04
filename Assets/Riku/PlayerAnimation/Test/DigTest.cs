using UnityEngine;

/// <summary>
/// プレイヤーの穴掘りアニメーション（開始、ループ、終了）を制御およびテストするためのクラスです。
/// 5キーで穴掘りアニメーションの開始と終了を制御します。
/// </summary>
public class DigTest : MonoBehaviour
{
    private Animator animator;
    private bool isDig;
    private int lastLoopCount;
    bool animCheck;

    /// <summary>
    /// 初期化時にAnimatorコンポーネントを取得し、変数を初期化します。
    /// </summary>
    void Start()
    {
        animator = GetComponent<Animator>();
        isDig = false;
        lastLoopCount = 0;
        animCheck = false;
    }

    /// <summary>
    /// 毎フレーム呼び出され、キー入力とアニメーションの状態に基づいて穴掘りアニメーションを制御します。
    /// </summary>
    void Update()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // 5キーが押された時の処理
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            // 穴掘りアニメーションの再生中にキーが押された場合、ループを抜けるためにisDigをtrueにする
            if ((stateInfo.IsName("Dig_Start") || stateInfo.IsName("Dig_Loop")) &&
                stateInfo.normalizedTime % 1f >= 0.25f)
            {
                isDig = true;
            }
            // 穴掘りアニメーションが再生されていない場合にキーが押された場合、アニメーションを開始する
            else if (!(stateInfo.IsName("Dig_Start") || stateInfo.IsName("Dig_Loop") || stateInfo.IsName("Dig_End")))
            {
                animator.SetBool("isDig", true);
            }
        }

        // "Dig_Start"アニメーション中の処理
        if (stateInfo.IsName("Dig_Start"))
        {
            // アニメーションが終了し、まだチェックされていない場合
            if (stateInfo.normalizedTime >= 1f && !animCheck)
            {
                // isDigフラグに基づいて次の状態へ遷移
                animator.SetBool("isDig", isDig);
                isDig = false;
                animCheck = true; // チェック済みにする
            }
        }
        // "Dig_Loop"アニメーション中の処理
        else if (stateInfo.IsName("Dig_Loop"))
        {
            if (animCheck && !stateInfo.IsName("Dis_Start")) // Dis_StartはDig_Startのタイポの可能性
            {
                animCheck = false;
            }
            int currentLoopCount = (int)stateInfo.normalizedTime; // 現在のループ回数を取得
            // ループ回数が増え、まだチェックされていない場合
            if (currentLoopCount > lastLoopCount && !animCheck)
            {
                // isDigフラグに基づいて次の状態へ遷移
                animator.SetBool("isDig", isDig);
                isDig = false;
                animCheck = true; // チェック済みにする
            }
            lastLoopCount = currentLoopCount; // ループ回数を更新
        }
        // 上記以外の状態の場合、各変数をリセット
        else
        {
            isDig = false;
            lastLoopCount = 0;
            animCheck = false;
        }
    }
}
