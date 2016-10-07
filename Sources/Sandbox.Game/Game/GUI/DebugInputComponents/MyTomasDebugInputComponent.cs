#region Using

using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Input;
using VRage.Utils;
using VRageMath;


#endregion

namespace Sandbox.Game.Gui
{
    public class MyTomasInputComponent : MyDebugComponent
    {
        public static float USE_WHEEL_ANIMATION_SPEED = 1f;

        private long m_previousSpectatorGridId = 0;

        public static string ClipboardText = string.Empty;

        public override string GetName()
        {
            return "Tomas";
        }

        public MyTomasInputComponent()
        {
            AddShortcut(MyKeys.Delete, true, true, false, false,
               () => "Delete all characters",
               delegate
               {
                   foreach (var obj in MyEntities.GetEntities().OfType<MyCharacter>())
                   {
                       if (obj == MySession.Static.ControlledEntity)
                       {
                           MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator);
                       }
                       obj.Close();
                   }

                   foreach (var obj in MyEntities.GetEntities().OfType<MyCubeGrid>())
                   {
                       foreach (var obj2 in obj.GetBlocks())
                       {
                           if (obj2.FatBlock is MyCockpit)
                           {
                               var cockpit = obj2.FatBlock as MyCockpit;
                               if (cockpit.Pilot != null)
                               {
                                   cockpit.Pilot.Close();
                               }
                           }
                       }
                   }
                   return true;
               });

