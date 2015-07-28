#region Using

using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems.StructuralIntegrity;
using Sandbox.Game.World;
using Sandbox.Graphics;
using System.Collections.Generic;
using System.Linq;
using VRage.Input;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

#endregion

namespace Sandbox.Game.Gui
{

    class MyDebugEntity : MyEntity
    {
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            Render.ModelStorage = MyModels.GetModelOnlyData(@"Models\StoneRoundLargeFull.mwm");
        }     
    }

    public class MyPetaInputComponent : MyDebugComponent
    {
        public static bool ENABLE_SI_DESTRUCTIONS = true;
        public static bool OLD_SI = false;
        public static bool DEBUG_DRAW_TENSIONS = false;
        public static bool DEBUG_DRAW_PATHS = false;
        public static float SI_DYNAMICS_MULTIPLIER = 1;
        
        public override string GetName()
        {
            return "Peta";
        }

        public MyPetaInputComponent()
        {
            AddShortcut(MyKeys.OemBackslash, true, true, false, false,
                () => "Debug draw physics clusters: " + MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_CLUSTERS,
                delegate
                {
                    MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_CLUSTERS = !MyDebugDrawSettings.DEBUG_DRAW_PHYSICS_CLUSTERS;
                    return true;
                });

            AddShortcut(MyKeys.OemBackslash, false, false, false, false,
                () => "Advance all moving entities",
                delegate
                {

                    foreach (var entity in MyEntities.GetEntities().ToList())
                    {
                        if (entity.Physics != null)
                        {
                            Vector3D posDelta = entity.Physics.LinearVelocity * SI_DYNAMICS_MULTIPLIER;
                            MyPhysics.Clusters.EnsureClusterSpace(entity.PositionComp.WorldAABB + posDelta);

                            if (entity.Physics.LinearVelocity.Length() > 0.1f)
                            {
                                entity.PositionComp.SetPosition(entity.PositionComp.GetPosition() + posDelta);
                            }
                        }
                    }
                    return true;
                });

            AddShortcut(MyKeys.S, true, true, false, false,
               () => "Insert controllable sphere",
               delegate
               {
                   MyControllableSphere sphere = new MyControllableSphere();
                   sphere.Init();
                   MyEntities.Add(sphere);

                   sphere.PositionComp.SetPosition(MySector.MainCamera.Position + 2 * MySector.MainCamera.ForwardVector);
                   sphere.Physics.Enabled = false;

                   MySession.LocalHumanPlayer.Controller.TakeControl(sphere);
                   return true;
               });

            AddShortcut(MyKeys.Back, true, true, false, false,
               () => "Freeze gizmo: " + MyCubeBuilder.Static.FreezeGizmo,
               delegate
               {
                   MyCubeBuilder.Static.FreezeGizmo = !MyCubeBuilder.Static.FreezeGizmo;
                   return true;
               });

            AddShortcut(MyKeys.NumPad8, true, false, false, false,
            () => "Wave to friend",
            delegate
            {
                MySession.LocalCharacter.AddCommand(new MyAnimationCommand()
                {
                    AnimationSubtypeName = "Wave",
                    BlendTime = 0.3f,
                    TimeScale = 1,
                }, true);
                return true;
            });

            AddShortcut(MyKeys.NumPad9, true, false, false, false,
              () => "Dynamics multiplier: " + ((int)SI_DYNAMICS_MULTIPLIER).ToString(),
              delegate
              {
                  SI_DYNAMICS_MULTIPLIER *= 10;
                  if (SI_DYNAMICS_MULTIPLIER > 10000)
                      SI_DYNAMICS_MULTIPLIER = 1;
                  return true;
              });

              AddShortcut(MyKeys.NumPad7, true, false, false, false,
              () => "Use next ship",
              delegate
              {
                  MyCharacterInputComponent.UseNextShip();
                  return true;
              });

              AddShortcut(MyKeys.NumPad5, true, false, false, false,
                () => "Insert tree",
                delegate
                {
                    InsertTree();
                    return true;
                });

              AddShortcut(MyKeys.NumPad6, true, false, false, false,
             () => "SI Debug draw paths",
             delegate
             {
                 MyDebugDrawSettings.ENABLE_DEBUG_DRAW = !MyDebugDrawSettings.ENABLE_DEBUG_DRAW;
                 if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
                 {
                     MyStructuralIntegrity.Enabled = true;
                     MyDebugDrawSettings.DEBUG_DRAW_STRUCTURAL_INTEGRITY = true;
                     DEBUG_DRAW_PATHS = true;
                     DEBUG_DRAW_TENSIONS = false;
                 }

                //foreach (var entity in MyEntities.GetEntities())
                //{
                //    if (entity.GetTopMostParent().Physics != null)
                //    {
                //        var body = entity.GetTopMostParent().Physics;
                //        if (body.RigidBody != null)
                //        {
                //            Vector3 newVel = body.Entity.Physics.LinearVelocity;
                //            float y = newVel.Y;
                //            newVel.Y = -newVel.Z;
                //            newVel.Z = y;
                //            body.RigidBody.ApplyLinearImpulse(newVel * body.Mass * 20);
                //        }
                //    }
                //}
                 return true;
             });

              AddShortcut(MyKeys.NumPad1, true, false, false, false,
             () => "Reorder clusters",
             delegate
             {
                 if (MySession.ControlledEntity != null)
                 {
                     MySession.ControlledEntity.Entity.GetTopMostParent().Physics.ReorderClusters();
                 }
                 return true;
             });

              AddShortcut(MyKeys.NumPad3, true, false, false, false,
             () => "SI Debug draw tensions",
             delegate
             {
                 MyDebugDrawSettings.ENABLE_DEBUG_DRAW = !MyDebugDrawSettings.ENABLE_DEBUG_DRAW;
                 if (MyDebugDrawSettings.ENABLE_DEBUG_DRAW)
                 {
                     MyStructuralIntegrity.Enabled = true;
                     MyDebugDrawSettings.DEBUG_DRAW_STRUCTURAL_INTEGRITY = true;
                     DEBUG_DRAW_PATHS = false;
                     DEBUG_DRAW_TENSIONS = true;
                 }
                 //foreach (var entity in MyEntities.GetEntities())
                 //{
                 //    if (entity.GetTopMostParent().Physics != null)
                 //    {
                 //        var body = entity.GetTopMostParent().Physics;
                 //        if (body.RigidBody != null)
                 //        {
                 //            Vector3 newVel = body.Entity.Physics.LinearVelocity;
                 //            float x = newVel.X;
                 //            newVel.X = -newVel.X;
                 //            newVel.Z = x;
                 //            body.RigidBody.ApplyLinearImpulse(newVel * body.Mass * 20);
                 //        }
                 //    }
                 //}
                 return true;
             });

              AddShortcut(MyKeys.NumPad4, true, false, false, false,
                 () => "Enable SI destructions: " + ENABLE_SI_DESTRUCTIONS,
                 delegate
                 {
                     ENABLE_SI_DESTRUCTIONS = !ENABLE_SI_DESTRUCTIONS;
                     return true;
                 });


            
              AddShortcut(MyKeys.Up, true, false, false, false,
                 () => "SI Selected cube up",
                 delegate
                 {
                     MyAdvancedStaticSimulator.SelectedCube = new Vector3I(MyAdvancedStaticSimulator.SelectedCube.X, MyAdvancedStaticSimulator.SelectedCube.Y + 1, MyAdvancedStaticSimulator.SelectedCube.Z);
                     return true;
                 });
              AddShortcut(MyKeys.Down, true, false, false, false,
                 () => "SI Selected cube down",
                 delegate
                 {
                     MyAdvancedStaticSimulator.SelectedCube = new Vector3I(MyAdvancedStaticSimulator.SelectedCube.X, MyAdvancedStaticSimulator.SelectedCube.Y - 1, MyAdvancedStaticSimulator.SelectedCube.Z);
                     return true;
                 });
              AddShortcut(MyKeys.Left, true, false, false, false,
                   () => "SI Selected cube left",
                   delegate
                   {
                       MyAdvancedStaticSimulator.SelectedCube = new Vector3I(MyAdvancedStaticSimulator.SelectedCube.X - 1, MyAdvancedStaticSimulator.SelectedCube.Y, MyAdvancedStaticSimulator.SelectedCube.Z);
                       return true;
                   });
              AddShortcut(MyKeys.Right, true, false, false, false,
                   () => "SI Selected cube right",
                   delegate
                   {
                       MyAdvancedStaticSimulator.SelectedCube = new Vector3I(MyAdvancedStaticSimulator.SelectedCube.X + 1, MyAdvancedStaticSimulator.SelectedCube.Y, MyAdvancedStaticSimulator.SelectedCube.Z);
                       return true;
                   });
              AddShortcut(MyKeys.Up, true, true, false, false,
                     () => "SI Selected cube forward",
                     delegate
                     {
                         MyAdvancedStaticSimulator.SelectedCube = new Vector3I(MyAdvancedStaticSimulator.SelectedCube.X, MyAdvancedStaticSimulator.SelectedCube.Y, MyAdvancedStaticSimulator.SelectedCube.Z - 1);
                         return true;
                     });
              AddShortcut(MyKeys.Down, true, true, false, false,
                       () => "SI Selected cube back",
                       delegate
                       {
                           MyAdvancedStaticSimulator.SelectedCube = new Vector3I(MyAdvancedStaticSimulator.SelectedCube.X, MyAdvancedStaticSimulator.SelectedCube.Y, MyAdvancedStaticSimulator.SelectedCube.Z + 1);
                           return true;
                       });

              AddShortcut(MyKeys.NumPad2, true, false, false, false,
                        () => "Spawn simple skinned object",
                        delegate
                        {
                            //MyDebugDrawSettings.ENABLE_DEBUG_DRAW = true;
                            //MyDebugDrawSettings.DEBUG_DRAW_FRACTURED_PIECES = !MyDebugDrawSettings.DEBUG_DRAW_FRACTURED_PIECES;

                            //foreach (var entity in MyEntities.GetEntities())
                            //{
                            //    if (entity is MyFracturedPiece)
                            //    {
                            //        foreach (var id in entity.Render.RenderObjectIDs)
                            //        {
                            //            VRageRender.MyRenderProxy.UpdateRenderObjectVisibility(id, !MyDebugDrawSettings.DEBUG_DRAW_FRACTURED_PIECES, false);
                            //        }
                            //    }
                            //}

                            SpawnSimpleSkinnedObject();

                            return true;
                        });

            
        }

        public override bool HandleInput()
        {
            if (base.HandleInput())
                return true;

            bool handled = false;

          
            //foreach (var ent in MyEntities.GetEntities())
            //{
            //    if (ent is MyCubeGrid)
            //    {
            //        if (ent.PositionComp.WorldAABB.Contains(MySector.MainCamera.Position) == ContainmentType.Disjoint)
            //        {
            //            ent.Close();
            //        }
            //    }
            //}

            //var measureStart = new VRage.Library.Utils.MyTimeSpan(System.Diagnostics.Stopwatch.GetTimestamp());

            var list = MyDefinitionManager.Static.GetAnimationDefinitions();

            foreach (var skin in m_skins)
            {                
                skin.UpdateAnimation();

                if (MyRandom.Instance.NextFloat() > 0.95f)
                {
                    var randomAnim = list.ItemAt(MyRandom.Instance.Next(0, list.Count));
                    var command = new MyAnimationCommand()
                    {
                        AnimationSubtypeName = randomAnim.Id.SubtypeName,
                        FrameOption = MyFrameOption.Loop,
                        TimeScale = 1,
                        BlendTime = 0.3f
                    };
                    skin.AddCommand(command);
                }
            }


            //var measureEnd = new VRage.Library.Utils.MyTimeSpan(System.Diagnostics.Stopwatch.GetTimestamp());
            //var total = measureEnd - measureStart;
            //m_skins.Clear();

            return handled;
        }


        public override void Draw()
        {
            if (MySector.MainCamera == null)
                return;

            base.Draw();

            //Vector3D compassPosition = MySector.MainCamera.Position + MySector.MainCamera.ForwardVector + MySector.MainCamera.LeftVector / 1.5f - MySector.MainCamera.UpVector / 2;
            //VRageRender.MyRenderProxy.DebugDrawAxis(MatrixD.CreateTranslation(compassPosition), 0.1f, false);
            //VRageRender.MyRenderProxy.DebugDrawSphere(compassPosition, 0.02f, Color.White, 0.4f, false);

            if (MyDebugDrawSettings.DEBUG_DRAW_FRACTURED_PIECES)
            {
                foreach (var entity in MyEntities.GetEntities())
                {
                    var piece = entity as MyFracturedPiece;
                    if (piece != null)
                    {
                        MyPhysicsDebugDraw.DebugDrawBreakable(piece.Physics.BreakableBody, piece.Physics.ClusterToWorld(Vector3D.Zero));
                    }
                }
            }




            return;
            var mat = "WeaponLaser";
            float thickness = 0.05f;

            float spacing = 5;
            int count = 100;
            Vector4 color = Color.White.ToVector4();

            //Vector3 center = MySector.MainCamera.Position + new Vector3(0, -10, 0);
            Vector3 center = Vector3.Zero;
            Vector3 startPos = center - new Vector3(spacing * count / 2, 0, 0);

            for (int i = 0; i < count; i++)
            {
                Vector3D lineStart = startPos + new Vector3(spacing * i, 0, -spacing * count / 2);
                Vector3D lineEnd = startPos + new Vector3(spacing * i, 0, spacing * count / 2);

                Vector3D closestPoint = Vector3.Zero;
                Vector3D campos = MySector.MainCamera.Position;
                closestPoint = MyUtils.GetClosestPointOnLine(ref lineStart, ref lineEnd, ref campos);
                var distance = MySector.MainCamera.GetDistanceWithFOV(closestPoint);

                var lineThickness = thickness * MathHelper.Clamp(distance, 0.1f, 10);

                MySimpleObjectDraw.DrawLine(lineStart, lineEnd, mat, ref color, (float)lineThickness );
            }

        }

        void InsertTree()
        {
            MyDefinitionId id = new MyDefinitionId(MyObjectBuilderType.Parse("MyObjectBuilder_Tree"), "Tree04_v2");
            var itemDefinition = MyDefinitionManager.Static.GetEnvironmentItemDefinition(id);

            if (MyModels.GetModelOnlyData(itemDefinition.Model).HavokBreakableShapes != null)
            {
                var breakableShape = MyModels.GetModelOnlyData(itemDefinition.Model).HavokBreakableShapes[0].Clone();
                MatrixD worldMatrix = MatrixD.CreateWorld(MySession.ControlledEntity.Entity.PositionComp.GetPosition() +2 * MySession.ControlledEntity.Entity.WorldMatrix.Forward, Vector3.Forward, Vector3.Up);



                List<HkdShapeInstanceInfo> children = new List<HkdShapeInstanceInfo>();
                breakableShape.GetChildren(children);
                children[0].Shape.SetFlagRecursively(HkdBreakableShape.Flags.IS_FIXED);
                var piece = Sandbox.Engine.Physics.MyDestructionHelper.CreateFracturePiece(breakableShape, MyPhysics.SingleWorld.DestructionWorld, ref worldMatrix, false, itemDefinition.Id, true);
            }

        }

        List<MySkinnedEntity> m_skins = new List<MySkinnedEntity>();
        void SpawnSimpleSkinnedObject()
        {

            var skin = new MySkinnedEntity();

            MyObjectBuilder_Character ob = new MyObjectBuilder_Character();
            ob.PositionAndOrientation = new VRage.MyPositionAndOrientation(MySector.MainCamera.Position + 2 * MySector.MainCamera.ForwardVector, MySector.MainCamera.ForwardVector, MySector.MainCamera.UpVector);
            skin.Init(null, @"Models\Characters\Basic\ME_barbar.mwm", null, null);
            skin.Init(ob);

            MyEntities.Add(skin);

            var command = new MyAnimationCommand()
            {
                AnimationSubtypeName = "IdleBarbar",
                FrameOption = MyFrameOption.Loop,
                TimeScale = 1
            };
            skin.AddCommand(command);

            m_skins.Add(skin);
        }
    }
}
