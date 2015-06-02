#region Using

using Havok;
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
using VRage.Library.Utils;
using VRage.FileSystem;
using VRage.ObjectBuilders;
using VRage.Components;
using VRage.ModAPI;

#endregion

namespace VRage
{
    public class MySkinnedEntity //: IMyEntity
    {
        #region Fields

        //ulong m_actualUpdateFrame = 0;
        //ulong m_actualDrawFrame = 0;
        //ulong m_transformedBonesFrame = 0;
        //bool m_characterBonesReady = false;

        CapsuleD[] m_bodyCapsules = new CapsuleD[1];//new CapsuleD[10];
       
//        float m_currentHeadAnimationCounter = 0;
        List<List<int>> m_bodyCapsuleBones = new List<List<int>>();

        List<Matrix> m_simulatedBones = new List<Matrix>();
        

        #endregion

        #region Init


        public MySkinnedEntity()
        {
        }

        public void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            //NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

            //Render.NeedsDraw = true;
            //Render.CastShadows = true;
            //Render.NeedsResolveCastShadow = false;
            //Render.SkipIfTooSmall = false;

            //PositionComp.LocalAABB = new BoundingBox(-new Vector3(0.3f, 0.0f, 0.3f), new Vector3(0.3f, 1.8f, 0.3f));

         
            //InitAnimations();
            //ValidateBonesProperties();
            //CalculateTransforms();

            //Physics.Enabled = true;

            //Physics.LinearVelocity = characterOb.LinearVelocity;

            // Ragdoll
            //if (Physics != null && MyPerGameSettings.EnableRagdollModels)
            //{
            //    InitRagdoll();
            //}

            //if ((Definition.RagdollBonesMappings.Count > 1) && (MyPerGameSettings.EnableRagdollModels) && Physics.Ragdoll != null)
            //{                
            //   InitRagdollMapper();               
            //}

           

            //if (Definition.RagdollBonesMappings.Count > 0) 
            //    CreateBodyCapsulesForHits(Definition.RagdollBonesMappings);
            //else
            //    m_bodyCapsuleBones.Clear();
            //InitSounds();

        }

       

        private void CreateBodyCapsulesForHits(Dictionary<string, string[]> bonesMappings)
        {
            m_bodyCapsuleBones.Clear();
            m_bodyCapsules = new CapsuleD[bonesMappings.Count];           
            foreach (var boneSet in bonesMappings)
            {
                try
                {                    
                    //String[] boneNames = boneSet.Value;
                    //int firstBone;
                    //int lastBone;					
                    //Debug.Assert(boneNames.Length >= 2, "In ragdoll model definition of bonesets is only one bone, can not create body capsule properly! Model:" + ModelName + " BoneSet:" + boneSet.Key);
                    //FindBone(boneNames.First(), out firstBone);
                    //FindBone(boneNames.Last(), out lastBone);     
                    //List<int> boneList = new List<int>(2);
                    //boneList.Add(firstBone);
                    //boneList.Add(lastBone);
                    //m_bodyCapsuleBones.Add(boneList);                    
                }
                catch (Exception e)
                {
                    Debug.Fail(e.Message);                   
                }
            }            
        }

        ///// <summary>
        ///// Loads Ragdoll data
        ///// </summary>
        ///// <param name="ragDollFile"></param>
        //public void InitRagdoll()
        //{
        //    //if (!Sync.IsServer) return;
        //    if (Physics.Ragdoll != null)
        //    {
        //        Physics.CloseRagdollMode();
        //        Physics.Ragdoll.ResetToRigPose();
        //        Physics.Ragdoll.SetToKeyframed();                
        //        //Physics.CloseRagdoll();
        //        //Physics.Ragdoll = null;
        //        return;
        //    }

        //    Physics.Ragdoll = new HkRagdoll();

        //    bool dataLoaded = false;
        //    if (Model.HavokData != null && Model.HavokData.Length > 0)  
        //    {
        //        try
        //        {
        //            dataLoaded = Physics.Ragdoll.LoadRagdollFromBuffer(Model.HavokData);
        //        }
        //        catch (Exception e)
        //        {
        //            Debug.Fail("Error loading ragdoll from buffer: " + e.Message);
        //            Physics.CloseRagdoll();
        //            Physics.Ragdoll = null;
        //        }
        //    }            
        //    else if (Definition.RagdollDataFile != null)
        //    {
        //        String ragDollFile = System.IO.Path.Combine(MyFileSystem.ContentPath, Definition.RagdollDataFile);
        //        if (System.IO.File.Exists(ragDollFile))
        //        {                  
        //            dataLoaded = Physics.Ragdoll.LoadRagdollFromFile(ragDollFile);
        //        }
        //        else
        //        {               
        //            System.Diagnostics.Debug.Fail("Cannot find ragdoll file: " + ragDollFile);               
        //        }
        //    }

