using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xfs;

namespace XfsClient
{
    public class C4S_PingHandler : XfsAMRpcHandler<C4S_Ping, S4C_Ping>
    {
        protected override void Run(XfsSession session, C4S_Ping message, Action<S4C_Ping> reply)
        {



            Console.WriteLine(XfsTimeHelper.CurrentTime() + " C4G_PingHandler-17: " + message.Message);

        }
    }
}
