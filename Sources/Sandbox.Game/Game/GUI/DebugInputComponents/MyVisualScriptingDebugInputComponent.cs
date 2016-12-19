using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.SessionComponents;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRageRender;
using Sandbox.Game.Entities.Inventory;

namespace Sandbox.Game.GUI.DebugInputComponents
{
    public class MyVisualScriptingDebugInputComponent : MyDebugComponent
    {
        private List<MyAreaTriggerComponent> m_queriedTriggers = new List<MyAreaTriggerComponent>();
        private MyAreaTriggerComponent m_selectedTrigger;
        private MatrixD m_lastCapturedCameraMatrix;

        public MyVisualScriptingDebugInputComponent()
        {
            AddSwitch(MyKeys.NumPad0, keys => ToggleDebugDraw(), new MyRef<bool>(() => MyDebugDrawSettings.ENABLE_DEBUG_DRAW && MyDebugDrawSettings.DEBUG_DRAW_UPDATE_TRIGGER, null), "Debug Draw");
            AddShortcut(MyKeys.NumPad1, true, false, false, false, () => "Trigger: Attach new to entity", TryPutTriggerOnEntity);
            AddShortcut(MyKeys.NumPad2, true, false, false, false, () => "Trigger: Snap to position", SnapTriggerToPosition);
            AddShortcut(MyKeys.NumPad2, true, true, false, false, () => "Trigger: Snap to triggers position", SnapToTriggersPosition);
            AddShortcut(MyKeys.NumPad3, true, false, false, false, () => "Spawn Trigger", SpawnTrigger);
            AddShortcut(MyKeys.NumPad4, true, false, false, false, () => "Naming: FatBlock/Floating Object", TryNamingAnBlockOrFloatingObject);
            AddShortcut(MyKeys.NumPad5, true, false, false, false, () => "Trigger: Select", SelectTrigger);
            AddShortcut(MyKeys.NumPad6, true, false, false, false, () => "Naming: Grid", TryNamingAGrid);
            AddShortcut(MyKeys.NumPad7, true, false, false, false, () => "Delete trigger", DeleteTrigger);
            AddShortcut(MyKeys.NumPad8, true, false, false, false, () => "Trigger: Set Size", SetTriggerSize);
            AddShortcut(MyKeys.NumPad9, true, false, false, false, () => "Reset missions + run GameStarted", ResetMissionsAndRunGameStarted);
            AddShortcut(MyKeys.Add, true, false, false, false, () => "Trigger: Enlarge", () => ResizeATrigger(true));
            AddShortcut(MyKeys.Subtract, true, false, false, false, () => "Trigger: Shrink", () => ResizeATrigger(false));
            AddShortcut(MyKeys.Multiply, true, false, false, false, () => "Trigger: Rename", RenameTrigger);
            AddShortcut(MyKeys.T, true, true, false, false, () => "Copy camera data", CopyCameraDataToClipboard);
            AddShortcut(MyKeys.N, true, true, false, false, () => "Spawn empty entity", SpawnEntityDebug);
            AddShortcut(MyKeys.B, true, true, false, false, () => "Reload Screen", ReloadScreen);

            m_lastCapturedCameraMatrix = MatrixD.Identity;
        }

        private bool ReloadScreen()
        {
            MyScreenManager.CloseScreen(typeof(MyGuiScreenNewGame));
            MyScreenManager.AddScreen(new MyGuiScreenNewGame());
            return true;
        }

        private bool ResetMissionsAndRunGameStarted()
        {
            var component = MySession.Static.GetComponent<MyVisualScriptManagerSessionComponent>();
            if(component != null)
                component.Reset();

            return true;
        }

        private bool CopyCameraDataToClipboard()
        {
            var matrix = MyAPIGateway.Session.Camera.WorldMatrix;
            var text =  "Position:  " + matrix.Translation + "\n" +
                        "Direction: " + matrix.Forward + "\n" + 
                        "Up:        " + matrix.Up;

            Thread thread = new Thread(() => Clipboard.SetText(text));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            m_lastCapturedCameraMatrix = new MatrixD(matrix);

            return true;
        }

