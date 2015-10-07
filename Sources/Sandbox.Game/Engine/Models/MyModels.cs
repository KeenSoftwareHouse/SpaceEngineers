#define USE_SERIAL_MODEL_LOAD

using Sandbox.Game.World;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using System.Threading.Tasks;
using System.Threading;
using System;
using Sandbox.Engine.Utils;
using Sandbox;
using Sandbox.Common;
using System.Diagnostics;
using Sandbox.Definitions;
using VRage;
using VRage.Import;
using VRage.Collections;

namespace Sandbox.Engine.Models
{
    public static partial class MyModels
    {
        static MyConcurrentDictionary<string, MyModel> m_models = new MyConcurrentDictionary<string, MyModel>();

        /// <summary>
        /// Event that occures when some model needs to be loaded.
        /// </summary>
        private static readonly AutoResetEvent m_loadModelEvent;


        static MyModels()
        {
            m_loadModelEvent = new AutoResetEvent(false);
        }

        public static void LoadData()
        {
            VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyModels.LoadData");
            MySandboxGame.Log.WriteLine(string.Format("MyModels.LoadData - START"));

            MySandboxGame.Log.WriteLine("Pre-caching cube block models");
            foreach (var definition in MyDefinitionManager.Static.GetAllDefinitions())
            {
                var cubeBlockDefinition = definition as MyCubeBlockDefinition;
                if (cubeBlockDefinition == null)
                    continue;

                if (cubeBlockDefinition.Model == null)
                    continue;

                MyModel model = MyModels.GetModelOnlyData(cubeBlockDefinition.Model);
                model.CheckLoadingErrors(definition.Context);

                foreach (var models in cubeBlockDefinition.BuildProgressModels)
                {
                    var buildProgressModel =  MyModels.GetModelOnlyData(models.File);
                    buildProgressModel.CheckLoadingErrors(definition.Context);
                }
            }

            MySandboxGame.Log.WriteLine(string.Format("MyModels.LoadData - END"));
            VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        }

        public static void UnloadData()
        {
            foreach (var model in GetLoadedModels())
            {
                model.UnloadData();
            }
            m_models.Clear();
        }

        //  Lazy-loading and then returning reference to model
        //  Doesn't load vertex/index shader and doesn't touch GPU. Use it when you need model data - vertex, triangles, octre...
        public static MyModel GetModelOnlyData(string modelAsset)
        {
            if (string.IsNullOrEmpty(modelAsset))
                return null;

            MyModel model;
            if (!m_models.TryGetValue(modelAsset, out model))
            {
                model = new MyModel(modelAsset);
                m_models[modelAsset] = model;
            }

            model.LoadData();
            return model;
        }

        //  Lazy-loading and then returning reference to model
        public static MyModel GetModelOnlyAnimationData(string modelAsset)
        {
            MyModel model;
            if (!m_models.TryGetValue(modelAsset, out model))
            {
                model = new MyModel(modelAsset);
                m_models[modelAsset] = model;
            }

            model.LoadAnimationData();
            return model;
        }


        //  Lazy-loading and then returning reference to model
        //  Doesn't load vertex/index shader and doesn't touch GPU. Use it when you need model data - vertex, triangles, octre...
        public static MyModel GetModelOnlyDummies(string modelAsset)
        {
            MyModel model;
            if (!m_models.TryGetValue(modelAsset, out model))
            {
                model = new MyModel(modelAsset);
                m_models[modelAsset] = model;
            }

            model.LoadOnlyDummies();
            return model;
        }

        public static MyModel GetModelOnlyModelInfo(string modelAsset)
        {
            Debug.Assert(modelAsset != null);
            MyModel model;
            if (!m_models.TryGetValue(modelAsset, out model))
            {
                model = new MyModel(modelAsset);
                m_models[modelAsset] = model;
            }

            model.LoadOnlyModelInfo();
            return model;

        }

        public static MyModel GetModel(string modelAsset)
        {
            if (modelAsset == null)
                return null;

            MyModel model;
            if (m_models.TryGetValue(modelAsset, out model))
            {
                return model;
            }
            return null;
       }

        public static List<MyModel> GetLoadedModels()
        {
            var list = new List<MyModel>();
            m_models.GetValues(list);
            return list;
        }
    }
}