        //    if (Definition.RagdollRootBody != String.Empty)
        //    {
        //        if (!Physics.Ragdoll.SetRootBody(Definition.RagdollRootBody))
        //        {
        //            Debug.Fail("Can not set root body with name: " + Definition.RagdollRootBody + " on model " + ModelName + ". Please check your definitions.");
        //        }
        //    }

        //    if (!dataLoaded)
        //    {
        //        Physics.Ragdoll.Dispose();
        //        Physics.Ragdoll = null;
        //    }
           
        //    if (Physics.Ragdoll != null && MyFakes.ENABLE_RAGDOLL_DEFAULT_PROPERTIES)
        //    {
        //        Physics.SetRagdollDefaults();
        //    }

        //}

        

        //public void InitRagdollMapper()
        //{
        //    if (m_bones.Count == 0) return;
        //    if (Physics == null || Physics.Ragdoll == null) return;

        //    RagdollMapper = new MyRagdollMapper(this, m_bones);

        //    RagdollMapper.Init(Definition.RagdollBonesMappings);
        //}

      

    

        #endregion

        #region Simulation

        //public override void UpdateBeforeSimulation()
        //{
        //    base.UpdateBeforeSimulation();

        //    m_actualUpdateFrame++;

          

        //    // TODO: This should be changed so the ragdoll gets registered in the generators, now for SE, apply gravity explictly
        //    // Apply Gravity on Ragdoll
        //    if (Physics.Ragdoll != null && Physics.Ragdoll.IsAddedToWorld && (!Physics.Ragdoll.IsKeyframed || RagdollMapper.IsPartiallySimulated))
        //    {
        //        Vector3 gravity = MyGravityProviderSystem.CalculateGravityInPoint(PositionComp.WorldAABB.Center) + Physics.HavokWorld.Gravity * CHARACTER_GRAVITY_MULTIPLIER;                
        //        Physics.AddForce(MyPhysicsForceType.APPLY_WORLD_FORCE, gravity * Definition.Mass, null, null);
        //    }


        //    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Ragdoll");
        //    UpdateRagdoll();
        //    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        //}

      

        //public override void UpdateAfterSimulation()
        //{
        //    base.UpdateAfterSimulation();

        //    m_updateCounter++;

        //    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Update Animation");
        //    UpdateAnimation();
        //    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();


        //    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Calculate transforms");
        //    CalculateTransforms();
        //    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();

        //    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Calculate dependent matrices");
        //    CalculateDependentMatrices();
        //    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();


        //    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("Simulate Ragdoll");
        //    SimulateRagdoll();    // probably should be in UpdateDying, but changes the animation of the bones..
        //    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
            
        //}



        bool ShouldUseAnimatedHeadRotation()
        {
            //if (m_currentHeadAnimationCounter > 0.15f)
            //  return true;

            return false;
        }

        //private void CalculateDependentMatrices()
        //{
        //    Render.UpdateThrustMatrices(BoneTransforms);

        //    m_actualWorldAABB = BoundingBoxD.CreateInvalid();

        //    for (int i = 1; i < Model.Bones.Length; i++)
        //    {
        //        Vector3D p1 = Vector3D.Transform(m_bones[i].Parent.AbsoluteTransform.Translation, m_helperMatrix * WorldMatrix);
        //        Vector3D p2 = Vector3D.Transform(m_bones[i].AbsoluteTransform.Translation, m_helperMatrix * WorldMatrix);

        //        m_actualWorldAABB.Include(ref p1);
        //        m_actualWorldAABB.Include(ref p2);
        //    }

        //    ContainmentType containmentType;
        //    m_aabb.Contains(ref m_actualWorldAABB, out containmentType);
        //    if (containmentType != ContainmentType.Contains)
        //    {
        //        m_actualWorldAABB.Inflate(0.5f);
        //        MatrixD worldMatrix = WorldMatrix;
        //        VRageRender.MyRenderProxy.UpdateRenderObject(Render.RenderObjectIDs[0], ref worldMatrix, false, m_actualWorldAABB);
        //        m_aabb = m_actualWorldAABB;
        //    }
        //}

        #endregion

    }
}
