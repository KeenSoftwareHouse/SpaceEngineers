#if !XB1 // XB1_NOOPENVRWRAPPER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Input;
using VRageMath;
using VRage.OpenVRWrapper;
using VRageRender;
using Valve.VR;

namespace Sandbox.Game.Gui
{
    public class MyVRDebugInputComponent : MyDebugComponent
    {
        public static MyVRDebugInputComponent Static { get; private set; }

        public MyVRDebugInputComponent()
        {
            Static = this;
        }

        public override string GetName()
        {
            return "VR";
        }

        public override bool HandleInput()
        {
            bool handled = false;
            if (MyInput.Static.IsKeyPress(MyKeys.Control))
            {
                /*if (MyInput.Static.IsNewKeyPressed(MyKeys.Add))
                    MyOpenVR.FloorOffset += 0.01f;
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Subtract))
                    MyOpenVR.FloorOffset -= 0.01f;*/
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Add))
                    m_logTiming = !m_logTiming;
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Subtract))
                    m_freezeTiming = !m_freezeTiming;
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Multiply))
                    MyOpenVR.LmuDebugOnOff = !MyOpenVR.LmuDebugOnOff;
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Divide))
                    MyOpenVR.Debug2DImage = !MyOpenVR.Debug2DImage;
                if (MyInput.Static.IsNewKeyPressed(MyKeys.Decimal))
                    MyOpenVR.SyncWait = !MyOpenVR.SyncWait;
            }
            return handled;
        }
        Vector2 m_c1touch = new Vector2(0, 0);
        Vector2 m_c2touch = new Vector2(0, 0);

        StringBuilder sb = new StringBuilder();
        Compositor_FrameTiming m_timing = new Compositor_FrameTiming();
        bool m_freezeTiming = false;
        bool m_logTiming = false;
        private void DrawFrameTiming()
        {
            int vertPos = 250;
            if (!m_freezeTiming)
            {
                var ret = MyOpenVR.GetFrameTiming(ref m_timing, 0);
            }
            foreach (var field in m_timing.GetType().GetFields())
            {
                MyRenderProxy.DebugDrawText2D(new Vector2(50, vertPos += 10), field.Name + ": " + field.GetValue(m_timing), Color.NavajoWhite, 0.5f);
                if (m_logTiming)
                    VRage.Utils.MyLog.Default.WriteLine(field.Name + ": " + field.GetValue(m_timing));
            }
            MyRenderProxy.DebugDrawText2D(new Vector2(50, vertPos += 10), "freeze (ctrl -): "+m_freezeTiming+"  to console (ctrl +): " + m_logTiming, Color.White, 0.5f);
        }

        public override void Draw()
        {
            base.Draw();
            MyRenderProxy.DebugDrawText2D(new Vector2(50, 50), "LMU (ctrl *):" + MyOpenVR.LmuDebugOnOff, Color.White, 1.0f);
            MyRenderProxy.DebugDrawText2D(new Vector2(250, 50), "2D (ctrl /):" + MyOpenVR.Debug2DImage, Color.White, 1.0f);
            MyRenderProxy.DebugDrawText2D(new Vector2(450, 50), "SYNC (ctrl ,):" + MyOpenVR.SyncWait, Color.White, 1.0f);

            MyRenderProxy.DebugDrawText2D(new Vector2(100, 80), "Missed frames: " + MyOpenVR.MissedFramesCount, Color.Purple, 1.0f);
            MyRenderProxy.DebugDrawText2D(new Vector2(400, 80), "Wait ms: " + MyOpenVR.WaitTimeMs.ToString("00.00"), Color.MediumPurple, 1.0f);
            MyRenderProxy.DebugDrawText2D(new Vector2(100, 100), "IPD (hmd wheel) =" + (MyOpenVR.Ipd_2 * 2).ToString(), Color.Green, 1.0f);
            //MyRenderProxy.DebugDrawText2D(new Vector2(100, 120), "height (ctrl +-) =" + MyOpenVR.FloorOffset.ToString(), Color.Green, 1.0f);

            Quaternion rot = Quaternion.CreateFromRotationMatrix(MyOpenVR.Controller1Matrix);
            Vector3 angles = MyMath.QuaternionToEuler(rot);
            var anglesDeg = Vector3.Multiply(angles, (float)(180f / Math.PI));
            MyRenderProxy.DebugDrawText2D(new Vector2(100, 150), "C1 angles:" + anglesDeg.ToString(), Color.Gray, 1.0f);

            sb.Clear();
            for (int i = 0; i < 64; i++)
            {
                if (MyOpenVR.Controller1State.WasButtonPressed((Valve.VR.EVRButtonId)i))
                    sb.Append('+'+i.ToString()+" ");
                if (MyOpenVR.Controller1State.WasButtonReleased((Valve.VR.EVRButtonId)i))
                    sb.Append('-' + i.ToString() + " ");
                if (MyOpenVR.Controller1State.IsButtonPressed((Valve.VR.EVRButtonId)i))
                {
                    sb.Append(MyOpenVR.GetButtonName(i));
                    sb.Append(" ");
                }
            }
            MyRenderProxy.DebugDrawText2D(new Vector2(100, 170), sb.ToString(), Color.Yellow, 1.0f);

            //touchpads:
            if (MyOpenVR.Controller1State.GetTouchpadXY(ref m_c1touch))
                MyRenderProxy.DebugDrawText2D(new Vector2(100, 190), "C1 touchpad:" + m_c1touch.X.ToString("0.00") + ", " + m_c1touch.Y.ToString("0.00"), Color.RosyBrown, 1.0f);
            if (MyOpenVR.Controller2State.GetTouchpadXY(ref m_c2touch))
                MyRenderProxy.DebugDrawText2D(new Vector2(100, 210), "C2 touchpad:" + m_c2touch.X.ToString("0.00") + ", " + m_c2touch.Y.ToString("0.00"), Color.RosyBrown, 1.0f);
            MyRenderProxy.DebugDrawText2D(new Vector2(100, 230), "C1 analog trigger: " + MyOpenVR.Controller1State.GetAnalogTrigger().ToString("0.00"), Color.Yellow, 1.0f);

            DrawFrameTiming();
        }
    }
}
#endif // !XB1
