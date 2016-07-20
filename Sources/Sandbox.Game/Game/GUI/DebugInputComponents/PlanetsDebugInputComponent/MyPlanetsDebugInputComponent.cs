using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Gui
{
    public partial class MyPlanetsDebugInputComponent : MyMultiDebugInputComponent
    {
        public MyPlanetsDebugInputComponent()
        {
            m_components = new MyDebugComponent[]{
                new ShapeComponent(this),
                new InfoComponent(this),
                new SectorsComponent(this),
                new SectorTreeComponent(this),
                new MiscComponent(this) 
            };
        }

        #region Base
        private MyDebugComponent[] m_components;

        public override MyDebugComponent[] Components
        {
            get { return m_components; }
        }

        public override string GetName()
        {
            return "Planets";
        }

        public override void DrawInternal()
        {
            if (CameraPlanet != null)
                Text(Color.DarkOrange, "Current Planet: {0}", CameraPlanet.StorageName);
        }
        #endregion

        private List<MyVoxelBase> m_voxels = new List<MyVoxelBase>();

        public MyPlanet GetClosestContainingPlanet(Vector3D point)
        {
            m_voxels.Clear();
            BoundingBoxD b = new BoundingBoxD(point, point);
            MyGamePruningStructure.GetAllVoxelMapsInBox(ref b, m_voxels);

            double dist = double.PositiveInfinity;

            MyPlanet p = null;

            foreach (var v in m_voxels)
            {
                if (v is MyPlanet)
                {
                    var d = Vector3.Distance(v.WorldMatrix.Translation, point);
                    if (d < dist)
                    {
                        dist = d;
                        p = (MyPlanet)v;
                    }
                }
            }

            return p;
        }

        public override void Draw()
        {
            if(MySession.Static != null)
                base.Draw();
        }

        public override void Update100()
        {
            CameraPlanet = GetClosestContainingPlanet(MySector.MainCamera.Position);
            if (MySession.Static.LocalCharacter != null)
                CharacterPlanet = GetClosestContainingPlanet(MySession.Static.LocalCharacter.PositionComp.GetPosition());

            base.Update100();
        }

        #region Common

        public MyPlanet CameraPlanet;
        public MyPlanet CharacterPlanet;

        #endregion
    }
}
