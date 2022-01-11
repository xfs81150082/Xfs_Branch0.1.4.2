using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xfs;

namespace Xfs
{
    [XfsMessageHandler(XfsSenceType.XfsClient)]
    public class C4S_HeartHandler : XfsAMRpcHandler<C4S_Heart, S4C_Heart>
    {
        protected override void Run(XfsSession session, C4S_Heart message, Action<S4C_Heart> reply)
        {
            session.GetComponent<XfsHeartComponent>().CdCount = 0;

            Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + this.GetType() + " 17. session: " + session.Socket.LocalEndPoint);
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " " + this.GetType() + " 18. CdCount: " + session.GetComponent<XfsHeartComponent>().CdCount);
        }


    }
}
