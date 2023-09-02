using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseSystem : MonoBehaviour
{
    private World World;
    [SerializeField] private InputActionReference _pauseAction;
    [SerializeField] private GameObject _pauseScreen;
    [SerializeField] private Button _resumeButton;

    public bool IsPaused { get; private set; }

    private void Awake()
    {
        World = World.Instance;
        IsPaused = false;

        _resumeButton.onClick.AddListener(ResumeGame);
        _pauseAction.action.performed += TogglePause;
    }

    private void OnDestroy()
    {
        _pauseAction.action.performed -= TogglePause;
    }

    private void TogglePause(InputAction.CallbackContext context)
    {
        IsPaused = !IsPaused;

        if (IsPaused)
            StopGame();
        else
            ResumeGame();
    }

    private void StopGame()
    {
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        World.OnPause?.Invoke();
        _pauseScreen.SetActive(true);
    }

    private void ResumeGame()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        World.OnResume?.Invoke();
        _pauseScreen.SetActive(false);
    }
}
