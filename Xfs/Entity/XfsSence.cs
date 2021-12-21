namespace Xfs
{
    public sealed class XfsSence : XfsEntity
    {
        private bool isServer;
        public bool IsServer
        {
            get
            {
                if (Type == XfsSenceType.XfsClient)
                {
                    this.isServer = false;
                }
                else
                {
                    this.isServer = true;
                }
                return this.isServer;
            }
        }
        public XfsSenceType Type { get; set; }
        public XfsSence() { }
        public string Name { get; set; }
        public XfsSence(long id) : base(id) {  }
        public XfsSence(XfsSenceType type)
        {
            this.Type = type;           
        }
    }

    public enum XfsSenceType
    {
        XfsClient,
        XfsServer,

        //XfsAllServer,
        //XfsNone,
        
        
    }

}