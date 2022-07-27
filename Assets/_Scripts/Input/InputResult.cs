public class InputResult
{
    public int XInput = 0;
    public int YInput = 0;

    public bool HasValue => XInput != 0 || YInput != 0;
}