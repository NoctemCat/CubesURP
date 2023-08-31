using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleMenu : MonoBehaviour
{
    [SerializeField] private GameObject Title;
    [SerializeField] private GameObject Settings;

    [SerializeField] private Button StartButton;
    [SerializeField] private Button EnterSettingsButton;
    [SerializeField] private Button LeaveSettingsButton;
    [SerializeField] private Button QuitButton;

    [SerializeField] private WorldData WorldData;
    [SerializeField] private TMP_InputField SeedField;

    private void Awake()
    {
        StartButton.onClick.AddListener(StartGame);
        EnterSettingsButton.onClick.AddListener(EnterSettings);
        LeaveSettingsButton.onClick.AddListener(LeaveSettings);
        QuitButton.onClick.AddListener(QuitGame);
    }

    public void StartGame()
    {
        WorldData.Seed = SeedField.text;
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
    }
    public void EnterSettings()
    {
        Title.SetActive(false);
        Settings.SetActive(true);
    }
    public void LeaveSettings()
    {
        Title.SetActive(true);
        Settings.SetActive(false);
    }
    public void QuitGame()
    {
        Application.Quit();
    }
}
