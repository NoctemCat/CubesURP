using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInterface : DynamicInterface
{
    [SerializeField] private Toggle _creativeToggle;
    private EventSystem _eventSystem;
    public override void Start()
    {
        base.Start();
        _eventSystem = ServiceLocator.Get<EventSystem>();
        var inventorySystem = ServiceLocator.Get<InventorySystem>();
        inventorySystem.RegisterInventoryUI("Player Inventory", this);

        gameObject.SetActive(false);

        _creativeToggle.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(bool value)
    {
        if (value)
            _eventSystem.TriggerEvent(EventType.EnableCreative);
        else
            _eventSystem.TriggerEvent(EventType.DisableCreative);
    }
}