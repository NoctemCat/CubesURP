

// https://discussions.unity.com/t/onmouseover-ui-button-c/166886/4
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private bool _mouseOver = false;

    public virtual void Update()
    {
        if (_mouseOver)
            OnPointerStay();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _mouseOver = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _mouseOver = false;
    }

    public virtual void OnPointerStay() { }
}