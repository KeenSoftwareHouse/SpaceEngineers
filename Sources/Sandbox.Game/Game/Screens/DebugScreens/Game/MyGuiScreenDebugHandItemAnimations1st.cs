#region Using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using VRage;
using VRageMath;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;

#endregion

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Game", "Hand item animations")]
    class MyGuiScreenDebugHandItemAnimations : MyGuiScreenDebugHandItemBase
    {
        Matrix m_storedWalkingItem;

        bool m_canUpdateValues = true;

        float m_itemWalkingRotationX;
        float m_itemWalkingRotationY;
        float m_itemWalkingRotationZ;
        float m_itemWalkingPositionX;
        float m_itemWalkingPositionY;
        float m_itemWalkingPositionZ;

        MyGuiControlSlider m_itemWalkingRotationXSlider;
        MyGuiControlSlider m_itemWalkingRotationYSlider;
        MyGuiControlSlider m_itemWalkingRotationZSlider;
        MyGuiControlSlider m_itemWalkingPositionXSlider;
        MyGuiControlSlider m_itemWalkingPositionYSlider;
        MyGuiControlSlider m_itemWalkingPositionZSlider;

        MyGuiControlSlider m_blendTimeSlider;
        MyGuiControlSlider m_xAmplitudeOffsetSlider;
        MyGuiControlSlider m_yAmplitudeOffsetSlider;
        MyGuiControlSlider m_zAmplitudeOffsetSlider;
        MyGuiControlSlider m_xAmplitudeScaleSlider;
        MyGuiControlSlider m_yAmplitudeScaleSlider;
        MyGuiControlSlider m_zAmplitudeScaleSlider;

        MyGuiControlSlider m_runMultiplierSlider;

        MyGuiControlCheckbox m_simulateLeftHandCheckbox;
        MyGuiControlCheckbox m_simulateRightHandCheckbox;


        public MyGuiScreenDebugHandItemAnimations()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Hand item animations", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            RecreateHandItemsCombo();

            m_sliderDebugScale = 0.6f;

            m_itemWalkingRotationXSlider = AddSlider("Walk item rotation X", 0f, 0f, 360f, null);
            m_itemWalkingRotationXSlider.ValueChanged = WalkingItemChanged;
            m_itemWalkingRotationYSlider = AddSlider("Walk item rotation Y", 0f, 0f, 360f, null);
            m_itemWalkingRotationYSlider.ValueChanged = WalkingItemChanged;
            m_itemWalkingRotationZSlider = AddSlider("Walk item rotation Z", 0f, 0f, 360f, null);
            m_itemWalkingRotationZSlider.ValueChanged = WalkingItemChanged;
            m_itemWalkingPositionXSlider = AddSlider("Walk item position X", 0f, -1f, 1f, null);
            m_itemWalkingPositionXSlider.ValueChanged = WalkingItemChanged;
            m_itemWalkingPositionYSlider = AddSlider("Walk item position Y", 0f, -1f, 1f, null);
            m_itemWalkingPositionYSlider.ValueChanged = WalkingItemChanged;
            m_itemWalkingPositionZSlider = AddSlider("Walk item position Z", 0f, -1f, 1f, null);
            m_itemWalkingPositionZSlider.ValueChanged = WalkingItemChanged;

            m_blendTimeSlider = AddSlider("Blend time", 0f, 0.001f, 1f, null); ;
            m_blendTimeSlider.ValueChanged = AmplitudeChanged;
            m_xAmplitudeOffsetSlider = AddSlider("X offset amplitude", 0f, -5.0f, 5f, null);
            m_xAmplitudeOffsetSlider.ValueChanged = AmplitudeChanged;
            m_yAmplitudeOffsetSlider = AddSlider("Y offset amplitude", 0f, -5.0f, 5f, null);
            m_yAmplitudeOffsetSlider.ValueChanged = AmplitudeChanged;
            m_zAmplitudeOffsetSlider = AddSlider("Z offset amplitude", 0f, -5.0f, 5f, null);
            m_zAmplitudeOffsetSlider.ValueChanged = AmplitudeChanged;
            m_xAmplitudeScaleSlider = AddSlider("X scale amplitude", 0f, -5f, 5f, null);
            m_xAmplitudeScaleSlider.ValueChanged = AmplitudeChanged;
            m_yAmplitudeScaleSlider = AddSlider("Y scale amplitude", 0f, -5f, 5f, null);
            m_yAmplitudeScaleSlider.ValueChanged = AmplitudeChanged;
            m_zAmplitudeScaleSlider = AddSlider("Z scale amplitude", 0f, -5f, 5f, null);
            m_zAmplitudeScaleSlider.ValueChanged = AmplitudeChanged;

            m_runMultiplierSlider = AddSlider("Run multiplier", 0f, -5f, 5f, null);
            m_runMultiplierSlider.ValueChanged = AmplitudeChanged;

            m_simulateLeftHandCheckbox = AddCheckBox("Simulate left hand", false, SimulateHandChanged);
            m_simulateRightHandCheckbox = AddCheckBox("Simulate right hand", false, SimulateHandChanged);


            AddButton(new StringBuilder("Walk!"), OnWalk);
            AddButton(new StringBuilder("Run!"), OnRun);
            RecreateSaveAndReloadButtons();

            SelectFirstHandItem();

            m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugHandItemsAnimations";
        }

        protected override void handItemsCombo_ItemSelected()
        {
            base.handItemsCombo_ItemSelected();
            
            m_storedWalkingItem = CurrentSelectedItem.ItemWalkingLocation;

            UpdateValues();
        }

        void OnWalk(MyGuiControlButton button)
        {
            MyCharacter character = MySession.Static.LocalCharacter;
            character.DebugMode = !character.DebugMode;

            character.SwitchAnimation(MyCharacterMovementEnum.Walking);
            character.SetCurrentMovementState(MyCharacterMovementEnum.Walking);
        }

        void OnRun(MyGuiControlButton button)
        {
            MyCharacter character = MySession.Static.LocalCharacter;
            character.DebugMode = !character.DebugMode;

            character.SwitchAnimation(MyCharacterMovementEnum.Sprinting);
            character.SetCurrentMovementState(MyCharacterMovementEnum.Sprinting);
        }

        void UpdateValues()
        {

            m_itemWalkingRotationX = 0;
            m_itemWalkingRotationY = 0;
            m_itemWalkingRotationZ = 0;
            m_itemWalkingPositionX = m_storedWalkingItem.Translation.X;
            m_itemWalkingPositionY = m_storedWalkingItem.Translation.Y;
            m_itemWalkingPositionZ = m_storedWalkingItem.Translation.Z;

            m_canUpdateValues = false;

            m_itemWalkingRotationXSlider.Value = m_itemWalkingRotationX;
            m_itemWalkingRotationYSlider.Value = m_itemWalkingRotationY;
            m_itemWalkingRotationZSlider.Value = m_itemWalkingRotationZ;
            m_itemWalkingPositionXSlider.Value = m_itemWalkingPositionX;
            m_itemWalkingPositionYSlider.Value = m_itemWalkingPositionY;
            m_itemWalkingPositionZSlider.Value = m_itemWalkingPositionZ;

            m_blendTimeSlider.Value = CurrentSelectedItem.BlendTime;
            m_xAmplitudeOffsetSlider.Value = CurrentSelectedItem.XAmplitudeOffset;
            m_yAmplitudeOffsetSlider.Value = CurrentSelectedItem.YAmplitudeOffset;
            m_zAmplitudeOffsetSlider.Value = CurrentSelectedItem.ZAmplitudeOffset;
            m_xAmplitudeScaleSlider.Value = CurrentSelectedItem.XAmplitudeScale;
            m_yAmplitudeScaleSlider.Value = CurrentSelectedItem.YAmplitudeScale;
            m_zAmplitudeScaleSlider.Value = CurrentSelectedItem.ZAmplitudeScale;

            m_runMultiplierSlider.Value = CurrentSelectedItem.RunMultiplier;

            m_simulateLeftHandCheckbox.IsChecked = CurrentSelectedItem.SimulateLeftHand;
            m_simulateRightHandCheckbox.IsChecked = CurrentSelectedItem.SimulateRightHand;

            m_canUpdateValues = true;
        }

        void WalkingItemChanged(MyGuiControlSlider slider)
        {
            if (m_canUpdateValues)
            {
                m_itemWalkingRotationX = m_itemWalkingRotationXSlider.Value;
                m_itemWalkingRotationY = m_itemWalkingRotationYSlider.Value;
                m_itemWalkingRotationZ = m_itemWalkingRotationZSlider.Value;
                m_itemWalkingPositionX = m_itemWalkingPositionXSlider.Value;
                m_itemWalkingPositionY = m_itemWalkingPositionYSlider.Value;
                m_itemWalkingPositionZ = m_itemWalkingPositionZSlider.Value;

                CurrentSelectedItem.ItemWalkingLocation = m_storedWalkingItem
                  * Matrix.CreateRotationX(MathHelper.ToRadians(m_itemWalkingRotationX))
                  * Matrix.CreateRotationY(MathHelper.ToRadians(m_itemWalkingRotationY))
                  * Matrix.CreateRotationZ(MathHelper.ToRadians(m_itemWalkingRotationZ));
                CurrentSelectedItem.ItemWalkingLocation.Translation = new Vector3(m_itemWalkingPositionX, m_itemWalkingPositionY, m_itemWalkingPositionZ);
            }
        }

        void AmplitudeChanged(MyGuiControlSlider slider)
        {
            if (m_canUpdateValues)
            {
                CurrentSelectedItem.BlendTime = m_blendTimeSlider.Value;
                CurrentSelectedItem.XAmplitudeOffset = m_xAmplitudeOffsetSlider.Value;
                CurrentSelectedItem.YAmplitudeOffset = m_yAmplitudeOffsetSlider.Value;
                CurrentSelectedItem.ZAmplitudeOffset = m_zAmplitudeOffsetSlider.Value;
                CurrentSelectedItem.XAmplitudeScale = m_xAmplitudeScaleSlider.Value;
                CurrentSelectedItem.YAmplitudeScale = m_yAmplitudeScaleSlider.Value;
                CurrentSelectedItem.ZAmplitudeScale = m_zAmplitudeScaleSlider.Value;

                CurrentSelectedItem.RunMultiplier = m_runMultiplierSlider.Value;
            }
        }

        void SimulateHandChanged(MyGuiControlCheckbox checkbox)
        {
            if (m_canUpdateValues)
            {
                CurrentSelectedItem.SimulateLeftHand = m_simulateLeftHandCheckbox.IsChecked;
                CurrentSelectedItem.SimulateRightHand = m_simulateRightHandCheckbox.IsChecked;
            }
        }
    }
}
