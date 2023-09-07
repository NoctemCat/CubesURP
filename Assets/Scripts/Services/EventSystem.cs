using System;
using System.Collections.Generic;
using UnityEngine;

public enum EventType
{
    PauseGame,
    ResumeGame,
    DropItems,
    Structures_AddStructuresToSort,
    Chunk_AddSortedStructures,
    PlayerChunkChanged,
    AddChunkToSave,
    EnableCreative,
    DisableCreative,
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

public class PlayerChunkChangedArgs : EventArgs
{
    public Vector3Int newChunkPos;
    public PlayerChunkChangedArgs() => eventType = EventType.PlayerChunkChanged;
}


public delegate void EventHandler(in EventArgs args);

public class EventSystem
{
    private readonly Dictionary<EventType, EventHandler> _eventDictionary = new();

    public void StartListening(EventType eventType, EventHandler listener)
    {
        if (_eventDictionary.TryGetValue(eventType, out EventHandler thisEvent))
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

    public void StopListening(EventType eventType, EventHandler listener)
    {
        if (_eventDictionary.TryGetValue(eventType, out EventHandler thisEvent))
        {
            thisEvent -= listener;
            _eventDictionary[eventType] = thisEvent;
        }
    }

    public void TriggerEvent(EventType eventType, in EventArgs args = null)
    {
        if (_eventDictionary.TryGetValue(eventType, out EventHandler thisEvent))
        {
            thisEvent.Invoke(args);
        }
    }
}