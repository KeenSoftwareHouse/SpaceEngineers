#region Using

using Sandbox.Engine.Physics;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Game.World;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Game.Gui;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;


#endregion

namespace Sandbox.Game.Gui
{
    #region Crosshair

    public class MyHudCrosshair
    {
        private struct SpriteInfo
        {
            public MyHudTexturesEnum SpriteEnum;
            public Color Color;
            public Vector2 HalfSize;
            public MyStringId SpriteId;
            public int FadeoutTime;
            public int TimeRemaining;
            public bool Visible;
        }

        private Vector2 m_rightVector;
        private Vector2 m_position;
        private List<SpriteInfo> m_sprites;
        private int m_lastGameplayTimeInMs;

        private static MyStringId m_defaultSpriteId;

        public Vector2 Position
        {
            get
            {
                return m_position;
            }
        }

        public static Vector2 ScreenCenter
        {
            get { return new Vector2(0.5f, MyGuiManager.GetHudSizeHalf().Y); }
        }

        public bool Visible { get; private set; }

        static MyHudCrosshair()
        {
            m_defaultSpriteId = MyStringId.GetOrCompute("Default");
        }

        public MyHudCrosshair()
        {
            m_sprites = new List<SpriteInfo>();
            m_lastGameplayTimeInMs = 0;
            ResetToDefault();
        }

        public void ResetToDefault(bool clear = true)
        {
            SetDefaults(clear);
        }

        public void HideDefaultSprite()
        {
            for (int i = 0; i < m_sprites.Count; ++i)
            {
                SpriteInfo sprite = m_sprites[i];
                if (sprite.SpriteId != m_defaultSpriteId)
                {
                    continue;
                }

                sprite.Visible = false;
                m_sprites[i] = sprite;
            }
        }

        public void Recenter()
        {
            m_position = ScreenCenter;
        }

        public void ChangePosition(Vector2 newPosition)
        {
            m_position = newPosition;
        }

        public void ChangeDefaultSprite(MyHudTexturesEnum newSprite, float size = 0.0f)
        {
            for (int i = 0; i < m_sprites.Count; ++i)
            {
                var sprite = m_sprites[i];
                if (sprite.SpriteId != m_defaultSpriteId)
                {
                    continue;
                }

                // Don't change the size by default
                if (size != 0.0f)
                {
                    sprite.HalfSize = Vector2.One * size;
                }
                sprite.SpriteEnum = newSprite;
                m_sprites[i] = sprite;
            }
        }

        /// <summary>
        /// Adds a temporary sprite to the list of sprites that make up the crosshair
        /// </summary>
        /// <param name="spriteEnum">Texture of the sprite to use</param>
        /// <param name="spriteId">An id that will be checked to prevent adding the same sprite twice</param>
        /// <param name="timeout">Time the sprite will be visible (includes fadeout time)</param>
        /// <param name="fadeTime">For how long should the sprite fade out when it disappears</param>
        public void AddTemporarySprite(MyHudTexturesEnum spriteEnum, MyStringId spriteId, int timeout = 2000, int fadeTime = 1000, Color? color = null, float size = 0.02f)
        {
            SpriteInfo sprite = new SpriteInfo();
            sprite.Color = color.HasValue ? color.Value : MyHudConstants.HUD_COLOR_LIGHT;
            sprite.FadeoutTime = fadeTime;
            sprite.HalfSize = Vector2.One * size;
            sprite.SpriteId = spriteId;
            sprite.SpriteEnum = spriteEnum;
            sprite.TimeRemaining = timeout;
            sprite.Visible = true;

            for (int i = 0; i < m_sprites.Count; ++i)
            {
                if (m_sprites[i].SpriteId == spriteId)
                {
                    m_sprites[i] = sprite;
                    return;
                }
            }

            m_sprites.Add(sprite);
        }

        private void SetDefaults(bool clear)
        {
            if (clear)
            {
                m_sprites.Clear();
            }
            CreateDefaultSprite();

            m_rightVector = Vector2.UnitX;
        }

