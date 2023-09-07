

using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{

    private Dictionary<string, InventoryObject> _inventories;
    private Dictionary<string, UserInterface> _inventoriesUI;

    [field: SerializeField] public Transform ScreenHolder { get; private set; }
    [field: SerializeField] public GameObject ScreenPrefab { get; private set; }

    private void Awake()
    {
        _inventories = new();
        _inventoriesUI = new();

        ServiceLocator.Register(this);
    }
    private void OnDestroy()
    {
        ServiceLocator.Unregister(this);
    }

    public InventoryObject Get(string id)
    {
        return _inventories[id];
    }

    public void RegisterInventory(InventoryObject inventory)
    {
        _inventories[inventory.Id] = inventory;
    }

    public void RegisterInventoryUI(string id, UserInterface userInterface)
    {
        _inventoriesUI[id] = userInterface;
    }

    public void Show(InventoryObject inventory)
    {
        if (_inventoriesUI.TryGetValue(inventory.Id, out UserInterface userInterface))
        {
            userInterface.Load(inventory);
            userInterface.gameObject.SetActive(true);
        }
        else
        {
            GameObject obj = Instantiate(ScreenPrefab, ScreenHolder);
            userInterface = obj.GetComponent<UserInterface>();
            userInterface.Load(inventory);
            _inventoriesUI[inventory.Id] = userInterface;
            userInterface.gameObject.SetActive(true);
        }
    }

    public void Hide(InventoryObject inventory)
    {
        if (_inventoriesUI.TryGetValue(inventory.Id, out UserInterface userInterface))
        {
            userInterface.gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Inventory UI doesn't exist");
        }
    }
}