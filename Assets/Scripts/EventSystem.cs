using System;
using UnityEngine;

public class EventSystem
{
    public event Action OnPauseGame;
    public event Action OnResumeGame;
    public event Action<Vector3, Vector3, ItemObject, int> OnDropItems;

    public void PauseGame()
    {
        OnPauseGame?.Invoke();
    }

    public void ResumeGame()
    {
        OnResumeGame?.Invoke();
    }

    public void DropItems(Vector3 origin, Vector3 velocity, ItemObject itemObject, int amount)
    {
        OnDropItems?.Invoke(origin, velocity, itemObject, amount);
    }
}