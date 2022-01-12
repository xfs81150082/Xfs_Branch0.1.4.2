using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xfs
{
    [XfsObjectSystem]
    public class XfsSessionPoolComponentAwakeSytem : XfsAwakeSystem<XfsSessionPoolComponent,int>
    {    
        public override void Awake(XfsSessionPoolComponent self, int a)
        {
            self.Init(a);
        }
    }
}
