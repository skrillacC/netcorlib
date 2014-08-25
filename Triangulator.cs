/*
 * Created by SharpDevelop.
 * User: skrillac
 * Date: 4/20/2013
 * Time: 10:23 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace netcorlib
{	
	public enum TriangulatorOptions
	{
		Server,
		Client
	}
	
	/// <summary>
	/// Use of this class on both local and remote machines allows each side to know the other's IP address.
	/// </summary>
	public class Triangulator
	{	
		private BroadcastSocketArgs bsa;
		private RecvBroadcastSocket rbs;
		
		public Triangulator(TriangulatorOptions toz)
		{
			if (toz.Equals(TriangulatorOptions.Server))
			{
				
				bsa = new BroadcastSocketArgs(49491, BroadcastSocket.ExportBroadcastEndpoint(49491));
				BroadcastSocket.ExportBroadcastSocket(bsa.BroadcastSend);
				
				//rbs = new RecvBroadcastSocket(49492);
				
				//data to be broadcast	
				bsa.Buffer = Encoding.ASCII.GetBytes(MachineIPAddress.FindMachineAddress().ToString());
			
				try
				{
					bsa.BroadcastSend.BeginSendTo(bsa.Buffer, 0, bsa.Buffer.Length, SocketFlags.None, bsa.WorkingEndpoint, new AsyncCallback(async_broadcast), bsa.BroadcastSend);
				}
				catch (Exception ex)
				{
					bsa.Dispose();
					throw new GeneralNetworkingException("BroadcastSocketArgs begin_send_to() failed!\n", ex);
				}
			}
			else
			{
				rbs = new RecvBroadcastSocket(49491);
			}
			
		}
		private void async_broadcast(IAsyncResult iar)
		{	
			//Method changed to stop forever broadcast loop if Socket is marked for disposal
			
			if (!bsa.b_trigger)
			{
				bsa.BroadcastSend = (Socket)iar.AsyncState;
				Thread.Sleep(999);
				
				try
				{
					bsa.BroadcastSend.BeginSendTo(bsa.Buffer, 0, bsa.Buffer.Length, SocketFlags.None, bsa.WorkingEndpoint, new AsyncCallback(async_broadcast), bsa.BroadcastSend);
				}
				catch (Exception ex)
				{
					bsa.Dispose();
					throw new GeneralNetworkingException("BroadcastSocketArgs begin_send_to() failed!\n", ex);
				}
			}
			else	//something has triggered a cease in broadcasting. mind you this is user issued
			{
				bsa.Dispose();
			}
			
		}
		/// <summary>
		/// Returns an IPAddress received from the remote machine
		/// </summary>
		public IPAddress ReceiveConfirmation()
		{
			try
			{
				rbs.GetRemoteAddress();		//writing address to rbs Class
				rbs.rba.mre.WaitOne();		//pausing thread until mre.Set() called from RecvBroadcastSocket Class
				
				//at this location a valid IPAddress exists in rbs.rba.RemoteAddress
				return rbs.rba.RemoteAddress;
			}
			catch (ObjectDisposedException ode)
			{
				throw new GeneralNetworkingException("Underlying objects have been disposed!", ode);
			}
		}
		/// <summary>
		/// Returns an IPEndPoint created from the IPAddress of the remote machine
		/// </summary>
		public IPEndPoint ReceiveConfirmation(int specport)
		{
			try
			{
				
				rbs.GetRemoteAddress();
				rbs.rba.mre.WaitOne();
				
				//at this location a valid IPAddress exists in rbs.rba.RemoteAddress
				return new IPEndPoint(rbs.rba.RemoteAddress, specport);
			}
			catch (ObjectDisposedException ode)
			{
				throw new GeneralNetworkingException("Underlying objects have been disposed", ode);
			}
		}
		//Releases all resources used with this instance
		public void Dispose()
		{
			if (bsa != null)
				bsa.Dispose();		//modifying b_trigger to true will do the same thing
			
			if (rbs != null)
				rbs.Dispose();
		}
	}
}