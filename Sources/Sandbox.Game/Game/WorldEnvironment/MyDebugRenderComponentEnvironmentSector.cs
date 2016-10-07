using Sandbox.Game.Components;
using Sandbox.Game.Entities.Planet;
using VRage.ModAPI;

namespace Sandbox.Game.WorldEnvironment
{
    class MyDebugRenderComponentEnvironmentSector : MyDebugRenderComponent
    {
        public override void DebugDraw()
        {
            if (!MyPlanetEnvironmentSessionComponent.DebugDrawSectors) return;

            var sector = (MyEnvironmentSector)Entity;

            sector.DebugDraw();
        }

        public MyDebugRenderComponentEnvironmentSector(IMyEntity entity)
            : base(entity)
        {
        }
    }
}
