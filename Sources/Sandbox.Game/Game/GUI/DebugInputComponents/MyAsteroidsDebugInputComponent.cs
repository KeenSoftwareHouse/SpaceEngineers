using Sandbox.Game.Entities.Character;
using Sandbox.Game.World;
using Sandbox.Game.World.Generator;
using System.Collections.Generic;
using VRage;
using VRage.Input;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using Sandbox.Game.Entities;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;

namespace Sandbox.Game.Gui
{
    class MyAsteroidsDebugInputComponent : MyDebugComponent
    {
        private bool m_drawSeeds = false;
        private bool m_drawTrackedEntities = false;
        private bool m_drawAroundCamera = false;
        private bool m_drawRadius = false;
        private bool m_drawDistance = false;
        private bool m_drawCells = false;

        private List<MyCharacter> m_plys = new List<MyCharacter>();

        private float m_originalFarPlaneDisatance = -1;
        private float m_debugFarPlaneDistance = 1000000;
        private bool m_fakeFarPlaneDistance = false;

        public MyAsteroidsDebugInputComponent()
        {
            //AddShortcut(MyKeys.NumPad1, true, false, false, false,
            //    () => "Debug draw procedural asteroid seeds: " + m_drawSeeds,
            //    delegate
            //    {
            //        m_drawSeeds = !m_drawSeeds;
            //        return true;
            //    });

            //AddShortcut(MyKeys.NumPad2, true, false, false, false,
            //    () => "Debug draw procedural tracked entities: " + m_drawTrackedEntities,
            //    delegate
            //    {
            //        m_drawTrackedEntities = !m_drawTrackedEntities;
            //        return true;
            //    });

            //AddShortcut(MyKeys.NumPad3, true, false, false, false,
            //    () => "Debug draw around camera: " + m_drawAroundCamera,
            //    delegate
            //    {
            //        m_drawAroundCamera = !m_drawAroundCamera;
            //        return true;
            //    });

            //AddShortcut(MyKeys.NumPad4, true, false, false, false,
            //    () => "Toggle farplane distance: " + (MySector.MainCamera == null ? -1f : MySector.MainCamera.FarPlaneDistance),
            //    delegate
            //    {
            //        m_fakeFarPlaneDistance = !m_fakeFarPlaneDistance;
            //        if (m_originalFarPlaneDisatance == -1)
            //        {
            //            m_originalFarPlaneDisatance = MySector.MainCamera.FarPlaneDistance;
            //        }
            //        if (m_fakeFarPlaneDistance)
            //        {
            //            MySector.MainCamera.FarPlaneDistance = m_debugFarPlaneDistance;
            //        }
            //        else
            //        {
            //            MySector.MainCamera.FarPlaneDistance = m_originalFarPlaneDisatance;
            //        }
            //        return true;
            //    });

            //AddShortcut(MyKeys.NumPad5, true, false, false, false,
            //    () => "Debug draw procedural seed radius: " + m_drawRadius,
            //    delegate
            //    {
            //        m_drawRadius = !m_drawRadius;
            //        return true;
            //    });

            //AddShortcut(MyKeys.NumPad6, true, false, false, false,
            //    () => "Debug draw procedural seed distance: " + m_drawDistance,
            //    delegate
            //    {
            //        m_drawDistance = !m_drawDistance;
            //        return true;
            //    });


            //AddShortcut(MyKeys.NumPad8, true, false, false, false,
            //    () => "Debug draw procedural cells: " + m_drawCells,
            //    delegate
            //    {
            //        m_drawCells = !m_drawCells;
            //        return true;
            //    });

            //AddShortcut(MyKeys.NumPad9, true, false, false, false,
            //    () => "Spawn new moving player somewhere: " + m_plys.Count,
            //    delegate
            //    {
            //        var pos = new Vector3D(MyRandom.Instance.NextFloat() * 2 - 1f, MyRandom.Instance.NextFloat() * 2 - 1f, MyRandom.Instance.NextFloat() * 2 - 1f) * 150000;
            //        var vel = new Vector3(MyRandom.Instance.NextFloat() * 2 - 1f, MyRandom.Instance.NextFloat() * 2 - 1f, MyRandom.Instance.NextFloat() * 2 - 1f);
            //        vel.Normalize();
            //        var ply = MyCharacter.CreateCharacter(MatrixD.CreateTranslation(pos), vel * 100 * (MyRandom.Instance.NextFloat() * 0.5f + 0.5f), "ALIEN SPACE NINJA", MyCharacter.DefaultModel, null, null, false);
            //        m_plys.Add(ply);
            //        return true;
            //    });

            //AddShortcut(MyKeys.NumPad0, true, false, false, false,
            //    () => "Remove one spawned player: " + m_plys.Count,
            //    delegate
            //    {
            //        if (m_plys.Count == 0)
            //            return false;
            //        var ply = m_plys[0];
            //        m_plys.Remove(ply);
            //        ply.Close();
            //        return true;
            //    });

            AddShortcut(MyKeys.NumPad3, true, false, false, false, () => "Enable Meteor Debug Draw", delegate
            {
                MyDebugDrawSettings.DEBUG_DRAW_METEORITS_DIRECTIONS = true;
                return true;
            });

            AddShortcut(MyKeys.NumPad2, true, false, false, false, () => "Spawn meteor shower", delegate
            {
                MyMeteorShower.StartDebugWave(MySession.Static.LocalCharacter.WorldMatrix.Translation);
                return true;
            });

            AddShortcut(MyKeys.NumPad1, true, false, false, false, () => "Spawn small asteroid", delegate
            {
                Vector3 charpos = MySession.Static.LocalCharacter.WorldMatrix.Translation;
                Vector3 chardir = MySession.Static.LocalCharacter.WorldMatrix.Forward;
                MyMeteor.SpawnRandom(charpos + chardir * 2, chardir);
                return true;
            });

            AddShortcut(MyKeys.NumPad0, true, false, false, false, () => "Spawn crater", delegate
            {
                SpawnCrater();
                return true;
            });
        }

