#region Using

using Sandbox.Common;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Input;
using VRageMath;


#endregion

namespace Sandbox.Game.Entities
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class MyPlaceAreas : MySessionComponentBase
    {
        #region Fields

        MyDynamicAABBTreeD m_aabbTree = new MyDynamicAABBTreeD(MyConstants.GAME_PRUNING_STRUCTURE_AABB_EXTENSION);

		public static MyPlaceAreas Static;
		private static List<MyPlaceArea> m_tmpAreas = new List<MyPlaceArea>();

		public MyAreaMarkerDefinition AreaMarkerDefinition = null;

        #endregion

		public MyPlaceAreas()
		{
			Static = this;
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
                area.PlaceAreaProxyId = MyConstants.PRUNING_PROXY_ID_UNITIALIZED;
            }

            Clear();
        }

        public void AddPlaceArea(MyPlaceArea area)
        {
            if (area.PlaceAreaProxyId == MyConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                BoundingBoxD box = area.WorldAABB;
                area.PlaceAreaProxyId = m_aabbTree.AddProxy(ref box, area, 0);
            }
        }

        public void RemovePlaceArea(MyPlaceArea area)
        {
            if (area.PlaceAreaProxyId != MyConstants.PRUNING_PROXY_ID_UNITIALIZED)
            {
                m_aabbTree.RemoveProxy(area.PlaceAreaProxyId);
                area.PlaceAreaProxyId = MyConstants.PRUNING_PROXY_ID_UNITIALIZED;
            }
        }

        public void MovePlaceArea(MyPlaceArea area)
        {
            if (area.PlaceAreaProxyId != MyConstants.PRUNING_PROXY_ID_UNITIALIZED)
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

			if (MySession.GetCameraControllerEnum() == Common.ObjectBuilders.MyCameraControllerEnum.ThirdPersonSpectator || MySession.GetCameraControllerEnum() == Common.ObjectBuilders.MyCameraControllerEnum.Entity)
			{
				var headMatrix = MySession.ControlledEntity.GetHeadMatrix(true, true);
				cameraPos = headMatrix.Translation;
				cameraDir = headMatrix.Forward;
			}
			else
			{
				cameraPos = MySector.MainCamera.Position;
				cameraDir = MySector.MainCamera.WorldMatrix.Forward;
			}

			List<MyPhysics.HitInfo> hitInfos = new List<MyPhysics.HitInfo>();

			MyPhysics.CastRay(cameraPos, cameraPos + cameraDir * 100, hitInfos, MyPhysics.ObjectDetectionCollisionLayer);
			if (hitInfos.Count == 0)
				return;

			MyPhysics.HitInfo? closestValidHit = null;
			foreach (var hitInfo in hitInfos)
			{
				var ent = hitInfo.HkHitInfo.Body.GetEntity();
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
				
				bool isAttacker = (definition.Id == MyBattleAreaMarker.AttackerSpawnDefId);
				bool isDefender = (definition.Id == MyBattleAreaMarker.DefenderSpawnDefId);
				if (!isAttacker)
				{
					m_tmpAreas.Clear();
					MyPlaceAreas.Static.GetAllAreas(m_tmpAreas);

					foreach (var area in m_tmpAreas)
					{
						if (area.AreaType == definition.Id.SubtypeId)
						{
							area.Container.Entity.Close();
						}
					}
					m_tmpAreas.Clear();
				}

				var forward = Vector3D.Reject(cameraDir, Vector3D.Up);

				if (Vector3D.IsZero(forward))
					forward = Vector3D.Forward;

				MyAreaMarker flag = null;
				var positionAndOrientation = new MyPositionAndOrientation(position, Vector3D.Normalize(forward), Vector3D.Up);

				if(isAttacker || isDefender)
				{
					uint battleSlot = 0;
					MyBattleAreaMarker.GetNextBattleSlot(out battleSlot, isAttacker);
					flag = new MyBattleAreaMarker(positionAndOrientation, definition, battleSlot);
				}
				else
					flag = new MyAreaMarker(positionAndOrientation, definition);

				MyEntities.Add(flag);
			}
		}

		public override void HandleInput()
		{
			base.HandleInput();

			if (!(MyScreenManager.GetScreenWithFocus() is MyGuiScreenGamePlay))
				return;

			if (MyControllerHelper.IsControl(MySpaceBindingCreator.CX_CHARACTER, MyControlsSpace.PRIMARY_TOOL_ACTION))
			{
				if (MySession.ControlledEntity != null && AreaMarkerDefinition != null)
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
