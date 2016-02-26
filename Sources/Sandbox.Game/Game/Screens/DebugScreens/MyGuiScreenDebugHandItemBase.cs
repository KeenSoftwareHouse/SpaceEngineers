using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using VRageMath;
using Sandbox.Game;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Game.Definitions;

namespace Sandbox.Game.Gui
{
    abstract class MyGuiScreenDebugHandItemBase : MyGuiScreenDebugBase
    {
        private List<MyHandItemDefinition> m_handItemDefinitions = new List<MyHandItemDefinition>();
        private MyGuiControlCombobox m_handItemsCombo;
        protected MyHandItemDefinition CurrentSelectedItem;
        private MyCharacter m_playerCharacter;

        public void OnWeaponChanged(object sender, EventArgs e)
        {
            SelectFirstHandItem();
            
        }

        protected override void OnShow()
        {
            m_playerCharacter = MySession.Static.LocalCharacter;
            if (m_playerCharacter != null)
            {
                m_playerCharacter.OnWeaponChanged += OnWeaponChanged;
            }
            base.OnShow();
        }

        /// <summary>
        /// Called when [show].
        /// </summary>
        protected override void OnClosed()
        {
            if (m_playerCharacter != null)
            {
                m_playerCharacter.OnWeaponChanged -= OnWeaponChanged;
            }
            base.OnClosed();
        }

        protected void RecreateHandItemsCombo()
        {
            m_handItemsCombo = AddCombo();

            m_handItemDefinitions.Clear();
            foreach (var handItemDef in MyDefinitionManager.Static.GetHandItemDefinitions())
            {
                var def = MyDefinitionManager.Static.GetDefinition(handItemDef.PhysicalItemId);
                int handItemKey = m_handItemDefinitions.Count;
                m_handItemDefinitions.Add(handItemDef);
                m_handItemsCombo.AddItem(handItemKey, def.DisplayNameText);
            }

            m_handItemsCombo.SortItemsByValueText();
            m_handItemsCombo.ItemSelected += handItemsCombo_ItemSelected;

        }

        protected void RecreateSaveAndReloadButtons()
        {
            AddButton(new StringBuilder("Save"), OnSave);
            AddButton(new StringBuilder("Reload"), OnReload);
            AddButton(new StringBuilder("Transform"), OnTransform);
            AddButton(new StringBuilder("Transform All"), OnTransformAll);
        }

        protected void SelectFirstHandItem()
        {

            MyCharacter playerCharacter = MySession.Static.LocalCharacter;
            var weapon = playerCharacter.CurrentWeapon;

            if (weapon == null)
            {
                if (m_handItemsCombo.GetItemsCount() > 0)
                {
                    m_handItemsCombo.SelectItemByIndex(0);
                }
            }
            else
            {
                if (m_handItemsCombo.GetItemsCount() > 0)
                {
                    try
                    {
                       
                        if (weapon.DefinitionId.TypeId != typeof(MyObjectBuilder_PhysicalGunObject))
                        {
                            var physicalItemId = MyDefinitionManager.Static.GetPhysicalItemForHandItem(weapon.DefinitionId).Id;
                            //def = MyDefinitionManager.Static.GetDefinition(physicalItemId);
                            int index = m_handItemDefinitions.FindIndex(x => x.PhysicalItemId == physicalItemId);
                            m_handItemsCombo.SelectItemByKey(index);
                        }
                        else
                        {
                            MyDefinitionBase def;
                            def = MyDefinitionManager.Static.GetDefinition(weapon.DefinitionId);
                            int index = m_handItemDefinitions.FindIndex(x => x.DisplayNameText == def.DisplayNameText);
                            m_handItemsCombo.SelectItemByKey(index);
                        }


                       
                    }
                    catch (Exception e)
                    {
                        m_handItemsCombo.SelectItemByIndex(0);
                    }
                }
            }
        }

        protected virtual void handItemsCombo_ItemSelected()
        {
            CurrentSelectedItem = m_handItemDefinitions[(int)m_handItemsCombo.GetSelectedKey()]; ;
        }

        private void OnSave(MyGuiControlButton button)
        {
            MyDefinitionManager.Static.SaveHandItems();
        }

        private void OnReload(MyGuiControlButton button)
        {
            MyDefinitionManager.Static.ReloadHandItems();
        }


        // USE THIS TO TRANSFORM OLD RIG HAND POSITIONS TO NEW ONE
        private void OnTransformAll(MyGuiControlButton button)
        {
            var items = MyDefinitionManager.Static.GetHandItemDefinitions();

            foreach (var item in items)
            {
                TransformItem(item);
            }

        }

        // USE THIS TO TRANSFORM OLD RIG HAND POSITIONS TO NEW ONE
        private void OnTransform(MyGuiControlButton button)
        {
            TransformItem(CurrentSelectedItem);
        }


        private void TransformItem(MyHandItemDefinition item)
        {
            //SwapYZ(ref item.ItemLocation);
            //SwapYZ(ref item.ItemLocation3rd);
            //SwapYZ(ref item.ItemShootLocation);
            //SwapYZ(ref item.ItemShootLocation3rd);
            //SwapYZ(ref item.ItemWalkingLocation);
            //SwapYZ(ref item.ItemWalkingLocation3rd);
            ////SwapYZ(ref CurrentSelectedItem.LeftHand);
            ////SwapYZ(ref CurrentSelectedItem.RightHand);
            //SwapYZ(ref item.MuzzlePosition);


            Reorientate(ref item.LeftHand);
            Reorientate(ref item.RightHand);
        }

        private void Reorientate(ref Matrix m)
        {
            Matrix transform = new MatrixD(  -1, 0, 0, 0,
                                              0, -1, 0, 0,
                                              0, 0, 1, 0,
                                              0, 0, 0, 1
                                              );
            Vector3 translation = m.Translation;
            m = transform * m;
            m.Translation = translation;
        }

        private void Reorientate(ref Vector3 v)
        {
            v.X = -v.X;
            v.Y = -v.Y;
        }

        private void SwapYZ(ref Matrix m)
        {
            Vector3 translation = m.Translation;           
            float y = m.Translation.Y;            
            translation.Y = m.Translation.Z;
            translation.Z = y;
            m.Translation = translation;
        }

        private void SwapYZ(ref Vector3 v)
        {           
            float y = v.Y;
            v.Y = v.Z;
            v.Z = y;            
        }
    }
}
