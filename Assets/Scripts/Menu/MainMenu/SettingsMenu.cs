using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour
{
    private Settings _settings;
    [SerializeField] private TMP_Text _viewDistanceLabel;
    [SerializeField] private TMP_Text _mouseSensitivityLabel;
    [SerializeField] private Slider _viewDistanceSlider;
    [SerializeField] private Slider _mouseSensitivitySlider;
    [SerializeField] private TMP_InputField _generateAtOnceField;
    [SerializeField] private TMP_InputField _timeBetweenGeneratingField;
    [SerializeField] private Button _leaveSettingsButton;

    [SerializeField] private GameObject _currentPanel;
    [SerializeField] private GameObject _leaveToPanel;

    private void Awake()
    {
        if (File.Exists($"{Application.dataPath}/settings.cfg"))
        {
            LoadSettings();
        }
        else
        {
            _settings = new();
            _settings.Init();
            SaveSettings();
        }

        _viewDistanceSlider.value = _settings.viewDistance;
        _viewDistanceLabel.text = $"View Distance: {_settings.viewDistance}";
        _mouseSensitivitySlider.value = _settings.mouseSenstivity;
        _mouseSensitivityLabel.text = $"Mouse Sensitivity: {_settings.mouseSenstivity:0.00}";
        _generateAtOnceField.text = _settings.generateAtOnce.ToString();
        _timeBetweenGeneratingField.text = _settings.timeBetweenGenerating.ToString();

        _viewDistanceSlider.onValueChanged.AddListener(OnViewDistanceChanged);
        _mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        _generateAtOnceField.onEndEdit.AddListener(OnGenerateAtOnceEnded);
        _timeBetweenGeneratingField.onEndEdit.AddListener(OnTimeBetweenEnded);
        _leaveSettingsButton.onClick.AddListener(SaveSettings);
    }

    private void OnViewDistanceChanged(float slider)
    {
        _viewDistanceLabel.text = $"View Distance: {(int)slider}";
        _settings.viewDistance = (int)slider;
    }

    private void OnMouseSensitivityChanged(float slider)
    {
        _mouseSensitivityLabel.text = $"Mouse Sensitivity: {slider:0.00}";
        _settings.mouseSenstivity = slider;
    }

    private void OnGenerateAtOnceEnded(string strValue)
    {
        _settings.generateAtOnce = int.Parse(strValue);
    }

    private void OnTimeBetweenEnded(string strValue)
    {
        _settings.timeBetweenGenerating = float.Parse(strValue);
    }

    public void SaveSettings()
    {
        string saveSettings = JsonUtility.ToJson(_settings, true);
        File.WriteAllText($"{Application.dataPath}/settings.cfg", saveSettings);

        _currentPanel.SetActive(false);
        _leaveToPanel.SetActive(true);
    }

    public void LoadSettings()
    {
        string loadSettings = File.ReadAllText($"{Application.dataPath}/settings.cfg");
        _settings = JsonUtility.FromJson<Settings>(loadSettings);
    }
}
