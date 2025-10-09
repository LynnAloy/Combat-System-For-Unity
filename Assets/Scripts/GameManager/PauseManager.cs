using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseManager : Singleton<PauseManager>
{
    public bool IsPaused { get; private set; } = false;

    public void TogglePause()
    {
        IsPaused = !IsPaused;
    }
}
