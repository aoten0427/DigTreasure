using UnityEngine;

/// <summary>
/// 使用するデバイスを選択(gitには送らないため個人で変更していい)
/// </summary>
public class InputType
{
    public static IInput UseInput()
    {
        return new KeybordInput();
        //return new ControllerInput();
    }
}
