using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(LineRenderer))]
public sealed class WorldRouteRenderer : MonoBehaviour
{
    [Header("Ground")]
    [SerializeField] private Terrain terrain;

    [Header("Route")]
    [SerializeField, Min(0.2f)]
    private float sampleSpacing = 1.5f;

    [SerializeField, Min(0f)]
    private float groundOffset = 0.15f;

    private LineRenderer routeLine;
    private readonly List<Vector3> sampledPoints = new();

    private void Awake()
    {
        routeLine = GetComponent<LineRenderer>();

        routeLine.useWorldSpace = true;
        routeLine.loop = false;
        routeLine.shadowCastingMode = ShadowCastingMode.Off;
        routeLine.receiveShadows = false;
        routeLine.enabled = false;
    }

    public void ShowRoute(IReadOnlyList<Vector3> corners)
    {
        if (terrain == null || corners == null || corners.Count < 2)
        {
            ClearRoute();
            return;
        }

        sampledPoints.Clear();

        for (int segmentIndex = 0; segmentIndex < corners.Count - 1; segmentIndex++)
        {
            Vector3 start = corners[segmentIndex];
            Vector3 end = corners[segmentIndex + 1];

            Vector2 startXZ = new Vector2(start.x, start.z);
            Vector2 endXZ = new Vector2(end.x, end.z);

            float distance = Vector2.Distance(startXZ, endXZ);

            int stepCount = Mathf.Max(1, Mathf.CeilToInt(distance / sampleSpacing));

            int firstStep = segmentIndex == 0 ? 0 : 1;

            for (int step = firstStep; step <= stepCount; step++)
            {
                float t = step / (float)stepCount;

                Vector3 point = Vector3.Lerp(start, end, t);

                point.y = terrain.SampleHeight(point) + terrain.transform.position.y + groundOffset;

                sampledPoints.Add(point);
            }
        }

        routeLine.positionCount = sampledPoints.Count;
        routeLine.SetPositions(sampledPoints.ToArray());
        routeLine.enabled = sampledPoints.Count >= 2;
    }

    public void ClearRoute()
    {
        sampledPoints.Clear();

        if (routeLine == null)
        {
            return;
        }

        routeLine.positionCount = 0;
        routeLine.enabled = false;
    }
}