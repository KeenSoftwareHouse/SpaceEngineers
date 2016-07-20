using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Valve.VR;
using VRageMath;

namespace VRage.OpenVRWrapper
{
    public class ControllerState
    {
        VRControllerState_t m_controllerState;
        ulong m_buttonPressedAcc;
        ulong m_buttonReleasedAcc;
        ulong m_previousButtons;
        public void Update(CVRSystem system, uint id)
        {
            if (id == 999)
                return;
            //is pressed
            system.GetControllerState(id, ref m_controllerState);
            //was pressed
            ulong buttonsChanged = m_previousButtons ^ m_controllerState.ulButtonPressed;
            m_buttonPressedAcc |= buttonsChanged & m_controllerState.ulButtonPressed;
            //was released
            m_buttonReleasedAcc |= buttonsChanged & ~m_controllerState.ulButtonPressed;
            m_previousButtons = m_controllerState.ulButtonPressed;
        }
        public void Clear()
        {
            m_buttonPressedAcc = 0;
            m_buttonReleasedAcc = 0;
        }
        public bool IsButtonPressed(EVRButtonId button)
        {
            return 0 != (m_controllerState.ulButtonPressed & ((ulong)1 << (int)button));
        }
        public bool WasButtonPressed(EVRButtonId button)
        {
            return 0 != (m_buttonPressedAcc & ((ulong)1 << (int)button));
        }
        public bool WasButtonReleased(EVRButtonId button)
        {
            return 0 != (m_buttonReleasedAcc & ((ulong)1 << (int)button));
        }
        //touchpad:
        public bool IsButtonTouched(EVRButtonId button)
        {
            return 0 != (m_controllerState.ulButtonTouched & ((ulong)1 << (int)button));
        }
        public bool GetTouchpadXY(ref Vector2 pos)
        {
            if (m_controllerState.rAxis != null)
                if (0 != (m_controllerState.ulButtonTouched & ((ulong)1 << (int)EVRButtonId.k_EButton_SteamVR_Touchpad)))
                {
                    pos.X = m_controllerState.rAxis[0].x;
                    pos.Y = m_controllerState.rAxis[0].y;
                    return true;
                }
            return false;
        }
        public float GetAnalogTrigger()
        {
            if (m_controllerState.rAxis != null)
                return m_controllerState.rAxis[1].x;//0...1
            return 0;
        }
    }
}
