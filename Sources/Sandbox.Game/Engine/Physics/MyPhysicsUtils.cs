//using Havok;
//using Sandbox.Engine.Utils;
//using VRageMath;

//namespace Sandbox.Engine.Physics
//{
//    static class MyPhysicsUtils
//    {
//        public enum ConstraintWrapper
//        {
//            None,
//            Malleable,
//            Breakable
//        }
//        public struct WheelConstraintData
//        {
//            public Vector3 PosA;
//            public Vector3 PosB;
//            public Vector3 AxisA;
//            public Vector3 AxisB;
//            public Vector3 Suspension;//In B body space
//            public Vector3 Steering;///In B body space
//            public float Damping;
//            public float Strength;
//            public float LimitMin;
//            public float LimitMax;

//            internal void Transform(MyPhysicsBody bodyA, MyPhysicsBody bodyB)
//            {
//                if (bodyA.IsWelded)
//                {
//                    PosA = Vector3.Transform(PosA, bodyA.WeldInfo.Transform);
//                    AxisA = Vector3.TransformNormal(AxisA, bodyA.WeldInfo.Transform);
//                }
//                if (bodyB.IsWelded)
//                {
//                    PosB = Vector3.Transform(PosB, bodyB.WeldInfo.Transform);
//                    AxisB = Vector3.TransformNormal(AxisB, bodyB.WeldInfo.Transform);
//                    Suspension = Vector3.TransformNormal(Suspension, bodyB.WeldInfo.Transform);
//                    Steering = Vector3.TransformNormal(Steering, bodyB.WeldInfo.Transform);
//                }
//            }
            
//        }

//        public struct LimitedHingeConstraintData
//        {
//            public Vector3 PosA;
//            public Vector3 PosB;
//            public Vector3 AxisA;
//            public Vector3 AxisB;
//            public Vector3 AxisAPerp;
//            public Vector3 AxisBPerp;
//            public bool UseMotor;
//            public HkConstraintMotor OutHkMotor;

//            internal void Transform(MyPhysicsBody bodyA, MyPhysicsBody bodyB)
//            {
//                if (bodyA.IsWelded)
//                {
//                    PosA = Vector3.Transform(PosA, bodyA.WeldInfo.Transform);
//                    AxisA = Vector3.TransformNormal(AxisA, bodyA.WeldInfo.Transform);
//                    AxisAPerp = Vector3.TransformNormal(AxisAPerp, bodyA.WeldInfo.Transform);
//                }
//                if (bodyB.IsWelded)
//                {
//                    PosB = Vector3.Transform(PosB, bodyB.WeldInfo.Transform);
//                    AxisB = Vector3.TransformNormal(AxisB, bodyB.WeldInfo.Transform);
//                    AxisBPerp = Vector3.TransformNormal(AxisBPerp, bodyA.WeldInfo.Transform);
//                }
//            }
//        }

//        public struct FixedConstraintData
//        {
//            public Matrix PivotA;
//            public Matrix PivotB;
//            internal void Transform(MyPhysicsBody bodyA, MyPhysicsBody bodyB)
//            {
//                if (bodyA.IsWelded)
//                    PivotA = PivotA * bodyA.WeldInfo.Transform;
//                if (bodyB.IsWelded)
//                    PivotB = PivotB * bodyB.WeldInfo.Transform;
//            }
//        }

//        public struct WrapperConstraintData
//        {
//            public float Strength;
//            public bool ReapplyVelocityOnBreak;
//            public bool RemoveFromWorldOnBreak;
//        }

//        public static HkConstraint CreateConstraint(ref LimitedHingeConstraintData data, MyPhysicsBody bodyA, MyPhysicsBody bodyB, ConstraintWrapper wrapper = ConstraintWrapper.None, WrapperConstraintData wrapperData = default(WrapperConstraintData))
//        {
//            data.Transform(bodyA, bodyB);
//            HkConstraintData hkData = CreateConstraintData(ref data, bodyA, bodyB);
//            hkData = WrapData(hkData, wrapper, wrapperData);
//            return new HkConstraint(bodyA.RigidBody, bodyB.RigidBody, hkData);
//        }
//        public static HkConstraint CreateConstraint(ref WheelConstraintData data, MyPhysicsBody bodyA, MyPhysicsBody bodyB, ConstraintWrapper wrapper = ConstraintWrapper.None, WrapperConstraintData wrapperData = default(WrapperConstraintData))
//        {
//            data.Transform(bodyA, bodyB);
//            HkConstraintData hkData = CreateConstraintData(ref data, bodyA, bodyB);
//            hkData = WrapData(hkData, wrapper, wrapperData);
//            return new HkConstraint(bodyA.RigidBody, bodyB.RigidBody, hkData);
//        }

