

#region Using

using Havok;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using VRage.Audio;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Input;
using VRage.ModAPI;
using VRageMath;
#if XB1 // XB1_ALLINONEASSEMBLY
using VRage.Utils;
#endif // XB1

#endregion

namespace Sandbox.Game.Gui
{
    public partial class MyHonzaInputComponent
    {
        public class DefaultComponent : MyDebugComponent
        {
            public static float MassMultiplier = 100;

            private static long m_lastMemory;
            private static HkMemorySnapshot? m_snapA;

            public static bool ApplyMassMultiplier;

            public enum ShownMassEnum
            {
                Havok = 0,
                Real = 1,
                SI = 2,
                None = 3,
                MaxVal
            }
            public static ShownMassEnum ShowRealBlockMass = ShownMassEnum.None;
            private int m_memoryB;
            private int m_memoryA;
            private static bool HammerForce;
            private float RADIUS = 0.005f;

            private bool m_drawBodyInfo = true;
            private bool m_drawUpdateInfo = false;


            private List<Type> m_dbgComponents = new List<Type>();

            public override string GetName()
            {
                return "Honza";
            }

            public DefaultComponent()
            {
                AddShortcut(MyKeys.None, false, false, false, false,
                    () => "Hammer (CTRL + Mouse left)",
                    null
                 );

                AddShortcut(MyKeys.H, true, true, true, false,
                    () => "Hammer force: " + (HammerForce ? "ON" : "OFF"),
                    delegate
                    {
                        HammerForce = !HammerForce;
                        return true;
                    }
                    );

                AddShortcut(MyKeys.OemPlus, true, true, false, false,
                 () => "Radius+: " + RADIUS,
                 delegate
                 {
                     RADIUS += 0.5f;
                     return true;
                 }
                 );

                AddShortcut(MyKeys.OemMinus, true, true, false, false,
                () => "",
                delegate
                {
                    RADIUS -= 0.5f;
                    return true;
                }
                );

                AddShortcut(MyKeys.NumPad7, true, false, false, false,
                    () => "Shown mass: " + ShowRealBlockMass.ToString(),
                    delegate
                    {
                        ShowRealBlockMass++;
                        ShowRealBlockMass = (ShownMassEnum)((int)ShowRealBlockMass % (int)ShownMassEnum.MaxVal);
                        return true;
                    });
                AddShortcut(MyKeys.NumPad8, true, false, false, false,
                    () => "MemA: " + m_memoryA + " MemB: " + m_memoryB + " Diff:" + (m_memoryB - m_memoryA),
                   Diff);
                AddShortcut(MyKeys.NumPad9, true, false, false, false,
                    () => "",
                    () => { m_drawBodyInfo = !m_drawBodyInfo; m_drawUpdateInfo = !m_drawUpdateInfo; return true; });
                AddShortcut(MyKeys.NumPad6, true, false, false, false,
                    () => "Prioritize: " + (MyFakes.PRIORITIZE_PRECALC_JOBS ? "On" : "Off"),
                    () => { MyFakes.PRIORITIZE_PRECALC_JOBS = !MyFakes.PRIORITIZE_PRECALC_JOBS; return true; });
                m_dbgComponents.Clear();
#if XB1 // XB1_ALLINONEASSEMBLY
                foreach (var t in MyAssembly.GetTypes())
#else // !XB1
                foreach (var t in Assembly.GetExecutingAssembly().GetTypes())
#endif // !XB1
                {
                    if (t.IsSubclassOf(typeof(MyRenderComponentBase)))
                        continue;
                    if (t.IsSubclassOf(typeof(MySyncComponentBase)))
                        continue;
                    if (t.IsSubclassOf(typeof(MyEntityComponentBase)))
                    {
                        m_dbgComponents.Add(t);
                    }
                }

            }

