using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private Settings Settings;
    [SerializeField] private TMP_Text ViewDistanceLabel;
    [SerializeField] private TMP_Text MouseSensitivityLabel;
    [SerializeField] private Slider ViewDistanceSlider;
    [SerializeField] private Slider MouseSensitivitySlider;
    [SerializeField] private TMP_InputField GenerateAtOnceField;
    [SerializeField] private TMP_InputField TimeBetweenGeneratingField;
    [SerializeField] private Button LeaveSettingsButton;

    private void Awake()
    {
        if (File.Exists($"{Application.dataPath}/settings.cfg"))
        {
            LoadSettings();
        }
        else
        {
            Settings = new();
            Settings.Init();
            SaveSettings();
        }

        ViewDistanceSlider.value = Settings.ViewDistance;
        ViewDistanceLabel.text = $"View Distance: {Settings.ViewDistance}";
        MouseSensitivitySlider.value = Settings.MouseSenstivity;
        MouseSensitivityLabel.text = $"Mouse Sensitivity: {Settings.MouseSenstivity:0.00}";
        GenerateAtOnceField.text = Settings.GenerateAtOnce.ToString();
        TimeBetweenGeneratingField.text = Settings.TimeBetweenGenerating.ToString();

        ViewDistanceSlider.onValueChanged.AddListener(OnViewDistanceChanged);
        MouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        GenerateAtOnceField.onEndEdit.AddListener(OnGenerateAtOnceEnded);
        TimeBetweenGeneratingField.onEndEdit.AddListener(OnTimeBetweenEnded);
        LeaveSettingsButton.onClick.AddListener(SaveSettings);
    }

    private void OnViewDistanceChanged(float slider)
    {
        ViewDistanceLabel.text = $"View Distance: {(int)slider}";
        Settings.ViewDistance = (int)slider;
    }

    private void OnMouseSensitivityChanged(float slider)
    {
        MouseSensitivityLabel.text = $"Mouse Sensitivity: {slider:0.00}";
        Settings.MouseSenstivity = slider;
    }

    private void OnGenerateAtOnceEnded(string strValue)
    {
        Settings.GenerateAtOnce = int.Parse(strValue);
    }

    private void OnTimeBetweenEnded(string strValue)
    {
        Settings.TimeBetweenGenerating = float.Parse(strValue);
    }

    public void SaveSettings()
    {
        string saveSettings = JsonUtility.ToJson(Settings, true);
        File.WriteAllText($"{Application.dataPath}/settings.cfg", saveSettings);
    }

    public void LoadSettings()
    {
        string loadSettings = File.ReadAllText($"{Application.dataPath}/settings.cfg");
        Settings = JsonUtility.FromJson<Settings>(loadSettings);
    }
}
