using System;
using System.Collections.Generic;
using System.IO;
using VRage.FileSystem;
using VRage.Render11.Common;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace VRage.Render11.Resources
{
    internal class MyTextureAtlas
    {
        internal struct Element
        {
            internal ISrvBindable Texture;
            internal Vector4 UvOffsetScale;
        }
        Dictionary<string, Element> m_elements;

        internal MyTextureAtlas(string textureDir, string atlasFile)
        {
            ParseAtlasDescription(textureDir, atlasFile, out m_elements);
        }

        internal Element FindElement(string id) { return m_elements[id]; }

        static void ParseAtlasDescription(string textureDir, string atlasFile, out Dictionary<string, Element> atlasDict)
        {
            atlasDict = new Dictionary<string, Element>();
            MyFileTextureManager texManager = MyManagers.FileTextures;
            try
            {
                //var atlas = new MyTextureAtlas(64);
                var fsPath = Path.Combine(MyFileSystem.ContentPath, atlasFile);
                using (var file = MyFileSystem.OpenRead(fsPath))
                using (StreamReader sr = new StreamReader(file))
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

                        name = textureDir + System.IO.Path.GetFileName(name);
                        var atlasTexture = textureDir + atlasName;

                        var element = new Element();
                        element.Texture = texManager.GetTexture(atlasTexture, MyFileTextureEnum.GUI, true);
                        element.UvOffsetScale = uv;
                        atlasDict[name] = element;
                    }
                }

            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine("Warning: " + e.ToString());
            }
        }
    }
}
