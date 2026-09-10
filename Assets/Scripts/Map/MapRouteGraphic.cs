using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public sealed class MapRouteGraphic : MaskableGraphic
{
    [SerializeField, Min(1f)]private float thickness = 6f;

    private readonly List<Vector2> points = new();

    protected override void Awake()
    {
        base.Awake();
        raycastTarget = false;
    }

    public void SetPoints(IReadOnlyList<Vector2> newPoints)
    {
        points.Clear();

        if (newPoints != null)
        {
            for (int i = 0; i < newPoints.Count; i++)
            {
                points.Add(newPoints[i]);
            }
        }

        SetVerticesDirty();
    }

    public void ClearRoute()
    {
        points.Clear();
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vertexHelper)
    {
        vertexHelper.Clear();

        if (points.Count < 2)
        {
            return;
        }

        float halfThickness = thickness * 0.5f;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 start = points[i];
            Vector2 end = points[i + 1];
            Vector2 direction = end - start;

            if (direction.sqrMagnitude <= Mathf.Epsilon)
            {
                continue;
            }

            direction.Normalize();

            Vector2 normal = new Vector2(-direction.y, direction.x) * halfThickness;

            AddQuad(vertexHelper, start - normal, start + normal, end + normal, end - normal);
        }
    }

    private void AddQuad(VertexHelper vertexHelper, Vector2 bottomLeft, Vector2 topLeft, Vector2 topRight, Vector2 bottomRight)
    {
        int startIndex = vertexHelper.currentVertCount;

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = bottomLeft;
        vertexHelper.AddVert(vertex);

        vertex.position = topLeft;
        vertexHelper.AddVert(vertex);

        vertex.position = topRight;
        vertexHelper.AddVert(vertex);

        vertex.position = bottomRight;
        vertexHelper.AddVert(vertex);

        vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);

        vertexHelper.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        thickness = Mathf.Max(1f, thickness);
        SetVerticesDirty();
    }
}