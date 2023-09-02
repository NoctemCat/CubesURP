using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NewWorldMenu : MonoBehaviour
{
    [SerializeField] private ActiveWorldData _activeWorldData;
    [SerializeField] private TMP_InputField _worldNameInput;
    [SerializeField] private TMP_InputField _worldSeedInput;
    [SerializeField] private Button _createWorldButton;
    [SerializeField] private Button _toReturnButton;
    [SerializeField] private GameObject _currentPanel;
    [SerializeField] private GameObject _returnToPanel;

    private SelectWorldMenu _selectWorldMenu;

    private void Awake()
    {
        _selectWorldMenu = GetComponent<SelectWorldMenu>();
        _createWorldButton.onClick.AddListener(OnCreateWorld);
        _toReturnButton.onClick.AddListener(OnReturnButtonClicked);
    }

    private void OnReturnButtonClicked()
    {
        _selectWorldMenu.ParseSavedDirectories();

        _currentPanel.SetActive(false);
        _returnToPanel.SetActive(true);
    }

    private void OnCreateWorld()
    {
        string worldsPath = PathHelper.WorldsPath;
        string worldDir = Path.Combine(worldsPath, _worldNameInput.text);

        if (_worldNameInput.text.Trim() == string.Empty)
        {
            Debug.Log("World name can't be empty");
            return;
        }

        if (Directory.Exists(worldDir))
        {
            Debug.Log("Directory already exist");
            return;
        }

        Directory.CreateDirectory(worldDir);

        WorldData data = new()
        {
            Seed = _worldSeedInput.text,
            WorldName = _worldNameInput.text,
            AccessDate = DateTime.Now,
        };
        string worldDataPath = PathHelper.GetWorldDataPath(worldDir);

        SaveWorldData(data, worldDataPath);

        _activeWorldData.SetData(data);
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
    }

    public void SaveWorldData(WorldData data, string worldDataPath)
    {
        string saveSettings = JsonUtility.ToJson(data, true);
        File.WriteAllText(worldDataPath, saveSettings);
    }

}

