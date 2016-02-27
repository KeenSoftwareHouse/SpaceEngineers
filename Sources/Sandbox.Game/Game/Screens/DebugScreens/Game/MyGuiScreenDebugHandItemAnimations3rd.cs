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
    [MyDebugScreen("Game", "Hand item animations 3rd")]
    class MyGuiScreenDebugHandItemAnimations3rd : MyGuiScreenDebugHandItemBase
    {
        Matrix m_storedItem;
        Matrix m_storedWalkingItem;

        bool m_canUpdateValues = true;

        float m_itemRotationX;
        float m_itemRotationY;
        float m_itemRotationZ;
        float m_itemPositionX;
        float m_itemPositionY;
        float m_itemPositionZ;

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

        MyGuiControlSlider m_itemRotationXSlider;
        MyGuiControlSlider m_itemRotationYSlider;
        MyGuiControlSlider m_itemRotationZSlider;
        MyGuiControlSlider m_itemPositionXSlider;
        MyGuiControlSlider m_itemPositionYSlider;
        MyGuiControlSlider m_itemPositionZSlider;

        MyGuiControlSlider m_amplitudeMultiplierSlider;


        public MyGuiScreenDebugHandItemAnimations3rd()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Hand item animations 3rd", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            RecreateHandItemsCombo();

            m_sliderDebugScale = 0.6f;

            m_itemRotationXSlider = AddSlider("item rotation X", 0f, 0f, 360f, null);
            m_itemRotationXSlider.ValueChanged = ItemChanged;
            m_itemRotationYSlider = AddSlider("item rotation Y", 0f, 0f, 360f, null);
            m_itemRotationYSlider.ValueChanged = ItemChanged;
            m_itemRotationZSlider = AddSlider("item rotation Z", 0f, 0f, 360f, null);
            m_itemRotationZSlider.ValueChanged = ItemChanged;
            m_itemPositionXSlider = AddSlider("item position X", 0f, -1f, 1f, null);
            m_itemPositionXSlider.ValueChanged = ItemChanged;
            m_itemPositionYSlider = AddSlider("item position Y", 0f, -1f, 1f, null);
            m_itemPositionYSlider.ValueChanged = ItemChanged;
            m_itemPositionZSlider = AddSlider("item position Z", 0f, -1f, 1f, null);
            m_itemPositionZSlider.ValueChanged = ItemChanged;


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
            m_amplitudeMultiplierSlider = AddSlider("Amplitude multiplier", 0f, -1f, 3f, null);
            m_amplitudeMultiplierSlider.ValueChanged = WalkingItemChanged;


            AddButton(new StringBuilder("Walk!"), OnWalk);
            AddButton(new StringBuilder("Run!"), OnRun);
            RecreateSaveAndReloadButtons();

            SelectFirstHandItem();

            m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugHandItemsAnimations3rd";
        }

        protected override void handItemsCombo_ItemSelected()
        {
            base.handItemsCombo_ItemSelected();

            m_storedWalkingItem = CurrentSelectedItem.ItemWalkingLocation3rd;
            m_storedItem = CurrentSelectedItem.ItemLocation3rd;

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

            m_itemRotationX = 0;
            m_itemRotationY = 0;
            m_itemRotationZ = 0;
            m_itemPositionX = m_storedItem.Translation.X;
            m_itemPositionY = m_storedItem.Translation.Y;
            m_itemPositionZ = m_storedItem.Translation.Z;

            m_canUpdateValues = false;

            m_itemWalkingRotationXSlider.Value = m_itemWalkingRotationX;
            m_itemWalkingRotationYSlider.Value = m_itemWalkingRotationY;
            m_itemWalkingRotationZSlider.Value = m_itemWalkingRotationZ;
            m_itemWalkingPositionXSlider.Value = m_itemWalkingPositionX;
            m_itemWalkingPositionYSlider.Value = m_itemWalkingPositionY;
            m_itemWalkingPositionZSlider.Value = m_itemWalkingPositionZ;


            m_itemRotationXSlider.Value = m_itemRotationX;
            m_itemRotationYSlider.Value = m_itemRotationY;
            m_itemRotationZSlider.Value = m_itemRotationZ;
            m_itemPositionXSlider.Value = m_itemPositionX;
            m_itemPositionYSlider.Value = m_itemPositionY;
            m_itemPositionZSlider.Value = m_itemPositionZ;

            m_amplitudeMultiplierSlider.Value = CurrentSelectedItem.AmplitudeMultiplier3rd;
        
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

                CurrentSelectedItem.ItemWalkingLocation3rd = m_storedWalkingItem
                  * Matrix.CreateRotationX(MathHelper.ToRadians(m_itemWalkingRotationX))
                  * Matrix.CreateRotationY(MathHelper.ToRadians(m_itemWalkingRotationY))
                  * Matrix.CreateRotationZ(MathHelper.ToRadians(m_itemWalkingRotationZ));
                CurrentSelectedItem.ItemWalkingLocation3rd.Translation = new Vector3(m_itemWalkingPositionX, m_itemWalkingPositionY, m_itemWalkingPositionZ);

                CurrentSelectedItem.AmplitudeMultiplier3rd = m_amplitudeMultiplierSlider.Value;
            }
        }

        void ItemChanged(MyGuiControlSlider slider)
        {
            if (m_canUpdateValues)
            {
                m_itemRotationX = m_itemRotationXSlider.Value;
                m_itemRotationY = m_itemRotationYSlider.Value;
                m_itemRotationZ = m_itemRotationZSlider.Value;
                m_itemPositionX = m_itemPositionXSlider.Value;
                m_itemPositionY = m_itemPositionYSlider.Value;
                m_itemPositionZ = m_itemPositionZSlider.Value;

                CurrentSelectedItem.ItemLocation3rd = m_storedItem
                  * Matrix.CreateRotationX(MathHelper.ToRadians(m_itemRotationX))
                  * Matrix.CreateRotationY(MathHelper.ToRadians(m_itemRotationY))
                  * Matrix.CreateRotationZ(MathHelper.ToRadians(m_itemRotationZ));
                CurrentSelectedItem.ItemLocation3rd.Translation = new Vector3(m_itemPositionX, m_itemPositionY, m_itemPositionZ);
            }
        }
     
    }
}
