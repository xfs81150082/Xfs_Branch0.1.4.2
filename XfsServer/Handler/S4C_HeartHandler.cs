using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xfs;

namespace XfsServer
{
    [XfsMessageHandler(XfsSenceType.XfsServer)]
    public class S4C_HeartHandler : XfsAMRpcHandler<C4S_Heart, S4C_Heart>
    {
        protected override void Run(XfsSession session, C4S_Heart message, Action<S4C_Heart> reply)
        {






        }


    }

}
