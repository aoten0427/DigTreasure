using UnityEngine;

public interface IInput
{
    public bool Attack();

    public bool Dig();

    public bool Jump();

    public Vector3 Move();

    public bool LookOn();

    public bool Gard();
}
