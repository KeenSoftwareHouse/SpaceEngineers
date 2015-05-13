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
using VRage;
using VRageMath;

#endregion

namespace Sandbox.Game.Gui
{
    [MyDebugScreen("Game", "Hand items")]
    class MyGuiScreenDebugHandItems : MyGuiScreenDebugHandItemBase
    {
        Matrix m_storedLeftHand;
        Matrix m_storedRightHand;
        Matrix m_storedItem;
        bool m_canUpdateValues = true;


        float m_leftHandRotationX;
        float m_leftHandRotationY;
        float m_leftHandRotationZ;
        float m_leftHandPositionX;
        float m_leftHandPositionY;
        float m_leftHandPositionZ;

        float m_rightHandRotationX;
        float m_rightHandRotationY;
        float m_rightHandRotationZ;
        float m_rightHandPositionX;
        float m_rightHandPositionY;
        float m_rightHandPositionZ;

        float m_itemRotationX;
        float m_itemRotationY;
        float m_itemRotationZ;
        float m_itemPositionX;
        float m_itemPositionY;
        float m_itemPositionZ;

        MyGuiControlSlider m_leftHandRotationXSlider;
        MyGuiControlSlider m_leftHandRotationYSlider;
        MyGuiControlSlider m_leftHandRotationZSlider;
        MyGuiControlSlider m_leftHandPositionXSlider;
        MyGuiControlSlider m_leftHandPositionYSlider;
        MyGuiControlSlider m_leftHandPositionZSlider;

        MyGuiControlSlider m_rightHandRotationXSlider;
        MyGuiControlSlider m_rightHandRotationYSlider;
        MyGuiControlSlider m_rightHandRotationZSlider;
        MyGuiControlSlider m_rightHandPositionXSlider;
        MyGuiControlSlider m_rightHandPositionYSlider;
        MyGuiControlSlider m_rightHandPositionZSlider;

        MyGuiControlSlider m_itemRotationXSlider;
        MyGuiControlSlider m_itemRotationYSlider;
        MyGuiControlSlider m_itemRotationZSlider;
        MyGuiControlSlider m_itemPositionXSlider;
        MyGuiControlSlider m_itemPositionYSlider;
        MyGuiControlSlider m_itemPositionZSlider;

        public MyGuiScreenDebugHandItems()
        {
            RecreateControls(true);
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);

            m_scale = 0.7f;

            AddCaption("Hand items properties", Color.Yellow.ToVector4());
            AddShareFocusHint();

            m_currentPosition = -m_size.Value / 2.0f + new Vector2(0.02f, 0.10f);

            RecreateHandItemsCombo();

            //float slidersOffset = 0.02f;
            m_sliderDebugScale = 0.6f;

            m_leftHandRotationXSlider = AddSlider("Left hand rotation X", 0f, 0f, 360f, null);
            m_leftHandRotationXSlider.ValueChanged = LeftHandChanged;
            m_leftHandRotationYSlider = AddSlider("Left hand rotation Y", 0f, 0f, 360f, null);
            m_leftHandRotationYSlider.ValueChanged = LeftHandChanged;
            m_leftHandRotationZSlider = AddSlider("Left hand rotation Z", 0f, 0f, 360f, null);
            m_leftHandRotationZSlider.ValueChanged = LeftHandChanged;
            m_leftHandPositionXSlider = AddSlider("Left hand position X", 0f, -1f, 1f, null);
            m_leftHandPositionXSlider.ValueChanged = LeftHandChanged;
            m_leftHandPositionYSlider = AddSlider("Left hand position Y", 0f, -1f, 1f, null);
            m_leftHandPositionYSlider.ValueChanged = LeftHandChanged;
            m_leftHandPositionZSlider = AddSlider("Left hand position Z", 0f, -1f, 1f, null);
            m_leftHandPositionZSlider.ValueChanged = LeftHandChanged;

