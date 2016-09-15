using Sandbox.Common;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using VRage;
using VRageRender;
using BoundingBox = VRageMath.BoundingBox;
using BoundingSphere = VRageMath.BoundingSphere;
using Line = VRageMath.Line;
using MathHelper = VRageMath.MathHelper;
using Matrix = VRageMath.Matrix;
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;
using VRageMath;
using VRage.Utils;

namespace Sandbox.Engine.Utils
{
    //public static class MyUtils
    //{
    //    //private static MyTextureAtlas LoadTextureAtlas(string textureDir, string atlasFile)
    //    //{
    //    //    var atlas  = new MyTextureAtlas(64);
    //    //    var fsPath = Path.Combine(MyFileSystem.ContentPath, atlasFile);

    //    //    using (var stream = MyFileSystem.OpenRead(fsPath))
    //    //    using (StreamReader sr = new StreamReader(stream))
    //    //    {
    //    //        while (!sr.EndOfStream)
    //    //        {
    //    //            string line = sr.ReadLine();

    //    //            if (line.StartsWith("#"))
    //    //                continue;
    //    //            if (line.Trim(' ').Length == 0)
    //    //                continue;

    //    //            string[] parts = line.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);

    //    //            string name = parts[0];

    //    //            string atlasName = parts[1];

    //    //            Vector4 uv = new Vector4(
    //    //                Convert.ToSingle(parts[4], System.Globalization.CultureInfo.InvariantCulture),
    //    //                Convert.ToSingle(parts[5], System.Globalization.CultureInfo.InvariantCulture),
    //    //                Convert.ToSingle(parts[7], System.Globalization.CultureInfo.InvariantCulture),
    //    //                Convert.ToSingle(parts[8], System.Globalization.CultureInfo.InvariantCulture));

    //    //            string atlasTexture = textureDir + atlasName;
    //    //            MyTextureAtlasItem item = new MyTextureAtlasItem(atlasTexture, uv);
    //    //            atlas.Add(name, item);
    //    //        }
    //    //    }

    //    //    return atlas;
    //    //}

    //    //public static void LoadTextureAtlas(string[] enumsToStrings, string textureDir, string atlasFile, out string texture, out MyAtlasTextureCoordinate[] textureCoords)
    //    //{
    //    //    MyTextureAtlas atlas = LoadTextureAtlas(textureDir, atlasFile);

    //    //    //  Here we define particle texture coordinates inside of texture atlas
    //    //    textureCoords = new MyAtlasTextureCoordinate[enumsToStrings.Length];

    //    //    texture = null;

    //    //    for (int i = 0; i < enumsToStrings.Length; i++)
    //    //    {
    //    //        MyTextureAtlasItem textureAtlasItem = atlas[enumsToStrings[i]];

    //    //        textureCoords[i] = new MyAtlasTextureCoordinate(new Vector2(textureAtlasItem.UVOffsets.X, textureAtlasItem.UVOffsets.Y), new Vector2(textureAtlasItem.UVOffsets.Z, textureAtlasItem.UVOffsets.W));

    //    //        //  Texture atlas content processor support having more DDS files for one atlas, but we don't want it (because we want to have all particles in one texture, so we can draw fast).
    //    //        //  So here we just take first and only texture.
    //    //        if (texture == null)
    //    //        {
    //    //            texture = textureAtlasItem.AtlasTexture;
    //    //        }
    //    //    }
    //    //}


    //        //Vrati najkratsiu vzdialenost medzi bodom a rovinou (definovanou normalou a lubovolnym bodom na rovine).
    //        //Moze vratit aj zapornu vzdialenost, pokial sa bod nachadza na opacnej strane roviny nez ukazuje normalovy vektor.
    //        //Predpokladame ze, normalovy vektor je normalizovany.
    //    public static float GetDistanceFromPointToPlane(ref Vector3 point, ref MyPlane plane)
    //    {
    //        return
    //             plane.Normal.X * (point.X - plane.Point.X) +
    //             plane.Normal.Y * (point.Y - plane.Point.Y) +
    //             plane.Normal.Z * (point.Z - plane.Point.Z);
    //    }


    //    //	This tells if a sphere is BEHIND, in FRONT, or INTERSECTS a plane, also it's distance
    //    public static MySpherePlaneIntersectionEnum GetSpherePlaneIntersection(ref BoundingSphereD sphere, ref MyPlane plane, out float distanceFromPlaneToSphere)
    //    {
    //        //  First we need to find the distance our polygon plane is from the origin.
    //        float planeDistance = plane.GetPlaneDistance();

    //        //  Here we use the famous distance formula to find the distance the center point
    //        //  of the sphere is from the polygon's plane.  
    //        distanceFromPlaneToSphere = (float)(plane.Normal.X * sphere.Center.X + plane.Normal.Y * sphere.Center.Y + plane.Normal.Z * sphere.Center.Z + planeDistance);

