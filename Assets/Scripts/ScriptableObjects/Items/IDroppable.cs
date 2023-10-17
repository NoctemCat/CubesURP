
using UnityEngine;

public interface IDroppableMesh
{
    float ItemScale { get; set; }
    Mesh ItemMesh { get; set; }
    Material ItemMaterial { get; set; }
}

public interface IDroppablePrefab
{
    float ItemScale { get; set; }
    Vector3 ColliderCenter { get; set; }
    Vector3 ColliderSize { get; set; }
    GameObject ItemPrefab { get; set; }
}