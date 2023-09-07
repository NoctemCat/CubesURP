using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugScreen : MonoBehaviour
{
    public InputActionReference toggleDebug;
    private World _world;
    private TMP_Text _debugText;
    private Transform _debugPanel;
    private float _frameRate;
    private float _timer;
    private readonly StringBuilder _debug = new();

    private void OnEnable()
    {
        // started, cancelled, performed 
        toggleDebug.action.performed += ToggleDebug;
    }

    private void OnDisable()
    {
        toggleDebug.action.performed -= ToggleDebug;
    }

    private void Start()
    {
        _world = ServiceLocator.Get<World>();
        _debugPanel = transform.GetChild(0);
        _debugText = _debugPanel.GetComponentInChildren<TMP_Text>();
    }

    private void ToggleDebug(InputAction.CallbackContext obj)
    {
        _debugPanel.gameObject.SetActive(!_debugPanel.gameObject.activeSelf);
    }

    private void Update()
    {
        if (_debugPanel.gameObject.activeSelf)
        {
            var pPos = _world.PlayerObj.transform.position;

            //StringBuilder debug = new(256);
            _debug.Clear();
            _debug.AppendLine($"Debug Screen");
            _debug.AppendLine($"World Name: {_world.WorldData.worldName}");
            _debug.AppendLine($"World Seed: {_world.WorldData.seed}");
            _debug.AppendLine($"FPS: {_frameRate}");
            _debug.AppendLine($"XYZ: {Mathf.FloorToInt(pPos.x)} / {Mathf.FloorToInt(pPos.y)} / {Mathf.FloorToInt(pPos.z)}");
            //debug.AppendLine($"Chunk: {_world.playerChunk.x} / {World.PlayerChunk.y} / {World.PlayerChunk.z}");

            _debugText.text = _debug.ToString();
        }

        if (_timer > 1f)
        {
            _frameRate = (int)(1f / Time.unscaledDeltaTime);
            _timer = 0f;
        }
        else
        {
            _timer += Time.deltaTime;
        }
    }
}
