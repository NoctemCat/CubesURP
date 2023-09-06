using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ResizeRect : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    [SerializeField] private RectTransform _rectTransform;
    [SerializeField] private float _cellWidth;
    [SerializeField] private float _scrollbarSize;
    private Vector2 _delta;
    private Vector2 _origSize;
    private Vector2 _origPos;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _origPos = _rectTransform.position;

        _origSize = _rectTransform.sizeDelta;
        _delta = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        var offset = eventData.position - _delta;
        var _size = _origSize;

        _size.x = Mathf.Floor((_size.x - _scrollbarSize) / _cellWidth) * _cellWidth + _scrollbarSize;
        int cellNumber = Mathf.FloorToInt(offset.x / _cellWidth);
        offset.x = cellNumber * _cellWidth;

        _rectTransform.position = _origPos + offset / 2f;
        offset.y = -offset.y;
        _rectTransform.sizeDelta = _size + offset;
    }
}
