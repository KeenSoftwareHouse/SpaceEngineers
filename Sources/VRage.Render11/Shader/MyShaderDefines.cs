using System.Text;
using SharpDX.Direct3D;
using System.Collections.Generic;
using System;

namespace VRageRender
{
    internal static class MyShadersDefines
    {
        internal const string ShadersFolderName = "Shaders";
        internal const string CachePath = "ShaderCache";

        internal static string GetString(this IEnumerable<ShaderMacro> macros)
        {
            if (macros == null)
                return string.Empty;

            int i = 0;
            var sb = new StringBuilder();
            foreach (ShaderMacro macro in macros)
            {
                if (i > 0)
                    sb.AppendFormat(";");
                if (macro.Definition != null)
                    sb.AppendFormat("{0}={1}", macro.Name, macro.Definition);
                else sb.Append(macro.Name);
                i++;
            }
            return sb.ToString();
        }

        internal static string GetString(this IEnumerable<MyVertexInputComponent> components)
        {
            if (components == null)
                return string.Empty;

            int i = 0;
            var sb = new StringBuilder();
            foreach (MyVertexInputComponent component in components)
            {
                if (i > 0)
                    sb.AppendFormat(";");
                sb.Append(component.Type);
                i++;
            }
            return sb.ToString();
        }
    }
}
