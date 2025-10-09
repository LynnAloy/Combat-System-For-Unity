using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkinnedMeshHighlighter : MonoBehaviour
{
    [SerializeField] private List<SkinnedMeshRenderer> meshToHighlight;
    [SerializeField] private Material originalMaterial;
    [SerializeField] private Material highlightedMaterial;

    public void HighlightMesh(bool isHighlighted)
    {
        foreach (var mesh in meshToHighlight)
        {
            mesh.material = isHighlighted ? highlightedMaterial : originalMaterial;
        }
    }
}
