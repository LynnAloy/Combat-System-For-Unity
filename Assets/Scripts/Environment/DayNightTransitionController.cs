using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
public sealed class DayNightTransitionController : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private Light sun;
    [SerializeField] private Volume globalVolume;

    [Header("Presets")]
    [SerializeField] private TimeOfDayPresetSO dayPreset;
    [SerializeField] private TimeOfDayPresetSO nightPreset;

    [Header("Environment")]
    [SerializeField] private bool useFlatAmbient = true;
    [SerializeField] private bool controlFog = true;
    [SerializeField, Range(0f, 1f)] private float skyboxSwitchPoint = 0.5f;

    private ColorAdjustments colorAdjustments;
    private WhiteBalance whiteBalance;
    private bool initialized;

    private void Awake()
    {
        TryInitialize();
    }

    public void Apply(float normalizedTime)
    {
        if (!initialized && !TryInitialize())
        {
            return;
        }

        float time = Mathf.Clamp01(normalizedTime);
        float smoothTime = Mathf.SmoothStep(0f, 1f, time);

        Quaternion dayRotation = Quaternion.Euler(dayPreset.SunEuler);
        Quaternion nightRotation = Quaternion.Euler(nightPreset.SunEuler);

        sun.transform.rotation = Quaternion.Slerp(
            dayRotation,
            nightRotation,
            smoothTime);

        sun.color = Color.Lerp(
            dayPreset.SunColor,
            nightPreset.SunColor,
            smoothTime);

        sun.intensity = Mathf.Lerp(
            dayPreset.SunIntensity,
            nightPreset.SunIntensity,
            smoothTime);

        ApplyEnvironment(smoothTime);
        ApplySkybox(time);
        ApplyPostProcessing(smoothTime);
    }

    public void CompleteAtNight()
    {
        Apply(1f);

        // 环境探针刷新较昂贵，不要每帧调用。
        DynamicGI.UpdateEnvironment();
    }

    private bool TryInitialize()
    {
        if (sun == null || sun.type != LightType.Directional)
        {
            Debug.LogError(
                "A Directional Light must be assigned.",
                this);
            return false;
        }

        if (dayPreset == null || nightPreset == null)
        {
            Debug.LogError(
                "Day and night presets must be assigned.",
                this);
            return false;
        }

        InitializePostProcessing();
        initialized = true;
        return true;
    }

    private void InitializePostProcessing()
    {
        if (globalVolume == null)
        {
            return;
        }

        // profile 会在运行时克隆 sharedProfile，不会修改磁盘资产。
        VolumeProfile runtimeProfile = globalVolume.profile;

        if (!runtimeProfile.TryGet(out colorAdjustments))
        {
            colorAdjustments = runtimeProfile.Add<ColorAdjustments>(true);
        }

        if (!runtimeProfile.TryGet(out whiteBalance))
        {
            whiteBalance = runtimeProfile.Add<WhiteBalance>(true);
        }

        colorAdjustments.postExposure.overrideState = true;
        colorAdjustments.colorFilter.overrideState = true;
        whiteBalance.temperature.overrideState = true;
    }

    private void ApplyEnvironment(float time)
    {
        if (useFlatAmbient)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(
                dayPreset.AmbientColor,
                nightPreset.AmbientColor,
                time);
        }

        RenderSettings.ambientIntensity = Mathf.Lerp(
            dayPreset.AmbientIntensity,
            nightPreset.AmbientIntensity,
            time);

        if (!controlFog)
        {
            return;
        }

        RenderSettings.fog = true;
        RenderSettings.fogColor = Color.Lerp(
            dayPreset.FogColor,
            nightPreset.FogColor,
            time);

        RenderSettings.fogDensity = Mathf.Lerp(
            dayPreset.FogDensity,
            nightPreset.FogDensity,
            time);
    }

    private void ApplySkybox(float time)
    {
        Material targetSkybox = time < skyboxSwitchPoint
            ? dayPreset.Skybox
            : nightPreset.Skybox;

        if (targetSkybox != null &&
            RenderSettings.skybox != targetSkybox)
        {
            RenderSettings.skybox = targetSkybox;
        }
    }

    private void ApplyPostProcessing(float time)
    {
        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = Mathf.Lerp(
                dayPreset.PostExposure,
                nightPreset.PostExposure,
                time);

            colorAdjustments.colorFilter.value = Color.Lerp(
                dayPreset.ColorFilter,
                nightPreset.ColorFilter,
                time);
        }

        if (whiteBalance != null)
        {
            whiteBalance.temperature.value = Mathf.Lerp(
                dayPreset.Temperature,
                nightPreset.Temperature,
                time);
        }
    }
}