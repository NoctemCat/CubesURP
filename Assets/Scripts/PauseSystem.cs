using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseSystem : MonoBehaviour
{
    [SerializeField] private InputActionReference _pauseAction;
    [SerializeField] private GameObject _pauseScreen;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        IsPaused = false;
        _pauseAction.action.performed += TogglePause;
    }

    private void OnDestroy()
    {
        _pauseAction.action.performed -= TogglePause;
    }

    private void TogglePause(InputAction.CallbackContext context)
    {
        IsPaused = !IsPaused;

        _pauseScreen.SetActive(IsPaused);
        if (IsPaused)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
    }

}
