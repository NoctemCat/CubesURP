using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInventorySlot : MonoBehaviour
{
    private Image _image;
    private TMP_Text _amount;

    void Start()
    {
        _image = transform.GetChild(0).GetChild(0).GetComponent<Image>();
        _amount = transform.GetChild(1).GetComponent<TMP_Text>();
    }

    public void Set(Sprite icon, string amount)
    {
        _image.sprite = icon;
        _image.enabled = true;
        _amount.text = amount;
    }
    public void Set(Sprite icon)
    {
        _image.sprite = icon;
        _image.enabled = true;
    }
    public void Set(string amount)
    {
        _amount.text = amount;
    }
}
