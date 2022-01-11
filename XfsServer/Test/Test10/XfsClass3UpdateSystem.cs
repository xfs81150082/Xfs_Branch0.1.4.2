using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xfs;

namespace XfsServer
{
    [XfsObjectSystem]
    public class XfsClass3UpdateSystem : XfsUpdateSystem<Test13Class3>
    {
        public override void Update(Test13Class3 self)
        {
            //Console.WriteLine(XfsTimerTool.CurrentTime() + " Class3 Update: " + self.test3);
        }
    }
}