            private bool Diff()
            {
                foreach (var ent in MyEntities.GetEntities())
                {
                    if ((ent.PositionComp.GetPosition() - MySession.Static.ControlledEntity.Entity.PositionComp.GetPosition()).Length() > 100)
                    {
                        ent.Close();
                    }
                }
                return true;
                if (!m_snapA.HasValue)
                    m_snapA = HkBaseSystem.GetMemorySnapshot();
                else
                {
                    var snapB = HkBaseSystem.GetMemorySnapshot();
                    int a, b;
                    HkMemorySnapshot.Diff(m_snapA.Value, snapB, out a, out b);
                    //HkMemorySnapshot.Diff(m_snapA.Value, snapB);
                    m_snapA.Value.RemoveReference();
                    m_snapA = null;
                    snapB.RemoveReference();
                    m_memoryA = a;
                    m_memoryB = b;
                }
                return true;
            }

            public override bool HandleInput()
            {
                m_counter++;

                //if (MySector.MainCamera != null)
                //{
                //    List<MyPhysics.HitInfo> lst12 = new List<MyPhysics.HitInfo>();
                //    for (int i = 0; i < 1000; i++)
                //    {
                //        MyPhysics.CastRay(MySector.MainCamera.Position, MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 1000, lst12);
                //        lst12.Clear();
                //    }
                //}
                if (base.HandleInput())
                    return true;

                bool handled = false;

                if (MyInput.Static.IsAnyCtrlKeyPressed() && MyInput.Static.IsNewLeftMouseReleased())
                {
                    Hammer();
                }

                handled = HandleMassMultiplier(handled);

                handled = HandleMemoryDiff(handled);

                if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad9))
                    MyScriptManager.Static.LoadData();

                if (SelectedEntity != null && MyInput.Static.IsNewKeyPressed(MyKeys.NumPad3))
                {
                    var c = Activator.CreateInstance(m_dbgComponents[m_memoryA]);
                    SelectedEntity.Components.Add(m_dbgComponents[m_memoryA], c as MyComponentBase);
                }

                if (MyAudio.Static != null)
                {
                    foreach (var em in MyAudio.Static.Get3DSounds())
                    {
                        VRageRender.MyRenderProxy.DebugDrawSphere(em.SourcePosition, 0.1f, Color.Red, 1, false);
                    }
                }

                return handled;
            }

            private static bool HandleMassMultiplier(bool handled)
            {
                if (MyInput.Static.IsNewKeyPressed(MyKeys.NumPad1))
                {
                    ApplyMassMultiplier = !ApplyMassMultiplier;
                    handled = true;
                }
                var mul = 1;
                if (MyInput.Static.IsKeyPress(MyKeys.N))
                    mul = 10;
                if (MyInput.Static.IsKeyPress(MyKeys.B))
                    mul = 100;
                if (MyInput.Static.IsNewKeyPressed(MyKeys.OemQuotes))
                {
                    if (MassMultiplier > 1)
                        MassMultiplier += mul;
                    else
                        MassMultiplier *= mul;
                    handled = true;
                }
                if (MyInput.Static.IsNewKeyPressed(MyKeys.OemSemicolon))
                {
                    if (MassMultiplier > 1)
                        MassMultiplier -= mul;
                    else
                        MassMultiplier /= mul;
                    handled = true;
                }
                return handled;
            }

