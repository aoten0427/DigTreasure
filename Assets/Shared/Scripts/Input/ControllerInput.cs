using UnityEngine;

/// <summary>
/// コントローラーインプット(仮置きキーボード)
/// </summary>
public class ControllerInput : IInput
{
    public bool Attack()
    {
        return Input.GetKey(KeyCode.U);
    }

    public bool Dig()
    {
        return (Input.GetKeyDown(KeyCode.J));
    }

    public bool Gard()
    {
        return Input.GetKeyDown(KeyCode.I);
    }

    public bool Jump()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }

    public bool LookOn()
    {
        return Input.GetKeyDown(KeyCode.L);
    }

    public Vector3 Move()
    {
        return new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
    }
}
