using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TooltipScreenSpaceUI : MonoBehaviour
{
    public static TooltipScreenSpaceUI Instance { get; private set; }
    private RectTransform _background;
    private TextMeshProUGUI _textMeshPro;
    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Func<string> _getTooltipTextFunc;


    private void Awake()
    {
        Instance = this;

        _background = transform.Find("Background").GetComponent<RectTransform>();
        _textMeshPro = transform.Find("Text").GetComponent<TextMeshProUGUI>();
        _rectTransform = transform.GetComponent<RectTransform>();
        _canvas = transform.GetComponentInParent<Canvas>();

        HideTooltip();
    }

    private void Update()
    {
        SetText(_getTooltipTextFunc());
        Vector2 anchoredPosition = Mouse.current.position.ReadValue() / _canvas.scaleFactor;

        if (anchoredPosition.x + _background.rect.width > _canvas.pixelRect.width)
            anchoredPosition.x = _canvas.pixelRect.width - _background.rect.width;

        if (anchoredPosition.y + _background.rect.height > _canvas.pixelRect.height)
            anchoredPosition.y = _canvas.pixelRect.height - _background.rect.height;

        Rect screenRect = new Rect(0, 0, Screen.currentResolution.width, Screen.currentResolution.height);
        if (anchoredPosition.x < screenRect.x)
            anchoredPosition.x = screenRect.x;

        if (anchoredPosition.y < screenRect.y)
            anchoredPosition.y = screenRect.y;

        _rectTransform.anchoredPosition = anchoredPosition;
    }

    private void SetText(string tooltipText)
    {
        _textMeshPro.SetText(tooltipText);
        _textMeshPro.ForceMeshUpdate();

        Vector2 textSize = _textMeshPro.GetRenderedValues(false);
        textSize += new Vector2(_textMeshPro.margin.x, _textMeshPro.margin.y) * 2;
        _background.sizeDelta = textSize;
    }

    private void ShowTooltip(string tooltipText)
    {
        ShowTooltip(() => tooltipText);
    }

    private void ShowTooltip(Func<string> getTooltip)
    {
        _getTooltipTextFunc = getTooltip;
        gameObject.SetActive(true);
        SetText(_getTooltipTextFunc());
    }


    private void HideTooltip()
    {
        gameObject.SetActive(false);
    }

    public static void ShowTooltip_Static(string tooltipText)
    {
        Instance.ShowTooltip(tooltipText);
    }

    public static void ShowTooltip_Static(Func<string> getTooltip)
    {
        Instance.ShowTooltip(getTooltip);
    }

    public static void HideTooltip_Static()
    {
        Instance.HideTooltip();
    }
}
