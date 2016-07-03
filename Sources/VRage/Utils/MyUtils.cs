using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using VRageMath;
using VRageRender;
using System.Linq;


namespace VRage.Utils
{
    public static partial class MyUtils
    {
        public static readonly StringBuilder EmptyStringBuilder = new StringBuilder();
        public static readonly Matrix ZeroMatrix = new Matrix(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
        static readonly string[] BYTE_SIZE_PREFIX = new string[] { "", "K", "M", "G", "T" };
        private static List<char> m_splitBuffer = new List<char>(16);

        [Conditional("DEBUG")]
        public static void AssertIsValid(Matrix matrix)
        {
            System.Diagnostics.Debug.Assert(IsValid(matrix));
        }
        [Conditional("DEBUG")]
        public static void AssertIsValid(MatrixD matrix)
        {
            System.Diagnostics.Debug.Assert(IsValid(matrix));
        }
        [Conditional("DEBUG")]
        public static void AssertIsValid(Vector3 vec)
        {
            System.Diagnostics.Debug.Assert(IsValid(vec));
        }
        [Conditional("DEBUG")]
        public static void AssertIsValid(Vector3D vec)
        {
            System.Diagnostics.Debug.Assert(IsValid(vec));
        }
        [Conditional("DEBUG")]
        public static void AssertIsValid(Vector3? vec)
        {
            System.Diagnostics.Debug.Assert(IsValid(vec));
        }
        [Conditional("DEBUG")]
        public static void AssertIsValid(Vector2 vec)
        {
            System.Diagnostics.Debug.Assert(IsValid(vec));
        }
        [Conditional("DEBUG")]
        public static void AssertIsValid(float f)
        {
            System.Diagnostics.Debug.Assert(IsValid(f));
        }
        [Conditional("DEBUG")]
        public static void AssertIsValid(double f)
        {
            System.Diagnostics.Debug.Assert(IsValid(f));
        }
        [Conditional("DEBUG")]
        public static void AssertIsValid(Quaternion q)
        {
            System.Diagnostics.Debug.Assert(IsValid(q));
        }
        [Conditional("DEBUG")]
        public static void AssertIsValidOrZero(Matrix matrix)
        {
            System.Diagnostics.Debug.Assert(IsValidOrZero(matrix));
        }
        [Conditional("DEBUG")]
        public static void AssertLengthValid(ref Vector3 vec)
        {
            System.Diagnostics.Debug.Assert(HasValidLength(vec));
        }
        [Conditional("DEBUG")]
        public static void AssertLengthValid(ref Vector3D vec)
        {
            System.Diagnostics.Debug.Assert(HasValidLength(vec));
        }
        public static bool HasValidLength(Vector3 vec)
        {
            return vec.Length() > MyMathConstants.EPSILON10;
        }
        public static bool HasValidLength(Vector3D vec)
        {
            return vec.Length() > MyMathConstants.EPSILON10;
        }
        public static bool IsEqual(float value1, float value2)
        {
            return MyUtils.IsZero(value1 - value2);
        }
        public static bool IsEqual(Vector2 value1, Vector2 value2)
        {
            return MyUtils.IsZero(value1.X - value2.X) && MyUtils.IsZero(value1.Y - value2.Y);
        }
        public static bool IsEqual(Vector3 value1, Vector3 value2)
        {
            return MyUtils.IsZero(value1.X - value2.X) && MyUtils.IsZero(value1.Y - value2.Y) && MyUtils.IsZero(value1.Z - value2.Z);
        }

        public static bool IsEqual(Quaternion value1, Quaternion value2)
        {
            return MyUtils.IsZero(value1.X - value2.X) && MyUtils.IsZero(value1.Y - value2.Y) && MyUtils.IsZero(value1.Z - value2.Z) && MyUtils.IsZero(value1.W - value2.W);
        }
        public static bool IsEqual(QuaternionD value1, QuaternionD value2)
        {
            return MyUtils.IsZero(value1.X - value2.X) && MyUtils.IsZero(value1.Y - value2.Y) && MyUtils.IsZero(value1.Z - value2.Z) && MyUtils.IsZero(value1.W - value2.W);
        }

        public static bool IsEqual(Matrix value1, Matrix value2)
        {
            return MyUtils.IsZero(value1.Left - value2.Left)
                && MyUtils.IsZero(value1.Up - value2.Up)
                && MyUtils.IsZero(value1.Forward - value2.Forward)
                && MyUtils.IsZero(value1.Translation - value2.Translation);
        }
        public static bool IsValid(Matrix matrix)
        {
            return matrix.Up.IsValid() && matrix.Left.IsValid() && matrix.Forward.IsValid() && matrix.Translation.IsValid() && (matrix != Matrix.Zero);
        }
        public static bool IsValid(MatrixD matrix)
        {
            return matrix.Up.IsValid() && matrix.Left.IsValid() && matrix.Forward.IsValid() && matrix.Translation.IsValid() && (matrix != MatrixD.Zero);
        }
        public static bool IsValid(Vector3 vec)
        {
            return IsValid(vec.X) && IsValid(vec.Y) && IsValid(vec.Z);
        }
        public static bool IsValid(Vector3D vec)
        {
            return IsValid(vec.X) && IsValid(vec.Y) && IsValid(vec.Z);
        }
        public static bool IsValid(Vector2 vec)
        {
            return IsValid(vec.X) && IsValid(vec.Y);
        }
        public static bool IsValid(float f)
        {
            return !float.IsNaN(f) && !float.IsInfinity(f);
        }
        public static bool IsValid(double f)
        {
            return !double.IsNaN(f) && !double.IsInfinity(f);
        }
        public static bool IsValid(Vector3? vec)
        {
            return vec == null ? true : IsValid(vec.Value.X) && IsValid(vec.Value.Y) && IsValid(vec.Value.Z);
        }
        public static bool IsValid(Quaternion q)
        {
            return IsValid(q.X) && IsValid(q.Y) && IsValid(q.Z) && IsValid(q.W) &&
                !MyUtils.IsZero(q);
        }
        public static bool IsValidNormal(Vector3 vec)
        {
            const float epsilon = 0.001f;
            var length = vec.LengthSquared();
            return vec.IsValid() && length > 1 - epsilon && length < 1 + epsilon;
        }
        public static bool IsValidOrZero(Matrix matrix)
        {
            return IsValid(matrix.Up) && IsValid(matrix.Left) && IsValid(matrix.Forward) && IsValid(matrix.Translation);
        }
        public static bool IsZero(float value, float epsilon = MyMathConstants.EPSILON)
        {
            return (value > -epsilon) && (value < epsilon);
        }
        public static bool IsZero(double value, float epsilon = MyMathConstants.EPSILON)
        {
            return (value > -epsilon) && (value < epsilon);
        }
        public static bool IsZero(Vector3 value, float epsilon = MyMathConstants.EPSILON)
        {
            return IsZero(value.X, epsilon) && IsZero(value.Y, epsilon) && IsZero(value.Z, epsilon);
        }
        public static bool IsZero(Vector3D value, float epsilon = MyMathConstants.EPSILON)
        {
            return IsZero(value.X, epsilon) && IsZero(value.Y, epsilon) && IsZero(value.Z, epsilon);
        }
        public static bool IsZero(Quaternion value, float epsilon = MyMathConstants.EPSILON)
        {
            return IsZero(value.X, epsilon) && IsZero(value.Y, epsilon) && IsZero(value.Z, epsilon) && IsZero(value.W, epsilon);
        }
        public static bool IsZero(Vector4 value)
        {
            return IsZero(value.X) && IsZero(value.Y) && IsZero(value.Z) && IsZero(value.W);
        }

        public static void CheckFloatValues(object graph, string name, ref double? min, ref double? max)
        {
#if BLIT
            Debug.Assert(false);
#else
            if (new StackTrace().FrameCount > 1000)
            {
                Debug.Fail("Infinite loop?");
            }
#endif
            if (graph == null) return;

            if (graph is float)
            {
                var val = (float)graph;
                if (float.IsInfinity(val) || float.IsNaN(val))
                {
                    Debug.Fail("invalid number");
                    throw new InvalidOperationException("Invalid value: " + name);
                }

                if (!min.HasValue || val < min) min = val;
                if (!max.HasValue || val > max) max = val;
            }

            if (graph is double)
            {
                var val = (double)graph;
                if (double.IsInfinity(val) || double.IsNaN(val))
                {
                    Debug.Fail("invalid number");
                    throw new InvalidOperationException("Invalid value: " + name);
                }

                if (!min.HasValue || val < min) min = val;
                if (!max.HasValue || val > max) max = val;
            }

            if (graph.GetType().IsPrimitive || graph is string || graph is DateTime) return;

            if (graph as IEnumerable != null)
            {
                foreach (var item in graph as IEnumerable)
                {
                    CheckFloatValues(item, name + "[]", ref min, ref max);
                }
                return;
            }
            foreach (var f in graph.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                CheckFloatValues(f.GetValue(graph), name + "." + f.Name, ref min, ref max);
            }
            foreach (var p in graph.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                CheckFloatValues(p.GetValue(graph, null), name + "." + p.Name, ref min, ref max);
            }
        }
        public static void DeserializeValue(XmlReader reader, out Vector3 value)
        {
            object val = reader.Value;
            reader.Read();

            string[] parts = ((string)val).Split(' ');
            Vector3 v = new Vector3(Convert.ToSingle(parts[0], CultureInfo.InvariantCulture), Convert.ToSingle(parts[1], CultureInfo.InvariantCulture), Convert.ToSingle(parts[2], CultureInfo.InvariantCulture));
            value = v;
        }
        public static void DeserializeValue(XmlReader reader, out Vector4 value)
        {
            object val = reader.Value;
            reader.Read();

            string[] parts = ((string)val).Split(' ');
            Vector4 v = new Vector4(Convert.ToSingle(parts[0], CultureInfo.InvariantCulture), Convert.ToSingle(parts[1], CultureInfo.InvariantCulture), Convert.ToSingle(parts[2], CultureInfo.InvariantCulture), Convert.ToSingle(parts[3], CultureInfo.InvariantCulture));
            value = v;
        }
        public static string FormatByteSizePrefix(ref float byteSize)
        {
            long multiple = 1;
            for (int i = 0; i < BYTE_SIZE_PREFIX.Length; i++)
            {
                multiple *= 1024;
                if (byteSize < multiple)
                {
                    byteSize = byteSize / (multiple / 1024);
                    return BYTE_SIZE_PREFIX[i];
                }
            }
            return String.Empty;
        }
        public static Color[] GenerateBoxColors()
        {
            List<Color> c = new List<Color>();
            for (float h = 0; h < 1; h += 0.2f)
            {
                for (float s = 0; s < 1; s += 0.33f)
                {
                    for (float v = 0; v < 1; v += 0.33f)
                    {
                        var hue = MathHelper.Lerp(180 / 360.0f, 210 / 360.0f, h);
                        var sat = MathHelper.Lerp(0.4f, 0.9f, s);
                        var val = MathHelper.Lerp(0.4f, 1.0f, v);
                        c.Add(ColorExtensions.HSVtoColor(new Vector3(hue, sat, val)));
                    }
                }
            }
            c.ShuffleList();
            return c.ToArray();
        }
        /// <summary>
        /// Generate oriented quad by matrix
        /// </summary>
        public static void GenerateQuad(out MyQuadD quad, ref Vector3D position, float width, float height, ref MatrixD matrix)
        {
            Vector3 billboardAxisX = matrix.Left * width;
            Vector3 billboardAxisY = matrix.Up * height;

            //	Coordinates of four points of a billboard's quad
            quad.Point0 = position + billboardAxisX + billboardAxisY;
            quad.Point1 = position + billboardAxisX - billboardAxisY;
            quad.Point2 = position - billboardAxisX - billboardAxisY;
            quad.Point3 = position - billboardAxisX + billboardAxisY;
        }
        /// <summary>
        /// Calculating the Angle between two Vectors (return in radians).
        /// </summary>
        public static float GetAngleBetweenVectors(Vector3 vectorA, Vector3 vectorB)
        {
            Debug.Assert(vectorA.Length().IsEqual(1f));
            Debug.Assert(vectorB.Length().IsEqual(1f));

            //  Calculate the cosine of the angle using the dot product
            //  Use normalised vectors to simplify the formula
            float cosAngle = Vector3.Dot(vectorA, vectorB);

            //  Result from Vector3.Dot are sometime not accurate due to rounding errors, so sometime 0 degree angle is
            //  calculated not as 1.0 but 1.00000001. We need to fix this, because ACOS accepts only number in range <-1, +1>
            if ((cosAngle > 1.0f) && (cosAngle <= 1.0001f))
            {
                cosAngle = 1.0f;
            }
            if ((cosAngle < -1.0f) && (cosAngle >= -1.0001f))
            {
                cosAngle = -1.0f;
            }

            //  Calculate the angle in radians
            return (float)Math.Acos(cosAngle);
        }
        public static float GetAngleBetweenVectorsAndNormalise(Vector3 vectorA, Vector3 vectorB)
        {
            return GetAngleBetweenVectors(Vector3.Normalize(vectorA), Vector3.Normalize(vectorB));
        }
        public static float GetAngleBetweenVectorsForSphereCollision(Vector3 vector1, Vector3 vector2)
        {
            //	Get the dot product of the vectors
            float dotProduct = Vector3.Dot(vector1, vector2);

            //	Get the product of both of the vectors magnitudes
            float vectorsMagnitude = vector1.Length() * vector2.Length();

            float angle = (float)Math.Acos(dotProduct / vectorsMagnitude);

            //	Ak bol parameter pre acos() nie v ramci intervalo -1 az +1, tak to je zle, a funkcia musi vratit 0
            if (float.IsNaN(angle) == true)
            {
                return 0.0f;
            }

            //	Vysledny uhol
            return angle;
        }
        //  Return quad whos face is always looking to the camera. This is the real billboard, with perspective distortions!
        //  Billboard's orientation is calculated by projecting unit sphere's north pole to billboard's plane. From this we can get up/right vectors.
        //  Idea is this: if camera is in the middle of unit sphere and billboard is touching this sphere at some place (still unit sphere), we
        //  know billboard's plan is perpendicular to the sphere, so if we want billboard's orientation to be like earth's latitude and Colatitude,
        //  we need to find billboard's up vector in the direction. This is when projection cames into place.
        //  Notice that we don't need camera left/up vector. We only need camera position. Because it's about the sphere around the player. Not camera orientation.
        //  IMPORTANT: One problem of this approach is that if billboard is right above the player, its orientation will swing. Thats because we are projecting
        //  Rotation is around vector pointing from camera position to center of the billboard.
        //  the point, but it ends right in the billboard's centre.
        //  So we not use this for particles. We use it only for background sphere (starts, galaxies) prerender.
        //  Return false if billboard wasn't for any reason created (e.g. too close to the camera)
        public static bool GetBillboardQuadAdvancedRotated(out MyQuadD quad, Vector3D position, float radiusX, float radiusY, float angle, Vector3D cameraPosition)
        {
            //  Optimized: Vector3 dirVector = MyMwcUtils.Normalize(position - cameraPosition);
            Vector3D dirVector;
            dirVector.X = (position.X - cameraPosition.X);
            dirVector.Y = (position.Y - cameraPosition.Y);
            dirVector.Z = (position.Z - cameraPosition.Z);

            // If distance to camera is really small don't draw it.
            if (dirVector.LengthSquared() <= MyMathConstants.EPSILON)
            {
                //  Some empty quad
                quad = new MyQuadD();
                return false;
            }

            dirVector = MyUtils.Normalize(dirVector);

            Vector3D projectedPoint;
            // Project Up onto plane defined by origin and dirVector
            Vector3D.Reject(ref Vector3D.Up, ref dirVector, out projectedPoint);

            Vector3D upVector;
            if (projectedPoint.LengthSquared() <= MyMathConstants.EPSILON_SQUARED)
            {
                //  If projected point equals to zero, we know billboard is exactly above or bottom of camera 
                //  and we can't calculate proper orientation. So we just select some direction. Good thing we
                //  know is that billboard's plan ix XY, so we can choose any point on this plane
                upVector = Vector3D.Forward;
            }
            else
            {
                //  Optimized: upVector = MyMwcUtils.Normalize(projectedPoint);
                MyUtils.Normalize(ref projectedPoint, out upVector);
            }

            //  Optimized: Vector3 leftVector = MyMwcUtils.Normalize(Vector3.Cross(upVector, dirVector));
            Vector3D leftVector;
            Vector3D.Cross(ref upVector, ref dirVector, out leftVector);
            leftVector = MyUtils.Normalize(leftVector);

            //	Two main vectors of a billboard rotated around the view axis/vector
            float angleCos = (float)Math.Cos(angle);
            float angleSin = (float)Math.Sin(angle);

            Vector3D billboardAxisX;
            billboardAxisX.X = (radiusX * angleCos) * leftVector.X + (radiusY * angleSin) * upVector.X;
            billboardAxisX.Y = (radiusX * angleCos) * leftVector.Y + (radiusY * angleSin) * upVector.Y;
            billboardAxisX.Z = (radiusX * angleCos) * leftVector.Z + (radiusY * angleSin) * upVector.Z;

            Vector3D billboardAxisY;
            billboardAxisY.X = (-radiusX * angleSin) * leftVector.X + (radiusY * angleCos) * upVector.X;
            billboardAxisY.Y = (-radiusX * angleSin) * leftVector.Y + (radiusY * angleCos) * upVector.Y;
            billboardAxisY.Z = (-radiusX * angleSin) * leftVector.Z + (radiusY * angleCos) * upVector.Z;

            //	Coordinates of four points of a billboard's quad
            quad.Point0.X = position.X + billboardAxisX.X + billboardAxisY.X;
            quad.Point0.Y = position.Y + billboardAxisX.Y + billboardAxisY.Y;
            quad.Point0.Z = position.Z + billboardAxisX.Z + billboardAxisY.Z;

            quad.Point1.X = position.X - billboardAxisX.X + billboardAxisY.X;
            quad.Point1.Y = position.Y - billboardAxisX.Y + billboardAxisY.Y;
            quad.Point1.Z = position.Z - billboardAxisX.Z + billboardAxisY.Z;

            quad.Point2.X = position.X - billboardAxisX.X - billboardAxisY.X;
            quad.Point2.Y = position.Y - billboardAxisX.Y - billboardAxisY.Y;
            quad.Point2.Z = position.Z - billboardAxisX.Z - billboardAxisY.Z;

            quad.Point3.X = position.X + billboardAxisX.X - billboardAxisY.X;
            quad.Point3.Y = position.Y + billboardAxisX.Y - billboardAxisY.Y;
            quad.Point3.Z = position.Z + billboardAxisX.Z - billboardAxisY.Z;

            return true;
        }
        public static bool GetBillboardQuadAdvancedRotated(out MyQuadD quad, Vector3D position, float radius, float angle, Vector3D cameraPosition)
        {
            return GetBillboardQuadAdvancedRotated(out quad, position, radius, radius, angle, cameraPosition);
        }
        public static void GetBillboardQuadOriented(out MyQuadD quad, ref Vector3D position, float radius, ref Vector3 leftVector, ref Vector3 upVector)
        {
            Vector3D billboardAxisX = leftVector * radius;
            Vector3D billboardAxisY = upVector * radius;

            //	Coordinates of four points of a billboard's quad
            quad.Point0 = position + billboardAxisX + billboardAxisY;
            quad.Point1 = position + billboardAxisX - billboardAxisY;
            quad.Point2 = position - billboardAxisX - billboardAxisY;
            quad.Point3 = position - billboardAxisX + billboardAxisY;
        }
        /// <summary>
        /// This billboard isn't facing the camera. It's always oriented in specified direction. May be used as thrusts, or inner light of reflector.
        /// </summary>
        public static void GetBillboardQuadOriented(out MyQuadD quad, ref Vector3D position, float width, float height, ref Vector3 leftVector, ref Vector3 upVector)
        {
            Vector3D billboardAxisX = leftVector * width;
            Vector3D billboardAxisY = upVector * height;

            //	Coordinates of four points of a billboard's quad
            quad.Point0 = position + billboardAxisX + billboardAxisY;
            quad.Point1 = position + billboardAxisX - billboardAxisY;
            quad.Point2 = position - billboardAxisX - billboardAxisY;
            quad.Point3 = position - billboardAxisX + billboardAxisY;
        }
        //  Try to convert string to bool. If not possible, null is returned.
        public static bool? GetBoolFromString(string s)
        {
            bool outBool;
            if (bool.TryParse(s, out outBool) == false)
            {
                return null;
            }
            return outBool;
        }
        //  Try to convert string to bool. If not possible, null is returned.
        //  If 's' can't be converted to a valid bool, 'defaultValue' is returned
        public static bool GetBoolFromString(string s, bool defaultValue)
        {
            bool? outBool = GetBoolFromString(s);
            return (outBool == null) ? defaultValue : outBool.Value;
        }
        public static BoundingSphereD GetBoundingSphereFromBoundingBox(ref BoundingBoxD box)
        {
            BoundingSphereD ret;
            ret.Center = (box.Max + box.Min) / 2.0;
            ret.Radius = Vector3D.Distance(ret.Center, box.Max);
            return ret;
        }
        //  Try to convert string to int. If not possible, null is returned.
        public static byte? GetByteFromString(string s)
        {
            byte outByte;
            if (byte.TryParse(s, out outByte) == false)
            {
                return null;
            }
            return outByte;
        }
        //  Converts spherical coordinates (horizontal and vertical angle) to cartesian coordinates (relative to sphere centre).
        //  Use radius to specify sphere's radius (set to 1 if unit sphere).
        //  Angles are in radians.
        //  Input spherical coordinate system: horisontal is angle on XZ plane starting at -Z direction, vertical is angle on YZ plan, starting at -Z direction.
        //  Output cartesian coordinate system: forward is -Z, up is +Y, right is +X
        //  Formulas for conversion from/to spherical/cartezian are from: http://en.wikipedia.org/wiki/Spherical_coordinates
        //  IMPORTANT: This should be RIGHT version of this method, instead of GetCartesianCoordinatesFromSpherical_Weird()
        public static Vector3 GetCartesianCoordinatesFromSpherical(float angleHorizontal, float angleVertical, float radius)
        {
            angleVertical = MathHelper.PiOver2 - angleVertical;
            angleHorizontal = MathHelper.Pi - angleHorizontal;

            return new Vector3(
                (float)(radius * Math.Sin(angleVertical) * Math.Sin(angleHorizontal)),
                (float)(radius * Math.Cos(angleVertical)),
                (float)(radius * Math.Sin(angleVertical) * Math.Cos(angleHorizontal)));
        }
        //  Clamping int into range <min...max> (including min and max)
        public static int GetClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        public static Vector3 GetClosestPointOnLine(ref Vector3 linePointA, ref Vector3 linePointB, ref Vector3D point)
        {
            float dist = 0;
            return GetClosestPointOnLine(ref linePointA, ref linePointB, ref point, out dist);
        }
        public static Vector3D GetClosestPointOnLine(ref Vector3D linePointA, ref Vector3D linePointB, ref Vector3D point)
        {
            double dist = 0;
            return GetClosestPointOnLine(ref linePointA, ref linePointB, ref point, out dist);
        }
        public static Vector3 GetClosestPointOnLine(ref Vector3 linePointA, ref Vector3 linePointB, ref Vector3D point, out float dist)
        {
            //	Create the vector from end point vA to our point vPoint.
            Vector3 vector1 = point - linePointA;

            //	Create a normalized direction vector from end point vA to end point vB
            Vector3 vector2 = MyUtils.Normalize(linePointB - linePointA);

            //	Use the distance formula to find the distance of the line segment (or magnitude)
            float d = Vector3.Distance(linePointA, linePointB);

            //	Using the dot product, we project the vVector1 onto the vector vVector2.
            //	This essentially gives us the distance from our projected vector from vA.
            float t = Vector3.Dot(vector2, vector1);

            dist = t;

            //	If our projected distance from vA, "t", is less than or equal to 0, it must
            //	be closest to the end point vA.  We want to return this end point.
            if (t <= 0) return linePointA;

            //	If our projected distance from vA, "t", is greater than or equal to the magnitude
            //	or distance of the line segment, it must be closest to the end point vB.  So, return vB.
            if (t >= d) return linePointB;

            //	Here we create a vector that is of length t and in the direction of vVector2
            Vector3 vector3 = vector2 * t;

            //	To find the closest point on the line segment, we just add vVector3 to the original
            //	end point vA. 
            //	Return the closest point on the line segment
            return linePointA + vector3;
        }
        public static Vector3D GetClosestPointOnLine(ref Vector3D linePointA, ref Vector3D linePointB, ref Vector3D point, out double dist)
        {
            //	Create the vector from end point vA to our point vPoint.
            Vector3D vector1 = point - linePointA;

            //	Create a normalized direction vector from end point vA to end point vB
            Vector3D vector2 = MyUtils.Normalize(linePointB - linePointA);

            //	Use the distance formula to find the distance of the line segment (or magnitude)
            var d = Vector3D.Distance(linePointA, linePointB);

            //	Using the dot product, we project the vVector1 onto the vector vVector2.
            //	This essentially gives us the distance from our projected vector from vA.
            var t = Vector3D.Dot(vector2, vector1);

            dist = t;

            //	If our projected distance from vA, "t", is less than or equal to 0, it must
            //	be closest to the end point vA.  We want to return this end point.
            if (t <= 0) return linePointA;

            //	If our projected distance from vA, "t", is greater than or equal to the magnitude
            //	or distance of the line segment, it must be closest to the end point vB.  So, return vB.
            if (t >= d) return linePointB;

            //	Here we create a vector that is of length t and in the direction of vVector2
            Vector3D vector3 = vector2 * t;

            //	To find the closest point on the line segment, we just add vVector3 to the original
            //	end point vA. 
            //	Return the closest point on the line segment
            return linePointA + vector3;
        }
        /// <summary>
        /// Returns intersection point between sphere and its edges. But only if there is intersection between sphere and one of the edges.
        /// If sphere intersects somewhere inside the triangle, this method will not detect it.
        /// </summary>
        public static Vector3? GetEdgeSphereCollision(ref Vector3D sphereCenter, float sphereRadius, ref MyTriangle_Vertexes triangle)
        {
            Vector3 intersectionPoint;

            // This returns the closest point on the current edge to the center of the sphere.
            intersectionPoint = GetClosestPointOnLine(ref triangle.Vertex0, ref triangle.Vertex1, ref sphereCenter);

            // Now, we want to calculate the distance between the closest point and the center
            float distance1 = Vector3.Distance(intersectionPoint, sphereCenter);

            // If the distance is less than the radius, there must be a collision so return true
            if (distance1 < sphereRadius)
            {
                return intersectionPoint;
            }

            // This returns the closest point on the current edge to the center of the sphere.
            intersectionPoint = GetClosestPointOnLine(ref triangle.Vertex1, ref triangle.Vertex2, ref sphereCenter);

            // Now, we want to calculate the distance between the closest point and the center
            float distance2 = Vector3.Distance(intersectionPoint, sphereCenter);

            // If the distance is less than the radius, there must be a collision so return true
            if (distance2 < sphereRadius)
            {
                return intersectionPoint;
            }

            // This returns the closest point on the current edge to the center of the sphere.
            intersectionPoint = GetClosestPointOnLine(ref triangle.Vertex2, ref triangle.Vertex0, ref sphereCenter);

            // Now, we want to calculate the distance between the closest point and the center
            float distance3 = Vector3.Distance(intersectionPoint, sphereCenter);

            // If the distance is less than the radius, there must be a collision so return true
            if (distance3 < sphereRadius)
            {
                return intersectionPoint;
            }

            // The was no intersection of the sphere and the edges of the polygon
            return null;
        }
        //  Try to convert string to float. If not possible, null is returned.
        public static float? GetFloatFromString(string s)
        {
            float outFloat;
            if (float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture.NumberFormat, out outFloat) == false)
            {
                return null;
            }
            return outFloat;
        }
        //  Try to convert string to float. If not possible, null is returned.
        //  If 's' can't be converted to a valid float, 'defaultValue' is returned
        public static float GetFloatFromString(string s, float defaultValue)
        {
            float? outFloat = GetFloatFromString(s);
            return (outFloat == null) ? defaultValue : outFloat.Value;
        }
        /// <summary>
        /// Return true if point is inside the triangle.
        /// </summary>
        public static bool GetInsidePolygonForSphereCollision(ref Vector3D point, ref MyTriangle_Vertexes triangle)
        {
            const float MATCH_FACTOR = 0.99f;		// Used to cover up the error in floating point
            float angle = 0.0f;						// Initialize the angle

            //	Spocitame uhol medzi bodmi trojuholnika a intersection bodu (ale na vypocet uhlov pouzivame funkciu ktora je
            //	bezpecna aj pre sphere coldet, problem so SafeACos())
            angle += GetAngleBetweenVectorsForSphereCollision(triangle.Vertex0 - point, triangle.Vertex1 - point);	// Find the angle between the 2 vectors and add them all up as we go along
            angle += GetAngleBetweenVectorsForSphereCollision(triangle.Vertex1 - point, triangle.Vertex2 - point);	// Find the angle between the 2 vectors and add them all up as we go along
            angle += GetAngleBetweenVectorsForSphereCollision(triangle.Vertex2 - point, triangle.Vertex0 - point);	// Find the angle between the 2 vectors and add them all up as we go along

            if (angle >= (MATCH_FACTOR * (2.0 * MathHelper.Pi)))	// If the angle is greater than 2 PI, (360 degrees)
            {
                return true;							// The point is inside of the polygon
            }

            return false;								// If you get here, it obviously wasn't inside the polygon, so Return FALSE
        }
        /// <summary>
        /// Return true if point is inside the triangle.
        /// </summary>
        public static bool GetInsidePolygonForSphereCollision(ref Vector3 point, ref MyTriangle_Vertexes triangle)
        {
            const float MATCH_FACTOR = 0.99f;		// Used to cover up the error in floating point
            float angle = 0.0f;						// Initialize the angle

            //	Spocitame uhol medzi bodmi trojuholnika a intersection bodu (ale na vypocet uhlov pouzivame funkciu ktora je
            //	bezpecna aj pre sphere coldet, problem so SafeACos())
            angle += GetAngleBetweenVectorsForSphereCollision(triangle.Vertex0 - point, triangle.Vertex1 - point);	// Find the angle between the 2 vectors and add them all up as we go along
            angle += GetAngleBetweenVectorsForSphereCollision(triangle.Vertex1 - point, triangle.Vertex2 - point);	// Find the angle between the 2 vectors and add them all up as we go along
            angle += GetAngleBetweenVectorsForSphereCollision(triangle.Vertex2 - point, triangle.Vertex0 - point);	// Find the angle between the 2 vectors and add them all up as we go along

            if (angle >= (MATCH_FACTOR * (2.0 * MathHelper.Pi)))	// If the angle is greater than 2 PI, (360 degrees)
            {
                return true;							// The point is inside of the polygon
            }

            return false;								// If you get here, it obviously wasn't inside the polygon, so Return FALSE
        }
        //  Convert string to int. If not possible (string isn't int), return null.
        public static int? GetInt32FromString(string s)
        {
            int ret;
            if (Int32.TryParse(s, out ret))
            {
                return ret;
            }
            else
            {
                return null;
            }
        }
        //  Try to convert string to int. If not possible, null is returned.
        public static int? GetIntFromString(string s)
        {
            int outInt;
            if (int.TryParse(s, out outInt) == false)
            {
                return null;
            }
            return outInt;
        }
        //  Try to convert string to int. If not possible, null is returned.
        //  If 's' can't be converted to a valid float, 'defaultValue' is returned
        public static int GetIntFromString(string s, int defaultValue)
        {
            int? outInt = GetIntFromString(s);
            return (outInt == null) ? defaultValue : outInt.Value;
        }
        /// <summary>
        /// Distance between "from" and opposite side of the "sphere". Always positive.
        /// </summary>
        public static double GetLargestDistanceToSphere(ref Vector3D from, ref BoundingSphereD sphere)
        {
            return Vector3D.Distance(from, sphere.Center) + sphere.Radius;
        }
        /// <summary>
        /// Calculates intersection between line and bounding box and if found, distance is returned. Otherwise null is returned.
        /// </summary>
        public static float? GetLineBoundingBoxIntersection(ref Line line, ref BoundingBox boundingBox)
        {
            //  Create temporary ray and do intersection. But we can't rely only on it, because ray doesn't have end, yet our line does, so we 
            //  need to check if ray-bounding_box intersection lies in the range of our line
            VRageMath.Ray ray = new VRageMath.Ray(line.From, line.Direction);
            float? intersectionDistance = boundingBox.Intersects(ray);
            if (intersectionDistance.HasValue == false)
            {
                //  No intersection between ray/line and bounding box
                return null;
            }
            else
            {
                if (intersectionDistance.Value <= line.Length)
                {
                    //  Intersection between ray/line and bounding box IS withing the range of the line
                    return intersectionDistance.Value;
                }
                else
                {
                    //  Intersection between ray/line and bounding box IS NOT withing the range of the line
                    return null;
                }
            }
        }
        /// <summary>
        /// Checks whether a ray intersects a triangleVertexes. This uses the algorithm
        /// developed by Tomas Moller and Ben Trumbore, which was published in the
        /// Journal of Graphics Tools, pitch 2, "Fast, Minimum Storage Ray-Triangle
        /// Intersection".
        ///
        /// This method is implemented using the pass-by-reference versions of the
        /// XNA math functions. Using these overloads is generally not recommended,
        /// because they make the code less readable than the normal pass-by-value
        /// versions. This method can be called very frequently in a tight inner loop,
        /// however, so in this particular case the performance benefits from passing
        /// everything by reference outweigh the loss of readability.
        /// </summary>
        public static float? GetLineTriangleIntersection(ref Line line, ref MyTriangle_Vertexes triangle)
        {
            // Compute vectors along two edges of the triangleVertexes.
            Vector3 edge1, edge2;

            Vector3.Subtract(ref triangle.Vertex1, ref triangle.Vertex0, out edge1);
            Vector3.Subtract(ref triangle.Vertex2, ref triangle.Vertex0, out edge2);

            // Compute the determinant.
            Vector3 directionCrossEdge2;
            Vector3.Cross(ref line.Direction, ref edge2, out directionCrossEdge2);

            float determinant;
            Vector3.Dot(ref edge1, ref directionCrossEdge2, out determinant);

            // If the ray is parallel to the triangleVertexes plane, there is no collision.
            if (determinant > -float.Epsilon && determinant < float.Epsilon)
            {
                return null;
            }

            float inverseDeterminant = 1.0f / determinant;

            // Calculate the U parameter of the intersection point.
            Vector3 distanceVector;
            Vector3.Subtract(ref line.From, ref triangle.Vertex0, out distanceVector);

            float triangleU;
            Vector3.Dot(ref distanceVector, ref directionCrossEdge2, out triangleU);
            triangleU *= inverseDeterminant;

            // Make sure it is inside the triangleVertexes.
            if (triangleU < 0 || triangleU > 1)
            {
                return null;
            }

            // Calculate the V parameter of the intersection point.
            Vector3 distanceCrossEdge1;
            Vector3.Cross(ref distanceVector, ref edge1, out distanceCrossEdge1);

            float triangleV;
            Vector3.Dot(ref line.Direction, ref distanceCrossEdge1, out triangleV);
            triangleV *= inverseDeterminant;

            // Make sure it is inside the triangleVertexes.
            if (triangleV < 0 || triangleU + triangleV > 1)
            {
                return null;
            }

            // Compute the distance along the ray to the triangleVertexes.
            float rayDistance;
            Vector3.Dot(ref edge2, ref distanceCrossEdge1, out rayDistance);
            rayDistance *= inverseDeterminant;

            // Is the triangleVertexes behind the ray origin?
            if (rayDistance < 0)
            {
                return null;
            }

            //  Does the intersection point lie on the line (ray hasn't end, but line does)
            if (rayDistance > line.Length) return null;

            return rayDistance;
        }
        //  Find max numeric value in enum
        public static int GetMaxValueFromEnum<T>()
        {
            Array values = Enum.GetValues(typeof(T));

            //  Doesn't make sence to find max in empty enum            
            MyDebug.AssertDebug(values.Length >= 1);

            int max = int.MinValue;
            Type underlyingType = Enum.GetUnderlyingType(typeof(T));
            if (underlyingType == typeof(System.Byte))
            {
                foreach (byte value in values)
                {
                    if (value > max) max = value;
                }
            }
            else if (underlyingType == typeof(System.Int16))
            {
                foreach (short value in values)
                {
                    if (value > max) max = value;
                }
            }
            else if (underlyingType == typeof(System.UInt16))
            {
                foreach (ushort value in values)
                {
                    if (value > max) max = value;
                }
            }
            else if (underlyingType == typeof(System.Int32))
            {
                foreach (int value in values)
                {
                    if (value > max) max = value;
                }
            }
            else
            {
                //  Unhandled underlying type - probably "long"
                throw new InvalidBranchException();
            }

            return max;
        }
        public static Vector3 GetNormalVectorFromTriangle(ref MyTriangle_Vertexes inputTriangle)
        {
            //return MyVRageUtils.Normalize(Vector3.Cross(inputTriangle.Vertex2 - inputTriangle.Vertex0, inputTriangle.Vertex1 - inputTriangle.Vertex0));
            return Vector3.Normalize(Vector3.Cross(inputTriangle.Vertex2 - inputTriangle.Vertex0, inputTriangle.Vertex1 - inputTriangle.Vertex0));
        }
        public static double GetPointLineDistance(ref Vector3D linePointA, ref Vector3D linePointB, ref Vector3D point)
        {
            Vector3D line = linePointB - linePointA;
            return Vector3D.Cross(line, point - linePointA).Length() / line.Length();
        }
        public static void GetPolyLineQuad(out MyQuadD retQuad, ref MyPolyLineD polyLine, Vector3D cameraPosition)
        {
            Vector3D cameraToPoint = MyUtils.Normalize(cameraPosition - polyLine.Point0);
            Vector3D sideVector = GetVector3Scaled(Vector3D.Cross(polyLine.LineDirectionNormalized, cameraToPoint), polyLine.Thickness);

            retQuad.Point0 = polyLine.Point0 - sideVector;
            retQuad.Point1 = polyLine.Point1 - sideVector;
            retQuad.Point2 = polyLine.Point1 + sideVector;
            retQuad.Point3 = polyLine.Point0 + sideVector;
        }
        public static T GetRandomItem<T>(this T[] list)
        {
            return list[GetRandomInt(list.Length)];
        }
        public static T GetRandomItemFromList<T>(this List<T> list)
        {
            return list[GetRandomInt(list.Count)];
        }
        /// <summary>
        /// Calculates distance from point 'from' to boundary of 'sphere'. If point is inside the sphere, distance will be negative.
        /// </summary>
        public static double GetSmallestDistanceToSphere(ref Vector3D from, ref BoundingSphereD sphere)
        {
            return Vector3D.Distance(from, sphere.Center) - sphere.Radius;
        }
        public static double GetSmallestDistanceToSphereAlwaysPositive(ref Vector3D from, ref BoundingSphereD sphere)
        {
            var distance = GetSmallestDistanceToSphere(ref from, ref sphere);
            if (distance < 0) distance = 0;
            return distance;
        }
        /// <summary>
        /// This tells if a sphere is BEHIND, in FRONT, or INTERSECTS a plane, also it's distance
        /// </summary>
        public static MySpherePlaneIntersectionEnum GetSpherePlaneIntersection(ref BoundingSphereD sphere, ref PlaneD plane, out double distanceFromPlaneToSphere)
        {
            //  First we need to find the distance our polygon plane is from the origin.
            var planeDistance = plane.D;

            //  Here we use the famous distance formula to find the distance the center point
            //  of the sphere is from the polygon's plane.  
            distanceFromPlaneToSphere = (plane.Normal.X * sphere.Center.X + plane.Normal.Y * sphere.Center.Y + plane.Normal.Z * sphere.Center.Z + planeDistance);

            //  If the absolute value of the distance we just found is less than the radius, 
            //  the sphere intersected the plane.
            if (Math.Abs(distanceFromPlaneToSphere) < sphere.Radius)
            {
                return MySpherePlaneIntersectionEnum.INTERSECTS;
            }
            else if (distanceFromPlaneToSphere >= sphere.Radius)
            {
                //  Else, if the distance is greater than or equal to the radius, the sphere is
                //  completely in FRONT of the plane.
                return MySpherePlaneIntersectionEnum.FRONT;
            }

            //  If the sphere isn't intersecting or in FRONT of the plane, it must be BEHIND
            return MySpherePlaneIntersectionEnum.BEHIND;
        }
        /// <summary>
        /// This tells if a sphere is BEHIND, in FRONT, or INTERSECTS a plane, also it's distance
        /// </summary>
        public static MySpherePlaneIntersectionEnum GetSpherePlaneIntersection(ref BoundingSphereD sphere, ref MyPlane plane, out float distanceFromPlaneToSphere)
        {
            //  First we need to find the distance our polygon plane is from the origin.
            float planeDistance = plane.GetPlaneDistance();

            //  Here we use the famous distance formula to find the distance the center point
            //  of the sphere is from the polygon's plane.  
            distanceFromPlaneToSphere = (float)(plane.Normal.X * sphere.Center.X + plane.Normal.Y * sphere.Center.Y + plane.Normal.Z * sphere.Center.Z + planeDistance);

            //  If the absolute value of the distance we just found is less than the radius, 
            //  the sphere intersected the plane.
            if (Math.Abs(distanceFromPlaneToSphere) < sphere.Radius)
            {
                return MySpherePlaneIntersectionEnum.INTERSECTS;
            }
            else if (distanceFromPlaneToSphere >= sphere.Radius)
            {
                //  Else, if the distance is greater than or equal to the radius, the sphere is
                //  completely in FRONT of the plane.
                return MySpherePlaneIntersectionEnum.FRONT;
            }

            //  If the sphere isn't intersecting or in FRONT of the plane, it must be BEHIND
            return MySpherePlaneIntersectionEnum.BEHIND;
        }
        /// <summary>
        /// Method returns intersection point between sphere and triangle (which is defined by vertexes and plane).
        /// If no intersection found, method returns null.
        /// See below how intersection point can be calculated, because it's not so easy - for example sphere vs. triangle will 
        /// hardly generate just intersection point... more like intersection area or something.
        /// </summary>
        public static Vector3? GetSphereTriangleIntersection(ref BoundingSphereD sphere, ref MyPlane trianglePlane, ref MyTriangle_Vertexes triangle)
        {
            //	Vzdialenost gule od roviny trojuholnika
            float distance;

            //	Zistim, ci sa gula nachadza pred alebo za rovinou trojuholnika, alebo ju presekava
            MySpherePlaneIntersectionEnum spherePlaneIntersection = GetSpherePlaneIntersection(ref sphere, ref trianglePlane, out distance);

            //	Ak gula presekava rovinu, tak hladam pseudo-priesecnik
            if (spherePlaneIntersection == MySpherePlaneIntersectionEnum.INTERSECTS)
            {
                //	Offset ktory pomoze vypocitat suradnicu stredu gule premietaneho na rovinu trojuholnika
                Vector3 offset = trianglePlane.Normal * distance;

                //	Priesecnik na rovine trojuholnika, je to premietnuty stred gule na rovine trojuholnika
                Vector3 intersectionPoint;
                intersectionPoint.X = (float)(sphere.Center.X - offset.X);
                intersectionPoint.Y = (float)(sphere.Center.Y - offset.Y);
                intersectionPoint.Z = (float)(sphere.Center.Z - offset.Z);

                if (GetInsidePolygonForSphereCollision(ref intersectionPoint, ref triangle))		//	Ak priesecnik nachadza v trojuholniku
                {
                    //	Toto je pripad, ked sa podarilo premietnut stred gule na rovinu trojuholnika a tento priesecnik sa
                    //	nachadza vnutri trojuholnika (tzn. sedia uhly)
                    return intersectionPoint;
                }
                else													//	Ak sa priesecnik nenachadza v trojuholniku, este stale sa moze nachadzat na hrane trojuholnika
                {
                    Vector3? edgeIntersection = GetEdgeSphereCollision(ref sphere.Center, (float)sphere.Radius / 1.0f, ref triangle);
                    if (edgeIntersection != null)
                    {
                        //	Toto je pripad, ked sa priemietnuty stred gule nachadza mimo trojuholnika, ale intersection gule a trojuholnika tam
                        //	je, pretoze gula presekava jednu z hran trojuholnika. Takze vratim suradnice priesecnika na jednej z hran.
                        return edgeIntersection.Value;
                    }
                }
            }

            //	Sphere doesn't collide with any triangle
            return null;
        }
        /// <summary>
        /// Method returns intersection point between sphere and triangle (which is defined by vertexes and plane).
        /// If no intersection found, method returns null.
        /// See below how intersection point can be calculated, because it's not so easy - for example sphere vs. triangle will 
        /// hardly generate just intersection point... more like intersection area or something.
        /// </summary>
        public static Vector3? GetSphereTriangleIntersection(ref BoundingSphereD sphere, ref PlaneD trianglePlane, ref MyTriangle_Vertexes triangle)
        {
            //	Vzdialenost gule od roviny trojuholnika
            double distance;

            //	Zistim, ci sa gula nachadza pred alebo za rovinou trojuholnika, alebo ju presekava
            MySpherePlaneIntersectionEnum spherePlaneIntersection = GetSpherePlaneIntersection(ref sphere, ref trianglePlane, out distance);

            //	Ak gula presekava rovinu, tak hladam pseudo-priesecnik
            if (spherePlaneIntersection == MySpherePlaneIntersectionEnum.INTERSECTS)
            {
                //	Offset ktory pomoze vypocitat suradnicu stredu gule premietaneho na rovinu trojuholnika
                Vector3D offset = trianglePlane.Normal * distance;

                //	Priesecnik na rovine trojuholnika, je to premietnuty stred gule na rovine trojuholnika
                Vector3D intersectionPoint;
                intersectionPoint.X = sphere.Center.X - offset.X;
                intersectionPoint.Y = sphere.Center.Y - offset.Y;
                intersectionPoint.Z = sphere.Center.Z - offset.Z;

                if (GetInsidePolygonForSphereCollision(ref intersectionPoint, ref triangle))		//	Ak priesecnik nachadza v trojuholniku
                {
                    //	Toto je pripad, ked sa podarilo premietnut stred gule na rovinu trojuholnika a tento priesecnik sa
                    //	nachadza vnutri trojuholnika (tzn. sedia uhly)
                    return intersectionPoint;
                }
                else													//	Ak sa priesecnik nenachadza v trojuholniku, este stale sa moze nachadzat na hrane trojuholnika
                {
                    Vector3? edgeIntersection = GetEdgeSphereCollision(ref sphere.Center, (float)sphere.Radius / 1.0f, ref triangle);
                    if (edgeIntersection != null)
                    {
                        //	Toto je pripad, ked sa priemietnuty stred gule nachadza mimo trojuholnika, ale intersection gule a trojuholnika tam
                        //	je, pretoze gula presekava jednu z hran trojuholnika. Takze vratim suradnice priesecnika na jednej z hran.
                        return edgeIntersection.Value;
                    }
                }
            }

            //	Sphere doesn't collide with any triangle
            return null;
        }
        public static Vector3D GetTransformNormalNormalized(Vector3D vec, ref MatrixD matrix)
        {
            Vector3D ret;
            Vector3D.TransformNormal(ref vec, ref matrix, out ret);
            ret = MyUtils.Normalize(ret);
            return ret;
        }
        public static Vector3D GetVector3Scaled(Vector3D originalVector, float newLength)
        {
            if (newLength == 0.0f)
            {
                //	V pripade ze chceme aby mal vektor nulovu dlzku, tak komponenty vektora zmenime
                //	na nulu rucne, lebo delenie nulou by sposobilo vznik neplatnych floatov a problemy!
                return Vector3D.Zero;
            }
            else
            {
                double originalLength = originalVector.Length();

                //	Ak je dlzka povodneho vektora nulova, nema zmysel scalovat jeho dlzku
                if (originalLength == 0.0)
                {
                    return Vector3D.Zero;
                }

                //  Return scaled vector
                double mul = newLength / originalLength;
                return new Vector3D(originalVector.X * mul, originalVector.Y * mul, originalVector.Z * mul);
            }
        }

