using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.AI.Pathfinding
{
    /// <summary>
    /// Class that contains the OBBs that are used to obtain tiled mesh (ground and grid), used by Pathfinding
    /// </summary>
    public class MyNavmeshOBBs
    {
        #region OBBCoords struct
        public struct OBBCoords
        {
            public Vector2I Coords;
            public MyOrientedBoundingBoxD OBB;
        }
        #endregion

        #region Fields
        private const int NEIGHBOUR_OVERLAP_TILES = 2;

        private MyOrientedBoundingBoxD?[][] m_obbs;
        private float m_tileHalfSize, m_tileHalfHeight;
        MyPlanet m_planet;
        int m_middleCoord;
        #endregion

        #region Properties
        public int OBBsPerLine { get; private set; }

        public MyOrientedBoundingBoxD BaseOBB { get; private set; }

        public MyOrientedBoundingBoxD CenterOBB 
        { 
            get { return m_obbs[m_middleCoord][m_middleCoord].Value; }
            private set { m_obbs[m_middleCoord][m_middleCoord] = value; }
        }

        public List<Vector3D> NeighboursCenters { get; private set; }
        #endregion

        #region Contructor
        public MyNavmeshOBBs(MyPlanet planet, Vector3D centerPoint, Vector3D forwardDirection, int obbsPerLine, int tileSize, int tileHeight)
        {
            m_planet = planet;

            // There will always be an odd number of obbs in a line
            OBBsPerLine = obbsPerLine;
            if (OBBsPerLine % 2 == 0)
                OBBsPerLine += 1;

            m_middleCoord = (OBBsPerLine - 1) / 2;

            m_tileHalfSize = tileSize * 0.5f;
            m_tileHalfHeight = tileHeight * 0.5f;

            m_obbs = new MyOrientedBoundingBoxD?[OBBsPerLine][];
            for (int i = 0; i < OBBsPerLine; i++)
                m_obbs[i] = new MyOrientedBoundingBoxD?[OBBsPerLine];

            Initialize(centerPoint, forwardDirection);

            BaseOBB = GetBaseOBB();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Return the OBB at the specific coordinate or null, if is out of bounds
        /// </summary>
        public MyOrientedBoundingBoxD? GetOBB(int coordX, int coordY)
        {
            if (coordX < 0 || coordX >= OBBsPerLine ||
                coordY < 0 || coordY >= OBBsPerLine)
                return null;

            // The coordinates in the matrix are swapped ...
            return m_obbs[coordX][coordY];
        }

        /// <summary>
        /// Return the OBB at the specific position, or null, if is out of bounds
        /// </summary>
        public MyOrientedBoundingBoxD? GetOBB(Vector3D worldPosition)
        {
            // TODO: search needs to get smarter
            foreach (var obbLine in m_obbs)
                foreach (var obb in obbLine)
                {
                    /*
                    Vector3D diff = obb.Value.Center - worldPosition;
                    if (Math.Abs(diff.X) <= obb.Value.HalfExtent.X &&
                        Math.Abs(diff.Y) <= obb.Value.HalfExtent.Y &&
                        Math.Abs(diff.Z) <= obb.Value.HalfExtent.Z)
                     */
                    if (obb.Value.Contains(ref worldPosition))
                        return obb;
                }
            return null;
        }

        /// <summary>
        /// Return the OBB coord at the specific coordinate or null, if is out of bounds
        /// </summary>
        public OBBCoords? GetOBBCoord(int coordX, int coordY)
        {
            if (coordX < 0 || coordX >= OBBsPerLine ||
                coordY < 0 || coordY >= OBBsPerLine)
                return null;

            // The coordinates in the matrix are swapped ...
            return new OBBCoords { OBB = m_obbs[coordX][coordY].Value, Coords = new Vector2I(coordX, coordY) };
        }

        /// <summary>
        /// Return the OBB coord at the specific position, or null, if is out of bounds
        /// </summary>
        public OBBCoords? GetOBBCoord(Vector3D worldPosition)
        {
            // TODO: search needs to get smarter
            for (int i = 0; i < m_obbs.Length; i++)
                for (int j = 0; j < m_obbs[0].Length; j++)
                {
                    var obb = m_obbs[i][j].Value;
                    /*
                    Vector3D diff = obb.Center - worldPosition;
                    if (Math.Abs(diff.X) <= obb.HalfExtent.X &&
                        Math.Abs(diff.Y) <= obb.HalfExtent.Y &&
                        Math.Abs(diff.Z) <= obb.HalfExtent.Z)
                     * */
                    if (obb.Contains(ref worldPosition))
                        return new OBBCoords { OBB = obb, Coords = new Vector2I(i,j) };
                }
            return null;
        }

        /// <summary>
        /// Returns a list of OBBs intersected by a line
        /// </summary>
        public List<OBBCoords> GetIntersectedOBB(LineD line)
        {
            Dictionary<OBBCoords, double> intersectedOBBs = new Dictionary<OBBCoords, double>();

            //TODO: don't search in all OBBs but start with one that intersect the line and expand around this until all its neighbours don't intersect it

            for (int i = 0; i < m_obbs.Length; i++)
                for (int j = 0; j < m_obbs[0].Length; j++)
                    if (m_obbs[i][j].Value.Contains(ref line.From) ||
                        m_obbs[i][j].Value.Contains(ref line.To) ||
                        m_obbs[i][j].Value.Intersects(ref line).HasValue)
                        // Coords are swapped
                        intersectedOBBs.Add(new OBBCoords{ OBB = m_obbs[i][j].Value, Coords = new Vector2I(i,j) }, Vector3D.Distance(line.From, m_obbs[i][j].Value.Center));
            /*
            foreach (var obbLine in m_obbs)
                foreach (var obb in obbLine)
                    if (obb.Value.Contains(ref line.From) ||
                        obb.Value.Contains(ref line.To) ||
                        obb.Value.Intersects(ref line).HasValue)
                        intersectedOBBs.Add(obb.Value, Vector3D.Distance(line.From, obb.Value.Center));
            */
            return intersectedOBBs.OrderBy(d => d.Value)
                                  .Select(kvp => kvp.Key)
                                  .ToList();
        }

        /*
        /// <summary>
        /// Returns the coordinates of the OBB
        /// </summary>
        public bool GetCoords(MyOrientedBoundingBoxD obb, out int xCoord, out int yCoord)
        {
            xCoord = yCoord = -1;

            for (int i = 0; i < m_obbs.Length; i++)
                for (int j = 0; j < m_obbs[0].Length; j++)
                    if (obb == m_obbs[i][j])
                    {
                        // Coordinates are swapped
                        xCoord = i;
                        yCoord = j;
                        return true;
                    }
            return false;
        }
        */

        /// <summary>
        /// Debug draws the OBBs
        /// </summary>
        public void DebugDraw()
        {
            for (int i = 0; i < m_obbs.Length; i++)
                for (int j = 0; j < m_obbs[0].Length; j++)
                    if (m_obbs[i][j].HasValue)
                        MyRenderProxy.DebugDrawOBB(m_obbs[i][j].Value, Color.Red, 0, true, false);

            MyRenderProxy.DebugDrawOBB(BaseOBB, Color.White, 0, true, false);

            if (m_obbs[0][0].HasValue)
                MyRenderProxy.DebugDrawSphere(m_obbs[0][0].Value.Center, 5f, Color.Yellow, 0, true, false);

            //if (m_obbs[m_middleCoord][0].HasValue)
            //    MyRenderProxy.DebugDrawSphere(m_obbs[m_middleCoord][0].Value.Center, 5f, Color.Yellow, 0, true, false);
            /*
            if (m_obbs[0][m_middleCoord].HasValue)
                MyRenderProxy.DebugDrawSphere(m_obbs[0][m_middleCoord].Value.Center, 5f, Color.Green, 0, true, false);
            */

            //if (m_obbs[m_middleCoord][OBBsPerLine - 1].HasValue)
            //    MyRenderProxy.DebugDrawSphere(m_obbs[m_middleCoord][OBBsPerLine - 1].Value.Center, 5f, Color.White, 0, true, false);
            /*
            if (m_obbs[OBBsPerLine - 1][m_middleCoord].HasValue)
                MyRenderProxy.DebugDrawSphere(m_obbs[OBBsPerLine - 1][m_middleCoord].Value.Center, 5f, Color.Blue, 0, true, false);

* */
            if (m_obbs[0][OBBsPerLine - 1].HasValue)
                MyRenderProxy.DebugDrawSphere(m_obbs[0][OBBsPerLine - 1].Value.Center, 5f, Color.Green, 0, true, false);

            if (m_obbs[OBBsPerLine - 1][OBBsPerLine - 1].HasValue)
                MyRenderProxy.DebugDrawSphere(m_obbs[OBBsPerLine - 1][OBBsPerLine - 1].Value.Center, 5f, Color.Blue, 0, true, false);

            if (m_obbs[OBBsPerLine - 1][0].HasValue)
                MyRenderProxy.DebugDrawSphere(m_obbs[OBBsPerLine - 1][0].Value.Center, 5f, Color.White, 0, true, false);


            ///////////////////////
            var backLeftOBB = m_obbs[0][0];
            var backRightOBB = m_obbs[OBBsPerLine - 1][0];
            var frontRightOBB = m_obbs[OBBsPerLine - 1][OBBsPerLine - 1];

            var lowerBackLeftPoint = GetOBBCorner(backLeftOBB.Value, OBBCorner.LowerBackLeft);
            MyRenderProxy.DebugDrawSphere(lowerBackLeftPoint, 5f, Color.White, 0, true, false);
            var lowerBackRightPoint = GetOBBCorner(backRightOBB.Value, OBBCorner.LowerBackRight);
            MyRenderProxy.DebugDrawSphere(lowerBackRightPoint, 5f, Color.White, 0, true, false);
            var lowerFrontRightPoint = GetOBBCorner(frontRightOBB.Value, OBBCorner.LowerFrontRight);
            MyRenderProxy.DebugDrawSphere(lowerFrontRightPoint, 5f, Color.White, 0, true, false);
            ///////////////////////

            //MyRenderProxy.DebugDrawSphere(NeighboursCenters[0], 5f, Color.White, 0, true, false);

            /*
            foreach (var point in NeighboursCenters)
            {
                MyRenderProxy.DebugDrawSphere(point, 5f, Color.Yellow, 0, true, false);
                Vector3D g = -Vector3.Normalize(MyGravityProviderSystem.CalculateTotalGravityInPoint(point));
                MyRenderProxy.DebugDrawOBB(new MyOrientedBoundingBoxD(point, BaseOBB.HalfExtent, Quaternion.CreateFromForwardUp(CenterOBB.Orientation.Forward, g)), Color.White, 0, true, false);
            }
            */

            /*
            var point = NeighboursCenters.Last();
            MyRenderProxy.DebugDrawSphere(point, 5f, Color.Yellow, 0, true, false);
            Vector3D g = -Vector3.Normalize(MyGravityProviderSystem.CalculateTotalGravityInPoint(point));
            MyRenderProxy.DebugDrawOBB(new MyOrientedBoundingBoxD(point, BaseOBB.HalfExtent, Quaternion.CreateFromForwardUp(CenterOBB.Orientation.Forward, g)), Color.White, 0, true, false);
        */
        }

        public void Clear()
        {
            for (int i = 0; i < m_obbs.Length; i++)
                Array.Clear(m_obbs[i], 0, m_obbs.Length);
            Array.Clear(m_obbs, 0, m_obbs.Length);

            m_obbs = null;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Initializes the matrix of OBBs
        /// </summary>
        private void Initialize(Vector3D initialPoint, Vector3D forwardDirection)
        {
            double nextCenterAngle;
            CenterOBB = GetCenterOBB(initialPoint, forwardDirection, out nextCenterAngle);
            m_obbs[m_middleCoord][m_middleCoord] = CenterOBB;

            Fill(nextCenterAngle);

            // Neighbours centers overlap
            double neighboursAngle = nextCenterAngle * Math.Max(2 * m_middleCoord - (NEIGHBOUR_OVERLAP_TILES - 1), 1);
            SetNeigboursCenter(neighboursAngle);
        }

        /// <summary>
        /// Fills OBBs matrix
        /// </summary>
        private void Fill(double angle)
        {
            MyOrientedBoundingBoxD newCenterOBB;
            // Middle "vertical line"
            Vector2I index = new Vector2I(m_middleCoord, 0);

            for (int i = 0; i < OBBsPerLine; i++)
            {
                if (m_obbs[index.Y][index.X].HasValue)
                    newCenterOBB = m_obbs[index.Y][index.X].Value;
                else
                    newCenterOBB = CreateOBB(NewTransformedPoint(CenterOBB.Center,
                                                                 CenterOBB.Orientation.Forward, 
                                                                 (float)(angle) * (i - m_middleCoord)),
                                             CenterOBB.Orientation.Forward);
                FillOBBHorizontalLine(newCenterOBB, index, angle);
                index.Y++;
            }
        }

        /// <summary>
        /// Fills a "horizontal" line of OBBs
        /// </summary>
        private void FillOBBHorizontalLine(MyOrientedBoundingBoxD lineCenterOBB, Vector2I currentIndex, double angle)
        {
            m_obbs[currentIndex.Y][currentIndex.X] = lineCenterOBB;

            for (int i = 0; i < OBBsPerLine; i++)
                if (i != currentIndex.X)
                {
                    var ob = CreateOBB(NewTransformedPoint(lineCenterOBB.Center,
                                                           lineCenterOBB.Orientation.Right,
                                                           (float)(angle * (i - m_middleCoord))),
                                       lineCenterOBB.Orientation.Right);
                    m_obbs[currentIndex.Y][i] = ob;
                }
        }

        /// <summary>
        /// Returns the initial OBB, which is below the initialPoint (the "side" points are the ones that touch surface)
        /// Returns also the angle to the others OBB centers, in radians
        /// </summary>
        private MyOrientedBoundingBoxD GetCenterOBB(Vector3D initialPoint, Vector3D forwardDirection, out double angle)
        {
            Vector3D worldOffset = m_planet.PositionComp.WorldAABB.Center;

            // d - distance to center of planet
            double d = (initialPoint - worldOffset).Length();

            // aRad - half the angle that the 2 side points (endpoints of a chord that have the same length as the initial center point) make with the center of the planet, i
            // sin A = s / d;
            double sinA = m_tileHalfSize / d;
            double aRad = Math.Asin(sinA);

            angle = aRad * 2;

            /*
            // g = gravity Vector on the initial point
            Vector3D g = Vector3.Normalize(MyGravityProviderSystem.CalculateTotalGravityInPoint(initialPoint));

            double cosA = Math.Cos(aRad);

            // newL - new length (distance to center of planet) of OBB center point
            double newL = cosA * d;
            // newC - new OBB center point
            Vector3D newC = (newL * -g) + worldOffset;

            return CreateOBB(newC, forwardDirection);
            */

            // The center point is now the received initial position
            return CreateOBB(initialPoint, forwardDirection);
        }

        /// <summary>
        /// Returns a transformed point, rotated angle radians around rotation vector
        /// </summary>
        private Vector3D NewTransformedPoint(Vector3D point, Vector3 rotationVector, float angle)
        {
            Vector3D worldOffset = m_planet.PositionComp.WorldAABB.Center;

            Quaternion rotation = Quaternion.CreateFromAxisAngle(rotationVector, angle);
            return Vector3D.Transform(point - worldOffset, rotation) + worldOffset;
        }

        /// <summary>
        /// Creates and returns an OBB with the center at the specified position, with the gravity vector from that point and with the given perpendicular vector
        /// </summary>
        private MyOrientedBoundingBoxD CreateOBB(Vector3D center, Vector3D perpedicularVector)
        {
            Vector3D gravityVector = -Vector3D.Normalize(GameSystems.MyGravityProviderSystem.CalculateTotalGravityInPoint(center));
            return new MyOrientedBoundingBoxD(center, 
                                              new Vector3D(m_tileHalfSize, m_tileHalfHeight, m_tileHalfSize), 
                                              Quaternion.CreateFromForwardUp(perpedicularVector, gravityVector));
        }

        /// <summary>
        /// Returns the base of the OBBs as new OBB, connecting its 4 corner bottom points.
        /// </summary>
        /// <returns>The base as a OBB</returns>
        private MyOrientedBoundingBoxD GetBaseOBB()
        {
            var backLeftOBB = m_obbs[0][0];
            var backRightOBB = m_obbs[OBBsPerLine - 1][0];
            var frontRightOBB = m_obbs[OBBsPerLine - 1][OBBsPerLine - 1];

            var lowerBackLeftPoint = GetOBBCorner(backLeftOBB.Value, OBBCorner.LowerBackLeft);
            var lowerBackRightPoint = GetOBBCorner(backRightOBB.Value, OBBCorner.LowerBackRight);
            var lowerFrontRightPoint = GetOBBCorner(frontRightOBB.Value, OBBCorner.LowerFrontRight);

            var centerPoint = (lowerBackLeftPoint + lowerFrontRightPoint) / 2;
            var halfLength = (lowerBackLeftPoint - lowerBackRightPoint).Length() / 2;
            double halfHeight = 0.01;

            return new MyOrientedBoundingBoxD(centerPoint, new Vector3D(halfLength, halfHeight, halfLength), CenterOBB.Orientation);
        }

        /// <summary>
        /// Sets the 8 neighbour center points
        /// </summary>
        /// <param name="angle"></param>
        private void SetNeigboursCenter(double angle)
        {
            NeighboursCenters = new List<Vector3D>();
            
            var leftPoint = NewTransformedPoint(CenterOBB.Center, CenterOBB.Orientation.Forward, (float)angle);
            var rightPoint = NewTransformedPoint(CenterOBB.Center, CenterOBB.Orientation.Forward, -(float)angle);
            var backPoint = NewTransformedPoint(CenterOBB.Center, CenterOBB.Orientation.Right, (float)angle);
            var frontPoint = NewTransformedPoint(CenterOBB.Center, CenterOBB.Orientation.Right, -(float)angle);
            NeighboursCenters.Add(leftPoint);
            NeighboursCenters.Add(rightPoint);
            NeighboursCenters.Add(backPoint);
            NeighboursCenters.Add(frontPoint);

            var leftFrontPoint = NewTransformedPoint(frontPoint, CenterOBB.Orientation.Forward, -(float)angle);
            var rightFrontPoint = NewTransformedPoint(frontPoint, CenterOBB.Orientation.Forward, (float)angle);
            var leftBackPoint = NewTransformedPoint(backPoint, CenterOBB.Orientation.Forward, -(float)angle);
            var rightBackPoint = NewTransformedPoint(backPoint, CenterOBB.Orientation.Forward, (float)angle);
            NeighboursCenters.Add(leftFrontPoint);
            NeighboursCenters.Add(rightFrontPoint);
            NeighboursCenters.Add(leftBackPoint);
            NeighboursCenters.Add(rightBackPoint);
        }

        #region OBB corners
        /* MyOBBs corners
        * 00 - Upper Front Left
        * 01 - Upper Back Left
        * 02 - Lower Back Left
        * 03 - Lower Front Left
        * 04 - Upper Front Right
        * 05 - Upper Back Right
        * 06 - Lower Back Right
        * 07 - Lower Front Right
        */
        public enum OBBCorner
        {
            UpperFrontLeft = 0,
            UpperBackLeft,
            LowerBackLeft,
            LowerFrontLeft,
            UpperFrontRight,
            UpperBackRight,
            LowerBackRight,
            LowerFrontRight
        }

        /// <summary>
        /// Returns the specified corner from the OBB.
        /// </summary>
        /// <param name="obb"></param>
        /// <param name="corner"></param>
        /// <returns>Returns the </returns>
        public static Vector3D GetOBBCorner(MyOrientedBoundingBoxD obb, OBBCorner corner)
        {
            Vector3D[] corners = new Vector3D[8];
            obb.GetCorners(corners, 0);
            return corners[(int)corner];
        }
        #endregion

        #endregion
    }
}
