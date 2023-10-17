using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

public class GroundItem : MonoBehaviour
{
    public ItemObject ItemObj;
    public int Amount;
    public float CreateTime { get; private set; }
    private static readonly Collider[] _colliders = new Collider[10];
    private static LayerMask _mask;
    private static bool _initMask;

    private void Start()
    {
        if (!_initMask)
        {
            _initMask = true;
            _mask = LayerMask.GetMask("GroundItems");
        }
        CreateTime = Time.time;
    }

    public void SetItem(ItemObject item, int amount)
    {
        ItemObj = item;
        Amount = amount;
        CreateTime = Time.time;

        if (item is IDroppableMesh droppable)
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = droppable.ItemMesh;

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            renderer.material = droppable.ItemMaterial;

            transform.localScale = new(droppable.ItemScale, droppable.ItemScale, droppable.ItemScale);

            BoxCollider collider = GetComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = Vector3.one;
        }
        else if (item is IDroppablePrefab dropPrefab)
        {
            GameObject prefab = dropPrefab.ItemPrefab;

            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = prefab.GetComponent<MeshFilter>().sharedMesh;

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            renderer.materials = prefab.GetComponent<MeshRenderer>().sharedMaterials;

            transform.localScale = new(dropPrefab.ItemScale, dropPrefab.ItemScale, dropPrefab.ItemScale);

            BoxCollider collider = GetComponent<BoxCollider>();
            collider.center = dropPrefab.ColliderCenter;
            collider.size = dropPrefab.ColliderSize;
        }
    }

    private void FixedUpdate()
    {
        if (Time.time - CreateTime <= 0.2f) return;

        int nums = Physics.OverlapSphereNonAlloc(transform.position, transform.localScale.x / 2 / 2, _colliders, _mask);
        for (int i = 0; i < nums; i++)
        {
            if (gameObject == _colliders[i].gameObject || !ItemObj.stackable) continue;

            if (_colliders[i].gameObject.TryGetComponent(out GroundItem other) &&
                Time.time - other.CreateTime > 0.2f &&
                ItemObj.data.id == other.ItemObj.data.id
            )
            {
                Amount += other.Amount;
                _colliders[i].gameObject.SetActive(false);
            }
        }
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //}

}


