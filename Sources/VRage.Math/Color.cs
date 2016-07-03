using System;
using System.Globalization;
using VRageMath.PackedVector;

namespace VRageMath
{
    /// <summary>
    /// Represents a four-component color using red, green, blue, and alpha data.
    /// </summary>
    [ProtoBuf.ProtoContract, Serializable]
	[Unsharper.UnsharperDisableReflection()]
    public struct Color : IPackedVector<uint>, IPackedVector, IEquatable<Color>
    {
        /// <summary>
        /// Gets the current color as a packed value.
        /// </summary>
        [ProtoBuf.ProtoMember]
        public uint PackedValue;

        /// <summary>
        /// Gets or sets the red component value of this Color.
        /// </summary>
        public byte R
        {
            get
            {
                return (byte)this.PackedValue;
            }
            set
            {
                this.PackedValue = this.PackedValue & 4294967040U | (uint)value;
            }
        }

        /// <summary>
        /// Gets or sets the green component value of this Color.
        /// </summary>
        public byte G
        {
            get
            {
                return (byte)(this.PackedValue >> 8);
            }
            set
            {
                this.PackedValue = (uint)((int)this.PackedValue & -65281 | (int)value << 8);
            }
        }

        /// <summary>
        /// Gets or sets the blue component value of this Color.
        /// </summary>
        public byte B
        {
            get
            {
                return (byte)(this.PackedValue >> 16);
            }
            set
            {
                this.PackedValue = (uint)((int)this.PackedValue & -16711681 | (int)value << 16);
            }
        }

