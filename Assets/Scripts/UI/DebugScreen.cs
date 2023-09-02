using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugScreen : MonoBehaviour
{
    public InputActionReference toggleDebug;
    private World World;
    private TMP_Text debugText;
    private Transform debugPanel;

    float frameRate;
    float timer;

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
        World = World.Instance;
        debugPanel = transform.GetChild(0);
        debugText = debugPanel.GetComponentInChildren<TMP_Text>();
    }

    private void ToggleDebug(InputAction.CallbackContext obj)
    {
        debugPanel.gameObject.SetActive(!debugPanel.gameObject.activeSelf);
    }

    private void Update()
    {
        if (debugPanel.gameObject.activeSelf)
        {
            var pPos = World.PlayerObj.transform.position;

            StringBuilder debug = new(256);
            debug.AppendLine($"Debug Screen");
            debug.AppendLine($"World Name: {World.WorldData.WorldName}");
            debug.AppendLine($"World Seed: {World.WorldData.Seed}");
            debug.AppendLine($"FPS: {frameRate}");
            debug.AppendLine($"XYZ: {Mathf.FloorToInt(pPos.x)} / {Mathf.FloorToInt(pPos.y)} / {Mathf.FloorToInt(pPos.z)}");
            debug.AppendLine($"Chunk: {World.PlayerChunk.x} / {World.PlayerChunk.y} / {World.PlayerChunk.z}");

            debugText.text = debug.ToString();
        }

        if (timer > 1f)
        {
            frameRate = (int)(1f / Time.unscaledDeltaTime);
            timer = 0f;
        }
        else
        {
            timer += Time.deltaTime;
        }
    }
}