        /// <summary>
        /// Check intersection between line and bounding sphere
        /// We don't use BoundingSphere.Contains(Ray ...) because ray doesn't have an end, but line does, so we need
        /// to check if line really intersects the sphere.
        /// </summary>
        public static bool IsLineIntersectingBoundingSphere(ref LineD line, ref BoundingSphereD boundingSphere)
        {
            //  Create temporary ray and do intersection. But we can't rely only on it, because ray doesn't have end, yet our line does, so we 
            //  need to check if ray-bounding_sphere intersection lies in the range of our line
            VRageMath.RayD ray = new VRageMath.RayD(line.From, line.Direction);
            double? intersectionDistance = boundingSphere.Intersects(ray);
            if (intersectionDistance.HasValue == false)
            {
                //  No intersection between ray/line and bounding sphere
                return false;
            }
            else
            {
                if (intersectionDistance.Value <= line.Length)
                {
                    //  Intersection between ray/line and bounding sphere IS withing the range of the line
                    return true;
                }
                else
                {
                    //  Intersection between ray/line and bounding sphere IS NOT withing the range of the line
                    return false;
                }
            }
        }
        //  We want to skip all wrong triangles, those that have two vertex at almost the same location, etc.
        //  We do it simply, by calculating triangle normal and then checking if this normal has length large enough
        public static bool IsWrongTriangle(Vector3 vertex0, Vector3 vertex1, Vector3 vertex2)
        {
            //  Distance between two vertexes is the fastest test
            Vector3 triangleEdgeVector1 = vertex2 - vertex0;
            if (triangleEdgeVector1.LengthSquared() <= MyMathConstants.EPSILON_SQUARED) return true;

            //  Distance between two vertexes is the fastest test
            Vector3 triangleEdgeVector2 = vertex1 - vertex0;
            if (triangleEdgeVector2.LengthSquared() <= MyMathConstants.EPSILON_SQUARED) return true;

            //  Distance between two vertexes is the fastest test
            Vector3 triangleEdgeVector3 = vertex1 - vertex2;
            if (triangleEdgeVector3.LengthSquared() <= MyMathConstants.EPSILON_SQUARED) return true;

            //  But we also need to do a cross product, because distance tests are not sufficient in case when all vertexes lie on a line
            //   Vector3 norm;
            //   Vector3.Cross(ref triangleEdgeVector1, ref triangleEdgeVector2, out norm);
            //   if (norm.LengthSquared() < EPSILON) return true;

            return false;
        }
        public static Vector3D LinePlaneIntersection(Vector3D planePoint, Vector3 planeNormal, Vector3D lineStart, Vector3 lineDir)
        {
            Debug.Assert(planeNormal.Length().IsEqual(1f));
            Debug.Assert(lineDir.Length().IsEqual(1f));

            var nominator = Vector3D.Dot(planePoint - lineStart, planeNormal);
            var denominator = Vector3.Dot(lineDir, planeNormal);
            return lineStart + (Vector3D)lineDir * (nominator / denominator);
        }
        /// <summary>
        /// Protected normalize with assert
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static Vector3 Normalize(Vector3 vec)
        {
            //  Check if vector has reasonable length, otherwise Normalize is going to fail
            //  AssertLengthValid(ref vec);
            return Vector3.Normalize(vec);
        }
        public static Vector3D Normalize(Vector3D vec)
        {
            //  Check if vector has reasonable length, otherwise Normalize is going to fail
            AssertLengthValid(ref vec);
            return Vector3D.Normalize(vec);
        }
        /// <summary>
        /// Protected normalize with assert
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static void Normalize(ref Vector3 vec, out Vector3 normalized)
        {
            //  Check if vector has reasonable length, otherwise Normalize is going to fail
            AssertLengthValid(ref vec);
            Vector3.Normalize(ref vec, out normalized);
        }
        public static void Normalize(ref Vector3D vec, out Vector3D normalized)
        {
            //  Check if vector has reasonable length, otherwise Normalize is going to fail
            AssertLengthValid(ref vec);
            Vector3D.Normalize(ref vec, out normalized);
        }
        /// <summary>
        /// Protected normalize with assert
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static void Normalize(ref Matrix m, out Matrix normalized)
        {
            normalized = Matrix.CreateWorld(
                m.Translation,
                Normalize(m.Forward),
                Normalize(m.Up));
        }
        public static void Normalize(ref MatrixD m, out MatrixD normalized)
        {
            normalized = MatrixD.CreateWorld(
                m.Translation,
                Normalize(m.Forward),
                Normalize(m.Up));
        }
        // Get the yaw,pitch and roll from a rotation matrix.
        public static void RotationMatrixToYawPitchRoll(ref Matrix mx, out float yaw, out float pitch, out float roll)
        {
            float clamped = mx.M32;
            if (clamped > 1) clamped = 1;
            else if (clamped < -1) clamped = -1;
            pitch = (float)Math.Asin(-clamped);
            float threshold = 0.001f;
            float test = (float)Math.Cos(pitch);
            if (test > threshold)
            {
                roll = (float)Math.Atan2(mx.M12, mx.M22);
                yaw = (float)Math.Atan2(mx.M31, mx.M33);
            }
            else
            {
                roll = (float)Math.Atan2(-mx.M21, mx.M11);
                yaw = 0.0f;
            }
        }
        public static void SerializeValue(XmlWriter writer, Vector3 v)
        {
            writer.WriteValue(v.X.ToString(CultureInfo.InvariantCulture) + " " + v.Y.ToString(CultureInfo.InvariantCulture) + " " + v.Z.ToString(CultureInfo.InvariantCulture));
        }
        public static void SerializeValue(XmlWriter writer, Vector4 v)
        {
            writer.WriteValue(v.X.ToString(CultureInfo.InvariantCulture) + " " + v.Y.ToString(CultureInfo.InvariantCulture) + " " + v.Z.ToString(CultureInfo.InvariantCulture) + " " + v.W.ToString(CultureInfo.InvariantCulture));
        }

