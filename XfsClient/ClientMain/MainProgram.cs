using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xfs;

namespace XfsClient
{
    class MainProgram
    {
        static void Main(string[] args)
        {
            XfsComponentFactory.Create<XfsClientInit>().Start();


        }
    }
}
