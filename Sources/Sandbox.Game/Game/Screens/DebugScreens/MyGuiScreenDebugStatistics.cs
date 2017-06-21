#if !XB1
using Sandbox.Common;
using Sandbox.Engine.Utils;
using Sandbox.Game.Audio;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Replication;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.Audio;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRage.Win32;
using VRageMath;
using VRageRender;
using MyGuiConstants = Sandbox.Graphics.GUI.MyGuiConstants;

namespace Sandbox.Game.Gui
{
    public class MyGuiScreenDebugStatistics : MyGuiScreenDebugBase
    {
        static StringBuilder m_frameDebugText = new StringBuilder(1024);
        static StringBuilder m_frameDebugTextRA = new StringBuilder(2048);
        static List<StringBuilder> m_texts = new List<StringBuilder>(32);
        static List<StringBuilder> m_rightAlignedtexts = new List<StringBuilder>(32);

        List<MyKeys> m_pressedKeys = new List<MyKeys>(10);

        public MyGuiScreenDebugStatistics()
            : base(new Vector2(0.5f, 0.5f), new Vector2(), null, true)
        {
            m_isTopMostScreen = true;
            m_drawEvenWithoutFocus = true;
            CanHaveFocus = false;
            m_canShareInput = false;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugStatistics";
        }

        //  Frame Debug Text - is cleared at the begining of Update and rendered at the end of Draw
        public void AddToFrameDebugText(string s)
        {
            m_frameDebugText.AppendLine(s);
        }

        //  Frame Debug Text - is cleared at the begining of Update and rendered at the end of Draw
        public void AddToFrameDebugText(StringBuilder s)
        {
            m_frameDebugText.AppendStringBuilder(s);
            m_frameDebugText.AppendLine();
        }

        //  Frame Debug Text Right Aligned - is cleared at the begining of Update and rendered at the end of Draw
        public void AddDebugTextRA(string s)
        {
            m_frameDebugTextRA.Append(s);
            m_frameDebugTextRA.AppendLine();
        }
        //  Frame Debug Text Right Aligned - is cleared at the begining of Update and rendered at the end of Draw
        public void AddDebugTextRA(StringBuilder s)
        {
            m_frameDebugTextRA.AppendStringBuilder(s);
            m_frameDebugTextRA.AppendLine();
        }

        //  Frame Debug Text - is cleared at the begining of Update and rendered at the end of Draw
        public void ClearFrameDebugText()
        {
            m_frameDebugText.Clear();
            m_frameDebugTextRA.Clear();
            //    MyAudio.Static.WriteDebugInfo(m_frameDebugTextRA);
        }

        public Vector2 GetScreenLeftTopPosition()
        {
            float deltaPixels = 25 * MyGuiManager.GetSafeScreenScale();
            Rectangle fullscreenRectangle = MyGuiManager.GetSafeFullscreenRectangle();
            return MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(new Vector2(deltaPixels, deltaPixels));
        }

        public Vector2 GetScreenRightTopPosition()
        {
            float deltaPixels = 25 * MyGuiManager.GetSafeScreenScale();
            Rectangle fullscreenRectangle = MyGuiManager.GetSafeFullscreenRectangle();
            return MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate_FULLSCREEN(new Vector2(fullscreenRectangle.Width - deltaPixels, deltaPixels));
        }

        static List<StringBuilder> m_statsStrings = new List<StringBuilder>();
        static int m_stringIndex = 0;

        public static StringBuilder StringBuilderCache
        {
            get
            {
                if (m_stringIndex >= m_statsStrings.Count)
                    m_statsStrings.Add(new StringBuilder(1024));

                StringBuilder sb = m_statsStrings[m_stringIndex++];
                return sb.Clear();
            }
        }

