using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Graphics.GUI
{
    public static class MyGuiTextShadows
    {
        public const string TEXT_SHADOW_DEFAULT = "Default";

        private static Dictionary<string, ShadowTextureSet> m_textureSets = new Dictionary<string, ShadowTextureSet>();

        public static void AddTextureSet(string name, IEnumerable<ShadowTexture> textures)
        {
            ShadowTextureSet set = new ShadowTextureSet();
            set.AddTextures(textures);
            m_textureSets[name] = set;
        }

        public static void ClearShadowTextures()
        {
            m_textureSets.Clear();
        }

        public static void DrawShadow(ref Vector2 position, ref Vector2 textSize, string textureSet = null, float fogAlphaMultiplier = 1,
            MyGuiDrawAlignEnum alignment = MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER)
        {
            if (textureSet == null)
                textureSet = TEXT_SHADOW_DEFAULT;

            ShadowTexture texture;
            Vector2 shadowSize = GetShadowSize(ref textSize, textureSet, out texture);
            Vector2 shadowPosition = AdjustPosition(position, ref textSize, ref shadowSize, alignment);

            Color color = new Color(0, 0, 0, (byte)(255 * texture.DefaultAlpha * fogAlphaMultiplier));
            MyGuiManager.DrawSpriteBatch(texture.Texture, shadowPosition, shadowSize, color, alignment);
        }

        public static Vector2 GetShadowSize(ref Vector2 size, string textureSet = null)
        {
            if (textureSet == null)
                textureSet = TEXT_SHADOW_DEFAULT;

            ShadowTexture texture;
            Vector2 shadowSize = GetShadowSize(ref size, textureSet, out texture);
            return shadowSize;
        }

        private static Vector2 GetShadowSize(ref Vector2 size, string textureSet, out ShadowTexture texture)
        {
            Vector2 shadowSize = size;

            ShadowTextureSet set;
            bool found = m_textureSets.TryGetValue(textureSet, out set);
            if (!found)
            {
                found = m_textureSets.TryGetValue(TEXT_SHADOW_DEFAULT, out set);
                if (!found)
                    throw new Exception("Missing Default shadow texture. Check ShadowTextureSets.sbc");
            }

            texture = set.GetOptimalTexture(size.X);

            shadowSize.X *= texture.GrowFactorWidth;
            shadowSize.Y *= texture.GrowFactorHeight;
            return shadowSize;
        }

        private static Vector2 AdjustPosition(Vector2 position, ref Vector2 textSize, ref Vector2 shadowSize, MyGuiDrawAlignEnum alignment)
        {
            // CHECK-ME: May be needed to support other alignments
            switch (alignment)
            {
                case MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER:
                {
                    // Do nothing
                    break;
                }
                case MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP:
                {
                    float diffWidth = shadowSize.X - textSize.X;
                    float diffHeight = shadowSize.Y - textSize.Y;
                    position.X -= diffWidth / 2;
                    position.Y -= diffHeight / 2;
                    break;
                }
                case MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP:
                {
                    float diffWidth = shadowSize.X - textSize.X;
                    float diffHeight = shadowSize.Y - textSize.Y;
                    position.X += diffWidth / 2;
                    position.Y -= diffHeight / 2;
                    break;
                }
            }

            return position;
        }
    }

    public class ShadowTextureSet
    {
        public List<ShadowTexture> Textures { get; private set; }

        public ShadowTextureSet()
        {
            Textures = new List<ShadowTexture>();
        }

        public void AddTextures(IEnumerable<ShadowTexture> textures)
        {
            Textures.AddRange(textures);
            Textures.Sort((t1, t2) =>
            {
                if (t1.MinWidth == t2.MinWidth)
                    return 0;
                else if (t1.MinWidth < t2.MinWidth)
                    return -1;
                else
                    return 1;
            });
        }

        public ShadowTexture GetOptimalTexture(float size)
        {
            int indexLow = 0;
            int indexUp = Textures.Count - 1;
            int indexDiff = indexUp - indexLow;

            int indexMiddle;
            while (indexDiff >= 0)
            {
                indexMiddle = (indexDiff / 2) + indexLow;

                ShadowTexture texture = Textures[indexMiddle];
                float minSizeMiddle = texture.MinWidth;

                if (size == minSizeMiddle || indexDiff == 0 && size > minSizeMiddle)
                {
                    return texture;
                }
                else if (minSizeMiddle > size)
                {
                    indexUp = indexMiddle - 1;
                }
                else
                {
                    indexLow = indexMiddle + 1;
                }

                indexDiff = indexUp - indexLow;
            };

            return indexUp > 0 ? Textures[indexUp] : Textures[indexLow];
        }
    }

    [DebuggerDisplay("MinSize = { MinSize }")]
    public class ShadowTexture
    {
        public ShadowTexture(string texture, float minwidth, float growFactorWidth, float growFactorHeight, float defaultAlpha)
        {
            Texture = texture;
            MinWidth = minwidth;
            GrowFactorWidth = growFactorWidth;
            GrowFactorHeight = growFactorHeight;
            DefaultAlpha = defaultAlpha;
        }

        public string Texture { get; private set; }

        public float MinWidth { get; private set; }

        public float GrowFactorWidth { get; private set; }

        public float GrowFactorHeight { get; private set; }

        public float DefaultAlpha { get; private set; }
    }
}
