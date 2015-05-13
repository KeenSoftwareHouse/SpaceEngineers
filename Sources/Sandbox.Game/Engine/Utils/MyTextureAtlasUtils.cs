using Sandbox.Common;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using VRage;
using VRage.FileSystem;
using VRage.Library.Utils;
using VRage.Utils;
using VRageRender;
using BoundingBox = VRageMath.BoundingBox;
using BoundingSphere = VRageMath.BoundingSphere;
using Line = VRageMath.Line;
using MathHelper = VRageMath.MathHelper;
using Matrix = VRageMath.Matrix;
using Vector2 = VRageMath.Vector2;
using Vector3 = VRageMath.Vector3;
using Vector4 = VRageMath.Vector4;

namespace Sandbox.Engine.Utils
{
    class MyTextureAtlasUtils
    {
        private static MyTextureAtlas LoadTextureAtlas(string textureDir, string atlasFile)
        {
            var atlas = new MyTextureAtlas(64);
            var fsPath = Path.Combine(MyFileSystem.ContentPath, atlasFile);

            using (var stream = MyFileSystem.OpenRead(fsPath))
            using (StreamReader sr = new StreamReader(stream))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();

                    if (line.StartsWith("#"))
                        continue;
                    if (line.Trim(' ').Length == 0)
                        continue;

                    string[] parts = line.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);

                    string name = parts[0];

                    string atlasName = parts[1];

                    Vector4 uv = new Vector4(
                        Convert.ToSingle(parts[4], System.Globalization.CultureInfo.InvariantCulture),
                        Convert.ToSingle(parts[5], System.Globalization.CultureInfo.InvariantCulture),
                        Convert.ToSingle(parts[7], System.Globalization.CultureInfo.InvariantCulture),
                        Convert.ToSingle(parts[8], System.Globalization.CultureInfo.InvariantCulture));

                    string atlasTexture = textureDir + atlasName;
                    MyTextureAtlasItem item = new MyTextureAtlasItem(atlasTexture, uv);
                    atlas.Add(name, item);
                }
            }

            return atlas;
        }

        public static void LoadTextureAtlas(string[] enumsToStrings, string textureDir, string atlasFile, out string texture, out MyAtlasTextureCoordinate[] textureCoords)
        {
            MyTextureAtlas atlas = LoadTextureAtlas(textureDir, atlasFile);

            //  Here we define particle texture coordinates inside of texture atlas
            textureCoords = new MyAtlasTextureCoordinate[enumsToStrings.Length];

            texture = null;

            for (int i = 0; i < enumsToStrings.Length; i++)
            {
                MyTextureAtlasItem textureAtlasItem = atlas[enumsToStrings[i]];

                textureCoords[i] = new MyAtlasTextureCoordinate(new Vector2(textureAtlasItem.UVOffsets.X, textureAtlasItem.UVOffsets.Y), new Vector2(textureAtlasItem.UVOffsets.Z, textureAtlasItem.UVOffsets.W));

                //  Texture atlas content processor support having more DDS files for one atlas, but we don't want it (because we want to have all particles in one texture, so we can draw fast).
                //  So here we just take first and only texture.
                if (texture == null)
                {
                    texture = textureAtlasItem.AtlasTexture;
                }
            }
        }
    }
}
