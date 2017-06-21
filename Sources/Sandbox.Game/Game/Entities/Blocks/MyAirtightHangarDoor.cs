using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using System.Text;
using VRage.Game.Entity;
using VRageMath;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_AirtightHangarDoor))]
    public class MyAirtightHangarDoor : MyAirtightDoorGeneric, ModAPI.IMyAirtightHangarDoor
    {

        protected override void UpdateDoorPosition()
        {
            if (this.CubeGrid.Physics == null)
                return;

            float opening = (m_currOpening - 1) * m_subparts.Count * m_subpartMovementDistance;
            float maxOpening = 0;
            foreach (var subpart in m_subparts)
            {
                maxOpening -= m_subpartMovementDistance;
                if (subpart != null && subpart.Physics != null)
                {
                    subpart.PositionComp.LocalMatrix = Matrix.CreateTranslation(new Vector3(0f, (opening < maxOpening ? maxOpening : opening), 0f));
                    if (subpart.Physics.LinearVelocity.Equals(CubeGrid.Physics.LinearVelocity, 0.01f) == false)
                    {
                        subpart.Physics.LinearVelocity = this.CubeGrid.Physics.LinearVelocity;
                    }

                    if (subpart.Physics.AngularVelocity.Equals(this.CubeGrid.Physics.AngularVelocity, 0.01f) == false)
                    {
                        subpart.Physics.AngularVelocity = this.CubeGrid.Physics.AngularVelocity;
                    }
                }
            }
        }

        protected override void FillSubparts()
        {
            int i = 1;
            StringBuilder partName = new StringBuilder();
            MyEntitySubpart foundPart;
            m_subparts.Clear();
            while (true)
            {
                partName.Clear().Append("HangarDoor_door").Append(i++);
                Subparts.TryGetValue(partName.ToString(), out foundPart);
                if (foundPart == null)
                    break;
                m_subparts.Add(foundPart);
            };
        }

    }
}
