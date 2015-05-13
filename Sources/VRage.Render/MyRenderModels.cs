#define USE_SERIAL_MODEL_LOAD

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System;
using VRage.Import;

namespace VRageRender
{
    partial class MyRenderModels : MyRenderComponentBase
    {
        static Dictionary<string, MyRenderModel> m_modelsByAssetName = new Dictionary<string,MyRenderModel>();
        public static Dictionary<string, MyMaterialDescriptor> Materials = new Dictionary<string, MyMaterialDescriptor>();

        //Enables entity objects to do custom work after content is loaded (ie. device reset)
        public delegate void ContentLoadedDelegate();
        public static event ContentLoadedDelegate OnContentLoaded;

        /// <summary>
        /// Queue of textures to load.
        /// </summary>
        private static readonly ConcurrentQueue<MyRenderModel> m_loadingQueue;

        /// <summary>
        /// Event that occures when some model needs to be loaded.
        /// </summary>
        private static readonly AutoResetEvent m_loadModelEvent;


        public override int GetID()
        {
            return (int)MyRenderComponentID.Models;
        }

        static MyRenderModels()
        {
            m_loadingQueue = new ConcurrentQueue<MyRenderModel>();
            m_loadModelEvent = new AutoResetEvent(false);
            //Task.Factory.StartNew(BackgroundLoader, TaskCreationOptions.LongRunning);

           // InitModels();
        }

