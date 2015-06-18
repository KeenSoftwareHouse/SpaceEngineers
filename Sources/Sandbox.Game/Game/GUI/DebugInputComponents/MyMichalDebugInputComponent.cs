using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.AI;
using Sandbox.Game.AI.BehaviorTree;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.VoiceChat;
using Sandbox.Game.World;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sandbox.Game.Gui
{
    [PreloadRequired]
    public class MyMichalDebugInputComponent : MyDebugComponent
    {
        public static MyMichalDebugInputComponent Static { get; private set; }

        static MyMichalDebugInputComponent()
        {
        }

        public MyMichalDebugInputComponent()
        {
            Static = this;

            Axes = new List<MyJoystickAxesEnum>();
            AxesCollection = new Dictionary<MyJoystickAxesEnum, float?>();
            foreach (var axis in Enum.GetValues(typeof(MyJoystickAxesEnum)))
            {
                MyJoystickAxesEnum val = (MyJoystickAxesEnum)axis;
                AxesCollection[val] = null;
                Axes.Add(val);
            }

            AddShortcut(MyKeys.NumPad7, true, false, false, false, () => "Spawn flora LMB: " + SPAWN_FLORA_ENTITY, () => { SPAWN_FLORA_ENTITY = !SPAWN_FLORA_ENTITY; return true; });

            AddShortcut(MyKeys.NumPad0, true, false, false, false, () => "Debug draw", DebugDrawFunc);

            AddShortcut(MyKeys.NumPad9, true, false, false, false, OnRecording, ToggleVoiceChat);

            if (MyPerGameSettings.Game == GameEnum.SE_GAME)
            {
                AddShortcut(MyKeys.NumPad1, true, false, false, false, () => "Remove grids with space balls", RemoveGridsWithSpaceBallsFunc);
                AddShortcut(MyKeys.NumPad2, true, false, false, false, () => "Throw 50 ores and 50 scrap metals", ThrowFloatingObjectsFunc);
            }

            if (MyPerGameSettings.EnableAi)
            {
                AddShortcut(MyKeys.NumPad6, true, false, false, false, () => "Next head matrix", NextHeadMatrix);
                AddShortcut(MyKeys.NumPad5, true, false, false, false, () => "Previous head matrix", PreviousHeadMatrix);
                AddShortcut(MyKeys.NumPad3, true, false, false, false, OnSelectBotForDebugMsg, () => { OnSelectDebugBot = !OnSelectDebugBot; return true; });
                AddShortcut(MyKeys.NumPad4, true, false, false, false, () => "Remove bot", () => { MyAIComponent.Static.DebugRemoveFirstBot(); return true; });
           //     AddShortcut(MyKeys.NumPad7, true, false, false, false, () => { return "DEBUG ANIMALS " + (MyDebugDrawSettings.DEBUG_DRAW_ANIMALS ? "TRUE" : "FALSE"); }, () => { MyDebugDrawSettings.DEBUG_DRAW_ANIMALS = !MyDebugDrawSettings.DEBUG_DRAW_ANIMALS; return true; });
            }
        }

        public override string GetName()
        {
            return "Michal";
        }

        public int DebugPacketCount = 0;
        public int CurrentQueuedBytes = 0;
        public bool Reliable = true;
        public bool DebugDraw = false;
        public bool CustomGridCreation = false;

        public IMyBot SelectedBot = null;
        public int BotPointer = 0;
        public int SelectedTreeIndex = 0;
        public MyBehaviorTree SelectedTree = null;
        public int[] BotsIndexes = new int[0];

        private Dictionary<MyJoystickAxesEnum, float?> AxesCollection;
        private List<MyJoystickAxesEnum> Axes;

        public MatrixD HeadMatrix = MatrixD.Identity;
        private const int HeadMatrixFlag = 1 << 0 | 1 << 1 | 1 << 2 | 1 << 3;
        private int CurrentHeadMatrixFlag = 0;

        public bool SPAWN_FLORA_ENTITY = false;
        public bool OnSelectDebugBot = false;

        public override bool HandleInput()
        {
            //float newState = MyInput.Static.GetJoystickAxisStateForGameplay(axis);
            //float oldState = MyInput.Static.GetPreviousJoystickAxisStateForGameplay(axis);
            foreach (var axis in Axes)
            {
                if (MyInput.Static.IsJoystickAxisValid(axis))
                {
                    float val = MyInput.Static.GetJoystickAxisStateForGameplay(axis);
                    AxesCollection[axis] = val;
                }
                else
                {
                    AxesCollection[axis] = null;
                }
            }

            return base.HandleInput();
        }
    
        public override void Draw()
        {
            base.Draw();

#region DebugDrawOld
            //if (DebugDraw)
            //{
            //    string formated = string.Format("Num of packets to send: {0}", DebugPacketCount);

            //    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 0.0f), formated, Color.Red, 0.5f);
            //    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 10.0f), "Packet type: " + (Reliable ? "Reliable" : "Unreliable"), Color.Red, 0.5f);

            //    if (MySession.LocalCharacter != null)
            //    {
            //        var entities = MyEntities.GetEntities();
            //        foreach (var entity in entities)
            //        {
            //            if (!entity.Closed && entity.Physics != null)
            //            {
            //                var worldAABB = entity.PositionComp.WorldAABB;
            //                VRageRender.MyRenderProxy.DebugDrawAABB(entity.PositionComp.WorldAABB, Color.Aqua.ToVector3(), 1.0f, 1.0f, true);
            //                VRageRender.MyRenderProxy.DebugDrawLine3D(worldAABB.Min, worldAABB.Max, Color.Ivory, Color.LawnGreen, true);
            //            }
            //        }

                    
            //        var cameraController = MySession.GetCameraControllerEnum();
            //        string parsedName = "Camera controller enum: " + cameraController.ToString() + " => " + (MySession.Static.CameraController == null ? "NULL" : MySession.Static.CameraController.ToString());
            //        var controlledEntity = "Controlled entity: " + (MySession.ControlledEntity == null ? "NULL" : MySession.ControlledEntity.ToString());
            //        string customCubeGridString = "Custom grid creation: " + (CustomGridCreation ? "TRUE" : "FALSE");
            //        VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 20.0f), parsedName, Color.Red, 0.5f);
            //        VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 30.0f), controlledEntity, Color.Red, 0.5f);
            //        VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 40.0f), customCubeGridString, Color.Red, 0.5f);

            //        if (MySession.LocalCharacter != null)
            //            VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 50.0f), "Character pos: " + MySession.LocalCharacter.PositionComp.GetPosition().ToString(), Color.Red, 0.5f);

            //        VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, 60.0f), "Number of floating objects: " + (MyFloatingObjects.FloatingOreCount + MyFloatingObjects.FloatingItemCount), Color.Red, 0.5f);
            //        //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(50, 10f), "Zero position: " + MyEntities

            //        //VRageRender.MyRenderProxy.DebugDrawAABB(new BoundingBoxD(new Vector3D(-1, -1, -1), new Vector3D(1, 1, 1)), Color.Red, 1.0f, 1.0f, false);
            //        VRageRender.MyRenderProxy.DebugDrawAxis(MatrixD.CreateTranslation(0, 0, 0), 2f, false);

            //        var q = from cubegrid in MyEntities.GetEntities().OfType<MyCubeGrid>()
            //                from spaceBall in cubegrid.CubeBlocks.Where(x => x.FatBlock is MySpaceBall)
            //                select spaceBall.FatBlock;

            //        foreach (var sb in q)
            //        {
            //            //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, i * 10 + 80.0f), body.HkHitInfo.Body.GetEntity().ToString(), Color.Red, 0.5f);
            //            VRageRender.MyRenderProxy.DebugDrawAxis(sb.PositionComp.WorldMatrix, 0.5f, true);
            //        }

            //        foreach (var obj in MyEntities.GetEntities().OfType<MyCubeGrid>())
            //        {
            //            if (obj.CubeBlocks.Count > 1)
            //            {
            //                foreach (var cb in obj.CubeBlocks)
            //                {
            //                    if (cb.Neighbours.Count <= 0)
            //                    {
            //                   //     VRageRender.MyRenderProxy.DebugDrawCapsule(new BoundingBoxD(cb.Min, cb.Max), Color.PaleGreen, 1.0f, 1.0f, false);
            //                        var ff = obj.GridIntegerToWorld(cb.Position);
            //                        VRageRender.MyRenderProxy.DebugDrawSphere(ff, 4, Color.Red, 1.0f, false);
            //                    }

            //                }
            //            }
            //        }

            //        if (MyAIComponent.Static != null && MyFakes.ENABLE_AI)
            //        {
            //            if (SelectedTree != null)
            //            {
            //            }
            //        }

            //        if (MySession.LocalCharacter != null)
            //        {
            //            var head = MySession.LocalCharacter.GetHeadMatrix(false);
            //            var headPos = head.Translation - (Vector3D)head.Forward * 0.3;
            //            var dir = head.Forward;
            //            var from = headPos;

            //            Vector3D to = from + dir * Sandbox.Engine.Utils.MyConstants.DEFAULT_INTERACTIVE_DISTANCE;


            ////VRageRender.MyRenderProxy.DebugDrawLine3D(from, to, Color.Red, Color.Green, true);
            ////VRageRender.MyRenderProxy.DebugDrawSphere(headPos, 0.05f, Color.Red.ToVector3(), 1.0f, false);
            //            List<Sandbox.Engine.Physics.MyPhysics.HitInfo> toList = new List<Engine.Physics.MyPhysics.HitInfo>();
            //            Sandbox.Engine.Physics.MyPhysics.CastRay(from, to, toList);

            //            for (int i = 0; i < toList.Count; i++)
            //            {
            //                var body = toList[i];
            //                VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.0f, i * 10 + 80.0f), body.HkHitInfo.Body.GetEntity().ToString(), Color.Red, 0.5f);

            //                VRageRender.MyRenderProxy.DebugDrawSphere(body.Position, 1, Color.Red, 1.0f, false);
            //            }
            //        }
            //    }    
            //}
#endregion
            if (DebugDraw)
            {
                if (MySession.LocalCharacter != null)
                {
                  //  VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(600, 20), "Character position: " + MySession.LocalCharacter.PositionComp.GetPosition().ToString(), Color.Red, 1.0f);

                    HeadMatrix = MySession.LocalCharacter.GetHeadMatrix((CurrentHeadMatrixFlag & 1) == 1, (CurrentHeadMatrixFlag & 2) == 2, (CurrentHeadMatrixFlag & 4) == 4, (CurrentHeadMatrixFlag & 8) == 8);
                    VRageRender.MyRenderProxy.DebugDrawAxis(HeadMatrix, 1, false);
                    

                    string getheadmatrixString = string.Format("GetHeadMatrix({0}, {1}, {2}, {3})", (CurrentHeadMatrixFlag & 1) == 1, (CurrentHeadMatrixFlag & 2) == 2, (CurrentHeadMatrixFlag & 4) == 4, (CurrentHeadMatrixFlag & 8) == 8);
                    VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(600, 20), getheadmatrixString, Color.Red, 1.0f);

                    var worldMat = MySession.LocalCharacter.WorldMatrix;
                    var forw = worldMat.Forward;
                    var angle = MathHelper.ToRadians(15);
                    var cosAngle = Math.Cos(angle);
                    var sinAngle = Math.Sin(angle);
                    MatrixD rotat = MatrixD.CreateRotationY(angle);
                    MatrixD invRotat = MatrixD.Transpose(rotat);

                    var mainForw = worldMat.Translation + worldMat.Forward;
                    var leftForw = worldMat.Translation + Vector3D.TransformNormal(worldMat.Forward, rotat);
                    var rightForw = worldMat.Translation + Vector3D.TransformNormal(worldMat.Forward, invRotat);
                    //leftForw.Normalize();
                    //rightForw.Normalize();
                    VRageRender.MyRenderProxy.DebugDrawLine3D(worldMat.Translation, mainForw, Color.Aqua, Color.Aqua, false);
                    VRageRender.MyRenderProxy.DebugDrawLine3D(worldMat.Translation, leftForw, Color.Red, Color.Red, false);
                    VRageRender.MyRenderProxy.DebugDrawLine3D(worldMat.Translation, rightForw, Color.Green, Color.Green, false);

                    
                    var toolbar = MyToolbarComponent.CurrentToolbar;
                    if (toolbar != null)
                    {
                        var safeRect = MyGuiManager.GetSafeGuiRectangle();
                        var initVec2 = new Vector2(safeRect.Right, safeRect.Top + safeRect.Height * 0.5f);
                    }
                }

                if (MyAIComponent.Static != null && MyAIComponent.Static.Bots != null)
                {
                    Vector2 initPos = new Vector2(10, 150);
                    var keys = MyAIComponent.Static.Bots.BotsDictionary.Keys;
                    BotsIndexes = new int[keys.Count];
                    keys.CopyTo(BotsIndexes, 0);

                    var entities = MyEntities.GetEntities();
                    foreach (var entity in entities)
                    {
                        if (entity is MyCubeGrid)
                        {
                            var cubegrid = entity as MyCubeGrid;
                            if (cubegrid.BlocksCount == 1)
                            {
                                var first = cubegrid.GetCubeBlock(new Vector3I(0, 0, 0));
                                if (first != null)
                                {
                                    VRageRender.MyRenderProxy.DebugDrawText3D(first.FatBlock.PositionComp.GetPosition(), first.BlockDefinition.Id.SubtypeName, Color.Aqua, 1.0f, false);
                                    VRageRender.MyRenderProxy.DebugDrawPoint(first.FatBlock.PositionComp.GetPosition(), Color.Aqua, false);
                                }
                            }
                        }
                    }
                    
                    //foreach (var pair in MyAIComponent.Static.Bots.BotsDictionary)
                    //{
                    //    if (pair.Value is IMyEntityBot)
                    //    {
                    //        var entityBot = pair.Value as IMyEntityBot;
                    //        Color labelColor = Color.Green;
                    //        if (pair.Value != SelectedBot)
                    //            labelColor = Color.Red;
                    //        VRageRender.MyRenderProxy.DebugDrawText2D(initPos, string.Format("Bot[{0}]: {1}", pair.Key, pair.Value.BehaviorName), labelColor, 0.5f, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
                    //        if (entityBot.BotEntity != null)
                    //        {
                    //            var markerPos = entityBot.BotEntity.PositionComp.WorldAABB.Center;
                    //            markerPos.Y += entityBot.BotEntity.PositionComp.WorldAABB.HalfExtents.Y;
                    //            VRageRender.MyRenderProxy.DebugDrawText3D(markerPos, string.Format("Bot:{0}", pair.Key), labelColor, 1f, false, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP);
                    //        }
                    //        initPos.Y += 20;
                    //    }
                    //}
                }

                if (m_lineStart.HasValue && m_lineEnd.HasValue)
                {
                    VRageRender.MyRenderProxy.DebugDrawLine3D(m_lineStart.Value, m_lineEnd.Value, Color.Red, Color.Green, true);
                }

                if (m_sphereCen.HasValue && m_rad.HasValue)
                {
                    VRageRender.MyRenderProxy.DebugDrawSphere(m_sphereCen.Value, m_rad.Value, Color.Red, 1.0f, true);
                }

                var initVec = new Vector2(10, 250);
                var offset = new Vector2(0, 10);
                foreach (var axis in Axes)
                {
                    if (AxesCollection[axis].HasValue)
                    {
                        VRageRender.MyRenderProxy.DebugDrawText2D(initVec, axis.ToString() + ": " + AxesCollection[axis].Value, Color.Aqua, 0.4f);
                    }
                    else
                    {
                        VRageRender.MyRenderProxy.DebugDrawText2D(initVec, axis.ToString() + ": INVALID", Color.Aqua, 0.4f);
                    }
                    initVec += offset;
                }

                var mousePosition = MyGuiManager.MouseCursorPosition;
                VRageRender.MyRenderProxy.DebugDrawText2D(initVec, "Mouse coords: " + mousePosition.ToString(), Color.BlueViolet, 0.4f);
            }
        }

        private Vector3D? m_lineStart;
        private Vector3D? m_lineEnd;

        private Vector3D? m_sphereCen;
        private float? m_rad;

        public void SetDebugDrawLine(Vector3D start, Vector3D end)
        {
            m_lineStart = start;
            m_lineEnd = end;
        }

        public void SetDebugSphere(Vector3D cen, float rad)
        {
            m_sphereCen = cen;
            m_rad = rad;
        }

        public bool DebugDrawFunc()
        {
            DebugDraw = !DebugDraw;
            return true;
        }

        private bool ThrowFloatingObjectsFunc()
        {
            var view = MySession.Static.CameraController.GetViewMatrix();
            var inv = Matrix.Invert(view);

            //MyPhysicalInventoryItem item = new MyPhysicalInventoryItem(100, 
            var oreBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>("Stone");
			var scrapBuilder = MyFloatingObject.ScrapBuilder;

            for (int i = 1; i <= 25; i++)
            {
                var item = new MyPhysicalInventoryItem((MyRandom.Instance.Next() % 200) + 1, oreBuilder);
                var obj = MyFloatingObjects.Spawn(item, inv.Translation + inv.Forward * i * 1.0f, inv.Forward, inv.Up);
                obj.Physics.LinearVelocity = inv.Forward * 50;
            }

            Vector3D scrapPos = inv.Translation;
            scrapPos.X += 10;
            for (int i = 1; i <= 25; i++)
            {
                var item = new MyPhysicalInventoryItem((MyRandom.Instance.Next() % 200) + 1, scrapBuilder);
                var obj = MyFloatingObjects.Spawn(item, scrapPos + inv.Forward * i * 1.0f, inv.Forward, inv.Up);
                obj.Physics.LinearVelocity = inv.Forward * 50;
            }

            return true;
        }

        private bool RemoveGridsWithSpaceBallsFunc()
        {
            var grids = MyEntities.GetEntities();
            foreach (var grid in grids)
            {
                if (grid is MyCubeGrid)
                {
                    if ((grid as MyCubeGrid).GetFirstBlockOfType<MySpaceBall>() != null)
                        grid.Close();
                }
            }
            return true;
        }

        private string OnSelectBotForDebugMsg()
        {
            return string.Format("Auto select bot for debug: {0}", OnSelectDebugBot ? "TRUE" : "FALSE");
        }

        private string OnRecording()
        {
            if (MyVoiceChatSessionComponent.Static != null)
                return string.Format("VoIP recording: {0}", (MyVoiceChatSessionComponent.Static.IsRecording ? "TRUE" : "FALSE"));
            else
                return string.Format("VoIP unavailable");
        }

        private bool ToggleVoiceChat()
        {
            if (MyVoiceChatSessionComponent.Static.IsRecording)
            {
                MyVoiceChatSessionComponent.Static.StopRecording();
            }
            else
            {
                MyVoiceChatSessionComponent.Static.StartRecording();
            }
            return true;
        }

        private bool NextHeadMatrix()
        {
            CurrentHeadMatrixFlag++;
            if (CurrentHeadMatrixFlag > HeadMatrixFlag)
                CurrentHeadMatrixFlag = HeadMatrixFlag;
            if (MySession.LocalCharacter != null)
            {
                HeadMatrix = MySession.LocalCharacter.GetHeadMatrix((CurrentHeadMatrixFlag & 1) == 1, (CurrentHeadMatrixFlag & 2) == 2, (CurrentHeadMatrixFlag & 4) == 4, (CurrentHeadMatrixFlag & 8) == 8);
            }
            return true;
        }

        private bool PreviousHeadMatrix()
        {
            CurrentHeadMatrixFlag--;
            if (CurrentHeadMatrixFlag < 0)
                CurrentHeadMatrixFlag = 0;
            if (MySession.LocalCharacter != null)
            {
                HeadMatrix = MySession.LocalCharacter.GetHeadMatrix((CurrentHeadMatrixFlag & 1) == 1, (CurrentHeadMatrixFlag & 2) == 2, (CurrentHeadMatrixFlag & 4) == 4, (CurrentHeadMatrixFlag & 8) == 8);
            }
            return true;
        }
    }
}
