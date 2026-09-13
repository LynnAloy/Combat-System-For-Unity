using UnityEngine;

[CreateAssetMenu(fileName = "TimeOfDayPreset", menuName = "Rendering/Time Of Day Preset")]
public sealed class TimeOfDayPresetSO : ScriptableObject
{
    [Header("Sun")]
    [SerializeField] private Vector3 sunEuler;
    [SerializeField] private Color sunColor = Color.white;
    [SerializeField, Min(0f)] private float sunIntensity = 1f;

    [Header("Environment")]
    [SerializeField] private Color ambientColor = Color.gray;
    [SerializeField, Min(0f)] private float ambientIntensity = 1f;
    [SerializeField] private Color fogColor = Color.gray;
    [SerializeField, Min(0f)] private float fogDensity = 0.01f;
    [SerializeField] private Material skybox;

    [Header("Post-processing")]
    [SerializeField] private float postExposure;
    [SerializeField] private Color colorFilter = Color.white;
    [SerializeField, Range(-100f, 100f)] private float temperature;

    public Vector3 SunEuler => sunEuler;
    public Color SunColor => sunColor;
    public float SunIntensity => sunIntensity;
    public Color AmbientColor => ambientColor;
    public float AmbientIntensity => ambientIntensity;
    public Color FogColor => fogColor;
    public float FogDensity => fogDensity;
    public Material Skybox => skybox;
    public float PostExposure => postExposure;
    public Color ColorFilter => colorFilter;
    public float Temperature => temperature;
}