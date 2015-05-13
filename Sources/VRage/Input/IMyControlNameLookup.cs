
namespace VRage.Input
{
    public interface IMyControlNameLookup
    {
        string GetKeyName(MyKeys key);
        string GetName(MyMouseButtonsEnum button);
        string GetName(MyJoystickButtonsEnum joystickButton);
        string GetName(MyJoystickAxesEnum joystickAxis);
        string UnassignedText { get; }
    }
}
