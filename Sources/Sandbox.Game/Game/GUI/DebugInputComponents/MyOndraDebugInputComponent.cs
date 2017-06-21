#region Using

using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems.StructuralIntegrity;
using Sandbox.Game.World;
using SteamSDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Input;
using VRage.ObjectBuilders;
using VRageMath;
using VRageRender;

#endregion

namespace Sandbox.Game.Gui
{
    class MyOndraInputComponent : MyDebugComponent
    {
        bool m_gridDebugInfo = false;
        
        public override string GetName()
        {
            return "Ondra";
        }

        void phantom_Enter(HkPhantomCallbackShape sender, HkRigidBody body)
        {
        }

        void phantom_Leave(HkPhantomCallbackShape sender, HkRigidBody body)
        {
        }

        public override void Draw()
        {
            base.Draw();
        }

        public override bool HandleInput()
        {
            bool handled = false;

            if (m_gridDebugInfo)
            {
                LineD line = new LineD(MySector.MainCamera.Position, MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 1000);
                MyCubeGrid grid;
                Vector3I cubePos;
                double distance;
                if (MyCubeGrid.GetLineIntersection(ref line, out grid, out cubePos, out distance))
                {
                    var gridMatrix = grid.WorldMatrix;
                    var boxMatrix = Matrix.CreateTranslation(cubePos * grid.GridSize) * gridMatrix;
                    var block = grid.GetCubeBlock(cubePos);

                    MyRenderProxy.DebugDrawText2D(new Vector2(), cubePos.ToString(), Color.White, 0.7f);
                    MyRenderProxy.DebugDrawOBB(Matrix.CreateScale(new Vector3(grid.GridSize) + new Vector3(0.15f)) * boxMatrix, Color.Red.ToVector3(), 0.2f, true, true);

                    //int[, ,] bones = grid.Skeleton.AddCubeBones(cubePos);

                    //Vector3 closestBone = Vector3.Zero;
                    //Vector3I closestPoint = Vector3I.Zero;
                    //float closestPointDist = float.MaxValue;
                    //int closestBoneIndex = 0;

                    //for (int x = -1; x <= 1; x += 1)
                    //{
                    //    for (int y = -1; y <= 1; y += 1)
                    //    {
                    //        for (int z = -1; z <= 1; z += 1)
                    //        {
                    //            int boneIndex = bones[x + 1, y + 1, z + 1];
                    //            Vector3 bone = grid.Skeleton[boneIndex];

                    //            var pos = boxMatrix.Translation + new Vector3(grid.GridSize / 2) * new Vector3(x, y, z);
                    //            //MyRenderProxy.DebugDrawSphere(pos, 0.2f, Color.Blue.ToVector3(), 1.0f, false);
                    //            MyRenderProxy.DebugDrawText3D(pos, String.Format("{0:G2}, {1:G2}, {2:G2}", bone.X, bone.Y, bone.Z), Color.White, 0.5f, false);

                    //            var dist = MyUtils.GetPointLineDistance(ref line, ref pos);
                    //            if (dist < closestPointDist)
                    //            {
                    //                closestPointDist = dist;
                    //                closestPoint = new Vector3I(x, y, z);
                    //                closestBoneIndex = boneIndex;
                    //                closestBone = bone;

                    //            }
                    //        }
                    //    }
                    //}

                    //MyRenderProxy.DebugDrawText3D(boxMatrix.Translation + new Vector3(grid.GridSize / 2) * closestPoint * 1.0f, String.Format("{0:G2}, {1:G2}, {2:G2}", closestBone.X, closestBone.Y, closestBone.Z), Color.Red, 0.5f, false);
                    //var bonePos = grid.Skeleton[bones[closestPoint.X + 1, closestPoint.Y + 1, closestPoint.Z + 1]];
                    //MyRenderProxy.DebugDrawSphere(boxMatrix.Translation + new Vector3(grid.GridSize / 2) * closestPoint * 1.0f + bonePos, 0.5f, Color.Red.ToVector3(), 0.4f, true, true);

                    //if (input.IsNewKeyPressed(Keys.P) && block != null)
                    //{
                    //    if (input.IsAnyShiftKeyPressed())
                    //    {
                    //        grid.ResetBlockSkeleton(block);
                    //    }
                    //    else
                    //    {
                    //        grid.Skeleton[bones[closestPoint.X + 1, closestPoint.Y + 1, closestPoint.Z + 1]] = Vector3.Zero;
                    //        grid.AddDirtyBone(cubePos, closestPoint + Vector3I.One);
                    //        //grid.SetBlockDirty(block);
                    //    }
                    //    handled = true;
                    //}

                    //// Move bones to center by 0.1f
                    //if (input.IsNewKeyPressed(Keys.OemOpenBrackets))
                    //{
                    //    int index = bones[closestPoint.X + 1, closestPoint.Y + 1, closestPoint.Z + 1];
                    //    grid.Skeleton[index] -= Vector3.Sign(grid.Skeleton[index]) * 0.1f;
                    //    grid.AddDirtyBone(cubePos, closestPoint + Vector3I.One);
                    //    //grid.SetBlockDirty(block);
                    //    handled = true;
                    //}

                    //// Reduce max offset by 0.1f
                    //if (input.IsNewKeyPressed(Keys.OemCloseBrackets))
                    //{
                    //    int index = bones[closestPoint.X + 1, closestPoint.Y + 1, closestPoint.Z + 1];
                    //    var old = Vector3.Abs(grid.Skeleton[index]);
                    //    var max = new Vector3(Math.Max(Math.Max(old.X, old.Y), old.Z));
                    //    if (max.X > 0.1f)
                    //    {
                    //        grid.Skeleton[index] = Vector3.Clamp(grid.Skeleton[index], -max + 0.1f, max - 0.1f);
                    //    }
                    //    else
                    //    {
                    //        grid.Skeleton[index] = Vector3.Zero;
                    //    }
                    //    grid.AddDirtyBone(cubePos, closestPoint + Vector3I.One);
                    //    //grid.SetBlockDirty(block);
                    //    handled = true;
                    //}
                }
            }

            if (MyInput.Static.IsAnyAltKeyPressed())
                return handled;

            bool shift = MyInput.Static.IsAnyShiftKeyPressed();
            bool ctrl = MyInput.Static.IsAnyCtrlKeyPressed();

            //if (input.IsNewKeyPressed(Keys.I))
            //{
            //    foreach (var grid in MyEntities.GetEntities().OfType<MyCubeGrid>())
            //    {
            //        foreach (var block in grid.GetBlocks().ToArray())
            //        {
            //            grid.DetectMerge(block.Min, block.Max);
            //        }
            //    }
            //    handled = true;
            //}

            // Disabled since it is common to have normal control bound to O key.
            // If you ever need this again, bind it to something more complicated, like key combination.
            //if (input.IsNewKeyPressed(Keys.O))
            //{
            //    m_gridDebugInfo = !m_gridDebugInfo;
            //    handled = true;
            //}

            //for (int i = 0; i <= 9; i++)
            //{
            //    if (MyInput.Static.IsNewKeyPressed((Keys)(((int)Keys.D0) + i)))
            //    {
            //        string name = "Slot" + i.ToString();
            //        if (ctrl)
            //        {
            //            MySession.Static.Name = name;
            //            MySession.Static.WorldID = MySession.Static.GetNewWorldId();
            //            MySession.Static.Save(name);
            //        }
            //        else if (shift)
            //        {
            //            var path = MyLocalCache.GetSessionSavesPath(name, false, false);
            //            if (System.IO.Directory.Exists(path))
            //            {
            //                MySession.Static.Unload();
            //                MySession.Static.Load(path);
            //            }
            //        }
            //        handled = true;
            //    }
            //}

            //if (MyInput.Static.IsNewKeyPressed(Keys.End))
            //{
            //    MyMeteorShower.MeteorWave(null);
            //}

            // Disabled for god sake!
            //if (MyInput.Static.IsNewKeyPressed(Keys.PageUp) && MyInput.Static.IsAnyCtrlKeyPressed())
            //{
            //    MyReloadTestComponent.Enabled = true;
            //}
            //if (MyInput.Static.IsNewKeyPressed(Keys.PageDown) && MyInput.Static.IsAnyCtrlKeyPressed())
            //{
            //    MyReloadTestComponent.Enabled = false;
            //}

            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad6))
            {
                var view = MySector.MainCamera.ViewMatrix;
                var inv = Matrix.Invert(view);

                //MyPhysicalInventoryItem item = new MyPhysicalInventoryItem(100, 
                var oreBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>("Stone");
                var item = new MyPhysicalInventoryItem(1, oreBuilder);
                var obj = MyFloatingObjects.Spawn(item, inv.Translation + inv.Forward * 1.0f, inv.Forward, inv.Up);
                obj.Physics.LinearVelocity = inv.Forward * 50;
            }

