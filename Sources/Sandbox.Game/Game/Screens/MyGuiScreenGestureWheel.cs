using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Gui;
using Sandbox.Definitions;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.Localization;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;

using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.ObjectBuilders;
using VRage.FileSystem;
using VRageMath;

namespace Sandbox.Game.Screens
{
    public class MyGuiScreenGestureWheel : MyGuiScreenBase
    {
        public static MyGuiScreenGestureWheel Static;
        List<MyGuiControlImageButton> m_gestures = new List<MyGuiControlImageButton>();

        public MyGuiScreenGestureWheel()
            : base()
        {
            MySandboxGame.Log.WriteLine("MyGuiScreenGestureWheel.ctor START");

            Static = this;

            MyInput.Static.SetMousePosition((int)MySandboxGame.Config.ScreenWidth / 2, (int)MySandboxGame.Config.ScreenHeight / 2);

            EnabledBackgroundFade = false;
            m_isTopMostScreen = true;
            CanHideOthers = false;
            m_drawEvenWithoutFocus = true;
            CloseButtonEnabled = false;
            DrawMouseCursor = true;
            m_canShareInput = true;
            m_size = new Vector2(1, 1);
            m_position = new Vector2(0.5f, 0.5f);

            AddGestures();

            foreach (MyGuiControlImageButton button in m_gestures)
            {
                Controls.Add(button);
            }

            MySandboxGame.Log.WriteLine("MyGuiScreenGestureWheel.ctor END");
        }

        void AddGestures()
        {
            m_gestures = new List<MyGuiControlImageButton>();

            List<MyAnimationDefinition> definition = MyDefinitionManager.Static.GetAnimationDefinitions().Where(x => x.AllowInCockpit && x.Public).ToList();
            int count = definition.Count;
            float radius = MathHelper.Clamp(0.1f / 8 * count, 0.01f, 10f);

            for (int i = 0; i < count; i++)
            {
                if (definition[i].Public && definition[i].AllowInCockpit)
                {
                    MyObjectBuilder_ToolbarItemAnimation animationData = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ToolbarItemAnimation>();
                    animationData.DefinitionId = definition[i].Id;

                    MyGuiControlImageButton btn = new MyGuiControlImageButton();

                    btn.Size = new Vector2(64f / MyGuiConstants.GUI_OPTIMAL_SIZE.X, 64f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y);
                    btn.BackgroundTexture = new MyGuiCompositeTexture(definition[i].Icon);
                    btn.BorderEnabled = false;
                    btn.ColorMask = Color.White;
                    btn.Position = GetButtonPosition(count, radius, i);
                    btn.SetToolTip(definition[i].DisplayNameText);
                    btn.UserData = definition[i];

                    m_gestures.Add(btn);
                }
            }
        }

        Vector2 GetButtonPosition(int totalCount, float radius, int current)
        {
            var angle = 360 / totalCount * Math.PI / 180.0f;

            Vector2 pos = new Vector2();
            pos.X = this.GetPositionAbsoluteTopLeft().X + (float)Math.Cos(angle * current) * radius;
            pos.Y = this.GetPositionAbsoluteTopLeft().Y + (float)Math.Sin(angle * current) * radius;

            return pos * new Vector2(1024f / MyGuiConstants.GUI_OPTIMAL_SIZE.X, 1024f / MyGuiConstants.GUI_OPTIMAL_SIZE.Y);
        }

        protected override void OnClosed()
        {
            Static = null;
            base.OnClosed();
            MyGuiScreenGamePlay.ActiveGameplayScreen = null;
        }

        public override bool CloseScreen()
        {
            Static = null;
            return base.CloseScreen();
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenGestureWheel";
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            base.HandleInput(receivedFocusInThisUpdate);

            if (MyInput.Static.IsMousePressed(MyMouseButtonsEnum.Middle))
            {
                var control = GetMouseOverControl();
                var validControl = (control != null && control is MyGuiControlImageButton && control.UserData != null && control.UserData is MyAnimationDefinition);

                // tint selected Gesture
                if (validControl)
                {
                    control.ColorMask = Color.Yellow;
                }
                else
                {
                    foreach (var b in m_gestures) b.ColorMask = Color.White;
                }
            }

            // Get selected Gesture and Play the Animation when Middle Mouse Button is released
            if (MyInput.Static.IsMouseReleased(MyMouseButtonsEnum.Middle))
            {
                if (m_closingCueEnum.HasValue)
                    MyGuiSoundManager.PlaySound(m_closingCueEnum.Value);
                else
                    MyGuiSoundManager.PlaySound(GuiSounds.MouseClick);

                var control = GetMouseOverControl();
                var validControl = (control != null && control is MyGuiControlImageButton && control.UserData != null && control.UserData is MyAnimationDefinition);

                if (validControl)
                {
                    var def = control.UserData as MyAnimationDefinition;
                    MySession.LocalCharacter.PlayCharacterAnimation(def.Id.SubtypeName, def.Loop, MyPlayAnimationMode.Play, 0.2f, sync: true);
                }

                CloseScreen();
            }
        }

        public override void HandleUnhandledInput(bool receivedFocusInThisUpdate)
        {
            if (MyInput.Static.IsNewGameControlPressed(MyControlsSpace.PAUSE_GAME))
            {
                MySandboxGame.UserPauseToggle();
            }
        }
    }
}
