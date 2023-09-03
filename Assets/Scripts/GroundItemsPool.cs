using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GroundItemsPool : MonoBehaviour
{
    public static GroundItemsPool Instance { get; private set; }
    public List<GameObject> pooledObjects;
    public GameObject objectToPool;
    public int amountToPool;

    void Awake()
    {
        //SharedInstance = this;Instance
        if (Instance == null)
            Instance = this;
        else
            Destroy(this);
    }

    void Start()
    {
        pooledObjects = new List<GameObject>();

        AddToPool(amountToPool);
    }
    public void AddToPool(int num)
    {
        //amountToPool += num;
        GameObject tmp;
        for (int i = 0; i < num; i++)
        {
            tmp = Instantiate(objectToPool, transform);
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

    public static void DropItems(Vector3 origin, Vector3 velocity, ItemObject itemObject, int amount)
    {
        GameObject item = Instance.GetPooledObject();
        item.transform.position = origin;

        BlockPhysics phys = item.GetComponent<BlockPhysics>();
        phys.Reset();
        phys.AddVelocity(velocity);

        GroundItem gItem = item.GetComponent<GroundItem>();
        gItem.SetItem(itemObject, amount);

        item.SetActive(true);
    }
}
