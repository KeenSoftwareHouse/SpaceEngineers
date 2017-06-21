using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VRageMath;

namespace VRageRender.Import
{
    /// <summary>
    /// material params for export
    /// </summary>
    public class MyMaterialDescriptor
    {
        public string MaterialName { get; private set; }

        public Dictionary<string, string> Textures = new Dictionary<string, string>();
        public Dictionary<string, string> UserData = new Dictionary<string, string>();

        public string Technique { set; get; }

        public MyMeshDrawTechnique TechniqueEnum
        {
            get
            {
                MyMeshDrawTechnique ret;
                bool success = Enum.TryParse(Technique, out ret);
                MyRenderProxy.Assert(success, "Cannot convert to draw technique");
                return ret;
            }
            set { Technique = value.ToString(); }
        }

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

            if (version < 1052002)
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

			if (version >= 1068001) // 01068001
            {
                int userDataCount = reader.ReadInt32();
                for (int i = 0; i < userDataCount; i++)
                {
                    var name = reader.ReadString();
                    var data = reader.ReadString();
                    UserData.Add(name, data);
                }
            }

            if (version < 1157001)
            {
                reader.ReadSingle();  //SpecularPower 
                reader.ReadSingle();  //DiffuseColor.X
                reader.ReadSingle();  //DiffuseColor.Y
                reader.ReadSingle();  //DiffuseColor.Z
                reader.ReadSingle();  //ExtraData.X 
                reader.ReadSingle();  //ExtraData.Y
                reader.ReadSingle();  //ExtraData.Z
            }

            if (version < 1052001)
            {
                Technique = ((MyMeshDrawTechnique)reader.ReadInt32()).ToString();
            }
            else
                Technique = reader.ReadString();

            if (Technique == "GLASS")
            {
                if (version >= 1043001)
                {
                    GlassCW = reader.ReadString();
                    GlassCCW = reader.ReadString();
                    GlassSmoothNormals = reader.ReadBoolean();

                    // Partial backwards compatibility for old mods
                    if (!string.IsNullOrEmpty(GlassCCW) 
                        && !MyTransparentMaterials.ContainsMaterial(MaterialName) // Can be removed when all our materials are fixed
                        && MyTransparentMaterials.ContainsMaterial(GlassCCW)) // Can be removed when all our materials are fixed
                    {
                        MaterialName = GlassCCW;
                    }
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
                    if (!float.TryParse(windScaleVal, NumberStyles.Any, CultureInfo.InvariantCulture, out f))
                        return windScaleAndFreq;

                    windScaleAndFreq.X = f;

                    if (UserData.TryGetValue("WindFrequency", out windScaleVal))
                    {
                        if (!float.TryParse(windScaleVal, NumberStyles.Any, CultureInfo.InvariantCulture, out f))
                            return windScaleAndFreq;
                    }

                    windScaleAndFreq.Y = f;
                }

                return windScaleAndFreq;
            }
        }
    }
}