        public override string GetName()
        {
            return "Visual Scripting";
        }

        public override void Update10()
        {
            base.Update10();

            if (MyAPIGateway.Session == null) return;

            m_queriedTriggers.Clear();
            var allContainingTriggers = MySessionComponentTriggerSystem.Static.GetIntersectingTriggers(MyAPIGateway.Session.Camera.Position);
            foreach (var trigger in allContainingTriggers)
            {
                var scriptTrigger = trigger as MyAreaTriggerComponent;
                if(scriptTrigger != null)
                    m_queriedTriggers.Add(scriptTrigger);
            }
        }

        private bool RenameTrigger()
        {
            if(m_selectedTrigger == null)
                return false;

            var dialog = new ValueGetScreenWithCaption("Rename Dialog", m_selectedTrigger.Name, text =>
            {
                m_selectedTrigger.Name = text;
                return true;
            });
            MyGuiSandbox.AddScreen(dialog);

            return true;
        }

        private bool SelectTrigger()
        {
            var position = MyAPIGateway.Session.Camera.Position;
            var minLength = double.MaxValue;

            if(m_selectedTrigger != null)
                m_selectedTrigger.CustomDebugColor = Color.Red;

            foreach (var trigger in m_queriedTriggers)
            {
                var length = (trigger.Center - position).LengthSquared();
                if(length < minLength)
                {
                    minLength = length;
                    m_selectedTrigger = trigger;
                }
            }

            if(Math.Abs(minLength - double.MaxValue) < double.Epsilon)
                m_selectedTrigger = null;

            if(m_selectedTrigger != null)
                m_selectedTrigger.CustomDebugColor = Color.Yellow;

            return true;
        }

        private bool SnapToTriggersPosition()
        {
            if(m_selectedTrigger == null) return true;

            if(MySession.Static.ControlledEntity is MyCharacter)
                MySession.Static.LocalCharacter.PositionComp.SetPosition(m_selectedTrigger.Center);

            return true;
        }

        private bool SnapTriggerToPosition()
        {
            if(m_selectedTrigger == null)
                return false;


            if(MyAPIGateway.Session.CameraController is MySpectatorCameraController)
                m_selectedTrigger.Center = MyAPIGateway.Session.Camera.Position;
            else
                m_selectedTrigger.Center = MyAPIGateway.Session.LocalHumanPlayer.GetPosition();

            return true;
        }

        public bool SetTriggerSize()
        {
            if(m_selectedTrigger == null)
                return false;

            var dialog = new ValueGetScreenWithCaption("Set trigger size dialog", m_selectedTrigger.Radius.ToString(CultureInfo.InvariantCulture), delegate(string text)
            {
                float value;
                if(!float.TryParse(text, out value))
                    return false;

                m_selectedTrigger.Radius = value;

                return true;
            });
            MyGuiSandbox.AddScreen(dialog);


            return true;
        }

        public bool DeleteTrigger()
        {
            if(m_selectedTrigger == null)
                return false;

            if(m_selectedTrigger.Entity.DisplayName == "TriggerHolder")
                m_selectedTrigger.Entity.Close();
            else
            {
                m_selectedTrigger.Entity.Components.Remove(typeof(MyAreaTriggerComponent), m_selectedTrigger);
            }

            m_selectedTrigger = null;

            return true;
        }

        public bool ResizeATrigger(bool enlarge)
        {
            if (m_selectedTrigger == null)
                return false;

            m_selectedTrigger.Radius = enlarge ? m_selectedTrigger.Radius + 0.2 : m_selectedTrigger.Radius - 0.2;

            return true;
        }

