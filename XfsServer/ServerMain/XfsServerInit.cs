using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xfs;

namespace XfsServer
{
    public class XfsServerInit : XfsComponent
    {
        //程序启动入口
        public void Start()
        {      
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " ... ");              
            Thread.Sleep(1);
            Console.WriteLine(XfsTimeHelper.CurrentTime() + " ThreadId: " + Thread.CurrentThread.ManagedThreadId);
         
            // 异步方法全部会回掉到主线程
            //SynchronizationContext.SetSynchronizationContext(OneThreadSynchronizationContext.Instance);

            try
            {
                XfsDLLType dllType1 = XfsDLLType.Xfs;
                XfsDLLType dllType2 = XfsDLLType.XfsServer;
                Assembly assembly1 = XfsDllHelper.GetAssembly(dllType1.ToString());
                Assembly assembly2 = XfsDllHelper.GetAssembly(dllType2.ToString());
                XfsGame.EventSystem.Add(dllType1, assembly1);
                XfsGame.EventSystem.Add(dllType2, assembly2);


                ///服务器加载组件
                XfsGame.XfsSence.Type = XfsSenceType.XfsServer;
                XfsGame.XfsSence.AddComponent<XfsTimerComponent>();
                
                XfsGame.XfsSence.AddComponent<XfsStartConfigComponent>();                         ///加载组件 : 操作号码
                XfsGame.XfsSence.AddComponent<XfsOpcodeTypeComponent>();                          ///加载组件 : 信息组件
                XfsGame.XfsSence.AddComponent<XfsMessageDispatcherComponent>();                   ///加载组件 : 信息分检组件
                XfsGame.XfsSence.AddComponent<XfsNetOuterComponent>();                            ///加载组件 : 通信组件-外网消息组件


                //XfsGame.XfsSence.AddComponent<NetInnerComponent, string>(innerConfig.Address);  //// 内网消息组件
                //XfsGame.XfsSence.AddComponent<XfsMessageHandlerComponent>();                    ///加载组件 : 信息处理组件


                XfsGame.XfsSence.AddComponent<XfsServerTest>();                                       ///服务器加载组件 : 通信组件Server
                

                //XfsGame.XfsSence.AddComponent<TestEntity1>();                                     ///服务器加载组件 : 通信组件Server


                //XfsGame.XfsSence.AddComponent<XfsConfigComponent>();        //// 配置管理
                //OuterConfig outerConfig = XfsGame.XfsSence.GetComponent<XfsStartConfigComponent>().GetComponent<OuterConfig>();                // 根据不同的AppType添加不同的组件
                //InnerConfig innerConfig = XfsGame.XfsSence.GetComponent<XfsStartConfigComponent>().GetComponent<InnerConfig>();
                //ClientConfig clientConfig = XfsGame.XfsSence.GetComponent<XfsStartConfigComponent>().GetComponent<ClientConfig>();

                //// 发送普通actor消息
                //XfsGame.XfsSence.AddComponent<ActorMessageSenderComponent>();
                //// 发送location actor消息
                //XfsGame.XfsSence.AddComponent<ActorLocationSenderComponent>();
                //// location server需要的组件
                //XfsGame.XfsSence.AddComponent<LocationComponent>();
                //// 访问location server的组件
                //XfsGame.XfsSence.AddComponent<LocationProxyComponent>();
                //// 这两个组件是处理actor消息使用的
                //XfsGame.XfsSence.AddComponent<MailboxDispatcherComponent>();
                //XfsGame.XfsSence.AddComponent<ActorMessageDispatcherComponent>();

                //// manager server组件，用来管理其它进程使用
                //XfsGame.XfsSence.AddComponent<AppManagerComponent>();
                //XfsGame.XfsSence.AddComponent<GateSessionKeyComponent>();                





                Console.WriteLine(XfsTimeHelper.CurrentTime() + " ThreadId: " + Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 服务器配置完成： " + XfsGame.XfsSence.Type);

                while (true)
                {
                    try
                    {
                        Thread.Sleep(1);
                        XfsOneThreadSynchronizationContext.Instance.Update();
                        XfsGame.EventSystem.Update();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + e);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " : " + e);
            }
        }



    }
}
