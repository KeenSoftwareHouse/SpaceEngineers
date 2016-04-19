using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.GameSystems;
using Sandbox.Game.World;
using VRage.Game.Components;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRageMath;

namespace Sandbox.Game.EntityComponents
{
    //class used for detection of environment with air - important for realistic sounds
    [MyComponentBuilder(typeof(MyObjectBuilder_AtmosphereDetectorComponent))]
    public class MyAtmosphereDetectorComponent : MyEntityComponentBase
    {
        private enum AtmosphereStatus
        {
            Space,
            ShipOrStation,
            Atmosphere
        }

        private MyCharacter m_character = null;
        private bool m_localPlayer = true;
        private bool m_inAtmosphere = false;
        private AtmosphereStatus m_atmosphereStatus = AtmosphereStatus.Space;
        public bool InAtmosphere { get { return m_atmosphereStatus == AtmosphereStatus.Atmosphere; } }
        public bool InShipOrStation { get { return m_atmosphereStatus == AtmosphereStatus.ShipOrStation; } }

        public void InitComponent(bool onlyLocalPlayer, MyCharacter character)
        {
            m_localPlayer = onlyLocalPlayer;
            m_character = character;
        }


        public void UpdateAtmosphereStatus()
        {
            if (m_character != null && (m_localPlayer == false || (MySession.Static != null && m_character == MySession.Static.LocalCharacter)))
            {
                Vector3D pos = m_character.PositionComp.GetPosition();
                Vector3 gravity = MyGravityProviderSystem.CalculateNaturalGravityInPoint(pos);
                if (gravity.LengthSquared() > 0f)
                {
                    MyPlanet planet = MyGravityProviderSystem.GetNearestPlanet(pos);
                    float d = (float)Vector3D.DistanceSquared(planet.PositionComp.GetPosition(), pos);
                    if (planet != null && planet.HasAtmosphere && Vector3D.DistanceSquared(planet.PositionComp.GetPosition(), pos) < planet.AtmosphereRadius * planet.AtmosphereRadius)
                        m_atmosphereStatus = AtmosphereStatus.Atmosphere;//in atmosphere
                    else
                        m_atmosphereStatus = AtmosphereStatus.Space;
                }
                else
                {
                    m_atmosphereStatus = AtmosphereStatus.Space;
                }

                if (m_atmosphereStatus == AtmosphereStatus.Space)
                {
                    float oxygen = 0f;
                    if (m_character.OxygenComponent != null)
                    {
                        if (m_localPlayer)
                        {
                            if (MySession.Static.ControlledEntity is MyCharacter)
                            {
                                //in suit
                                oxygen = m_character.OxygenComponent.EnvironmentOxygenLevel;
                            }
                            else
                            {
                                //in cockpit
                                oxygen = m_character.OxygenComponent.OxygenLevelAtCharacterLocation;
                            }
                        }
                        else
                        {
                            oxygen = m_character.OxygenComponent.EnvironmentOxygenLevel;
                        }
                    }
                    if (oxygen > 0.1f)
                    {
                        m_atmosphereStatus = AtmosphereStatus.ShipOrStation;//in pressurized environment
                    }
                }
            }
        }


        public override string ComponentTypeDebugString
        {
            get { return "AtmosphereDetector"; }
        }
    }
}