        public override void Draw()
        {
            base.Draw();

            if (MyDebugDrawSettings.DEBUG_DRAW_UPDATE_TRIGGER)
            {
                Vector2 pos = new Vector2(350, 10);
                StringBuilder sb = new StringBuilder();

                MyRenderProxy.DebugDrawText2D(pos, "Queried Triggers", Color.White, 0.7f);
                foreach (var trigger in m_queriedTriggers)
                {
                    pos.Y += 20;
                    sb.Clear();
                    if(trigger.Entity != null && trigger.Entity.Name != null)
                        sb.Append("EntityName: " + trigger.Entity.Name + " ");

                    sb.Append("Trigger: " + trigger.Name + " radius: " + trigger.Radius);
                    MyRenderProxy.DebugDrawText2D(pos, sb.ToString(), Color.White, 0.7f);
                }

                pos.X += 250;
                pos.Y = 10;
                MyRenderProxy.DebugDrawText2D(pos, "Selected Trigger", Color.White, 0.7f);
                pos.Y += 20;
                sb.Clear();
                if(m_selectedTrigger != null)
                {
                    if (m_selectedTrigger.Entity != null && m_selectedTrigger.Entity.Name != null)
                        sb.Append("EntityName: " + m_selectedTrigger.Entity.Name + " ");

                    sb.Append("Trigger: " + m_selectedTrigger.Name + " radius: " + m_selectedTrigger.Radius);
                    MyRenderProxy.DebugDrawText2D(pos, sb.ToString(), Color.White, 0.7f);
                }

                if (m_lastCapturedCameraMatrix != MatrixD.Identity)
                {
                    MyRenderProxy.DebugDrawAxis(m_lastCapturedCameraMatrix, 5f, true);
                }
            }
        }

        public bool ToggleDebugDraw()
        {
            if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
            {
                MyDebugDrawSettings.ENABLE_DEBUG_DRAW = false;
                MyDebugDrawSettings.DEBUG_DRAW_UPDATE_TRIGGER = false;
            }
            else
            {
                MyDebugDrawSettings.ENABLE_DEBUG_DRAW = true;
                MyDebugDrawSettings.DEBUG_DRAW_UPDATE_TRIGGER = true;
            }

            return true;
        }

        public bool SpawnTrigger()
        {
            var dialog = new ValueGetScreenWithCaption("Spawn new Trigger", "", delegate(string text)
            {
                var trigger = new MyAreaTriggerComponent(text);
                var entity = new MyEntity();
                trigger.Radius = 2;
                trigger.Center = MyAPIGateway.Session.Camera.Position;
                entity.PositionComp.SetPosition(MyAPIGateway.Session.Camera.Position);
                entity.PositionComp.LocalVolume = new BoundingSphere(Vector3.Zero, 0.5f);
                entity.EntityId = MyEntityIdentifier.AllocateId();
                entity.Components.Remove<MyPhysicsComponentBase>();
                entity.Components.Remove<MyRenderComponentBase>();
                entity.DisplayName = "TriggerHolder";
                MyEntities.Add(entity);

                if (!entity.Components.Contains(typeof(MyTriggerAggregate)))
                    entity.Components.Add(typeof(MyTriggerAggregate), new MyTriggerAggregate());
                entity.Components.Get<MyTriggerAggregate>().AddComponent(trigger);


                if(m_selectedTrigger != null)
                    m_selectedTrigger.CustomDebugColor = Color.Red;;

                m_selectedTrigger = trigger;
                m_selectedTrigger.CustomDebugColor = Color.Yellow;
                return true;
            });
            MyGuiSandbox.AddScreen(dialog);

            return true;
        }


        public bool SpawnEntityDebug()
        {
            SpawnEntity(null);
            return true;
        }

        public static MyEntity SpawnEntity(Action<MyEntity> onEntity)
        {
            var dialog = new ValueGetScreenWithCaption("Spawn new Entity", "", delegate(string text)
            {
                var entity = new MyEntity();
                entity.WorldMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
                entity.PositionComp.SetPosition(MyAPIGateway.Session.Camera.Position);
                entity.EntityId = MyEntityIdentifier.AllocateId();
                entity.Components.Remove<MyPhysicsComponentBase>();
                entity.Components.Remove<MyRenderComponentBase>();
                entity.DisplayName = "EmptyEntity";
                MyEntities.Add(entity);
                entity.Name = text;
                MyEntities.SetEntityName(entity, true);

                if (onEntity != null)
                    onEntity(entity);

                return true;
            });
            MyGuiSandbox.AddScreen(dialog);

            return null;
        }

