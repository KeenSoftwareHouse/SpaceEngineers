﻿using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.Components
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MyEntityComponentDescriptor : System.Attribute
    {
        public Type EntityBuilderType;

        public MyEntityComponentDescriptor(Type entityBuilderType)
        {
            EntityBuilderType = entityBuilderType;
        }
    }

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class MyEntitySubIdComponentDescriptor : System.Attribute
	{
		public string SubIdName;

		public MyEntitySubIdComponentDescriptor(string subIdName)
		{
			SubIdName = subIdName;
		}
	}

    public abstract class MyComponentBase
    {
        public IMyEntity Entity { get { return CurrentContainer != null ? CurrentContainer.Entity : null; } }  // to be obsolete once components are finished
        public MyComponentContainer CurrentContainer { get; private set; }

        public virtual void OnAddedToContainer(MyComponentContainer container)
        {
            CurrentContainer = container;
        }

        public virtual void OnRemovedFromContainer(MyComponentContainer container)
        {

            CurrentContainer = null;
        }
    }
}