            m_rightHandRotationXSlider = AddSlider("Right hand rotation X", 0f, 0f, 360f, null);
            m_rightHandRotationXSlider.ValueChanged = RightHandChanged;
            m_rightHandRotationYSlider = AddSlider("Right hand rotation Y", 0f, 0f, 360f, null);
            m_rightHandRotationYSlider.ValueChanged = RightHandChanged;
            m_rightHandRotationZSlider = AddSlider("Right hand rotation Z", 0f, 0f, 360f, null);
            m_rightHandRotationZSlider.ValueChanged = RightHandChanged;
            m_rightHandPositionXSlider = AddSlider("Right hand position X", 0f, -1f, 1f, null);
            m_rightHandPositionXSlider.ValueChanged = RightHandChanged;
            m_rightHandPositionYSlider = AddSlider("Right hand position Y", 0f, -1f, 1f, null);
            m_rightHandPositionYSlider.ValueChanged = RightHandChanged;
            m_rightHandPositionZSlider = AddSlider("Right hand position Z", 0f, -1f, 1f, null);
            m_rightHandPositionZSlider.ValueChanged = RightHandChanged;

            m_itemRotationXSlider = AddSlider("Item rotation X", 0f, 0f, 360f, null);
            m_itemRotationXSlider.ValueChanged = ItemChanged;
            m_itemRotationYSlider = AddSlider("Item rotation Y", 0f, 0f, 360f, null);
            m_itemRotationYSlider.ValueChanged = ItemChanged;
            m_itemRotationZSlider = AddSlider("Item rotation Z", 0f, 0f, 360f, null);
            m_itemRotationZSlider.ValueChanged = ItemChanged;
            m_itemPositionXSlider = AddSlider("Item position X", 0f, -1f, 1f, null);
            m_itemPositionXSlider.ValueChanged = ItemChanged;
            m_itemPositionYSlider = AddSlider("Item position Y", 0f, -1f, 1f, null);
            m_itemPositionYSlider.ValueChanged = ItemChanged;
            m_itemPositionZSlider = AddSlider("Item position Z", 0f, -1f, 1f, null);
            m_itemPositionZSlider.ValueChanged = ItemChanged;

            RecreateSaveAndReloadButtons();

            SelectFirstHandItem();

            m_currentPosition.Y += 0.01f;
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDebugHandItems";
        }

        protected override void handItemsCombo_ItemSelected()
        {
            base.handItemsCombo_ItemSelected();

            m_storedLeftHand = CurrentSelectedItem.LeftHand;
            m_storedRightHand = CurrentSelectedItem.RightHand;
            m_storedItem = CurrentSelectedItem.ItemLocation;

            UpdateValues();
        }

        void UpdateValues()
        {
            m_leftHandRotationX = 0;
            m_leftHandRotationY = 0;
            m_leftHandRotationZ = 0;
            m_leftHandPositionX = m_storedLeftHand.Translation.X;
            m_leftHandPositionY = m_storedLeftHand.Translation.Y;
            m_leftHandPositionZ = m_storedLeftHand.Translation.Z;

            m_rightHandRotationX = 0;
            m_rightHandRotationY = 0;
            m_rightHandRotationZ = 0;
            m_rightHandPositionX = m_storedRightHand.Translation.X;
            m_rightHandPositionY = m_storedRightHand.Translation.Y;
            m_rightHandPositionZ = m_storedRightHand.Translation.Z;

            m_itemRotationX = 0;
            m_itemRotationY = 0;
            m_itemRotationZ = 0;
            m_itemPositionX = m_storedItem.Translation.X;
            m_itemPositionY = m_storedItem.Translation.Y;
            m_itemPositionZ = m_storedItem.Translation.Z;

            m_canUpdateValues = false;

            m_leftHandRotationXSlider.Value = m_leftHandRotationX;
            m_leftHandRotationYSlider.Value = m_leftHandRotationY;
            m_leftHandRotationZSlider.Value = m_leftHandRotationZ;
            m_leftHandPositionXSlider.Value = m_leftHandPositionX;
            m_leftHandPositionYSlider.Value = m_leftHandPositionY;
            m_leftHandPositionZSlider.Value = m_leftHandPositionZ;

            m_rightHandRotationXSlider.Value = m_rightHandRotationX;
            m_rightHandRotationYSlider.Value = m_rightHandRotationY;
            m_rightHandRotationZSlider.Value = m_rightHandRotationZ;
            m_rightHandPositionXSlider.Value = m_rightHandPositionX;
            m_rightHandPositionYSlider.Value = m_rightHandPositionY;
            m_rightHandPositionZSlider.Value = m_rightHandPositionZ;

            m_itemRotationXSlider.Value = m_itemRotationX;
            m_itemRotationYSlider.Value = m_itemRotationY;
            m_itemRotationZSlider.Value = m_itemRotationZ;
            m_itemPositionXSlider.Value = m_itemPositionX;
            m_itemPositionYSlider.Value = m_itemPositionY;
            m_itemPositionZSlider.Value = m_itemPositionZ;

            m_canUpdateValues = true;

        }