        private bool TryPutTriggerOnEntity()
        {
            var worldMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            List<MyPhysics.HitInfo> hits = new List<MyPhysics.HitInfo>();
            MyPhysics.CastRay(worldMatrix.Translation, worldMatrix.Translation + worldMatrix.Forward * 30, hits, 15);

            foreach (var hitInfo in hits)
            {
                var body = (MyPhysicsBody)hitInfo.HkHitInfo.Body.UserObject;
                if (body.Entity is MyCubeGrid)
                {
                    var rayEntity = (MyEntity)body.Entity;
                    var dialog = new ValueGetScreenWithCaption("Entity Spawn on: " + rayEntity.DisplayName, "", delegate(string text)
                    {
                        if (m_selectedTrigger != null)
                            m_selectedTrigger.CustomDebugColor = Color.Red;

                        m_selectedTrigger = new MyAreaTriggerComponent(text);
                        if (!rayEntity.Components.Contains(typeof(MyTriggerAggregate)))
                            rayEntity.Components.Add(typeof(MyTriggerAggregate), new MyTriggerAggregate());
                        rayEntity.Components.Get<MyTriggerAggregate>().AddComponent(m_selectedTrigger);
                        m_selectedTrigger.Center = MyAPIGateway.Session.Camera.Position;
                        m_selectedTrigger.Radius = 2;

                        m_selectedTrigger.CustomDebugColor = Color.Yellow;

                        return true;
                    });
                    MyGuiSandbox.AddScreen(dialog);
                    return true;
                }
            }
            return false;
        }

