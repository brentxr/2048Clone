using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverScreen : MonoBehaviour
{

    private Animator _animator;
    
    
    // Start is called before the first frame update
    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    public void SetGameOver(bool isGameOver)
    {
        _animator.SetBool("IsGameOver", isGameOver);
    }

    
}
