#region Using

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System.Collections.Generic;
using System.Linq;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using MyGuiConstants = Sandbox.Graphics.GUI.MyGuiConstants;

#endregion

namespace Sandbox.Game.Gui
{
    class MyGuiScreenDialogInventoryCheat : MyGuiScreenBase
    {
        List<MyPhysicalItemDefinition> m_physicalItemDefinitions = new List<MyPhysicalItemDefinition>();

        MyGuiControlTextbox m_amountTextbox;
        MyGuiControlButton m_confirmButton;
        MyGuiControlButton m_cancelButton;
        MyGuiControlCombobox m_items;
        private static double m_lastAmount=0;
        private static int m_lastSelectedItem=0;

        private static int addedAsteroidsCount = 0;

        public MyGuiScreenDialogInventoryCheat() :
            base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, null)
        {
            CanHideOthers = false;
            EnabledBackgroundFade = true;
            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDialogInventoryCheat";
        }

        public override void RecreateControls(bool contructor)
        {
            base.RecreateControls(contructor);

            this.Controls.Add(new MyGuiControlLabel(new Vector2(0.0f, -0.10f), text: "Select the amount and type of items to spawn in your inventory", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER));
            m_amountTextbox = new MyGuiControlTextbox(new Vector2(-0.2f, 0.0f), null, 9, null, MyGuiConstants.DEFAULT_TEXT_SCALE, MyGuiControlTextboxType.DigitsOnly);
            m_items = new MyGuiControlCombobox(new Vector2(0.2f, 0.0f), new Vector2(0.3f, 0.05f), null, null, 10, null);
            m_confirmButton = new MyGuiControlButton(new Vector2(0.21f, 0.10f), MyGuiControlButtonStyleEnum.Default, new Vector2(0.2f, 0.05f), null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, new System.Text.StringBuilder("Confirm"));
            m_cancelButton = new MyGuiControlButton(new Vector2(-0.21f, 0.10f), MyGuiControlButtonStyleEnum.Default, new Vector2(0.2f, 0.05f), null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, new System.Text.StringBuilder("Cancel"));

            foreach (var definition in MyDefinitionManager.Static.GetAllDefinitions())
            {
                var physicalItemDef = definition as MyPhysicalItemDefinition;
                if (physicalItemDef == null || physicalItemDef.CanSpawnFromScreen == false)
                    continue;

                int key = m_physicalItemDefinitions.Count;
                m_physicalItemDefinitions.Add(physicalItemDef);
                m_items.AddItem(key, definition.DisplayNameText);
            }

            this.Controls.Add(m_amountTextbox);
            this.Controls.Add(m_items);
            this.Controls.Add(m_confirmButton);
            this.Controls.Add(m_cancelButton);

            m_amountTextbox.Text = string.Format("{0}", m_lastAmount);
            m_items.SelectItemByIndex(m_lastSelectedItem);

            m_confirmButton.ButtonClicked += confirmButton_OnButtonClick;
            m_cancelButton.ButtonClicked += cancelButton_OnButtonClick;
        }
        public override void HandleUnhandledInput(bool receivedFocusInThisUpdate)
        {
            base.HandleUnhandledInput(receivedFocusInThisUpdate);

            if (MyInput.Static.IsKeyPress(MyKeys.Enter))
            {
                confirmButton_OnButtonClick(m_confirmButton);
            }

            if (MyInput.Static.IsKeyPress(MyKeys.Escape))
            {
                cancelButton_OnButtonClick(m_cancelButton);
            }
        }