    //        //  If the absolute value of the distance we just found is less than the radius, 
    //        //  the sphere intersected the plane.
    //        if (Math.Abs(distanceFromPlaneToSphere) < sphere.Radius)
    //        {
    //            return MySpherePlaneIntersectionEnum.INTERSECTS;
    //        }
    //        else if (distanceFromPlaneToSphere >= sphere.Radius)
    //        {
    //            //  Else, if the distance is greater than or equal to the radius, the sphere is
    //            //  completely in FRONT of the plane.
    //            return MySpherePlaneIntersectionEnum.FRONT;
    //        }

    //        //  If the sphere isn't intersecting or in FRONT of the plane, it must be BEHIND
    //        return MySpherePlaneIntersectionEnum.BEHIND;
    //    }

    //    //  Method returns intersection point between sphere and triangle (which is defined by vertexes and plane).
    //    //  If no intersection found, method returns null.
    //    //  See below how intersection point can be calculated, because it's not so easy - for example sphere vs. triangle will 
    //    //  hardly generate just intersection point... more like intersection area or something.
    //    public static Vector3? GetSphereTriangleIntersection(ref BoundingSphereD sphere, ref MyPlane trianglePlane, ref MyTriangle_Vertices triangle)
    //    {
    //        //	Vzdialenost gule od roviny trojuholnika
    //        float distance;

    //        //	Zistim, ci sa gula nachadza pred alebo za rovinou trojuholnika, alebo ju presekava
    //        MySpherePlaneIntersectionEnum spherePlaneIntersection = GetSpherePlaneIntersection(ref sphere, ref trianglePlane, out distance);

    //        //	Ak gula presekava rovinu, tak hladam pseudo-priesecnik
    //        if (spherePlaneIntersection == MySpherePlaneIntersectionEnum.INTERSECTS)
    //        {
    //            //	Offset ktory pomoze vypocitat suradnicu stredu gule premietaneho na rovinu trojuholnika
    //            Vector3 offset = trianglePlane.Normal * distance;

    //            //	Priesecnik na rovine trojuholnika, je to premietnuty stred gule na rovine trojuholnika
    //            Vector3 intersectionPoint;
    //            intersectionPoint.X = (float)(sphere.Center.X - offset.X);
    //            intersectionPoint.Y = (float)(sphere.Center.Y - offset.Y);
    //            intersectionPoint.Z = (float)(sphere.Center.Z - offset.Z);

    //            if (GetInsidePolygonForSphereCollision(ref intersectionPoint, ref triangle))		//	Ak priesecnik nachadza v trojuholniku
    //            {
    //                //	Toto je pripad, ked sa podarilo premietnut stred gule na rovinu trojuholnika a tento priesecnik sa
    //                //	nachadza vnutri trojuholnika (tzn. sedia uhly)
    //                return intersectionPoint;
    //            }
    //            else													//	Ak sa priesecnik nenachadza v trojuholniku, este stale sa moze nachadzat na hrane trojuholnika
    //            {
    //                Vector3? edgeIntersection = CommonUtils.GetEdgeSphereCollision(ref sphere.Center, (float)sphere.Radius / 1.0f, ref triangle);
    //                if (edgeIntersection != null)
    //                {
    //                    //	Toto je pripad, ked sa priemietnuty stred gule nachadza mimo trojuholnika, ale intersection gule a trojuholnika tam
    //                    //	je, pretoze gula presekava jednu z hran trojuholnika. Takze vratim suradnice priesecnika na jednej z hran.
    //                    return edgeIntersection.Value;
    //                }
    //            }
    //        }

    //        //	Sphere doesn't collide with any triangle
    //        return null;
    //    }

    //    //	Return true if point is inside the triangle.
    //    public static bool GetInsidePolygonForSphereCollision(ref Vector3 point, ref MyTriangle_Vertices triangle)
    //    {
    //        const float MATCH_FACTOR = 0.99f;		// Used to cover up the error in floating point
    //        float angle = 0.0f;						// Initialize the angle

    //        //	Spocitame uhol medzi bodmi trojuholnika a intersection bodu (ale na vypocet uhlov pouzivame funkciu ktora je
    //        //	bezpecna aj pre sphere coldet, problem so SafeACos())
    //        angle += CommonUtils.GetAngleBetweenVectorsForSphereCollision(triangle.Vertex0 - point, triangle.Vertex1 - point);	// Find the angle between the 2 vectors and add them all up as we go along
    //        angle += CommonUtils.GetAngleBetweenVectorsForSphereCollision(triangle.Vertex1 - point, triangle.Vertex2 - point);	// Find the angle between the 2 vectors and add them all up as we go along
    //        angle += CommonUtils.GetAngleBetweenVectorsForSphereCollision(triangle.Vertex2 - point, triangle.Vertex0 - point);	// Find the angle between the 2 vectors and add them all up as we go along

    //        if (angle >= (MATCH_FACTOR * (2.0 * MathHelper.Pi)))	// If the angle is greater than 2 PI, (360 degrees)
    //        {
    //            return true;							// The point is inside of the polygon
    //        }

    //        return false;								// If you get here, it obviously wasn't inside the polygon, so Return FALSE
    //    }
    //}
}