/*
 * Created by SharpDevelop.
 * User: skrillac
 * Date: 4/3/2013
 * Time: 5:09 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace netcorlib
{
	
	/// <summary>
	/// Information class to be used by RecvBroadcastSocket. Implemented by the Triangulator class
	/// </summary>
	public class RecvBroadcastSocketArgs
	{
		public Socket BroadcastReceive = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		public readonly int BufferSize;
		public readonly int LocalPort;
		public byte[] Buffer;
		public readonly EndPoint BoundEndpoint;
		public EndPoint AnyEndpoint = (EndPoint) new IPEndPoint(IPAddress.Any, 0);	//not important to developer
		internal IPAddress RemoteAddress; //remote address of host stored here
		internal ManualResetEvent mre = new ManualResetEvent(false);		//used by calling Triangulator class
		
		public RecvBroadcastSocketArgs(int port, EndPoint boundep)
		{
			BufferSize = 256;
			Buffer = new byte[BufferSize];
			LocalPort = port;
			BoundEndpoint = boundep;
		}
		
		/// <summary>
		/// Releases all resources used by class
		/// </summary>
		public void Dispose()
		{
			if (BroadcastReceive.IsBound)	//if socket in use then shutdown
				BroadcastReceive.Shutdown(SocketShutdown.Both);
			BroadcastReceive.Close();
		}
		//default constructor will be provided by assembly
	}
	
	/// <summary>
	/// Sets up a Socket to receive Broadcasts carrying important information
	/// </summary>
	public class RecvBroadcastSocket
	{	
		public RecvBroadcastSocketArgs rba;
		
		public RecvBroadcastSocket(int Port)
		{
			rba = new RecvBroadcastSocketArgs(Port, (EndPoint) new IPEndPoint(MachineIPAddress.FindMachineAddress(), Port));
		}
		//private asynchronous verification for returned host addr
		private void async_recvfrom(IAsyncResult iar)
		{
			rba.BroadcastReceive = (Socket)iar.AsyncState;
			int recvd;
			
			try
			{ 
				recvd = rba.BroadcastReceive.EndReceiveFrom(iar, ref rba.AnyEndpoint);
			}
			catch (Exception e)
			{
				rba.Dispose();
				throw new GeneralNetworkingException("RecvBroadcastSocketArgs end_receive_from() failed!\n", e);
			}
			
			try
			{
				string ip = System.Text.Encoding.ASCII.GetString(rba.Buffer, 0, recvd);		//receiving string
				rba.RemoteAddress = IPAddress.Parse(ip);					//parsing string
				rba.mre.Set();	//signaling calling class of success
			}
			catch (Exception ex)
			{
				//don't throw exception, but continue listening for udp packets sent to machineaddr:port
				//placing socket in udp receiving state. specified to receive any packets sent to machineaddr:port
				
				//first a bit of cleaning
				rba.Buffer = new byte[rba.BufferSize];
				
				try
				{
					rba.BroadcastReceive.BeginReceiveFrom(rba.Buffer, 0, rba.BufferSize, SocketFlags.None, ref rba.AnyEndpoint, new AsyncCallback(async_recvfrom), rba.BroadcastReceive);
				}
				catch (Exception ex2)
				{
					rba.Dispose();
					throw new GeneralNetworkingException("RecvBroadcastSocketArgs begin_receive_from() failed!\n", ex2);
				}
			}
		}
		/// <summary>
		/// Receives a packet from a potential host. Will return the IPAddress of a potential server/client
		/// </summary>
		public void GetRemoteAddress()
		{
			//setting the socket option to broadcast
			try
			{
				rba.BroadcastReceive.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);	//setting up broadcast socket
				rba.BroadcastReceive.Bind(rba.BoundEndpoint);	//binding to machine address on specified port number
			}
			catch (Exception ex)					//error handling
			{
				throw new GeneralNetworkingException("RecvBroadcastSocketArgs bind() failed!\n", ex);
			}
			
			try
			{
				//placing socket in udp receiving state. specified to receive any packets sent to machineaddr:port
				rba.BroadcastReceive.BeginReceiveFrom(rba.Buffer, 0, rba.BufferSize, SocketFlags.None, ref rba.AnyEndpoint, new AsyncCallback(async_recvfrom), rba.BroadcastReceive);
			}
			catch (Exception ex2)
			{
				rba.Dispose();
				throw new GeneralNetworkingException("RecvBroadcastSocketArgs begin_receive_from() failed!\n", ex2);
			}
		}
		/// <summary>
		/// Releases all resources used by class
		/// </summary>
		public void Dispose()
		{
			rba.Dispose();
		}
	}
}
