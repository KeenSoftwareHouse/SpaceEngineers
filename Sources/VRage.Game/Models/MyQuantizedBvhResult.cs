#region Using

using System.Collections.Generic;
using VRageMath;
using BulletXNA.BulletCollision;
using VRageRender;
using VRage.Utils;
using VRage.Game.Components;
#endregion

namespace VRage.Game.Models
{
    public class MyQuantizedBvhResult
    {
        MyModel m_model;
        MyIntersectionResultLineTriangle? m_result;
        LineD m_line;
        IntersectionFlags m_flags;

        public readonly ProcessCollisionHandler ProcessTriangleHandler;

        public MyQuantizedBvhResult()
        {
            ProcessTriangleHandler = new ProcessCollisionHandler(ProcessTriangle);
        }

        public MyIntersectionResultLineTriangle? Result
        {
            get
            {
                return m_result;
            }
        }        

        public void Start(MyModel model, LineD line, IntersectionFlags flags = IntersectionFlags.DIRECT_TRIANGLES)
        {
            m_result = null;
            m_model = model;
            m_line = line;
            m_flags = flags;
        }

        private float? ProcessTriangle(int triangleIndex)
        {
            System.Diagnostics.Debug.Assert((int)m_flags != 0);

            MyTriangle_Vertices triangle;
            MyTriangleVertexIndices triangleIndices = m_model.Triangles[triangleIndex];

            m_model.GetVertex(triangleIndices.I0, triangleIndices.I2, triangleIndices.I1, out triangle.Vertex0, out triangle.Vertex1, out triangle.Vertex2);

            Vector3 calculatedTriangleNormal = MyUtils.GetNormalVectorFromTriangle(ref triangle);
               
            //We dont want backside intersections
            if (((int)(m_flags & IntersectionFlags.FLIPPED_TRIANGLES) == 0) &&
                Vector3.Dot(m_line.Direction, calculatedTriangleNormal) > 0)
                return null;

            Line lineF = (Line)m_line;
            float? distance = MyUtils.GetLineTriangleIntersection(ref lineF, ref triangle);

            if (distance != null && float.IsNaN(distance.Value))
            {
                System.Diagnostics.Debug.Fail("Invalid triangle in " + m_model.AssetName);
                MyLog.Default.Warning("Invalid triangle in " + m_model.AssetName);
            }

            //  If intersection occured and if distance to intersection is closer to origin than any previous intersection
            if ((distance != null && !float.IsNaN(distance.Value)) && ((m_result == null) || (distance.Value < m_result.Value.Distance)))
            {
                //  We need to remember original triangleVertexes coordinates (not transformed by world matrix)
                MyTriangle_BoneIndicesWeigths? boneWeights = m_model.GetBoneIndicesWeights(triangleIndex);
                m_result = new MyIntersectionResultLineTriangle(triangleIndex, ref triangle, ref boneWeights, ref calculatedTriangleNormal, distance.Value);
                return distance.Value;
            }
            return null;
        }
    }

    class MyResultComparer : IComparer<MyIntersectionResultLineTriangle>
    {
        public int Compare(MyIntersectionResultLineTriangle x, MyIntersectionResultLineTriangle y)
        {
            return x.Distance.CompareTo(y.Distance);
        }
    }

    public class MyQuantizedBvhAllTrianglesResult
    {
        MyModel m_model;
        List<MyIntersectionResultLineTriangle> m_result = new List<MyIntersectionResultLineTriangle>();
        LineD m_line;
        IntersectionFlags m_flags;
        MyResultComparer m_comparer = new MyResultComparer();

        public readonly ProcessCollisionHandler ProcessTriangleHandler;

        public MyQuantizedBvhAllTrianglesResult()
        {
            ProcessTriangleHandler = new ProcessCollisionHandler(ProcessTriangle);
        }

        public List<MyIntersectionResultLineTriangle> Result
        {
            get
            {
                return m_result;
            }
        }

        public void Start(MyModel model, LineD line, IntersectionFlags flags = IntersectionFlags.DIRECT_TRIANGLES)
        {
            m_result.Clear();
            m_model = model;
            m_line = line;
            m_flags = flags;
        }

        private float? ProcessTriangle(int triangleIndex)
        {
            System.Diagnostics.Debug.Assert((int)m_flags != 0);

            MyTriangle_Vertices triangle;
            MyTriangleVertexIndices triangleIndices = m_model.Triangles[triangleIndex];

            m_model.GetVertex(triangleIndices.I0, triangleIndices.I2, triangleIndices.I1, out triangle.Vertex0, out triangle.Vertex1, out triangle.Vertex2);

            Vector3 calculatedTriangleNormal = MyUtils.GetNormalVectorFromTriangle(ref triangle);

            //We dont want backside intersections
            if (((int)(m_flags & IntersectionFlags.FLIPPED_TRIANGLES) == 0) &&
                Vector3.Dot(m_line.Direction, calculatedTriangleNormal) > 0)
                return null;

            Line lineF = (Line)m_line;
            float? distance = MyUtils.GetLineTriangleIntersection(ref lineF, ref triangle);

            if (distance.HasValue)
            {
                MyTriangle_BoneIndicesWeigths? boneWeights = m_model.GetBoneIndicesWeights(triangleIndex);
                var result = new MyIntersectionResultLineTriangle(triangleIndex, ref triangle, ref boneWeights, ref calculatedTriangleNormal, distance.Value);
                m_result.Add(result);
            }

            return distance;
        }

        public void End()
        {
            m_result.Sort(m_comparer);
        }
    }
}