        void LeftHandChanged(MyGuiControlSlider slider)
        {
            if (m_canUpdateValues)
            {
                m_leftHandRotationX = m_leftHandRotationXSlider.Value;
                m_leftHandRotationY = m_leftHandRotationYSlider.Value;
                m_leftHandRotationZ = m_leftHandRotationZSlider.Value;
                m_leftHandPositionX = m_leftHandPositionXSlider.Value;
                m_leftHandPositionY = m_leftHandPositionYSlider.Value;
                m_leftHandPositionZ = m_leftHandPositionZSlider.Value;

                CurrentSelectedItem.LeftHand = m_storedLeftHand
                    * Matrix.CreateRotationX(MathHelper.ToRadians(m_leftHandRotationX))
                    * Matrix.CreateRotationY(MathHelper.ToRadians(m_leftHandRotationY))
                    * Matrix.CreateRotationZ(MathHelper.ToRadians(m_leftHandRotationZ));
                CurrentSelectedItem.LeftHand.Translation = new Vector3(m_leftHandPositionX, m_leftHandPositionY, m_leftHandPositionZ);
            }
        }

        void RightHandChanged(MyGuiControlSlider slider)
        {           
            if (m_canUpdateValues)
            {
                m_rightHandRotationX = m_rightHandRotationXSlider.Value;
                m_rightHandRotationY = m_rightHandRotationYSlider.Value;
                m_rightHandRotationZ = m_rightHandRotationZSlider.Value;
                m_rightHandPositionX = m_rightHandPositionXSlider.Value;
                m_rightHandPositionY = m_rightHandPositionYSlider.Value;
                m_rightHandPositionZ = m_rightHandPositionZSlider.Value;

                CurrentSelectedItem.RightHand = m_storedRightHand
                    * Matrix.CreateRotationX(MathHelper.ToRadians(m_rightHandRotationX))
                    * Matrix.CreateRotationY(MathHelper.ToRadians(m_rightHandRotationY))
                    * Matrix.CreateRotationZ(MathHelper.ToRadians(m_rightHandRotationZ));
                CurrentSelectedItem.RightHand.Translation = new Vector3(m_rightHandPositionX, m_rightHandPositionY, m_rightHandPositionZ);
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

                CurrentSelectedItem.ItemLocation = m_storedItem
                  * Matrix.CreateRotationX(MathHelper.ToRadians(m_itemRotationX))
                  * Matrix.CreateRotationY(MathHelper.ToRadians(m_itemRotationY))
                  * Matrix.CreateRotationZ(MathHelper.ToRadians(m_itemRotationZ));
                CurrentSelectedItem.ItemLocation.Translation = new Vector3(m_itemPositionX, m_itemPositionY, m_itemPositionZ);
            }
        }
    }
}
