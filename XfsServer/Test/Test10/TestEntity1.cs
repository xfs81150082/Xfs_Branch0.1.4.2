using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xfs;

namespace XfsServer
{
    public class TestEntity1 : XfsEntity
    {
        public TestEntity1()
        {
            this.AddComponent<Test11Class1>();
            this.AddComponent<Test12Class2>();
            this.AddComponent<Test13Class3>();
        }


    }
}
