using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DisplayInvDrag : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    Vector2 delta;

    public void OnBeginDrag(PointerEventData eventData)
    {
        Vector2 parentPos = transform.parent.transform.position;
        delta = eventData.position - parentPos;
    }

    public void OnDrag(PointerEventData eventData)
    {

        transform.parent.transform.position = eventData.position - delta;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    }
}