            private void DrawBodyInfo()
            {
                Vector2 pos = new Vector2(400, 10);
                MyEntity hitEntity = null;

                HkRigidBody body = null;
                if (SelectedEntity != null && SelectedEntity.Physics != null)
                    body = ((MyEntity)SelectedEntity).Physics.RigidBody;

                if (MySector.MainCamera != null && body == null)
                {
                    List<MyPhysics.HitInfo> lst = new List<MyPhysics.HitInfo>();
                    MyPhysics.CastRay(MySector.MainCamera.Position, MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 100, lst);
                    foreach (var hit in lst)
                    {
                        body = hit.HkHitInfo.Body;
                        if (body == null || body.Layer == MyPhysics.CollisionLayers.NoCollisionLayer) continue;
                        hitEntity = hit.HkHitInfo.GetHitEntity() as MyEntity;

                        var sb = new System.Text.StringBuilder("ShapeKeys: ");
                        for (int i = 0; i < HkWorld.HitInfo.ShapeKeyCount; i++)
                        {
                            var key = hit.HkHitInfo.GetShapeKey(i);
                            if (key == uint.MaxValue) break;
                            sb.Append(string.Format("{0} ", key));
                        }
                        VRageRender.MyRenderProxy.DebugDrawText2D(pos, sb.ToString(), Color.White, 0.7f);
                        pos.Y += 20;
                        if (hitEntity != null && hitEntity.GetPhysicsBody() != null)
                        {
                            VRageRender.MyRenderProxy.DebugDrawText2D(pos, string.Format("Weld: {0}", hitEntity.GetPhysicsBody().WeldInfo.Children.Count), Color.White, 0.7f);
                        }
                        pos.Y += 20;
                        break;
                    }
                }
                //if (MySector.MainCamera != null)
                //{
                //    LineD line = new LineD(MySector.MainCamera.Position, MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 100);
                //    var intersect = MyEntities.GetIntersectionWithLine(ref line, MySession.ControlledEntity.Entity, null);
                //    if (intersect.HasValue)
                //    {
                //        VRageRender.MyRenderProxy.DebugDrawText2D(pos, intersect.Value.Entity.ToString() + " "
                //              , Color.White, 0.8f);
                //    }
                //}

                if (body != null && m_drawBodyInfo)
                {
                    //body.Activate();
                    //VRageRender.MyRenderProxy.DebugDrawText2D(pos, body.GetEntity(0).ToString() + " "
                    //       + MyDestructionHelper.MassFromHavok(body.Mass), Color.White, 0.8f);
                    pos.Y += 20;
                    VRageRender.MyRenderProxy.DebugDrawText2D(pos, "Layer: " + body.Layer, body.Layer == 0 ? Color.Red : Color.White, 0.7f);
                    pos.Y += 20;
                    VRageRender.MyRenderProxy.DebugDrawText2D(pos, string.Format("Friction: {0}  Restitution: {1}", body.Friction, body.Restitution), Color.White, 0.7f);
                    pos.Y += 20;
                    VRageRender.MyRenderProxy.DebugDrawText2D(pos, "Lin: " + body.LinearVelocity.Length(), Color.White, 0.7f);
                    pos.Y += 20;
                    VRageRender.MyRenderProxy.DebugDrawText2D(pos, "Ang: " + body.AngularVelocity.Length(), Color.White, 0.7f);
                    pos.Y += 20;
                    VRageRender.MyRenderProxy.DebugDrawText2D(pos, "Act: " + (body.IsActive ? "true" : "false"), Color.White, 0.7f);
                    pos.Y += 20;
                    VRageRender.MyRenderProxy.DebugDrawText2D(pos, "Stat: " + (body.IsFixedOrKeyframed ? "true" : "false"), Color.White, 0.7f);
                    pos.Y += 20;
                    VRageRender.MyRenderProxy.DebugDrawText2D(pos, "Solver: " + (body.Motion.GetDeactivationClass()), Color.White, 0.7f);
                    pos.Y += 20;
                    VRageRender.MyRenderProxy.DebugDrawText2D(pos, "Mass: " + body.Mass, Color.White, 0.7f);
                    pos.Y += 20;
                    //VRageRender.MyRenderProxy.DebugDrawText2D(pos, "SysID: " + body.GetBody().HavokCollisionSystemID, Color.White, 0.7f);
                    pos.Y += 20;
                    //VRageRender.MyRenderProxy.DebugDrawText2D(pos, "CharLin: " + MySession.ControlledEntity.Entity.Physics.LinearVelocity.Length(), Color.White, 0.7f);
                }

                if (SelectedEntity != null && m_drawUpdateInfo)
                {
                    VRageRender.MyRenderProxy.DebugDrawText2D(pos, "Updates: " + m_counter, Color.White, 0.7f);
                    pos.Y += 20;
                    VRageRender.MyRenderProxy.DebugDrawText2D(pos, "PositionUpd: " + dbgPosCounter, Color.White, 0.7f);
                    pos.Y += 20;
                    VRageRender.MyRenderProxy.DebugDrawText2D(pos, "Frames per update: " + m_counter / (float)dbgPosCounter, Color.White, 0.7f);
                    pos.Y += 20;
                }
            }

