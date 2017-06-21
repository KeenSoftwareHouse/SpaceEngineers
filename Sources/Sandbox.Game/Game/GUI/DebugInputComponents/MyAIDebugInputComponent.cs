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
using VRage.Game.Entity;
using VRageRender;
using VRageRender.Utils;

namespace Sandbox.Game.Gui
{
    /// <summary>
    /// AI Debug Input class (base on Cestmir Debug Input)
    /// </summary>
    [StaticEventOwner]
    public class MyAIDebugInputComponent : MyDebugComponent
    {
        private bool m_drawSphere = false;
        private BoundingSphere m_sphere;
        private Matrix m_sphereMatrix;
        private string m_string;
        private Vector3D m_point1;
        private Vector3D m_point2;

        private MySmartPath m_smartPath;
        private Vector3D m_currentTarget;
        private List<Vector3D> m_pastTargets = new List<Vector3D>();

        public static int FaceToRemove;
        public static int BinIndex = -1;

        private struct DebugDrawPoint
        {
            public Vector3D Position;
            public Color Color;
        }
        private static List<DebugDrawPoint> DebugDrawPoints = new List<DebugDrawPoint>();

        private struct DebugDrawSphere
        {
            public Vector3D Position;
            public float Radius;
            public Color Color;
        }
        private static List<DebugDrawSphere> DebugDrawSpheres = new List<DebugDrawSphere>();

        private struct DebugDrawBox
        {
            public BoundingBoxD Box;
            public Color Color;
        }
        private static List<DebugDrawBox> DebugDrawBoxes = new List<DebugDrawBox>();

        private static MyWingedEdgeMesh DebugDrawMesh = null;
        private static List<MyPolygon> DebugDrawPolys = new List<MyPolygon>();

        public static List<BoundingBoxD> Boxes = null;

        public MyAIDebugInputComponent()
        {
            if (MyPerGameSettings.EnableAi)
            {
                AddShortcut(MyKeys.NumPad0, true, false, false, false, () => "Toggle Draw Grid Physical Mesh", ToggleDrawPhysicalMesh);

                AddShortcut(MyKeys.NumPad1, true, false, false, false, () => "Add bot", AddBot);
                AddShortcut(MyKeys.NumPad2, true, false, false, false, () => "Remove bot", RemoveBot);

                AddShortcut(MyKeys.NumPad4, true, false, false, false, () => "Toggle Draw Debug", ToggleDrawDebug);
                AddShortcut(MyKeys.NumPad5, true, false, false, false, () => "Toggle Wireframe", ToggleWireframe);
                AddShortcut(MyKeys.NumPad6, true, false, false, false, () => "Set PF target", SetPathfindingDebugTarget);

                AddShortcut(MyKeys.NumPad7, true, false, false, false, () => "Toggle Draw Navmesh", ToggleDrawNavmesh);
                AddShortcut(MyKeys.NumPad8, true, false, false, false, () => "Generate Navmesh Tile", GenerateNavmeshTile);
                AddShortcut(MyKeys.NumPad9, true, false, false, false, () => "Invalidate Navmesh Position", InvalidateNavmeshPosition);
            }

            // DEBUG ONLY
            // REMOVES foliage
            /*MyRenderProxy.Settings.TerrainDetailD0 = 0;
            MyRenderProxy.Settings.TerrainDetailD1 = 0;
            MyRenderProxy.Settings.TerrainDetailD2 = 0;
            MyRenderProxy.Settings.TerrainDetailD3 = 0;
            MyRenderProxy.Settings.GrassMaxDrawDistance = 0;
            MyRenderProxy.Settings.GrassPostprocessCloseDistance = 0;
            MyRenderProxy.Settings.GrassGeometryClippingDistance = 0;
            MyRenderProxy.Settings.GrassGeometryScalingNearDistance = 0;
            MyRenderProxy.Settings.GrassGeometryScalingFarDistance = 0;
            MyRenderProxy.Settings.GrassGeometryDistanceScalingFactor = 0;
            MyRenderProxy.Settings.WindStrength = 0;
            MyRenderProxy.SetSettingsDirty();*/
        }

        private static bool m_drawDebug = false;
        private bool ToggleDrawDebug()
        {
            m_drawDebug = !m_drawDebug;
            MyAIComponent.Static.PathfindingSetDrawDebug(m_drawDebug);
            return true;
        }

        private bool ToggleWireframe()
        {
            MyRenderProxy.Settings.Wireframe = !MyRenderProxy.Settings.Wireframe;

            return true;
        }


        private bool SetPathfindingDebugTarget()
        {
            Vector3D? position = GetTargetPosition();
            MyAIComponent.Static.SetPathfindingDebugTarget(position);

            return true;
        }

