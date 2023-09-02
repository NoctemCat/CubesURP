using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleMenu : MonoBehaviour
{
    [SerializeField] private GameObject _titlePanel;
    [SerializeField] private GameObject _selectWorldPanel;
    [SerializeField] private GameObject _settingsPanel;

    [SerializeField] private Button _enterSelectWorldPanel;
    [SerializeField] private Button _enterSettingsButton;
    [SerializeField] private Button _quitButton;

    private void Awake()
    {
        _enterSelectWorldPanel.onClick.AddListener(EnterSelectWorld);
        _enterSettingsButton.onClick.AddListener(EnterSettings);
        _quitButton.onClick.AddListener(QuitGame);
    }

    public void EnterSelectWorld()
    {
        _titlePanel.SetActive(false);
        _selectWorldPanel.SetActive(true);
    }
    public void EnterSettings()
    {
        _titlePanel.SetActive(false);
        _settingsPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
