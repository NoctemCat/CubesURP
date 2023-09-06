using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GroundItemsPool : MonoBehaviour
{
    public List<GameObject> pooledObjects;
    public GameObject objectToPool;
    public int amountToPool;

    private void Awake()
    {
        ServiceLocator.Register(this);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister(this);
    }

    private void Start()
    {
        ServiceLocator.Get<EventSystem>().OnDropItems += DropItems;

        pooledObjects = new List<GameObject>();
        AddToPool(amountToPool);
    }

    private void Update()
    {
    }

    public void AddToPool(int num)
    {
        //amountToPool += num;
        GameObject tmp;
        for (int i = 0; i < num; i++)
        {
            tmp = Instantiate(objectToPool, transform);
            tmp.SetActive(false);
            tmp.SetActive(true);
            tmp.SetActive(false);
            pooledObjects.Add(tmp);
        }
    }

    public GameObject GetPooledObject()
    {
        for (int i = 0; i < pooledObjects.Count; i++)
        {
            if (!pooledObjects[i].activeInHierarchy)
            {
                return pooledObjects[i];
            }
        }
        AddToPool(10);
        return GetPooledObject();
    }

    public void DropItems(Vector3 origin, Vector3 velocity, ItemObject itemObject, int amount)
    {
        GameObject item = GetPooledObject();

        item.SetActive(true);
        item.transform.position = origin;

        BlockPhysics phys = item.GetComponent<BlockPhysics>();
        phys.AddVelocity(velocity);

        GroundItem gItem = item.GetComponent<GroundItem>();
        gItem.SetItem(itemObject, amount);
    }

    //public static void DropItems(Vector3 origin, Vector3 velocity, ItemObject itemObject, int amount)
    //{
    //    GameObject item = ServiceLocator.Get<GroundItemsPool>().GetPooledObject();

    //    item.SetActive(true);
    //    item.transform.position = origin;

    //    BlockPhysics phys = item.GetComponent<BlockPhysics>();
    //    phys.AddVelocity(velocity);

    //    GroundItem gItem = item.GetComponent<GroundItem>();
    //    gItem.SetItem(itemObject, amount);
    //}
}
