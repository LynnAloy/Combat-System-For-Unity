using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CharacterStateMovementSpeedModifierSO : CharacterStateModifierSO
{
    [SerializeField] private float duration = 30f; 

    public override void AffectCharacter(GameObject character, float val)
    {
        PlayerController player = character.GetComponent<PlayerController>();
        if (player != null)
        {
            var controllerType = player.GetType();
            var movementSpeedField = controllerType.GetField("movementSpeed",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (movementSpeedField != null)
            {
                float currentSpeed = (float)movementSpeedField.GetValue(player);
                float newSpeed = currentSpeed + val;
                movementSpeedField.SetValue(player, newSpeed);

                Debug.Log($"移动速度增加 {val}，当前速度: {newSpeed}");

                player.StartCoroutine(ResetMovementSpeedAfterDelay(player, currentSpeed, duration));
            }
        }
        else
        {
            Debug.LogWarning("PlayerController is missing!");
        }
    }

    private IEnumerator ResetMovementSpeedAfterDelay(PlayerController player, float originalSpeed, float delay)
    {
        yield return new WaitForSeconds(delay);

        var controllerType = player.GetType();
        var movementSpeedField = controllerType.GetField("movementSpeed",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (movementSpeedField != null)
        {
            movementSpeedField.SetValue(player, originalSpeed);
            Debug.Log("移动速度效果结束，恢复原速度");
        }
    }
}