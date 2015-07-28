﻿using System.Text;
using VRageMath;
using Sandbox.Engine.Utils;

using Sandbox.Game.World;
using Sandbox.Engine.Models;
using Sandbox.Graphics.GUI;
using Sandbox.Game.Entities.Character;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage;
using Sandbox.Game.Entities;

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Render", "Character")]
    class MyGuiScreenDebugCharacter : MyGuiScreenDebugBase
    {
        MyGuiControlCombobox m_animationComboA;
        MyGuiControlCombobox m_animationComboB;
        MyGuiControlSlider m_blendSlider;

        MyGuiControlCombobox m_animationCombo;
        MyGuiControlCheckbox m_loopCheckbox;

        public MyGuiScreenDebugCharacter()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Render Character", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);


            m_currentPosition.Y += 0.01f;


            if (MySession.ControlledEntity == null || !(MySession.ControlledEntity is MyCharacter))
            {
                AddLabel("None active character", Color.Yellow.ToVector4(), 1.2f);
                return;
            }

            MyCharacter playerCharacter = MySession.LocalCharacter;
            
            if (!constructor)
                playerCharacter.DebugMode = true;

            AddSlider("Max slope", playerCharacter.Definition.MaxSlope, 0f, 89f, (slider) => { playerCharacter.Definition.MaxSlope = slider.Value; });

            AddLabel(playerCharacter.Model.AssetName, Color.Yellow.ToVector4(), 1.2f);
              
            AddLabel("Animation A:", Color.Yellow.ToVector4(), 1.2f);

            m_animationComboA = AddCombo();
            int i = 0;
            foreach (var animation in playerCharacter.Definition.AnimationNameToSubtypeName)
            {
                m_animationComboA.AddItem(i++, new StringBuilder(animation.Key));
            }
            m_animationComboA.SelectItemByIndex(0);

            AddLabel("Animation B:", Color.Yellow.ToVector4(), 1.2f);

            m_animationComboB = AddCombo();
            i = 0;
            foreach (var animation in playerCharacter.Definition.AnimationNameToSubtypeName)
            {
                m_animationComboB.AddItem(i++, new StringBuilder(animation.Key));
            }
            m_animationComboB.SelectItemByIndex(0);

            m_blendSlider = AddSlider("Blend time", 0.5f, 0, 3, null);

            AddButton(new StringBuilder("Play A->B"), OnPlayBlendButtonClick);

            m_currentPosition.Y += 0.01f;


            m_animationCombo = AddCombo();
            i = 0;
            foreach (var animation in playerCharacter.Definition.AnimationNameToSubtypeName)
            {
                m_animationCombo.AddItem(i++, new StringBuilder(animation.Key));
            }
            m_animationCombo.SortItemsByValueText();
            m_animationCombo.SelectItemByIndex(0);

            m_loopCheckbox = AddCheckBox("Loop", false, null);

            m_currentPosition.Y += 0.02f;

            foreach (var name in playerCharacter.Definition.BoneSets.Keys)
            {
                var checkBox = AddCheckBox(name, false, null);
                checkBox.UserData = name;
                if (name == "Body")
                    checkBox.IsChecked = true;
            }

            AddButton(new StringBuilder("Play animation"), OnPlayButtonClick);

            AddCheckBox("Draw damage and hit hapsules", null, MemberHelper.GetMember(() => MyDebugDrawSettings.DEBUG_DRAW_SHOW_DAMAGE));

            m_currentPosition.Y += 0.01f;
        }

        void OnPlayBlendButtonClick(MyGuiControlButton sender)
        {
            MyCharacter playerCharacter = MySession.LocalCharacter;

            playerCharacter.PlayCharacterAnimation(
                m_animationComboA.GetSelectedKey().ToString(),
                MyBlendOption.Immediate,
                MyFrameOption.None,                
                m_blendSlider.Value);

            playerCharacter.PlayCharacterAnimation(
                m_animationComboB.GetSelectedKey().ToString(),
                MyBlendOption.WaitForPreviousEnd,
                MyFrameOption.Loop,                
                m_blendSlider.Value);
        }

        void OnPlayButtonClick(MyGuiControlButton sender)
        {
            string bonesArea = "";
            foreach (var control in Controls)
            {
                if (control is MyGuiControlCheckbox)
                {
                    MyGuiControlCheckbox chb = control as MyGuiControlCheckbox;
                    if (chb.IsChecked && chb.UserData != null)
                    {
                        bonesArea += " " + chb.UserData;
                    }
                }
            }

            MySession.LocalCharacter.PlayCharacterAnimation(
                m_animationCombo.GetSelectedValue().ToString(),
                MyBlendOption.Immediate,
                m_loopCheckbox.IsChecked ? MyFrameOption.Loop : MyFrameOption.None,                
                m_blendSlider.Value
            );
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugCharacter";
        }

        public override bool CloseScreen()
        {
            MyCharacter playerCharacter = MySession.LocalCharacter;

            if (playerCharacter != null)
            {
                playerCharacter.DebugMode = false;
            }

            return base.CloseScreen();
        }
    }
}
