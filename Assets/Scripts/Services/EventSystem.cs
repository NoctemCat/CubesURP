using System;
using System.Collections.Generic;
using UnityEngine;

public enum EventType
{
    PauseGame,
    ResumeGame,
    DropItems
}

public class EventArgs
{
    public EventType eventType;
}

public class DropItemsArgs : EventArgs
{
    public Vector3 origin;
    public Vector3 velocity;
    public ItemObject itemObject;
    public int amount;

    public DropItemsArgs() => eventType = EventType.DropItems;
}

public class EventSystem
{
    private readonly Dictionary<EventType, Action<EventArgs>> _eventDictionary = new();

    public void StartListening(EventType eventType, Action<EventArgs> listener)
    {
        if (_eventDictionary.TryGetValue(eventType, out Action<EventArgs> thisEvent))
        {
            thisEvent += listener;
            _eventDictionary[eventType] = thisEvent;
        }
        else
        {
            thisEvent += listener;
            _eventDictionary.Add(eventType, thisEvent);
        }
    }

    public void StopListening(EventType eventType, Action<EventArgs> listener)
    {
        if (_eventDictionary.TryGetValue(eventType, out Action<EventArgs> thisEvent))
        {
            thisEvent -= listener;
            _eventDictionary[eventType] = thisEvent;
        }
    }

    public void TriggerEvent(EventType eventType, EventArgs args = null)
    {
        if (_eventDictionary.TryGetValue(eventType, out Action<EventArgs> thisEvent))
        {
            args ??= new() { eventType = eventType };
            thisEvent.Invoke(args);
        }
    }
}