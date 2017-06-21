using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities.Cube;
using System.Text;
using VRageMath;

using System.Diagnostics;
using System;
using VRage.Game;
using VRage.Utils;
using VRage.Game.Entity;

namespace Sandbox.Game.Entities
{
    [MyCubeBlockType(typeof(MyObjectBuilder_AirtightSlideDoor))]
    public class MyAirtightSlideDoor : MyAirtightDoorGeneric, ModAPI.IMyAirtightSlideDoor
    {
        private bool hadEnoughPower;

        public override void Init(MyObjectBuilder_CubeBlock builder, MyCubeGrid cubeGrid)
        {
            m_emissiveNames=new String[]{"Emissive"};
 	        base.Init(builder, cubeGrid);
        }

        protected override void UpdateDoorPosition()
        {
            if (this.CubeGrid.Physics == null)
                return;
            if (m_subparts.Count == 0)
                return;//construction model
            const float MAX_SHIFT = 1.75f;
            const float SUBPART_SIZE = 1.2f;
            const float TURN_SIZE = MAX_SHIFT - SUBPART_SIZE;
            float DIVIDE_POINT = (float)Math.Sqrt(SUBPART_SIZE * SUBPART_SIZE - TURN_SIZE * TURN_SIZE);//TODO precompute for final values from final model

            float shift = m_currOpening * MAX_SHIFT;


            float rotation = m_currOpening*MathHelper.PiOver2;
            if (shift<DIVIDE_POINT)
            {   //end of doors still moving straight along the edge
                rotation = (float)Math.Asin(shift / SUBPART_SIZE);
            }
            else
            {   //other end follows the turn. We are not computing position of the end to be on specified circle,
                //but rather some simple curve with correct derivations on both ends
                float x = (MAX_SHIFT-shift)/(MAX_SHIFT-DIVIDE_POINT);
                rotation = MathHelper.PiOver2 - x * x * (float)(MathHelper.PiOver2 - Math.Asin(DIVIDE_POINT / SUBPART_SIZE));
            }
            shift -= 1f;
            Matrix localMatrix;
            bool invertSign = true;
            foreach (var subpart in m_subparts)
            {
                if (subpart != null && subpart.Physics != null)
                {
                    Matrix.CreateRotationY((invertSign?rotation:-rotation), out localMatrix);
                    localMatrix.Translation = new Vector3((invertSign ? -SUBPART_SIZE : SUBPART_SIZE), 0f, shift);
                    subpart.PositionComp.LocalMatrix = localMatrix;
                    
                    if (subpart.Physics.LinearVelocity != this.CubeGrid.Physics.LinearVelocity)
                        subpart.Physics.LinearVelocity = this.CubeGrid.Physics.LinearVelocity;

                    if (subpart.Physics.AngularVelocity != this.CubeGrid.Physics.AngularVelocity)
                        subpart.Physics.AngularVelocity = this.CubeGrid.Physics.AngularVelocity;
                }
                invertSign = !invertSign;
            }
        }

        protected override void FillSubparts()
        {
            MyEntitySubpart foundPart;
            m_subparts.Clear();
            if (Subparts.TryGetValue("DoorLeft", out foundPart))
                m_subparts.Add(foundPart);
            if (Subparts.TryGetValue("DoorRight", out foundPart))
                m_subparts.Add(foundPart);
        }

        protected override void UpdateEmissivity(bool force)
        {
            if (IsWorking)
                SetEmissive(Color.Green,1,force);
            else
                if (IsEnoughPower())
                    SetEmissive(Color.Red, 1, force);
                else
                    SetEmissive(Color.Red, 0, force);
        }
        public override void UpdateBeforeSimulation100()
        {
            base.UpdateBeforeSimulation100();
            if (!IsWorking)
            {
                bool enoughPower = IsEnoughPower();
                if (enoughPower != hadEnoughPower)
                {
                    hadEnoughPower = enoughPower;
                    UpdateEmissivity(false);
                }
            }
        }
    }
}
