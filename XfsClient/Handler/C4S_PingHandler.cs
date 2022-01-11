using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xfs;

namespace XfsClient
{
    [XfsMessageHandler(XfsSenceType.XfsClient)]
    public class C4S_PingHandler : XfsAMRpcHandler<C4S_Ping, S4C_Ping>
    {
        protected override void Run(XfsSession session, C4S_Ping message, Action<S4C_Ping> reply)
        {
            ///从服务端发来的信息
            ///
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " C4S_PingHandler 17. " + message.Message);

            S4C_Ping response = new S4C_Ping();
            response.RpcId = message.RpcId;
            response.Message = "我是客户端..." + message.Message;

            ///调用行为代理委托（回调函数）,并传入参数response。
            reply(response);

            Console.WriteLine(XfsTimeHelper.CurrentTime() + " C4S_PingHandler 27. 已发送回消息.");
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " C4S_PingHandler 28. 已发送回消息."+ XfsTimeHelper.Now());

        }
    }
}
