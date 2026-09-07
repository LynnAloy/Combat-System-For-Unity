using System;
using System.Collections.Generic;
using UnityEngine;

public enum PauseReason
{
    Inventory,
    Shop,
    Dialogue,
    Quest,
    SystemMenu,
    Cutscene
}

public sealed class PauseToken : IDisposable
{
    private int id;

    internal PauseToken(int id)
    {
        this.id = id;
    }

    public void Dispose()
    {
        if (id == 0)
        {
            return;
        }

        PauseManager.Release(id);
        id = 0;
    }
}

public static class PauseManager
{
    private static readonly Dictionary<int, PauseReason> activePauses = new();
    private static int nextTokenId;
    private static float timeScaleBeforePause = 1f;

    public static bool IsPaused => activePauses.Count > 0;
    public static event Action<bool> PauseChanged;

    public static PauseToken Acquire(PauseReason reason)
    {
        bool wasPaused = IsPaused;
        int tokenId = ++nextTokenId;
        activePauses.Add(tokenId, reason);

        if (!wasPaused)
        {
            timeScaleBeforePause = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            PauseChanged?.Invoke(true);
        }

        return new PauseToken(tokenId);
    }

    internal static void Release(int tokenId)
    {
        if (!activePauses.Remove(tokenId) || IsPaused)
        {
            return;
        }

        Time.timeScale = timeScaleBeforePause;
        PauseChanged?.Invoke(false);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        activePauses.Clear();
        nextTokenId = 0;
        timeScaleBeforePause = 1f;
        Time.timeScale = 1f;
        PauseChanged = null;
    }
}