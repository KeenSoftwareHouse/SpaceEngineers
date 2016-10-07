#region Using

using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Multiplayer;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Game.Components;
using VRage.Game;
using VRage.Input;
using VRage.Network;
using VRage.ObjectBuilders;
using VRageMath;
using System;


#endregion

namespace Sandbox.Game.Entities
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    [StaticEventOwner]
    public class MyPlaceAreas : MySessionComponentBase
    {
        #region Fields

        MyDynamicAABBTreeD m_aabbTree = new MyDynamicAABBTreeD(MyConstants.GAME_PRUNING_STRUCTURE_AABB_EXTENSION);

		public static MyPlaceAreas Static;

		public MyAreaMarkerDefinition AreaMarkerDefinition = null;

        #endregion

		public MyPlaceAreas()
		{
			Static = this;
		}

        public override Type[] Dependencies
        {
            get
            {
                return new Type[] { typeof(MyToolbarComponent) };
            }
        }

		public override void LoadData()
		{
			base.LoadData();

			MyToolbarComponent.CurrentToolbar.SelectedSlotChanged += CurrentToolbar_SelectedSlotChanged;
			MyToolbarComponent.CurrentToolbar.SlotActivated += CurrentToolbar_SlotActivated;
			MyToolbarComponent.CurrentToolbar.Unselected += CurrentToolbar_Unselected;
		}

        protected override void UnloadData()
        {
			MyToolbarComponent.CurrentToolbar.SelectedSlotChanged -= CurrentToolbar_SelectedSlotChanged;
			MyToolbarComponent.CurrentToolbar.SlotActivated -= CurrentToolbar_SlotActivated;
			MyToolbarComponent.CurrentToolbar.Unselected -= CurrentToolbar_Unselected;

            base.UnloadData();

            List<MyPlaceArea> areas = new List<MyPlaceArea>();
            m_aabbTree.GetAll(areas, false);
            foreach (var area in areas)
            {
                area.PlaceAreaProxyId = MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED;
            }

            Clear();
        }

        public void AddPlaceArea(MyPlaceArea area)
        {
            if (area.PlaceAreaProxyId == MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                BoundingBoxD box = area.WorldAABB;
                area.PlaceAreaProxyId = m_aabbTree.AddProxy(ref box, area, 0);
            }
        }

        public void RemovePlaceArea(MyPlaceArea area)
        {
            if (area.PlaceAreaProxyId != MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                m_aabbTree.RemoveProxy(area.PlaceAreaProxyId);
                area.PlaceAreaProxyId = MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED;
            }
        }

        public void MovePlaceArea(MyPlaceArea area)
        {
            if (area.PlaceAreaProxyId != MyVRageConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                BoundingBoxD box = area.WorldAABB;
                m_aabbTree.MoveProxy(area.PlaceAreaProxyId, ref box, Vector3.Zero);
            }
        }

        public void Clear()
        {
            m_aabbTree.Clear();
        }

        public void GetAllAreasInSphere(BoundingSphereD sphere, List<MyPlaceArea> result)
        {
            m_aabbTree.OverlapAllBoundingSphere<MyPlaceArea>(ref sphere, result, false);
        }

        public void GetAllAreas(List<MyPlaceArea> result)
        {
            m_aabbTree.GetAll<MyPlaceArea>(result, false);
        }

		private void PlaceAreaMarker()
		{
			Vector3D cameraPos, cameraDir;

			if (MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.ThirdPersonSpectator || MySession.Static.GetCameraControllerEnum() == MyCameraControllerEnum.Entity)
			{
				var headMatrix = MySession.Static.ControlledEntity.GetHeadMatrix(true, true);
				cameraPos = headMatrix.Translation;
				cameraDir = headMatrix.Forward;
			}
			else
			{
				cameraPos = MySector.MainCamera.Position;
				cameraDir = MySector.MainCamera.WorldMatrix.Forward;
			}

			List<MyPhysics.HitInfo> hitInfos = new List<MyPhysics.HitInfo>();

            MyPhysics.CastRay(cameraPos, cameraPos + cameraDir * 100, hitInfos, MyPhysics.CollisionLayers.ObjectDetectionCollisionLayer);
			if (hitInfos.Count == 0)
				return;

			MyPhysics.HitInfo? closestValidHit = null;
			foreach (var hitInfo in hitInfos)
			{
				var ent = hitInfo.HkHitInfo.GetHitEntity();
				if (ent is MyCubeGrid)
				{
					closestValidHit = hitInfo;
					break;
				}
				else if (ent is MyVoxelMap)
				{
					closestValidHit = hitInfo;
					break;
				}
			}

			if (closestValidHit.HasValue)
			{
				MyAreaMarkerDefinition definition = AreaMarkerDefinition;
				Debug.Assert(definition != null, "Area marker definition cannot be null!");
				if (definition == null) return;

				Vector3D position = closestValidHit.Value.Position;

				var forward = Vector3D.Reject(cameraDir, Vector3D.Up);

				if (Vector3D.IsZero(forward))
					forward = Vector3D.Forward;

				var positionAndOrientation = new MyPositionAndOrientation(position, Vector3D.Normalize(forward), Vector3D.Up);

                MyObjectBuilder_AreaMarker objectBuilder = (MyObjectBuilder_AreaMarker)MyObjectBuilderSerializer.CreateNewObject(definition.Id);
                objectBuilder.PersistentFlags = MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;
                objectBuilder.PositionAndOrientation = positionAndOrientation;

				if (objectBuilder.IsSynced)
                {
                    SerializableDefinitionId id = definition.Id;
                    MyMultiplayer.RaiseStaticEvent(x => CreateNewPlaceArea, id,positionAndOrientation);
                }			
				else
				{
					MyAreaMarker flag = MyEntityFactory.CreateEntity<MyAreaMarker>(objectBuilder);
					flag.Init(objectBuilder);

					MyEntities.Add(flag);
				}
			}
		}

        [Event, Reliable,Server]
        static void CreateNewPlaceArea(SerializableDefinitionId id, MyPositionAndOrientation positionAndOrientation)
        {
            MyObjectBuilder_AreaMarker objectBuilder = (MyObjectBuilder_AreaMarker)MyObjectBuilderSerializer.CreateNewObject(id);
            objectBuilder.PersistentFlags = MyPersistentEntityFlags2.Enabled | MyPersistentEntityFlags2.InScene;
            objectBuilder.PositionAndOrientation = positionAndOrientation;

            MyEntities.CreateFromObjectBuilderAndAdd(objectBuilder);
        }

		public override void HandleInput()
		{
			base.HandleInput();

			if (!(MyScreenManager.GetScreenWithFocus() is MyGuiScreenGamePlay))
				return;

			if (MyControllerHelper.IsControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.PRIMARY_TOOL_ACTION))
			{
				if (MySession.Static.ControlledEntity != null && AreaMarkerDefinition != null)
					PlaceAreaMarker();
			}
		}

        public void DebugDraw()
        {
            var result = new List<MyPlaceArea>();
            var resultAABBs = new List<BoundingBoxD>();
            m_aabbTree.GetAll(result, true, resultAABBs);
            for (int i = 0; i < result.Count; i++)
            {
                VRageRender.MyRenderProxy.DebugDrawAABB(resultAABBs[i], Vector3.One, 1, 1, false);
            }
        }

		private void CurrentToolbar_SelectedSlotChanged(MyToolbar toolbar, MyToolbar.SlotArgs args)
		{
			if (!(toolbar.SelectedItem is MyToolbarItemAreaMarker))
				AreaMarkerDefinition = null;
		}

		private void CurrentToolbar_SlotActivated(MyToolbar toolbar, MyToolbar.SlotArgs args)
		{
			if (!(toolbar.GetItemAtIndex(toolbar.SlotToIndex(args.SlotNumber.Value)) is MyToolbarItemAreaMarker))
				AreaMarkerDefinition = null;
		}

		private void CurrentToolbar_Unselected(MyToolbar toolbar)
		{
			AreaMarkerDefinition = null;
		}
    }
}
