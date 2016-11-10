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
using System.Threading.Tasks;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.Gui;
using VRage.Profiler;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.GUI.HudViewers
{
    public class MyHudMarkerRenderBase
    {
        protected const double LS_METRES = 299792458.0001367;
        protected const double LY_METRES = 9.460730473e+15;

        public class MyMarkerStyle
        {
            public string Font { get; set; }
            public MyHudTexturesEnum TextureDirectionIndicator { get; set; }
            public MyHudTexturesEnum TextureTarget { get; set; }
            public Color Color { get; set; }
            public float TextureTargetRotationSpeed { get; set; }
            public float TextureTargetScale { get; set; }

            public MyMarkerStyle(string font, MyHudTexturesEnum textureDirectionIndicator, MyHudTexturesEnum textureTarget, Color color, float textureTargetRotationSpeed = 0f, float textureTargetScale = 1f)
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

        protected MyGuiScreenHudBase m_hudScreen;
        protected List<MyMarkerStyle> m_markerStyles;
        protected int[] m_markerStylesForBlocks;

        protected List<MyHudEntityParams> m_sortedMarkers = new List<MyHudEntityParams>(128);
        protected DistanceComparer m_distanceComparer = new DistanceComparer();

        public MyHudMarkerRenderBase(MyGuiScreenHudBase hudScreen)
        {
            m_hudScreen = hudScreen;
            m_markerStyles = new List<MyMarkerStyle>();

            int neutralStyle, enemyStyle, ownerStyle, factionStyle;
            neutralStyle = AllocateMarkerStyle(MyFontEnum.White, MyHudTexturesEnum.DirectionIndicator, MyHudTexturesEnum.Target_neutral, MyHudConstants.MARKER_COLOR_WHITE);
            enemyStyle = AllocateMarkerStyle(MyFontEnum.Red, MyHudTexturesEnum.DirectionIndicator, MyHudTexturesEnum.Target_enemy, MyHudConstants.MARKER_COLOR_WHITE);
            ownerStyle = AllocateMarkerStyle(MyFontEnum.DarkBlue, MyHudTexturesEnum.DirectionIndicator, MyHudTexturesEnum.Target_me, MyHudConstants.MARKER_COLOR_WHITE);
            factionStyle = AllocateMarkerStyle(MyFontEnum.Green, MyHudTexturesEnum.DirectionIndicator, MyHudTexturesEnum.Target_friend, MyHudConstants.MARKER_COLOR_WHITE);

            m_markerStylesForBlocks = new int[MyUtils.GetMaxValueFromEnum<VRage.Game.MyRelationsBetweenPlayerAndBlock>() + 1];
            m_markerStylesForBlocks[(int)VRage.Game.MyRelationsBetweenPlayerAndBlock.Neutral] = neutralStyle;
            m_markerStylesForBlocks[(int)VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies] = enemyStyle;
            m_markerStylesForBlocks[(int)VRage.Game.MyRelationsBetweenPlayerAndBlock.Owner] = ownerStyle;
            m_markerStylesForBlocks[(int)VRage.Game.MyRelationsBetweenPlayerAndBlock.FactionShare] = factionStyle;
            m_markerStylesForBlocks[(int)VRage.Game.MyRelationsBetweenPlayerAndBlock.NoOwnership] = factionStyle;
        }

        public virtual void Update()
        {
        }

        public virtual void Draw()
        {
        }

        public int AllocateMarkerStyle(string font, MyHudTexturesEnum directionIcon, MyHudTexturesEnum targetIcon, Color color)
        {
            int newHandle = m_markerStyles.Count;
            m_markerStyles.Add(new MyMarkerStyle(font, directionIcon, targetIcon, color));
            return newHandle;
        }

        public void OverrideStyleForRelation(VRage.Game.MyRelationsBetweenPlayerAndBlock relation, string font, MyHudTexturesEnum directionIcon, MyHudTexturesEnum targetIcon, Color color)
        {
            int handle = GetStyleForRelation(relation);
            m_markerStyles[handle] = new MyMarkerStyle(font, directionIcon, targetIcon, color);
        }

        public int GetStyleForRelation(VRage.Game.MyRelationsBetweenPlayerAndBlock relation)
        {
            return m_markerStylesForBlocks[(int)relation];
        }

        public virtual void DrawLocationMarkers(MyHudLocationMarkers locationMarkers)
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
                MyEntity entity = entityMarker.Entity as MyEntity;
                if (entityMarker.ShouldDraw != null && !entityMarker.ShouldDraw())
                    continue;

                if (entityMarker.MustBeDirectlyVisible)
                {
                    LineD raycast = new LineD(MySector.MainCamera.Position, (Vector3)entity.PositionComp.WorldVolume.Center);
                    raycast.From += raycast.Direction;
                    var result = MyEntities.GetIntersectionWithLine(ref raycast, entity, MySession.Static.ControlledEntity as MyEntity);
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
            if (MySession.Static.ControlledEntity == null)
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

            double distance = Vector3D.Distance(position, MySession.Static.ControlledEntity.Entity.WorldMatrix.Translation);
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

                    // Create a string builder with the distance in metres, kilometres, light seconds or light years
                    if (distance > LY_METRES)
                    {
                        objectDistance.Append(Math.Round(distance / LY_METRES, 2).ToString());
                        objectDistance.Append("ly");
                    }
                    else if (distance > LS_METRES)
                    {
                        objectDistance.Append(Math.Round(distance / LS_METRES, 2).ToString());
                        objectDistance.Append("ls");
                    }
                    else if (distance > 1000)
                    {
                        objectDistance.Append(Math.Round(distance / 1000, 2).ToString());
                        objectDistance.Append("km");
                    }
                    else
                    {
                        objectDistance.Append(Math.Round(distance, 2).ToString());
                        objectDistance.Append("m");
                    }
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

            Vector2 screen = new Vector2(MyGuiManager.GetSafeFullscreenRectangle().Width, MyGuiManager.GetSafeFullscreenRectangle().Height);

            float hudSizeX = screen.X / MyGuiManager.GetHudSize().X;
            float hudSizeY = screen.Y / MyGuiManager.GetHudSize().Y;

            var pos = position;
            if (MyVideoSettingsManager.IsTripleHead())
                pos.X += 1.0f;

            float yScale = screen.Y / 1080f;
            halfWidth *= yScale;
            halfHeight *= yScale;

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

        /// <summary>
        /// Add textured quad with specified UP direction and width/height.
        /// </summary>
        protected void AddTexturedQuad(string texture, Vector2 position, Vector2 upVector, Color color, float halfWidth, float halfHeight)
        {
            Vector2 screen = new Vector2(MyGuiManager.GetSafeFullscreenRectangle().Width, MyGuiManager.GetSafeFullscreenRectangle().Height);

            float hudSizeX = screen.X / MyGuiManager.GetHudSize().X;
            float hudSizeY = screen.Y / MyGuiManager.GetHudSize().Y;

            if (MyVideoSettingsManager.IsTripleHead())
                position.X += 1.0f;

            position.X *= hudSizeX;
            position.Y *= hudSizeY;

            float yScale = screen.Y / 1080f;
            halfWidth *= yScale;
            halfHeight *= yScale;

            RectangleF dest = new RectangleF(position.X - halfWidth, position.Y - halfHeight, halfWidth * 2, halfHeight * 2);
            Rectangle? source = null;

            VRageRender.MyRenderProxy.DrawSprite(texture, ref dest, false, ref source, color, 0,
                new Vector2(1, 0), ref Vector2.Zero, SpriteEffects.None, 0);
        }
    }
}
