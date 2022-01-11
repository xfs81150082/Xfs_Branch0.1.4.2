using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xfs;

namespace XfsServer
{
    [XfsObjectSystem]
    public class XfsClass1StartSystem : XfsStartSystem<Test11Class1>
    {
        public override void Start(Test11Class1 self)
        {
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " Class1 Start 运行成功: " + self.test1);
        }
    }
}
