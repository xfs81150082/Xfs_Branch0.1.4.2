using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xfs;

namespace XfsClient
{
    [XfsMessageHandler(XfsSenceType.XfsClient)]
    public class C4S_HeartHandler : XfsAMRpcHandler<C4S_Heart, S4C_Heart>
    {
        protected override void Run(XfsSession session, C4S_Heart message, Action<S4C_Heart> reply)
        {






        }


    }

}
