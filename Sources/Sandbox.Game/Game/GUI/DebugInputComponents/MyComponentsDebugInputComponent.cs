using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.AI;
using Sandbox.Game.AI.Pathfinding;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using Sandbox.ModAPI;
using VRage.Library.Utils;
using System.Linq;
using VRage.ModAPI;
using System.Diagnostics;
using VRage.Network;
using Sandbox.Common;
using Sandbox.Engine.Multiplayer;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.Game.Entity;

namespace Sandbox.Game.Gui
{
    class MyGuiScreenConfigComponents : MyGuiScreenBase
    {
        MyGuiControlButton m_confirmButton;
        MyGuiControlButton m_cancelButton;
        long m_entityId;
        List<MyEntity> m_entities;
        MyGuiControlCombobox m_entitiesSelection;
        MyGuiControlListbox m_removeComponentsListBox;
        MyGuiControlListbox m_addComponentsListBox;

        public MyGuiScreenConfigComponents(List<MyEntity> entities) :
            base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, null)
        {
            m_entities = entities;
            m_entityId = entities.FirstOrDefault().EntityId;
            CanHideOthers = false;
            EnabledBackgroundFade = true;
            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenConfigComponents";
        }

        public override void RecreateControls(bool contructor)
        {
            base.RecreateControls(contructor);

            // LABEL
            this.Controls.Add(new MyGuiControlLabel(new Vector2(0.0f, -0.46f), text: "Select components to remove and components to add", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER));

            // ENTITY SELECTION
            if (m_entitiesSelection == null)
            {
                m_entitiesSelection = new MyGuiControlCombobox();
                m_entitiesSelection.ItemSelected += EntitySelected;
            }

            m_entitiesSelection.Position = new Vector2(0f, -0.42f);
            m_entitiesSelection.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;

            m_entitiesSelection.ClearItems();
            foreach (var ent in m_entities)
            {
                m_entitiesSelection.AddItem(ent.EntityId, ent.ToString());
            }
            m_entitiesSelection.SelectItemByKey(m_entityId, false);

            this.Controls.Add(m_entitiesSelection);

            // ENTITY ID LABEL
            this.Controls.Add(new MyGuiControlLabel(new Vector2(0.0f, -0.39f), text: String.Format("EntityID = {0}", m_entityId), font: MyFontEnum.White, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER));

            // ENTITY NAME LABEL AND COMPONENTS
            MyEntity entity;
            if (MyEntities.TryGetEntityById(m_entityId, out entity))
            {
                this.Controls.Add(new MyGuiControlLabel(new Vector2(0.0f, -0.36f), text: String.Format("Name: {1}, Type: {0}", entity.GetType().Name, entity.DisplayNameText), font: MyFontEnum.White, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER));
            }

            // REMOVE COMPONENTS LABEL
            this.Controls.Add(new MyGuiControlLabel(new Vector2(-0.21f, -0.32f), text: "Select components to remove", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER));
                        
            // COMPONENTS REMOVE SELECTION BOX
            if (m_removeComponentsListBox == null)
            {
                m_removeComponentsListBox = new MyGuiControlListbox();
            }
            m_removeComponentsListBox.ClearItems();
            m_removeComponentsListBox.MultiSelect = true;
            m_removeComponentsListBox.Name = "RemoveComponents";            
            List<Type> components;
            if (MyComponentContainerExtension.TryGetEntityComponentTypes(m_entityId, out components))
            {
                foreach (var component in components)
                {
                    MyGuiControlListbox.Item item = new MyGuiControlListbox.Item(text: new StringBuilder(component.Name), userData : component);
                    m_removeComponentsListBox.Add(item);
                }
                m_removeComponentsListBox.VisibleRowsCount = components.Count + 1;
            }
            m_removeComponentsListBox.Position = new Vector2(-0.21f, 0f);            
            m_removeComponentsListBox.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;
            m_removeComponentsListBox.ItemSize = new Vector2(0.38f, 0.036f);
            m_removeComponentsListBox.Size = new Vector2(0.4f, 0.6f);            
            this.Controls.Add(m_removeComponentsListBox);

            // ADD COMPONENTS LABEL
            this.Controls.Add(new MyGuiControlLabel(new Vector2(0.21f, -0.32f), text: "Select components to add", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER));

            // COMPONENTS ADD SELECTION BOX
            if (m_addComponentsListBox == null)
            {
                m_addComponentsListBox = new MyGuiControlListbox();
            }
            m_addComponentsListBox.ClearItems();
            m_addComponentsListBox.MultiSelect = true;
            m_addComponentsListBox.Name = "AddComponents";
            components.Clear();
            
            List<MyDefinitionId> definitions = new List<MyDefinitionId>();
            MyDefinitionManager.Static.GetDefinedEntityComponents(ref definitions);

            foreach (var id in definitions)
            {
                var text = id.ToString();
                if (text.StartsWith("MyObjectBuilder_"))
                {
                    text = text.Remove(0, "MyObectBuilder_".Length+1);
                }                    
                MyGuiControlListbox.Item item = new MyGuiControlListbox.Item(text: new StringBuilder(text), userData: id);
                m_addComponentsListBox.Add(item);
            }
            m_addComponentsListBox.VisibleRowsCount = definitions.Count + 1;
            m_addComponentsListBox.Position = new Vector2(0.21f, 0f);
            m_addComponentsListBox.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;
            m_addComponentsListBox.ItemSize = new Vector2(0.36f, 0.036f);
            m_addComponentsListBox.Size = new Vector2(0.4f, 0.6f);
            this.Controls.Add(m_addComponentsListBox);

            m_confirmButton = new MyGuiControlButton(new Vector2(0.21f, 0.35f), MyGuiControlButtonStyleEnum.Default, new Vector2(0.2f, 0.05f), null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, new System.Text.StringBuilder("Confirm"));
            m_cancelButton = new MyGuiControlButton(new Vector2(-0.21f, 0.35f), MyGuiControlButtonStyleEnum.Default, new Vector2(0.2f, 0.05f), null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, new System.Text.StringBuilder("Cancel"));

            this.Controls.Add(m_confirmButton);
            this.Controls.Add(m_cancelButton);

            m_confirmButton.ButtonClicked += confirmButton_OnButtonClick;
            m_cancelButton.ButtonClicked += cancelButton_OnButtonClick;
        }

