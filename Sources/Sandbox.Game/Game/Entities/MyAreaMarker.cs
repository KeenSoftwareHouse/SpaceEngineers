using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Game.Gui;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Sandbox.Game.Entities
{
    [MyEntityType(typeof(MyObjectBuilder_AreaMarker))]
    class MyAreaMarker : MyEntity, IMyUseObject
    {
        MyAreaMarkerDefinition m_definition;

        MatrixD m_localActivationMatrix;

        public override Vector3D LocationForHudMarker
        {
            get
            {
                return PositionComp.GetPosition() + Vector3D.TransformNormal(m_definition.MarkerPosition, PositionComp.WorldMatrix);
            }
        }

        public MyAreaMarker()
        {
        }

		public MyAreaMarker(MyPositionAndOrientation positionAndOrientation, MyAreaMarkerDefinition definition)
        {
            m_definition = definition;
            Debug.Assert(definition != null, "Area marker definition cannot be null!");
            if (definition == null) return;

			MatrixD matrix = MatrixD.CreateWorld(positionAndOrientation.Position, positionAndOrientation.Forward, positionAndOrientation.Up);

			PositionComp.SetWorldMatrix((MatrixD)matrix);
			if (MyPerGameSettings.LimitedWorld)
			{
				ClampToWorld();
			}

            InitInternal();
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            MyDefinitionManager.Static.TryGetDefinition(new MyDefinitionId(typeof(MyObjectBuilder_AreaMarkerDefinition), objectBuilder.SubtypeId), out m_definition);
            Debug.Assert(m_definition != null, "Area marker definition cannot be null!");
            if (m_definition == null) return;

            InitInternal();
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            var ob = base.GetObjectBuilder(copy) as MyObjectBuilder_AreaMarker;

            ob.SubtypeName = m_definition.Id.SubtypeName;

            return ob;
        }

        private void InitInternal()
        {
            base.Init(null, m_definition.Model, null, null);
            Render.ColorMaskHsv = m_definition.ColorHSV;
            Render.Transparency = 0.25f;
            Render.AddRenderObjects();

			List<MyTextureChange> textureChanges = new List<MyTextureChange>();
			textureChanges.Add(new MyTextureChange { TextureName = m_definition.ColorMetalTexture, MaterialSlot = "ColorMetalTexture" });
			textureChanges.Add(new MyTextureChange { TextureName = m_definition.AddMapsTexture, MaterialSlot = "AddMapsTexture" });

            VRageRender.MyRenderProxy.ChangeMaterialTexture(Render.RenderObjectIDs[0], "BotFlag", textureChanges); // TODO: change the material name

            m_localActivationMatrix = MatrixD.CreateScale(this.PositionComp.LocalAABB.HalfExtents * 2.0f) * MatrixD.CreateTranslation(this.PositionComp.LocalAABB.Center);

            var shape = new HkBoxShape(m_localActivationMatrix.Scale);
            Physics = new MyPhysicsBody(this, RigidBodyFlag.RBF_DISABLE_COLLISION_RESPONSE);
            Physics.CreateFromCollisionObject(shape, Vector3.Zero, WorldMatrix, null, MyPhysics.ObjectDetectionCollisionLayer);
            Physics.Enabled = true;

            Components.Add<MyPlaceArea>(new MySpherePlaceArea(10.0f, m_definition.Id.SubtypeId)); // TODO: Add radius to the definition

            MyHud.LocationMarkers.RegisterMarker(this, new MyHudEntityParams() {
                FlagsEnum = MyHudIndicatorFlagsEnum.SHOW_TEXT,
                Text = m_definition.DisplayNameEnum.HasValue ? MyTexts.Get(m_definition.DisplayNameEnum.Value) : new StringBuilder(),
                TargetMode = MyRelationsBetweenPlayerAndBlock.Neutral,
                MaxDistance = 200.0f,
                MustBeDirectlyVisible = true
            } );
        }

        protected override void Closing()
        {
            MyHud.LocationMarkers.UnregisterMarker(this);

            base.Closing();
        }

        public float InteractiveDistance
        {
            get { return 5.0f; }
        }

        public MatrixD ActivationMatrix
        {
            get { return m_localActivationMatrix * WorldMatrix; }
        }

        public int RenderObjectID
        {
            get { return (int)Render.RenderObjectIDs[0]; }
        }

        public bool ShowOverlay
        {
            get { return true; }
        }

        public UseActionEnum SupportedActions
        {
            get { return UseActionEnum.Manipulate; }
        }

        public bool ContinuousUsage
        {
            get { return false; }
        }

        public void Use(UseActionEnum actionEnum, Sandbox.Game.Entities.Character.MyCharacter user)
        {
            Close();
        }

        public MyActionDescription GetActionInfo(UseActionEnum actionEnum)
        {
            return new MyActionDescription()
            {
                Text = MyStringId.GetOrCompute("NotificationRemoveAreaMarker"),
                FormatParams = new object[] { MyInput.Static.GetGameControl(MyControlsSpace.USE) }
            };
        }

        public bool HandleInput()
        {
            return false;
        }

        public void OnSelectionLost()
        {
        }
    }
}
