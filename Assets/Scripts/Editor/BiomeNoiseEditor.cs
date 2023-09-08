using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System;
using Unity.Mathematics;
using System.Collections.Generic;

[CustomEditor(typeof(BiomeNoise))]
public class BiomeNoiseEditor : Editor
{
    BiomeNoise _biomeNoise;
    Texture2D _noiseTexture;

    public string test;

    private void OnEnable()
    {
        _biomeNoise = (BiomeNoise)target;
        _noiseTexture = new Texture2D(128, 128)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        UpdateTexture();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (_biomeNoise.needsAutoUpdate)
        {
            UpdateTexture();
            _biomeNoise.AutoUpdate();
        }

        GUILayout.Space(10);

        GUILayout.BeginHorizontal(GUILayout.ExpandWidth(true));
        GUILayout.FlexibleSpace();

        GUILayoutOption[] options = new GUILayoutOption[2];
        options[0] = GUILayout.Width(_biomeNoise.previewWidth);
        options[1] = GUILayout.Height(_biomeNoise.previewWidth);
        GUILayout.Label("", options);
        GUI.DrawTexture(GUILayoutUtility.GetLastRect(), _noiseTexture);

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    private void UpdateTexture()
    {
        BiomeHelper.FillTexture(
            ref _noiseTexture,
            128,
            128,
            _biomeNoise.seed,
            _biomeNoise.octaves,
            _biomeNoise.complexOctaves,
            _biomeNoise.noiseScale / _biomeNoise.chunksShownWidth,
            _biomeNoise.offset
        );
    }


}