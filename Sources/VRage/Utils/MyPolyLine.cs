using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
#if !XB1
using System.Text.RegularExpressions;
#endif // !XB1
using VRageMath;
using VRage;
using System.Diagnostics;
using System.Collections;
using System.ComponentModel;
using System.Xml;
using System.Runtime.InteropServices;

namespace VRage.Utils
{
    public struct MyPolyLine
    {
        public Vector3 LineDirectionNormalized;     //  Vector from point 0 to 1, calculated as point1 - point0, than normalized
        public Vector3 Point0;
        public Vector3 Point1;
        public float Thickness;
    }

    public struct MyPolyLineD
    {
        public Vector3 LineDirectionNormalized;     //  Vector from point 0 to 1, calculated as point1 - point0, than normalized
        public Vector3D Point0;
        public Vector3D Point1;
        public float Thickness;
    }
}

