using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScoreDisplay : MonoBehaviour
{

    private TMP_Text _text;
    private Animator _animator;
    
    // Start is called before the first frame update
    void Awake()
    {
        _text = GetComponent<TMP_Text>();
        _animator = GetComponent<Animator>();
    }

    public void UpdateScore(int score)
    {
        _text.text = score.ToString();
        if (_animator != null)
            _animator.SetTrigger("ScoreUpdated");
    }

    
}
