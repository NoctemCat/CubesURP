using System.Collections;
using System.Collections.Generic;
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
            string debug = "Debug Screen\n";
            debug += frameRate + " fps\n";
            var pPos = World.PlayerObj.transform.position;
            debug += "XYZ: " + Mathf.FloorToInt(pPos.x) + " / " + Mathf.FloorToInt(pPos.y) + " / " + Mathf.FloorToInt(pPos.z) + "\n";
            debug += "Chunk: " + World.PlayerChunk.x + " / " + World.PlayerChunk.z + "\n";
            debugText.text = debug;
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
