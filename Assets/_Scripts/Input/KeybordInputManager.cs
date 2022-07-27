using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeybordInputManager : IInputManager
{
    private int _lastXInput;
    private int _lastYInput;

    public InputResult GetInput()
    {
        InputResult result = new InputResult();
        
        var xInput = Mathf.RoundToInt(Input.GetAxisRaw("Horizontal"));
        var yInput = Mathf.RoundToInt(Input.GetAxisRaw("Vertical"));

        if (_lastXInput == 0 && _lastYInput == 0)
        {
            result.XInput = xInput;
            result.YInput = yInput;
        }

        _lastXInput = xInput;
        _lastYInput = yInput;

        return result;
    }
}