        public static void ShuffleList<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = GetRandomInt(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        public static void SplitStringBuilder(StringBuilder destination, StringBuilder source, string splitSeparator)
        {
            char currentChar;
            int length = source.Length;
            int splitLength = splitSeparator.Length;
            int currentSplitIndex = 0;

            for (int i = 0; i < length; i++)
            {
                currentChar = source[i];

                if (currentChar == splitSeparator[currentSplitIndex])
                {
                    currentSplitIndex++;
                    // total split separator match, we append new line
                    if (currentSplitIndex == splitLength)
                    {
                        destination.AppendLine();
                        m_splitBuffer.Clear();
                        currentSplitIndex = 0;
                    }
                    // if only part match, we add to split buffer
                    else
                    {
                        m_splitBuffer.Add(currentChar);
                    }
                }
                else
                {
                    // if there was part split match, we must append characters from split buffer
                    if (currentSplitIndex > 0)
                    {
                        foreach (char c in m_splitBuffer)
                        {
                            destination.Append(c);
                        }
                        m_splitBuffer.Clear();
                        currentSplitIndex = 0;
                    }
                    // we append current char
                    destination.Append(currentChar);
                }
            }

            // we must append characters which remain in split buffer
            foreach (char c in m_splitBuffer)
            {
                destination.Append(c);
            }
            m_splitBuffer.Clear();
        }
        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
        public static void VectorPlaneRotation(Vector3D xVector, Vector3D yVector, out Vector3D xOut, out Vector3D yOut, float angle)
        {
            Vector3D newX = xVector * Math.Cos(angle) + yVector * Math.Sin(angle);
            Vector3D newY = xVector * Math.Cos(angle + Math.PI / 2.0) + yVector * Math.Sin(angle + Math.PI / 2.0);
            xOut = newX;
            yOut = newY;
        }

        /// <summary>
        /// When location is null, creates new instance, stores it in location and returns it.
        /// When location is not null, returns it.
        /// </summary>
        public static T Init<T>(ref T location)
            where T : class, new()
        {
            if (location == null)
                location = new T();
            return location;
        }

        public static void InterlockedMax(ref long storage, long value)
        {
            long localMax = Interlocked.Read(ref storage);
            while (value > localMax)
            {
                Interlocked.CompareExchange(ref storage, value, localMax);
                localMax = Interlocked.Read(ref storage);
            }
        }


#if BLITCREMENTAL
        //////////////////////////////////////////////////////////////
        //contents of MyUtils-Hash.
        //////////////////////////////////////////////////////////////

        const int HashSeed = -2128831035; // 0x811C9DC5

        private static int HashStep(int value, int hash)
        {
            hash = hash ^ value;
            hash *= 16777619;
            return hash;
        }

        public unsafe static int GetHash(double d, int hash = HashSeed)
        {
            if (d == 0)
                return hash;//both positive and negative zeros go to same hash
            UInt64 value = *(UInt64*)(&d);
            unchecked
            {
                hash = HashStep((int)value, HashStep((int)(value >> 32), hash));
            }
            return hash;
        }

        public static int GetHash(string str, int hash = HashSeed)
        {
            //two chars per int32
            if (str != null)
            {
                int i = 0;
                for (; i < str.Length - 1; i += 2)
                {
                    hash = HashStep(((int)str[i] << 16) + (int)str[i + 1], hash);
                }
                if ((str.Length & 1) != 0)
                {//last char
                    hash = HashStep((int)str[i], hash);
                }
            }
            return hash;
        }

        //public static Int32 GetHash(string text, Int32 seed = 0)
        //{
        //    int hash = seed;
        //    unchecked
        //    {
        //        if (text != null)
        //        {
        //            foreach (char c in text)
        //            {
        //                hash = hash * 37 + c;
        //            }
        //        }
        //        return hash;
        //    }
        //}

        //////////////////////////////////////////////////////////////
        //Contents of MyUtils-Mesh
        //////////////////////////////////////////////////////////////


        struct Edge : IEquatable<Edge>
        {
            public int I0;
            public int I1;

            public bool Equals(Edge other)
            {
                //return Equals(other.GetHashCode(), GetHashCode());
                return other.GetHashCode() == GetHashCode();
            }

            public override int GetHashCode()
            {
                return I0 < I1 ? (I0.GetHashCode() * 397) ^ I1.GetHashCode() : (I1.GetHashCode() * 397) ^ I0.GetHashCode();
            }
        }

        public static void GetOpenBoundaries(Vector3[] vertices, int[] indices, List<Vector3> openBoundaries)
        {
            System.Diagnostics.Debug.Assert(indices.Length > 0);
            System.Diagnostics.Debug.Assert(indices.Length % 3 == 0);

            Dictionary<int, List<int>> indicesRemap = new Dictionary<int, List<int>>(); //for same vertices
            for (int i = 0; i < vertices.Length; i++)
                for (int j = 0; j < i; j++)
                {
                    if (MyUtils.IsEqual(vertices[j], vertices[i]))
                    {
                        if (!indicesRemap.ContainsKey(j))
                            indicesRemap[j] = new List<int>();

                        indicesRemap[j].Add(i);
                        break;
                    }
                }

            foreach (var pair in indicesRemap)
            {
                foreach (var remapValue in pair.Value)
                {
                    for (int i = 0; i < indices.Length; i++)
                    {
                        if (indices[i] == remapValue)
                            indices[i] = pair.Key;
                    }
                }
            }


            Dictionary<Edge, int> edgeCounts = new Dictionary<Edge, int>();

            for (int i = 0; i < indices.Length; i += 3)
            {
                AddEdge(indices[i], indices[i + 1], edgeCounts);
                AddEdge(indices[i + 1], indices[i + 2], edgeCounts);
                AddEdge(indices[i + 2], indices[i], edgeCounts);
            }

            openBoundaries.Clear();
            foreach (var edgeCount in edgeCounts)
            {
                System.Diagnostics.Debug.Assert(edgeCount.Value > 0);

                if (edgeCount.Value == 1)
                {
                    openBoundaries.Add(vertices[edgeCount.Key.I0]);
                    openBoundaries.Add(vertices[edgeCount.Key.I1]);
                }
            }
        }

        static void AddEdge(int i0, int i1, Dictionary<Edge, int> edgeCounts)
        {
            Edge edge = new Edge() { I0 = i0, I1 = i1 };

            System.Diagnostics.Debug.Assert(edge.I0 != edge.I1);

            var hash = edge.GetHashCode();

            if (edgeCounts.ContainsKey(edge))
                edgeCounts[edge] = edgeCounts[edge] + 1;
            else
                edgeCounts[edge] = 1;
        }

        //////////////////////////////////////////////////////////////
        // MyUtils-Random
        //////////////////////////////////////////////////////////////

        [ThreadStatic]
        static Random m_secretRandom;

        static Random m_random
        {
            get
            {
                if (m_secretRandom == null)
                {
                    m_secretRandom = new Random();
                }
                return m_secretRandom;
            }
        }

        public static int GetRandomInt(int maxValue)
        {
            return m_random.Next(maxValue);
        }

        //  Return random int in range <minValue...maxValue>, the range of return values includes minValue but not maxValue
        public static int GetRandomInt(int minValue, int maxValue)
        {
            return m_random.Next(minValue, maxValue);
        }

        /// <summary>
        /// Returns a uniformly-distributed random vector from inside of a box (-1,-1,-1), (1, 1, 1)
        /// </summary>
        public static Vector3 GetRandomVector3()
        {
            return new Vector3(GetRandomFloat(-1, 1), GetRandomFloat(-1, 1), GetRandomFloat(-1, 1));
        }

        /// <summary>
        /// Returns a uniformly-distributed random vector from inside of a box (-1,-1,-1), (1, 1, 1)
        /// </summary>
        public static Vector3D GetRandomVector3D()
        {
            return new Vector3D(GetRandomFloat(-1, 1), GetRandomFloat(-1, 1), GetRandomFloat(-1, 1));
        }

        public static Vector3D GetRandomPerpendicularVector(ref Vector3D axis)
        {
            Debug.Assert(Vector3D.IsUnit(ref axis));
            Vector3D tangent = Vector3D.CalculatePerpendicularVector(axis);
            Vector3D bitangent; Vector3D.Cross(ref axis, ref tangent, out bitangent);
            double angle = GetRandomDouble(0, 2 * MathHelper.Pi);
            return Math.Cos(angle) * tangent + Math.Sin(angle) * bitangent;
        }

        public static Vector3D GetRandomDiscPosition(ref Vector3D center, double radius, ref Vector3D tangent, ref Vector3D bitangent)
        {
            Debug.Assert(Vector3D.IsUnit(ref tangent));
            Debug.Assert(Vector3D.IsUnit(ref bitangent));
            double radial = Math.Sqrt(GetRandomDouble(0, 1) * radius * radius);
            double angle = GetRandomDouble(0, 2 * MathHelper.Pi);
            return center + radial * (Math.Cos(angle) * tangent + Math.Sin(angle) * bitangent);
        }

        public static Vector3 GetRandomBorderPosition(ref BoundingSphere sphere)
        {
            return sphere.Center + GetRandomVector3Normalized() * sphere.Radius;
        }

        public static Vector3D GetRandomBorderPosition(ref BoundingSphereD sphere)
        {
            return sphere.Center + GetRandomVector3Normalized() * (float)sphere.Radius;
        }

        public static Vector3 GetRandomPosition(ref BoundingBox box)
        {
            return box.Center + GetRandomVector3() * box.HalfExtents;
        }

        public static Vector3D GetRandomPosition(ref BoundingBoxD box)
        {
            return box.Center + GetRandomVector3() * box.HalfExtents;
        }

        public static Vector3 GetRandomBorderPosition(ref BoundingBox box)
        {
            BoundingBoxD bbox = BoundingBoxD.ToBoundingBoxD(box);
            return new Vector3(GetRandomBorderPosition(ref bbox));
        }

        public static Vector3D GetRandomBorderPosition(ref BoundingBoxD box)
        {
            Vector3D vec = box.Size;

            // First, sample one of the six faces according to the face areas.
            // Then, sample point uniformly in the sampled face
            var surfaceReciproc = 2.0 / box.SurfaceArea;
            var probXY = vec.X * vec.Y * surfaceReciproc;
            var probXZ = vec.X * vec.Z * surfaceReciproc;
            var probYZ = 1.0f - probXY - probXZ;

            var rnd = m_random.NextDouble();
            if (rnd < probXY)
            {
                if (rnd < probXY * 0.5)
                    vec.Z = box.Min.Z;
                else
                    vec.Z = box.Max.Z;
                vec.X = GetRandomDouble(box.Min.X, box.Max.X);
                vec.Y = GetRandomDouble(box.Min.Y, box.Max.Y);
                return vec;
            }

            rnd -= probXY;
            if (rnd < probXZ)
            {
                if (rnd < probXZ * 0.5)
                    vec.Y = box.Min.Y;
                else
                    vec.Y = box.Max.Y;
                vec.X = GetRandomDouble(box.Min.X, box.Max.X);
                vec.Z = GetRandomDouble(box.Min.Z, box.Max.Z);
                return vec;
            }

            rnd -= probYZ;
            if (rnd < probYZ * 0.5)
                vec.X = box.Min.X;
            else
                vec.X = box.Max.X;
            vec.Y = GetRandomDouble(box.Min.Y, box.Max.Y);
            vec.Z = GetRandomDouble(box.Min.Z, box.Max.Z);
            return vec;
        }

        //  Return random Vector3, always normalized
        public static Vector3 GetRandomVector3Normalized()
        {
            float phi = GetRandomRadian();
            float z = GetRandomFloat(-1.0f, 1.0f);
            float root = (float)Math.Sqrt(1.0 - z * z);

            return new Vector3D(root * Math.Cos(phi), root * Math.Sin(phi), z);
        }

        //  Random vector distributed over the hemisphere about normal. 
        //  Returns random vector that always lies in hemisphere (half-sphere) defined by 'normal'
        public static Vector3 GetRandomVector3HemisphereNormalized(Vector3 normal)
        {
            Vector3 randomVector = GetRandomVector3Normalized();

            if (Vector3.Dot(randomVector, normal) < 0)
            {
                return -randomVector;
            }
            else
            {
                return randomVector;
            }
        }

        //  Random vector distributed over the circle about normal. 
        //  Returns random vector that always lies on circle
        public static Vector3 GetRandomVector3CircleNormalized()
        {
            float angle = MyUtils.GetRandomRadian();

            Vector3 v = new Vector3(
                (float)Math.Sin(angle),
                0,
                (float)Math.Cos(angle));

            return v;
        }

        //  Return by random +1 or -1. Nothing else. Propability is 50/50.
        public static float GetRandomSign()
        {
            return Math.Sign((float)m_random.NextDouble() - 0.5f);
        }

        public static float GetRandomFloat()
        {
            return (float)m_random.NextDouble();
        }

        //  Return random float in range <minValue...maxValue>
        public static float GetRandomFloat(float minValue, float maxValue)
        {
            return (float)m_random.NextDouble() * (maxValue - minValue) + minValue;
        }

        //  Return random double in range <minValue...maxValue>
        public static double GetRandomDouble(double minValue, double maxValue)
        {
            return m_random.NextDouble() * (maxValue - minValue) + minValue;
        }

        //  Return random radian, covering whole circle 0..360 degrees (but returned value is in radians)
        public static float GetRandomRadian()
        {
            return GetRandomFloat(0, 2 * MathHelper.Pi);
        }

        static byte[] m_randomBuffer = new byte[8];
        public static long GetRandomLong()
        {
            m_random.NextBytes(m_randomBuffer);
            return BitConverter.ToInt64(m_randomBuffer, 0);
        }

        /// <summary>
        /// Returns a random TimeSpan between begin (inclusive) and end (exclusive).
        /// </summary>
        public static TimeSpan GetRandomTimeSpan(TimeSpan begin, TimeSpan end)
        {
            long rndTicks = GetRandomLong();
            return new TimeSpan(begin.Ticks + rndTicks % (end.Ticks - begin.Ticks));
        }

        //////////////////////////////////////////////////////////////
        // MyUtils-FileSystem
        //////////////////////////////////////////////////////////////

        /// <summary>
        /// Vytvori zadany adresar. Automaticky povytvara celu adresarovu strukturu, teda ak chcem vytvorit c:\volaco\opica
        /// a c:\volaco zatial neexistuje, tak tato metoda ho vytvori.
        /// </summary>
        /// <param name="folderPath"></param>
        public static void CreateFolder(String folderPath)
        {
            System.IO.Directory.CreateDirectory(folderPath);
        }

        // SHALLOW copy of a directory
        public static void CopyDirectory(string source, string destination)
        {
            if (System.IO.Directory.Exists(source))
            {
                if (!Directory.Exists(destination))
                    Directory.CreateDirectory(destination);

                string[] files = Directory.GetFiles(source);

                // Copy the files and overwrite destination files if they already exist.
                foreach (string s in files)
                {
                    // Use static Path methods to extract only the file name from the path.
                    string fileName = Path.GetFileName(s);
                    string destFile = Path.Combine(destination, fileName);
                    File.Copy(s, destFile, true);
                }
            }
        }

#if true //!BLIT
        // Strips invalid chars in a filename (:, @, /, etc...)
        public static string StripInvalidChars(string filename)
        {
            return Path.GetInvalidFileNameChars().Aggregate(filename, (current, c) => current.Replace(c.ToString(), string.Empty));
        }
#endif

        //////////////////////////////////////////////////////////////
        // MyUtils-String.cs
        //////////////////////////////////////////////////////////////

        public const string C_CRLF = "\r\n";

        /// <summary>
        /// Default number suffix, k = thousand, m = million, g/b = billion
        /// </summary>
        public static Tuple<string, float>[] DefaultNumberSuffix = new Tuple<string, float>[]
        {
            new Tuple<string, float>("k", 1000),
            new Tuple<string, float>("m", 1000 * 1000),
            new Tuple<string, float>("g", 1000 * 1000 * 1000),
            new Tuple<string, float>("b", 1000 * 1000 * 1000),
        };

        //  Example: for AlignIntToRight(12, 4, "0") it returns "0012"
        public static string AlignIntToRight(int value, int charsCount, char ch)
        {
            string ret = value.ToString();
            int length = ret.Length;
            if (length > charsCount) return ret;
            return new string(ch, charsCount - length) + ret;
        }

        public static bool TryParseWithSuffix(this string text, NumberStyles numberStyle, IFormatProvider formatProvider, out float value, Tuple<string, float>[] suffix = null)
        {
            foreach (var s in suffix ?? DefaultNumberSuffix)
            {
                if (text.EndsWith(s.Item1, StringComparison.InvariantCultureIgnoreCase))
                {
                    bool result = float.TryParse(text.Substring(0, text.Length - s.Item1.Length), numberStyle, formatProvider, out value);
                    value *= s.Item2;
                    return result;
                }
            }
            return float.TryParse(text, out value);
        }

        #region Coordinate computation from alignment
        /// <summary>
        /// Aligns rectangle, works in screen/texture/pixel coordinates, not normalized coordinates.
        /// </summary>
        /// <returns>Pixel coordinates for texture.</returns>
        public static Vector2 GetCoordAligned(Vector2 coordScreen, Vector2 size, MyGuiDrawAlignEnum drawAlign)
        {
            switch (drawAlign)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP:
                    return coordScreen;

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER:
                    return coordScreen - size * 0.5f;

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP:
                    return coordScreen - size * new Vector2(0.5f, 0.0f);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM:
                    return coordScreen - size * new Vector2(0.5f, 1.0f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM:
                    return coordScreen - size;

                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER:
                    return coordScreen - size * new Vector2(0.0f, 0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER:
                    return coordScreen - size * new Vector2(1.0f, 0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM:
                    return coordScreen - size * new Vector2(0.0f, 1.0f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP:
                    return coordScreen - size * new Vector2(1.0f, 0.0f);

                default:
                    throw new InvalidBranchException();
            }
        }

        /// <summary>
        /// Modifies input coordinate (in center) using alignment and
        /// size of the rectangle. Result is at position inside rectangle
        /// specified by alignment.
        /// </summary>
        public static Vector2 GetCoordAlignedFromCenter(Vector2 coordCenter, Vector2 size, MyGuiDrawAlignEnum drawAlign)
        {
            switch (drawAlign)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP: return coordCenter + size * new Vector2(-0.5f, -0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER: return coordCenter + size * new Vector2(-0.5f, 0.0f);
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM: return coordCenter + size * new Vector2(-0.5f, 0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP: return coordCenter + size * new Vector2(0.0f, -0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER: return coordCenter;
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM: return coordCenter + size * new Vector2(0.0f, 0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP: return coordCenter + size * new Vector2(0.5f, -0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER: return coordCenter + size * new Vector2(0.5f, 0.0f);
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM: return coordCenter + size * new Vector2(0.5f, 0.5f);

                default:
                    throw new InvalidBranchException();
            }
        }

        public static Vector2 GetCoordAlignedFromTopLeft(Vector2 topLeft, Vector2 size, MyGuiDrawAlignEnum drawAlign)
        {
            switch (drawAlign)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP: return topLeft;
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER: return topLeft + size * new Vector2(0f, 0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM: return topLeft + size * new Vector2(0f, 1f);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP: return topLeft + size * new Vector2(0.5f, 0f);
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER: return topLeft + size * new Vector2(0.5f, 0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM: return topLeft + size * new Vector2(0.5f, 1f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP: return topLeft + size * new Vector2(1f, 0f);
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER: return topLeft + size * new Vector2(1f, 0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM: return topLeft + size * new Vector2(1f, 1f);

                default:
                    Debug.Fail("Invalid branch reached.");
                    return topLeft;
            }
        }

        /// <summary>
        /// Reverses effect of alignment to compute top-left corner coordinate.
        /// </summary>
        public static Vector2 GetCoordTopLeftFromAligned(Vector2 alignedCoord, Vector2 size, MyGuiDrawAlignEnum drawAlign)
        {
            switch (drawAlign)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP:
                    return alignedCoord;

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER:
                    return alignedCoord - size * 0.5f;

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP:
                    return alignedCoord - size * new Vector2(0.5f, 0.0f);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM:
                    return alignedCoord - size * new Vector2(0.5f, 1.0f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM:
                    return alignedCoord - size;

                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER:
                    return alignedCoord - size * new Vector2(0.0f, 0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER:
                    return alignedCoord - size * new Vector2(1.0f, 0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM:
                    return alignedCoord - size * new Vector2(0.0f, 1.0f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP:
                    return alignedCoord - size * new Vector2(1.0f, 0.0f);

                default:
                    throw new InvalidBranchException();
            }
        }

        /// <summary>
        /// Reverses effect of alignment to compute top-left corner coordinate.
        /// </summary>
        public static Vector2I GetCoordTopLeftFromAligned(Vector2I alignedCoord, Vector2I size, MyGuiDrawAlignEnum drawAlign)
        {
            switch (drawAlign)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP:
                    return alignedCoord;

                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER:
                    return new Vector2I(
                        alignedCoord.X,
                        alignedCoord.Y - size.Y / 2);

                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM:
                    return new Vector2I(
                        alignedCoord.X,
                        alignedCoord.Y - size.Y);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP:
                    return new Vector2I(
                        alignedCoord.X - size.X / 2,
                        alignedCoord.Y);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER:
                    return new Vector2I(
                        alignedCoord.X - size.X / 2,
                        alignedCoord.Y - size.Y / 2);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM:
                    return new Vector2I(
                        alignedCoord.X - size.X / 2,
                        alignedCoord.Y - size.Y);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP:
                    return new Vector2I(
                        alignedCoord.X - size.X,
                        alignedCoord.Y);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER:
                    return new Vector2I(
                        alignedCoord.X - size.X,
                        alignedCoord.Y - size.Y / 2);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM:
                    return new Vector2I(
                        alignedCoord.X - size.X,
                        alignedCoord.Y - size.Y);

                default:
                    throw new InvalidBranchException();
            }
        }

        /// <summary>
        /// Reverses effect of alignment to compute center coordinate.
        /// </summary>
        public static Vector2 GetCoordCenterFromAligned(Vector2 alignedCoord, Vector2 size, MyGuiDrawAlignEnum drawAlign)
        {
            switch (drawAlign)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP:
                    return alignedCoord + size * 0.5f;

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER:
                    return alignedCoord;

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP:
                    return alignedCoord + size * new Vector2(0.0f, 0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM:
                    return alignedCoord - size * new Vector2(0.0f, 0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM:
                    return alignedCoord - size * 0.5f;

                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER:
                    return alignedCoord + size * new Vector2(0.5f, 0.0f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER:
                    return alignedCoord - size * new Vector2(0.5f, 0.0f);

                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM:
                    return alignedCoord + size * new Vector2(0.5f, -0.5f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP:
                    return alignedCoord + size * new Vector2(-0.5f, 0.5f);

                default:
                    throw new InvalidBranchException();
            }
        }

        /// <summary>
        /// Returns coordinate within given rectangle specified by draw align. Rectangle position should be
        /// upper left corner. Conversion assumes that Y coordinates increase downwards.
        /// </summary>
        public static Vector2 GetCoordAlignedFromRectangle(ref RectangleF rect, MyGuiDrawAlignEnum drawAlign)
        {
            switch (drawAlign)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP: return rect.Position;
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER: return rect.Position + rect.Size * new Vector2(0f, 0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_BOTTOM: return rect.Position + rect.Size * new Vector2(0f, 1f);

                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_TOP: return rect.Position + rect.Size * new Vector2(0.5f, 0f);
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER: return rect.Position + rect.Size * 0.5f;
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM: return rect.Position + rect.Size * new Vector2(0.5f, 1f);

                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP: return rect.Position + rect.Size * new Vector2(1f, 0f);
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_CENTER: return rect.Position + rect.Size * new Vector2(1f, 0.5f);
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_BOTTOM: return rect.Position + rect.Size * 1f;

                default:
                    throw new InvalidBranchException();
            }
        }
        #endregion

#endif
    }

}
