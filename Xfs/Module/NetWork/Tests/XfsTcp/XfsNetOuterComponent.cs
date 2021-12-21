namespace Xfs
{
	public class XfsNetOuterComponent : XfsNetSocketComponent
	{
        public override XfsSenceType SenceType => XfsGame.XfsSence.Type;
        public override bool IsServer => XfsGame.XfsSence.IsServer;

    }
}