using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xfs;

namespace XfsServer
{
    [XfsMessageHandler(XfsSenceType.XfsServer)]
    public class S4C_PingHandler : XfsAMRpcHandler<C4S_Ping, S4C_Ping>
    {
        protected override void Run(XfsSession session, C4S_Ping message, Action<S4C_Ping> reply)
        {
            ///从客户端发来的信息

            Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + this.GetType().Name + "-16" + " : 收到客户端发来的信息。RpcId:" + "(" + message.RpcId + ":" + message.Message + ")");


            //Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + this.GetType().Name + "-16" + " : 收到客户端发来的信息。RpcId:" + "(" + message.RpcId + ":" + message.Message + ")"+" Peer: "+session.RemoteAddress.ToString());
            
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + this.GetType().Name + "-19" + " : 等待5秒后回复信息...");
            XfsGame.XfsSence.GetComponent<XfsTimerComponent>().WaitAsync(5000);
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + this.GetType().Name + "-21" + " : 5秒时间到，发送回复信息。RpcId:" + message.RpcId);

           S4C_Ping response = new S4C_Ping();
            response.RpcId = message.RpcId;
            //response.Message = "我是服务器..." + " Address: " + session.RemoteAddress.ToString();
            response.Message = "我是服务器..." + " Address:111111111 " ;

            ///调用行为代理委托（回调函数）,并传入参数response。
            reply(response);

            Console.WriteLine(XfsTimeHelper.CurrentTime() + " G4C_PingHandler-31, 已发送回消息." );
        }

        ///async XfsVoid SpawnUnit(XfsSession session)
        /// {
        ///    M2T_CreateUnit response = (M2T_CreateUnit) await session.Call(new T2M_CreateUnit() { UnitType = (int)1, RolerId = 2, UnitId = 0 });
        /// }        
        /// 
        //private async XfsVoid XfsVoidTest22()
        //{
        //    await new XfsSession().Call(new C4G_Ping());
        //}


   

    }
}
