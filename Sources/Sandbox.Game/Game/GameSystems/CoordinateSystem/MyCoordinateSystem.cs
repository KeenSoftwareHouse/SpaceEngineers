using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Components.Session;
using VRage.Game.Definitions.SessionComponents;
using VRage.Game.Entity;
using VRage.Game.ObjectBuilders.Components;
using VRage.Library.Utils;
using VRage.Network;
using VRage.Serialization;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.GameSystems.CoordinateSystem
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, 1000, typeof(MyObjectBuilder_CoordinateSystem)), StaticEventOwner]
    public class MyCoordinateSystem : MySessionComponentBase
    {

        #region Events

        public static event Action OnCoordinateChange;

        #endregion

        #region Local Structs

        /// <summary>
        /// Used for transporting information about coord system to clients.
        /// </summary>
        private struct MyCreateCoordSysBuffer
        {
            public long Id;
            public Vector3D Position;

            [Serialize(MyPrimitiveFlags.Normalized)]
            public Quaternion Rotation;
        }

        public struct CoordSystemData
        {
            public MyTransformD SnappedTransform;
            public MyTransformD Origin;
            public Vector3D LocalSnappedPos;
        }

        #endregion

        #region Static & Const members

        public static MyCoordinateSystem Static;
        private double m_angleTolerance = 0.0001;
        private double m_positionTolerance = 0.001;
        private int m_coorsSystemSize = 1000;
        
        #endregion

        #region Data members

        private Dictionary<long, MyLocalCoordSys> m_localCoordSystems = new Dictionary<long, MyLocalCoordSys>();
        
        private long m_lastCoordSysId = 1;

        private bool m_drawBoundingBox = false;
        private long m_selectedCoordSys = 0;
        private long m_lastSelectedCoordSys = 0;
        private bool m_localCoordExist = false;
        private bool m_selectionChanged = false;
        private bool m_visible = false;

        #endregion

        #region Properties

        ///<summary>
        ///Returns id of selected coord system.
        ///</summary>
        public long SelectedCoordSys { get { return m_selectedCoordSys; } }

        ///<summary>
        ///Returns id of last selected coord system.
        ///</summary>
        public long LastSelectedCoordSys { get { return m_lastSelectedCoordSys; } }

        /// <summary>
        /// Indicates if there is any selected coord system.
        /// </summary>
        public bool LocalCoordExist { get { return this.m_localCoordExist; } }

        //public bool SelectionChanged { get { return this.m_selectionChanged; } }

        /// <summary>
        /// Indicates if LCS graphic representation is visible.
        /// </summary>
        public bool Visible { get { return this.m_visible; } set { m_visible = value; } }

        #endregion

        #region Constructor

        public MyCoordinateSystem()
        {
            Static = this;
            if(Sync.IsServer)
                MyEntities.OnEntityAdd += MyEntities_OnEntityCreate;

        }

        #endregion

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);

            var coordSysBuilder = sessionComponent as MyObjectBuilder_CoordinateSystem;

            this.m_lastCoordSysId = coordSysBuilder.LastCoordSysId;
            
            foreach(var coordSys in coordSysBuilder.CoordSystems)
            {
                MyTransformD origin = new MyTransformD();
                origin.Position = coordSys.Position;
                origin.Rotation = coordSys.Rotation;

                MyLocalCoordSys newCoordSys = new MyLocalCoordSys(origin, m_coorsSystemSize);
                newCoordSys.Id = coordSys.Id;

                m_localCoordSystems.Add(coordSys.Id, newCoordSys);

            }

        }

        public override void InitFromDefinition(MySessionComponentDefinition definition)
        {
            base.InitFromDefinition(definition);
            MyCoordinateSystemDefinition coordSysDef = definition as MyCoordinateSystemDefinition;

            if (coordSysDef == null)
            {
                Debug.Fail("Wrong definition, something is very wrong. Check SessionComponent.sbc");
            }

            this.m_coorsSystemSize = coordSysDef.CoordSystemSize;
            this.m_angleTolerance = coordSysDef.AngleTolerance;
            this.m_positionTolerance = coordSysDef.PositionTolerance;
        }

        public override void LoadData()
        {
            base.LoadData();
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            this.m_lastCoordSysId = 1;
            this.m_localCoordSystems.Clear();
            this.m_drawBoundingBox = false;
            this.m_selectedCoordSys = 0;
            this.m_lastSelectedCoordSys = 0;
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            var newBuilder = base.GetObjectBuilder() as MyObjectBuilder_CoordinateSystem;

            newBuilder.LastCoordSysId = this.m_lastCoordSysId;

            foreach(KeyValuePair<long, MyLocalCoordSys> pair in m_localCoordSystems)
            {
                MyObjectBuilder_CoordinateSystem.CoordSysInfo coordSysToSave = new MyObjectBuilder_CoordinateSystem.CoordSysInfo();
                coordSysToSave.Id = pair.Value.Id;
                coordSysToSave.EntityCount = pair.Value.EntityConuter;
                coordSysToSave.Position = pair.Value.Origin.Position;
                coordSysToSave.Rotation = pair.Value.Origin.Rotation;

                newBuilder.CoordSystems.Add(coordSysToSave);
            }

            return newBuilder;
        }

        /// <summary>
        /// Returns closest local coord system.
        /// </summary>
        /// <param name="position">World position.</param>
        /// <returns>Local coord system.</returns>
        private MyLocalCoordSys GetClosestCoordSys(ref Vector3D position, bool checkContain = true)
        {
            MyLocalCoordSys closestOne = null;
            double closestDistanceSq = double.MaxValue;

            foreach (MyLocalCoordSys coordSys in m_localCoordSystems.Values)
            {

                if (!checkContain || coordSys.Contains(ref position))
                {

                    double distnaceSq = (coordSys.Origin.Position - position).LengthSquared();
                    if (distnaceSq < closestDistanceSq)
                    {
                        closestOne = coordSys;
                        closestDistanceSq = distnaceSq;
                    }
                }
            }

            return closestOne;
        }

        /// <summary>
        /// Method on client that is called by server. Will Trigger creation of coord system.
        /// </summary>
        /// <param name="transform">Origin transform.</param>
        /// <param name="coordSysId">Coord system id.</param>
        [Event, Reliable, BroadcastExcept]
        private static void CoordSysCreated_Client(MyCreateCoordSysBuffer createBuffer)
        {
            MyTransformD origin = new MyTransformD();
            origin.Position = createBuffer.Position;
            origin.Rotation = createBuffer.Rotation;
            MyCoordinateSystem.Static.CreateCoordSys_ClientInternal(ref origin, createBuffer.Id);
        }

        /// <summary>
        /// Only creates coord system. Call only on client in reaction on server create.
        /// </summary>
        /// <param name="transform">Origin of the coord system.</param>
        /// <param name="coordSysId">Coord system id that should be used in creation.</param>
        private void CreateCoordSys_ClientInternal(ref MyTransformD transform, long coordSysId)
        {
            MyLocalCoordSys localCoordSys = new MyLocalCoordSys(transform, m_coorsSystemSize);
            localCoordSys.Id = coordSysId;
            m_localCoordSystems.Add(coordSysId, localCoordSys);
        }

        /// <summary>
        /// Creates coord system and sends it to clients. Should be called only on server.
        /// </summary>
        /// <param name="cubeGrid">Cube grid that is an origin.</param>
        /// <param name="staticGridAlignToCenter">Indcates if grid should be aligned to center or no.</param>
        public void CreateCoordSys(MyCubeGrid cubeGrid, bool staticGridAlignToCenter, bool sync = false)
        {
            //In me this system is not working for now (will change after implementing planets there)
            //if(MyPerGameSettings.Game == GameEnum.ME_GAME)
            //{
            //    return;
            //}
            
            Debug.Assert(Sync.IsServer, "Called on client. This method should be called only on server.");

            MyTransformD origin = new MyTransformD(cubeGrid.PositionComp.WorldMatrix);
            origin.Rotation.Normalize();
            float gridSize = cubeGrid.GridSize;

            if (!staticGridAlignToCenter)
            {
                origin.Position -= (origin.Rotation.Forward + origin.Rotation.Right + origin.Rotation.Up) * gridSize * 0.5f;
            }

            MyLocalCoordSys localCoordSys = new MyLocalCoordSys(origin, m_coorsSystemSize);
            long newId = m_lastCoordSysId++; // Just raise by one. There wont be so much id's for long to be overflooded.
            localCoordSys.Id = newId;
            m_localCoordSystems.Add(newId, localCoordSys);

            if (cubeGrid.LocalCoordSystem != 0)
                this.UnregisterCubeGrid(cubeGrid);

            this.RegisterCubeGrid(cubeGrid, localCoordSys);

            MyCreateCoordSysBuffer createCoordSysBuffer = new MyCreateCoordSysBuffer();
            createCoordSysBuffer.Position = origin.Position;
            createCoordSysBuffer.Rotation = origin.Rotation;
            createCoordSysBuffer.Id = newId;

            if(sync)
                MyMultiplayer.RaiseStaticEvent(x => CoordSysCreated_Client, createCoordSysBuffer);

        }

        public static void GetPosRoundedToGrid(ref Vector3D vecToRound, double gridSize, bool isStaticGridAlignToCenter)
        {
            if (isStaticGridAlignToCenter)
            {
                vecToRound = Vector3I.Round(vecToRound / gridSize) * gridSize;
            }
            else
            {
                vecToRound = Vector3I.Round(vecToRound / gridSize + 0.5) * gridSize - 0.5 * gridSize;
            }
        }

        [Event, Reliable, Broadcast]
        private static void CoorSysRemoved_Client(long coordSysId)
        {
            MyCoordinateSystem.Static.RemoveCoordSys(coordSysId);
        }

        /// <summary>
        /// Removes coord system.
        /// </summary>
        /// <param name="coordSysId">Coord system id.</param>
        private void RemoveCoordSys(long coordSysId)
        {
            this.m_localCoordSystems.Remove(coordSysId);
        }

        void MyEntities_OnEntityCreate(MyEntity obj)
        {
            var cubeGrid = obj as MyCubeGrid;
            // If there is local coor sys under the cube. Try register
            if(cubeGrid != null && cubeGrid.LocalCoordSystem != 0)
            {
                MyLocalCoordSys coordSys = this.GetCoordSysById(cubeGrid.LocalCoordSystem);

                if (coordSys != null)
                {
                    RegisterCubeGrid(cubeGrid, coordSys);
                }
            }
        }

        /// <summary>
        /// Registers cube grid under closest coord system.
        /// </summary>
        /// <param name="cubeGrid">Cube grid to register.</param>
        public void RegisterCubeGrid(MyCubeGrid cubeGrid)
        {

            if(cubeGrid.LocalCoordSystem != 0)
                return;

            Vector3D worldPos = cubeGrid.PositionComp.GetPosition();
            MyLocalCoordSys localCoordSys = this.GetClosestCoordSys(ref worldPos);
            if (localCoordSys == null)
                return;
            this.RegisterCubeGrid(cubeGrid, localCoordSys);
        }

        /// <summary>
        /// Registers cube grid under given local coord system.
        /// </summary>
        /// <param name="cubeGrid">Cube grid to register.</param>
        /// <param name="coordSys">Local coord system.</param>
        private void RegisterCubeGrid(MyCubeGrid cubeGrid, MyLocalCoordSys coordSys)
        {

            cubeGrid.OnClose += CubeGrid_OnClose;
            cubeGrid.OnPhysicsChanged += CubeGrid_OnPhysicsChanged;
            cubeGrid.LocalCoordSystem = coordSys.Id;
            coordSys.EntityConuter++;
        }

        /// <summary>
        /// Unregisters cube grid from given local coord system.
        /// </summary>
        /// <param name="cubeGrid">Cube grid to be unregistered.</param>
        private void UnregisterCubeGrid(MyCubeGrid cubeGrid)
        {
            cubeGrid.OnClose -= CubeGrid_OnClose;
            cubeGrid.OnPhysicsChanged -= CubeGrid_OnPhysicsChanged;

            long coordSysId = cubeGrid.LocalCoordSystem;
            MyLocalCoordSys coordSys = this.GetCoordSysById(coordSysId);

            // reset local coord after unregistering.
            cubeGrid.LocalCoordSystem = 0;

            if (coordSys == null)
                return;

            coordSys.EntityConuter--;
             
            // Empty coord system, Remove it.
            if(coordSys.EntityConuter <= 0)
            {
                this.RemoveCoordSys(coordSys.Id);
                MyMultiplayer.RaiseStaticEvent(x => CoorSysRemoved_Client, coordSysId);
            }
        }

        private void CubeGrid_OnPhysicsChanged(MyEntity obj)
        {
            Debug.Assert(Sync.IsServer, "Called on client. This method should be called only on server.");

            MyCubeGrid cubeGrid = obj as MyCubeGrid;
            if (cubeGrid != null && !cubeGrid.IsStatic)
            {
                this.UnregisterCubeGrid(cubeGrid);
            }
        }

        private void CubeGrid_OnClose(MyEntity obj)
        {
            Debug.Assert(Sync.IsServer, "Called on client. This method should be called only on server.");

            MyCubeGrid cubeGrid = obj as MyCubeGrid;
            if (cubeGrid != null)
            {
                this.UnregisterCubeGrid(cubeGrid);
            }

        }

        private MyLocalCoordSys GetCoordSysById(long id)
        {
            if(m_localCoordSystems.ContainsKey(id))
            {
                return m_localCoordSystems[id];
            }

            //Debug.Fail("Probably desynch!");
            return null;

        }
        
        /// <summary>
        /// Converts world position to be snapped to closest grid.
        /// </summary>
        /// <param name="worldPos">World position.</param>
        /// <param name="gridSize">Grid size.</param>
        /// <param name="staticGridAlignToCenter">Is grid align to static.</param>
        /// <returns></returns>
        public CoordSystemData SnapWorldPosToClosestGrid(ref Vector3D worldPos, double gridSize, bool staticGridAlignToCenter)
        {

            m_lastSelectedCoordSys = m_selectedCoordSys;

            MyLocalCoordSys localCoordSys = null;

            localCoordSys = this.GetClosestCoordSys(ref worldPos);

            // If no coord sys found, return origin(0,0,0) with no rotation!
            if (localCoordSys == null)
            {
                localCoordSys = new MyLocalCoordSys(m_coorsSystemSize);
                m_selectedCoordSys = 0;
            }
            else
                m_selectedCoordSys = localCoordSys.Id;

            if (m_selectedCoordSys == 0)
            {
                m_localCoordExist = false;
            }
            else
            {
                m_localCoordExist = true;
            }

            if (m_selectedCoordSys != m_lastSelectedCoordSys)
            {
                m_selectionChanged = true;
                if(OnCoordinateChange != null)
                    OnCoordinateChange();
            }
            else
                m_selectionChanged = false;

            //if (!m_localCoordExist && m_selectionChanged)
            //{
            //    this.Disable();
            //}
            //else if (m_selectionChanged && m_lastSelectedCoordSys == 0)
            //{
            //    this.Enable();
            //}

            CoordSystemData coordData = new CoordSystemData();

            Quaternion rotation = localCoordSys.Origin.Rotation;
            Quaternion invRotation = Quaternion.Inverse(rotation);
            Vector3D position = localCoordSys.Origin.Position;

            Vector3D vec = worldPos - position;
            vec = Vector3D.Transform(vec, invRotation);

            MyCoordinateSystem.GetPosRoundedToGrid(ref vec, gridSize, staticGridAlignToCenter);

            coordData.LocalSnappedPos = vec;

            vec = Vector3D.Transform(vec, rotation);

            MyTransformD localCoordsTransform = new MyTransformD();
            localCoordsTransform.Position = position + vec;
            localCoordsTransform.Rotation = rotation;

            coordData.SnappedTransform = localCoordsTransform;
            coordData.Origin = localCoordSys.Origin;

            return coordData;
        }

        /// <summary>
        /// Indicates if position is inside of local coordinates area.
        /// </summary>
        /// <param name="worldPos">World position.</param>
        /// <returns>If true, position is inside of closest local coordinate system.</returns>
        public bool IsAnyLocalCoordSysExist(ref Vector3D worldPos)
        {
            foreach (MyLocalCoordSys coordSys in m_localCoordSystems.Values)
            {
                if (coordSys.Contains(ref worldPos))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Naive way of checking if transform is snaped to coord sys. In second iteration update for better way.
        /// </summary>
        /// <param name="tranform">Transform to check</param>
        /// <returns>Indicates if transform is aligned correctly to any grid system.</returns>
        public bool IsLocalCoordSysExist(ref MatrixD tranform, double gridSize)
        {
            foreach (MyLocalCoordSys coordSys in m_localCoordSystems.Values)
            {
                Vector3D position = tranform.Translation;
                
                if (coordSys.Contains(ref position))
                {

                    double dotProductForward = Math.Abs(Vector3D.Dot(coordSys.Origin.Rotation.Forward, tranform.Forward));
                    double dotProductUp = Math.Abs(Vector3D.Dot(coordSys.Origin.Rotation.Up, tranform.Up));

                    if ((dotProductForward < m_angleTolerance || dotProductForward > 1.0 - m_angleTolerance) &&
                        (dotProductUp < m_angleTolerance || dotProductUp > 1.0 - m_angleTolerance))
                    {
                        
                        Vector3D relativeToOriginWorld = position - coordSys.Origin.Position;
                        Quaternion rotation = coordSys.Origin.Rotation;
                        Quaternion invRotation = Quaternion.Inverse(rotation);
                        Vector3D vec = Vector3D.Transform(relativeToOriginWorld, invRotation);
                        double halfGridSize = (gridSize / 2.0);
                        double xDif = Math.Abs(vec.X % halfGridSize);
                        double yDif = Math.Abs(vec.Y % halfGridSize);
                        double zDif = Math.Abs(vec.Z % halfGridSize);

                        if ((xDif < m_positionTolerance || xDif > halfGridSize - m_positionTolerance) &&
                            (yDif < m_positionTolerance || yDif > halfGridSize - m_positionTolerance) &&
                            (zDif < m_positionTolerance || zDif > halfGridSize - m_positionTolerance))
                            return true;
                    }
                    
                }

            }

            return false;
        }

        /// <summary>
        /// Sets last and current selected coordinate system to none.
        /// </summary>
        public void ResetSelection()
        {
            this.m_lastSelectedCoordSys = 0;
            this.m_selectedCoordSys = 0;
            this.m_drawBoundingBox = false;
        }

        //public void Enable()
        //{
        //    this.m_visible = true;
        //}

        //public void Disable()
        //{
        //    this.m_visible = false;
        //}

        #region Drawing

        public override void Draw()
        {

            if (!m_visible)
                return;

            if (this.m_selectedCoordSys == 0)
            {
                this.m_drawBoundingBox = false;
            }
            else if (this.m_selectedCoordSys != 0)
            {
                this.m_drawBoundingBox = true;
            }


            if (m_drawBoundingBox)
            {
                MyLocalCoordSys coordSys = this.GetCoordSysById(m_selectedCoordSys);
                if (coordSys != null)
                {
                    coordSys.Draw();
                }

            }

            base.Draw();

            //if (!MyFakes.ENABLE_DEBUG_DRAW_COORD_SYS)
            //    return;

            // DEBUG DRAW BELOW

            //foreach (MyLocalCoordSys coordSys in m_localCoordSystems.Values)
            //{
            //    MyRenderProxy.DebugDrawSphere(coordSys.Origin.Position, 0.05f, Color.Orange, 1.0f, false);
            //    MyRenderProxy.DebugDrawLine3D(coordSys.Origin.Position, coordSys.Origin.Position + coordSys.Origin.TransformMatrix.Forward, Color.Blue, Color.Blue, false);
            //    MyRenderProxy.DebugDrawLine3D(coordSys.Origin.Position, coordSys.Origin.Position + coordSys.Origin.TransformMatrix.Up, Color.Green, Color.Green, false);
            //    MyRenderProxy.DebugDrawLine3D(coordSys.Origin.Position, coordSys.Origin.Position + coordSys.Origin.TransformMatrix.Right, Color.Red, Color.Red, false);

            //    MyRenderProxy.DebugDrawOBB(coordSys.BoundingBox, Color.Orange, 0.1f, true, false);
            //}


        }

        /// <summary>
        /// Gets local coordinate system indication color.
        /// </summary>
        /// <param name="coordSysId">Local coordinate system ID.</param>
        /// <returns>Indication color.</returns>
        public Color GetCoordSysColor(long coordSysId)
        {
            if (m_localCoordSystems.ContainsKey(coordSysId))
            {
                return m_localCoordSystems[coordSysId].RenderColor;
            }

            Debug.Fail("Coord system does not exist");
            return Color.White;
        }

        #endregion

    }

}
