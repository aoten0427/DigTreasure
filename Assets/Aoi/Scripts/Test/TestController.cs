using UnityEngine;
using UnityEngine.InputSystem;

public class TestController : MonoBehaviour
{
    // このクラスは、Input Actions アセットから生成されたクラスを使う想定です
    private PlayerControls _controls;  // 例：PlayerControls は Input Actions アセットで生成された名前

    private Vector2 _moveInput;
    private bool _jumpPressed;

    private void Awake()
    {
        _controls = new PlayerControls();

        // Move アクションが発生したら呼ばれる処理
        _controls.GamePlay.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _controls.GamePlay.Move.canceled += ctx => _moveInput = Vector2.zero;

        // Jump ボタン
        _controls.GamePlay.Jump.performed += ctx => _jumpPressed = true;
    }

    private void OnEnable()
    {
        _controls.GamePlay.Enable();
    }

    private void OnDisable()
    {
        _controls.GamePlay.Disable();
    }

    private void Update()
    {
        // 移動入力
        Vector2 move = _moveInput;
        // 例：X 軸：横移動、Y 軸：縦移動
        Debug.Log($"Move Input: {move}");

        // ジャンプ入力
        if (_jumpPressed)
        {
            Debug.Log("Jump pressed!");
            _jumpPressed = false;
        }
    }
}