            private bool HandleMemoryDiff(bool handled)
            {
                if (MyInput.Static.IsAnyCtrlKeyPressed() && MyInput.Static.IsNewKeyPressed(MyKeys.PageUp))
                {
                    m_memoryA--;
                    handled = true;
                }
                if (MyInput.Static.IsAnyCtrlKeyPressed() && MyInput.Static.IsNewKeyPressed(MyKeys.PageDown))
                {
                    m_memoryA++;
                    handled = true;
                }
                m_memoryA = (m_memoryA + m_dbgComponents.Count) % m_dbgComponents.Count;
                return handled;
            }

            private void Hammer()
            {
                var IntersectionStart = MySector.MainCamera.Position;
                var IntersectionDirection = MySector.MainCamera.ForwardVector;
                LineD line = new LineD(IntersectionStart, IntersectionStart + IntersectionDirection * 200);

                var m_tmpHitList = new List<MyPhysics.HitInfo>();
                MyPhysics.CastRay(line.From, line.To, m_tmpHitList, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);
                // Remove character hits.
                m_tmpHitList.RemoveAll(delegate(MyPhysics.HitInfo hit)
                {
                    return (hit.HkHitInfo.GetHitEntity() == MySession.Static.ControlledEntity.Entity);
                });

                if (m_tmpHitList.Count == 0)
                    return;

                MyEntity closestEntity = null;
                MyPhysics.HitInfo closestHit = default(MyPhysics.HitInfo);

                foreach (var hit in m_tmpHitList)
                {
                    if (hit.HkHitInfo.Body != null)
                    {
                        closestEntity = hit.HkHitInfo.GetHitEntity() as MyEntity;
                        closestHit = hit;
                        break;
                    }
                }
                if (closestEntity == null)
                    return;
                HkdFractureImpactDetails details = HkdFractureImpactDetails.Create();
                details.SetBreakingBody(closestEntity.Physics.RigidBody);
                details.SetContactPoint(closestEntity.Physics.WorldToCluster(closestHit.Position));
                details.SetDestructionRadius(RADIUS);
                details.SetBreakingImpulse(Sandbox.MyDestructionConstants.STRENGTH * 10);
                if (HammerForce)
                    details.SetParticleVelocity(-line.Direction * 20);
                details.SetParticlePosition(closestEntity.Physics.WorldToCluster(closestHit.Position));
                details.SetParticleMass(1000000);
                //details.ZeroColidingParticleVelocity();
                details.Flag = details.Flag | HkdFractureImpactDetails.Flags.FLAG_DONT_RECURSE;
                if (closestEntity.GetPhysicsBody().HavokWorld.DestructionWorld != null)
                {
                    MyPhysics.FractureImpactDetails destruction = new MyPhysics.FractureImpactDetails();
                    destruction.Details = details;
                    destruction.World = closestEntity.GetPhysicsBody().HavokWorld;
                    destruction.Entity = closestEntity;
                    MyPhysics.EnqueueDestruction(destruction);
                    //closestGrid.GetPhysicsBody().HavokWorld.DestructionWorld.TriggerDestruction(ref details);
                }
                //details.RemoveReference();
            }

