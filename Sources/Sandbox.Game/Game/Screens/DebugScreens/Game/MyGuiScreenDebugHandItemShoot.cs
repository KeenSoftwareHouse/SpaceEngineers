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
    [MyDebugScreen("Game", "Hand item shoot")]
    class MyGuiScreenDebugHandItemShoot : MyGuiScreenDebugHandItemBase
    {
        Matrix m_storedShootLocation;
        Matrix m_storedShootLocation3rd;

        bool m_canUpdateValues = true;

        float m_itemRotationX;
        float m_itemRotationY;
        float m_itemRotationZ;
        float m_itemPositionX;
        float m_itemPositionY;
        float m_itemPositionZ;

        MyGuiControlSlider m_itemRotationXSlider;
        MyGuiControlSlider m_itemRotationYSlider;
        MyGuiControlSlider m_itemRotationZSlider;
        MyGuiControlSlider m_itemPositionXSlider;
        MyGuiControlSlider m_itemPositionYSlider;
        MyGuiControlSlider m_itemPositionZSlider;

        float m_itemRotationX3rd;
        float m_itemRotationY3rd;
        float m_itemRotationZ3rd;
        float m_itemPositionX3rd;
        float m_itemPositionY3rd;
        float m_itemPositionZ3rd;

        MyGuiControlSlider m_itemRotationX3rdSlider;
        MyGuiControlSlider m_itemRotationY3rdSlider;
        MyGuiControlSlider m_itemRotationZ3rdSlider;
        MyGuiControlSlider m_itemPositionX3rdSlider;
        MyGuiControlSlider m_itemPositionY3rdSlider;
        MyGuiControlSlider m_itemPositionZ3rdSlider;

        MyGuiControlSlider m_itemMuzzlePositionXSlider;
        MyGuiControlSlider m_itemMuzzlePositionYSlider;
        MyGuiControlSlider m_itemMuzzlePositionZSlider;

        MyGuiControlSlider m_blendSlider;

        MyGuiControlSlider m_shootScatterXSlider;
        MyGuiControlSlider m_shootScatterYSlider;
        MyGuiControlSlider m_shootScatterZSlider;
        MyGuiControlSlider m_scatterSpeedSlider;


        public MyGuiScreenDebugHandItemShoot()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Hand item shoot", Color.Yellow.ToVector4());
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

            m_itemRotationX3rdSlider = AddSlider("item rotation X 3rd", 0f, 0f, 360f, null);
            m_itemRotationX3rdSlider.ValueChanged = ItemChanged;
            m_itemRotationY3rdSlider = AddSlider("item rotation Y 3rd", 0f, 0f, 360f, null);
            m_itemRotationY3rdSlider.ValueChanged = ItemChanged;
            m_itemRotationZ3rdSlider = AddSlider("item rotation Z 3rd", 0f, 0f, 360f, null);
            m_itemRotationZ3rdSlider.ValueChanged = ItemChanged;
            m_itemPositionX3rdSlider = AddSlider("item position X 3rd", 0f, -1f, 1f, null);
            m_itemPositionX3rdSlider.ValueChanged = ItemChanged;
            m_itemPositionY3rdSlider = AddSlider("item position Y 3rd", 0f, -1f, 1f, null);
            m_itemPositionY3rdSlider.ValueChanged = ItemChanged;
            m_itemPositionZ3rdSlider = AddSlider("item position Z 3rd", 0f, -1f, 1f, null);
            m_itemPositionZ3rdSlider.ValueChanged = ItemChanged;


            m_itemMuzzlePositionXSlider = AddSlider("item muzzle X", 0f, -1f, 1f, null);
            m_itemMuzzlePositionXSlider.ValueChanged = ItemChanged;
            m_itemMuzzlePositionYSlider = AddSlider("item muzzle Y", 0f, -1f, 1f, null);
            m_itemMuzzlePositionYSlider.ValueChanged = ItemChanged;
            m_itemMuzzlePositionZSlider = AddSlider("item muzzle Z", 0f, -1f, 1f, null);
            m_itemMuzzlePositionZSlider.ValueChanged = ItemChanged;


            m_blendSlider = AddSlider("Shoot blend", 0f, 0f, 3f, null);
            m_blendSlider.ValueChanged = ItemChanged;

            m_shootScatterXSlider = AddSlider("Scatter X", 0f, 0, 1f, null);
            m_shootScatterXSlider.ValueChanged = ItemChanged;
            m_shootScatterYSlider = AddSlider("Scatter Y", 0f, 0, 1f, null);
            m_shootScatterYSlider.ValueChanged = ItemChanged;
            m_shootScatterZSlider = AddSlider("Scatter Z", 0f, 0, 1f, null);
            m_shootScatterZSlider.ValueChanged = ItemChanged;
            m_scatterSpeedSlider = AddSlider("Scatter speed", 0f, 0f, 1f, null);
            m_scatterSpeedSlider.ValueChanged = ItemChanged;


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
            
            m_storedShootLocation = CurrentSelectedItem.ItemShootLocation;
            m_storedShootLocation3rd = CurrentSelectedItem.ItemShootLocation3rd;

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

            m_itemRotationX = 0;
            m_itemRotationY = 0;
            m_itemRotationZ = 0;
            m_itemPositionX = m_storedShootLocation.Translation.X;
            m_itemPositionY = m_storedShootLocation.Translation.Y;
            m_itemPositionZ = m_storedShootLocation.Translation.Z;

            m_itemRotationX3rd = 0;
            m_itemRotationY3rd = 0;
            m_itemRotationZ3rd = 0;
            m_itemPositionX3rd = m_storedShootLocation3rd.Translation.X;
            m_itemPositionY3rd = m_storedShootLocation3rd.Translation.Y;
            m_itemPositionZ3rd = m_storedShootLocation3rd.Translation.Z;

            m_canUpdateValues = false;

            m_itemRotationXSlider.Value = m_itemRotationX;
            m_itemRotationYSlider.Value = m_itemRotationY;
            m_itemRotationZSlider.Value = m_itemRotationZ;
            m_itemPositionXSlider.Value = m_itemPositionX;
            m_itemPositionYSlider.Value = m_itemPositionY;
            m_itemPositionZSlider.Value = m_itemPositionZ;

            m_itemRotationX3rdSlider.Value = m_itemRotationX3rd;
            m_itemRotationY3rdSlider.Value = m_itemRotationY3rd;
            m_itemRotationZ3rdSlider.Value = m_itemRotationZ3rd;
            m_itemPositionX3rdSlider.Value = m_itemPositionX3rd;
            m_itemPositionY3rdSlider.Value = m_itemPositionY3rd;
            m_itemPositionZ3rdSlider.Value = m_itemPositionZ3rd;

            m_itemMuzzlePositionXSlider.Value = CurrentSelectedItem.MuzzlePosition.X;
            m_itemMuzzlePositionYSlider.Value = CurrentSelectedItem.MuzzlePosition.Y;
            m_itemMuzzlePositionZSlider.Value = CurrentSelectedItem.MuzzlePosition.Z;

            m_shootScatterXSlider.Value = CurrentSelectedItem.ShootScatter.X;
            m_shootScatterYSlider.Value = CurrentSelectedItem.ShootScatter.Y;
            m_shootScatterZSlider.Value = CurrentSelectedItem.ShootScatter.Z;
            m_scatterSpeedSlider.Value = CurrentSelectedItem.ScatterSpeed;

            
            m_blendSlider.Value = CurrentSelectedItem.ShootBlend;
        

            m_canUpdateValues = true;
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

                CurrentSelectedItem.ItemShootLocation = m_storedShootLocation
                  * Matrix.CreateRotationX(MathHelper.ToRadians(m_itemRotationX))
                  * Matrix.CreateRotationY(MathHelper.ToRadians(m_itemRotationY))
                  * Matrix.CreateRotationZ(MathHelper.ToRadians(m_itemRotationZ));
                CurrentSelectedItem.ItemShootLocation.Translation = new Vector3(m_itemPositionX, m_itemPositionY, m_itemPositionZ);


                m_itemRotationX3rd = m_itemRotationX3rdSlider.Value;
                m_itemRotationY3rd = m_itemRotationY3rdSlider.Value;
                m_itemRotationZ3rd = m_itemRotationZ3rdSlider.Value;
                m_itemPositionX3rd = m_itemPositionX3rdSlider.Value;
                m_itemPositionY3rd = m_itemPositionY3rdSlider.Value;
                m_itemPositionZ3rd = m_itemPositionZ3rdSlider.Value;

                CurrentSelectedItem.ItemShootLocation3rd = m_storedShootLocation3rd
                  * Matrix.CreateRotationX(MathHelper.ToRadians(m_itemRotationX3rd))
                  * Matrix.CreateRotationY(MathHelper.ToRadians(m_itemRotationY3rd))
                  * Matrix.CreateRotationZ(MathHelper.ToRadians(m_itemRotationZ3rd));
                CurrentSelectedItem.ItemShootLocation3rd.Translation = new Vector3(m_itemPositionX3rd, m_itemPositionY3rd, m_itemPositionZ3rd);

                CurrentSelectedItem.ShootBlend = m_blendSlider.Value;

                CurrentSelectedItem.MuzzlePosition.X = m_itemMuzzlePositionXSlider.Value;
                CurrentSelectedItem.MuzzlePosition.Y = m_itemMuzzlePositionYSlider.Value;
                CurrentSelectedItem.MuzzlePosition.Z = m_itemMuzzlePositionZSlider.Value;

                CurrentSelectedItem.ShootScatter.X = m_shootScatterXSlider.Value;
                CurrentSelectedItem.ShootScatter.Y = m_shootScatterYSlider.Value;
                CurrentSelectedItem.ShootScatter.Z = m_shootScatterZSlider.Value;
                CurrentSelectedItem.ScatterSpeed = m_scatterSpeedSlider.Value;

            }
        }
     
    }
}