        /// <summary>
        /// Backgrounds the loader.
        /// </summary>
        private static void BackgroundLoader()
        {
            while (true)
            {
                try
                {
                    MyRenderModel modelToLoadInDraw;
                    if (m_loadingQueue.TryDequeue(out modelToLoadInDraw))
                    {
                        modelToLoadInDraw.LoadInDraw(Textures.LoadingMode.Immediate);
                    }
                    else
                    {
                        m_loadModelEvent.WaitOne();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // NOTE: This will happend when game is disposed while loading so skip load of this model.
                }
            }
        }

        public static void AddRuntimeModel(string name, MyRenderModel model)
        {
            if (m_modelsByAssetName.ContainsKey(name))
            {
                //System.Diagnostics.Debug.Fail(name + " runtime model is already added");
                return;
            }
            m_modelsByAssetName.Add(name, model);
        }

        internal static void LoadModelInDrawInBackground(MyRenderModel model)
        {          /*
            if (MyFakes.LOAD_MODELS_IMMEDIATELY)
            {
                model.LoadInDraw(Managers.LoadingMode.Immediate);
            }
            else */
            {
                m_loadingQueue.Enqueue(model);
                m_loadModelEvent.Set();
            }
        }

        public static void ReloadData()
        {
            MyRender.GetRenderProfiler().StartProfilingBlock("MyModels.ReloadData");
            MyRender.Log.WriteLine(string.Format("MyModels.ReloadData - START"));

            List<MyRenderModel> loadedModels = new List<MyRenderModel>();
#if USE_SERIAL_MODEL_LOAD
            foreach (var pair in m_modelsByAssetName)
            {
                if (pair.Value.UnloadData())
                    loadedModels.Add(pair.Value);
            }
#else
            Parallel.For(0, m_models.Length, i =>
            {
                if (m_models[i].UnloadData())
                    loadedModels.Add(i);
            });
#endif
            //load only previously loaded models
#if USE_SERIAL_MODEL_LOAD
            foreach (MyRenderModel model in loadedModels)
            {
                model.LoadData();
            }
#else
            Parallel.ForEach(loadedModels, i =>
            {
                m_models[i].LoadData();
            });
#endif

            /*
            if (MyEntities.GetEntities() != null)
            {
                foreach (MyEntity entity in MyEntities.GetEntities())
                {
                    entity.InitDrawTechniques();
                }
            } */

            MyRender.Log.WriteLine(string.Format("MyModels.ReloadData - END"));
            MyRender.GetRenderProfiler().EndProfilingBlock();
        }


        public static void ReloadModels()
        {
            MyRender.GetRenderProfiler().StartProfilingBlock("MyModels.ReloadModels");
            MyRender.Log.WriteLine(string.Format("MyModels.ReloadModels - START"));

            List<MyRenderModel> loadedModels = new List<MyRenderModel>();

            foreach (var pair in m_modelsByAssetName)
            {
                if (pair.Value.UnloadData())
                    loadedModels.Add(pair.Value);
            }

            //load only previously loaded models
            foreach (MyRenderModel model in loadedModels)
            {
                model.UnloadData();
                model.LoadData();
                model.LoadContent();
            }

            MyRender.Log.WriteLine(string.Format("MyModels.ReloadModels - END"));
            MyRender.GetRenderProfiler().EndProfilingBlock();
        }

        public override void LoadContent()
        {
            MyRender.Log.WriteLine("MyModels.LoadContent() - START");
            MyRender.Log.IncreaseIndent();
            MyRender.GetRenderProfiler().StartProfilingBlock("MyModels::LoadContent");

            if (OnContentLoaded != null)
                OnContentLoaded();

            foreach (var pair in m_modelsByAssetName)
            {
                if (pair.Value != null)
                {
                    pair.Value.LoadContent();
                }
            }

            MyRender.GetRenderProfiler().EndProfilingBlock();
            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyModels.LoadContent() - END");
        }

        public override void UnloadContent()
        {
            foreach (var pair in m_modelsByAssetName)
            {
                if (pair.Value != null)
                {
                    pair.Value.UnloadContent();
                }
            }

            foreach (var pair in m_modelsByAssetName)
            {
                if (pair.Value != null)
                {
                    pair.Value.UnloadData();
                }
            }

            Materials.Clear();
        }


        /*
        //	Special method that loads data into GPU, and can be called only from Draw method, never from LoadContent or from background thread.
        //	Because that would lead to empty vertex/index buffers if they are filled/created while game is minimized 
        //  (remember the issue - alt-tab during loading screen)
        public static void LoadInDraw()
        {
            MyRender.Log.WriteLine("MyModels.LoadInDraw - START");
            MyRender.Log.IncreaseIndent();

            for (int i = 0; i < m_models.Length; i++)
            {
                //  Because this LoadInDraw will stop normal update calls, we might not be able to send keep alive
                //  messages to server for some time. This will help it - it will make networking be up-to-date.
                //MyClientServer.Update();

                MyModel model = m_models[i];
                if (model != null)
                {
                    //  We can call this on every model because MyModel.LoadInDraw checks if model's data
                    //  were loaded, and that can happen only if some phys object used it for its initialization
                    //  Summary: here only models that are used by some phys object are loaded
                    model.LoadInDraw();
                }
            }

            MyRender.Log.DecreaseIndent();
            MyRender.Log.WriteLine("MyModels.LoadInDraw - END");
        } */

        public static void UnloadExcept(HashSet<string> keepModels)
        {
            foreach (var pair in m_modelsByAssetName)
            {
                if (pair.Value != null /*&& !m_models[i].KeepInMemory*/ && !keepModels.Contains(pair.Key))
                {
                    // Unload data which was previously loaded
                    pair.Value.UnloadContent();
                    if (pair.Value.UnloadData())
                    {
                        MyRender.Log.WriteLine("Unloading model: " + pair.Value.AssetName);
                    }
                }
            }
        }
              /*
        //  Lazy-loading and then returning reference to model
        //  Doesn't load vertex/index shader and doesn't touch GPU. Use it when you need model data - vertex, triangles, octre...
        public static MyModel GetModelOnlyData(MyModelsEnum modelEnum)
        {
            int modelInt = (int)modelEnum;
            m_models[modelInt].LoadData();
            return m_models[modelInt];
        }

        //  Lazy-loading and then returning reference to model
        //  Doesn't load vertex/index shader and doesn't touch GPU. Use it when you need model data - vertex, triangles, octre...
        public static MyModel GetModelOnlyDummies(MyModelsEnum modelEnum)
        {
            int modelInt = (int)modelEnum;
            m_models[modelInt].LoadOnlyDummies();
            return m_models[modelInt];
        }

        public static MyModel GetModelOnlyModelInfo(MyModelsEnum modelEnum)
        {
            int modelInt = (int)modelEnum;
            m_models[modelInt].LoadOnlyModelInfo();
            return m_models[modelInt];
        }  */
   
        public static MyRenderModel GetModel(string assetName)
        {
            MyRenderModel model;
            m_modelsByAssetName.TryGetValue(assetName, out model);

            if (model == null)
            {
                model = new MyRenderModel(assetName, MyMeshDrawTechnique.MESH);
                m_modelsByAssetName.Add(assetName, model);
            }


            model.LoadData();
            model.LoadContent();

            return model;
        }

        public static MyRenderModel UnloadModel(string assetName)
        {
            MyRenderModel model;
            if (m_modelsByAssetName.TryGetValue(assetName, out model))
            {
                model.UnloadContent();
                model.UnloadData();
            }

            return model;
        }

        public static MyRenderModel GetMaterials(string assetName)
        {
            MyRenderModel model;
            m_modelsByAssetName.TryGetValue(assetName, out model);

            if (model == null)
            {
                model = new MyRenderModel(assetName, MyMeshDrawTechnique.MESH);
            }


            model.LoadMaterials();

            return model;
        }
    }
}