        private void SpawnCrater()
        {
            Vector3 myPosition = MySession.Static.LocalCharacter.WorldMatrix.Translation;
            Vector3 myDirection = MySession.Static.LocalCharacter.WorldMatrix.Forward;

            MyPhysics.CastRay(myPosition, myPosition + myDirection * 100);


        }

        public override bool HandleInput()
        {
            if (MySession.Static == null)
                return false;
            return base.HandleInput();
        }

        public override void Draw()
        {
            base.Draw();

            if (MySession.Static == null)
                return;
            if (MySector.MainCamera == null)
                return;

            if (MyProceduralWorldGenerator.Static == null)
                return;

            if (m_drawAroundCamera)
            {
                MyProceduralWorldGenerator.Static.OverlapAllPlanetSeedsInSphere(new BoundingSphereD(MySector.MainCamera.Position, MySector.MainCamera.FarPlaneDistance * 2), m_tmpSeedsList);
            }

            MyProceduralWorldGenerator.Static.GetAllExisting(m_tmpSeedsList);

            double max_distance = 100 * 2 * 60 * 60;
            foreach (var seed in m_tmpSeedsList)
            {
                if (!m_drawSeeds)
                    continue;

                var pos = seed.BoundingVolume.Center;

                VRageRender.MyRenderProxy.DebugDrawSphere(pos, seed.Size / 2, seed.Params.Type == MyObjectSeedType.Asteroid ? Color.Green : Color.Red, 1.0f, true);
                //if ((pos - MySector.MainCamera.Position).Length() < max_distance)
                {
                    if (m_drawRadius)
                    {
                        VRageRender.MyRenderProxy.DebugDrawText3D(pos, string.Format("{0:0}m", seed.Size), Color.Yellow, 0.8f, true);
                    }
                    if (m_drawDistance)
                    {
                        double distance = (pos - MySector.MainCamera.Position).Length();
                        VRageRender.MyRenderProxy.DebugDrawText3D(pos, string.Format("{0:0.0}km", distance / 1000), Color.Lerp(Color.Green, Color.Red, (float)(distance / max_distance)), 0.8f, true, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM);
                    }
                }
            }
            m_tmpSeedsList.Clear();

            if (m_drawTrackedEntities)
            {
                foreach (var kv in MyProceduralWorldGenerator.Static.GetTrackedEntities())
                {
                    VRageRender.MyRenderProxy.DebugDrawSphere(kv.Value.CurrentPosition, (float)(kv.Value.BoundingVolume.Radius), Color.White, 1.0f, true);
                }
            }

            if (m_drawCells)
            {
                MyProceduralWorldGenerator.Static.GetAllExistingCells(m_tmpCellsList);
                foreach (var cell in m_tmpCellsList)
                {
                    VRageRender.MyRenderProxy.DebugDrawAABB(cell.BoundingVolume, Color.Blue, 1f, 1f, true);
                }
            }
            m_tmpCellsList.Clear();

            VRageRender.MyRenderProxy.DebugDrawSphere(Vector3D.Zero, 0, Color.White, 0.0f, false);
        }

        List<MyObjectSeed> m_tmpSeedsList = new List<MyObjectSeed>();
        List<MyProceduralCell> m_tmpCellsList = new List<MyProceduralCell>();

        public override string GetName()
        {
            return "Asteroids";
        }
    }
}
