﻿#region Using


using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

#endregion

namespace Sandbox.Definitions
{
    [MyDefinitionType(typeof(MyObjectBuilder_HandItemDefinition))]
    public class MyHandItemDefinition : MyDefinitionBase
    {
        public Matrix LeftHand;
        public Matrix RightHand;

        public Matrix ItemLocation;
        public Matrix ItemLocation3rd;
        public Matrix ItemWalkingLocation;
        public Matrix ItemWalkingLocation3rd;

        public float BlendTime;

        public float XAmplitudeOffset;
        public float YAmplitudeOffset;
        public float ZAmplitudeOffset;

        public float XAmplitudeScale;
        public float YAmplitudeScale;
        public float ZAmplitudeScale;

        public float RunMultiplier;
        public float AmplitudeMultiplier3rd;

        public bool SimulateLeftHand = true;
        public bool SimulateRightHand = true;

        public string FingersAnimation;
        public Matrix ItemShootLocation;
        public Matrix ItemShootLocation3rd;
        public float ShootBlend;

        public Vector3 MuzzlePosition;

        public Vector3 ShootScatter;
        public float ScatterSpeed;

        public MyDefinitionId PhysicalItemId;

        public Vector4 LightColor;
        public float LightFalloff;
        public float LightRadius;
        public float LightGlareSize;
        public float LightIntensityLower;
        public float LightIntensityUpper;
        public float ShakeAmountTarget;
        public float ShakeAmountNoTarget;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            var ob = builder as MyObjectBuilder_HandItemDefinition;
            MyDebug.AssertDebug(ob != null);

            Id = builder.Id;

            LeftHand = Matrix.CreateFromQuaternion(Quaternion.Normalize(ob.LeftHandOrientation));
            LeftHand.Translation = ob.LeftHandPosition;

            RightHand = Matrix.CreateFromQuaternion(Quaternion.Normalize(ob.RightHandOrientation));
            RightHand.Translation = ob.RightHandPosition;

            ItemLocation = Matrix.CreateFromQuaternion(Quaternion.Normalize(ob.ItemOrientation));
            ItemLocation.Translation = ob.ItemPosition;

            ItemWalkingLocation = Matrix.CreateFromQuaternion(Quaternion.Normalize(ob.ItemWalkingOrientation));
            ItemWalkingLocation.Translation = ob.ItemWalkingPosition;

            BlendTime = ob.BlendTime;

            XAmplitudeOffset = ob.XAmplitudeOffset;
            YAmplitudeOffset = ob.YAmplitudeOffset;
            ZAmplitudeOffset = ob.ZAmplitudeOffset;

            XAmplitudeScale = ob.XAmplitudeScale;
            YAmplitudeScale = ob.YAmplitudeScale;
            ZAmplitudeScale = ob.ZAmplitudeScale;

            RunMultiplier = ob.RunMultiplier;

            ItemLocation3rd = Matrix.CreateFromQuaternion(Quaternion.Normalize(ob.ItemOrientation3rd));
            ItemLocation3rd.Translation = ob.ItemPosition3rd;

            ItemWalkingLocation3rd = Matrix.CreateFromQuaternion(Quaternion.Normalize(ob.ItemWalkingOrientation3rd));
            ItemWalkingLocation3rd.Translation = ob.ItemWalkingPosition3rd;

            AmplitudeMultiplier3rd = ob.AmplitudeMultiplier3rd;

            SimulateLeftHand = ob.SimulateLeftHand;
            SimulateRightHand = ob.SimulateRightHand;

            FingersAnimation = MyDefinitionManager.Static.GetAnimationDefinitionCompatibility(ob.FingersAnimation);

            ItemShootLocation = Matrix.CreateFromQuaternion(Quaternion.Normalize(ob.ItemShootOrientation));
            ItemShootLocation.Translation = ob.ItemShootPosition;
            ItemShootLocation3rd = Matrix.CreateFromQuaternion(Quaternion.Normalize(ob.ItemShootOrientation3rd));
            ItemShootLocation3rd.Translation = ob.ItemShootPosition3rd;
            ShootBlend = ob.ShootBlend;

