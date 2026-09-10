using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class WorldMapInteractionController :MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
{
    [Header("References")]
    [SerializeField] private Camera worldMapCamera;
    [SerializeField] private Terrain mapTerrain;

    [Header("Zoom")]
    [SerializeField, Min(0.01f)]
    private float minOrthographicSize = 50f;

    [SerializeField, Min(0.01f)]
    private float maxOrthographicSize = 550f;

    [SerializeField, Min(0.01f)]
    private float zoomStep = 40f;

    [SerializeField]
    private bool zoomTowardsPointer = true;

    private RectTransform mapRect;

    private Vector2 dragStartLocalPosition;
    private Vector3 cameraPositionAtDragStart;

    private Vector3 initialCameraPosition;
    private float initialOrthographicSize;

    private void Awake()
    {
        mapRect = transform as RectTransform;

        if (worldMapCamera == null)
        {
            Debug.LogError($"{nameof(WorldMapInteractionController)} requires a World Map Camera.", this);

            enabled = false;
            return;
        }

        if (!worldMapCamera.orthographic)
        {
            Debug.LogError("World Map Camera must use Orthographic projection.", worldMapCamera);

            enabled = false;
            return;
        }

        initialCameraPosition = worldMapCamera.transform.position;
        initialOrthographicSize = worldMapCamera.orthographicSize;

        ClampCameraToTerrain();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (!TryGetLocalPointerPosition(eventData, out dragStartLocalPosition))
        {
            return;
        }

        cameraPositionAtDragStart = worldMapCamera.transform.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (!TryGetLocalPointerPosition(eventData, out Vector2 currentLocalPosition))
        {
            return;
        }

        Rect rect = mapRect.rect;

        if (rect.width <= 0f || rect.height <= 0f)
        {
            return;
        }

        Vector2 localDelta = currentLocalPosition - dragStartLocalPosition;

        float visibleWorldHeight = worldMapCamera.orthographicSize * 2f;

        float visibleWorldWidth = visibleWorldHeight * worldMapCamera.aspect;

        float horizontalMovement = localDelta.x / rect.width * visibleWorldWidth;

        float verticalMovement = localDelta.y / rect.height * visibleWorldHeight;

        Vector3 worldMovement = worldMapCamera.transform.right * horizontalMovement + worldMapCamera.transform.up * verticalMovement;

        Vector3 targetPosition = cameraPositionAtDragStart - worldMovement;

        // 地图相机只在 XZ 平面移动。
        targetPosition.y = cameraPositionAtDragStart.y;

        worldMapCamera.transform.position = targetPosition;

        ClampCameraToTerrain();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (Mathf.Approximately(eventData.scrollDelta.y, 0f))
        {
            return;
        }

        Vector3 worldPointBeforeZoom = default;
        bool hasPointerWorldPoint = zoomTowardsPointer && TryGetPointerWorldPosition(eventData, out worldPointBeforeZoom);

        float newSize = worldMapCamera.orthographicSize - eventData.scrollDelta.y * zoomStep;

        newSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);

        if (Mathf.Approximately(newSize, worldMapCamera.orthographicSize))
        {
            return;
        }

        worldMapCamera.orthographicSize = newSize;

        if (hasPointerWorldPoint && TryGetPointerWorldPosition(eventData, out Vector3 worldPointAfterZoom))
        {
            Vector3 cameraPosition = worldMapCamera.transform.position;

            Vector3 correction = worldPointBeforeZoom - worldPointAfterZoom;

            cameraPosition += correction;
            cameraPosition.y = worldMapCamera.transform.position.y;

            worldMapCamera.transform.position = cameraPosition;
        }

        ClampCameraToTerrain();
    }

    public void ResetView()
    {
        if (worldMapCamera == null)
        {
            return;
        }

        worldMapCamera.transform.position = initialCameraPosition;

        worldMapCamera.orthographicSize = initialOrthographicSize;

        ClampCameraToTerrain();
    }

    private bool TryGetLocalPointerPosition(
        PointerEventData eventData,
        out Vector2 localPosition)
    {
        Camera eventCamera = eventData.pressEventCamera ?? eventData.enterEventCamera;

        return RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                mapRect,
                eventData.position,
                eventCamera,
                out localPosition);
    }

    private bool TryGetPointerWorldPosition(PointerEventData eventData, out Vector3 worldPosition)
    {
        worldPosition = default;

        if (!TryGetLocalPointerPosition(
            eventData,
            out Vector2 localPosition))
        {
            return false;
        }

        Rect rect = mapRect.rect;

        if (rect.width <= 0f || rect.height <= 0f)
        {
            return false;
        }

        float viewportX =
            Mathf.InverseLerp(
                rect.xMin,
                rect.xMax,
                localPosition.x);

        float viewportY =
            Mathf.InverseLerp(
                rect.yMin,
                rect.yMax,
                localPosition.y);

        worldPosition =
            worldMapCamera.ViewportToWorldPoint(
                new Vector3(
                    viewportX,
                    viewportY,
                    worldMapCamera.nearClipPlane));

        return true;
    }

    private void ClampCameraToTerrain()
    {
        if (worldMapCamera == null ||
            mapTerrain == null ||
            mapTerrain.terrainData == null)
        {
            return;
        }

        Vector3 terrainOrigin =
            mapTerrain.transform.position;

        Vector3 terrainSize =
            mapTerrain.terrainData.size;

        float minX = terrainOrigin.x;
        float maxX = terrainOrigin.x + terrainSize.x;
        float minZ = terrainOrigin.z;
        float maxZ = terrainOrigin.z + terrainSize.z;

        float halfHeight =
            worldMapCamera.orthographicSize;

        float halfWidth =
            halfHeight * worldMapCamera.aspect;

        Vector3 cameraPosition =
            worldMapCamera.transform.position;

        cameraPosition.x = ClampAxis(
            cameraPosition.x,
            minX,
            maxX,
            halfWidth);

        cameraPosition.z = ClampAxis(
            cameraPosition.z,
            minZ,
            maxZ,
            halfHeight);

        worldMapCamera.transform.position =
            cameraPosition;
    }

    private static float ClampAxis(
        float value,
        float minimum,
        float maximum,
        float cameraExtent)
    {
        float availableSize = maximum - minimum;

        if (cameraExtent * 2f >= availableSize)
        {
            return (minimum + maximum) * 0.5f;
        }

        return Mathf.Clamp(
            value,
            minimum + cameraExtent,
            maximum - cameraExtent);
    }

    private void OnValidate()
    {
        minOrthographicSize =
            Mathf.Max(0.01f, minOrthographicSize);

        maxOrthographicSize =
            Mathf.Max(
                minOrthographicSize,
                maxOrthographicSize);

        zoomStep = Mathf.Max(0.01f, zoomStep);
    }
}