        public override bool Draw()
        {
            if (!base.Draw()) return false;

            float rowDistance = MyGuiConstants.DEBUG_STATISTICS_ROW_DISTANCE;
            float textScale = MyGuiConstants.DEBUG_STATISTICS_TEXT_SCALE;

            m_stringIndex = 0;
            m_texts.Clear();
            m_rightAlignedtexts.Clear();
           
            
            m_texts.Add(StringBuilderCache.GetFormatedFloat("FPS: ", MyFpsManager.GetFps()));
            m_texts.Add(new StringBuilder("Renderer: ").Append(MyRenderProxy.RendererInterfaceName()));

            if (MySector.MainCamera != null)
            {
                m_texts.Add(GetFormatedVector3(StringBuilderCache, "Camera pos: ", MySector.MainCamera.Position));
            }

            m_texts.Add(MyScreenManager.GetGuiScreensForDebug());
            m_texts.Add(StringBuilderCache.GetFormatedBool("Paused: ", MySandboxGame.IsPaused));
            m_texts.Add(StringBuilderCache.GetFormatedDateTimeOffset("System Time: ", TimeUtil.LocalTime));
            m_texts.Add(StringBuilderCache.GetFormatedTimeSpan("Total GAME-PLAY Time: ", TimeSpan.FromMilliseconds(MySandboxGame.TotalGamePlayTimeInMilliseconds)));
            m_texts.Add(StringBuilderCache.GetFormatedTimeSpan("Total Session Time: ", MySession.Static == null ? new TimeSpan(0) : MySession.Static.ElapsedPlayTime));
            m_texts.Add(StringBuilderCache.GetFormatedTimeSpan("Total Foot Time: ", MySession.Static == null ? new TimeSpan(0) : MySession.Static.TimeOnFoot));
            m_texts.Add(StringBuilderCache.GetFormatedTimeSpan("Total Jetpack Time: ", MySession.Static == null ? new TimeSpan(0) : MySession.Static.TimeOnJetpack));
            m_texts.Add(StringBuilderCache.GetFormatedTimeSpan("Total Small Ship Time: ", MySession.Static == null ? new TimeSpan(0) : MySession.Static.TimeOnSmallShip));
            m_texts.Add(StringBuilderCache.GetFormatedTimeSpan("Total Big Ship Time: ", MySession.Static == null ? new TimeSpan(0) : MySession.Static.TimeOnBigShip));
            m_texts.Add(StringBuilderCache.GetFormatedTimeSpan("Total Time: ", TimeSpan.FromMilliseconds(MySandboxGame.TotalTimeInMilliseconds)));

            /*if (MySession.Static != null && MySession.Static.LocalCharacter != null)
            {
                var physGroup = MyExternalReplicable.FindByObject(MySession.Static.LocalCharacter).FindStateGroup<MyCharacterPhysicsStateGroup>();
                m_texts.Add(StringBuilderCache.GetFormatedBool("Character has support: ", physGroup != null && physGroup.DebugSupport != null));
            }*/
            
            m_texts.Add(StringBuilderCache.GetFormatedLong("GC.GetTotalMemory: ", GC.GetTotalMemory(false), " bytes"));

            if (MyFakes.DETECT_LEAKS)
            {
                var o = SharpDX.Diagnostics.ObjectTracker.FindActiveObjects();
                m_texts.Add(StringBuilderCache.GetFormatedInt("SharpDX Active object count: ", o.Count));
            }

            //TODO: I am unable to show this without allocations
            m_texts.Add(StringBuilderCache.GetFormatedLong("Environment.WorkingSet: ", WinApi.WorkingSet, " bytes"));

            // TODO: OP! Get available texture memory
            //m_texts.Add(StringBuilderCache.GetFormatedFloat("Available videomemory: ", MySandboxGame.Static.GraphicsDevice.AvailableTextureMemory / (1024.0f * 1024.0f), " MB"));
            m_texts.Add(StringBuilderCache.GetFormatedFloat("Allocated videomemory: ", 0, " MB"));

#if PHYSICS_SENSORS_PROFILING
            if (MyPhysics.physicsSystem != null)
            {
                m_texts.Add(StringBuilderCache.GetFormatedInt("Physics sensor - new allocated interactions: ", MyPhysics.physicsSystem.GetSensorInteractionModule().GetNewAllocatedInteractionsCount()));
                m_texts.Add(StringBuilderCache.GetFormatedInt("Physics sensor - interactions in use: ", MyPhysics.physicsSystem.GetSensorInteractionModule().GetInteractionsInUseCount()));
                m_texts.Add(StringBuilderCache.GetFormatedInt("Physics sensor - interactions in use MAX: ", MyPhysics.physicsSystem.GetSensorInteractionModule().GetInteractionsInUseCountMax()));
                m_texts.Add(StringBuilderCache.GetFormatedInt("Physics sensor - all sensors: ", MyPhysics.physicsSystem.GetSensorModule().SensorsCount()));
                m_texts.Add(StringBuilderCache.GetFormatedInt("Physics sensor - active sensors: ", MyPhysics.physicsSystem.GetSensorModule().ActiveSensors.Count));
            }
#endif

            m_texts.Add(StringBuilderCache.GetFormatedInt("Sound Instances Total: ", MyAudio.Static.GetSoundInstancesTotal2D()).Append(" 2d / ").AppendInt32(MyAudio.Static.GetSoundInstancesTotal3D()).Append(" 3d"));
            if(MyMusicController.Static != null)
            {
                if (MyMusicController.Static.CategoryPlaying.Equals(MyStringId.NullOrEmpty))
                    m_texts.Add(StringBuilderCache.Append("No music playing, last category: " + MyMusicController.Static.CategoryLast.ToString() + ", next track in ")
                        .AppendDecimal(Math.Max(0f,MyMusicController.Static.NextMusicTrackIn), 1).Append("s"));
                else
                    m_texts.Add(StringBuilderCache.Append("Playing music category: " + MyMusicController.Static.CategoryPlaying.ToString()));
            }

            if (MyPerGameSettings.UseReverbEffect)
            {
                m_texts.Add(StringBuilderCache.Append("Current reverb effect: " + (MyAudio.Static.EnableReverb ? MyEntityReverbDetectorComponent.CurrentReverbPreset.ToLower() : "disabled")));
            }
            var tmp = StringBuilderCache;
            MyAudio.Static.WriteDebugInfo(tmp);
            m_texts.Add(tmp);
            for (int i = 0; i < 8; i++ )
                m_texts.Add(StringBuilderCache.Clear());

            MyInput.Static.GetPressedKeys(m_pressedKeys);
            AddPressedKeys("Current keys              : ", m_pressedKeys);

            m_texts.Add(StringBuilderCache.Clear());
            m_texts.Add(m_frameDebugText);

            m_rightAlignedtexts.Add(m_frameDebugTextRA);
              
            Vector2 origin = GetScreenLeftTopPosition();
            Vector2 rightAlignedOrigin = GetScreenRightTopPosition();

            for (int i = 0; i < m_texts.Count; i++)
            {
                MyGuiManager.DrawString(MyFontEnum.White, m_texts[i], origin + new Vector2(0, i * rowDistance), textScale,
                    Color.Yellow, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            }
            for (int i = 0; i < m_rightAlignedtexts.Count; i++)
            {
                MyGuiManager.DrawString(MyFontEnum.White, m_rightAlignedtexts[i], rightAlignedOrigin + new Vector2(-0.3f, i * rowDistance), textScale,
                    Color.Yellow, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            }

            ClearFrameDebugText();
            
#if DEBUG
            //list of last played sounds
            MyGuiManager.DrawString(MyFontEnum.White, new StringBuilder("Last played sounds:"), rightAlignedOrigin + new Vector2(-0.3f, 0.8f), textScale,
                    Color.Yellow, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            int colorIndex = (MyCueBank.lastSoundIndex > 0) ? (MyCueBank.lastSoundIndex - 1) : (MyCueBank.lastSounds.Count - 1);//index of last sound
            for (int i = 0; i < MyCueBank.lastSounds.Count; i++)
            {
                MyGuiManager.DrawString(MyFontEnum.White, MyCueBank.lastSounds[i], rightAlignedOrigin + new Vector2(-0.275f, 0.8f + (1 + i) * rowDistance), textScale,
                    (i == colorIndex ? Color.LightBlue : Color.Yellow), MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            }
#endif

            return true;
        }

        private static StringBuilder GetFormatedVector3(StringBuilder sb, string before, Vector3D value, string after = "")
        {
            sb.Clear();
            sb.Append(before);
            sb.Append("{");
            sb.ConcatFormat("{0: #,000} ", value.X);
            sb.ConcatFormat("{0: #,000} ", value.Y);
            sb.ConcatFormat("{0: #,000} ", value.Z);
            sb.Append("}");
            sb.Append(after);
            return sb;
        }

        private void AddPressedKeys(string groupName, List<MyKeys> keys)
        {
            var text = StringBuilderCache;
            text.Append(groupName);
            for (int i = 0; i < keys.Count; i++)
            {
                if (i > 0)
                {
                    text.Append(", ");
                }
                text.Append(MyInput.Static.GetKeyName((MyKeys)keys[i]));
            }
            m_texts.Add(text);
        }

        private StringBuilder GetShadowText(string text, int cascade, int value)
        {
            var sb = StringBuilderCache;
            sb.Clear();
            sb.ConcatFormat("{0} (c {1}): ", text, cascade);
            sb.Concat(value);
            return sb;
        }

        private StringBuilder GetLodText(string text, int lod, int value)
        {
            var sb = StringBuilderCache;
            sb.Clear();
            sb.ConcatFormat("{0}_LOD{1}: ", text, lod);
            sb.Concat(value);
            return sb;
        }
    }
}
#endif // !XB1
