#define USE_SERIAL_MODEL_LOAD

using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using VRage.Collections;
using VRage.Utils;

namespace VRage.Game.Models
{
    public static class MyModels
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

        // VRAGE TODO: currently it brings in dependencies (MySandboxGame, MyDefinitionErrors)
        //             and it preloads nothing. Commented and to be reconsidered later.
        //             The only caller: MySandboxGame.LoadData().
        //             
        //public static void LoadData()
        //{
        //    VRageRender.MyRenderProxy.GetRenderProfiler().StartProfilingBlock("MyModels.LoadData");
        //    MySandboxGame.Log.WriteLine(string.Format("MyModels.LoadData - START"));

        //    MySandboxGame.Log.WriteLine("Pre-caching cube block models");
        //    foreach (var definition in MyDefinitionManager.Static.GetAllDefinitions())
        //    {
        //        var cubeBlockDefinition = definition as MyCubeBlockDefinition;
        //        if (cubeBlockDefinition == null)
        //            continue;

        //        if (cubeBlockDefinition.Model == null)
        //            continue;

        //        MyModel model = MyModels.GetModelOnlyData(cubeBlockDefinition.Model);
        //        bool newErrorFound;
        //        model.CheckLoadingErrors(definition.Context, out newErrorFound);
        //        if (newErrorFound)
        //            Sandbox.Definitions.MyDefinitionErrors.Add(definition.Context, 
        //                "There was error during loading of model, please check log file.", Sandbox.Definitions.TErrorSeverity.Error);

        //        foreach (var models in cubeBlockDefinition.BuildProgressModels)
        //        {
        //            var buildProgressModel =  MyModels.GetModelOnlyData(models.File);
        //            buildProgressModel.CheckLoadingErrors(definition.Context, out newErrorFound);
        //            if (newErrorFound)
        //                Sandbox.Definitions.MyDefinitionErrors.Add(definition.Context,
        //                    "There was error during loading of model, please check log file.", Sandbox.Definitions.TErrorSeverity.Error);

        //        }
        //    }

        //    MySandboxGame.Log.WriteLine(string.Format("MyModels.LoadData - END"));
        //    VRageRender.MyRenderProxy.GetRenderProfiler().EndProfilingBlock();
        //}

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
        //  Param forceReloadMwm: Reloads MWM even when it is already in cache. Useful when debugging.
        //  May return null on failure.
        public static MyModel GetModelOnlyAnimationData(string modelAsset, bool forceReloadMwm = false)
        {
            MyModel model;
            if (forceReloadMwm || !m_models.TryGetValue(modelAsset, out model))
            {
                model = new MyModel(modelAsset);
                m_models[modelAsset] = model;
            }

            try
            {
                model.LoadAnimationData();
                return model;
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLine(e);
                Debug.Fail("Cannot load asset \"" + modelAsset + "\".\n" + e.Message);
                return null;
            }
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