

using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{

    private Dictionary<string, InventoryObject> Inventories;
    private Dictionary<string, UserInterface> InventoriesUI;

    public Transform ScreenHolder;
    public GameObject ScreenPrefab;

    private void Awake()
    {
        Inventories = new();
        InventoriesUI = new();

        ServiceLocator.Register(this);
    }
    private void OnDestroy()
    {
        ServiceLocator.Unregister(this);
    }

    public InventoryObject Get(string id = "")
    {
        //if(id == "")
        //    id = System.Guid.NewGuid().ToString();

        //if(Inventories)
        //id

        return Inventories[id];
    }

    public void RegisterInventory(InventoryObject inventory)
    {
        Inventories[inventory.Id] = inventory;
        //if (InventoriesUI.TryGetValue(inventory.Id, out UserInterface userInterface))
        //{
        //    userInterface.Load(Inventories[inventory.Id]);
        //}
        //else
        //{
        //    GameObject obj = Instantiate(ScreenPrefab, ScreenHolder);
        //    UserInterface ui = obj.GetComponent<UserInterface>();
        //    ui.Load(inventory);
        //    InventoriesUI[inventory.Id] = ui;
        //    //obj.SetActive(false);
        //}
    }

    public void RegisterInventoryUI(string id, UserInterface userInterface)
    {
        InventoriesUI[id] = userInterface;
        //if (Inventories.TryGetValue(id, out InventoryObject inventory))
        //{
        //    InventoriesUI[id].Load(inventory);
        //}
    }

    public void Show(InventoryObject inventory)
    {
        //inventory.Id
        if (InventoriesUI.TryGetValue(inventory.Id, out UserInterface userInterface))
        {
            userInterface.Load(inventory);
            userInterface.gameObject.SetActive(true);
        }
        else
        {
            GameObject obj = Instantiate(ScreenPrefab, ScreenHolder);
            UserInterface ui = obj.GetComponent<UserInterface>();
            ui.Load(inventory);
            InventoriesUI[inventory.Id] = ui;
        }
    }

    public void Hide(InventoryObject inventory)
    {
        if (InventoriesUI.TryGetValue(inventory.Id, out UserInterface userInterface))
        {
            userInterface.gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Inventory UI doesn't exist");
        }
    }
}