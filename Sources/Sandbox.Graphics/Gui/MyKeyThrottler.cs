using System.Collections.Generic;
using VRage.Input;

namespace Sandbox.Graphics.GUI
{
    public class MyKeyThrottler
    {
        private class MyKeyThrottleState
        {
            /// <summary>
            /// This is not for converting key to string, but for controling repeated key input with delay
            /// </summary>
            public int LastKeyPressTime = MyGuiManager.FAREST_TIME_IN_PAST;

            /// <summary>
            /// The required delay until the key is ready again.
            /// </summary>
            public int RequiredDelay;
        }

        private Dictionary<MyKeys, MyKeyThrottleState> m_keyTimeControllers = new Dictionary<MyKeys, MyKeyThrottleState>();

        private MyKeyThrottleState GetKeyController(MyKeys key)
        {
            MyKeyThrottleState controller;
            if (m_keyTimeControllers.TryGetValue(key, out controller))
                return controller;

            controller = new MyKeyThrottleState();
            m_keyTimeControllers[key] = controller;
            return controller;
        }

        /// <summary>
        /// Determines if the given key was pressed during this update cycle, but it also
        /// makes sure a minimum amount of time has passed before allowing a press.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsNewPressAndThrottled(MyKeys key)
        {
            if (!MyInput.Static.IsNewKeyPressed(key))
                return false;

            var controller = GetKeyController(key);

            // If we find no controller, we cannot time so we assume the key is ready.
            if (controller == null)
                return true;

            // Make sure we wait a minimum amount of time before allowing a repeat of the action this key enables. This
            // version of the check overrides the currently configured required delay for the key on purpose.
            if ((MyGuiManager.TotalTimeInMilliseconds - controller.LastKeyPressTime) > MyGuiConstants.TEXTBOX_MOVEMENT_DELAY)
            {
                // Reset the required delay to the default for the next repeat.
                controller.LastKeyPressTime = MyGuiManager.TotalTimeInMilliseconds;
                return true;
            }

            // The key was pressed but was choked by the minimum time requirement.
            return false;
        }

        public ThrottledKeyStatus GetKeyStatus(MyKeys key)
        {
            if (!MyInput.Static.IsKeyPress(key))
                return ThrottledKeyStatus.UNPRESSED;

            var controller = GetKeyController(key);

            // If we find no controller, we cannot time so we assume the key is ready.
            if (controller == null)
                return ThrottledKeyStatus.PRESSED_AND_READY;

            // If the key was pressed during this update cycle, the key is deemed as instantly
            // ready, but it will be a longer delay before the next repeat is allowed.
            var wasPressedNow = MyInput.Static.IsNewKeyPressed(key);
            if (wasPressedNow)
            {
                controller.RequiredDelay = MyGuiConstants.TEXTBOX_INITIAL_THROTTLE_DELAY;
                controller.LastKeyPressTime = MyGuiManager.TotalTimeInMilliseconds;
                return ThrottledKeyStatus.PRESSED_AND_READY;
            }

            // If this is a continuous key press, we want to make sure we wait a minimum amount of time before allowing a repeat
            // of the action this key enables.
            if ((MyGuiManager.TotalTimeInMilliseconds - controller.LastKeyPressTime) > controller.RequiredDelay)
            {
                // Reset the required delay to the default for the next repeat.
                controller.RequiredDelay = MyGuiConstants.TEXTBOX_REPEAT_THROTTLE_DELAY;
                controller.LastKeyPressTime = MyGuiManager.TotalTimeInMilliseconds;
                return ThrottledKeyStatus.PRESSED_AND_READY;
            }

            // The key was pressed, but we're still waiting for the required delay.
            return ThrottledKeyStatus.PRESSED_AND_WAITING;
        }
    }
}