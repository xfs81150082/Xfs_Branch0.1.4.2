using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xfs;

namespace XfsServer
{
    [XfsObjectSystem]
    public class XfsClass2StartSystem : XfsStartSystem<Test12Class2>
    {
        public override void Start(Test12Class2 self)
        {
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " Class2 Start 运行成功: " + self.test2);
        }
    }
}