        void confirmButton_OnButtonClick(MyGuiControlButton sender)
        {
            MyEntity invObject = MySession.Static.ControlledEntity as MyEntity;
            if (invObject != null && invObject.HasInventory)
            {
                double amountDec = 0;
                double.TryParse(m_amountTextbox.Text, out amountDec);
                m_lastAmount = amountDec;

                MyFixedPoint amount = (MyFixedPoint)amountDec;

                if (m_items.GetSelectedKey() < 0 || (int)m_items.GetSelectedKey() >= m_physicalItemDefinitions.Count)
                    return;

                var itemId = m_physicalItemDefinitions[(int)m_items.GetSelectedKey()].Id;
                m_lastSelectedItem = (int)m_items.GetSelectedKey();
                MyInventory inventory = invObject.GetInventory(0) as MyInventory;
                System.Diagnostics.Debug.Assert(inventory != null, "Null or other inventory type!");

                if (inventory != null)
                {
                    if (!MySession.Static.CreativeMode)
                        amount = MyFixedPoint.Min(inventory.ComputeAmountThatFits(itemId), amount);

                    var builder = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(itemId);
                    inventory.DebugAddItems(amount, builder);
                }
            }

            CloseScreen();
        }

        void cancelButton_OnButtonClick(MyGuiControlButton sender)
        {
            CloseScreen();
        }
    }

    class MyGuiScreenDialogContainerType : MyGuiScreenBase
    {
        MyGuiControlTextbox m_containerTypeTextbox;
        MyGuiControlButton m_confirmButton;
        MyGuiControlButton m_cancelButton;

        MyCargoContainer m_container = null;

        public MyGuiScreenDialogContainerType(MyCargoContainer container) :
            base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, null)
        {
            CanHideOthers = false;
            EnabledBackgroundFade = true;
            m_container = container;
            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenDialogContainerType";
        }

        public override void RecreateControls(bool contructor)
        {
            base.RecreateControls(contructor);

            this.Controls.Add(new MyGuiControlLabel(new Vector2(0.0f, -0.10f), text: "Type the container type name for this cargo container:", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER));
            m_containerTypeTextbox = new MyGuiControlTextbox(new Vector2(0.0f, 0.0f), m_container.ContainerType, 100, null, MyGuiConstants.DEFAULT_TEXT_SCALE, MyGuiControlTextboxType.Normal);
            m_confirmButton = new MyGuiControlButton(new Vector2(0.21f, 0.10f), MyGuiControlButtonStyleEnum.Default, new Vector2(0.2f, 0.05f), null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, new System.Text.StringBuilder("Confirm"));
            m_cancelButton = new MyGuiControlButton(new Vector2(-0.21f, 0.10f), MyGuiControlButtonStyleEnum.Default, new Vector2(0.2f, 0.05f), null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, new System.Text.StringBuilder("Cancel"));

            this.Controls.Add(m_containerTypeTextbox);
            this.Controls.Add(m_confirmButton);
            this.Controls.Add(m_cancelButton);

            m_confirmButton.ButtonClicked += confirmButton_OnButtonClick;
            m_cancelButton.ButtonClicked += cancelButton_OnButtonClick;
        }

        public override void HandleUnhandledInput(bool receivedFocusInThisUpdate)
        {
            base.HandleUnhandledInput(receivedFocusInThisUpdate);
        }

        void confirmButton_OnButtonClick(MyGuiControlButton sender)
        {
            if (m_containerTypeTextbox.Text != null && m_containerTypeTextbox.Text != "")
            {
                m_container.ContainerType = m_containerTypeTextbox.Text;
            }
            else
            {
                m_container.ContainerType = null;
            }

            CloseScreen();
        }

        void cancelButton_OnButtonClick(MyGuiControlButton sender)
        {
            CloseScreen();
        }
    }

    class MyTestersInputComponent : MyDebugComponent
    {
        public MyTestersInputComponent()
        {
            AddShortcut(MyKeys.Back, true, true, false, false, () => "Freeze cube builder gizmo", delegate { MyCubeBuilder.Static.FreezeGizmo = !MyCubeBuilder.Static.FreezeGizmo; return true; });
            AddShortcut(MyKeys.NumPad0, false, false, false, false, () => "Add items to inventory (continuous)", delegate { AddItemsToInventory(0); return true; });
            AddShortcut(MyKeys.NumPad1, true, false, false, false, () => "Add items to inventory", delegate { AddItemsToInventory(1); return true; });
            AddShortcut(MyKeys.NumPad2, true, false, false, false, () => "Add components to inventory", delegate { AddItemsToInventory(2); return true; });
            AddShortcut(MyKeys.NumPad3, true, false, false, false, () => "Fill inventory with iron", FillInventoryWithIron);
            AddShortcut(MyKeys.NumPad4, true, false, false, false, () => "Add to inventory dialog...", delegate { var dialog = new MyGuiScreenDialogInventoryCheat(); MyGuiSandbox.AddScreen(dialog); return true; });
            AddShortcut(MyKeys.NumPad5, true, false, false, false, () => "Set container type", SetContainerType);
            AddShortcut(MyKeys.NumPad6, true, false, false, false, () => "Toggle debug draw", ToggleDebugDraw);
            AddShortcut(MyKeys.NumPad8, true, false, false, false, () => "Save the game", delegate { MyAsyncSaving.Start(); return true; });
        }

        public override string GetName()
        {
            return "Testers";
        }

        public bool AddItems(MyInventory inventory, MyObjectBuilder_PhysicalObject obj, bool overrideCheck)
        {
            return AddItems(inventory, obj, overrideCheck, 1);
        }

        public bool AddItems(MyInventory inventory, MyObjectBuilder_PhysicalObject obj, bool overrideCheck, MyFixedPoint amount)
        {
            if (overrideCheck || !inventory.ContainItems(amount, obj))
            {
                if (inventory.CanItemsBeAdded(amount, obj.GetId()))
                {
                    inventory.AddItems(amount, obj);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public override bool HandleInput()
        {
            if (MySession.Static == null) return false;
            if (MyScreenManager.GetScreenWithFocus() is MyGuiScreenDialogInventoryCheat) return false;

            return base.HandleInput();
        }

        private static bool ToggleDebugDraw()
        {
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
            {
                MyDebugDrawSettings.ENABLE_DEBUG_DRAW = false;
                MyDebugDrawSettings.DEBUG_DRAW_EVENTS = false;
            }
            else
            {
                MyDebugDrawSettings.ENABLE_DEBUG_DRAW = true;
                MyDebugDrawSettings.DEBUG_DRAW_EVENTS = true;
            }

            return true;
        }

        private static bool SetContainerType()
        {
            MyCharacter character = MySession.Static.LocalCharacter;
            if (character == null) return false;

            Matrix headMatrix = character.GetHeadMatrix(true);
            List<MyPhysics.HitInfo> hits = new List<MyPhysics.HitInfo>();
            Sandbox.Engine.Physics.MyPhysics.CastRay(headMatrix.Translation, headMatrix.Translation + headMatrix.Forward * 100.0f, hits);

            if (hits.Count == 0) return false;

            var hit = hits.FirstOrDefault();
            if (hit.HkHitInfo.Body == null) return false;
            IMyEntity entity = hit.HkHitInfo.GetHitEntity();

            if (!(entity is MyCargoContainer)) return false;

            var dialog = new MyGuiScreenDialogContainerType(entity as MyCargoContainer);

            MyGuiSandbox.AddScreen(dialog);
            return true;
        }

        private static bool FillInventoryWithIron()
        {
            var invObject = MySession.Static.ControlledEntity as MyEntity;
            if (invObject != null && invObject.HasInventory)
            {
                MyFixedPoint amount = 20000;

                var oreBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>("Iron");
                System.Diagnostics.Debug.Assert(invObject.GetInventory(0) as MyInventory != null,"Null or unexpected inventory type returned!");
                MyInventory inventory = invObject.GetInventory(0) as MyInventory;
                amount = inventory.ComputeAmountThatFits(oreBuilder.GetId());

                inventory.AddItems(amount, oreBuilder);
            }

            return true;
        }

        private void AddItemsToInventory(int variant)
        {
            bool overrideCheck = variant != 0;
            bool spawnNonfitting = variant != 0;
            bool componentsOnly = variant == 2;

            var invObject = MySession.Static.ControlledEntity as MyEntity;
            if (invObject != null && invObject.HasInventory)
            {
                System.Diagnostics.Debug.Assert(invObject.GetInventory(0) as MyInventory != null, "Null or unexpected inventory type returned!");
                MyInventory inventory = invObject.GetInventory(0) as MyInventory;
                //inventory.Clear();

                if (!componentsOnly)
                {
                    MyObjectBuilder_AmmoMagazine ammoMag = new MyObjectBuilder_AmmoMagazine();
                    ammoMag.SubtypeName = "NATO_5p56x45mm";
                    ammoMag.ProjectilesCount = 50;
                    AddItems(inventory, ammoMag, false, 5);

                    MyObjectBuilder_AmmoMagazine ammoMag2 = new MyObjectBuilder_AmmoMagazine();
                    ammoMag2.SubtypeName = "NATO_25x184mm";
                    ammoMag2.ProjectilesCount = 50;
                    AddItems(inventory, ammoMag2, false);

                    MyObjectBuilder_AmmoMagazine ammoMag3 = new MyObjectBuilder_AmmoMagazine();
                    ammoMag3.SubtypeName = "Missile200mm";
                    ammoMag3.ProjectilesCount = 50;
                    AddItems(inventory, ammoMag3, false);


                    AddItems(inventory, CreateGunContent("AutomaticRifleItem"), false);
                    AddItems(inventory, CreateGunContent("WelderItem"), false);
                    AddItems(inventory, CreateGunContent("AngleGrinderItem"), false);
                    AddItems(inventory, CreateGunContent("HandDrillItem"), false);
                }

                // Add all components
                foreach (var definition in MyDefinitionManager.Static.GetAllDefinitions())
                {
                    if (definition.Id.TypeId != typeof(MyObjectBuilder_Component) &&
                        definition.Id.TypeId != typeof(MyObjectBuilder_Ingot))
                        continue;

                    if (componentsOnly && definition.Id.TypeId != typeof(MyObjectBuilder_Component))
                        continue;

                    if (componentsOnly && ((MyComponentDefinition)definition).Volume > 0.05f)
                        continue;

                    var component = (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(definition.Id.TypeId);
                    component.SubtypeName = definition.Id.SubtypeName;
                    if (!AddItems(inventory, component, overrideCheck, 1) && spawnNonfitting)
                    {
                        Matrix headMatrix = MySession.Static.ControlledEntity.GetHeadMatrix(true);
                        MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(1, component), headMatrix.Translation + headMatrix.Forward * 0.2f, headMatrix.Forward, headMatrix.Up, MySession.Static.ControlledEntity.Entity.Physics);
                    }
                }

                if (!componentsOnly)
                {
                    string[] ores;
                    MyDefinitionManager.Static.GetOreTypeNames(out ores);
                    foreach (var ore in ores)
                    {
                        var oreBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>(ore);
                        if (!AddItems(inventory, oreBuilder, overrideCheck, 1) && spawnNonfitting)
                        {
                            Matrix headMatrix = MySession.Static.ControlledEntity.GetHeadMatrix(true);
                            MyFloatingObjects.Spawn(new MyPhysicalInventoryItem(1, oreBuilder), headMatrix.Translation + headMatrix.Forward * 0.2f, headMatrix.Forward, headMatrix.Up, MySession.Static.ControlledEntity.Entity.Physics);
                        }
                    }
                }
            }
        }

        private MyObjectBuilder_PhysicalGunObject CreateGunContent(string subtypeName)
        {
            var gunDef = new MyDefinitionId(typeof(MyObjectBuilder_PhysicalGunObject), subtypeName);
            return (MyObjectBuilder_PhysicalGunObject)MyObjectBuilderSerializer.CreateNewObject(gunDef);
        }
    }
}
