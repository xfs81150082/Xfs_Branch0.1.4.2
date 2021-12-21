﻿using System;
using System.Collections.Generic;

namespace Xfs
{
    [XfsObjectSystem]
    public class NumericWatcherComponentAwakeSystem : XfsAwakeSystem<XfsNumericWatcherComponent>
    {
        public override void Awake(XfsNumericWatcherComponent self)
        {
            self.Awake();
        }
    }

    [XfsObjectSystem]
    public class NumericWatcherComponentLoadSystem : XfsLoadSystem<XfsNumericWatcherComponent>
    {
        public override void Load(XfsNumericWatcherComponent self)
        {
            self.Load();
        }
    }

    /// <summary>
    /// 监视数值变化组件,分发监听
    /// </summary>
    public class XfsNumericWatcherComponent : XfsComponent
	{
		private Dictionary<XfsNumericType, List<IXfsNumericWatcher>>? allWatchers;

		public void Awake()
		{
			this.Load();
		}

		public void Load()
		{
            this.allWatchers = new Dictionary<XfsNumericType, List<IXfsNumericWatcher>>();

            List<Type> types = XfsGame.EventSystem.GetTypes(typeof(XfsNumericWatcherAttribute));
            foreach (Type type in types)
            {
                object[] attrs = type.GetCustomAttributes(typeof(XfsNumericWatcherAttribute), false);

                foreach (object attr in attrs)
                {
                    XfsNumericWatcherAttribute numericWatcherAttribute = (XfsNumericWatcherAttribute)attr;
                    IXfsNumericWatcher? obj = Activator.CreateInstance(type) as  IXfsNumericWatcher;
                    if (!this.allWatchers.ContainsKey(numericWatcherAttribute.NumericType))
                    {
                        this.allWatchers.Add(numericWatcherAttribute.NumericType, new List<IXfsNumericWatcher>());
                    }
                    if (obj != null)
                    {
                        this.allWatchers[numericWatcherAttribute.NumericType].Add(obj);
                    }
                }
            }
        }

        public void Run(XfsNumericType numericType, long id, int value)
        {
            List<IXfsNumericWatcher>? list;
            if (this.allWatchers != null && this.allWatchers.TryGetValue(numericType, out list))
            {
                foreach (IXfsNumericWatcher numericWatcher in list)
                {
                    numericWatcher.Run(id, value);
                }
            }
        }


	}
}