using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisionSensor : MonoBehaviour
{
    [SerializeField] private EnemyController enemyController;

    private void Awake()
    {
        enemyController.VisionSensor = this;
    }

    private void OnTriggerEnter(Collider other)
    {
        var meeleFighter = other.GetComponent<MeeleFighter>();
        if(meeleFighter != null)
        {
            enemyController.TargetsInRange.Add(meeleFighter);
            EnemyManager.Instance.AddEnemyInRange(enemyController);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var meeleFighter = other.GetComponent<MeeleFighter>();
        if(meeleFighter != null)
        {
            enemyController.TargetsInRange.Remove(meeleFighter);
            EnemyManager.Instance.RemoveEnemyInRange(enemyController);
        }
    }
}