        private bool GenerateNavmeshTile()
        {
            Vector3D? position = GetTargetPosition();
            MyAIComponent.Static.GenerateNavmeshTile(position);

            return true;
        }

        private bool InvalidateNavmeshPosition()
        {
            Vector3D? position = GetTargetPosition();
            MyAIComponent.Static.InvalidateNavmeshPosition(position);

            return true;
        }
        

        private static bool m_drawNavesh = false;
        private bool ToggleDrawNavmesh()
        {
            m_drawNavesh = !m_drawNavesh;
            MyAIComponent.Static.PathfindingSetDrawNavmesh(m_drawNavesh);
            return true;
        }

        private static bool m_drawPhysicalMesh = false;
        private bool ToggleDrawPhysicalMesh()
        {
            m_drawPhysicalMesh = !m_drawPhysicalMesh;
            return true;
        }


        /// <summary>
        /// Obtain position where the player is aiming/looking at.
        /// </summary>
        private Vector3D? GetTargetPosition()
        {
            var line = new LineD(MySector.MainCamera.Position, MySector.MainCamera.Position + MySector.MainCamera.ForwardVector * 1000);

            List<MyPhysics.HitInfo> tmpHitList = new List<MyPhysics.HitInfo>();

            MyPhysics.CastRay(line.From, line.To, tmpHitList, MyPhysics.CollisionLayers.DefaultCollisionLayer);
            // Remove character hits.
            tmpHitList.RemoveAll(delegate(MyPhysics.HitInfo hit)
            {
                return (hit.HkHitInfo.GetHitEntity() == MySession.Static.ControlledEntity.Entity);
            });

            if (tmpHitList.Count == 0)
                return null;

            return tmpHitList[0].Position;
        }

        private bool AddBot()
        {
            MyAgentDefinition bot;
            if (MyPerGameSettings.Game == GameEnum.SE_GAME)
                bot = MyDefinitionManager.Static.GetBotDefinition(new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.AI.Bot.MyObjectBuilder_AnimalBot), "Wolf")) as MyAgentDefinition;
            else
                bot = MyDefinitionManager.Static.GetBotDefinition(new MyDefinitionId(typeof(VRage.Game.ObjectBuilders.AI.Bot.MyObjectBuilder_HumanoidBot), "NormalBarbarian")) as MyAgentDefinition;

            //bot.BotBehaviorTree = new MyDefinitionId(bot.BotBehaviorTree.GetType(), "CyberhoundBehavior");
            MyAIComponent.Static.TrySpawnBot(bot);