        /// <summary>
        /// Gets or sets the alpha component value.
        /// </summary>
        public byte A
        {
            get
            {
                return (byte)(this.PackedValue >> 24);
            }
            set
            {
                this.PackedValue = (uint)((int)this.PackedValue & 16777215 | (int)value << 24);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:0 G:0 B:0 A:0.
        /// </summary>
        public static Color Transparent
        {
            get
            {
                return new Color(0U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:240 G:248 B:255 A:255.
        /// </summary>
        public static Color AliceBlue
        {
            get
            {
                return new Color(4294965488U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:250 G:235 B:215 A:255.
        /// </summary>
        public static Color AntiqueWhite
        {
            get
            {
                return new Color(4292340730U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:0 G:255 B:255 A:255.
        /// </summary>
        public static Color Aqua
        {
            get
            {
                return new Color(4294967040U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:127 G:255 B:212 A:255.
        /// </summary>
        public static Color Aquamarine
        {
            get
            {
                return new Color(4292149119U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:240 G:255 B:255 A:255.
        /// </summary>
        public static Color Azure
        {
            get
            {
                return new Color(4294967280U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:245 G:245 B:220 A:255.
        /// </summary>
        public static Color Beige
        {
            get
            {
                return new Color(4292670965U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:228 B:196 A:255.
        /// </summary>
        public static Color Bisque
        {
            get
            {
                return new Color(4291093759U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:0 G:0 B:0 A:255.
        /// </summary>
        public static Color Black
        {
            get
            {
                return new Color(4278190080U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:235 B:205 A:255.
        /// </summary>
        public static Color BlanchedAlmond
        {
            get
            {
                return new Color(4291685375U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:0 G:0 B:255 A:255.
        /// </summary>
        public static Color Blue
        {
            get
            {
                return new Color(4294901760U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:138 G:43 B:226 A:255.
        /// </summary>
        public static Color BlueViolet
        {
            get
            {
                return new Color(4293012362U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:165 G:42 B:42 A:255.
        /// </summary>
        public static Color Brown
        {
            get
            {
                return new Color(4280953509U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:222 G:184 B:135 A:255.
        /// </summary>
        public static Color BurlyWood
        {
            get
            {
                return new Color(4287084766U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:95 G:158 B:160 A:255.
        /// </summary>
        public static Color CadetBlue
        {
            get
            {
                return new Color(4288716383U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:127 G:255 B:0 A:255.
        /// </summary>
        public static Color Chartreuse
        {
            get
            {
                return new Color(4278255487U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:210 G:105 B:30 A:255.
        /// </summary>
        public static Color Chocolate
        {
            get
            {
                return new Color(4280183250U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:127 B:80 A:255.
        /// </summary>
        public static Color Coral
        {
            get
            {
                return new Color(4283465727U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:100 G:149 B:237 A:255.
        /// </summary>
        public static Color CornflowerBlue
        {
            get
            {
                return new Color(4293760356U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:248 B:220 A:255.
        /// </summary>
        public static Color Cornsilk
        {
            get
            {
                return new Color(4292671743U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:220 G:20 B:60 A:255.
        /// </summary>
        public static Color Crimson
        {
            get
            {
                return new Color(4282127580U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:0 G:255 B:255 A:255.
        /// </summary>
        public static Color Cyan
        {
            get
            {
                return new Color(4294967040U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:0 G:0 B:139 A:255.
        /// </summary>
        public static Color DarkBlue
        {
            get
            {
                return new Color(4287299584U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:0 G:139 B:139 A:255.
        /// </summary>
        public static Color DarkCyan
        {
            get
            {
                return new Color(4287335168U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:184 G:134 B:11 A:255.
        /// </summary>
        public static Color DarkGoldenrod
        {
            get
            {
                return new Color(4278945464U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:169 G:169 B:169 A:255.
        /// </summary>
        public static Color DarkGray
        {
            get
            {
                return new Color(4289309097U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:0 G:100 B:0 A:255.
        /// </summary>
        public static Color DarkGreen
        {
            get
            {
                return new Color(4278215680U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:189 G:183 B:107 A:255.
        /// </summary>
        public static Color DarkKhaki
        {
            get
            {
                return new Color(4285249469U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:139 G:0 B:139 A:255.
        /// </summary>
        public static Color DarkMagenta
        {
            get
            {
                return new Color(4287299723U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:85 G:107 B:47 A:255.
        /// </summary>
        public static Color DarkOliveGreen
        {
            get
            {
                return new Color(4281297749U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:140 B:0 A:255.
        /// </summary>
        public static Color DarkOrange
        {
            get
            {
                return new Color(4278226175U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:153 G:50 B:204 A:255.
        /// </summary>
        public static Color DarkOrchid
        {
            get
            {
                return new Color(4291572377U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:139 G:0 B:0 A:255.
        /// </summary>
        public static Color DarkRed
        {
            get
            {
                return new Color(4278190219U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:233 G:150 B:122 A:255.
        /// </summary>
        public static Color DarkSalmon
        {
            get
            {
                return new Color(4286224105U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:143 G:188 B:139 A:255.
        /// </summary>
        public static Color DarkSeaGreen
        {
            get
            {
                return new Color(4287347855U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:72 G:61 B:139 A:255.
        /// </summary>
        public static Color DarkSlateBlue
        {
            get
            {
                return new Color(4287315272U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:47 G:79 B:79 A:255.
        /// </summary>
        public static Color DarkSlateGray
        {
            get
            {
                return new Color(4283387695U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:0 G:206 B:209 A:255.
        /// </summary>
        public static Color DarkTurquoise
        {
            get
            {
                return new Color(4291939840U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:148 G:0 B:211 A:255.
        /// </summary>
        public static Color DarkViolet
        {
            get
            {
                return new Color(4292018324U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:20 B:147 A:255.
        /// </summary>
        public static Color DeepPink
        {
            get
            {
                return new Color(4287829247U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:0 G:191 B:255 A:255.
        /// </summary>
        public static Color DeepSkyBlue
        {
            get
            {
                return new Color(4294950656U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:105 G:105 B:105 A:255.
        /// </summary>
        public static Color DimGray
        {
            get
            {
                return new Color(4285098345U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:30 G:144 B:255 A:255.
        /// </summary>
        public static Color DodgerBlue
        {
            get
            {
                return new Color(4294938654U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:178 G:34 B:34 A:255.
        /// </summary>
        public static Color Firebrick
        {
            get
            {
                return new Color(4280427186U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:250 B:240 A:255.
        /// </summary>
        public static Color FloralWhite
        {
            get
            {
                return new Color(4293982975U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:34 G:139 B:34 A:255.
        /// </summary>
        public static Color ForestGreen
        {
            get
            {
                return new Color(4280453922U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:0 B:255 A:255.
        /// </summary>
        public static Color Fuchsia
        {
            get
            {
                return new Color(4294902015U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:220 G:220 B:220 A:255.
        /// </summary>
        public static Color Gainsboro
        {
            get
            {
                return new Color(4292664540U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:248 G:248 B:255 A:255.
        /// </summary>
        public static Color GhostWhite
        {
            get
            {
                return new Color(4294965496U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:215 B:0 A:255.
        /// </summary>
        public static Color Gold
        {
            get
            {
                return new Color(4278245375U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:218 G:165 B:32 A:255.
        /// </summary>
        public static Color Goldenrod
        {
            get
            {
                return new Color(4280329690U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:128 G:128 B:128 A:255.
        /// </summary>
        public static Color Gray
        {
            get
            {
                return new Color(4286611584U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:0 G:128 B:0 A:255.
        /// </summary>
        public static Color Green
        {
            get
            {
                return new Color(4278222848U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:173 G:255 B:47 A:255.
        /// </summary>
        public static Color GreenYellow
        {
            get
            {
                return new Color(4281335725U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:240 G:255 B:240 A:255.
        /// </summary>
        public static Color Honeydew
        {
            get
            {
                return new Color(4293984240U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:105 B:180 A:255.
        /// </summary>
        public static Color HotPink
        {
            get
            {
                return new Color(4290013695U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:205 G:92 B:92 A:255.
        /// </summary>
        public static Color IndianRed
        {
            get
            {
                return new Color(4284243149U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:75 G:0 B:130 A:255.
        /// </summary>
        public static Color Indigo
        {
            get
            {
                return new Color(4286709835U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:255 B:240 A:255.
        /// </summary>
        public static Color Ivory
        {
            get
            {
                return new Color(4293984255U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:240 G:230 B:140 A:255.
        /// </summary>
        public static Color Khaki
        {
            get
            {
                return new Color(4287424240U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:230 G:230 B:250 A:255.
        /// </summary>
        public static Color Lavender
        {
            get
            {
                return new Color(4294633190U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:240 B:245 A:255.
        /// </summary>
        public static Color LavenderBlush
        {
            get
            {
                return new Color(4294308095U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:124 G:252 B:0 A:255.
        /// </summary>
        public static Color LawnGreen
        {
            get
            {
                return new Color(4278254716U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:250 B:205 A:255.
        /// </summary>
        public static Color LemonChiffon
        {
            get
            {
                return new Color(4291689215U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:173 G:216 B:230 A:255.
        /// </summary>
        public static Color LightBlue
        {
            get
            {
                return new Color(4293318829U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:240 G:128 B:128 A:255.
        /// </summary>
        public static Color LightCoral
        {
            get
            {
                return new Color(4286611696U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:224 G:255 B:255 A:255.
        /// </summary>
        public static Color LightCyan
        {
            get
            {
                return new Color(4294967264U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:250 G:250 B:210 A:255.
        /// </summary>
        public static Color LightGoldenrodYellow
        {
            get
            {
                return new Color(4292016890U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:144 G:238 B:144 A:255.
        /// </summary>
        public static Color LightGreen
        {
            get
            {
                return new Color(4287688336U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:211 G:211 B:211 A:255.
        /// </summary>
        public static Color LightGray
        {
            get
            {
                return new Color(4292072403U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:182 B:193 A:255.
        /// </summary>
        public static Color LightPink
        {
            get
            {
                return new Color(4290885375U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:160 B:122 A:255.
        /// </summary>
        public static Color LightSalmon
        {
            get
            {
                return new Color(4286226687U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:32 G:178 B:170 A:255.
        /// </summary>
        public static Color LightSeaGreen
        {
            get
            {
                return new Color(4289376800U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:135 G:206 B:250 A:255.
        /// </summary>
        public static Color LightSkyBlue
        {
            get
            {
                return new Color(4294626951U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:119 G:136 B:153 A:255.
        /// </summary>
        public static Color LightSlateGray
        {
            get
            {
                return new Color(4288252023U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:176 G:196 B:222 A:255.
        /// </summary>
        public static Color LightSteelBlue
        {
            get
            {
                return new Color(4292789424U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:255 B:224 A:255.
        /// </summary>
        public static Color LightYellow
        {
            get
            {
                return new Color(4292935679U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:0 G:255 B:0 A:255.
        /// </summary>
        public static Color Lime
        {
            get
            {
                return new Color(4278255360U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:50 G:205 B:50 A:255.
        /// </summary>
        public static Color LimeGreen
        {
            get
            {
                return new Color(4281519410U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:250 G:240 B:230 A:255.
        /// </summary>
        public static Color Linen
        {
            get
            {
                return new Color(4293325050U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:0 B:255 A:255.
        /// </summary>
        public static Color Magenta
        {
            get
            {
                return new Color(4294902015U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:128 G:0 B:0 A:255.
        /// </summary>
        public static Color Maroon
        {
            get
            {
                return new Color(4278190208U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:102 G:205 B:170 A:255.
        /// </summary>
        public static Color MediumAquamarine
        {
            get
            {
                return new Color(4289383782U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:0 G:0 B:205 A:255.
        /// </summary>
        public static Color MediumBlue
        {
            get
            {
                return new Color(4291624960U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:186 G:85 B:211 A:255.
        /// </summary>
        public static Color MediumOrchid
        {
            get
            {
                return new Color(4292040122U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:147 G:112 B:219 A:255.
        /// </summary>
        public static Color MediumPurple
        {
            get
            {
                return new Color(4292571283U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:60 G:179 B:113 A:255.
        /// </summary>
        public static Color MediumSeaGreen
        {
            get
            {
                return new Color(4285641532U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:123 G:104 B:238 A:255.
        /// </summary>
        public static Color MediumSlateBlue
        {
            get
            {
                return new Color(4293814395U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:0 G:250 B:154 A:255.
        /// </summary>
        public static Color MediumSpringGreen
        {
            get
            {
                return new Color(4288346624U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:72 G:209 B:204 A:255.
        /// </summary>
        public static Color MediumTurquoise
        {
            get
            {
                return new Color(4291613000U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:199 G:21 B:133 A:255.
        /// </summary>
        public static Color MediumVioletRed
        {
            get
            {
                return new Color(4286911943U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:25 G:25 B:112 A:255.
        /// </summary>
        public static Color MidnightBlue
        {
            get
            {
                return new Color(4285536537U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:245 G:255 B:250 A:255.
        /// </summary>
        public static Color MintCream
        {
            get
            {
                return new Color(4294639605U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:228 B:225 A:255.
        /// </summary>
        public static Color MistyRose
        {
            get
            {
                return new Color(4292994303U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:228 B:181 A:255.
        /// </summary>
        public static Color Moccasin
        {
            get
            {
                return new Color(4290110719U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:222 B:173 A:255.
        /// </summary>
        public static Color NavajoWhite
        {
            get
            {
                return new Color(4289584895U);
            }
        }

        /// <summary>
        /// Gets a system-defined color R:0 G:0 B:128 A:255.
        /// </summary>
        public static Color Navy
        {
            get
            {
                return new Color(4286578688U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:253 G:245 B:230 A:255.
        /// </summary>
        public static Color OldLace
        {
            get
            {
                return new Color(4293326333U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:128 G:128 B:0 A:255.
        /// </summary>
        public static Color Olive
        {
            get
            {
                return new Color(4278222976U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:107 G:142 B:35 A:255.
        /// </summary>
        public static Color OliveDrab
        {
            get
            {
                return new Color(4280520299U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:165 B:0 A:255.
        /// </summary>
        public static Color Orange
        {
            get
            {
                return new Color(4278232575U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:69 B:0 A:255.
        /// </summary>
        public static Color OrangeRed
        {
            get
            {
                return new Color(4278207999U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:218 G:112 B:214 A:255.
        /// </summary>
        public static Color Orchid
        {
            get
            {
                return new Color(4292243674U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:238 G:232 B:170 A:255.
        /// </summary>
        public static Color PaleGoldenrod
        {
            get
            {
                return new Color(4289390830U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:152 G:251 B:152 A:255.
        /// </summary>
        public static Color PaleGreen
        {
            get
            {
                return new Color(4288215960U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:175 G:238 B:238 A:255.
        /// </summary>
        public static Color PaleTurquoise
        {
            get
            {
                return new Color(4293848751U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:219 G:112 B:147 A:255.
        /// </summary>
        public static Color PaleVioletRed
        {
            get
            {
                return new Color(4287852763U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:239 B:213 A:255.
        /// </summary>
        public static Color PapayaWhip
        {
            get
            {
                return new Color(4292210687U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:218 B:185 A:255.
        /// </summary>
        public static Color PeachPuff
        {
            get
            {
                return new Color(4290370303U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:205 G:133 B:63 A:255.
        /// </summary>
        public static Color Peru
        {
            get
            {
                return new Color(4282353101U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:192 B:203 A:255.
        /// </summary>
        public static Color Pink
        {
            get
            {
                return new Color(4291543295U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:221 G:160 B:221 A:255.
        /// </summary>
        public static Color Plum
        {
            get
            {
                return new Color(4292714717U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:176 G:224 B:230 A:255.
        /// </summary>
        public static Color PowderBlue
        {
            get
            {
                return new Color(4293320880U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:128 G:0 B:128 A:255.
        /// </summary>
        public static Color Purple
        {
            get
            {
                return new Color(4286578816U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:0 B:0 A:255.
        /// </summary>
        public static Color Red
        {
            get
            {
                return new Color(4278190335U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:188 G:143 B:143 A:255.
        /// </summary>
        public static Color RosyBrown
        {
            get
            {
                return new Color(4287598524U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:65 G:105 B:225 A:255.
        /// </summary>
        public static Color RoyalBlue
        {
            get
            {
                return new Color(4292962625U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:139 G:69 B:19 A:255.
        /// </summary>
        public static Color SaddleBrown
        {
            get
            {
                return new Color(4279453067U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:250 G:128 B:114 A:255.
        /// </summary>
        public static Color Salmon
        {
            get
            {
                return new Color(4285694202U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:244 G:164 B:96 A:255.
        /// </summary>
        public static Color SandyBrown
        {
            get
            {
                return new Color(4284523764U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:46 G:139 B:87 A:255.
        /// </summary>
        public static Color SeaGreen
        {
            get
            {
                return new Color(4283927342U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:245 B:238 A:255.
        /// </summary>
        public static Color SeaShell
        {
            get
            {
                return new Color(4293850623U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:160 G:82 B:45 A:255.
        /// </summary>
        public static Color Sienna
        {
            get
            {
                return new Color(4281160352U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:192 G:192 B:192 A:255.
        /// </summary>
        public static Color Silver
        {
            get
            {
                return new Color(4290822336U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:135 G:206 B:235 A:255.
        /// </summary>
        public static Color SkyBlue
        {
            get
            {
                return new Color(4293643911U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:106 G:90 B:205 A:255.
        /// </summary>
        public static Color SlateBlue
        {
            get
            {
                return new Color(4291648106U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:112 G:128 B:144 A:255.
        /// </summary>
        public static Color SlateGray
        {
            get
            {
                return new Color(4287660144U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:250 B:250 A:255.
        /// </summary>
        public static Color Snow
        {
            get
            {
                return new Color(4294638335U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:0 G:255 B:127 A:255.
        /// </summary>
        public static Color SpringGreen
        {
            get
            {
                return new Color(4286578432U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:70 G:130 B:180 A:255.
        /// </summary>
        public static Color SteelBlue
        {
            get
            {
                return new Color(4290019910U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:210 G:180 B:140 A:255.
        /// </summary>
        public static Color Tan
        {
            get
            {
                return new Color(4287411410U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:0 G:128 B:128 A:255.
        /// </summary>
        public static Color Teal
        {
            get
            {
                return new Color(4286611456U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:216 G:191 B:216 A:255.
        /// </summary>
        public static Color Thistle
        {
            get
            {
                return new Color(4292394968U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:99 B:71 A:255.
        /// </summary>
        public static Color Tomato
        {
            get
            {
                return new Color(4282868735U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:64 G:224 B:208 A:255.
        /// </summary>
        public static Color Turquoise
        {
            get
            {
                return new Color(4291878976U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:238 G:130 B:238 A:255.
        /// </summary>
        public static Color Violet
        {
            get
            {
                return new Color(4293821166U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:245 G:222 B:179 A:255.
        /// </summary>
        public static Color Wheat
        {
            get
            {
                return new Color(4289978101U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:255 B:255 A:255.
        /// </summary>
        public static Color White
        {
            get
            {
                return new Color(uint.MaxValue);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:245 G:245 B:245 A:255.
        /// </summary>
        public static Color WhiteSmoke
        {
            get
            {
                return new Color(4294309365U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:255 G:255 B:0 A:255.
        /// </summary>
        public static Color Yellow
        {
            get
            {
                return new Color(4278255615U);
            }
        }

        /// <summary>
        /// Gets a system-defined color with the value R:154 G:205 B:50 A:255.
        /// </summary>
        public static Color YellowGreen
        {
            get
            {
                return new Color(4281519514U);
            }
        }

        public Color(uint packedValue)
        {
            this.PackedValue = packedValue;
        }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        /// <param name="r">Red component.</param><param name="g">Green component.</param><param name="b">Blue component.</param>
        public Color(int r, int g, int b)
        {
            if (((r | g | b) & -256) != 0)
            {
                r = Color.ClampToByte64((long)r);
                g = Color.ClampToByte64((long)g);
                b = Color.ClampToByte64((long)b);
            }
            g <<= 8;
            b <<= 16;
            this.PackedValue = (uint)(r | g | b | -16777216);
        }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        /// <param name="r">Red component.</param><param name="g">Green component.</param><param name="b">Blue component.</param><param name="a">Alpha component.</param>
        public Color(int r, int g, int b, int a)
        {
            if (((r | g | b | a) & -256) != 0)
            {
                r = Color.ClampToByte32(r);
                g = Color.ClampToByte32(g);
                b = Color.ClampToByte32(b);
                a = Color.ClampToByte32(a);
            }
            g <<= 8;
            b <<= 16;
            a <<= 24;
            this.PackedValue = (uint)(r | g | b | a);
        }

        public Color(float rgb)
        {
            this.PackedValue = Color.PackHelper(rgb, rgb, rgb, 1f);
        }


        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        /// <param name="r">Red component.</param><param name="g">Green component.</param><param name="b">Blue component.</param>
        public Color(float r, float g, float b)
        {
            this.PackedValue = Color.PackHelper(r, g, b, 1f);
        }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        /// <param name="r">Red component.</param><param name="g">Green component.</param><param name="b">Blue component.</param><param name="a">Alpha component.</param>
        public Color(float r, float g, float b, float a)
        {
            this.PackedValue = Color.PackHelper(r, g, b, a);
        }

        public Color(Color color, float a)
        {
            this.PackedValue = Color.PackHelper(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, a);
        }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        /// <param name="vector">A three-component color.</param>
        public Color(Vector3 vector)
        {
            this.PackedValue = Color.PackHelper(vector.X, vector.Y, vector.Z, 1f);
        }

        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        /// <param name="vector">A four-component color.</param>
        public Color(Vector4 vector)
        {
            this.PackedValue = Color.PackHelper(vector.X, vector.Y, vector.Z, vector.W);
        }

        /// <summary>
        /// Multiply operator.
        /// </summary>
        /// <param name="value">A four-component color</param><param name="scale">Scale factor.</param>
        public static Color operator *(Color value, float scale)
        {
            uint num1 = value.PackedValue;
            uint num2 = (uint)(byte)num1;
            uint num3 = (uint)(byte)(num1 >> 8);
            uint num4 = (uint)(byte)(num1 >> 16);
            uint num5 = (uint)(byte)(num1 >> 24);
            scale *= 65536f;
            uint num6 = (double)scale >= 0.0 ? ((double)scale <= 16777215.0 ? (uint)scale : 16777215U) : 0U;
            uint num7 = num2 * num6 >> 16;
            uint num8 = num3 * num6 >> 16;
            uint num9 = num4 * num6 >> 16;
            uint num10 = num5 * num6 >> 16;
            if (num7 > (uint)byte.MaxValue)
                num7 = (uint)byte.MaxValue;
            if (num8 > (uint)byte.MaxValue)
                num8 = (uint)byte.MaxValue;
            if (num9 > (uint)byte.MaxValue)
                num9 = (uint)byte.MaxValue;
            if (num10 > (uint)byte.MaxValue)
                num10 = (uint)byte.MaxValue;
            Color color;
            color.PackedValue = (uint)((int)num7 | (int)num8 << 8 | (int)num9 << 16 | (int)num10 << 24);
            return color;
        }

        public static Color operator +(Color value, Color other)
        {
            return new Color(value.R + other.R, value.G + other.G, value.B + other.B, value.A + other.A);
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="a">A four-component color.</param><param name="b">A four-component color.</param>
        public static bool operator ==(Color a, Color b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Equality operator for Testing two color objects to see if they are equal.
        /// </summary>
        /// <param name="a">A four-component color.</param><param name="b">A four-component color.</param>
        public static bool operator !=(Color a, Color b)
        {
            return !a.Equals(b);
        }

        void IPackedVector.PackFromVector4(Vector4 vector)
        {
            this.PackedValue = Color.PackHelper(vector.X, vector.Y, vector.Z, vector.W);
        }

        /// <summary>
        /// Convert a non premultipled color into color data that contains alpha.
        /// </summary>
        /// <param name="vector">A four-component color.</param>
        public static Color FromNonPremultiplied(Vector4 vector)
        {
            Color color;
            color.PackedValue = Color.PackHelper(vector.X * vector.W, vector.Y * vector.W, vector.Z * vector.W, vector.W);
            return color;
        }

        /// <summary>
        /// Converts a non-premultipled alpha color to a color that contains premultiplied alpha.
        /// </summary>
        /// <param name="r">Red component.</param><param name="g">Green component.</param><param name="b">Blue component.</param><param name="a">Alpha component.</param>
        public static Color FromNonPremultiplied(int r, int g, int b, int a)
        {
            r = Color.ClampToByte64((long)r * (long)a / (long)byte.MaxValue);
            g = Color.ClampToByte64((long)g * (long)a / (long)byte.MaxValue);
            b = Color.ClampToByte64((long)b * (long)a / (long)byte.MaxValue);
            a = Color.ClampToByte32(a);
            g <<= 8;
            b <<= 16;
            a <<= 24;
            Color color;
            color.PackedValue = (uint)(r | g | b | a);
            return color;
        }

        private static uint PackHelper(float vectorX, float vectorY, float vectorZ, float vectorW)
        {
            return PackUtils.PackUNorm((float)byte.MaxValue, vectorX) | PackUtils.PackUNorm((float)byte.MaxValue, vectorY) << 8 | PackUtils.PackUNorm((float)byte.MaxValue, vectorZ) << 16 | PackUtils.PackUNorm((float)byte.MaxValue, vectorW) << 24;
        }

        private static int ClampToByte32(int value)
        {
            if (value < 0)
                return 0;
            if (value > (int)byte.MaxValue)
                return (int)byte.MaxValue;
            else
                return value;
        }

        private static int ClampToByte64(long value)
        {
            if (value < 0L)
                return 0;
            if (value > (long)byte.MaxValue)
                return (int)byte.MaxValue;
            else
                return (int)value;
        }

        /// <summary>
        /// Gets a three-component vector representation for this object.
        /// </summary>
        public Vector3 ToVector3()
        {
            Vector3 vector3;
            vector3.X = PackUtils.UnpackUNorm((uint)byte.MaxValue, this.PackedValue);
            vector3.Y = PackUtils.UnpackUNorm((uint)byte.MaxValue, this.PackedValue >> 8);
            vector3.Z = PackUtils.UnpackUNorm((uint)byte.MaxValue, this.PackedValue >> 16);
            return vector3;
        }

        /// <summary>
        /// Gets a four-component vector representation for this object.
        /// </summary>
        public Vector4 ToVector4()
        {
            Vector4 vector4;
            vector4.X = PackUtils.UnpackUNorm((uint)byte.MaxValue, this.PackedValue);
            vector4.Y = PackUtils.UnpackUNorm((uint)byte.MaxValue, this.PackedValue >> 8);
            vector4.Z = PackUtils.UnpackUNorm((uint)byte.MaxValue, this.PackedValue >> 16);
            vector4.W = PackUtils.UnpackUNorm((uint)byte.MaxValue, this.PackedValue >> 24);
            return vector4;
        }

        /// <summary>
        /// Linearly interpolate a color.
        /// </summary>
        /// <param name="value1">A four-component color.</param><param name="value2">A four-component color.</param><param name="amount">Interpolation factor.</param>
        public static Color Lerp(Color value1, Color value2, float amount)
        {
            uint num1 = value1.PackedValue;
            uint num2 = value2.PackedValue;
            int num3 = (int)(byte)num1;
            int num4 = (int)(byte)(num1 >> 8);
            int num5 = (int)(byte)(num1 >> 16);
            int num6 = (int)(byte)(num1 >> 24);
            int num7 = (int)(byte)num2;
            int num8 = (int)(byte)(num2 >> 8);
            int num9 = (int)(byte)(num2 >> 16);
            int num10 = (int)(byte)(num2 >> 24);
            int num11 = (int)PackUtils.PackUNorm(65536f, amount);
            int num12 = num3 + ((num7 - num3) * num11 >> 16);
            int num13 = num4 + ((num8 - num4) * num11 >> 16);
            int num14 = num5 + ((num9 - num5) * num11 >> 16);
            int num15 = num6 + ((num10 - num6) * num11 >> 16);
            Color color;
            color.PackedValue = (uint)(num12 | num13 << 8 | num14 << 16 | num15 << 24);
            return color;
        }

        /// <summary>
        /// Multiply each color component by the scale factor.
        /// </summary>
        /// <param name="value">The source, four-component color.</param><param name="scale">The scale factor.</param>
        public static Color Multiply(Color value, float scale)
        {
            uint num1 = value.PackedValue;
            uint num2 = (uint)(byte)num1;
            uint num3 = (uint)(byte)(num1 >> 8);
            uint num4 = (uint)(byte)(num1 >> 16);
            uint num5 = (uint)(byte)(num1 >> 24);
            scale *= 65536f;
            uint num6 = (double)scale >= 0.0 ? ((double)scale <= 16777215.0 ? (uint)scale : 16777215U) : 0U;
            uint num7 = num2 * num6 >> 16;
            uint num8 = num3 * num6 >> 16;
            uint num9 = num4 * num6 >> 16;
            uint num10 = num5 * num6 >> 16;
            if (num7 > (uint)byte.MaxValue)
                num7 = (uint)byte.MaxValue;
            if (num8 > (uint)byte.MaxValue)
                num8 = (uint)byte.MaxValue;
            if (num9 > (uint)byte.MaxValue)
                num9 = (uint)byte.MaxValue;
            if (num10 > (uint)byte.MaxValue)
                num10 = (uint)byte.MaxValue;
            Color color;
            color.PackedValue = (uint)((int)num7 | (int)num8 << 8 | (int)num9 << 16 | (int)num10 << 24);
            return color;
        }

        /// <summary>
        /// Gets a string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return string.Format((IFormatProvider)CultureInfo.CurrentCulture, "{{R:{0} G:{1} B:{2} A:{3}}}", (object)this.R, (object)this.G, (object)this.B, (object)this.A);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        public override int GetHashCode()
        {
            return this.PackedValue.GetHashCode();
        }

        /// <summary>
        /// Test an instance of a color object to see if it is equal to this object.
        /// </summary>
        /// <param name="obj">A color object.</param>
        public override bool Equals(object obj)
        {
            if (obj is Color)
                return this.Equals((Color)obj);
            else
                return false;
        }

        /// <summary>
        /// Test a color to see if it is equal to the color in this instance.
        /// </summary>
        /// <param name="other">A four-component color.</param>
        public bool Equals(Color other)
        {
            return this.PackedValue.Equals(other.PackedValue);
        }

        public static implicit operator Color(Vector3 v)
        {
            return new Color(v.X, v.Y, v.Z, 1.0f);
        }

        public static implicit operator Vector3(Color v)
        {
            return v.ToVector3();
        }

        public static implicit operator Color(Vector4 v)
        {
            return new Color(v.X, v.Y, v.Z, v.W);
        }

        public static implicit operator Vector4(Color v)
        {
            return v.ToVector4();
        }

        uint IPackedVector<uint>.PackedValue
        {
            get { return PackedValue; }
            set { PackedValue = value; }
        }

        public static Color Lighten(Color inColor, double inAmount)
        {
            return new Color(
              (int)Math.Min(255, inColor.R + 255 * inAmount),
              (int)Math.Min(255, inColor.G + 255 * inAmount),
              (int)Math.Min(255, inColor.B + 255 * inAmount),
              inColor.A);
        }

        public static Color Darken(Color inColor, double inAmount)
        {
            return new Color(
              (int)Math.Max(0, inColor.R - 255 * inAmount),
              (int)Math.Max(0, inColor.G - 255 * inAmount),
              (int)Math.Max(0, inColor.B - 255 * inAmount),
              inColor.A);
        }

    }
}