        private void CreateDefaultSprite()
        {
            SpriteInfo newSprite = new SpriteInfo();
            newSprite.Color = MyHudConstants.HUD_COLOR_LIGHT;
            newSprite.FadeoutTime = 0;
            newSprite.HalfSize = Vector2.One * 0.02f;
            newSprite.SpriteId = m_defaultSpriteId;
            newSprite.SpriteEnum = MyHudTexturesEnum.crosshair;
            newSprite.TimeRemaining = 0;
            newSprite.Visible = true;

            bool found = false;
            for (int i = 0; i < m_sprites.Count; ++i)
            {
                if (m_sprites[i].SpriteId == m_defaultSpriteId)
                {
                    m_sprites[i] = newSprite;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                m_sprites.Add(newSprite);
            }
        }

        public static bool GetTarget(Vector3D from, Vector3D to, ref Vector3D target)
        {
            ProfilerShort.Begin("GetTarget");
            ProfilerShort.Begin("CastRay");
            var info = MyPhysics.CastRay(from, to, MyPhysics.CollisionLayers.DefaultCollisionLayer);
            if(info.HasValue)
                target = info.Value.Position;
            ProfilerShort.End();

            //Profiler.Begin("Iterate");
            //int index = 0;
            //while (index < hits.Count && 
            //    (hits[index].Body == null
            //    || hits[index].GetEntity() is Sandbox.Game.Weapons.MyAmmoBase
            //    || hits[index].GetEntity() is Sandbox.Game.Entities.Debris.MyDebrisBase))
            //{
            //    index++;
            //}
            //Profiler.End();

           // Vector3 targetPoint = to;

            ProfilerShort.End();

            return info.HasValue;
        }

        public static bool GetProjectedTarget(Vector3D from, Vector3D to, ref Vector2 target)
        {
            Vector3D targetPoint = Vector3D.Zero;

            if (GetTarget(from, to, ref targetPoint))
            {
                return GetProjectedVector(targetPoint, ref target);
            }

            return false;
        }

        public void Update()
        {
            int currentTime = MySandboxGame.TotalGamePlayTimeInMilliseconds;

            if (m_lastGameplayTimeInMs == 0)
            {
                m_lastGameplayTimeInMs = currentTime;
                return;
            }

            int dt = currentTime - m_lastGameplayTimeInMs;
            m_lastGameplayTimeInMs = currentTime;

            for (int i = 0; i < m_sprites.Count; ++i)
            {
                var sprite = m_sprites[i];
                if (sprite.SpriteId == m_defaultSpriteId)
                {
                    continue;
                }

                sprite.TimeRemaining -= dt;
                if (sprite.TimeRemaining <= 0)
                {
                    m_sprites.RemoveAt(i);
                    --i;
                }
                else
                {
                    m_sprites[i] = sprite;
                }
            }
        }

        public void Draw(string atlas, MyAtlasTextureCoordinate[] atlasCoords)
        {
            float hudSizeX = MyGuiManager.GetSafeFullscreenRectangle().Width / MyGuiManager.GetHudSize().X;
            float hudSizeY = MyGuiManager.GetSafeFullscreenRectangle().Height / MyGuiManager.GetHudSize().Y;
            var pos = m_position;
            if (MyVideoSettingsManager.IsTripleHead())
                pos.X += 1.0f;

            foreach (var sprite in m_sprites)
            {
                if (!sprite.Visible)
                {
                    continue;
                }

                int spriteCoord = (int)sprite.SpriteEnum;
                if (spriteCoord >= atlasCoords.Length)
                {
                    Debug.Assert(false, "Out of bounds of the crosshair array!");
                    continue;
                }

                MyAtlasTextureCoordinate textureCoord = atlasCoords[spriteCoord];

                Color spriteColor = sprite.Color;
                if (sprite.TimeRemaining < sprite.FadeoutTime)
                {
                    spriteColor.A = (byte)(spriteColor.A * sprite.TimeRemaining / sprite.FadeoutTime);
                }

                VRageRender.MyRenderProxy.DrawSpriteAtlas(
                    atlas,
                    pos,
                    textureCoord.Offset,
                    textureCoord.Size,
                    m_rightVector,
                    new Vector2(hudSizeX, hudSizeY),
                    spriteColor,
                    sprite.HalfSize);
            }
        }

        public static bool GetProjectedVector(Vector3D worldPosition, ref Vector2 target)
        {
            var transformedPoint = Vector3D.Transform(worldPosition, MySector.MainCamera.ViewMatrix);
            var projectedPoint = Vector4.Transform(transformedPoint, MySector.MainCamera.ProjectionMatrix);
            
            if (transformedPoint.Z > 0)
                return false;

            if (projectedPoint.W == 0) 
                return false;

            target = new Vector2(projectedPoint.X / projectedPoint.W / 2.0f + 0.5f, -projectedPoint.Y / projectedPoint.W / 2.0f + 0.5f);
            if (MyVideoSettingsManager.IsTripleHead())
            {
                target.X = (target.X - (1.0f / 3.0f)) / (1.0f / 3.0f);
            }

            target.Y *= MyGuiManager.GetHudSize().Y;

            return true;
        }
    }

    #endregion Crosshair
}
