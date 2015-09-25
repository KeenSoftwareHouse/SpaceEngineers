using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.GameSystems.Conveyors;
using Sandbox.Game.Multiplayer;
using VRageMath;

namespace Sandbox.Game.Entities.Cube
{
    [MyCubeBlockType(typeof(MyObjectBuilder_MotorAdvancedRotor))]
    public class MyMotorAdvancedRotor : MyMotorRotor, IMyConveyorEndpointBlock
    {
        private MyAttachableConveyorEndpoint m_conveyorEndpoint;

        public void InitializeConveyorEndpoint()
        {
            m_conveyorEndpoint = new MyAttachableConveyorEndpoint(this);
            AddDebugRenderComponent(new Components.MyDebugRenderComponentDrawConveyorEndpoint(m_conveyorEndpoint));
        }

        public IMyConveyorEndpoint ConveyorEndpoint
        {
            get { return m_conveyorEndpoint; }
        }
    }
}
