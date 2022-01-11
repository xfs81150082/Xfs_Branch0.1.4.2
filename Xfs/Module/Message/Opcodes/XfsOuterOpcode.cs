using System;

namespace Xfs
{
    #region
    [XfsMessage(XfsOuterOpcode.C2S_TestRequest)]
	public partial class C2S_TestRequest : IXfsRequest { }

    [XfsMessage(XfsOuterOpcode.S2C_TestResponse)]
	public partial class S2C_TestResponse : IXfsResponse { }

    [XfsMessage(XfsOuterOpcode.C4S_Ping)]
    public partial class C4S_Ping : IXfsRequest { }

    [XfsMessage(XfsOuterOpcode.S4C_Ping)]
    public partial class S4C_Ping : IXfsResponse { }
   
    [XfsMessage(XfsOuterOpcode.C4S_Heart)]
    public partial class C4S_Heart : IXfsRequest { }

    [XfsMessage(XfsOuterOpcode.S4C_Heart)]
    public partial class S4C_Heart : IXfsResponse { }
   
    [XfsMessage(XfsOuterOpcode.C4S_User)]
    public partial class C4S_User : IXfsRequest { }

    [XfsMessage(XfsOuterOpcode.S4C_User)]
    public partial class S4C_User : IXfsResponse { }







    #endregion


}
namespace Xfs
{
    public static partial class XfsOuterOpcode
    {
        #region
        public const int C2S_TestRequest = 101;
        public const int S2C_TestResponse = 102;

        public const int C4S_Ping = 111;
        public const int S4C_Ping = 112;

        public const int C4S_Heart = 141;
        public const int S4C_Heart = 142;

        public const int C4S_User = 143;
        public const int S4C_User = 144;







        #endregion


    }
}