            MuzzlePosition = ob.MuzzlePosition;

            ShootScatter = ob.ShootScatter;
            ScatterSpeed = ob.ScatterSpeed;
            PhysicalItemId = ob.PhysicalItemId;

            LightColor = ob.LightColor;
            LightFalloff = ob.LightFalloff;
            LightRadius = ob.LightRadius;
            LightGlareSize = ob.LightGlareSize;
            LightIntensityLower = ob.LightIntensityLower;
            LightIntensityUpper = ob.LightIntensityUpper;
            ShakeAmountTarget = ob.ShakeAmountTarget;
            ShakeAmountNoTarget = ob.ShakeAmountNoTarget;
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_HandItemDefinition>();
            
            ob.Id = Id;

            ob.LeftHandOrientation = Quaternion.CreateFromRotationMatrix(LeftHand);
            ob.LeftHandPosition = LeftHand.Translation;

            ob.RightHandOrientation = Quaternion.CreateFromRotationMatrix(RightHand);
            ob.RightHandPosition = RightHand.Translation;

            ob.ItemOrientation = Quaternion.CreateFromRotationMatrix(ItemLocation);
            ob.ItemPosition = ItemLocation.Translation;

            ob.ItemWalkingOrientation = Quaternion.CreateFromRotationMatrix(ItemWalkingLocation);
            ob.ItemWalkingPosition = ItemWalkingLocation.Translation;

            ob.BlendTime = BlendTime;

            ob.XAmplitudeOffset = XAmplitudeOffset;
            ob.YAmplitudeOffset = YAmplitudeOffset;
            ob.ZAmplitudeOffset = ZAmplitudeOffset;

            ob.XAmplitudeScale = XAmplitudeScale;
            ob.YAmplitudeScale = YAmplitudeScale;
            ob.ZAmplitudeScale = ZAmplitudeScale;

            ob.RunMultiplier = RunMultiplier;

            ob.ItemWalkingOrientation3rd = Quaternion.CreateFromRotationMatrix(ItemWalkingLocation3rd);
            ob.ItemWalkingPosition3rd = ItemWalkingLocation3rd.Translation;

            ob.ItemOrientation3rd = Quaternion.CreateFromRotationMatrix(ItemLocation3rd);
            ob.ItemPosition3rd = ItemLocation3rd.Translation;

            ob.AmplitudeMultiplier3rd = AmplitudeMultiplier3rd;

            ob.SimulateLeftHand = SimulateLeftHand;
            ob.SimulateRightHand = SimulateRightHand;

            ob.FingersAnimation = FingersAnimation;

            ob.ItemShootOrientation = Quaternion.CreateFromRotationMatrix(ItemShootLocation);
            ob.ItemShootPosition = ItemShootLocation.Translation;

            ob.ItemShootOrientation3rd = Quaternion.CreateFromRotationMatrix(ItemShootLocation3rd);
            ob.ItemShootPosition3rd = ItemShootLocation3rd.Translation;

            ob.ShootBlend = ShootBlend;

            ob.MuzzlePosition = MuzzlePosition;

            ob.ShootScatter = ShootScatter;
            ob.ScatterSpeed = ScatterSpeed;
            ob.PhysicalItemId = PhysicalItemId;

            ob.LightColor = LightColor;
            ob.LightFalloff = LightFalloff;
            ob.LightRadius = LightRadius;
            ob.LightGlareSize = LightGlareSize;
            ob.LightIntensityLower = LightIntensityLower;
            ob.LightIntensityUpper = LightIntensityUpper;
            ob.ShakeAmountTarget = ShakeAmountTarget;
            ob.ShakeAmountNoTarget = ShakeAmountNoTarget;

            return ob;
        }
    }
}
