

using UnityEngine;

[CreateAssetMenu(fileName = "New Materials Object", menuName = "Cubes/Materials Object")]
public class MaterialsObject : ScriptableObject
{
    public Material SolidMaterial;
    public Material TransparentMaterial;
}