//        public static HkConstraint CreateConstraint(ref FixedConstraintData data, MyPhysicsBody bodyA, MyPhysicsBody bodyB, ConstraintWrapper wrapper = ConstraintWrapper.None, WrapperConstraintData wrapperData = default(WrapperConstraintData))
//        {
//            data.Transform(bodyA, bodyB);
//            HkConstraintData hkData = CreateConstraintData(ref data, bodyA, bodyB);
//            hkData = WrapData(hkData, wrapper, wrapperData);
//            return new HkConstraint(bodyA.RigidBody, bodyB.RigidBody, hkData);
//        }

//        private static HkConstraintData WrapData(HkConstraintData toWrap, ConstraintWrapper wrapper, WrapperConstraintData wrapperData)
//        {
//            switch(wrapper)
//            {
//                case ConstraintWrapper.Malleable:
//                    var malleable = new HkMalleableConstraintData();
//                    malleable.SetData(toWrap);
//                    malleable.BreachImpulse = wrapperData.Strength;
//                    return malleable;
//                case ConstraintWrapper.Breakable:
//                    var breakable = new HkBreakableConstraintData(toWrap);
//                    breakable.Threshold = wrapperData.Strength;
//                    breakable.ReapplyVelocityOnBreak = wrapperData.ReapplyVelocityOnBreak;
//                    breakable.RemoveFromWorldOnBrake = wrapperData.RemoveFromWorldOnBreak;
//                    return breakable;
//            }
//            return toWrap;
//        }

//        private static HkConstraintData CreateConstraintData(ref LimitedHingeConstraintData data, MyPhysicsBody bodyA, MyPhysicsBody bodyB)
//        {
//            var hkData = new HkLimitedHingeConstraintData();
//            if (data.UseMotor)
//            {
//                data.OutHkMotor = hkData.Motor = new HkVelocityConstraintMotor(1.0f, 1000000f);
//            }
//            hkData.SetSolvingMethod(HkSolvingMethod.MethodStabilized);
//            hkData.DisableLimits();

//            hkData.SetInBodySpace(ref data.PosA, ref data.PosB, ref data.AxisA, ref data.AxisB, ref data.AxisAPerp, ref data.AxisBPerp);
//            return hkData;
//        }

//        private static HkConstraintData CreateConstraintData(ref WheelConstraintData data, MyPhysicsBody bodyA, MyPhysicsBody bodyB)
//        {
//            HkWheelConstraintData hkData = new HkWheelConstraintData();
//            //empirical values because who knows what havoc sees behind this 
//            //docs say one value should mean same effect for 2 ton or 200 ton vehicle 
//            //but we have virtual mass blocks so real mass doesnt corespond to actual "weight" in game and varying gravity
//            hkData.SetSuspensionDamping(data.Damping);
//            hkData.SetSuspensionStrength(data.Strength);
//            //Min/MaxHeight also define the limits of the suspension and SuspensionTravel lowers this limit
//            hkData.SetSuspensionMinLimit(data.LimitMin);
//            hkData.SetSuspensionMaxLimit(data.LimitMax);
//            hkData.SetInBodySpace(ref data.PosA, ref data.PosB, ref data.AxisA, ref data.AxisB, ref data.Suspension, ref data.Steering);
//            return hkData;
//        }

//        private static HkConstraintData CreateConstraintData(ref FixedConstraintData data, MyPhysicsBody bodyA, MyPhysicsBody bodyB)
//        {
//            if (bodyA.IsWelded)
//                data.PivotA = data.PivotA * bodyA.WeldInfo.Transform;
//            if (bodyB.IsWelded)
//                data.PivotB = data.PivotB * bodyB.WeldInfo.Transform;

//            var hkData = new HkFixedConstraintData();
//            hkData.SetInertiaStabilizationFactor(1);

//            hkData.SetSolvingMethod(HkSolvingMethod.MethodStabilized);
//            hkData.SetInBodySpace(ref data.PivotA, ref data.PivotB);
//            return hkData;
//        }

//    }
//}
