using System;
using System.Diagnostics;
using VRage.Generics;
using VRage.Render11.Common;

namespace VRage.Render11.Resources
{
    [DebuggerDisplay("Name = {Name}")]
    internal abstract class MyPersistentResourceBase<TDescription> : IMyPersistentResource<TDescription>
    {
        string m_name;
        protected TDescription m_description;

        internal void Init(string name, ref TDescription desc)
        {
            m_name = name;
            m_description = CloneDescription(ref desc);
        }

        public void ChangeDescription(ref TDescription desc)
        {
            TDescription description = CloneDescription(ref desc);
            ChangeDescriptionInternal(ref desc);
            m_description = description;
        }

        protected abstract void ChangeDescriptionInternal(ref TDescription desc);

        // Default is fine for immutable value types
        protected virtual TDescription CloneDescription(ref TDescription desc) { return desc; }

        internal abstract void OnDeviceInit();

        internal abstract void OnDeviceEnd();

        public string Name
        {
            get { return m_name; }
        }

        public TDescription Description
        {
            get { return m_description; }
        }
    }

    internal abstract class MyPersistentResource<TResource, TDescription> : MyPersistentResourceBase<TDescription>
        where TResource : IDisposable
    {
        TResource m_resource;
        bool m_isInit = false;

        protected sealed override void ChangeDescriptionInternal(ref TDescription description)
        {
            if (!m_isInit)
                return;

            // Don't dispose existing resource, may still be in use
            m_resource = CreateResource(ref description);
        }

        internal sealed override void OnDeviceInit()
        {
            m_resource = CreateResource(ref m_description);
            m_isInit = true;
        }

        protected abstract TResource CreateResource(ref TDescription desc);

        internal sealed override void OnDeviceEnd()
        {
            m_resource.Dispose();
            m_isInit = false;
        }

        public TResource Resource
        {
            get { return m_resource; }
        }
    }

    internal abstract class MyPersistentResourceManager<TResource, TDescription> : IManager, IManagerDevice
        where TResource : MyPersistentResourceBase<TDescription>, new()
    {
        bool m_isInit;

        MyObjectsPool<TResource> m_objectsPool;

        protected TResource CreateResource(string name, ref TDescription desc)
        {
            TResource resource;
            m_objectsPool.AllocateOrCreate(out resource);
            resource.Init(name, ref desc);

            if (m_isInit)
                resource.OnDeviceInit();

            return resource;
        }

        protected abstract int GetAllocResourceCount();

        public MyPersistentResourceManager()
        {
            m_objectsPool = new MyObjectsPool<TResource>(GetAllocResourceCount());
        }

        public int GetResourcesCount()
        {
            return m_objectsPool.ActiveCount;
        }

        public virtual void OnDeviceInit()
        {
            m_isInit = true;

            foreach (TResource res in m_objectsPool.Active)
                res.OnDeviceInit();
        }

        public virtual void OnDeviceReset()
        {
            foreach (TResource res in m_objectsPool.Active)
            {
                res.OnDeviceEnd();
                res.OnDeviceInit();
            }
        }

        public virtual void OnDeviceEnd()
        {
            m_isInit = false;

            foreach (TResource res in m_objectsPool.Active)
                res.OnDeviceEnd();
        }
    }

    internal interface IMyPersistentResource<TDescription>
    {
        void ChangeDescription(ref TDescription desc);
        string Name { get; }
        TDescription Description { get; }
    }
}
