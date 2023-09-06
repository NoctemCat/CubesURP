using System.Linq;
using UnityEngine;

public class PlayerInterface : DynamicInterface
{
    public override void Start()
    {
        base.Start();
        var inventorySystem = ServiceLocator.Get<InventorySystem>();
        inventorySystem.RegisterInventoryUI("Creative Inventory", this);

        gameObject.SetActive(false);
    }
}