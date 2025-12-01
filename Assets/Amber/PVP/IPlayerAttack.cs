using UnityEngine;

public interface IPlayerAttack
{
    PlayerProto Player { get; } 
    PlayerCombat Attacker { get; }
}
