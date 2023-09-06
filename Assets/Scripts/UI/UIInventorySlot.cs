using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIInventorySlot : MonoBehaviour
{
    private Image _image;
    private TMP_Text _amount;
    private string _name;
    private bool _initialized = false;

    void Start()
    {
        Init();
    }

    public void Init()
    {
        if (!_initialized)
        {
            _image = transform.GetChild(0).GetChild(0).GetComponent<Image>();
            _amount = transform.GetChild(1).GetComponent<TMP_Text>();
            _initialized = true;
        }
    }

    //public void Set(Item item)
    //{
    //    _image.sprite = item.;
    //    _image.enabled = true;
    //    _amount.text = amount;
    //}
    public void Set(Sprite icon, string amount, string name)
    {
        _image.sprite = icon;
        _image.enabled = true;
        _amount.text = amount;
        _name = name;
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
    public void Disable()
    {
        _image.enabled = false;
        _amount.text = "";
    }
}
