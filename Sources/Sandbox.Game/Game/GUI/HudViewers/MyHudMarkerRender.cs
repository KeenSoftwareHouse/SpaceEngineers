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
using VRage.Collections;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.Gui;
using VRage.Generics;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.GUI.HudViewers
{
    public class MyHudMarkerRender
    {

        //const float MAX_ANTENNA_DRAW_DISTANCE = 500000;
        const double LS_METRES = 299792458.0001367;
        const double LY_METRES = 9.460730473e+15;

        static float m_friendAntennaRange = MyPerGameSettings.MaxAntennaDrawDistance;

        public static float FriendAntennaRange 
        {
            get
            {
                return NormalizeLog(m_friendAntennaRange, 0.1f, MyPerGameSettings.MaxAntennaDrawDistance);
            }
            set
            {
                m_friendAntennaRange = Denormalize(value);
            }
        }

        static float m_ownerAntennaRange = MyPerGameSettings.MaxAntennaDrawDistance;
        public static float OwnerAntennaRange
        {
            get
            {
                return NormalizeLog(m_ownerAntennaRange, 0.1f, MyPerGameSettings.MaxAntennaDrawDistance);
            }
            set
            {
                m_ownerAntennaRange = Denormalize(value);
            }
        }

        static float m_enemyAntennaRange = MyPerGameSettings.MaxAntennaDrawDistance;
        public static float EnemyAntennaRange
        {
            get
            {
                return NormalizeLog(m_enemyAntennaRange, 0.1f, MyPerGameSettings.MaxAntennaDrawDistance);
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

            m_markerStylesForBlocks = new int[MyUtils.GetMaxValueFromEnum<VRage.Game.MyRelationsBetweenPlayerAndBlock>() + 1];
            m_markerStylesForBlocks[(int)VRage.Game.MyRelationsBetweenPlayerAndBlock.Neutral] = neutralStyle;
            m_markerStylesForBlocks[(int)VRage.Game.MyRelationsBetweenPlayerAndBlock.Enemies] = enemyStyle;
            m_markerStylesForBlocks[(int)VRage.Game.MyRelationsBetweenPlayerAndBlock.Owner] = ownerStyle;
            m_markerStylesForBlocks[(int)VRage.Game.MyRelationsBetweenPlayerAndBlock.FactionShare] = factionStyle;
            m_markerStylesForBlocks[(int)VRage.Game.MyRelationsBetweenPlayerAndBlock.NoOwnership] = factionStyle;
        }

        public int AllocateMarkerStyle(MyFontEnum font, MyHudTexturesEnum directionIcon, MyHudTexturesEnum targetIcon, Color color)
        {
            int newHandle = m_markerStyles.Count;
            m_markerStyles.Add(new MyMarkerStyle(font, directionIcon, targetIcon, color));
            return newHandle;
        }

        public void OverrideStyleForRelation(VRage.Game.MyRelationsBetweenPlayerAndBlock relation, MyFontEnum font, MyHudTexturesEnum directionIcon, MyHudTexturesEnum targetIcon, Color color)
        {
            int handle = GetStyleForRelation(relation);
            m_markerStyles[handle] = new MyMarkerStyle(font, directionIcon, targetIcon, color);
        }

        public int GetStyleForRelation(VRage.Game.MyRelationsBetweenPlayerAndBlock relation)
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
                MyEntity entity = entityMarker.Entity as MyEntity;
                if (entityMarker.ShouldDraw != null && !entityMarker.ShouldDraw())
                    continue;

                AddEntity(entity, entityMarker.TargetMode, entityMarker.Text);
            }

            m_hudScreen.DrawTexts();

            ProfilerShort.End();
        }

        private MyDynamicObjectPool<PointOfInterest> m_pointOfInterestPool = new MyDynamicObjectPool<PointOfInterest>(32);
        private List<PointOfInterest> m_pointsOfInterest = new List<PointOfInterest>();
        private class PointOfInterest
        {
            public const double ClusterAngle = 2.5;
            public const int MaxTextLength = 64;
            public const double ClusterNearDistance = 3500;
            public const double ClusterScaleDistance = 20000;
            public const double MinimumTargetRange = 2000;

            public enum PointOfInterestState
            {
                NonDirectional = 0,
                Directional,
            }

            public enum PointOfInterestType
            {
                /// <summary>
                /// Used for anything unknown
                /// </summary>
                Unknown = 0,

                /// <summary>
                /// Used for turret targets
                /// </summary>
                Target,

                /// <summary>
                /// Used for grouped POIs
                /// </summary>
                Group,

                /// <summary>
                /// Used for ore
                /// </summary>
                Ore,

                /// <summary>
                /// Used for hacked blocks
                /// </summary>
                Hack,

                /// <summary>
                /// Used for grids outside of grid identification range
                /// </summary>
                UnknownEntity,

                /// <summary>
                /// Used for characters
                /// </summary>
                Character,

                /// <summary>
                /// Used for small entities
                /// </summary>
                SmallEntity,

                /// <summary>
                /// Used for large entities
                /// </summary>
                LargeEntity,

                /// <summary>
                /// Used for static entities (Stations, etc)
                /// </summary>
                StaticEntity,
                
                /// <summary>
                /// Used for GPS coordinates
                /// </summary>
                GPS,
            }

            // World state
            public Vector3D WorldPosition { get; private set; }
            public PointOfInterestType POIType { get; private set; }
            public MyRelationsBetweenPlayerAndBlock Relationship { get; private set; }
            public MyEntity Entity { get; private set; }

            public StringBuilder Text { get; private set; }

            public List<PointOfInterest> m_group = new List<PointOfInterest>(10);

            public double Distance { get; private set; }

            public PointOfInterest()
            {
                WorldPosition = Vector3D.Zero;
                POIType = PointOfInterestType.Unknown;
                Relationship = MyRelationsBetweenPlayerAndBlock.Owner;

                Text = new StringBuilder(MaxTextLength, MaxTextLength);
            }

            /// <summary>
            /// Clears out all data and resets the POI for re-use.
            /// </summary>
            public void Reset()
            {
                WorldPosition = Vector3D.Zero;
                POIType = PointOfInterestType.Unknown;
                Relationship = MyRelationsBetweenPlayerAndBlock.Owner;
                Entity = null;

                Text.Clear();
                m_group.Clear();
                Distance = 0;
            }

            /// <summary>
            /// Sets the POI state
            /// </summary>
            /// <param name="position">World position of the POI.</param>
            /// <param name="type">POI Type, grid, ore, gps, etc.</param>
            /// <param name="relationship">Relationship of the local player to this POI</param>
            public void SetState(Vector3D position, PointOfInterestType type, MyRelationsBetweenPlayerAndBlock relationship)
            {
                WorldPosition = position;
                POIType = type;
                Relationship = relationship;

                Vector3D viewDirection = position - MySector.MainCamera.WorldMatrix.Translation;
                Distance = viewDirection.Length();
            }

            /// <summary>
            /// Stores the entity that goes with this POI
            /// </summary>
            /// <param name="entity">MyEntity that this POI is for</param>
            public void SetEntity(MyEntity entity)
            {
                Entity = entity;
            }

            /// <summary>
            /// Sets the text message for this POI, limited to MaxTextLength characters.
            /// </summary>
            /// <param name="text">The text message to set, limited to MaxTextLength characters.</param>
            public void SetText(StringBuilder text)
            {
                Text.Clear();

                if (text == null) return;

                Text.AppendSubstring(text, 0, Math.Min(text.Length, MaxTextLength));
            }

            /// <summary>
            /// Sets the text message for this POI, limited to MaxTextLength characters.
            /// </summary>
            /// <param name="text">The text message to set, limited to MaxTextLength characters.</param>
            public void SetText(string text)
            {
                Text.Clear();

                if (string.IsNullOrWhiteSpace(text)) return;

                Text.Append(text, 0, Math.Min(text.Length, MaxTextLength));
            }

            /// <summary>
            /// Adds another POI to this POI, turning this into a POI group.
            /// </summary>
            /// <param name="poi"></param>
            /// <returns>True if POI was added, false if not. Probable cause for failure is that this is not a group POI.</returns>
            public bool AddPOI(PointOfInterest poi)
            {
                if (POIType != PointOfInterestType.Group) return false;

                // Add poi to group and recompute average world position
                Vector3D worldPosition = WorldPosition;
                worldPosition *= m_group.Count;

                m_group.Add(poi);

                Text.Clear();
                Text.Append(m_group.Count);
                Text.Append(" Points of Interest");

                worldPosition += poi.WorldPosition;
                WorldPosition = worldPosition / m_group.Count;

                Vector3D viewDirection = WorldPosition - MySector.MainCamera.WorldMatrix.Translation;
                Distance = viewDirection.Length();

                if (poi.Relationship > Relationship)
                    Relationship = poi.Relationship;

                return true;
            }

            /// <summary>
            /// Checks if some given POI is within a radius matching an angle from the POV of a given location.
            /// </summary>
            /// <param name="poi">POI to check</param>
            /// <param name="cameraPosition">Position from which to check.</param>
            /// <param name="angle">Angle within the POI must fall from the camera position's POV, defaults to ClusterAngle.</param>
            /// <returns>True if it is within the radius, false otherwise.</returns>
            public bool IsPOINearby(PointOfInterest poi, Vector3D cameraPosition, double angle = ClusterAngle)
            {
                // Compute POI distance to camera
                Vector3D deltaPos = (cameraPosition - WorldPosition);
                double distance = deltaPos.Length();

                if (distance < ClusterNearDistance)
                {
                    /*
                     * TODO: Come up with a better way to improve screen space clustering
                     * This algorithm kinda works, but there are annoying edge cases
                     * For example, if you have a base, and at 900m you have a fighter, aligning both will merge them into a POI
                     * 
                     * Using the next bit of code achieves the best results with a ClusterNearDistance of 4000m instead
                     * 
                    // Use screen space clustering for POIs that are close to the camera
                    Vector2 myScreenPosition;
                    bool myIsBehind;
                    Vector2 theirScreenPosition;
                    bool theirIsBehind;

                    if (!TryComputeScreenPoint(WorldPosition, out myScreenPosition, out myIsBehind))
                        return false;
                    if (!TryComputeScreenPoint(poi.WorldPosition, out theirScreenPosition, out theirIsBehind))
                        return false;

                    if (myIsBehind != theirIsBehind) return false;

                    // Check for sufficient screen position overlap
                    Vector2 screenDist = (myScreenPosition - theirScreenPosition);
                    float sqrDist = screenDist.LengthSquared();

                    double acceptedDistance = 0.034;
                    // Enemies cluster a little later because this is more urgent information
                    if (this.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies && poi.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies)
                        acceptedDistance = 0.012;

                    if (sqrDist > acceptedDistance * acceptedDistance)
                        return false;

                    if (!IsRelationHostile(Relationship, poi.Relationship))
                        return true;*/

                    // No clustering possibility
                    return false;
                }

                // Compute max distance to make a ClusterAngle degree difference from the camera's PoV
                // The angle scales a bit based on distance, but will never be larger than twice the specified degrees
                double ratio = (distance - ClusterNearDistance) / ClusterScaleDistance;
                ratio = Math.Min(Math.Max(0, ratio), 1);
                double scaledAngle = angle + angle * ratio;

                double maxDistance = Math.Sin(scaledAngle * (Math.PI / 180f)) * distance;
                double maxDistanceSqr = maxDistance * maxDistance;

                // Compute POI distance to POI distance
                Vector3D deltaPOI = (poi.WorldPosition - WorldPosition);
                double poiDistance = deltaPOI.LengthSquared();

                // If poiDistance is within the max distance for ClusterAngle degree angle, then it can be considered clustered.
                bool isInRange = (poiDistance <= maxDistanceSqr);
                return isInRange;
            }

            /// <summary>
            /// Retrieves font and colouring information for a relationship.
            /// </summary>
            /// <param name="relationship"></param>
            /// <param name="color"></param>
            /// <param name="fontColor"></param>
            /// <param name="font"></param>
            public void GetColorAndFontForRelationship(MyRelationsBetweenPlayerAndBlock relationship, out Color color, out Color fontColor, out MyFontEnum font)
            {
                color = Color.White;
                fontColor = Color.White;
                font = MyFontEnum.White;
                switch (relationship)
                {
                    case MyRelationsBetweenPlayerAndBlock.Owner:
                        color = new Color(117, 201, 241);
                        fontColor = new Color(117, 201, 241);
                        font = MyFontEnum.Blue;
                        break;

                    case MyRelationsBetweenPlayerAndBlock.FactionShare:
                        color = new Color(101, 178, 90);
                        font = MyFontEnum.Green;
                        break;

                    // Neutral and unowned are the same colour
                    case MyRelationsBetweenPlayerAndBlock.Neutral:
                    case MyRelationsBetweenPlayerAndBlock.NoOwnership:
                        break;

                    case MyRelationsBetweenPlayerAndBlock.Enemies:
                        color = new Color(227, 62, 63);
                        font = MyFontEnum.Red;
                        break;
                }
            }

            /// <summary>
            /// Returns the POI color and font information.
            /// </summary>
            /// <param name="poiColor">The colour of the POI.</param>
            /// <param name="fontColor">The colour that should be used with this font.</param>
            /// <param name="font">The font to be used for this POI.</param>
            public void GetPOIColorAndFontInformation(out Color poiColor, out Color fontColor, out MyFontEnum font)
            {
                poiColor = Color.White;
                fontColor = Color.White;
                font = MyFontEnum.White;

                GetColorAndFontForRelationship(Relationship, out poiColor, out fontColor, out font);

                // Colour overrides for specific types
                switch (POIType)
                {
                    case PointOfInterestType.Ore:       // Ore markers are white
                    case PointOfInterestType.Unknown:   // Unknowns should be white
                    case PointOfInterestType.Group:     // Groups are white too
                        poiColor = Color.White;
                        font = MyFontEnum.White;
                        fontColor = Color.White;
                        break;

                    // GPS is always blue
                    case PointOfInterestType.GPS:
                        poiColor = new Color(117, 201, 241);
                        fontColor = new Color(117, 201, 241);
                        font = MyFontEnum.Blue;
                        break;
                }
            }

            /// <summary>
            /// Draws this POI
            /// </summary>
            /// <param name="renderer">MyHudMarkerRender instance that performs the rendering.</param>
            public void Draw(MyHudMarkerRender renderer)
            {
                Vector2 screenPosition = Vector2.Zero;
                bool isBehind = false;

                // If it's not possible to compute a screen position, don't draw anything
                //ProfilerShort.Begin("Compute screen position");
                if (!TryComputeScreenPoint(WorldPosition, out screenPosition, out isBehind))
                {
                    //ProfilerShort.End();
                    return;
                }

                Vector2 hudSize = MyGuiManager.GetHudSize();
                Vector2 center = MyGuiManager.GetHudSizeHalf();

                screenPosition *= hudSize;

                // Obtain POI font and colour information
                //ProfilerShort.BeginNextBlock("Obtain style");
                Color markerColor = Color.White;
                Color fontColor = Color.White;
                MyFontEnum font = MyFontEnum.White;
                GetPOIColorAndFontInformation(out markerColor, out fontColor, out font);

                //  This will bound the rectangle in circle, although it isn't real circle because we work in [0,1] dimensions, 
                //  but number of horizontal pixels is bigger, so at the end it's more elypse
                //  It must be done when point is out of circle or behind the camera
                //ProfilerShort.BeginNextBlock("Bind marker");
                Vector2 direction = screenPosition - center;
                Vector3D transformedPoint = Vector3D.Transform(WorldPosition, MySector.MainCamera.ViewMatrix);

                float minVal = 0.04f;
                float overshootVal = 0.5f - minVal;
                
                //ProfilerShort.BeginNextBlock("Draw direction or marker");
                if ((screenPosition.X < minVal || screenPosition.X > hudSize.X - minVal || screenPosition.Y < minVal || screenPosition.Y > hudSize.Y - minVal || transformedPoint.Z > 0))
                {
                    //ProfilerShort.Begin("Draw direction");
                    Vector2 normalizedDir = Vector2.Normalize(direction);
                    screenPosition = center + center * normalizedDir * 0.77f; // 0.77f clamps the arrows to the screen without overlapping the toolbar
                    direction = screenPosition - center;

                    if (direction.LengthSquared() > MyMathConstants.EPSILON_SQUARED)
                    {
                        direction.Normalize();
                    }
                    else
                    {
                        direction = new Vector2(1f, 0f);
                    }

                    // Draw directional arrow, offset by direction
                    renderer.AddTexturedQuad(MyHudTexturesEnum.DirectionIndicator, screenPosition, direction,
                           Color.White, MyHudConstants.HUD_DIRECTION_INDICATOR_SIZE * 0.8f, MyHudConstants.HUD_DIRECTION_INDICATOR_SIZE * 0.8f);

                    screenPosition -= direction * MyHudConstants.HUD_DIRECTION_INDICATOR_SIZE * 2.0f;
                    //ProfilerShort.End();
                }
                else
                {
                    //ProfilerShort.Begin("Draw marker box");
                    // Draw [ ] box
                    renderer.AddTexturedQuad(MyHudTexturesEnum.Target_neutral, screenPosition, -Vector2.UnitY, markerColor, MyHudConstants.HUD_DIRECTION_INDICATOR_SIZE, MyHudConstants.HUD_DIRECTION_INDICATOR_SIZE);
                    //ProfilerShort.End();
                }

                float fullFocus = 0.04f;
                float focusNoText = 0.08f;
                float focusEdge = 0.3f;

                int edgeState = 0;

                float alphaValue = 1;
                float alphaValueSubtext = 1;

                float directionLength = direction.Length();
                if (directionLength <= fullFocus)
                {
                    alphaValue = 1;
                    alphaValueSubtext = 1;
                    edgeState = 0;
                }
                else if (directionLength > fullFocus && directionLength < focusNoText)
                {
                    float fadeSize = focusEdge - fullFocus;
                    alphaValue = 1 - ((directionLength - fullFocus) / fadeSize);
                    alphaValue = alphaValue * alphaValue;

                    fadeSize = focusNoText - fullFocus;
                    alphaValueSubtext = 1 - ((directionLength - fullFocus) / fadeSize);
                    alphaValueSubtext = alphaValueSubtext * alphaValueSubtext;

                    edgeState = 1;
                }
                else if (directionLength >= focusNoText && directionLength < focusEdge)
                {
                    float fadeSize = focusEdge - fullFocus;
                    alphaValue = 1 - ((directionLength - fullFocus) / fadeSize);
                    alphaValue = alphaValue * alphaValue;

                    fadeSize = focusEdge - focusNoText;
                    alphaValueSubtext = 1 - ((directionLength - focusNoText) / fadeSize);
                    alphaValueSubtext = alphaValueSubtext * alphaValueSubtext;
                    
                    edgeState = 2;
                }
                else
                {
                    alphaValue = 0;
                    alphaValueSubtext = 0;
                    edgeState = 2;
                }

                alphaValue = MyMath.Clamp(alphaValue, 0, 1);

                // Render name, but only if visible
                //ProfilerShort.BeginNextBlock("Draw name");
                Vector2 textLabelOffset = new Vector2(0, 24f / MyGuiManager.GetFullscreenRectangle().Width);
                if (alphaValue > float.Epsilon)
                {
                    MyHudText objectName = renderer.m_hudScreen.AllocateText();
                    if (objectName != null)
                    {
                        fontColor.A = (byte)(255f * alphaValue);
                        objectName.Start(font, screenPosition - textLabelOffset, fontColor, 0.7f, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                        objectName.Append(this.Text);
                    }
                }

                MyHudText distanceBuilder = null;
                if (POIType != PointOfInterestType.Group)
                {
                    // Draw icon
                    //ProfilerShort.BeginNextBlock("Draw regular icon");
                    byte oldA = markerColor.A;
                    markerColor.A = (byte)(255);
                    DrawIcon(renderer, POIType, Relationship, screenPosition, markerColor);
                    markerColor.A = oldA;

                    // Render distance, groups render their distance differently
                    //ProfilerShort.BeginNextBlock("Draw regular distance");
                    distanceBuilder = renderer.m_hudScreen.AllocateText();
                    if (distanceBuilder != null)
                    {
                        StringBuilder stringBuilder = new StringBuilder();
                        AppendDistance(stringBuilder, Distance);

                        fontColor.A = 255;
                        distanceBuilder.Start(font, screenPosition + textLabelOffset * (0.7f + 0.3f * alphaValue), fontColor, 0.5f + 0.2f * alphaValue, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                        distanceBuilder.Append(stringBuilder);
                    }

                    // Non-group POIs end here
                    //ProfilerShort.End();
                    return;
                }

                var significantPOIs = GetSignificantGroupPOIs();

                //ProfilerShort.BeginNextBlock("Compute group offsets");
                Vector2[] offsetsSquare = { new Vector2(-6, -6), new Vector2(6, -6), new Vector2(-6, 6), new Vector2(6, 6), new Vector2(0, 20) };
                Vector2[] offsetsVertical = { new Vector2(16, -4), new Vector2(16, 12), new Vector2(16, 28), new Vector2(16, 44), new Vector2(16, 60) };

                for (int i = 0; i < offsetsSquare.Length; i++)
                {
                    float offsetVal = edgeState < 2 ? 1 : alphaValueSubtext;

                    offsetsSquare[i].X = (offsetsSquare[i].X + 22 * offsetVal) / MyGuiManager.GetFullscreenRectangle().Width;
                    offsetsSquare[i].Y = (offsetsSquare[i].Y / MyGuiManager.GetFullscreenRectangle().Height) * MyGuiManager.GetHudSize().Y;

                    offsetsVertical[i].X = offsetsVertical[i].X / MyGuiManager.GetFullscreenRectangle().Width;
                    offsetsVertical[i].Y = (offsetsVertical[i].Y / MyGuiManager.GetFullscreenRectangle().Height) * MyGuiManager.GetHudSize().Y;
                }

                int index = 0;
                MyRelationsBetweenPlayerAndBlock[] relations = { MyRelationsBetweenPlayerAndBlock.Owner, MyRelationsBetweenPlayerAndBlock.FactionShare, MyRelationsBetweenPlayerAndBlock.Neutral, MyRelationsBetweenPlayerAndBlock.Enemies };
                //ProfilerShort.BeginNextBlock("Draw group elements");
                for (int i = 0; i < relations.Length; i++)
                {
                    MyRelationsBetweenPlayerAndBlock relationship = relations[i];
                    if (!significantPOIs.ContainsKey(relationship)) continue;
                    
                    PointOfInterest poi = significantPOIs[relationship];
                    if (poi == null) continue;

                    GetColorAndFontForRelationship(relationship, out markerColor, out fontColor, out font);

                    float offsetVal = edgeState == 0 ? 1 : alphaValueSubtext;
                    if (edgeState >= 2)
                        offsetVal = 0;
                    Vector2 offset = Vector2.Lerp(offsetsSquare[index], offsetsVertical[index], offsetVal);

                    string icon = GetIconForRelationship(relationship);
                    DrawIcon(renderer, icon, screenPosition + offset, markerColor, 0.75f);
                    if (IsPoiAtHighAlert(poi))
                        DrawIcon(renderer, "Textures\\HUD\\marker_alert.png", screenPosition + offset, Color.White, 0.75f);

                    MyHudText objectName = renderer.m_hudScreen.AllocateText();
                    if (objectName != null)
                    {
                        float alpha = 1;
                        if (edgeState == 1)
                            alpha = alphaValueSubtext;
                        else if (edgeState > 1)
                            alpha = 0;

                        fontColor.A = (byte)(255f * alpha);
                        Vector2 horizontalOffset = new Vector2(8f / MyGuiManager.GetFullscreenRectangle().Width, 0);
                        objectName.Start(font, screenPosition + offset + horizontalOffset, fontColor, 0.55f, MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
                        objectName.Append(poi.Text);
                    }

                    index++;
                }

                //ProfilerShort.BeginNextBlock("Group GetPOIColorAndFontInformation");
                GetPOIColorAndFontInformation(out markerColor, out fontColor, out font);

                // Render distance
                //ProfilerShort.BeginNextBlock("Compute group distance label");
                float distanceOffset = edgeState == 0 ? 1 : alphaValueSubtext;
                if (edgeState >= 2)
                    distanceOffset = 0;
                Vector2 distancePos = Vector2.Lerp(offsetsSquare[4], offsetsVertical[index], distanceOffset);
                Vector2 horizontalDistanceOffset = Vector2.Lerp(Vector2.Zero, new Vector2(24f / MyGuiManager.GetFullscreenRectangle().Width, 0), distanceOffset);

                //ProfilerShort.BeginNextBlock("Draw group distance");
                distanceBuilder = renderer.m_hudScreen.AllocateText();
                if (distanceBuilder != null)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    AppendDistance(stringBuilder, Distance);

                    fontColor.A = 255;
                    distanceBuilder.Start(font, screenPosition + distancePos + horizontalDistanceOffset, fontColor, 0.5f + 0.2f * alphaValue, MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER);
                    distanceBuilder.Append(stringBuilder);
                }

                //ProfilerShort.End();
            }

            /// <summary>
            /// Returns the most significant POI for each relationship type within the group.
            /// </summary>
            /// <returns></returns>
            private Dictionary<MyRelationsBetweenPlayerAndBlock, PointOfInterest> GetSignificantGroupPOIs()
            {
                Dictionary<MyRelationsBetweenPlayerAndBlock, PointOfInterest> pois = new Dictionary<MyRelationsBetweenPlayerAndBlock, PointOfInterest>();

                for (int i = 0; i < m_group.Count; i++)
                {
                    PointOfInterest poi = m_group[i];

                    // No ownership is considered identical to neutral
                    MyRelationsBetweenPlayerAndBlock relationship = poi.Relationship;
                    if (relationship == MyRelationsBetweenPlayerAndBlock.NoOwnership)
                        relationship = MyRelationsBetweenPlayerAndBlock.Neutral;

                    if (pois.ContainsKey(relationship))
                    {
                        int poiComparisonResult = ComparePointOfInterest(poi, pois[relationship]);
                        if (poiComparisonResult > 0)
                            pois[relationship] = poi;
                    }
                    else
                    {
                        pois[relationship] = poi;
                    }
                }

                return pois;
            }

            private bool IsRelationHostile(MyRelationsBetweenPlayerAndBlock relationshipA, MyRelationsBetweenPlayerAndBlock relationshipB)
            {
                if (relationshipA == MyRelationsBetweenPlayerAndBlock.Owner || relationshipA == MyRelationsBetweenPlayerAndBlock.FactionShare)
                {
                    return (relationshipB == MyRelationsBetweenPlayerAndBlock.Enemies);
                }
                if (relationshipB == MyRelationsBetweenPlayerAndBlock.Owner || relationshipB == MyRelationsBetweenPlayerAndBlock.FactionShare)
                {
                    return (relationshipA == MyRelationsBetweenPlayerAndBlock.Enemies);
                }
                return false;
            }

            /// <summary>
            /// Checks if this POI has any kind of hostile activity nearby, by comparing it against the other elements in the group.
            /// </summary>
            private bool IsPoiAtHighAlert(PointOfInterest poi)
            {
                // Neutral parties are never at high alert
                if (poi.Relationship == MyRelationsBetweenPlayerAndBlock.Neutral) return false;

                foreach (var otherPOI in m_group)
                {
                    if (IsRelationHostile(poi.Relationship, otherPOI.Relationship))
                    {
                        Vector3 deltaPos = otherPOI.WorldPosition - poi.WorldPosition;
                        if (deltaPos.LengthSquared() < 1000000)
                            return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Checks if the current POI is a grid.
            /// </summary>
            /// <returns>True if it's a grid, false otherwise.</returns>
            private bool IsGrid()
            {
                return (POIType == PointOfInterestType.SmallEntity || POIType == PointOfInterestType.LargeEntity || POIType == PointOfInterestType.StaticEntity);
            }

            /// <summary>
            /// Draws an icon for the POI
            /// </summary>
            private static void DrawIcon(MyHudMarkerRender renderer, PointOfInterestType poiType, MyRelationsBetweenPlayerAndBlock relationship, Vector2 screenPosition, Color markerColor, float sizeScale = 1)
            {
                MyHudTexturesEnum centerIcon = MyHudTexturesEnum.corner;
                string centerIconSprite = string.Empty;
                Vector2 iconSize = new Vector2(12, 12);
                switch (poiType)
                {
                    default:
                    // Groups don't have an icon
                    case PointOfInterestType.Group:
                        return;

                    case PointOfInterestType.Hack:
                        centerIcon = MyHudTexturesEnum.hit_confirmation;
                        break;

                    case PointOfInterestType.Target:
                        centerIcon = MyHudTexturesEnum.TargetTurret;
                        break;

                    case PointOfInterestType.Ore:
                        centerIcon = MyHudTexturesEnum.HudOre;
                        break;

                    case PointOfInterestType.Character:
                    case PointOfInterestType.SmallEntity:
                    case PointOfInterestType.LargeEntity:
                    case PointOfInterestType.StaticEntity:
                    case PointOfInterestType.Unknown:
                    case PointOfInterestType.UnknownEntity:
                    case PointOfInterestType.GPS:
                        {
                            string icon = GetIconForRelationship(relationship);
                            DrawIcon(renderer, icon, screenPosition, markerColor);
                            return;
                        }
                }

                // Draw icon
                if (!string.IsNullOrWhiteSpace(centerIconSprite))
                {
                    iconSize *= sizeScale;
                    renderer.AddTexturedQuad(centerIconSprite, screenPosition, -Vector2.UnitY, markerColor, iconSize.X, iconSize.Y);
                }
                else
                {
                    float indicatorSize = MyHudConstants.HUD_DIRECTION_INDICATOR_SIZE * 0.8f * sizeScale;
                    renderer.AddTexturedQuad(centerIcon, screenPosition, -Vector2.UnitY, markerColor, indicatorSize, indicatorSize);
                }
            }

            /// <summary>
            /// Returns the icon path for the marker images for each relationship.
            /// </summary>
            public static string GetIconForRelationship(MyRelationsBetweenPlayerAndBlock relationship)
            {
                string centerIconSprite = string.Empty;
                switch (relationship)
                {
                    case MyRelationsBetweenPlayerAndBlock.Owner:
                        centerIconSprite = "Textures\\HUD\\marker_self.png";
                        break;
                    case MyRelationsBetweenPlayerAndBlock.FactionShare:
                        centerIconSprite = "Textures\\HUD\\marker_friendly.png";
                        break;
                    case MyRelationsBetweenPlayerAndBlock.NoOwnership:
                    case MyRelationsBetweenPlayerAndBlock.Neutral:
                        centerIconSprite = "Textures\\HUD\\marker_neutral.png";
                        break;
                    case MyRelationsBetweenPlayerAndBlock.Enemies:
                        centerIconSprite = "Textures\\HUD\\marker_enemy.png";
                        break;
                }
                return centerIconSprite;
            }

            /// <summary>
            /// Draws a texture based icon
            /// </summary>
            private static void DrawIcon(MyHudMarkerRender renderer, string centerIconSprite, Vector2 screenPosition, Color markerColor, float sizeScale = 1)
            {
                Vector2 iconSize = new Vector2(8, 8);

                // Draw icon
                iconSize *= sizeScale;
                renderer.AddTexturedQuad(centerIconSprite, screenPosition, -Vector2.UnitY, markerColor, iconSize.X, iconSize.Y);
            }

            /// <summary>
            /// Tries to compute the screenpoint for this POI from the main camera's PoV. May fail if the projection is invalid.
            /// projectedPoint2D will be set to Vector2.Zero if it was not possible to project.
            /// </summary>
            /// <param name="worldPosition">The world position to project to the screen.</param>
            /// <param name="projectedPoint2D">The screen position [-1, 1] by [-1, 1]</param>
            /// <param name="isBehind">Whether or not the position is behind the camera.</param>
            /// <param name="distance">Distance to the camera.</param>
            /// <returns>True if it could project, false otherwise.</returns>
            public static bool TryComputeScreenPoint(Vector3D worldPosition, out Vector2 projectedPoint2D, out bool isBehind)
            {
                //  Transform point to camera space, so Z = -1 is always forward and then do projective transformation
                Vector3D transformedPoint = Vector3D.Transform(worldPosition, MySector.MainCamera.ViewMatrix);
                Vector4D projectedPoint = Vector4D.Transform(transformedPoint, MySector.MainCamera.ProjectionMatrix);

                //  If point is behind camera we swap X and Y (this is mirror-like transformation)
                if (transformedPoint.Z > 0)
                {
                    projectedPoint.X *= -1;
                    projectedPoint.Y *= -1;
                }

                if (projectedPoint.W == 0)
                {
                    projectedPoint2D = Vector2.Zero;
                    isBehind = false;
                    return false;
                }

                //  Calculate centered coordinates in range <0..1>
                projectedPoint2D = new Vector2((float)(projectedPoint.X / projectedPoint.W / 2.0f) + 0.5f, (float)(-projectedPoint.Y / projectedPoint.W) / 2.0f + 0.5f);
                if (MyVideoSettingsManager.IsTripleHead())
                {
                    projectedPoint2D.X = (projectedPoint2D.X - (1.0f / 3.0f)) / (1.0f / 3.0f);
                }

                // Compute isBehind
                Vector3D viewDirection = worldPosition - MySector.MainCamera.WorldMatrix.Translation;
                viewDirection.Normalize();
                double dotProduct = Vector3D.Dot(MySector.MainCamera.ForwardVector, viewDirection);
                isBehind = (dotProduct < 0);

                return true;
            }

            /// <summary>
            /// Compares two POIs according to some pre-defined metrics
            /// </summary>
            private int ComparePointOfInterest(PointOfInterest poiA, PointOfInterest poiB)
            {
                // High alert POIs take priority
                bool highAlertA = IsPoiAtHighAlert(poiA);
                bool highAlertB = IsPoiAtHighAlert(poiB);
                int alertComparison = highAlertA.CompareTo(highAlertB);
                if (alertComparison != 0) return alertComparison;

                // Compare POI types
                if (poiA.POIType >= PointOfInterestType.UnknownEntity && poiB.POIType >= PointOfInterestType.UnknownEntity)
                {
                    int poiTypeComparison = poiA.POIType.CompareTo(poiB.POIType);
                    if (poiTypeComparison != 0)
                        return poiTypeComparison;
                }

                // If we get here, POI Type is equal or POI is considered a mostly irrelevant point

                // If both POIs are grids, return the largest grid size
                if (poiA.IsGrid() && poiB.IsGrid())
                {
                    MyCubeBlock blockA = poiA.Entity as MyCubeBlock;
                    MyCubeBlock blockB = poiB.Entity as MyCubeBlock;

                    if (blockA != null && blockB != null)
                    {
                        int gridSizeComparison = blockA.CubeGrid.BlocksCount.CompareTo(blockB.CubeGrid.BlocksCount);
                        if (gridSizeComparison != 0)
                            return gridSizeComparison;
                    }
                }

                // Closer by is more important than further away
                return poiA.Distance.CompareTo(poiB.Distance);
            }
        }

        /// <summary>
        /// Adds a generic POI, styled like a GPS coordinate.
        /// Currently only used to draw a center-of-the-world marker.
        /// </summary>
        public void AddPOI(Vector3D worldPosition, StringBuilder name, MyRelationsBetweenPlayerAndBlock relationship)
        {
            PointOfInterest poi = m_pointOfInterestPool.Allocate();
            m_pointsOfInterest.Add(poi);
            poi.Reset();
            poi.SetState(worldPosition, PointOfInterest.PointOfInterestType.GPS, relationship);
            poi.SetText(name);
        }

        public void AddEntity(MyEntity entity, MyRelationsBetweenPlayerAndBlock relationship, StringBuilder entityName)
        {
            if (entity == null) return;

            Vector3D worldPosition = entity.PositionComp.GetPosition();

            PointOfInterest.PointOfInterestType poiType = PointOfInterest.PointOfInterestType.UnknownEntity;
            if (entity is Sandbox.Game.Entities.Character.MyCharacter)
            {
                // Don't add marker for own character
                if (entity == MySession.Static.LocalCharacter)
                    return;

                poiType = PointOfInterest.PointOfInterestType.Character;
            }
            else
            {
                MyCubeBlock cubeBlock = entity as MyCubeBlock;
                if (cubeBlock != null && cubeBlock.CubeGrid != null)
                {
                    if (cubeBlock.CubeGrid.GridSizeEnum == MyCubeSize.Small)
                    {
                        poiType = PointOfInterest.PointOfInterestType.SmallEntity;
                    }
                    else
                    {
                        poiType = cubeBlock.CubeGrid.IsStatic ? PointOfInterest.PointOfInterestType.StaticEntity : PointOfInterest.PointOfInterestType.LargeEntity;
                    }
                }
            }

            PointOfInterest poi = m_pointOfInterestPool.Allocate();
            m_pointsOfInterest.Add(poi);
            poi.Reset();
            poi.SetState(worldPosition, poiType, relationship);
            poi.SetEntity(entity);
            poi.SetText(entityName);
        }

        public void AddGPS(Vector3D worldPosition, string name)
        {
            PointOfInterest poi = m_pointOfInterestPool.Allocate();
            m_pointsOfInterest.Add(poi);
            poi.Reset();
            poi.SetState(worldPosition, PointOfInterest.PointOfInterestType.GPS, MyRelationsBetweenPlayerAndBlock.Owner);
            poi.SetText(name);
        }

        public void AddButtonMarker(Vector3D worldPosition, string name)
        {
            PointOfInterest poi = m_pointOfInterestPool.Allocate();
            m_pointsOfInterest.Add(poi);
            poi.Reset();
            poi.SetState(worldPosition, PointOfInterest.PointOfInterestType.GPS, MyRelationsBetweenPlayerAndBlock.Owner);
            poi.SetText(name);
        }

        public void AddOre(Vector3D worldPosition, string name)
        {
            PointOfInterest poi = m_pointOfInterestPool.Allocate();
            m_pointsOfInterest.Add(poi);
            poi.Reset();
            poi.SetState(worldPosition, PointOfInterest.PointOfInterestType.Ore, MyRelationsBetweenPlayerAndBlock.NoOwnership);
            poi.SetText(name);
        }

        public void AddTarget(Vector3D worldPosition)
        {
            PointOfInterest poi = m_pointOfInterestPool.Allocate();
            m_pointsOfInterest.Add(poi);
            poi.Reset();
            poi.SetState(worldPosition, PointOfInterest.PointOfInterestType.Target, MyRelationsBetweenPlayerAndBlock.Enemies);
        }

        public void AddHacking(Vector3D worldPosition, StringBuilder name)
        {
            PointOfInterest poi = m_pointOfInterestPool.Allocate();
            m_pointsOfInterest.Add(poi);
            poi.Reset();
            poi.SetState(worldPosition, PointOfInterest.PointOfInterestType.Hack, MyRelationsBetweenPlayerAndBlock.Owner);
            poi.SetText(name);
        }

        /// <summary>
        /// Appends the distance in meters, kilometers, light seconds or light years to the string builder.
        /// Rounded to 2 decimals, i.e. 12.34 meters.
        /// </summary>
        /// <param name="stringBuilder">The string builder to be appended to.</param>
        /// <param name="distance">The distance in meters to be appended.</param>
        public static void AppendDistance(StringBuilder stringBuilder, double distance)
        {
            if (stringBuilder == null) return;

            distance = Math.Abs(distance);

            if (distance > LY_METRES)
            {
                stringBuilder.Append(Math.Round(distance / LY_METRES, 2).ToString());
                stringBuilder.Append("ly");
            }
            else if (distance > LS_METRES)
            {
                stringBuilder.Append(Math.Round(distance / LS_METRES, 2).ToString());
                stringBuilder.Append("ls");
            }
            else if (distance > 1000)
            {
                stringBuilder.Append(Math.Round(distance / 1000, 2).ToString());
                stringBuilder.Append("km");
            }
            else
            {
                stringBuilder.Append(Math.Round(distance, 2).ToString());
                stringBuilder.Append("m");
            }
        }

        public void Draw()
        {
            Vector3D cameraPosition = MySector.MainCamera.Position;

            ProfilerShort.Begin("Clustering");
            List<PointOfInterest> finalPOIs = new List<PointOfInterest>();
            // N*N scan to cluster POIs
            for (int i = 0; i<m_pointsOfInterest.Count; i++)
            {
                PointOfInterest poi = m_pointsOfInterest[i];
                PointOfInterest groupPOI = null;

                if (poi.POIType != PointOfInterest.PointOfInterestType.Target)
                {
                    for (int j = i + 1; j < m_pointsOfInterest.Count; )
                    {
                        PointOfInterest poi2 = m_pointsOfInterest[j];
                        if (poi2 == poi)
                        {
                            j++;
                            continue;
                        }

                        if (poi2.POIType == PointOfInterest.PointOfInterestType.Target)
                        {
                            j++;
                            continue;
                        }

                        if (poi.IsPOINearby(poi2, cameraPosition))
                        {
                            if (groupPOI == null)
                            {
                                groupPOI = m_pointOfInterestPool.Allocate();
                                groupPOI.Reset();
                                groupPOI.SetState(Vector3D.Zero, PointOfInterest.PointOfInterestType.Group, MyRelationsBetweenPlayerAndBlock.NoOwnership);
                                groupPOI.AddPOI(poi);
                            }

                            groupPOI.AddPOI(poi2);
                            m_pointsOfInterest.RemoveAt(j);
                        }
                        else
                        {
                            j++;
                        }
                    }
                }
                else
                {
                    // Compute POI distance to camera
                    Vector3D deltaPos = (cameraPosition - poi.WorldPosition);
                    double distance = deltaPos.Length();

                    // Ignore targets that are too far out to see
                    if (distance > PointOfInterest.MinimumTargetRange) continue;
                }

                if (groupPOI != null)
                {
                    finalPOIs.Add(groupPOI);
                }
                else
                {
                    finalPOIs.Add(poi);
                }
            }

            ProfilerShort.BeginNextBlock("Drawing");
            foreach (PointOfInterest poi in finalPOIs)
            {
                // Draw POI
                //ProfilerShort.Begin("Draw POI");
                poi.Draw(this);
                //ProfilerShort.End();

                // Return POI to pool
                if (poi.POIType == PointOfInterest.PointOfInterestType.Group)
                {
                    foreach (PointOfInterest child in poi.m_group)
                        m_pointOfInterestPool.Deallocate(child);
                }
                m_pointOfInterestPool.Deallocate(poi);
            }
            ProfilerShort.End();
            m_pointsOfInterest.Clear();
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

        /// <summary>
        /// Add textured quad with specified UP direction and width/height.
        /// </summary>
        protected void AddTexturedQuad(string texture, Vector2 position, Vector2 upVector, Color color, float halfWidth, float halfHeight)
        {
            position.X = MyGuiManager.GetFullscreenRectangle().Width * position.X;
            position.Y = MyGuiManager.GetFullscreenRectangle().Height * position.Y / MyGuiManager.GetHudSize().Y;

            RectangleF dest = new RectangleF(position.X - halfWidth, position.Y - halfHeight, halfWidth * 2, halfHeight * 2);
            Rectangle? source = null;

            VRageRender.MyRenderProxy.DrawSprite(texture, ref dest, false, ref source, color, 0,
                new Vector2(1, 0), ref Vector2.Zero, VRageRender.Graphics.SpriteEffects.None, 0);
        }

		static public float Normalize(float value)
		{
            return NormalizeLog(value, 0.1f, MyPerGameSettings.MaxAntennaDrawDistance);
		}

        static public float Denormalize(float value)
        {
            return DenormalizeLog(value, 0.1f, MyPerGameSettings.MaxAntennaDrawDistance);
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
