using System;
using System.Collections;
using GameplayEvents;
using GameplayEvents.Tasks;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class TaskCompletionCinematicController : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private TaskCompletedSignalSO triggerSignal;
    [SerializeField] private bool playOnce = true;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private CameraController gameplayCameraController;
    [SerializeField] private Transform elevatedCameraPose;
    [SerializeField, Min(0.01f)] private float cameraMoveDuration = 1.2f;
    [SerializeField]
    private AnimationCurve cameraEase =
        AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Time Passage")]
    [SerializeField] private DayNightTransitionController dayNight;
    [SerializeField, Min(0.01f)] private float transitionDuration = 3f;

    [Header("Optional Screen Flash")]
    [SerializeField] private CanvasGroup transitionOverlay;
    [SerializeField, Range(0f, 1f)] private float midpointFlashStrength = 1f;
    [SerializeField, Min(0.01f)] private float restoreFadeDuration = 0.25f;

    private IDisposable signalSubscription;
    private Coroutine activeRoutine;
    private PauseToken cutscenePause;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool cameraDriverWasEnabled;
    private bool cameraCaptured;
    private bool hasPlayed;

    private void OnEnable()
    {
        if (triggerSignal == null)
        {
            Debug.LogError(
                "Cinematic trigger signal is not assigned.",
                this);
            return;
        }

        signalSubscription = GameEventHub.Subscribe(
            triggerSignal,
            OnTaskCompleted);
    }

    private void OnDisable()
    {
        signalSubscription?.Dispose();
        signalSubscription = null;

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }

        RestoreRuntimeState();
    }

    public void Play()
    {
        if (activeRoutine != null ||
            (playOnce && hasPlayed))
        {
            return;
        }

        if (!ValidateConfiguration())
        {
            return;
        }

        hasPlayed = true;
        activeRoutine = StartCoroutine(PlaySequence());
    }

    private void OnTaskCompleted(TaskCompletedEvent payload)
    {
        Play();
    }

    private IEnumerator PlaySequence()
    {
        cutscenePause = PauseManager.Acquire(PauseReason.Cutscene);

        originalCameraPosition = cameraTransform.position;
        originalCameraRotation = cameraTransform.rotation;
        cameraCaptured = true;

        if (gameplayCameraController != null)
        {
            cameraDriverWasEnabled = gameplayCameraController.enabled;
            gameplayCameraController.enabled = false;
        }

        SetOverlayInteraction(true);

        yield return MoveCameraUp();
        yield return PlayTimePassage();
        yield return FadeOverlay(1f, restoreFadeDuration);

        RestoreCamera();

        yield return FadeOverlay(0f, restoreFadeDuration);

        RestoreRuntimeState();
    }

    private IEnumerator MoveCameraUp()
    {
        Vector3 startPosition = cameraTransform.position;
        Quaternion startRotation = cameraTransform.rotation;
        float elapsed = 0f;

        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float time = Mathf.Clamp01(
                elapsed / cameraMoveDuration);

            float easedTime = cameraEase.Evaluate(time);

            cameraTransform.position = Vector3.Lerp(
                startPosition,
                elevatedCameraPose.position,
                easedTime);

            cameraTransform.rotation = Quaternion.Slerp(
                startRotation,
                elevatedCameraPose.rotation,
                easedTime);

            yield return null;
        }

        cameraTransform.SetPositionAndRotation(
            elevatedCameraPose.position,
            elevatedCameraPose.rotation);
    }

    private IEnumerator PlayTimePassage()
    {
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float time = Mathf.Clamp01(elapsed / transitionDuration);

            dayNight.Apply(time);
            SetOverlayAlpha(CalculateMidpointFlash(time));

            yield return null;
        }

        dayNight.CompleteAtNight();
        SetOverlayAlpha(0f);
    }

    private float CalculateMidpointFlash(float time)
    {
        float distanceFromMiddle = Mathf.Abs(time - 0.5f) * 2f;
        float pulse = 1f - Mathf.Clamp01(distanceFromMiddle);

        // 让白屏只集中在切换 Skybox 的短暂区间。
        pulse = Mathf.Pow(pulse, 8f);

        return pulse * midpointFlashStrength;
    }

    private IEnumerator FadeOverlay(float targetAlpha, float duration)
    {
        if (transitionOverlay == null)
        {
            yield break;
        }

        float startAlpha = transitionOverlay.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float time = Mathf.Clamp01(elapsed / duration);

            transitionOverlay.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                time);

            yield return null;
        }

        transitionOverlay.alpha = targetAlpha;
    }

    private bool ValidateConfiguration()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform == null ||
            elevatedCameraPose == null ||
            dayNight == null)
        {
            Debug.LogError(
                "Camera, elevated pose and day/night controller are required.",
                this);
            return false;
        }

        return true;
    }

    private void RestoreRuntimeState()
    {
        RestoreCamera();

        if (gameplayCameraController != null)
        {
            gameplayCameraController.enabled = cameraDriverWasEnabled;
        }

        SetOverlayAlpha(0f);
        SetOverlayInteraction(false);

        cutscenePause?.Dispose();
        cutscenePause = null;
        activeRoutine = null;
    }

    private void RestoreCamera()
    {
        if (!cameraCaptured || cameraTransform == null)
        {
            return;
        }

        cameraTransform.SetPositionAndRotation(
            originalCameraPosition,
            originalCameraRotation);

        cameraCaptured = false;
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (transitionOverlay != null)
        {
            transitionOverlay.alpha = alpha;
        }
    }

    private void SetOverlayInteraction(bool enabled)
    {
        if (transitionOverlay == null)
        {
            return;
        }

        transitionOverlay.blocksRaycasts = enabled;
        transitionOverlay.interactable = false;
    }
}