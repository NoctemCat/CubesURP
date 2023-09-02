using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject _currentMenu;
    [SerializeField] private GameObject _settingsMenu;

    [SerializeField] private Button _toSettingButton;
    [SerializeField] private Button _toTitleScreenButton;
    [SerializeField] private Button _exitButton;

    private void Awake()
    {
        _toSettingButton.onClick.AddListener(ToSettings);
        _toTitleScreenButton.onClick.AddListener(ToTitleScreen);
        _exitButton.onClick.AddListener(OnExitClick);
    }

    private void ToSettings()
    {
        _currentMenu.SetActive(false);
        _settingsMenu.SetActive(true);
    }

    private void ToTitleScreen()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
    private void OnExitClick()
    {
        Application.Quit();
    }

}
