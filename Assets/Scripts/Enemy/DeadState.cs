using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadState : State<EnemyController> 
{
    override public void Enter(EnemyController owner) 
    {
        owner.VisionSensor.gameObject.SetActive(false);
        EnemyManager.Instance.RemoveEnemyInRange(owner);
        owner.NavAgent.enabled = false;
        owner.CharacterController.enabled = false;
    }
}