            return true;
        }

        private bool RemoveBot()
        {
            int highestExistingPlayer = -1;

            var players = Sync.Players.GetOnlinePlayers();
            foreach (var player in players)
            {
                if (player.Id.SteamId == Sync.MyId)

                    highestExistingPlayer = Math.Max(highestExistingPlayer, player.Id.SerialId);
            }

            if (highestExistingPlayer > 0)
            {
                var player = Sync.Players.GetPlayerById(new MyPlayer.PlayerId(Sync.MyId, highestExistingPlayer));
                Sync.Players.RemovePlayer(player);
            }

            return true;
        }

        #region Public Methods
        public override string GetName()
        {
            return "A.I.";
        }

        public override bool HandleInput()
        {
            if (MySession.Static == null)
                return false;

            if (MyScreenManager.GetScreenWithFocus() is MyGuiScreenDialogPrefabCheat) return false;
            if (MyScreenManager.GetScreenWithFocus() is MyGuiScreenDialogRemoveTriangle) return false;
            if (MyScreenManager.GetScreenWithFocus() is MyGuiScreenDialogViewEdge) return false;

            return base.HandleInput();
        }


        public static void AddDebugPoint(Vector3D point, Color color)
        {
            DebugDrawPoints.Add(new DebugDrawPoint() { Position = point, Color = color });
        }

        public static void ClearDebugPoints()
        {
            DebugDrawPoints.Clear();
        }

        public static void AddDebugSphere(Vector3D position, float radius, Color color)
        {
            DebugDrawSpheres.Add(new DebugDrawSphere() { Position = position, Radius = radius, Color = color });
        }

        public static void ClearDebugSpheres()
        {
            DebugDrawSpheres.Clear();
        }

        public static void AddDebugBox(BoundingBoxD box, Color color)
        {
            DebugDrawBoxes.Add(new DebugDrawBox() { Box = box, Color = color });
        }

        public static void ClearDebugBoxes()
        {
            DebugDrawBoxes.Clear();
        }

        public override void Draw()
        {
            base.Draw();

            if (MySector.MainCamera != null)
            {
                var pos = MySector.MainCamera.Position;
                var dir = MySector.MainCamera.ForwardVector;
                var hit = MyPhysics.CastRay(pos, pos + 500 * dir);
                if (hit.HasValue)
                {
                    var entity = hit.Value.HkHitInfo.GetHitEntity();
                    if (entity != null)
                    {
                        var voxel = entity.GetTopMostParent() as MyVoxelPhysics;
                        if (voxel != null)
                        {
                            var planet = voxel.Parent;
                            var grav = planet as IMyGravityProvider;
                            if (grav != null)
                            {
                                var gravity = grav.GetWorldGravity(hit.Value.Position);
                                gravity.Normalize();
                                var point = planet.PositionComp.GetPosition() - gravity * 9503;
                                MyRenderProxy.DebugDrawSphere(point, 0.5f, Color.Red, 1, false);
                                MyRenderProxy.DebugDrawSphere(point, 5.5f, Color.Yellow, 1, false);
                                hit = MyPhysics.CastRay(point, point + gravity * 500);
                                if (hit.HasValue)
                                    MyRenderProxy.DebugDrawText2D(new Vector2(10, 10), (hit.Value.HkHitInfo.HitFraction * 500).ToString(), Color.White, 0.8f);
                            }
                        }
                    }
                }
            }
            if (!MyDebugDrawSettings.ENABLE_DEBUG_DRAW) return;

            if (MyCubeBuilder.Static == null) return;

            if (m_smartPath != null)
            {
                m_smartPath.DebugDraw();
                VRageRender.MyRenderProxy.DebugDrawSphere(m_currentTarget, 2.0f, Color.HotPink, 1.0f, false);
                for (int i = 1; i < m_pastTargets.Count; ++i)
                {
                    VRageRender.MyRenderProxy.DebugDrawLine3D(m_pastTargets[i], m_pastTargets[i - 1], Color.Blue, Color.Blue, false);
                }
            }

            var bb = MyCubeBuilder.Static.GetBuildBoundingBox();
            VRageRender.MyRenderProxy.DebugDrawOBB(bb, Color.Red, 0.25f, false, false);

            var src = MyScreenManager.GetScreenWithFocus();

            if (MyScreenManager.GetScreenWithFocus() == null || MyScreenManager.GetScreenWithFocus().DebugNamePath != "MyGuiScreenGamePlay") return;

            if (m_drawSphere)
            {
                VRageRender.MyRenderProxy.DebugDrawSphere(m_sphere.Center, m_sphere.Radius, Color.Red, 1.0f, false, cull: true);
                VRageRender.MyRenderProxy.DebugDrawAxis(m_sphereMatrix, 50.0f, false);
                VRageRender.MyRenderProxy.DebugDrawText2D(new Vector2(200.0f, 0.0f), m_string, Color.Red, 0.5f);
            }


            VRageRender.MyRenderProxy.DebugDrawSphere(m_point1, 0.5f, Color.Orange.ToVector3(), 1.0f, true);
            VRageRender.MyRenderProxy.DebugDrawSphere(m_point2, 0.5f, Color.Orange.ToVector3(), 1.0f, true);

            foreach (var point in DebugDrawPoints)
            {
                //VRageRender.MyRenderProxy.DebugDrawSphere(point.Position, 0.05f, point.Color.ToVector3(), 1.0f, false);
                VRageRender.MyRenderProxy.DebugDrawSphere(point.Position, 0.03f, point.Color, 1.0f, false);
            }

            foreach (var sphere in DebugDrawSpheres)
            {
                VRageRender.MyRenderProxy.DebugDrawSphere(sphere.Position, sphere.Radius, sphere.Color, 1.0f, false);
            }

            foreach (var box in DebugDrawBoxes)
            {
                VRageRender.MyRenderProxy.DebugDrawAABB(box.Box, box.Color, 1.0f, 1.0f, false);
            }

            if (DebugDrawMesh != null)
            {
                Matrix identity = Matrix.Identity;
                DebugDrawMesh.DebugDraw(ref identity, MyDebugDrawSettings.DEBUG_DRAW_NAVMESHES);
            }

            foreach (var poly in DebugDrawPolys)
            {
                MatrixD identity = MatrixD.Identity;
                poly.DebugDraw(ref identity);
            }

            MyPolygonBoolOps.Static.DebugDraw(MatrixD.Identity);

            if (Boxes != null)
            {
                foreach (var box in Boxes)
                {
                    VRageRender.MyRenderProxy.DebugDrawAABB(box, Color.Red, 1.0f, 1.0f, true);
                }
            }
        }
        #endregion
    }
}
