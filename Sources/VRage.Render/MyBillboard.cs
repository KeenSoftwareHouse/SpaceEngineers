using System;
using VRageMath;

namespace VRageRender
{
    //  This class is used for storing and sorting particle billboards
    public class MyBillboard : IComparable
    {
        public enum BlenType
        {
            Standard,
            AdditiveBottom,
            AdditiveTop
        };

        public string Material;
        
        public BlenType BlendType = BlenType.Standard;
        
        //  Use these members only for readonly acces. Change them only by calling Init()
        public Vector3D Position0;
        public Vector3D Position1;
        public Vector3D Position2;
        public Vector3D Position3;
        public Vector4 Color;
        public float ColorIntensity;
        public float SoftParticleDistanceScale;
        public Vector2 UVOffset;
        public Vector2 UVSize;

        public int ParentID = -1;

        //  Distance to camera, for sorting
        public float DistanceSquared;

        public float Reflectivity;
        public float AlphaCutout;

        public int CustomViewProjection;

        public int CompareTo(object compareToObject)
        {
            var compareToParticle = (MyBillboard)compareToObject;

            return String.Compare(Material, compareToParticle.Material, StringComparison.Ordinal);
        }
    }
}
