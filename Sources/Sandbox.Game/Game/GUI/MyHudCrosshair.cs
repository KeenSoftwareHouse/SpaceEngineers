#region Using

using Sandbox.Engine.Physics;
using Sandbox.Engine.Platform.VideoMode;
using Sandbox.Game.World;
using Sandbox.Graphics;
using System;
using VRage;
using VRageMath;


#endregion

namespace Sandbox.Game.Gui
{
    

    #region Crosshair

    public struct MyHudCrosshair
    {
        public Color Color;
        public Vector2 HalfSize;
        public Vector2 Position;
        public MyHudTexturesEnum TextureEnum;
        public Vector2 UpVector;

        public static Vector2 ScreenCenter
        {
            get { return new Vector2(0.5f, MyGuiManager.GetHudSizeHalf().Y); }
        }

        public bool Visible { get; private set; }

        public void Show(Action<MyHudCrosshair> propertiesInit)
        {
            SetDefaults();
            Visible = true;
            if (propertiesInit != null)
                propertiesInit(this);
        }

        public void Hide()
        {
            Visible = false;
        }

        private void SetDefaults()
        {
            Position = ScreenCenter;
            Visible = false;
            Color = MyHudConstants.HUD_COLOR_LIGHT;
            HalfSize = Vector2.One * 0.02f;
            TextureEnum = MyHudTexturesEnum.crosshair;
            UpVector = Vector2.UnitY;
        }


        public static bool GetTarget(Vector3D from, Vector3D to, ref Vector3D target)
        {
            ProfilerShort.Begin("GetTarget");
            ProfilerShort.Begin("CastRay");
            return false;
            Vector3 normal;
            var body = MyPhysics.CastRay(from, to, out target, out normal);
            ProfilerShort.End();

            //Profiler.Begin("Iterate");
            //int index = 0;
            //while (index < hits.Count && 
            //    (hits[index].Body == null
            //    || hits[index].Body.GetEntity() is Sandbox.Game.Weapons.MyAmmoBase
            //    || hits[index].Body.GetEntity() is Sandbox.Game.Entities.Debris.MyDebrisBase))
            //{
            //    index++;
            //}
            //Profiler.End();

           // Vector3 targetPoint = to;

            ProfilerShort.End();

            return body != null;
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
