using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CharacterStateHealthModifierSO : CharacterStateModifierSO
{
    public override void AffectCharacter(GameObject character, float val)
    {
        MeeleFighter fighter = character.GetComponent<MeeleFighter>();
        if(fighter != null)
        {
            fighter.Heal(val);
            Debug.Log($"恢复 {val} 点生命值，当前生命值: {fighter.Health}");
        }
        else
        {
            Debug.Log("MeeleFighter is missing!");
        }
    }
}
