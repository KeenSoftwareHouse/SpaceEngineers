using Sandbox.Common;
using Sandbox.Game;
using VRage.Library.Utils;
using VRageMath;

namespace VRage.Input
{
    public static class MyInputExtensions
    {
        // Rotation constants
        public const float MOUSE_ROTATION_INDICATOR_MULTIPLIER = 0.075f;
        public const float ROTATION_INDICATOR_MULTIPLIER = 0.15f;  // empirical value for nice keyboard rotation: mouse/joystick/gamepad sensitivity can be tweaked by the user

        public static float GetRoll(this IMyInput self)
        {
            var cx = MyControllerHelper.CX_CHARACTER;
            var roll = MyControllerHelper.IsControlAnalog(cx, MyControlsSpace.ROLL_RIGHT) - MyControllerHelper.IsControlAnalog(cx, MyControlsSpace.ROLL_LEFT);
            return roll;
        }

        public static float GetDeveloperRoll(this IMyInput self)
        {
            float roll = 0f;
            roll += self.IsGameControlPressed(MyControlsSpace.ROLL_LEFT) ? -1f : 0f;
            roll += self.IsGameControlPressed(MyControlsSpace.ROLL_RIGHT) ?  1f : 0f;
            return roll;
        }

        public static Vector3 GetPositionDelta(this IMyInput self)
        {
            Vector3 moveIndicator = Vector3.Zero;

            var cx = MyControllerHelper.CX_CHARACTER;

            // get rotation based on primary controller settings
            moveIndicator.X = MyControllerHelper.IsControlAnalog(cx, MyControlsSpace.STRAFE_RIGHT) - MyControllerHelper.IsControlAnalog(cx, MyControlsSpace.STRAFE_LEFT);
            moveIndicator.Y = MyControllerHelper.IsControlAnalog(cx, MyControlsSpace.JUMP) - MyControllerHelper.IsControlAnalog(cx, MyControlsSpace.CROUCH);
            moveIndicator.Z = MyControllerHelper.IsControlAnalog(cx, MyControlsSpace.BACKWARD) - MyControllerHelper.IsControlAnalog(cx, MyControlsSpace.FORWARD);

            return moveIndicator;
        }

        public static Vector2 GetRotation(this IMyInput self)
        {
            Vector2 rotationIndicator = Vector2.Zero;

            // get rotation from mouse
            rotationIndicator = new Vector2(self.GetMouseYForGamePlay(), self.GetMouseXForGamePlay()) * MOUSE_ROTATION_INDICATOR_MULTIPLIER;
            //rotationIndicator = new Vector2(GetMouseYForGamePlay() - MySandboxGame.ScreenSizeHalf.Y, 1) * MyGuiConstants.MOUSE_ROTATION_INDICATOR_MULTIPLIER;

            var cx = MyControllerHelper.CX_CHARACTER;

            rotationIndicator.X -= MyControllerHelper.IsControlAnalog(cx, MyControlsSpace.ROTATION_UP);
            rotationIndicator.X += MyControllerHelper.IsControlAnalog(cx, MyControlsSpace.ROTATION_DOWN);
            rotationIndicator.Y -= MyControllerHelper.IsControlAnalog(cx, MyControlsSpace.ROTATION_LEFT);
            rotationIndicator.Y += MyControllerHelper.IsControlAnalog(cx, MyControlsSpace.ROTATION_RIGHT);

            // Fix rotation to be independent from physics step.
            rotationIndicator *= VRage.Game.MyEngineConstants.UPDATE_STEPS_PER_SECOND * ROTATION_INDICATOR_MULTIPLIER;

            return rotationIndicator;
        }

        public static Vector2 GetCursorPositionDelta(this IMyInput self)
        {
            Vector2 renormalizationFactor = Vector2.One;

            return (new Vector2(self.GetMouseX(), self.GetMouseY())) * renormalizationFactor;
        }

        #region ModAPI Implementation
        public static float GetRoll(this ModAPI.IMyInput self) { return ((IMyInput)self).GetRoll(); }
        public static Vector3 GetPositionDelta(this ModAPI.IMyInput self) { return ((IMyInput)self).GetPositionDelta(); }
        public static Vector2 GetRotation(this ModAPI.IMyInput self) { return ((IMyInput)self).GetRotation(); }
        public static Vector2 GetCursorPositionDelta(this ModAPI.IMyInput self) { return ((IMyInput)self).GetCursorPositionDelta(); }
        #endregion
    }
}