            private static bool SpawnBreakable(bool handled)
            {
                //if (MyInput.Static.IsNewKeyPressed(Keys.NumPad2))
                //{
                //    Vector3 he = new Vector3(1, 1, 1);
                //    HkShape psh = new HkBoxShape(he);
                //    HkMassProperties mp = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(he, 5);
                //    HkRigidBodyCinfo rbInfo = new HkRigidBodyCinfo();
                //    rbInfo.Shape = psh;
                //    rbInfo.SetMassProperties(mp);
                //    rbInfo.MotionType = HkMotionType.Dynamic;
                //    rbInfo.QualityType = HkCollidableQualityType.Moving;
                //    HkRigidBody rb = new HkRigidBody(rbInfo);
                //    HkdBreakableShape sh = new HkdBreakableShape(psh, ref mp);

                //    //sh.Fracture(MyPhysics.HavokWorld);
                //    //sh.Fracture(MyPhysics.DestructionWorld);
                //    sh.SetMassRecursively(100);
                //    sh.SetStrenghtRecursively(MyHonzaInputComponent.STRENGHT, 0.5f);

                //    HkdShapeInstanceInfo si = new HkdShapeInstanceInfo(sh, null, null);
                //    HkdCreateBodyInput ci = new HkdCreateBodyInput();

                //    //HkBreakableBody(HkBreakableShape^ breakableShape, HkRigidBody^ body)
                //    HkdBreakableBody body = new HkdBreakableBody(sh, rb);
                //    MyPhysics.DestructionWorld.AddBreakableBody(body);
                //    psh.RemoveReference();
                //    handled = true;
                //}

                //if (MyInput.Static.IsNewKeyPressed(Keys.NumPad3))
                //{
                //    var factory = MyPhysics.DestructionWorld.GetBreakableBodyFactory();
                //    Vector3 halfExtents = new Vector3(1, 1, 1);
                //    HkShape shape = new HkBoxShape(halfExtents);
                //    HkMassProperties massProp = HkInertiaTensorComputer.ComputeBoxVolumeMassProperties(halfExtents, 5);

                //    int shapeCount = 10;
                //    List<HkdShapeInstanceInfo> shapeInfos = new List<HkdShapeInstanceInfo>(shapeCount);
                //    for (int i = 0; i < shapeCount; i++)
                //    {
                //        HkdBreakableShape breakableShape = new HkdBreakableShape(shape, ref massProp);
                //        shapeInfos.Add(new HkdShapeInstanceInfo(breakableShape, null, new Vector3(0, 2 * i, 0)));
                //        var x = breakableShape.ReferenceCount;
                //    }

                //    HkdCompoundBreakableShape compBreakable = new HkdCompoundBreakableShape(null, shapeInfos);
                //    compBreakable.AutoConnect(MyPhysics.HavokWorld);
                //    //for (int i =0 ; i < shapeInfos.Count-1; i++)	
                //    //{
                //    //    Connection connection = Connection.Create();

                //    //    connection.SetShapesFromInfos(shapeInfos[i], shapeInfos[i+1]);
                //    //    connection.PivotA = connection.PivotB = Vector3.Zero;
                //    //    connection.SeparatingNormal = new Vector3(0,1,0);
                //    //    connection.ContactArea = 1.0f;
                //    //    connection.AddToCommonParent();
                //    //}
                //    compBreakable.SetStrenghtRecursively(300, 0.8f);
                //    compBreakable.BuildMassProperties(ref massProp);
                //    HkRigidBodyCinfo rbInfo = new HkRigidBodyCinfo();
                //    rbInfo.Shape = compBreakable.GetShape();
                //    rbInfo.SetMassProperties(massProp);
                //    rbInfo.MotionType = HkMotionType.Dynamic;
                //    rbInfo.QualityType = HkCollidableQualityType.Moving;
                //    HkRigidBody rb = new HkRigidBody(rbInfo);
                //    //CreateBodyInput ci = new CreateBodyInput();
                //    //HkBreakableBody body = fac.CreateBreakableBody(si,ci);
                //    //HkBreakableBody(HkBreakableShape^ breakableShape, HkRigidBody^ body)
                //    HkdBreakableBody body = new HkdBreakableBody(compBreakable, rb);
                //    MyPhysics.DestructionWorld.AddBreakableBody(body);
                //    shape.RemoveReference();
                //    handled = true;
                //}
                return handled;
            }

