using System;
using System.Collections.Generic;

namespace Xfs
{
	public static class XfsSenceTypeHelper
	{
		public static List<XfsSenceType> GetServerTypes()
		{
			List<XfsSenceType> appTypes = new List<XfsSenceType> { XfsSenceType.XfsClient, XfsSenceType.XfsServer };
			return appTypes;
		}

		public static bool Is(this XfsSenceType a, XfsSenceType b)
		{
			if ((a & b) != 0)
			{
				return true;
			}
			return false;
		}

	}
}