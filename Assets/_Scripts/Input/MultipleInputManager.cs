using System.Linq;
using UnityEngine;

public class MultipleInputManager : IInputManager
{

    private IInputManager[] _managers;

    public MultipleInputManager(params IInputManager[] managers)
    {
        _managers = managers;
    }
    
    public InputResult GetInput()
    {
        var inputResults = _managers.Select(manager => manager.GetInput());
        InputResult result = inputResults.FirstOrDefault(input => input.HasValue);
        return result ?? new InputResult();
    }
}
