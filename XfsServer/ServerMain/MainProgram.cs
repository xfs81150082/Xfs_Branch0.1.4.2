using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xfs;

namespace XfsServer
{
    class MainProgram
    {
        static void Main(string[] args)
        {
            XfsComponentFactory.Create<XfsServerInit>().Start();


        }
    }
}