        private void NameDialog(MyEntity entity)
        {
            var dialog = new ValueGetScreenWithCaption("Name a Grid dialog: " + entity.DisplayName, entity.Name ?? entity.DisplayName + " has no name.", delegate(string text)
            {
                MyEntity foundEntity;
                if (MyEntities.TryGetEntityByName(text, out foundEntity))
                {
                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                        buttonType: MyMessageBoxButtonsType.OK,
                        messageText: new StringBuilder("Entity with same name already exits, please enter different name."),
                        messageCaption: new StringBuilder("Naming error")));
                }
                else
                {
                    entity.Name = text;
                    MyEntities.SetEntityName(entity, true);

                    return true;
                }

                return false;
            });
            MyGuiSandbox.AddScreen(dialog);
        }

        public bool TryNamingAGrid()
        {
            var worldMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            List<MyPhysics.HitInfo> hits = new List<MyPhysics.HitInfo>();
            MyPhysics.CastRay(worldMatrix.Translation, worldMatrix.Translation + worldMatrix.Forward * 5, hits, 15);

            foreach (var hitInfo in hits)
            {
                var entity = (MyEntity)hitInfo.HkHitInfo.GetHitEntity();
                if (entity is MyCubeGrid)
                {
                    NameDialog(entity);
                    return true;
                }
            }
            return false;
        }

        public bool TryNamingAnBlockOrFloatingObject()
        {
            var worldMatrix = MyAPIGateway.Session.Camera.WorldMatrix; // most accurate for player view.
            var position = worldMatrix.Translation + worldMatrix.Forward * 0.5f;
            var ray = new RayD(position, worldMatrix.Forward * 1000);

            var boundingSphere = new BoundingSphereD(worldMatrix.Translation, 30);
            var entites = MyEntities.GetEntitiesInSphere(ref boundingSphere);

            List<MyPhysics.HitInfo> hits = new List<MyPhysics.HitInfo>();
            MyPhysics.CastRay(worldMatrix.Translation, worldMatrix.Translation + worldMatrix.Forward * 5, hits, 15);

            foreach (var hitInfo in hits)
            {
                var body = (MyPhysicsBody)hitInfo.HkHitInfo.Body.UserObject;
                if (body.Entity is MyFloatingObject)
                {
                    var rayEntity = (MyEntity) body.Entity;
                    NameDialog(rayEntity);
                    return true;
                }
            }

            foreach (var entity in entites)
            {
                var cubeGrid = entity as MyCubeGrid;

                if (cubeGrid != null && ray.Intersects(entity.PositionComp.WorldAABB).HasValue)
                {
                    var hit = cubeGrid.RayCastBlocks(worldMatrix.Translation, worldMatrix.Translation + worldMatrix.Forward * 100);
                    if (hit.HasValue)
                    {
                        var block = cubeGrid.GetCubeBlock(hit.Value);
                        
                        if(block.FatBlock != null)
                        {
                            var dialog = new ValueGetScreenWithCaption("Name block dialog: " + block.FatBlock.DefinitionDisplayNameText, block.FatBlock.Name ?? block.FatBlock.DefinitionDisplayNameText + " has no name.", delegate(string text)
                            {
                                MyEntity foundEntity;
                                if (MyEntities.TryGetEntityByName(text, out foundEntity))
                                {
                                    MyGuiSandbox.AddScreen(MyGuiSandbox.CreateMessageBox(
                                        buttonType: MyMessageBoxButtonsType.OK,
                                        messageText: new StringBuilder("Entity with same name already exits, please enter different name."),
                                        messageCaption: new StringBuilder("Naming error")));
                                }
                                else
                                {
                                    block.FatBlock.Name = text;
                                    MyEntities.SetEntityName(block.FatBlock, true);

                                    return true;
                                }

                                return false;
                            }); 
                            MyGuiSandbox.AddScreen(dialog);
                            entites.Clear();
                            return true;
                        }
                    }
                }
            }

            entites.Clear();
            return false;
        }
    }

    public class ValueGetScreenWithCaption : MyGuiScreenBase
    {
        public delegate bool ValueGetScreenAction(string valueText);

        MyGuiControlTextbox m_nameTextbox;
        MyGuiControlButton m_confirmButton;
        MyGuiControlButton m_cancelButton;

        string m_title;
        string m_caption;
        ValueGetScreenAction m_acceptCallback;

        public ValueGetScreenWithCaption(string title, string caption, ValueGetScreenAction ValueAcceptedCallback) :
            base(new Vector2(0.5f, 0.5f), MyGuiConstants.SCREEN_BACKGROUND_COLOR, null)
        {
            Debug.Assert(ValueAcceptedCallback != null);
            m_acceptCallback = ValueAcceptedCallback;
            m_title = title;
            m_caption = caption;
            m_canShareInput = false;
            m_isTopMostScreen = true;
            m_isTopScreen = true;

            CanHideOthers = false;
            EnabledBackgroundFade = true;
            RecreateControls(true);
        }

        public override string GetFriendlyName()
        {
            return "ValueGetScreenWithCaption";
        }

        public override void RecreateControls(bool contructor)
        {
            base.RecreateControls(contructor);

            this.Controls.Add(new MyGuiControlLabel(new Vector2(0.0f, -0.10f), text: m_title, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER));
            m_nameTextbox = new MyGuiControlTextbox(new Vector2(0.0f, 0.0f), m_caption);
            m_confirmButton = new MyGuiControlButton(new Vector2(0.21f, 0.10f), MyGuiControlButtonStyleEnum.Default, new Vector2(0.2f, 0.05f), null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, new StringBuilder("Confirm"));
            m_cancelButton = new MyGuiControlButton(new Vector2(-0.21f, 0.10f), MyGuiControlButtonStyleEnum.Default, new Vector2(0.2f, 0.05f), null, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER, null, new StringBuilder("Cancel"));

            this.Controls.Add(m_nameTextbox);
            this.Controls.Add(m_confirmButton);
            this.Controls.Add(m_cancelButton);

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
            if(m_acceptCallback(m_nameTextbox.Text))
                CloseScreen();
        }

        void cancelButton_OnButtonClick(MyGuiControlButton sender)
        {
            CloseScreen();
        }
    }
}