            public override void Draw()
            {
                base.Draw();


                var pos = new Vector2(600, 100);
                foreach (var t in m_dbgComponents)
                {
                    var has = SelectedEntity != null && SelectedEntity.Components.Contains(t);
                    VRageRender.MyRenderProxy.DebugDrawText2D(pos, t.Name, has ? Color.Green : Color.White, 0.5f);
                    pos.Y += 10;
                }

                pos = new Vector2(580, 100 + 10 * m_memoryA);
                VRageRender.MyRenderProxy.DebugDrawText2D(pos, ">>", Color.White, 0.5f);
                if (SelectedEntity != null)
                {
                    var bbd = new BoundingBoxD(SelectedEntity.PositionComp.LocalAABB.Min, SelectedEntity.PositionComp.LocalAABB.Max);
                    MyOrientedBoundingBoxD obb = new MyOrientedBoundingBoxD(bbd, SelectedEntity.WorldMatrix);
                    //VRageRender.MyRenderProxy.DebugDrawOBB(obb, Color.White, 1, false, false);
                    VRageRender.MyRenderProxy.DebugDrawAABB(SelectedEntity.PositionComp.WorldAABB, Color.White, 1, 1, false);
                }

                DrawBodyInfo();

                //var pos2 = MySector.MainCamera.Position;
                //var sphere = new BoundingSphereD(pos2, 1000);
                //var lst = new List<MyEntity>();
                //MyGamePruningStructure.GetAllEntitiesInSphere(ref sphere,lst);
                //foreach(var ent in MyEntities.GetEntities())
                //    if(!lst.Contains(ent))
                //        ent.Close();

                //if (!MySandboxGame.Static.IsRunning)
                //    return;
                //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.1f, 0.1f), "Destruction: " + (MyPerGameSettings.Destruction ? "ON" : "OFF"), Color.Red, 1);
                //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(200f, 5f), "(Ctrl+Shift+D)", Color.Red, 0.65f);
                //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.3f, 20f), "Strenght: " + STRENGHT, Color.Red, 1);
                //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.1f, 40f), "Fracture: " + (FRACTURE ? "ON" : "OFF"), Color.Red, 1);
                //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(160f, 45f), "(Ctrl+Shift+F)", Color.Red, 0.65f);
                //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.1f, 60f), "Integrity: " + (MyFakes.DESTRUCTION_STRUCTURAL_INTEGRITY ? "ON" : "OFF"), Color.Red, 1);
                //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(160f, 65f), "(Ctrl+I)", Color.Red, 0.65f);
                //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.1f, 80f), "Position: " + POSITION, Color.Red, 1);
                //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.1f, 100f), "Split: " + (GRID_SPLIT ? "ON" : "OFF"), Color.Red, 1);
                //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.1f, 120f), "AConn: " + (!ManualConnect ? "ON" : "OFF"), Color.Red, 1);
                //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.1f, 140f), "Hammer: " + (DestructionTool ? "ON" : "OFF"), Color.Red, 1);


                //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.1f, 0.1f), "MassMultiplier " + MassMultiplier, Color.Red, 1);
                //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.3f, 20f), ApplyMassMultiplier ? "ON" : "OFF", Color.Red, 1);
                //VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(0.3f, 40f), "Num1, N, B, ;, '", Color.Red, 1);
            }
        }

    }
}
