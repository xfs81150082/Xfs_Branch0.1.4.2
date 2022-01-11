﻿using System.Collections.Generic;
using System.Net;

namespace Xfs
{
	public static class XfsNetHelper
	{
		public static string[] GetAddressIPs()
		{
			//获取本地的IP地址
			List<string> addressIPs = new List<string>();
			foreach (IPAddress address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
			{
				addressIPs.Add(address.ToString());
			}
			return addressIPs.ToArray();
		}

	}
}
