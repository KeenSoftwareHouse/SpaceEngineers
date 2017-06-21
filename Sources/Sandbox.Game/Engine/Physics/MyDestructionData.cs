using Havok;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Models;
using Sandbox.Engine.Physics;
using Sandbox.Game;
using System.Collections.Generic;
using System.IO;
using VRage.Import;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using VRage;
using VRage.Utils;

using System.Diagnostics;
using Sandbox.Engine.Utils;
using VRage.Library.Utils;
using VRage.FileSystem;
using VRage.Game.Components;
using VRage.Game.Models;
using VRage.Profiler;
using VRageRender.Fractures;
using VRageRender.Utils;

namespace Sandbox
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MyDestructionData : MySessionComponentBase
    {
        private static List<HkdShapeInstanceInfo> m_tmpChildrenList = new List<HkdShapeInstanceInfo>();
        private static MyPhysicsMesh m_tmpMesh = new MyPhysicsMesh();

        public static MyDestructionData Static { get; set; }
        public HkWorld TemporaryWorld { get; private set; }
        public MyBlockShapePool BlockShapePool { get; private set; }
        HkDestructionStorage Storage;

        static Dictionary<string, MyPhysicalMaterialDefinition> m_physicalMaterials;

        public override bool IsRequiredByGame
        {
            get { return MyPerGameSettings.Destruction; }
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();
            BlockShapePool.RefillPools();
        }

        public override void LoadData()
        {
            if (!HkBaseSystem.DestructionEnabled)
            {
                MyLog.Default.WriteLine("Havok Destruction is not availiable in this build.");
                throw new System.InvalidOperationException("Havok Destruction is not availiable in this build.");
            }

            if (Static != null)
            {
                MyLog.Default.WriteLine("Destruction data was not freed. Unloading now...");
                //throw new System.InvalidOperationException("Destruction data was not freed");
                UnloadData();
            }
            Static = this;
            BlockShapePool = new MyBlockShapePool();

            TemporaryWorld = new HkWorld(true, 50000, MyPhysics.RestingVelocity, MyFakes.ENABLE_HAVOK_MULTITHREADING, 4);
            TemporaryWorld.MarkForWrite();
            TemporaryWorld.DestructionWorld = new HkdWorld(TemporaryWorld);
            TemporaryWorld.UnmarkForWrite();
            Storage = new HkDestructionStorage(TemporaryWorld.DestructionWorld);

            // pre-fracture cube blocks
            {
                foreach (var groupName in MyDefinitionManager.Static.GetDefinitionPairNames())
                {
                    var group = MyDefinitionManager.Static.GetDefinitionGroup(groupName);

                    if (group.Large != null)
                    {
                        var model = VRage.Game.Models.MyModels.GetModel(group.Large.Model);
                        if (model == null)
                            continue;

                        if (!MyFakes.LAZY_LOAD_DESTRUCTION || (model != null && model.HavokBreakableShapes != null)) //reload materials
                            LoadModelDestruction(group.Large.Model, group.Large, group.Large.Size * (MyDefinitionManager.Static.GetCubeSize(group.Large.CubeSize)));
                       
                       foreach(var progress in group.Large.BuildProgressModels)
                       {
                           model = VRage.Game.Models.MyModels.GetModel(progress.File);
                           if (model == null)
                               continue;

                           if (!MyFakes.LAZY_LOAD_DESTRUCTION || (model != null && model.HavokBreakableShapes != null)) //reload materials
                               LoadModelDestruction(progress.File, group.Large, group.Large.Size * (MyDefinitionManager.Static.GetCubeSize(group.Large.CubeSize)));
                       }
                        
                        
                        if (MyFakes.CHANGE_BLOCK_CONVEX_RADIUS)
                        {
                            if (model != null && model.HavokBreakableShapes != null)
                            {
                                var shape = model.HavokBreakableShapes[0].GetShape();
                                if(shape.ShapeType != HkShapeType.Sphere && shape.ShapeType != HkShapeType.Capsule)
                                    SetConvexRadius(model.HavokBreakableShapes[0], MyDestructionConstants.LARGE_GRID_CONVEX_RADIUS);
                            }
                        }
                    }

                    if (group.Small != null)
                    {
                        var model = VRage.Game.Models.MyModels.GetModel(group.Small.Model);
                        if (model == null)
                            continue;

                        if (!MyFakes.LAZY_LOAD_DESTRUCTION || (model != null && model.HavokBreakableShapes != null)) //reload materials
                            LoadModelDestruction(group.Small.Model, group.Small, group.Small.Size * (MyDefinitionManager.Static.GetCubeSize(group.Small.CubeSize)));

                        foreach (var progress in group.Small.BuildProgressModels)
                        {
                            model = VRage.Game.Models.MyModels.GetModel(progress.File);
                            if (model == null)
                                continue;
                            if (!MyFakes.LAZY_LOAD_DESTRUCTION || (model != null && model.HavokBreakableShapes != null)) //reload materials
                                LoadModelDestruction(progress.File, group.Small, group.Large.Size * (MyDefinitionManager.Static.GetCubeSize(group.Large.CubeSize)));
                        }

                        if (MyFakes.CHANGE_BLOCK_CONVEX_RADIUS)
                        {
                            if (model != null && model.HavokBreakableShapes != null)
                            {
                                var shape = model.HavokBreakableShapes[0].GetShape();
                                if (shape.ShapeType != HkShapeType.Sphere && shape.ShapeType != HkShapeType.Capsule)
                                    SetConvexRadius(model.HavokBreakableShapes[0], MyDestructionConstants.LARGE_GRID_CONVEX_RADIUS);
                            }
                        }
                    }
                }
                if (!MyFakes.LAZY_LOAD_DESTRUCTION)
                    BlockShapePool.Preallocate();
            }

            foreach (var def in MyDefinitionManager.Static.GetAllDefinitions<MyPhysicalModelDefinition>())
            {
                LoadModelDestruction(def.Model, def, Vector3.One, false, true);
            }
        }

        protected override void UnloadData()
        {

            TemporaryWorld.MarkForWrite(); //must be marked for write when deleting
            Storage.Dispose();
            Storage = null;
            TemporaryWorld.DestructionWorld.Dispose();

            TemporaryWorld.Dispose();
            TemporaryWorld = null;

            BlockShapePool.Free();
            BlockShapePool = null;

            Static = null;
        }

        HkReferenceObject CreateGeometryFromSplitPlane(string splitPlane)
        {
            string moddedSplitPlane = splitPlane;

            var model = VRage.Game.Models.MyModels.GetModelOnlyData(moddedSplitPlane);
            if (model != null)
            {
                var physicsMesh = CreatePhysicsMesh(model);

                var geometry = Storage.CreateGeometry(physicsMesh, Path.GetFileNameWithoutExtension(splitPlane));
                return geometry;
            }

            return null;
        }

        void FractureBreakableShape(HkdBreakableShape bShape, MyModelFractures modelFractures, string modPath)
        {
            HkdFracture fracture = null;
            HkReferenceObject geometry = null;

            if (modelFractures.Fractures[0] is RandomSplitFractureSettings)
            {
                var settings = (RandomSplitFractureSettings)modelFractures.Fractures[0];
                fracture = new HkdRandomSplitFracture()
                {
                    NumObjectsOnLevel1 = settings.NumObjectsOnLevel1,
                    NumObjectsOnLevel2 = settings.NumObjectsOnLevel2,
                    RandomRange = settings.RandomRange,
                    RandomSeed1 = settings.RandomSeed1,
                    RandomSeed2 = settings.RandomSeed2,
                    SplitGeometryScale = Vector4.One
                };

                if (!string.IsNullOrEmpty(settings.SplitPlane))
                {
                    var splitPlane = settings.SplitPlane;
                    if (!string.IsNullOrEmpty(modPath))
                        splitPlane = Path.Combine(modPath, settings.SplitPlane);

                    geometry = CreateGeometryFromSplitPlane(splitPlane);
                    if (geometry != null)
                    {
                        ((HkdRandomSplitFracture)fracture).SetGeometry(geometry);
                        VRageRender.MyRenderProxy.PreloadMaterials(splitPlane);
                    }
                }
            }
            if (modelFractures.Fractures[0] is VoronoiFractureSettings)
            {
                var settings = (VoronoiFractureSettings)modelFractures.Fractures[0];
                fracture = new HkdVoronoiFracture()
                {
                    Seed = settings.Seed,
                    NumSitesToGenerate = settings.NumSitesToGenerate,
                    NumIterations = settings.NumIterations
                };

                if (!string.IsNullOrEmpty(settings.SplitPlane))
                {
                    var splitPlane = settings.SplitPlane;
                    if (!string.IsNullOrEmpty(modPath))
                        splitPlane = Path.Combine(modPath, settings.SplitPlane);

                    geometry = CreateGeometryFromSplitPlane(splitPlane);

                    var pspm = VRage.Game.Models.MyModels.GetModel(splitPlane);

                    if (geometry != null)
                    {
                        ((HkdVoronoiFracture)fracture).SetGeometry(geometry);
                        VRageRender.MyRenderProxy.PreloadMaterials(splitPlane);
                    }
                }

            }
            if (modelFractures.Fractures[0] is WoodFractureSettings)
            {
                //TODO: Apply wood fracture algorithm
                var settings = (WoodFractureSettings)modelFractures.Fractures[0];
                fracture = new HkdWoodFracture()
                {
                    //Seed = settings.Seed,
                    //NumSitesToGenerate = settings.NumSitesToGenerate,
                    //NumIterations = settings.NumIterations
                };

                //if (!string.IsNullOrEmpty(settings.SplitPlane))
                //{
                //    var splitPlane = settings.SplitPlane;
                //    if (!string.IsNullOrEmpty(modPath))
                //        splitPlane = Path.Combine(modPath, settings.SplitPlane);

                //    geometry = CreateGeometryFromSplitPlane(splitPlane);

                //    var pspm = VRage.Game.Models.MyModels.GetModel(splitPlane);

                //    if (geometry != null)
                //    {
                //        ((HkdWoodFracture)fracture).SetGeometry(geometry);
                //        VRageRender.MyRenderProxy.PreloadMaterials(splitPlane);
                //    }
                //}
            }
           
            //if (woodButton.IsChecked)
            //{
            //    fracture = new HkdWoodFracture()
            //    {
            //        RandomSeed = 123456,
            //        BoardSplittingData = new HkdWoodFracture.SplittingData()
            //        {
            //        },
            //        SplinterSplittingData = new HkdWoodFracture.SplittingData()
            //        {
            //        }
            //    };
            //}

            if (fracture != null)
            {
                Storage.FractureShape(bShape, fracture);
                fracture.Dispose();
            }

            if (geometry != null)
                geometry.Dispose();
        }

        IPhysicsMesh CreatePhysicsMesh(MyModel model)
        {
            IPhysicsMesh physicsMesh = new MyPhysicsMesh();

            physicsMesh.SetAABB(model.BoundingBox.Min, model.BoundingBox.Max);


            for (int v = 0; v < model.GetVerticesCount(); v++)
            {
                Vector3 vertex = model.GetVertex(v);
                Vector3 normal = model.GetVertexNormal(v);
                Vector3 tangent = model.GetVertexTangent(v);
                if (model.TexCoords == null)
                    model.LoadTexCoordData();
                Vector2 texCoord = model.TexCoords[v].ToVector2();

                physicsMesh.AddVertex(vertex, normal, tangent, texCoord);
            }

            for (int i = 0; i < model.Indices16.Length; i++)
            {
                physicsMesh.AddIndex(model.Indices16[i]);
            }

            for (int i = 0; i < model.GetMeshList().Count; i++)
            {
                var mesh = model.GetMeshList()[i];
                physicsMesh.AddSectionData(mesh.IndexStart, mesh.TriCount, mesh.Material.Name);
            }

            return physicsMesh;
        }

        void CreateBreakableShapeFromCollisionShapes(MyModel model, Vector3 defaultSize, MyPhysicalModelDefinition modelDef)
        {
            // Make box half edge length of the grid so fractured block is smaller than not fractured, also good for compounds
            HkShape shape;
            if (model.HavokCollisionShapes != null && model.HavokCollisionShapes.Length > 0)
            {
                if (model.HavokCollisionShapes.Length > 1)
                {
                    shape = HkListShape.Create(model.HavokCollisionShapes, model.HavokCollisionShapes.Length, HkReferencePolicy.None);
                }
                else
                {
                    shape = model.HavokCollisionShapes[0];
                    shape.AddReference();
                }
            }
            else
            {
                //modelDef.Size * (modelDef.CubeSize == MyCubeSize.Large ? 2.5f : 0.25f)
                shape = new HkBoxShape(defaultSize * 0.5f, MyPerGameSettings.PhysicsConvexRadius);
            }

            var boxBreakable = new HkdBreakableShape(shape);
            boxBreakable.Name = model.AssetName;
            boxBreakable.SetMass(modelDef.Mass);
            model.HavokBreakableShapes = new HkdBreakableShape[] { boxBreakable };
            shape.RemoveReference();
        }

        public void LoadModelDestruction(string modelName, MyPhysicalModelDefinition modelDef, Vector3 defaultSize, bool destructionRequired = true, bool useShapeVolume = false)
        {
            var model = VRage.Game.Models.MyModels.GetModelOnlyData(modelName);

            if (model.HavokBreakableShapes != null) return;

            bool dontCreateFracturePieces = false;
            MyCubeBlockDefinition blockDefinition = modelDef as MyCubeBlockDefinition;
            if (blockDefinition != null)
            {
                dontCreateFracturePieces = !blockDefinition.CreateFracturedPieces;
            }

            var material = modelDef.PhysicalMaterial;

            //var shapeName = modelDef.Id.SubtypeName;
            var shapeName = modelName;

            if (model != null)
            {
                bool forceCollisionsInsteadDestruction = false;

                //if (model.AssetName.Contains("StoneBattlementAdvancedStraightTop"))
                //{
                //}

                model.LoadUV = true;
                HkdBreakableShape bShape;
                bool createPieceData = false;
                bool recalculateMass = false;
                bool registerShape = false;

                //TODO: Dynamic fracturing is workaround. We need to find the way how to serialize fractured model directly into hkt
                //      and then load it from BreakableShapes.
                if (model.ModelFractures != null)
                {
                    if (model.HavokCollisionShapes != null && model.HavokCollisionShapes.Length > 0)
                    {
                        CreateBreakableShapeFromCollisionShapes(model, defaultSize, modelDef);

                        var physicsMesh = CreatePhysicsMesh(model);

                        Storage.RegisterShapeWithGraphics(physicsMesh, model.HavokBreakableShapes[0], shapeName);

                        string modPath = null;

                        if (Path.IsPathRooted(model.AssetName))
                            modPath = model.AssetName.Remove(model.AssetName.LastIndexOf("Models"));

                        FractureBreakableShape(model.HavokBreakableShapes[0], model.ModelFractures, modPath);

                        recalculateMass = true;
                        registerShape = true;
                        createPieceData = true;
                    }
                }
                else
                if (model.HavokDestructionData != null && !forceCollisionsInsteadDestruction)
                {
                    try
                    {
                        //string dump = Storage.DumpDestructionData(model.HavokDestructionData);
                        if (model.HavokBreakableShapes == null) //models are cached between sessions
                        {
                            model.HavokBreakableShapes = Storage.LoadDestructionDataFromBuffer(model.HavokDestructionData);
                            createPieceData = true;
                            recalculateMass = true;
                            registerShape = true;
                        }
                    }
                    catch
                    {
                        model.HavokBreakableShapes = null;
                    }
                }
                model.HavokDestructionData = null; //we dont need to hold the byte data after loading shape
                model.HavokData = null;

                if (model.HavokBreakableShapes == null && destructionRequired)
                {
                    MyLog.Default.WriteLine(model.AssetName + " does not have destruction data");

                    CreateBreakableShapeFromCollisionShapes(model, defaultSize, modelDef);

                    recalculateMass = true;
                    registerShape = true;

                    if (MyFakes.SHOW_MISSING_DESTRUCTION && destructionRequired)
                    {
                        //Show missing destructions in pink
                        VRageRender.MyRenderProxy.ChangeModelMaterial(model.AssetName, "Debug");
                    }
                }
                    
                if (model.HavokBreakableShapes == null)
                {
                    MyLog.Default.WriteLine(string.Format("Model {0} - Unable to load havok destruction data", model.AssetName), LoggingOptions.LOADING_MODELS);
                    return;
                }

                System.Diagnostics.Debug.Assert(model.HavokBreakableShapes.Length > 0, "Incomplete destruction data");
                bShape = model.HavokBreakableShapes[0];

                //bShape.GetChildren(m_tmpChildrenList);
                //if (m_tmpChildrenList.Count == 0)
                //    bShape.UserObject = (uint)HkdBreakableShape.Flags.FRACTURE_PIECE;

                if (dontCreateFracturePieces)
                    bShape.SetFlagRecursively(HkdBreakableShape.Flags.DONT_CREATE_FRACTURE_PIECE);
                    
                //m_tmpChildrenList.Clear();

                if (registerShape)
                {
                    bShape.AddReference();

                    Storage.RegisterShape(
                                bShape,
                                shapeName
                            );
                }

                // Necessary, otherwise materials on fractures would be missing
                VRageRender.MyRenderProxy.PreloadMaterials(model.AssetName);

                if (createPieceData)
                    CreatePieceData(model, bShape);

                if (recalculateMass)
                {
                    var volume = bShape.CalculateGeometryVolume();
                    if (volume <= 0 || useShapeVolume)
                        volume = bShape.Volume;
                    var realMass = volume * material.Density;

                    System.Diagnostics.Debug.Assert(realMass > 0, "Invalid mass data");

                    bShape.SetMassRecursively(MyDestructionHelper.MassToHavok(realMass));
                }

                if(modelDef.Mass > 0)
                {
                    bShape.SetMassRecursively(MyDestructionHelper.MassToHavok(modelDef.Mass));
                }
                //Debug.Assert(CheckVolumeMassRec(bShape, 0.00001f, 0.01f), "Low volume or mass." + bShape.Name);
                DisableRefCountRec(bShape);

                if (MyFakes.CHANGE_BLOCK_CONVEX_RADIUS)
                {
                    if (model != null && model.HavokBreakableShapes != null)
                    {
                        var shape = model.HavokBreakableShapes[0].GetShape();
                        if (shape.ShapeType != HkShapeType.Sphere && shape.ShapeType != HkShapeType.Capsule)
                            SetConvexRadius(model.HavokBreakableShapes[0], MyDestructionConstants.LARGE_GRID_CONVEX_RADIUS);
                    }
                }

                if (MyFakes.LAZY_LOAD_DESTRUCTION)
                    BlockShapePool.AllocateForDefinition(shapeName, modelDef, MyBlockShapePool.PREALLOCATE_COUNT);
            }
            else
            {
                //No armor in ME!
            }
        }

        private void SetConvexRadius(HkdBreakableShape bShape, float radius)
        {
            var sh = bShape.GetShape();
            if (sh.IsConvex)
            {
                var convex = (HkConvexShape)sh;
                if(convex.ConvexRadius > radius)
                    convex.ConvexRadius = radius;
                return;
            }
            if (sh.IsContainer())
            {
                HkShapeContainerIterator container = sh.GetContainer();
                while (container.IsValid)
                {
                    if (container.CurrentValue.IsConvex)
                    {
                        var convex = (HkConvexShape)container.CurrentValue;
                        if (convex.ConvexRadius > radius)
                            convex.ConvexRadius = radius;
                    }
                    container.Next();
                }
            }
        }

        private bool CheckVolumeMassRec(HkdBreakableShape bShape, float minVolume, float minMass)
        {
            if (bShape.Name.Contains("Fake"))
                return true;
            if (bShape.Volume <= minVolume)
                return false;
            HkMassProperties mp = new HkMassProperties();
            bShape.BuildMassProperties(ref mp);
            if (mp.Mass <= minMass)
                return false;
            if (mp.InertiaTensor.M11 == 0 || mp.InertiaTensor.M22 == 0 || mp.InertiaTensor.M33 == 0)
                return false;
            for (int i = 0; i < bShape.GetChildrenCount(); i++)
            {
                if (!CheckVolumeMassRec(bShape.GetChildShape(i), minVolume, minMass))
                    return false;
            }
            return true;
        }

        public static MyPhysicalMaterialDefinition GetPhysicalMaterial(MyPhysicalModelDefinition modelDef, string physicalMaterial)
        {
            if (m_physicalMaterials == null)
            {
                m_physicalMaterials = new Dictionary<string, MyPhysicalMaterialDefinition>();
                foreach (var physMat in MyDefinitionManager.Static.GetPhysicalMaterialDefinitions())
                    m_physicalMaterials.Add(physMat.Id.SubtypeName, physMat);

                m_physicalMaterials["Default"] = new MyPhysicalMaterialDefinition()
                {
                    Density = 1920,
                    HorisontalTransmissionMultiplier = 1,
                    HorisontalFragility = 2,
                    CollisionMultiplier = 1.4f,
                    SupportMultiplier = 1.5f,
                };
            }

            if (!string.IsNullOrEmpty(physicalMaterial))
            {
                if (m_physicalMaterials.ContainsKey(physicalMaterial))
                    return m_physicalMaterials[physicalMaterial];
                else
                {
                    string s = "ERROR: Physical material " + physicalMaterial + " does not exist!";
                    System.Diagnostics.Debug.Fail(s);
                    MyLog.Default.WriteLine(s);
                }
            }

            //MyLog.Default.WriteLine("WARNING: " + modelDef.Id.SubtypeName + " has no physical material specified, trying to autodetect from name");


            if (modelDef.Id.SubtypeName.Contains("Stone") && m_physicalMaterials.ContainsKey("Stone"))
            {
                return m_physicalMaterials["Stone"];
            }

            if (modelDef.Id.SubtypeName.Contains("Wood") && m_physicalMaterials.ContainsKey("Wood"))
            {
                return m_physicalMaterials["Wood"];
            }

            if (modelDef.Id.SubtypeName.Contains("Timber") && m_physicalMaterials.ContainsKey("Timber"))
            {
                return m_physicalMaterials["Wood"];
            }


            //MyLog.Default.WriteLine("WARNING: Unable to find proper physical material for " + modelDef.Id.SubtypeName + ", using Default");
            return m_physicalMaterials["Default"];
        }

        private void DisableRefCountRec(HkdBreakableShape bShape)
        {
            bShape.DisableRefCount();
            var lst = new List<HkdShapeInstanceInfo>();
            bShape.GetChildren(lst);
            foreach (var child in lst)
                DisableRefCountRec(child.Shape);
        }

        private void CreatePieceData(MyModel model, HkdBreakableShape breakableShape)
        {
            //Root shape for fractured compound blocks
            {
                var msg = VRageRender.MyRenderProxy.PrepareAddRuntimeModel();
                ProfilerShort.Begin("GetDataFromShape");
                m_tmpMesh.Data = msg.ModelData;
                MyDestructionData.Static.Storage.GetDataFromShape(breakableShape, m_tmpMesh);
                System.Diagnostics.Debug.Assert(msg.ModelData.Sections.Count > 0, "Invalid data");
                if (msg.ModelData.Sections.Count > 0)
                {
                    if(MyFakes.USE_HAVOK_MODELS)
                        msg.ReplacedModel = model.AssetName;
                    VRageRender.MyRenderProxy.AddRuntimeModel(breakableShape.ShapeName, msg);
                }
                ProfilerShort.End();
            }

            using (m_tmpChildrenList.GetClearToken())
            {
                breakableShape.GetChildren(m_tmpChildrenList);
                LoadChildrenShapes(m_tmpChildrenList);
            }
        }

        private static void LoadChildrenShapes(List<HkdShapeInstanceInfo> children)
        {
            foreach (var shapeInstanceInfo in children)
            {
                System.Diagnostics.Debug.Assert(shapeInstanceInfo.IsValid(), "Invalid shapeInstanceInfo!");
                if (shapeInstanceInfo.IsValid())
                {
                    var msg = VRageRender.MyRenderProxy.PrepareAddRuntimeModel();
                    ProfilerShort.Begin("GetDataFromShape");
                    m_tmpMesh.Data = msg.ModelData;
                    MyDestructionData.Static.Storage.GetDataFromShapeInstance(shapeInstanceInfo, m_tmpMesh);
                    m_tmpMesh.Transform(shapeInstanceInfo.GetTransform());

                    System.Diagnostics.Debug.Assert(msg.ModelData.Sections.Count > 0, "Invalid data");
                    if (msg.ModelData.Sections.Count > 0)
                    {
                        VRageRender.MyRenderProxy.AddRuntimeModel(shapeInstanceInfo.ShapeName, msg);
                    }
                    ProfilerShort.End();
                    var list = new List<HkdShapeInstanceInfo>();
                    shapeInstanceInfo.GetChildren(list);
                    LoadChildrenShapes(list);
                }
            }
        }

        public float GetBlockMass(string model, MyCubeBlockDefinition def)
        {
            var sh = BlockShapePool.GetBreakableShape(model, def);
            var mass = sh.GetMass();
            BlockShapePool.EnqueShape(model, def.Id, sh);
            return mass;    // (OM) NOTE: this currently returns havok mass, we use MyDestructionHelper.MassFromHavok to recompute, if you change to use it here, check this method usage, whether this is not already converted somewhere
        }
    }
}
