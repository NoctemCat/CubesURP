using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GroundItemsPool : MonoBehaviour
{
    private EventSystem _eventSystem;
    public List<GameObject> pooledObjects;
    public GameObject objectToPool;
    public int amountToPool;

    private void Awake()
    {
        ServiceLocator.Register(this);
    }

    private void Start()
    {
        _eventSystem = ServiceLocator.Get<EventSystem>();
        _eventSystem.StartListening(EventType.DropItems, DropItems);

        pooledObjects = new List<GameObject>();
        AddToPool(amountToPool);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister(this);
        _eventSystem.StopListening(EventType.DropItems, DropItems);
    }

    public void AddToPool(int num)
    {
        //amountToPool += num;
        //GameObject tmp;
        for (int i = 0; i < num; i++)
        {
            GameObject tmp = Instantiate(objectToPool, transform);
            pooledObjects.Add(tmp);
            //BlockPhysics phys = tmp.GetComponent<BlockPhysics>();
            //phys.SetVelocity(Vector3.zero);
            tmp.SetActive(false);
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

    public void DropItems(in EventArgs eventArgs)
    {
        DropItemsArgs args = (DropItemsArgs)eventArgs;
        GameObject item = GetPooledObject();

        item.SetActive(true);
        item.transform.position = args.origin;

        GroundItem gItem = item.GetComponent<GroundItem>();
        gItem.SetItem(args.itemObject, args.amount);

        BlockPhysics phys = item.GetComponent<BlockPhysics>();
        phys.SetVelocity(args.velocity);
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
