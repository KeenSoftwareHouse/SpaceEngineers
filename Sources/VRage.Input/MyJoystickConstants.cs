
namespace VRage.Input
{
    static class MyJoystickConstants
    {
        public const int MAX_AXIS = 65535;
        public const int MIN_AXIS = 0;
        public const int CENTER_AXIS = (MAX_AXIS - MIN_AXIS) / 2;
        public const float ANALOG_PRESSED_THRESHOLD = 0.5f;  // 0 = neutral, 1 = fully to one side
        public const int MAXIMUM_BUTTON_COUNT = 16;
        public const bool BUTTON_JOYSTICK = true;
        public const float JOYSTICK_AS_MOUSE_MULTIPLIER = 4;
    }
}
