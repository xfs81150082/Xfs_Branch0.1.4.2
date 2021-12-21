﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xfs;

namespace XfsServer
{
    class Program
    {
        static void Main(string[] args)
        {
            //new XfsGateInit().Start();
            //new TestIocp().Init();

            XfsComponentFactory.Create<XfsServerInit>().Start();

        }

    }
}