            if (false && MyInput.Static.IsNewKeyPressed(MyKeys.NumPad9))
            {
                List<HkShape> trShapes = new List<HkShape>();
                List<HkConvexShape> shapes = new List<HkConvexShape>();
                List<Matrix> matrices = new List<Matrix>();

                var grid = new HkGridShape(2.5f, HkReferencePolicy.None);
                const short size = 50;
                for (short x = 0; x < size; x++)
                {
                    for (short y = 0; y < size; y++)
                    {
                        for (short z = 0; z < size; z++)
                        {
                            var box = new HkBoxShape(Vector3.One);
                            grid.AddShapes(new System.Collections.Generic.List<HkShape>() { box }, new Vector3S(x, y, z), new Vector3S(x, y, z));
                            trShapes.Add(new HkConvexTranslateShape(box, new Vector3(x, y, z), HkReferencePolicy.None));
                            shapes.Add(box);
                            matrices.Add(Matrix.CreateTranslation(new Vector3(x, y, z)));
                        }
                    }
                }

                var emptyGeom = new HkGeometry(new List<Vector3>(), new List<int>());

                var list = new HkListShape(trShapes.ToArray(), trShapes.Count, HkReferencePolicy.None);
                var compressedBv = new HkBvCompressedMeshShape(emptyGeom, shapes, matrices, HkWeldingType.None);
                var mopp = new HkMoppBvTreeShape(list, HkReferencePolicy.None);

                HkShapeBuffer buf = new HkShapeBuffer();

                //HkShapeContainerIterator i = compressedBv.GetIterator(buf);
                //int count = 0; // will be 125000
                //while (i.IsValid)
                //{
                //    count++;
                //    i.Next();
                //}                

                buf.Dispose();
                var info = new HkRigidBodyCinfo();
                info.Mass = 10;
                info.CalculateBoxInertiaTensor(Vector3.One, 10);
                info.MotionType = HkMotionType.Dynamic;
                info.QualityType = HkCollidableQualityType.Moving;
                info.Shape = compressedBv;
                var body = new HkRigidBody(info);

                //MyPhysics.HavokWorld.AddRigidBody(body);
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad7))
            {
                foreach (var g in MyEntities.GetEntities().OfType<MyCubeGrid>())
                {
                    foreach (var s in g.CubeBlocks.Select(s => s.FatBlock).Where(s => s != null).OfType<MyMotorStator>())
                    {
                        if (s.Rotor != null)
                        {
                            var q = Quaternion.CreateFromAxisAngle(s.Rotor.WorldMatrix.Up, MathHelper.ToRadians(45));
                            s.Rotor.CubeGrid.WorldMatrix = MatrixD.CreateFromQuaternion(q) * s.Rotor.CubeGrid.WorldMatrix;
                        }
                    }
                }
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad8))
            {
                var view = MySector.MainCamera.ViewMatrix;
                var inv = Matrix.Invert(view);

                var oreBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>("Stone");
                var obj = new MyObjectBuilder_FloatingObject() 
                { 
                    Item = new MyObjectBuilder_InventoryItem() 
                    { 
                        PhysicalContent = oreBuilder, Amount = 1000 
                    } 
                };
                obj.PositionAndOrientation = new MyPositionAndOrientation(inv.Translation + 2.0f * inv.Forward, inv.Forward, inv.Up);
                obj.PersistentFlags = MyPersistentEntityFlags2.InScene;
                var e = MyEntities.CreateFromObjectBuilderAndAdd(obj);
                e.Physics.LinearVelocity = Vector3.Normalize(inv.Forward) * 50.0f;
            }


            if (MyInput.Static.IsNewKeyPressed(MyKeys.Divide))
            {
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.Multiply))
            {
                MyDebugDrawSettings.ENABLE_DEBUG_DRAW = !MyDebugDrawSettings.ENABLE_DEBUG_DRAW;
                MyDebugDrawSettings.DEBUG_DRAW_STRUCTURAL_INTEGRITY = true;

                var grids = MyEntities.GetEntities().OfType<MyCubeGrid>();
                foreach (var g in grids)
                {
                    if (!g.IsStatic)// || g.GetBlocks().Count < 800) //to compute only castle
                        continue;

                    g.CreateStructuralIntegrity();
                }
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad1))
            {
                var e = MyEntities.GetEntities().OfType<MyCubeGrid>().FirstOrDefault();
                if (e != null)
                {
                    e.Physics.RigidBody.MaxLinearVelocity = 1000;
                    if (e.Physics.RigidBody2 != null)
                        e.Physics.RigidBody2.MaxLinearVelocity = 1000;

                    e.Physics.LinearVelocity = new Vector3(1000, 0, 0);
                }
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.Decimal))
            {
                MyPrefabManager.Static.SpawnPrefab("respawnship", MySector.MainCamera.Position, MySector.MainCamera.ForwardVector, MySector.MainCamera.UpVector);
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.Multiply) && MyInput.Static.IsAnyShiftKeyPressed())
            {
                GC.Collect(2);
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad5))
            {
                Thread.Sleep(250);
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad9))
            {
                var obj = MySession.Static.ControlledEntity != null ? MySession.Static.ControlledEntity.Entity : null;
                if (obj != null)
                {
                    const float dist = 5.0f;
                    obj.PositionComp.SetPosition(obj.PositionComp.GetPosition() + obj.WorldMatrix.Forward * dist);
                }
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad4))
            {
                MyEntity invObject = MySession.Static.ControlledEntity as MyEntity;
                if (invObject != null && invObject.HasInventory)
                {
                    MyFixedPoint amount = 20000;

                    var oreBuilder = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>("Stone");
                    MyInventory inventory = invObject.GetInventory(0) as MyInventory;
                    System.Diagnostics.Debug.Assert(inventory != null, "Null or unexpected type returned!");
                    inventory.AddItems(amount, oreBuilder);
                }

                handled = true;
            }

            //if (MyInput.Static.IsNewKeyPressed(Keys.NumPad8))
            //{
            //    var pos = MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 2;
            //    var grid = (MyObjectBuilder_CubeGrid)MyObjectBuilderSerializer.CreateNewObject(MyObjectBuilderTypeEnum.CubeGrid);
            //    grid.PositionAndOrientation = new MyPositionAndOrientation(pos, Vector3.Forward, Vector3.Up);
            //    grid.CubeBlocks = new List<MyObjectBuilder_CubeBlock>();
            //    grid.GridSizeEnum = MyCubeSize.Large;

            //    var block = new MyObjectBuilder_CubeBlock();
            //    block.BlockOrientation = MyBlockOrientation.Identity;
            //    block.Min = Vector3I.Zero;
            //    //var blockDefinition = Sandbox.Game.Managers.MyDefinitionManager.Static.GetCubeBlockDefinition(new CommonLib.ObjectBuilders.Definitions.MyDefinitionId(typeof(MyObjectBuilder_CubeBlock), "LargeBlockArmorBlock"));
            //    block.SubtypeName = "LargeBlockArmorBlock";
            //    grid.CubeBlocks.Add(block);
            //    grid.LinearVelocity = MySector.MainCamera.ForwardVector * 20;
            //    grid.PersistentFlags = MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;

            //    var x = MyEntities.CreateFromObjectBuilderAndAdd(grid);
            //}

            //if (MyInput.Static.IsNewKeyPressed(Keys.NumPad9))
            //{
            //    var pos = MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 2;
            //    var grid =  (MyObjectBuilder_CubeGrid)MyObjectBuilderSerializer.CreateNewObject(MyObjectBuilderTypeEnum.CubeGrid);
            //    grid.PositionAndOrientation = new MyPositionAndOrientation(pos, Vector3.Forward, Vector3.Up);
            //    grid.CubeBlocks = new List<MyObjectBuilder_CubeBlock>();
            //    grid.GridSizeEnum = MyCubeSize.Large;

            //    var block = new MyObjectBuilder_CubeBlock();
            //    block.BlockOrientation = MyBlockOrientation.Identity;
            //    block.Min = Vector3I.Zero;
            //    //var blockDefinition = Sandbox.Game.Managers.MyDefinitionManager.Static.GetCubeBlockDefinition(new CommonLib.ObjectBuilders.Definitions.MyDefinitionId(typeof(MyObjectBuilder_CubeBlock), "LargeBlockArmorBlock"));
            //    block.SubtypeName = "LargeBlockGyro";
            //    grid.CubeBlocks.Add(block);
            //    grid.LinearVelocity = MySector.MainCamera.ForwardVector * 20;
            //    grid.PersistentFlags = MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;

            //    var x = MyEntities.CreateFromObjectBuilderAndAdd(grid);
            //}

            if (MyInput.Static.IsAnyCtrlKeyPressed() && MyInput.Static.IsNewKeyPressed(MyKeys.Delete))
            {
                int count = MyEntities.GetEntities().OfType<MyFloatingObject>().Count();
                foreach (var obj in MyEntities.GetEntities().OfType<MyFloatingObject>())
                {
                    if (obj == MySession.Static.ControlledEntity)
                    {
                        MySession.Static.SetCameraController(MyCameraControllerEnum.Spectator);
                    }
                    obj.Close();
                }
                handled = true;
            }

            if (MyInput.Static.IsAnyCtrlKeyPressed() && MyInput.Static.IsNewKeyPressed(MyKeys.Decimal))
            {
                foreach (var obj in MyEntities.GetEntities())
                {
                    if (obj != MySession.Static.ControlledEntity && (MySession.Static.ControlledEntity == null || obj != MySession.Static.ControlledEntity.Entity.Parent) && obj != MyCubeBuilder.Static.FindClosestGrid())
                        obj.Close();
                }
                handled = true;
            }

            if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad9) || MyInput.Static.IsNewKeyPressed(MyKeys.NumPad5))
            {
                //MyCubeGrid.UserCollisions = input.IsNewKeyPressed(Keys.NumPad9);

                var body = MySession.Static.ControlledEntity.Entity.GetTopMostParent().Physics;
                if (body.RigidBody != null)
                {
                    //body.AddForce(Engine.Physics.MyPhysicsForceType.ADD_BODY_FORCE_AND_BODY_TORQUE, new Vector3(0, 0, 10 * body.Mass), null, null);
                    body.RigidBody.ApplyLinearImpulse(body.Entity.WorldMatrix.Forward * body.Mass * 2);
                }
                handled = true;
            }

            //if (input.IsNewKeyPressed(Keys.J) && input.IsAnyCtrlKeyPressed())
            //{
            //    MyGlobalInputComponent.CopyCurrentGridToClipboard();

            //    MyEntity addedEntity = MyGlobalInputComponent.PasteEntityFromClipboard();

            //    if (addedEntity != null)
            //    {
            //        Vector3 pos = addedEntity.GetPosition();
            //        pos.Z += addedEntity.WorldVolume.Radius * 1.5f;
            //        addedEntity.SetPosition(pos);
            //    }
            //    handled = true;
            //}

            if (MyInput.Static.IsAnyCtrlKeyPressed() && MyInput.Static.IsNewKeyPressed(MyKeys.OemComma))
            {
                foreach (var e in MyEntities.GetEntities().OfType<MyFloatingObject>().ToArray())
                    e.Close();
            }
            
            return handled;
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
    }
}