        private void EntitySelected()
        {
            m_entityId = m_entitiesSelection.GetSelectedKey();
            RecreateControls(false);
        }

        public override void HandleUnhandledInput(bool receivedFocusInThisUpdate)
        {
            base.HandleUnhandledInput(receivedFocusInThisUpdate);
        }

        void confirmButton_OnButtonClick(MyGuiControlButton sender)
        {
            foreach (var selection in m_removeComponentsListBox.SelectedItems)
            {
                MyComponentContainerExtension.TryRemoveComponent(m_entityId, (selection.UserData as Type));
            }

            foreach (var addComp in m_addComponentsListBox.SelectedItems)
            {
                if (addComp.UserData is MyDefinitionId)
                {
                    MyComponentContainerExtension.TryAddComponent(m_entityId, (MyDefinitionId)addComp.UserData);
                }
            }

            CloseScreen();
        }

        void cancelButton_OnButtonClick(MyGuiControlButton sender)
        {
            CloseScreen();
        }
    }

    class MyGuiScreenSpawnEntity : MyGuiScreenBase
    {
        MyGuiControlButton m_confirmButton;
        MyGuiControlButton m_cancelButton;
        MyGuiControlListbox m_addComponentsListBox;
        MyGuiControlCheckbox m_replicableEntityCheckBox;
        Vector3 m_position;

        public MyGuiScreenSpawnEntity(Vector3 position) :
            base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, null)
        {
            m_position = position;
            CanHideOthers = false;
            EnabledBackgroundFade = true;
            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenSpawnEntity";
        }

        public override void RecreateControls(bool contructor)
        {
            base.RecreateControls(contructor);

            // LABEL
            this.Controls.Add(new MyGuiControlLabel(new Vector2(0.0f, -0.46f), text: "Select components to include in entity", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER));


            m_replicableEntityCheckBox = new MyGuiControlCheckbox();
            m_replicableEntityCheckBox.Position = new Vector2(0f, -0.42f);
            m_replicableEntityCheckBox.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;            
            this.Controls.Add(m_replicableEntityCheckBox);

            // ENTITY TYPE LABEL
            this.Controls.Add(new MyGuiControlLabel(new Vector2(0.0f, -0.39f), text: "MyEntityReplicable / MyEntity", font: MyFontEnum.White, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER));


            // ADD COMPONENTS LABEL
            this.Controls.Add(new MyGuiControlLabel(new Vector2(0.0f, -0.32f), text: "Select components to add", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER));

            // COMPONENTS ADD SELECTION BOX
            if (m_addComponentsListBox == null)
            {
                m_addComponentsListBox = new MyGuiControlListbox();
            }
            m_addComponentsListBox.ClearItems();
            m_addComponentsListBox.MultiSelect = true;
            m_addComponentsListBox.Name = "AddComponents";

            List<MyDefinitionId> definitions = new List<MyDefinitionId>();
            MyDefinitionManager.Static.GetDefinedEntityComponents(ref definitions);

            foreach (var id in definitions)
            {
                var text = id.ToString();
                if (text.StartsWith("MyObjectBuilder_"))
                {
                    text = text.Remove(0, "MyObectBuilder_".Length + 1);
                }
                MyGuiControlListbox.Item item = new MyGuiControlListbox.Item(text: new StringBuilder(text), userData: id);
                m_addComponentsListBox.Add(item);
            }
            m_addComponentsListBox.VisibleRowsCount = definitions.Count + 1;
            m_addComponentsListBox.Position = new Vector2(0.0f, 0f);
            m_addComponentsListBox.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;
            m_addComponentsListBox.ItemSize = new Vector2(0.36f, 0.036f);
            m_addComponentsListBox.Size = new Vector2(0.4f, 0.6f);
            this.Controls.Add(m_addComponentsListBox);

            m_confirmButton = new MyGuiControlButton(new Vector2(0.21f, 0.35f), MyGuiControlButtonStyleEnum.Default, new Vector2(0.2f, 0.05f), null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, new System.Text.StringBuilder("Confirm"));
            m_cancelButton = new MyGuiControlButton(new Vector2(-0.21f, 0.35f), MyGuiControlButtonStyleEnum.Default, new Vector2(0.2f, 0.05f), null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, new System.Text.StringBuilder("Cancel"));

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
            MyContainerDefinition newDefinition = new MyContainerDefinition();

            foreach (var addComp in m_addComponentsListBox.SelectedItems)
            {                
                if (addComp.UserData is MyDefinitionId)
                {
                    var defId = (MyDefinitionId)addComp.UserData;
                    
                    MyContainerDefinition.DefaultComponent component = new MyContainerDefinition.DefaultComponent();
                    component.BuilderType = defId.TypeId;
                    component.SubtypeId = defId.SubtypeId;

                    newDefinition.DefaultComponents.Add(component);
                }
            }
            
            MyObjectBuilder_EntityBase entityOb = null;
            if (m_replicableEntityCheckBox.IsChecked)
            {
                entityOb = new MyObjectBuilder_ReplicableEntity();
                newDefinition.Id = new MyDefinitionId(typeof(MyObjectBuilder_ReplicableEntity), "DebugTest");
            }
            else
            {
                entityOb = new MyObjectBuilder_EntityBase();
                newDefinition.Id = new MyDefinitionId(typeof(MyObjectBuilder_EntityBase), "DebugTest");
            }
            MyDefinitionManager.Static.SetEntityContainerDefinition(newDefinition);

            entityOb.SubtypeName = newDefinition.Id.SubtypeName;
            entityOb.PositionAndOrientation = new MyPositionAndOrientation(m_position, Vector3.Forward, Vector3.Up);

            MyEntity entity = MyEntities.CreateFromObjectBuilderAndAdd(entityOb);

            Debug.Assert(entity != null, "Entity wasn't created!");

            CloseScreen();
        }

        void cancelButton_OnButtonClick(MyGuiControlButton sender)
        {
            CloseScreen();
        }
    }

    class MyGuiScreenSpawnDefinedEntity : MyGuiScreenBase
    {
        MyGuiControlButton m_confirmButton;
        MyGuiControlButton m_cancelButton;
        MyGuiControlListbox m_containersListBox;
        MyGuiControlCheckbox m_replicableEntityCheckBox;
        Vector3 m_position;

        public MyGuiScreenSpawnDefinedEntity(Vector3 position) :
            base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, null)
        {
            m_position = position;
            CanHideOthers = false;
            EnabledBackgroundFade = true;
            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "MyGuiScreenSpawnDefinedEntity";
        }

        public override void RecreateControls(bool contructor)
        {
            base.RecreateControls(contructor);

            // LABEL
            this.Controls.Add(new MyGuiControlLabel(new Vector2(0.0f, -0.46f), text: "Select entity to spawn", originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER));

            // CONTAINERS
            if (m_containersListBox == null)
            {
                m_containersListBox = new MyGuiControlListbox();
            }
            m_containersListBox.ClearItems();
            m_containersListBox.MultiSelect = false;
            m_containersListBox.Name = "Containers";

            List<MyDefinitionId> definitions = new List<MyDefinitionId>();
            MyDefinitionManager.Static.GetDefinedEntityContainers(ref definitions);

            foreach (var id in definitions)
            {
                var text = id.ToString();
                if (text.StartsWith("MyObjectBuilder_"))
                {
                    text = text.Remove(0, "MyObectBuilder_".Length + 1);
                }
                MyGuiControlListbox.Item item = new MyGuiControlListbox.Item(text: new StringBuilder(text), userData: id);
                m_containersListBox.Add(item);
            }
            m_containersListBox.VisibleRowsCount = definitions.Count + 1;
            m_containersListBox.Position = new Vector2(0.0f, 0f);
            m_containersListBox.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER;
            m_containersListBox.ItemSize = new Vector2(0.36f, 0.036f);
            m_containersListBox.Size = new Vector2(0.4f, 0.6f);
            this.Controls.Add(m_containersListBox);

            m_confirmButton = new MyGuiControlButton(new Vector2(0.21f, 0.35f), MyGuiControlButtonStyleEnum.Default, new Vector2(0.2f, 0.05f), null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, new System.Text.StringBuilder("Confirm"));
            m_cancelButton = new MyGuiControlButton(new Vector2(-0.21f, 0.35f), MyGuiControlButtonStyleEnum.Default, new Vector2(0.2f, 0.05f), null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, new System.Text.StringBuilder("Cancel"));

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
            MyContainerDefinition newDefinition = new MyContainerDefinition();

            foreach (var addComp in m_containersListBox.SelectedItems)
            {
                if (addComp.UserData is MyDefinitionId)
                {
                    var defId = (MyDefinitionId)addComp.UserData;

                    MyEntity entity = MyEntities.CreateEntityAndAdd(defId, true, m_position);
                    Debug.Assert(entity != null, "Entity wasn't created!");
                }
            }

            CloseScreen();
        }

        void cancelButton_OnButtonClick(MyGuiControlButton sender)
        {
            CloseScreen();
        }
    }

    [StaticEventOwner]
    public class MyComponentsDebugInputComponent : MyDebugComponent
    {        
        public static List<BoundingBoxD> Boxes = null;
        public static List<MyEntity> DetectedEntities = new List<MyEntity>();

        public MyComponentsDebugInputComponent()
        {
            AddShortcut(MyKeys.G, true, false, false, false, () => "Show components Config Screen.", ShowComponentsConfigScreen);
            AddShortcut(MyKeys.H, true, false, false, false, () => "Show entity spawn screen.", ShowEntitySpawnScreen);
            AddShortcut(MyKeys.J, true, false, false, false, () => "Show defined entites spawn screen.", ShowDefinedEntitySpawnScreen);
        }

        private bool ShowComponentsConfigScreen()
        {
            if (DetectedEntities.Count == 0)
                return false;

            var dialog = new MyGuiScreenConfigComponents(DetectedEntities);

            MyGuiSandbox.AddScreen(dialog);
            
            return true;
        }
        
        public override void Draw()
        {
            base.Draw();

            if (!MyDebugDrawSettings.ENABLE_DEBUG_DRAW) return;
            
            // TODO: Special draw..
        }

        public override string GetName()
        {
            return "Components config";
        }

        private bool ShowEntitySpawnScreen()
        {
            var entity = MySession.Static.ControlledEntity as MyEntity;

            if (entity != null)
            {
                var dialog = new MyGuiScreenSpawnEntity(entity.WorldMatrix.Translation + entity.WorldMatrix.Forward + entity.WorldMatrix.Up);
                MyGuiSandbox.AddScreen(dialog);
            }           

            return true;
        }

        private bool ShowDefinedEntitySpawnScreen()
        {
            var entity = MySession.Static.ControlledEntity as MyEntity;

            if (entity != null)
            {
                var dialog = new MyGuiScreenSpawnDefinedEntity(entity.WorldMatrix.Translation + entity.WorldMatrix.Forward + entity.WorldMatrix.Up);
                MyGuiSandbox.AddScreen(dialog);
            }

            return true;
        }

    }

}
