using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MoveCounter : MonoBehaviour
{
    private TMP_Text _text;
    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    public void UpdateCount(int moveCount)
    {
        bool shouldDisplayPlural = moveCount != 1;
        _text.text = $"{moveCount} {(shouldDisplayPlural ? "moves" : "move")}";
    }
}