            AddShortcut(MyKeys.NumPad4, true, false, false, false,
               () => "Spawn cargo ship or barbarians",
               delegate
               {
                   var theEvent = MyGlobalEvents.GetEventById(new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventBase), "SpawnCargoShip"));
                   if (theEvent == null)
                       theEvent = MyGlobalEvents.GetEventById(new MyDefinitionId(typeof(MyObjectBuilder_GlobalEventBase), "SpawnBarbarians"));
                   if (theEvent != null)
                   {
                       MyGlobalEvents.RemoveGlobalEvent(theEvent);
                       theEvent.SetActivationTime(TimeSpan.FromSeconds(1));
                       MyGlobalEvents.AddGlobalEvent(theEvent);
                   }
                   return true;
               });

            AddShortcut(MyKeys.NumPad5, true, false, false, false,
              () => "Spawn random meteor",
              delegate
              {
                  var camera = MySector.MainCamera;
                  var target = camera.Position + MySector.MainCamera.ForwardVector * 20.0f;
                  var spawnPosition = target + MySector.DirectionToSunNormalized * 1000.0f;
                  MyMeteor.SpawnRandom(spawnPosition, -MySector.DirectionToSunNormalized);
                  return true;
              });

            AddShortcut(MyKeys.NumPad8, true, false, false, false,
            () => "Switch control to next entity",
            delegate
            {
                if (MySession.Static.ControlledEntity != null)
                { //we already are controlling this object

                    var cameraController = MySession.Static.GetCameraControllerEnum();
                    if (cameraController != MyCameraControllerEnum.Entity && cameraController != MyCameraControllerEnum.ThirdPersonSpectator)
                    {
                        MySession.Static.SetCameraController(MyCameraControllerEnum.Entity, MySession.Static.ControlledEntity.Entity);
                    }
                    else
                    {
                        var entities = MyEntities.GetEntities().ToList();
                        int lastKnownIndex = entities.IndexOf(MySession.Static.ControlledEntity.Entity);

                        var entitiesList = new List<MyEntity>();
                        if (lastKnownIndex + 1 < entities.Count)
                            entitiesList.AddRange(entities.GetRange(lastKnownIndex + 1, entities.Count - lastKnownIndex - 1));

                        if (lastKnownIndex != -1)
                        {
                            entitiesList.AddRange(entities.GetRange(0, lastKnownIndex + 1));
                        }

                        MyCharacter newControlledObject = null;

                        for (int i = 0; i < entitiesList.Count; i++)
                        {
                            var character = entitiesList[i] as MyCharacter;
                            if (character != null)
                            {
                                newControlledObject = character;
                                break;
                            }
                        }

                        if (newControlledObject != null)
                        {
                            MySession.Static.LocalHumanPlayer.Controller.TakeControl(newControlledObject);
                        }
                    }
                }

                return true;
            });


            AddShortcut(MyKeys.NumPad7, true, false, false, false,
            () => "Use next ship",
            delegate
            {
                MyCharacterInputComponent.UseNextShip();
                return true;
            });

            AddShortcut(MyKeys.NumPad9, true, false, false, false,
          () => "Debug new grid screen",
          delegate
          {
              MyGuiSandbox.AddScreen(new DebugNewGridScreen());
              return true;
          });

            AddShortcut(MyKeys.N, true, false, false, false,
       () => "Refill all batteries",
       delegate
       {
           foreach (var entity in MyEntities.GetEntities())
           {
               MyCubeGrid grid = entity as MyCubeGrid;
               if (grid != null)
               {
                   foreach (var block in grid.GetBlocks())
                   {
                       MyBatteryBlock battery = block.FatBlock as MyBatteryBlock;
                       if (battery != null)
                       {
                           battery.CurrentStoredPower = battery.MaxStoredPower;
                       }
                   }
               }
           }
           return true;
       });

            AddShortcut(MyKeys.U, true, false, false, false,
        () => "Spawn new character",
        delegate
        {
            var character = MyCharacterInputComponent.SpawnCharacter();
            return true;
        });


            AddShortcut(MyKeys.NumPad2, true, false, false, false,
               () => "Merge static grids",
               delegate
               {
                   // Try to merge all static large grids
                   HashSet<MyCubeGrid> ignoredGrids = new HashSet<MyCubeGrid>();
                   while (true)
                   {
                       // Flag that we need new entities enumeration
                       bool needNewEntitites = false;

                       foreach (var entity in MyEntities.GetEntities())
                       {
                           MyCubeGrid grid = entity as MyCubeGrid;
                           if (grid != null && grid.IsStatic && grid.GridSizeEnum == MyCubeSize.Large)
                           {
                               if (ignoredGrids.Contains(grid))
                                   continue;

                               List<MySlimBlock> blocks = grid.GetBlocks().ToList();
                               foreach (var block in blocks)
                               {
                                   var mergedGrid = grid.DetectMerge(block);
                                   if (mergedGrid == null)
                                       continue;

                                   needNewEntitites = true;
                                   // Grid merged to other grid? Then break and loop all entities again.
                                   if (mergedGrid != grid)
                                       break;
                               }

                               if (!needNewEntitites)
                                   ignoredGrids.Add(grid);
                           }

                           if (needNewEntitites)
                               break;
                       }

                       if (!needNewEntitites)
                           break;
                   }
                   return true;
               });

            AddShortcut(MyKeys.Add, true, false, false, false,
                () => "Increase wheel animation speed",
                delegate
                {
                    USE_WHEEL_ANIMATION_SPEED += 0.05f;
                    return true;
                });

            AddShortcut(MyKeys.Subtract, true, false, false, false,
                () => "Decrease wheel animation speed",
                delegate
                {
                    USE_WHEEL_ANIMATION_SPEED -= 0.05f;
                    return true;
                });

            AddShortcut(MyKeys.Divide, true, false, false, false,
                () => "Show model texture names",
                delegate
                {
                    MyFakes.ENABLE_DEBUG_DRAW_TEXTURE_NAMES = !MyFakes.ENABLE_DEBUG_DRAW_TEXTURE_NAMES;
                    return true;
                });

            AddShortcut(MyKeys.NumPad1, true, false, false, false,
                () => "Throw from spectator: " + Sandbox.Game.Components.MySessionComponentThrower.USE_SPECTATOR_FOR_THROW,
                delegate
                {
                    Sandbox.Game.Components.MySessionComponentThrower.USE_SPECTATOR_FOR_THROW = !Sandbox.Game.Components.MySessionComponentThrower.USE_SPECTATOR_FOR_THROW;
                    return true;
                });

            AddShortcut(MyKeys.F2, true, false, false, false, () => "Spectator to next small grid", () => SpectatorToNextGrid(MyCubeSize.Small));
            AddShortcut(MyKeys.F3, true, false, false, false, () => "Spectator to next large grid", () => SpectatorToNextGrid(MyCubeSize.Large));

            AddShortcut(MyKeys.Multiply, true, false, false, false,
                () => "Show model texture names",
                CopyAssetToClipboard
                );
        }

        private bool CopyAssetToClipboard()
        {
            // DUE TO THREADING APPARTMENT REQUIREMENTS FOR WINDOWS.FORMS.CLIPLBOARD MUST BE RUN IN STA MODE
            System.Threading.Thread clipboardThread = new System.Threading.Thread(new System.Threading.ThreadStart(TextToClipboard));
            clipboardThread.ApartmentState = System.Threading.ApartmentState.STA;
            clipboardThread.Start();

            return true;
        }

        private void TextToClipboard()
        {
            if (ClipboardText != null && ClipboardText != String.Empty)
            {
#if !XB1
                System.Windows.Forms.Clipboard.SetText(ClipboardText);
#else
                System.Diagnostics.Debug.Assert(false, "Not Clipboard support on XB1!");
#endif
            }
        }

        public override bool HandleInput()
        {
            if (MySession.Static == null)
                return false;

            if (base.HandleInput())
                return true;

            bool handled = false;

            return handled;
        }

        public bool SpectatorToNextGrid(MyCubeSize size)
        {
            MyCubeGrid nextGrid = null;
            MyCubeGrid prevGrid = null;

            foreach (var entity in MyEntities.GetEntities())
            {
                MyCubeGrid grid = entity as MyCubeGrid;
                if (grid != null && grid.GridSizeEnum == size)
                {
                    if (m_previousSpectatorGridId == 0)
                    {
                        nextGrid = grid;
                        break;
                    }

                    if (prevGrid != null)
                    {
                        nextGrid = grid;
                        break;
                    }

                    if (grid.EntityId == m_previousSpectatorGridId)
                    {
                        prevGrid = grid;
                    }

                    if (nextGrid == null)
                    {
                        nextGrid = grid;
                    }
                }
            }

            if (nextGrid == null)
            {
                return false;
            }

            BoundingSphere bSphere = nextGrid.PositionComp.WorldVolume;
            Vector3D rotatedFW = Vector3D.Transform(Vector3D.Forward, MySpectator.Static.Orientation);
            MySpectator.Static.Position = nextGrid.PositionComp.GetPosition() - rotatedFW * bSphere.Radius * 2;
            m_previousSpectatorGridId = nextGrid.EntityId;
            return true;
        }

        void LobbyFound(Empty e, Result result)
        {
            for (uint i = 0; i < LobbySearch.LobbyCount; i++)
            {
                var id = LobbySearch.GetLobbyByIndex(i);
                MyHud.Notifications.Add(new MyHudNotificationDebug(String.Format("Lobby found {0}, player count {1}", id.LobbyId, id.MemberCount), 3000));
                id.Leave();
            }
        }

        class DebugNewGridScreen : MyGuiScreenBase
        {

            private MyGuiControlCombobox m_sizeCombobox;
            private MyGuiControlCheckbox m_staticCheckbox;

            public override string GetFriendlyName()
            {
                return "DebugNewGridScreen";
            }

            public DebugNewGridScreen() :
                base()
            {
                EnabledBackgroundFade = true;
                RecreateControls(true);
            }

            public override void RecreateControls(bool constructor)
            {
                base.RecreateControls(constructor);

                m_sizeCombobox = new MyGuiControlCombobox()
                {
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER,
                    Position = Vector2.Zero,
                };
                foreach (var val in typeof(MyCubeSize).GetEnumValues())
                {
                    m_sizeCombobox.AddItem((int)(MyCubeSize)val, new StringBuilder(val.ToString()));
                }
                m_sizeCombobox.SelectItemByKey((int)MyCubeSize.Large);

                m_staticCheckbox = new MyGuiControlCheckbox()
                {
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                    IsChecked = true,
                };
                var staticLabel = new MyGuiControlLabel()
                {
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER,
                    Position = new Vector2(m_staticCheckbox.Size.X, 0f),
                    Text = "Static grid"
                };

                var okButton = new MyGuiControlButton()
                {
                    OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP,
                    Text = "Ok",
                    Position = new Vector2(0f, 0.05f),
                };
                okButton.ButtonClicked += okButton_ButtonClicked;

                Elements.Add(m_sizeCombobox);
                Elements.Add(m_staticCheckbox);
                Elements.Add(staticLabel);
                Elements.Add(okButton);

            }

            void okButton_ButtonClicked(MyGuiControlButton obj)
            {
                MyCubeBuilder.Static.StartStaticGridPlacement((MyCubeSize)m_sizeCombobox.GetSelectedKey(), m_staticCheckbox.IsChecked);
                CloseScreen();
            }
        }

    }
}
