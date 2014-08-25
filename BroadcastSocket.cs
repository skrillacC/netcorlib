/*
 * Created by SharpDevelop.
 * User: skrillac
 * Date: 4/3/2013
 * Time: 5:08 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Net;
using System.Net.Sockets;

namespace netcorlib
{
	/// <summary>
	/// Information class to be used by the Triangulator class
	/// </summary>
	public class BroadcastSocketArgs
	{
		public Socket BroadcastSend = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		public readonly int BufferSize;
		public readonly int LocalPort;
		public byte[] Buffer;
		public readonly EndPoint WorkingEndpoint;
		internal bool b_trigger = false;
		
		public BroadcastSocketArgs(int port, EndPoint bcastep)
		{
			BufferSize = 256;
			Buffer = new byte[BufferSize];
			LocalPort = port;
			WorkingEndpoint = bcastep;
		}
		
		/// <summary>
		/// Releases all resources used by class
		/// </summary>
		public void Dispose()
		{
			if (BroadcastSend.IsBound)	//if the socket is in use then shutdown
				BroadcastSend.Shutdown(SocketShutdown.Both);
			
			BroadcastSend.Close();
		}
		
		//default constructor will be provided by assembly
	}
	
	/// <summary>
	/// Sets up a Socket for LAN Broadcasting
	/// </summary>
	public class BroadcastSocket
	{
		public static Socket ExportBroadcastSocket(Socket Broadcast)
		{
			Broadcast.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
			return Broadcast;
		}
		public static EndPoint ExportBroadcastEndpoint(int Port)
		{
			return (EndPoint) new IPEndPoint(IPAddress.Broadcast, Port);
		}
	
	}
}
