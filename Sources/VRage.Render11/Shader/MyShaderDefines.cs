using System.Text;
using SharpDX.Direct3D;

namespace VRageRender
{
    internal static class MyShadersDefines
    {
        internal static readonly ShaderMacro DebugMacro = new ShaderMacro("DEBUG", null);
        internal const string ShadersContentPath = "Shaders";
        internal const string CachePath = "ShaderCache";
        internal const string MaterialComboFile = "MaterialCombos.xml";

        internal enum Profiles
        {
            vs_5_0,
            ps_5_0,
            gs_5_0,
            cs_5_0,

            count
        }
        internal static string ProfileToString(Profiles val)
        {
            switch (val)
            {
                case Profiles.vs_5_0:
                    return "vs_5_0";

                case Profiles.ps_5_0:
                    return "ps_5_0";

                case Profiles.gs_5_0:
                    return "gs_5_0";

                case Profiles.cs_5_0:
                    return "cs_5_0";
            }

            return "";
        }
        internal static string ProfileEntryPoint(Profiles val)
        {
            switch (val)
            {
                case Profiles.vs_5_0:
                    return "__vertex_shader";

                case Profiles.ps_5_0:
                    return "__pixel_shader";

                case Profiles.gs_5_0:
                    return "__geometry_shader";

                case Profiles.cs_5_0:
                    return "__compute_shader";
            }

            return "";
        }

        internal static string GetString(this ShaderMacro[] macros)
        {
            if (macros == null)
                return string.Empty;

            var sb = new StringBuilder();
            for (int i = 0; i < macros.Length; i++)
            {
                if (i > 0)
                    sb.AppendFormat(";");
                if (macros[i].Definition != null)
                    sb.AppendFormat("{0}={1}", macros[i].Name, macros[i].Definition);
                else sb.Append(macros[i].Name);
            }
            return sb.ToString();
        }

        internal static string GetString(this MyVertexInputComponent[] components)
        {
            if (components == null)
                return string.Empty;

            var sb = new StringBuilder();
            for (int i = 0; i < components.Length; i++)
            {
                if (i > 0)
                    sb.AppendFormat(";");
                sb.Append(components[i].Type);
            }
            return sb.ToString();
        }
    }
}
