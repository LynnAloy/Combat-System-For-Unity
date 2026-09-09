using UnityEngine;

[DisallowMultipleComponent]
public sealed class MapController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.M;

    [Header("Map UI")]
    [SerializeField] private GameObject worldMapRoot;

    [SerializeField] private GameObject miniMapRoot;

    [Header("World Map Camera")]
    [SerializeField] private Camera worldMapCamera;

    [Header("Behaviour")]
    [SerializeField] private bool hideMiniMapWhileOpen = true;

    public bool IsOpen { get; private set; }

    private PauseToken mapPauseToken;

    private bool cursorVisibleBeforeOpening;
    private CursorLockMode cursorLockModeBeforeOpening;
    private bool miniMapWasActive;

    private void Awake()
    {
        if (worldMapRoot == gameObject)
        {
            Debug.LogError($"{nameof(MapController)} 不能挂在 World Map Root 本身，" + "否则关闭地图后脚本也会被禁用。", this);

            enabled = false;
            return;
        }

        if (worldMapRoot != null)
        {
            worldMapRoot.SetActive(false);
        }
        else
        {
            Debug.LogWarning($"{nameof(MapController)} 尚未绑定 World Map Root。", this);
        }

        if (worldMapCamera != null)
        {
            worldMapCamera.enabled = false;
        }

        IsOpen = false;
    }

    private void Update()
    {
        if (!Input.GetKeyDown(toggleKey))
        {
            return;
        }

        if (IsOpen)
        {
            CloseMap();
            return;
        }

        // 背包、对话、商店等暂停界面开启时，不再打开地图。
        if (PauseManager.IsPaused)
        {
            return;
        }

        OpenMap();
    }

    public void ToggleMap()
    {
        if (IsOpen)
        {
            CloseMap();
        }
        else
        {
            OpenMap();
        }
    }

    public void OpenMap()
    {
        if (IsOpen || PauseManager.IsPaused)
        {
            return;
        }

        cursorVisibleBeforeOpening = Cursor.visible;
        cursorLockModeBeforeOpening = Cursor.lockState;

        if (miniMapRoot != null)
        {
            miniMapWasActive = miniMapRoot.activeSelf;
        }

        // 必须通过现有 PauseManager 暂停，不直接设置 Time.timeScale。
        mapPauseToken = PauseManager.Acquire(PauseReason.MapMenu);
        IsOpen = true;

        if (worldMapRoot != null)
        {
            worldMapRoot.SetActive(true);
        }

        if (worldMapCamera != null)
        {
            worldMapCamera.enabled = true;
        }

        if (hideMiniMapWhileOpen && miniMapRoot != null)
        {
            miniMapRoot.SetActive(false);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void CloseMap()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;

        if (worldMapCamera != null)
        {
            worldMapCamera.enabled = false;
        }

        if (worldMapRoot != null)
        {
            worldMapRoot.SetActive(false);
        }

        if (hideMiniMapWhileOpen && miniMapRoot != null)
        {
            miniMapRoot.SetActive(miniMapWasActive);
        }

        ReleaseMapPause();

        // 如果关闭地图后仍有其他暂停原因，则不能抢先锁定鼠标。
        if (!PauseManager.IsPaused)
        {
            Cursor.visible = cursorVisibleBeforeOpening;
            Cursor.lockState = cursorLockModeBeforeOpening;
        }
    }

    private void ReleaseMapPause()
    {
        mapPauseToken?.Dispose();
        mapPauseToken = null;
    }

    private void OnDisable()
    {
        if (IsOpen)
        {
            CloseMap();
        }
        else
        {
            ReleaseMapPause();
        }
    }

    private void OnDestroy()
    {
        ReleaseMapPause();
    }
}