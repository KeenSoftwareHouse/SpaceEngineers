using System;
using System.Collections.Generic;
using VRageMath;

namespace VRageRender
{
    //  This class is used for storing and sorting particle billboards
    public class MyBillboard : IComparable
    {
        public string Material;
        public float BlendTextureRatio;
        public string BlendMaterial;
        
        //  Use these members only for readonly acces. Change them only by calling Init()
        public Vector3D Position0;
        public Vector3D Position1;
        public Vector3D Position2;
        public Vector3D Position3;
        public Color Color;
        public float ColorIntensity;
        public Vector2 UVOffset;

        public int ParentID = -1;

        //  Distance to camera, for sorting
        public float DistanceSquared;

        public float Size;
        public float Reflectivity;
        public float AlphaCutout;


        public bool EnableColorize = false;
        public bool Near = false;
        public bool Lowres = false;

        // Used for sorting
        public int Priority;

        public int CustomViewProjection;

        public List<MyBillboard> ContainedBillboards = new List<MyBillboard>();

        public bool CullWithStencil = false;
                 
        //  For sorting particles back-to-front (so bigger distance is first in the list)
        public int CompareTo(object compareToObject)
        {
            var compareToParticle = (MyBillboard)compareToObject;

            if (CustomViewProjection == compareToParticle.CustomViewProjection)
            {
                if (Priority == compareToParticle.Priority)
                {
                    return compareToParticle.DistanceSquared.CompareTo(this.DistanceSquared);
                }
                else
                {
                    return Priority.CompareTo(compareToParticle.Priority);
                }
            }
            else
            {
                return CustomViewProjection.CompareTo(compareToParticle.CustomViewProjection);
            }          
        }
    }
}
