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

        if (item is IDroppable droppable)
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            meshFilter.mesh = droppable.ItemMesh;
        }
    }

    private void FixedUpdate()
    {
        if (Time.time - CreateTime <= 0.2f) return;

        int nums = Physics.OverlapSphereNonAlloc(transform.position, transform.localScale.x / 2 / 2, _colliders, _mask);
        for (int i = 0; i < nums; i++)
        {
            if (gameObject == _colliders[i].gameObject) continue;

            if (_colliders[i].gameObject.TryGetComponent(out GroundItem other) && Time.time - other.CreateTime > 0.2f && ItemObj.Data.Id == other.ItemObj.Data.Id)
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


