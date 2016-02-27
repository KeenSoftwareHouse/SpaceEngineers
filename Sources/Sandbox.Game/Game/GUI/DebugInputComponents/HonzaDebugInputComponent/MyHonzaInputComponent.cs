﻿#region Using

using Havok;
//using PhysX;
//using PhysX.Cooking;
//using PhysX.Extensions;
//using PhysX.VisualDebugger;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using VRage.Audio;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Input;
using VRage.ModAPI;
using VRageMath;

#endregion

namespace Sandbox.Game.Gui
{
    public partial class MyHonzaInputComponent : MyMultiDebugInputComponent
    {
        private static IMyEntity m_selectedEntity = null;
        public static event Action OnSelectedEntityChanged;
        public static IMyEntity SelectedEntity
        {
            get { return m_selectedEntity; }
            set
            {
                if (m_selectedEntity == value)
                    return;
                m_selectedEntity = value;
                m_counter = dbgPosCounter = 0;
                if (OnSelectedEntityChanged != null)
                    OnSelectedEntityChanged();
            }
        }
        private static long m_counter = 0;
        public static long dbgPosCounter = 0;


        private MyDebugComponent[] m_components;

        public override MyDebugComponent[] Components
        {
            get { return m_components; }
        }

        public override string GetName()
        {
            return "Honza";
        }

        static MyHonzaInputComponent()
        {
            //InitPX();
            //Physx.PxBase.InitPX();
        }

        public override bool HandleInput()
        {
            var result = base.HandleInput();
            HandleEntitySelect();
            return result;
        }

        private void HandleEntitySelect()
        {
            if (MyInput.Static.IsAnyShiftKeyPressed() && MyInput.Static.IsNewLeftMousePressed())
            {
                if (SelectedEntity != null)
                {
                    ((MyEntity)SelectedEntity).ClearDebugRenderComponents();
                    SelectedEntity = null;
                }
                else
                {
                    if (MySector.MainCamera != null)
                    {
                        List<MyPhysics.HitInfo> lst = new List<MyPhysics.HitInfo>();
                        MyPhysics.CastRay(MySector.MainCamera.Position,
                            MySector.MainCamera.Position + MySector.MainCamera.ForwardVector*100, lst);
                        foreach (var hit in lst)
                        {
                            var body = hit.HkHitInfo.Body;
                            if (body == null || body.Layer == MyPhysics.CollisionLayers.NoCollisionLayer)
                                continue;
                            SelectedEntity = hit.HkHitInfo.GetHitEntity();
                        }
                    }

                }
            }
        }
        public MyHonzaInputComponent()
        {
            m_components = new MyDebugComponent[] { new DefaultComponent(), new PhysicsComponent(), new LiveWatchComponent() };
        }
    }
}
