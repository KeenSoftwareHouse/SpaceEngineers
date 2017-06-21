using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Havok;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Inventory;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.GUI.DebugInputComponents;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI.Interfaces;
using VRage.Game.SessionComponents;
using VRage.Game.VisualScripting.Missions;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.Screens
{
    public class MyGuiScreenScriptingTools : MyGuiScreenDebugBase
    {
        private static readonly Vector2 SCREEN_SIZE = new Vector2(0.40f, 1.2f);
        private static readonly float HIDDEN_PART_RIGHT = 0.04f;
        private static readonly float ITEM_HORIZONTAL_PADDING = 0.01f;
        private static readonly float ITEM_VERTICAL_PADDING = 0.005f;
        private static readonly Vector2 BUTTON_SIZE = new Vector2(0.06f, 0.03f);
        private static readonly Vector2 ITEM_SIZE = new Vector2(0.06f, 0.02f);
        private static readonly string ENTITY_NAME_PREFIX = "Waypoint_";
        
        private static uint m_entityCounter = 0;

        private IMyCameraController m_previousCameraController;
        private MyGuiControlButton m_setTriggerSizeButton;
        private MyGuiControlButton m_enlargeTriggerButton;
        private MyGuiControlButton m_shrinkTriggerButton;
        private MyGuiControlListbox m_triggersListBox;
        private MyGuiControlListbox m_smListBox;
        private MyGuiControlListbox m_levelScriptListBox;
        private MyGuiControlTextbox m_selectedTriggerNameBox;
        private MyGuiControlTextbox m_selectedEntityNameBox;
        private MyGuiControlTextbox m_selectedFunctionalBlockNameBox;
        private MyEntity m_selectedFunctionalBlock;
        private bool m_disablePicking;

        private readonly MyTriggerManipulator m_triggerManipulator;
        private readonly MyEntityTransformationSystem m_transformSys;
        private readonly MyVisualScriptManagerSessionComponent m_scriptManager;

        private readonly StringBuilder m_helperStringBuilder = new StringBuilder();

        public MyGuiScreenScriptingTools() : base(
            new Vector2(MyGuiManager.GetMaxMouseCoord().X - SCREEN_SIZE.X * 0.5f + HIDDEN_PART_RIGHT, 0.5f), 
            SCREEN_SIZE, 
            MyGuiConstants.SCREEN_BACKGROUND_COLOR, 
            false)
        {
            CanBeHidden = true;
            CanHideOthers = false;
            m_canCloseInCloseAllScreenCalls = true;
            m_canShareInput = true;
            m_isTopScreen = false;
            m_isTopMostScreen = false;

            // Create new manipulator with predicate for area triggers only
            m_triggerManipulator = new MyTriggerManipulator(trigger => trigger is MyAreaTriggerComponent);
            m_transformSys = MySession.Static.GetComponent<MyEntityTransformationSystem>();
            m_transformSys.ControlledEntityChanged += TransformSysOnControlledEntityChanged;
            m_transformSys.RayCasted += TransformSysOnRayCasted;
            m_scriptManager = MySession.Static.GetComponent<MyVisualScriptManagerSessionComponent>();

            // Switch to spectator
            MySession.Static.SetCameraController(MyCameraControllerEnum.SpectatorFreeMouse);

            // Enable Debug draw when opened
            MyDebugDrawSettings.ENABLE_DEBUG_DRAW = true;
            MyDebugDrawSettings.DEBUG_DRAW_UPDATE_TRIGGER = true;

            RecreateControls(true);
        }

        public override void HandleInput(bool receivedFocusInThisUpdate)
        {
            // Re enable picking for transformation system
            if (m_transformSys.DisablePicking)
            {
                m_transformSys.DisablePicking = false;
            }

            // Disable picking in Transform sys when clicked into GUI
            if (MyInput.Static.IsNewPrimaryButtonPressed())
            {
                var clickPos = MyGuiManager.GetNormalizedCoordinateFromScreenCoordinate(MyInput.Static.GetMousePosition());
                var screenBorderPosition = GetPosition() - SCREEN_SIZE * 0.5f;
                if (clickPos.X > screenBorderPosition.X)
                {
                    m_transformSys.DisablePicking = true;
                }
            }

            // Override for everything to be able to use toolbar
            if (!MyToolbarComponent.IsToolbarControlShown)
                MyToolbarComponent.IsToolbarControlShown = true;

            // To prevent space confirmation
            FocusedControl = null;

            if (MyInput.Static.IsNewKeyPressed(MyKeys.Escape) || MyInput.Static.IsNewKeyPressed(MyKeys.F11))
            {
                CloseScreen();
                return;
            }

            base.HandleInput(receivedFocusInThisUpdate);

            // Switch to spectator camera with free mouse
            if (MySpectatorCameraController.Static.SpectatorCameraMovement != MySpectatorCameraMovementEnum.FreeMouse)
            {
                MySpectatorCameraController.Static.SpectatorCameraMovement = MySpectatorCameraMovementEnum.FreeMouse;
            }

            // Broadcast input to all other screens
            foreach (var screen in MyScreenManager.Screens)
            {
                if(screen != this)
                {
                    screen.HandleInput(receivedFocusInThisUpdate);
                }
            }

            HandleShortcuts();
        }

        private void HandleShortcuts()
        {
            // Deselect
            if (MyInput.Static.IsAnyCtrlKeyPressed() && MyInput.Static.IsNewKeyPressed(MyKeys.D))
            {
                m_transformSys.SetControlledEntity(null);
            }

            if(MyInput.Static.IsAnyShiftKeyPressed() || MyInput.Static.IsAnyCtrlKeyPressed() || MyInput.Static.IsAnyAltKeyPressed())
                return;

            // Enlarge trigger
            if (MyInput.Static.IsNewKeyPressed(MyKeys.Add))
            {
                EnlargeTriggerOnClick(null);
            }

            // Shrink trigger
            if (MyInput.Static.IsNewKeyPressed(MyKeys.Subtract))
            {
                ShrinkTriggerOnClick(null);
            }

            // Delete shortcut
            if (MyInput.Static.IsNewKeyPressed(MyKeys.Delete))
            {
                DeleteEntityOnClicked(null);
            }

            // New entity shortcut
            if (MyInput.Static.IsNewKeyPressed(MyKeys.N))
            {
                SpawnEntityClicked(null);
            }
        }

        public override bool CloseScreen()
        {
            // Reset spectator and camera controller to previous state
            MySpectatorCameraController.Static.SpectatorCameraMovement = MySpectatorCameraMovementEnum.UserControlled;
            MySession.Static.SetCameraController(MyCameraControllerEnum.Entity, MySession.Static.ControlledEntity.Entity);

            // Disable Debug Draw
            MyDebugDrawSettings.ENABLE_DEBUG_DRAW = false;
            MyDebugDrawSettings.DEBUG_DRAW_UPDATE_TRIGGER = false;

            // Disable transform sys
            m_transformSys.Active = false;

            return base.CloseScreen();
        }

        public override bool Update(bool hasFocus)
        {
            // Enable/Disable cursor
            if (MyCubeBuilder.Static.CubeBuilderState.CurrentBlockDefinition != null || MyInput.Static.IsRightMousePressed())
            {
                DrawMouseCursor = false;
            }
            else
            {
                DrawMouseCursor = true;
            }

            // Update triggers and their GUI
            m_triggerManipulator.CurrentPosition = MyAPIGateway.Session.Camera.Position;
            UpdateTriggerList();

            // Update the level script listbox -- only change can be sudden fail of the script
            for (int index = 0; index < m_scriptManager.FailedLevelScriptExceptionTexts.Length; index++)
            {
                var failedLevelScriptExceptionText = m_scriptManager.FailedLevelScriptExceptionTexts[index];
                if (failedLevelScriptExceptionText != null && (bool)m_levelScriptListBox.Items[index].UserData)
                {
                    m_levelScriptListBox.Items[index].Text.Append(" - failed");
                    m_levelScriptListBox.Items[index].FontOverride = MyFontEnum.Red;
                    m_levelScriptListBox.Items[index].ToolTip.AddToolTip(failedLevelScriptExceptionText, font: MyFontEnum.Red);
                }
            }

            // Update running state machines
            foreach (var stateMachine in m_scriptManager.SMManager.RunningMachines)
            {
                var indexOf = m_smListBox.Items.FindIndex(item => (MyVSStateMachine)item.UserData == stateMachine);
                if (indexOf == -1)
                {
                    // new Entry
                    m_smListBox.Add(new MyGuiControlListbox.Item(new StringBuilder(stateMachine.Name), userData: stateMachine, toolTip: "Cursors:"));
                    indexOf = m_smListBox.Items.Count - 1;
                }

                var listItem = m_smListBox.Items[indexOf];
                // Remove tooltips for missing cursors
                for (int index = listItem.ToolTip.ToolTips.Count - 1; index >= 0; index--)
                {
                    var toolTip = listItem.ToolTip.ToolTips[index];

                    var found = false;
                    foreach (var cursor in stateMachine.ActiveCursors)
                    {
                        if (toolTip.Text.CompareTo(cursor.Node.Name) == 0)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found && index != 0)
                    {
                        // remove missing
                        listItem.ToolTip.ToolTips.RemoveAtFast(index);
                    }
                }

                foreach (var cursor in stateMachine.ActiveCursors)
                {
                    var found = false;
                    for (int index = listItem.ToolTip.ToolTips.Count - 1; index >= 0; index--)
                    {
                        var text = listItem.ToolTip.ToolTips[index];
                        if (text.Text.CompareTo(cursor.Node.Name) == 0)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // Add missing
                        listItem.ToolTip.AddToolTip(cursor.Node.Name);
                    }
                }
            }

            return base.Update(hasFocus);
        }

        private void UpdateTriggerList()
        {
            var itemCollection = m_triggersListBox.Items;

            for (var index = 0; index < itemCollection.Count; index++)
            {
                var areaTrigger = (MyAreaTriggerComponent)itemCollection[index].UserData;
                // Remove all that are not queried
                if (!m_triggerManipulator.CurrentQuery.Contains(areaTrigger))
                {
                    itemCollection.RemoveAtFast(index);
                }
            }

            foreach (var trigger in m_triggerManipulator.CurrentQuery)
            {
                var index = m_triggersListBox.Items.FindIndex(item => (MyTriggerComponent)item.UserData == trigger);
                if (index < 0)
                {
                    // Insert missing trigger
                    var areaTrigger = (MyAreaTriggerComponent)trigger;
                    var itemTextSb = new StringBuilder("Trigger: ");
                    itemTextSb.Append(areaTrigger.Name).Append(" Entity: ");

                    // For named entities add their name
                    itemTextSb.Append(string.IsNullOrEmpty(areaTrigger.Entity.Name)
                        ? areaTrigger.Entity.DisplayName
                        : areaTrigger.Entity.Name);

                    m_triggersListBox.Add(new MyGuiControlListbox.Item(
                            itemTextSb,
                            toolTip: areaTrigger.Name,
                            userData: areaTrigger
                        ));
                }
            }
        }

        public override void RecreateControls(bool constructor)
        {
            base.RecreateControls(constructor);
            float hiddenPartTop = (SCREEN_SIZE.Y - 1.0f) / 2.0f;
            Vector2 controlPadding = new Vector2(0.02f, 0);

            // Position the Caption
            var caption = AddCaption("Scripting Tools", Color.White.ToVector4(), controlPadding + new Vector2(-HIDDEN_PART_RIGHT, hiddenPartTop));
            m_currentPosition.Y = caption.PositionY + caption.Size.Y + ITEM_VERTICAL_PADDING;

            // Position all the controls under the caption
            // Debug draw checkbox
            PositionControls(new MyGuiControlBase[]
            {
                CreateLabel("Disable Transformation"), 
                CreateCheckbox(DisableTransformationOnCheckedChanged, m_transformSys.DisableTransformation)
            });
            // Selected entity controls
            m_selectedEntityNameBox = CreateTextbox("");
            PositionControls(new MyGuiControlBase[]
            {
                CreateLabel("Selected Entity: "), 
                m_selectedEntityNameBox, 
                CreateButton("Rename", RenameSelectedEntityOnClick)
            });
            m_selectedFunctionalBlockNameBox = CreateTextbox("");
            PositionControls(new MyGuiControlBase[]
            {
                CreateLabel("Selected Block: "), 
                m_selectedFunctionalBlockNameBox, 
                CreateButton("Rename", RenameFunctionalBlockOnClick)
            });
            // Spawn entity button
            PositionControls(new MyGuiControlBase[]
            {
                CreateButton("Spawn Entity", SpawnEntityClicked), 
                CreateButton("Delete Entity", DeleteEntityOnClicked)
            });

            // Trigger section
            PositionControl(CreateLabel("Triggers"));

            // Attach new trigger
            PositionControl(CreateButton("Attach to selected entity", AttachTriggerOnClick));

            m_enlargeTriggerButton = CreateButton("+", EnlargeTriggerOnClick);
            m_shrinkTriggerButton = CreateButton("-", ShrinkTriggerOnClick);
            m_setTriggerSizeButton = CreateButton("Size", SetSizeOnClick);

            // Enlarge, Set size, Shrink
            PositionControls(new MyGuiControlBase[]
            {
                m_enlargeTriggerButton, 
                m_setTriggerSizeButton, 
                m_shrinkTriggerButton
            });

            // Snap, Select, Delete
            PositionControls(new MyGuiControlBase[]
                {
                    CreateButton("Snap", SnapTriggerToCameraOrEntityOnClick),
                    CreateButton("Select", SelectTriggerOnClick),
                    CreateButton("Delete", DeleteTriggerOnClick)
                }
            );

            // Selected trigger section
            m_selectedTriggerNameBox = CreateTextbox("Trigger not selected");
            PositionControls(new MyGuiControlBase[] {CreateLabel("Selected Trigger:"), m_selectedTriggerNameBox});

            // Listbox for queried triggers
            m_triggersListBox = CreateListBox();
            m_triggersListBox.Size = new Vector2(0f, 0.07f);
            m_triggersListBox.ItemDoubleClicked += TriggersListBoxOnItemDoubleClicked;
            PositionControl(m_triggersListBox);
            // Because something is reseting the value
            m_triggersListBox.ItemSize = new Vector2(SCREEN_SIZE.X, ITEM_SIZE.Y);

            // Running Level Scripts
            PositionControl(CreateLabel("Running Level Scripts"));
            m_levelScriptListBox = CreateListBox();
            m_levelScriptListBox.Size = new Vector2(0f, 0.07f);
            PositionControl(m_levelScriptListBox);
            // Because something is reseting the value
            m_triggersListBox.ItemSize = new Vector2(SCREEN_SIZE.X, ITEM_SIZE.Y);

            // Fill with levelscripts -- they wont change during the process
            foreach (var runningLevelScriptName in m_scriptManager.RunningLevelScriptNames)
            {
                // user data are there to tell if the script already failed or not
                m_levelScriptListBox.Add(new MyGuiControlListbox.Item(new StringBuilder(runningLevelScriptName), userData: false));
            }

            // Running State machines
            PositionControl(CreateLabel("Running state machines"));
            m_smListBox = CreateListBox();
            m_smListBox.Size = new Vector2(0f, 0.07f);
            PositionControl(m_smListBox);
            // Because something is reseting the value
            m_smListBox.ItemSize = new Vector2(SCREEN_SIZE.X, ITEM_SIZE.Y);
        }

        #region GUI callbacks

        private void TransformSysOnRayCasted(LineD ray)
        {
            if(m_transformSys.ControlledEntity == null || m_disablePicking) return;

            if(m_selectedFunctionalBlock != null)
            {
                // Disable highlight on block
                var highlightSystem = MySession.Static.GetComponent<MyHighlightSystem>();
                if (highlightSystem != null)
                {
                    highlightSystem.RequestHighlightChange(new MyHighlightSystem.MyHighlightData
                    {
                        EntityId = m_selectedFunctionalBlock.EntityId,
                        PlayerId = -1,
                        Thickness = -1
                    });
                }

                m_selectedFunctionalBlock = null;
            }
            // If the entity was a grid, try picking a functional block
            var grid = m_transformSys.ControlledEntity as MyCubeGrid;
            if (grid != null)
            {
                var blockPosition = grid.RayCastBlocks(ray.From, ray.To);

                if (blockPosition.HasValue)
                {
                    var block = grid.GetCubeBlock(blockPosition.Value);
                    if (block.FatBlock != null)
                    {
                        m_selectedFunctionalBlock = block.FatBlock;
                    }
                }
            }

            // Refresh Gui data
            m_helperStringBuilder.Clear();
            if (m_selectedFunctionalBlock != null)
            {
                // Show display name when name is empty
                m_helperStringBuilder
                    .Append(string.IsNullOrEmpty(m_selectedFunctionalBlock.Name) ? m_selectedFunctionalBlock.DisplayNameText : m_selectedFunctionalBlock.Name);

                // Enable highlight on block
                var highlightSystem = MySession.Static.GetComponent<MyHighlightSystem>();
                if(highlightSystem != null)
                {
                    highlightSystem.RequestHighlightChange(new MyHighlightSystem.MyHighlightData
                    {
                        EntityId = m_selectedFunctionalBlock.EntityId,
                        IgnoreUseObjectData = true,
                        OutlineColor = Color.Blue,
                        PulseTimeInFrames = 120,
                        Thickness = 3,
                        PlayerId = -1
                    });
                }
            }

            m_selectedFunctionalBlockNameBox.SetText(m_helperStringBuilder);
        }

        private void RenameFunctionalBlockOnClick(MyGuiControlButton myGuiControlButton)
        {
            if(m_selectedFunctionalBlock == null) return;
            // disable picking
            m_disablePicking = true;
            m_transformSys.DisablePicking = true;

            // Rename dialog
            var dialog = new ValueGetScreenWithCaption("Entity Rename: " + m_selectedFunctionalBlock.DisplayNameText,
                "",
                delegate(string text)
                {
                    MyEntity e;
                    if (MyEntities.TryGetEntityByName(text, out e))
                        return false;

                    m_selectedFunctionalBlock.Name = text;
                    MyEntities.SetEntityName(m_selectedFunctionalBlock, true);
                    m_helperStringBuilder.Clear().Append(text);
                    m_selectedFunctionalBlockNameBox.SetText(m_helperStringBuilder);
                    return true;
                });

            // Enable picking
            dialog.Closed += source => {m_disablePicking = false; m_transformSys.DisablePicking = false; };

            MyGuiSandbox.AddScreen(dialog);
        }

        private void RenameSelectedEntityOnClick(MyGuiControlButton myGuiControlButton)
        {
            if (m_transformSys.ControlledEntity == null) return;

            // Disable picking
            m_disablePicking = true;
            m_transformSys.DisablePicking = true;

            // Rename dialog
            var selectedEntity = m_transformSys.ControlledEntity;
            var dialog = new ValueGetScreenWithCaption("Entity Rename: " + m_transformSys.ControlledEntity.DisplayNameText,
                "",
                delegate(string text)
                {
                    MyEntity e;
                    if(MyEntities.TryGetEntityByName(text, out e))
                        return false;

                    selectedEntity.Name = text;
                    MyEntities.SetEntityName(selectedEntity, true);

                    m_helperStringBuilder.Clear().Append(text);
                    m_selectedEntityNameBox.SetText(m_helperStringBuilder);
                    return true;
                });

            // Enable picking
            dialog.Closed += source => { m_disablePicking = false; m_transformSys.DisablePicking = false; };

            MyGuiSandbox.AddScreen(dialog);
        }

        private void DeleteEntityOnClicked(MyGuiControlButton myGuiControlButton)
        {
            if(m_transformSys.ControlledEntity != null)
            {
                m_transformSys.ControlledEntity.Close();
                m_transformSys.SetControlledEntity(null);
            }
        }

        private void AttachTriggerOnClick(MyGuiControlButton myGuiControlButton)
        {
            if(m_transformSys.ControlledEntity == null) return;

            var selectedEntity = m_transformSys.ControlledEntity;
            var dialog = new ValueGetScreenWithCaption("Entity Spawn on: " + m_transformSys.ControlledEntity.DisplayName,
                "", 
                delegate(string text)
            {
                var areaTrigger = new MyAreaTriggerComponent(text);
                m_triggerManipulator.SelectedTrigger = areaTrigger;

                if (!selectedEntity.Components.Contains(typeof(MyTriggerAggregate)))
                {
                    // Add agregate if its missing
                    selectedEntity.Components.Add(typeof(MyTriggerAggregate), new MyTriggerAggregate());
                }
                // add trigger it self
                selectedEntity.Components.Get<MyTriggerAggregate>().AddComponent(m_triggerManipulator.SelectedTrigger);
                // Init trigger dimensions
                areaTrigger.Center = MyAPIGateway.Session.Camera.Position;
                areaTrigger.Radius = 2;
                // Selected color
                areaTrigger.CustomDebugColor = Color.Yellow;

                return true;
            });
            MyGuiSandbox.AddScreen(dialog);
        }

        private void DeleteTriggerOnClick(MyGuiControlButton myGuiControlButton)
        {
            if(m_triggerManipulator.SelectedTrigger == null)
                return;

            // Remove the trigger component from entity container
            m_triggerManipulator.SelectedTrigger.Entity.Components.Remove(typeof(MyTriggerAggregate), m_triggerManipulator.SelectedTrigger);
            // Remove the trigger
            m_triggerManipulator.SelectedTrigger = null;
            // Remove the trigger from gui
            m_helperStringBuilder.Clear();
            m_selectedEntityNameBox.SetText(m_helperStringBuilder);
        }

        private void SnapTriggerToCameraOrEntityOnClick(MyGuiControlButton myGuiControlButton)
        {
            if(m_triggerManipulator.SelectedTrigger == null)
                return;

            var areaTrigger = (MyAreaTriggerComponent)m_triggerManipulator.SelectedTrigger;
            if (m_transformSys.ControlledEntity != null)
            {
                // Snap to entity
                areaTrigger.Center = m_transformSys.ControlledEntity.PositionComp.GetPosition();
                return;
            }

            // Change the trigges position to camera position
            areaTrigger.Center = MyAPIGateway.Session.Camera.Position;
        }

        private void TransformSysOnControlledEntityChanged(MyEntity oldEntity, MyEntity newEntity)
        {
            if(m_disablePicking) return;

            m_helperStringBuilder.Clear();
            if (newEntity != null)
            {
                // Change the entity name
                // Show display name when name is empty
                m_helperStringBuilder.Clear()
                    .Append(string.IsNullOrEmpty(newEntity.Name) ? newEntity.DisplayName : newEntity.Name);
            }

            m_selectedEntityNameBox.SetText(m_helperStringBuilder);

            // Because the ray event is called before entity changed
            TransformSysOnRayCasted(m_transformSys.LastRay);
        }

        private void TriggersListBoxOnItemDoubleClicked(MyGuiControlListbox listBox)
        {
            if (listBox.SelectedItems.Count == 0) return;

            // Set the selected trigger to the selected one
            var item = listBox.SelectedItems[0];
            var trigger = (MyAreaTriggerComponent) item.UserData;

            m_triggerManipulator.SelectedTrigger = trigger;
            // Reset the GUI data
            if (m_triggerManipulator.SelectedTrigger != null)
            {
                var areaTrigger = (MyAreaTriggerComponent) m_triggerManipulator.SelectedTrigger;

                // Set textbox Text ... not the easy way....
                m_helperStringBuilder.Clear();
                m_helperStringBuilder.Append(areaTrigger.Name);
                m_selectedTriggerNameBox.SetText(m_helperStringBuilder);
            }
        }

        private void SetSizeOnClick(MyGuiControlButton button)
        {
            if (m_triggerManipulator.SelectedTrigger == null) return;

            var areaTrigger = (MyAreaTriggerComponent) m_triggerManipulator.SelectedTrigger;
            var dialog = new ValueGetScreenWithCaption("Set trigger size dialog",
                areaTrigger.Radius.ToString(CultureInfo.InvariantCulture),
                delegate(string text)
                {
                    float value;
                    if (!float.TryParse(text, out value))
                        return false;

                    areaTrigger.Radius = value;

                    return true;
                });
            MyGuiSandbox.AddScreen(dialog);
        }

        private void ShrinkTriggerOnClick(MyGuiControlButton button)
        {
            if (m_triggerManipulator.SelectedTrigger != null)
            {
                var areaTrigger = (MyAreaTriggerComponent) m_triggerManipulator.SelectedTrigger;
                areaTrigger.Radius -= 0.2f;

                if (areaTrigger.Radius < 0.2f)
                {
                    areaTrigger.Radius = 0.2f;
                }
            }
        }

        private void EnlargeTriggerOnClick(MyGuiControlButton button)
        {
            if (m_triggerManipulator.SelectedTrigger != null)
            {
                var areaTrigger = (MyAreaTriggerComponent) m_triggerManipulator.SelectedTrigger;
                areaTrigger.Radius += 0.2f;
            }
        }

        private void SelectTriggerOnClick(MyGuiControlButton button)
        {
            // Perform pick and change screen data
            m_triggerManipulator.SelectClosest(MyAPIGateway.Session.Camera.Position);
            if (m_triggerManipulator.SelectedTrigger != null)
            {
                var areaTrigger = (MyAreaTriggerComponent) m_triggerManipulator.SelectedTrigger;

                // Set textbox Text ... not the easy way....
                m_helperStringBuilder.Clear();
                m_helperStringBuilder.Append(areaTrigger.Name);
                m_selectedTriggerNameBox.SetText(m_helperStringBuilder);
            }
        }


        // Spawn empty entity
        private void SpawnEntityClicked(MyGuiControlButton myGuiControlButton)
        {
            string newEntityName;
            MyEntity e;

            do
            {
               newEntityName = ENTITY_NAME_PREFIX + m_entityCounter++; 
            } while(MyEntities.TryGetEntityByName(newEntityName, out e));

            var entity = new MyEntity
            {
                WorldMatrix = MyAPIGateway.Session.Camera.WorldMatrix,
                EntityId = MyEntityIdentifier.AllocateId(),
                DisplayName = "Entity",
                Name = newEntityName
            };
            // Place the entity 2m in the front
            entity.PositionComp.SetPosition(MyAPIGateway.Session.Camera.Position + MyAPIGateway.Session.Camera.WorldMatrix.Forward * 2);
            entity.Components.Remove<MyPhysicsComponentBase>();

            MyEntities.Add(entity);
            MyEntities.SetEntityName(entity, true);

            m_transformSys.SetControlledEntity(entity);
        }

        private void DisableTransformationOnCheckedChanged(MyGuiControlCheckbox checkbox)
        {
            m_transformSys.DisableTransformation = checkbox.IsChecked;
        }

        #endregion

        #region Create Gui controls

        private MyGuiControlCheckbox CreateCheckbox(Action<MyGuiControlCheckbox> onCheckedChanged, bool isChecked)
        {
            var checkBox = new MyGuiControlCheckbox(
                visualStyle: MyGuiControlCheckboxStyleEnum.Debug,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                isChecked: isChecked
                );


            checkBox.Size = ITEM_SIZE;
            checkBox.IsCheckedChanged += onCheckedChanged;

            Controls.Add(checkBox);

            return checkBox;
        }

        private MyGuiControlTextbox CreateTextbox(string text, Action<MyGuiControlTextbox> textChanged = null)
        {
            var textBox = new MyGuiControlTextbox(
                visualStyle: MyGuiControlTextboxStyleEnum.Debug,
                defaultText: text,
                textScale: 0.8f
                );

            textBox.Enabled = false;
            textBox.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            textBox.Size = ITEM_SIZE;
            textBox.TextChanged += textChanged;

            Controls.Add(textBox);
            return textBox;
        }

        private MyGuiControlLabel CreateLabel(string text)
        {
            var label = new MyGuiControlLabel(
                size: ITEM_SIZE,
                text: text,
                font: MyFontEnum.Debug,
                originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                textScale: 0.8f
                );

            Controls.Add(label);

            return label;
        }

        private MyGuiControlListbox CreateListBox()
        {
            var listBox = new MyGuiControlListbox(visualStyle: MyGuiControlListboxStyleEnum.Blueprints)
            {
                OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP,
                Size = new Vector2(1f, BUTTON_SIZE.Y * 4) // Just about the size of 4 entries, nothing accurate
            };
            listBox.MultiSelect = false;
            listBox.Enabled = true;
            listBox.ItemSize = new Vector2(SCREEN_SIZE.X, ITEM_SIZE.Y);
            listBox.VisibleRowsCount = 3;

            Controls.Add(listBox);

            return listBox;
        }

        private MyGuiControlButton CreateButton(string text, Action<MyGuiControlButton> onClick)
        {
            MyGuiControlButton button = new MyGuiControlButton(
                position: new Vector2(m_buttonXOffset, m_currentPosition.Y),
                colorMask: Color.Yellow.ToVector4(),
                text: new StringBuilder(text),
                textScale: MyGuiConstants.DEFAULT_TEXT_SCALE*MyGuiConstants.DEBUG_BUTTON_TEXT_SCALE*m_scale,
                onButtonClick: onClick,
                visualStyle: MyGuiControlButtonStyleEnum.Rectangular);

            button.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
            button.Size = BUTTON_SIZE;

            Controls.Add(button);

            return button;
        }

        #endregion

        #region Position methods

        // Changes position and width of the control to fit the debug screen
        private void PositionControl(MyGuiControlBase control)
        {
            var totalWidth = SCREEN_SIZE.X - HIDDEN_PART_RIGHT - ITEM_HORIZONTAL_PADDING*2;
            // Store because some controls size gets changed when they are repositioned.
            var oldControlSize = control.Size;

            control.Position = new Vector2(m_currentPosition.X - SCREEN_SIZE.X/2 + ITEM_HORIZONTAL_PADDING,
                m_currentPosition.Y + ITEM_VERTICAL_PADDING);
            control.Size = new Vector2(totalWidth, oldControlSize.Y);

            m_currentPosition.Y += control.Size.Y + ITEM_VERTICAL_PADDING;
        }

        // Changes position of group of controls to fit in the debug screen side by side.
        private void PositionControls(MyGuiControlBase[] controls)
        {
            var totalWidth = SCREEN_SIZE.X - HIDDEN_PART_RIGHT - ITEM_HORIZONTAL_PADDING*2;
            var sizePerItem = totalWidth/controls.Length - 0.001f*controls.Length;
            var itemSizeWithPadding = sizePerItem + 0.001f*controls.Length;
            var maxHeight = 0f;

            for (var index = 0; index < controls.Length; index++)
            {
                var control = controls[index];

                control.Size = new Vector2(sizePerItem, control.Size.Y);
                control.PositionX = m_currentPosition.X + itemSizeWithPadding*index - SCREEN_SIZE.X/2 +
                                    ITEM_HORIZONTAL_PADDING;
                control.PositionY = (m_currentPosition.Y + ITEM_VERTICAL_PADDING);

                if (control.Size.Y > maxHeight)
                {
                    maxHeight = control.Size.Y;
                }
            }

            m_currentPosition.Y += maxHeight + ITEM_VERTICAL_PADDING;
        }

        #endregion
    }
}
