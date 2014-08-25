/*
 * Created by SharpDevelop.
 * User: skrillac
 * Date: 4/3/2013
 * Time: 5:07 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Net;

namespace netcorlib
{
	/// <summary>
	/// Retrieves the IPv4 Address for the current machine
	/// </summary>
	public class MachineIPAddress
	{
		public static IPAddress FindMachineAddress()
		{
			string strHostName = System.Net.Dns.GetHostName();
            IPAddress[] clientip = System.Net.Dns.GetHostAddresses(strHostName);
            foreach (IPAddress ip in clientip)
            {
                string temp = ip.AddressFamily.ToString();
                if (temp.Equals("InterNetwork"))
                {
                	return ip;
                }
            }

            return IPAddress.Loopback;
		}
	}
}
