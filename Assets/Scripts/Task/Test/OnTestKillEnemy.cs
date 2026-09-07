using FlexibleTaskSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnTestKillEnemy : MonoBehaviour
{
    [SerializeField] private AllTasks allTasks;

    public void OnKillEnemy()
    {
        allTasks.SendSignal("EnemyKilled");
    }
}
