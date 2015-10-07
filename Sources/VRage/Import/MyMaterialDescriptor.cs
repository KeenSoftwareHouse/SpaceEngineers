using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRage.Import
{
    /// <summary>
    /// material params for export
    /// </summary>
    public class MyMaterialDescriptor
    {
        public string MaterialName { get; private set; }

        public Vector3 DiffuseColor = Vector3.One;
        public float DiffuseColorX { get { return DiffuseColor.X; } set { DiffuseColor.X = value; } }
        public float DiffuseColorY { get { return DiffuseColor.Y; } set { DiffuseColor.Y = value; } }
        public float DiffuseColorZ { get { return DiffuseColor.Z; } set { DiffuseColor.Z = value; } }

        public float SpecularPower { get; set; }
        /// <summary>
        /// Extra data (animation of holos)
        /// </summary>
        public Vector3 ExtraData = Vector3.Zero;

        public float SpecularIntensity
        {
            get { return ExtraData.X; }
            set { ExtraData.X = value; }
        }

        public Dictionary<string, string> Textures = new Dictionary<string, string>();
        public Dictionary<string, string> UserData = new Dictionary<string, string>();

        public string Technique { get; set; }

        public string GlassCW { get; set; }
        public string GlassCCW { get; set; }
        public bool GlassSmoothNormals { get; set; }

        /// <summary>
        /// c-tor
        /// </summary>
        /// <param name="materialName"></param>
        public MyMaterialDescriptor(string materialName)
        {
            MaterialName = materialName;
            Technique = "MESH";
            GlassCCW = String.Empty;
            GlassCW = String.Empty;
            GlassSmoothNormals = true;
        }

        public MyMaterialDescriptor() {;}

        /// <summary>
        /// Write to binary file
        /// </summary>
        /// <param name="writer"></param>
        /// <returns></returns>
        public bool Write(BinaryWriter writer)
        {
            writer.Write((MaterialName != null) ? MaterialName : "");
            writer.Write(Textures.Count);
            foreach (var texture in Textures)
            {
                writer.Write(texture.Key);
                writer.Write(texture.Value == null ? "" : texture.Value);
            }

            writer.Write(UserData.Count);
            foreach (var userData in UserData)
            {
                writer.Write(userData.Key);
                writer.Write(userData.Value == null ? "" : userData.Value);
            }

            writer.Write(SpecularPower);
            writer.Write(DiffuseColor.X);
            writer.Write(DiffuseColor.Y);
            writer.Write(DiffuseColor.Z);
            writer.Write(ExtraData.X);
            writer.Write(ExtraData.Y);
            writer.Write(ExtraData.Z);

            writer.Write(Technique);

            if (Technique == "GLASS")
            {
                writer.Write(GlassCW);
                writer.Write(GlassCCW);
                writer.Write(GlassSmoothNormals);
            }

            return true;
        }

        public bool Read(BinaryReader reader, int version)
        {
            Textures.Clear();
            UserData.Clear();

            MaterialName = reader.ReadString();
            if (String.IsNullOrEmpty(MaterialName))
                MaterialName = null;

            if (version < 01052002)
            {
                var diffuseTextureName = reader.ReadString();
                if (!string.IsNullOrEmpty(diffuseTextureName))
                {
                    Textures.Add("DiffuseTexture", diffuseTextureName);
                }
                    
                var normalsTextureName = reader.ReadString();
                if (!string.IsNullOrEmpty(normalsTextureName))
                {
                    Textures.Add("NormalTexture", normalsTextureName);
                }
            }
            else
            {
                int texturesCount = reader.ReadInt32();
                for (int i = 0; i < texturesCount; i++)
                {
                    var textureName = reader.ReadString();
                    var texturePath = reader.ReadString();
                    Textures.Add(textureName, texturePath);
                }
            }

            if (version >= 01068001)
            {
                int userDataCount = reader.ReadInt32();
                for (int i = 0; i < userDataCount; i++)
                {
                    var name = reader.ReadString();
                    var data = reader.ReadString();
                    UserData.Add(name, data);
                }
            }

            SpecularPower = reader.ReadSingle();
            DiffuseColor.X = reader.ReadSingle();
            DiffuseColor.Y = reader.ReadSingle();
            DiffuseColor.Z = reader.ReadSingle();
            ExtraData.X = reader.ReadSingle();
            ExtraData.Y = reader.ReadSingle();
            ExtraData.Z = reader.ReadSingle();

            if (version < 01052001)
            {
                Technique = ((MyMeshDrawTechnique)reader.ReadInt32()).ToString();
            }
            else
                Technique = reader.ReadString();

            if (Technique == "GLASS")
            {
                if (version >= 01043001)
                {
                    GlassCW = reader.ReadString();
                    GlassCCW = reader.ReadString();
                    GlassSmoothNormals = reader.ReadBoolean();
                }
                else
                {
                    reader.ReadSingle();
                    reader.ReadSingle();
                    reader.ReadSingle();
                    reader.ReadSingle();
                    GlassCW = "GlassCW";
                    GlassCCW = "GlassCCW";
                    GlassSmoothNormals = false;
                }
            }

            return true;
        }

        public Vector2I UVTiles
        {
            get 
            { 
                string val;
                if (UserData.TryGetValue("UVTiles", out val))
                {
                    string[] cmps = val.Split(' ');
                    if (cmps.Length != 2)
                        return Vector2I.One;

                    int x, y;
                    
                    if (!int.TryParse(cmps[0], out x))
                        return Vector2I.One;

                    if (!int.TryParse(cmps[1], out y))
                        return Vector2I.One;

                    return new Vector2I(x, y);
                }

                return Vector2I.One;
            }
        }

        public MyFacingEnum Facing
        {
            get
            {
                string facingVal;
                if (UserData.TryGetValue("Facing", out facingVal))
                {
                    MyFacingEnum facing;
                    if (!Enum.TryParse(facingVal, out facing))
                        return MyFacingEnum.None;

                    return facing;
                }

                return MyFacingEnum.None;
            }
        }

        public Vector2 WindScaleAndFreq
        {
            get
            {
                string windScaleVal;
                Vector2 windScaleAndFreq = Vector2.Zero;
                if (UserData.TryGetValue("WindScale", out windScaleVal))
                {
                    float f;
                    if (!float.TryParse(windScaleVal, out f))
                        return windScaleAndFreq;

                    windScaleAndFreq.X = f;

                    if (UserData.TryGetValue("WindFrequency", out windScaleVal))
                    {
                        if (!float.TryParse(windScaleVal, out f))
                            return windScaleAndFreq;
                    }

                    windScaleAndFreq.Y = f;
                }

                return windScaleAndFreq;
            }
        }
    }
}
