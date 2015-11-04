using Sandbox.Common;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Generics;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.GUI.HudViewers
{
    public class MyHudMarkerRender
    {

        const float MAX_ANTENNA_DRAW_DISTANCE = 500000;

        static float m_friendAntennaRange = MAX_ANTENNA_DRAW_DISTANCE;

        public static float FriendAntennaRange 
        {
            get
            {
                return NormalizeLog(m_friendAntennaRange, 0.1f, MAX_ANTENNA_DRAW_DISTANCE);
            }
            set
            {
                m_friendAntennaRange = Denormalize(value);
            }
        }

        static float m_ownerAntennaRange = MAX_ANTENNA_DRAW_DISTANCE;
        public static float OwnerAntennaRange
        {
            get
            {
                return NormalizeLog(m_ownerAntennaRange, 0.1f, MAX_ANTENNA_DRAW_DISTANCE);
            }
            set
            {
                m_ownerAntennaRange = Denormalize(value);
            }
        }

        static float m_enemyAntennaRange = MAX_ANTENNA_DRAW_DISTANCE;
        public static float EnemyAntennaRange
        {
            get
            {
                return NormalizeLog(m_enemyAntennaRange, 0.1f, MAX_ANTENNA_DRAW_DISTANCE);
            }
            set
            {
                m_enemyAntennaRange = Denormalize(value);
            }
        }

        public class MyMarkerStyle
        {
            public MyFontEnum Font { get; set; }
            public MyHudTexturesEnum TextureDirectionIndicator { get; set; }
            public MyHudTexturesEnum TextureTarget { get; set; }
            public Color Color { get; set; }
            public float TextureTargetRotationSpeed { get; set; }
            public float TextureTargetScale { get; set; }

            public MyMarkerStyle(MyFontEnum font, MyHudTexturesEnum textureDirectionIndicator, MyHudTexturesEnum textureTarget, Color color, float textureTargetRotationSpeed = 0f, float textureTargetScale = 1f)
            {
                Font = font;
                TextureDirectionIndicator = textureDirectionIndicator;
                TextureTarget = textureTarget;
                this.Color = color;
                TextureTargetRotationSpeed = textureTargetRotationSpeed;
                TextureTargetScale = textureTargetScale;
            }
        }

        public class DistanceComparer : IComparer<MyHudEntityParams>
        {
            public int Compare(MyHudEntityParams x, MyHudEntityParams y)
            {
                return Vector3D.DistanceSquared(MySector.MainCamera.Position, y.Entity.PositionComp.GetPosition()).CompareTo(Vector3D.DistanceSquared(MySector.MainCamera.Position, x.Entity.PositionComp.GetPosition()));
            }
        }

        private MyGuiScreenHudBase m_hudScreen;
        private List<MyMarkerStyle> m_markerStyles;
        private int[] m_markerStylesForBlocks;

        private List<MyHudEntityParams> m_sortedMarkers = new List<MyHudEntityParams>(128);
        private DistanceComparer m_distanceComparer = new DistanceComparer();

        public MyHudMarkerRender(MyGuiScreenHudBase hudScreen)
        {
            m_hudScreen = hudScreen;
            m_markerStyles = new List<MyMarkerStyle>();

            int neutralStyle, enemyStyle, ownerStyle, factionStyle;
            neutralStyle = AllocateMarkerStyle(MyFontEnum.White, MyHudTexturesEnum.DirectionIndicator, MyHudTexturesEnum.Target_neutral, MyHudConstants.MARKER_COLOR_WHITE);
            enemyStyle = AllocateMarkerStyle(MyFontEnum.Red, MyHudTexturesEnum.DirectionIndicator, MyHudTexturesEnum.Target_enemy, MyHudConstants.MARKER_COLOR_WHITE);
            ownerStyle = AllocateMarkerStyle(MyFontEnum.DarkBlue, MyHudTexturesEnum.DirectionIndicator, MyHudTexturesEnum.Target_me, MyHudConstants.MARKER_COLOR_WHITE);
            factionStyle = AllocateMarkerStyle(MyFontEnum.Green, MyHudTexturesEnum.DirectionIndicator, MyHudTexturesEnum.Target_friend, MyHudConstants.MARKER_COLOR_WHITE);

            m_markerStylesForBlocks = new int[MyUtils.GetMaxValueFromEnum<MyRelationsBetweenPlayerAndBlock>() + 1];
            m_markerStylesForBlocks[(int)MyRelationsBetweenPlayerAndBlock.Neutral] = neutralStyle;
            m_markerStylesForBlocks[(int)MyRelationsBetweenPlayerAndBlock.Enemies] = enemyStyle;
            m_markerStylesForBlocks[(int)MyRelationsBetweenPlayerAndBlock.Owner] = ownerStyle;
            m_markerStylesForBlocks[(int)MyRelationsBetweenPlayerAndBlock.FactionShare] = factionStyle;
            m_markerStylesForBlocks[(int)MyRelationsBetweenPlayerAndBlock.NoOwnership] = factionStyle;
        }

        public int AllocateMarkerStyle(MyFontEnum font, MyHudTexturesEnum directionIcon, MyHudTexturesEnum targetIcon, Color color)
        {
            int newHandle = m_markerStyles.Count;
            m_markerStyles.Add(new MyMarkerStyle(font, directionIcon, targetIcon, color));
            return newHandle;
        }

        public void OverrideStyleForRelation(MyRelationsBetweenPlayerAndBlock relation, MyFontEnum font, MyHudTexturesEnum directionIcon, MyHudTexturesEnum targetIcon, Color color)
        {
            int handle = GetStyleForRelation(relation);
            m_markerStyles[handle] = new MyMarkerStyle(font, directionIcon, targetIcon, color);
        }

        public int GetStyleForRelation(MyRelationsBetweenPlayerAndBlock relation)
        {
            return m_markerStylesForBlocks[(int)relation];
        }

        public void DrawLocationMarkers(MyHudLocationMarkers locationMarkers)
        {
            ProfilerShort.Begin("MyHudMarkerRender.DrawLocationMarkers");

            m_sortedMarkers.Clear();
            foreach (var entityMarker in locationMarkers.MarkerEntities)
            {
                if (entityMarker.Value.Entity.PositionComp == null) //to draw marker entity must have position
                    continue;
                m_sortedMarkers.Add(entityMarker.Value);
            }
            m_sortedMarkers.Sort(m_distanceComparer);

            foreach (var entityMarker in m_sortedMarkers)
            {
                MyEntity entity = entityMarker.Entity;
                if (entityMarker.ShouldDraw != null && !entityMarker.ShouldDraw())
                    continue;

                float distance = (float)(MySector.MainCamera.Position - entity.PositionComp.WorldVolume.Center).Length();
                if ((entityMarker.TargetMode == MyRelationsBetweenPlayerAndBlock.NoOwnership ||
                     entityMarker.TargetMode == MyRelationsBetweenPlayerAndBlock.FactionShare) && m_friendAntennaRange < distance)
                {
                    continue;
                }
                if ((entityMarker.TargetMode == MyRelationsBetweenPlayerAndBlock.Neutral ||
                    entityMarker.TargetMode == MyRelationsBetweenPlayerAndBlock.Enemies) && m_enemyAntennaRange < distance)
                {
                    continue;
                }
                if (entityMarker.TargetMode == MyRelationsBetweenPlayerAndBlock.Owner && m_ownerAntennaRange < distance)
                {
                    continue;
                }

                if (entityMarker.MustBeDirectlyVisible)
                {
                    LineD raycast = new LineD(MySector.MainCamera.Position, (Vector3)entity.PositionComp.WorldVolume.Center);
                    raycast.From += raycast.Direction;
                    var result = MyEntities.GetIntersectionWithLine(ref raycast, entity, MySession.ControlledEntity as MyEntity);
                    if (result.HasValue && !(result.Value.Entity == entity ||
                                             result.Value.Entity.Parent == entity ||
                                             result.Value.Entity == entity.Parent))
                        continue;
                }

                DrawLocationMarker(
                    GetStyleForRelation(entityMarker.TargetMode),
                    entity.LocationForHudMarker,
                    entityMarker,
                    0, 0);
            }

            m_hudScreen.DrawTexts();

            ProfilerShort.End();
        }

        /// <summary>
        /// Draws location marker on screen.
        /// </summary>
        public void DrawLocationMarker(int styleHandle, Vector3D position, MyHudEntityParams hudParams, float targetDamageRatio, float targetArmorRatio, float alphaMultiplifier = 1f)
        {
            if (MySession.ControlledEntity == null)
                return;

            //  Transform point to camera space, so Z = -1 is always forward and then do projective transformation
            Vector3D transformedPoint = Vector3D.Transform(position, MySector.MainCamera.ViewMatrix);
            Vector4D projectedPoint = Vector4D.Transform(transformedPoint, MySector.MainCamera.ProjectionMatrix);

            //  If point is behind camera we swap X and Y (this is mirror-like transformation)
            if (transformedPoint.Z > 0)
            {
                projectedPoint.X *= -1;
                projectedPoint.Y *= -1;
            }

            if (projectedPoint.W == 0)
                return;

            MyMarkerStyle markerStyle = m_markerStyles[styleHandle];

            double distance = Vector3D.Distance(position, MySession.ControlledEntity.Entity.WorldMatrix.Translation);
            float maxDistance = hudParams.MaxDistance;
            byte colorAlphaInByte = 255;
            var hudColor = MyFakes.SHOW_FACTIONS_GUI ? markerStyle.Color : Color.White;
            hudColor.A = (byte)(colorAlphaInByte * alphaMultiplifier);

            //  Calculate centered coordinates in range <0..1>
            Vector2 projectedPoint2D = new Vector2((float)(projectedPoint.X / projectedPoint.W / 2.0f) + 0.5f, (float)(-projectedPoint.Y / projectedPoint.W) / 2.0f + 0.5f);
            if (MyVideoSettingsManager.IsTripleHead())
            {
                projectedPoint2D.X = (projectedPoint2D.X - (1.0f / 3.0f)) / (1.0f / 3.0f);
            }

            float objectNameYOffset = 0.0f; //offset to direction indicator

            //  This will bound the rectangle in circle, although it isn't real circle because we work in [1,1] dimensions, 
            //  but number of horizontal pixels is bigger, so at the end it's more elypse
            //  It must be done when point is out of circle or behind the camera
            Vector2 direction = projectedPoint2D - MyHudConstants.DIRECTION_INDICATOR_SCREEN_CENTER;
            if ((direction.Length() > MyHudConstants.DIRECTION_INDICATOR_MAX_SCREEN_DISTANCE) || (transformedPoint.Z > 0))
            {
                if ((hudParams.FlagsEnum & MyHudIndicatorFlagsEnum.SHOW_BORDER_INDICATORS) == 0)
                {
                    return;
                }

                if (direction.LengthSquared() > MyMathConstants.EPSILON_SQUARED)
                {
                    direction.Normalize();
                }
                else
                {
                    direction = new Vector2(1f, 0f);
                }
                projectedPoint2D = MyHudConstants.DIRECTION_INDICATOR_SCREEN_CENTER + direction * MyHudConstants.DIRECTION_INDICATOR_MAX_SCREEN_DISTANCE;

                //  Fix vertical scale
                projectedPoint2D.Y *= MyGuiManager.GetHudSize().Y;

                AddTexturedQuad(markerStyle.TextureDirectionIndicator, projectedPoint2D + direction * MyHudConstants.HUD_DIRECTION_INDICATOR_SIZE * 2.5f, direction,
                       hudColor, MyHudConstants.HUD_DIRECTION_INDICATOR_SIZE * 1.2f, MyHudConstants.HUD_DIRECTION_INDICATOR_SIZE * 0.8f);
            }
            else
            {
                //  Fix vertical scale
                projectedPoint2D.Y *= MyGuiManager.GetHudSize().Y;

                Color rectangleColor = Color.White;
                rectangleColor.A = colorAlphaInByte;

                if ((hudParams.FlagsEnum & MyHudIndicatorFlagsEnum.SHOW_FOCUS_MARK) > 0)
                {
                    Vector2 upVector = new Vector2(0, -1);
                    if (markerStyle.TextureTargetRotationSpeed != 0)
                    {
                        upVector = new Vector2((float)Math.Cos(MySandboxGame.TotalGamePlayTimeInMilliseconds / 1000f * markerStyle.TextureTargetRotationSpeed * MathHelper.Pi),
                                                (float)Math.Sin(MySandboxGame.TotalGamePlayTimeInMilliseconds / 1000f * markerStyle.TextureTargetRotationSpeed * MathHelper.Pi));
                    }

                    AddTexturedQuad(markerStyle.TextureTarget, projectedPoint2D, upVector, hudColor, MyHudConstants.HUD_DIRECTION_INDICATOR_SIZE * markerStyle.TextureTargetScale, MyHudConstants.HUD_DIRECTION_INDICATOR_SIZE * markerStyle.TextureTargetScale);
                }

                objectNameYOffset = -MyHudConstants.HUD_TEXTS_OFFSET;
            }

            if (hudParams.Text != null && hudParams.Text.Length > 0 && (hudParams.FlagsEnum & MyHudIndicatorFlagsEnum.SHOW_TEXT) != 0)
            {
                //  Add object's name
                MyHudText objectName = m_hudScreen.AllocateText();
                if (objectName != null)
                {
                    objectName.Start(markerStyle.Font, projectedPoint2D + new Vector2(0, hudParams.OffsetText ? objectNameYOffset : 0),
                        hudColor, 0.8f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    objectName.Append(hudParams.Text);
                }
            }

            // display hud icon
            if (hudParams.Icon != null && (hudParams.FlagsEnum & MyHudIndicatorFlagsEnum.SHOW_ICON) != 0)
            {
                Color iconColor = hudParams.IconColor.HasValue && MyFakes.SHOW_FACTIONS_GUI ? hudParams.IconColor.Value : Color.White;
                iconColor.A = (byte)(colorAlphaInByte * alphaMultiplifier);

                AddTexturedQuad(hudParams.Icon.Value, projectedPoint2D + hudParams.IconOffset, new Vector2(0, -1), iconColor, hudParams.IconSize.X / 2f, hudParams.IconSize.Y / 2f);
            }

            if (MyFakes.SHOW_HUD_DISTANCES && (hudParams.FlagsEnum & MyHudIndicatorFlagsEnum.SHOW_DISTANCE) != 0)
            {
                //  Add distance to object
                MyHudText objectDistance = m_hudScreen.AllocateText();
                if (objectDistance != null)
                {
                    objectDistance.Start(markerStyle.Font, projectedPoint2D + new Vector2(0, MyHudConstants.HUD_TEXTS_OFFSET),
                        hudColor, 0.8f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);

                    //  Create string builder with distance in metres, e.g. "123m"                    
                    objectDistance.AppendInt32((int)Math.Round(distance));
                    objectDistance.Append("m");
                }
            }
        }

        /// <summary>
        /// Add textured quad with specified UP direction and width/height.
        /// </summary>
        protected void AddTexturedQuad(MyHudTexturesEnum texture, Vector2 position, Vector2 upVector, Color color, float halfWidth, float halfHeight)
        {
            Vector2 rightVector = new Vector2(-upVector.Y, upVector.X);

            MyAtlasTextureCoordinate textureCoord = m_hudScreen.GetTextureCoord(texture);

            float hudSizeX = MyGuiManager.GetSafeFullscreenRectangle().Width / MyGuiManager.GetHudSize().X;
            float hudSizeY = MyGuiManager.GetSafeFullscreenRectangle().Height / MyGuiManager.GetHudSize().Y;
            var pos = position;
            if (MyVideoSettingsManager.IsTripleHead())
                pos.X += 1.0f;

            VRageRender.MyRenderProxy.DrawSpriteAtlas(
                m_hudScreen.TextureAtlas,
                pos,
                textureCoord.Offset,
                textureCoord.Size,
                rightVector,
                new Vector2(hudSizeX, hudSizeY),
                color,
                new Vector2(halfWidth, halfHeight));
        }

		static public float Normalize(float value)
		{
			return NormalizeLog(value, 0.1f, MAX_ANTENNA_DRAW_DISTANCE);
		}

        static public float Denormalize(float value)
        {
            return DenormalizeLog(value, 0.1f, MAX_ANTENNA_DRAW_DISTANCE);
        }
        static private float NormalizeLog(float f, float min, float max)
        {
            return MathHelper.Clamp(MathHelper.InterpLogInv(f, min, max), 0, 1);
        }

        static private float DenormalizeLog(float f, float min, float max)
        {
            return MathHelper.Clamp(MathHelper.InterpLog(f, min, max), min, max);
        }
    }
}
