using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.Entity.UseObject;
using VRage.Game.Gui;
using VRage.Import;
using VRage.Input;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Import;
using VRageRender.Messages;

namespace Sandbox.Game.Entities
{
	[MyEntityType(typeof(MyObjectBuilder_AreaMarker))]
    public class MyAreaMarker : MyEntity, IMyUseObject
    {
        protected MyAreaMarkerDefinition m_definition;
		public MyAreaMarkerDefinition Definition { get { return m_definition; } }

		private static List<MyPlaceArea> m_tmpPlaceAreas = new List<MyPlaceArea>();
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

            MyDefinitionManager.Static.TryGetDefinition(objectBuilder.GetId(), out m_definition);
            Debug.Assert(m_definition != null, "Area marker definition cannot be null!");
            if (m_definition == null) return;

			m_tmpPlaceAreas.Clear();
			MyPlaceAreas.Static.GetAllAreas(m_tmpPlaceAreas);

			MyPlaceArea firstFound = null;
			int markerCount = 0;
			foreach (var area in m_tmpPlaceAreas)
			{
				if (area.AreaType == m_definition.Id.SubtypeId)
				{
					if (firstFound == null)
						firstFound = area;
					++markerCount;
				}
			}
			if (m_definition.MaxNumber >= 0 && markerCount >= m_definition.MaxNumber)
			{
				if (SyncFlag)
					firstFound.Entity.SyncObject.SendCloseRequest();
				else
					firstFound.Entity.Close();
			}

			m_tmpPlaceAreas.Clear();
			
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
			textureChanges.Add(new MyTextureChange { TextureName = m_definition.ColorMetalTexture, TextureType = MyTextureType.ColorMetal });
            textureChanges.Add(new MyTextureChange { TextureName = m_definition.AddMapsTexture, TextureType = MyTextureType.Extensions });

            VRageRender.MyRenderProxy.ChangeMaterialTexture(Render.RenderObjectIDs[0], "BotFlag", textureChanges); // TODO: change the material name

            m_localActivationMatrix = MatrixD.CreateScale(this.PositionComp.LocalAABB.HalfExtents * 2.0f) * MatrixD.CreateTranslation(this.PositionComp.LocalAABB.Center);

            var shape = new HkBoxShape(m_localActivationMatrix.Scale);
            var physicsBody = new MyPhysicsBody(this, RigidBodyFlag.RBF_DISABLE_COLLISION_RESPONSE);
            Physics = physicsBody;
            physicsBody.CreateFromCollisionObject(shape, Vector3.Zero, WorldMatrix, null, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);
            physicsBody.Enabled = true;

            Components.Add<MyPlaceArea>(new MySpherePlaceArea(10.0f, m_definition.Id.SubtypeId)); // TODO: Add radius to the definition

			AddHudMarker();
        }

		public virtual void AddHudMarker()
		{
			MyHud.LocationMarkers.RegisterMarker(this, new MyHudEntityParams()
			{
				FlagsEnum = MyHudIndicatorFlagsEnum.SHOW_TEXT,
				Text = m_definition.DisplayNameEnum.HasValue ? MyTexts.Get(m_definition.DisplayNameEnum.Value) : new StringBuilder(),
				TargetMode = VRage.Game.MyRelationsBetweenPlayerAndBlock.Neutral,
				MaxDistance = 200.0f,
				MustBeDirectlyVisible = true
			});
		}

        protected override void Closing()
        {
            MyHud.LocationMarkers.UnregisterMarker(this);

            base.Closing();
        }

        IMyEntity IMyUseObject.Owner
        {
            get { return this; }
        }

        MyModelDummy IMyUseObject.Dummy
        {
            get { return null; }
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

        public void SetRenderID(uint id)
        {
        }

        public int InstanceID
        {
            get { return -1; }
        }

        public void SetInstanceID(int id)
        {
        }

        public bool ShowOverlay
        {
            get { return true; }
        }

        public UseActionEnum SupportedActions
        {
            get { return MyFakes.ENABLE_SEPARATE_USE_AND_PICK_UP_KEY ? UseActionEnum.PickUp : UseActionEnum.Manipulate; }
        }

        public bool ContinuousUsage
        {
            get { return false; }
        }

        public virtual void Use(UseActionEnum actionEnum, IMyEntity user)
        {
            Close();
        }

        public virtual MyActionDescription GetActionInfo(UseActionEnum actionEnum)
        {
            return new MyActionDescription()
            {
                Text = MyStringId.GetOrCompute("NotificationRemoveAreaMarker"),
                FormatParams = new object[] { MyInput.Static.GetGameControl(MyFakes.ENABLE_SEPARATE_USE_AND_PICK_UP_KEY ? MyControlsSpace.PICK_UP : MyControlsSpace.USE) }
            };
        }

        public bool HandleInput()
        {
            return false;
        }

        public void OnSelectionLost()
        {
        }

        bool IMyUseObject.PlayIndicatorSound
        {
            get { return true; }
        }
    }
}
