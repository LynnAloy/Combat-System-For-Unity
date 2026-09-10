using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class WorldMapNavigationController :MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [SerializeField] private Camera worldMapCamera;
    [SerializeField] private Terrain mapTerrain;
    [SerializeField] private Transform player;

    [Header("Map Overlay")]
    [SerializeField] private MapRouteGraphic routeGraphic;
    [SerializeField] private RectTransform routeLayer;
    [SerializeField] private RectTransform markerLayer;
    [SerializeField] private RectTransform destinationMarker;

    [Header("Navigation")]
    [SerializeField, Min(0.1f)]
    private float startSampleDistance = 3f;

    [SerializeField, Min(0.1f)]
    private float destinationSampleDistance = 5f;

    [SerializeField, Min(1f)]
    private float terrainRayDistance = 2000f;

    [SerializeField]
    private string[] allowedAreaNames =
    {
        "Walkable",
        "Road"
    };

    public bool HasDestination { get; private set; }
    public Vector3 Destination { get; private set; }

    public event Action<Vector3> DestinationChanged;
    public event Action DestinationCleared;

    private RectTransform mapRect;
    private TerrainCollider terrainCollider;
    private NavMeshPath navigationPath;
    private Vector3[] worldPathCorners;
    private int navigationAreaMask;

    private readonly List<Vector2> uiRoutePoints = new();

    private void Awake()
    {
        mapRect = transform as RectTransform;
        navigationPath = new NavMeshPath();

        if (mapTerrain != null)
        {
            terrainCollider =
                mapTerrain.GetComponent<TerrainCollider>();
        }

        navigationAreaMask = BuildAreaMask();

        if (destinationMarker != null)
        {
            destinationMarker.gameObject.SetActive(false);
        }

        routeGraphic?.ClearRoute();
    }

    private void LateUpdate()
    {
        if (HasDestination)
        {
            RefreshMapOverlay();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button ==
            PointerEventData.InputButton.Right)
        {
            ClearDestination();
            return;
        }

        if (eventData.button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }

        TrySetDestination(eventData);
    }

    public bool TrySetDestination(
        PointerEventData eventData)
    {
        if (!ValidateReferences())
        {
            return false;
        }

        if (!TryGetMapViewportPosition(
            eventData,
            out Vector2 viewportPosition))
        {
            return false;
        }

        Ray ray = worldMapCamera.ViewportPointToRay(
            new Vector3(viewportPosition.x, viewportPosition.y, 0f));

        if (!terrainCollider.Raycast(ray, out RaycastHit terrainHit, terrainRayDistance))
        {
            return false;
        }

        if (!NavMesh.SamplePosition(player.position, out NavMeshHit startHit, startSampleDistance, navigationAreaMask))
        {
            Debug.LogWarning("Player is not close enough to an allowed NavMesh area.", player);

            return false;
        }

        if (!NavMesh.SamplePosition(terrainHit.point, out NavMeshHit destinationHit, destinationSampleDistance, navigationAreaMask))
        {
            Debug.LogWarning("The selected map position is not reachable.", this);

            return false;
        }

        navigationPath.ClearCorners();

        bool pathFound = NavMesh.CalculatePath(
            startHit.position,
            destinationHit.position,
            navigationAreaMask,
            navigationPath);

        if (!pathFound || navigationPath.status != NavMeshPathStatus.PathComplete)
        {
            Debug.LogWarning($"No complete path to the selected position. " + $"Status: {navigationPath.status}", this);

            return false;
        }

        worldPathCorners = navigationPath.corners;

        if (worldPathCorners == null || worldPathCorners.Length < 2)
        {
            return false;
        }

        Destination = destinationHit.position;
        HasDestination = true;

        if (destinationMarker != null)
        {
            destinationMarker.gameObject.SetActive(true);
        }

        RefreshMapOverlay();
        DestinationChanged?.Invoke(Destination);

        return true;
    }

    public void ClearDestination()
    {
        if (!HasDestination)
        {
            return;
        }

        HasDestination = false;
        worldPathCorners = null;

        routeGraphic?.ClearRoute();

        if (destinationMarker != null)
        {
            destinationMarker.gameObject.SetActive(false);
        }

        DestinationCleared?.Invoke();
    }

    private void RefreshMapOverlay()
    {
        if (worldPathCorners == null)
        {
            return;
        }

        uiRoutePoints.Clear();

        for (int i = 0; i < worldPathCorners.Length; i++)
        {
            Vector3 viewportPoint = worldMapCamera.WorldToViewportPoint(worldPathCorners[i]);

            uiRoutePoints.Add(ViewportToLocalPoint(routeLayer, viewportPoint));
        }

        routeGraphic?.SetPoints(uiRoutePoints);

        if (destinationMarker != null)
        {
            Vector3 viewportPoint =
                worldMapCamera.WorldToViewportPoint(
                    Destination);

            destinationMarker.anchoredPosition =
                ViewportToLocalPoint(
                    markerLayer,
                    viewportPoint);
        }
    }

    private bool TryGetMapViewportPosition(
        PointerEventData eventData,
        out Vector2 viewportPosition)
    {
        viewportPosition = default;

        Camera eventCamera =
            eventData.pressEventCamera ??
            eventData.enterEventCamera;

        if (!RectTransformUtility
            .ScreenPointToLocalPointInRectangle(
                mapRect,
                eventData.position,
                eventCamera,
                out Vector2 localPosition))
        {
            return false;
        }

        Rect rect = mapRect.rect;

        if (rect.width <= 0f || rect.height <= 0f)
        {
            return false;
        }

        viewportPosition.x = Mathf.InverseLerp(
            rect.xMin,
            rect.xMax,
            localPosition.x);

        viewportPosition.y = Mathf.InverseLerp(
            rect.yMin,
            rect.yMax,
            localPosition.y);

        return true;
    }

    private static Vector2 ViewportToLocalPoint(
        RectTransform targetLayer,
        Vector3 viewportPoint)
    {
        if (targetLayer == null)
        {
            return Vector2.zero;
        }

        Rect rect = targetLayer.rect;

        return new Vector2(
            Mathf.Lerp(
                rect.xMin,
                rect.xMax,
                viewportPoint.x),
            Mathf.Lerp(
                rect.yMin,
                rect.yMax,
                viewportPoint.y));
    }

    private int BuildAreaMask()
    {
        int areaMask = 0;

        if (allowedAreaNames != null)
        {
            for (int i = 0;
                 i < allowedAreaNames.Length;
                 i++)
            {
                int areaIndex = NavMesh.GetAreaFromName(
                    allowedAreaNames[i]);

                if (areaIndex < 0)
                {
                    Debug.LogWarning(
                        $"NavMesh area '{allowedAreaNames[i]}' does not exist.",
                        this);

                    continue;
                }

                areaMask |= 1 << areaIndex;
            }
        }

        if (areaMask == 0)
        {
            Debug.LogWarning(
                "No valid NavMesh areas configured. " +
                "Falling back to All Areas.",
                this);

            areaMask = NavMesh.AllAreas;
        }

        return areaMask;
    }

    private bool ValidateReferences()
    {
        if (worldMapCamera == null ||
            mapRect == null ||
            player == null ||
            terrainCollider == null)
        {
            Debug.LogError(
                $"{nameof(WorldMapNavigationController)} " +
                "has missing references.",
                this);

            return false;
        }

        return true;
    }
}