using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xfs;

namespace XfsClient
{
    public class XfsClientInit : XfsComponent
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
                XfsDLLType dllType2 = XfsDLLType.XfsClient;
                Assembly assembly1 = XfsDllHelper.GetAssembly(dllType1.ToString());
                Assembly assembly2 = XfsDllHelper.GetAssembly(dllType2.ToString());
                XfsGame.EventSystem.Add(dllType1, assembly1);
                XfsGame.EventSystem.Add(dllType2, assembly2);


                ///服务器加载组件
                XfsGame.XfsSence.Type = XfsSenceType.XfsClient;
                XfsGame.XfsSence.AddComponent<XfsTimerComponent>();
                
                XfsGame.XfsSence.AddComponent<XfsStartConfigComponent>();                           ///加载组件 : 信息组件
                XfsGame.XfsSence.AddComponent<XfsOpcodeTypeComponent>();                            ///加载组件 : 操作号码
                XfsGame.XfsSence.AddComponent<XfsMessageDispatcherComponent>();                     ///加载组件 : 信息分检组件
                XfsGame.XfsSence.AddComponent<XfsNetOuterComponent>();                              ///加载组件 : 通信组件


                //XfsGame.XfsSence.AddComponent<NetInnerComponent, string>(innerConfig.Address);  //// 内网消息组件
                //XfsGame.XfsSence.AddComponent<XfsMessageHandlerComponent>();                      ///加载组件 : 信息处理组件


                XfsGame.XfsSence.AddComponent<XfsTest>();                                           ///加载组件 : 测试组件


                Console.WriteLine(XfsTimeHelper.CurrentTime() + " ThreadId: " + Thread.CurrentThread.ManagedThreadId);
                Console.WriteLine(XfsTimeHelper.CurrentTime() + " 客户端配置完成： " + XfsGame.XfsSence.Type);

                while (true)
                {
                    try
                    {
                        Thread.Sleep(1);
                        //XfsOneThreadSynchronizationContext.Instance.Update();
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
