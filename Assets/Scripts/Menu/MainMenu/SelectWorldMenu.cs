using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectWorldMenu : MonoBehaviour
{
    [SerializeField] private ActiveWorldData _activeWorldData;
    [SerializeField] private GameObject _worldPanelPrefab;
    [SerializeField] private GameObject _worldPanelsHolder;
    [SerializeField] private Button _toCreateNewButton;
    [SerializeField] private Button _toReturnButton;

    [SerializeField] private GameObject _currentPanel;
    [SerializeField] private GameObject _createToPanel;
    [SerializeField] private GameObject _returnToPanel;

    private List<WorldData> _worldDatas;

    void Awake()
    {
        _worldDatas = new(4);

        _toCreateNewButton.onClick.AddListener(OnCreateButtonClicked);
        _toReturnButton.onClick.AddListener(OnReturnButtonClicked);

        ParseSavedDirectories();
    }

    private void OnCreateButtonClicked()
    {
        _currentPanel.SetActive(false);
        _returnToPanel.SetActive(false);
        _createToPanel.SetActive(true);
    }

    private void OnReturnButtonClicked()
    {
        _currentPanel.SetActive(false);
        _createToPanel.SetActive(false);
        _returnToPanel.SetActive(true);
    }

    public void ParseSavedDirectories()
    {
        _worldDatas.Clear();
        foreach (Transform panel in _worldPanelsHolder.transform)
        {
            Destroy(panel.gameObject);
        }

        string worldsPath = PathHelper.WorldsPath;

        Directory.CreateDirectory(worldsPath);

        DirectoryInfo di = new(worldsPath);
        var dirs = di.GetDirectories();

        foreach (var dir in dirs)
        {
            string worldDataPath = PathHelper.GetWorldDataPath(dir.FullName);
            if (File.Exists(worldDataPath))
            {
                _worldDatas.Add(LoadWorldData(worldDataPath));
            }
        }

        _worldDatas.Sort(delegate (WorldData data1, WorldData data2)
        {
            return DateTime.Compare(data1.accessDate, data2.accessDate);
        });
        _worldDatas.Reverse();

        for (int i = 0; i < _worldDatas.Count; i++)
        {
            WorldData data = _worldDatas[i];

            GameObject worldPanel = Instantiate(_worldPanelPrefab, _worldPanelsHolder.transform);

            Transform select = worldPanel.transform.GetChild(0);
            TMP_Text text = select.GetComponentInChildren<TMP_Text>();
            text.text = data.worldName;

            Button selectClick = select.GetComponent<Button>();
            selectClick.onClick.AddListener(() =>
            {
                data.accessDate = DateTime.Now;
                SaveWorldData(data, PathHelper.GetWorldDataPath(Path.Combine(worldsPath, data.worldName)));

                _activeWorldData.SetData(data);
                SceneManager.LoadScene("Main", LoadSceneMode.Single);
            });

            Transform delete = worldPanel.transform.GetChild(1);

            Button deleteClick = delete.GetComponent<Button>();
            deleteClick.onClick.AddListener(() =>
            {
                var dir = new DirectoryInfo(Path.Combine(worldsPath, data.worldName));
                dir.Delete(true);
                ParseSavedDirectories();
            });
        }
    }


    public WorldData LoadWorldData(string worldDataPath)
    {
        string loadWorldData = File.ReadAllText(worldDataPath);
        WorldData worldData = JsonUtility.FromJson<WorldData>(loadWorldData);
        return worldData;
    }

    public void SaveWorldData(WorldData data, string worldDataPath)
    {
        string saveSettings = JsonUtility.ToJson(data, true);
        File.WriteAllText(worldDataPath, saveSettings);
    }
}
