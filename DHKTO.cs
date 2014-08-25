/*
 * Created by SharpDevelop.
 * User: skrillac
 * Date: 6/10/2013
 * Time: 9:42 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Net;
using System.Net.Sockets;

namespace netcorlib
{
	
	public class DHTArgs
	{
		public Socket DHTSigned = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		public EndPoint BoundEndpoint;
		public EndPoint WorkingEndpoint;
		public readonly int BufferSize;
		public byte[] Buffer;
		
		public DHTArgs(EndPoint ep_int, EndPoint ep_out)
		{
			BoundEndpoint = ep_int;
			try
			{
				DHTSigned.Bind(BoundEndpoint);
			}
			catch (Exception e)
			{
				throw new GeneralNetworkingException("Diffie-Hellman process interrupted! bind() failure!\n", e);
			}
			
			WorkingEndpoint = ep_out;
			BufferSize = 512;
			Buffer = new byte[sizeof(int)];
		}
		
		///<summary>
		/// Releases all resources used by class 
		///</summary>
		public void Dispose()
		{
			DHTSigned.Shutdown(SocketShutdown.Both);
			DHTSigned.Close();
		}
	}
	
	/// <summary>
	/// This class provides background communication during key creation and transfer.
	/// </summary>
	public class DHKTO
	{
		private DHTArgs dht;
		
		public DHKTO(EndPoint ep_int, EndPoint ep_out)
		{
			dht = new DHTArgs(ep_int, ep_out);
		}
		/// <summary>
		/// Sends the public primes and bases of the Diffie-Hellman process
		/// </summary>
		public void SendPrimesBases(int[,] primes_bases)
		{
			//Here i allocate space for each integer in each array of the main array
			
			dht.Buffer = new byte[(5 * primes_bases.GetLength(0))];
			
			for (int i = 0, j = 0; i < primes_bases.GetLength(0); i++, j+=5)
			{	
				byte[] b_conv = new byte[4];
				b_conv = BitConverter.GetBytes(primes_bases[i, 0]);		//storing bytes from bitconverter
				
				for (int k = 0; k < 4; k++)			//copying bytes from bitconverter
					dht.Buffer[j+k] = b_conv[k];
				
				dht.Buffer[j+4] = (byte)primes_bases[i, 1];									//agreed base
			}
			
			dht.DHTSigned.SendTo(dht.Buffer, 0, dht.Buffer.Length, SocketFlags.None, dht.WorkingEndpoint);
		}
		/// <summary>
		/// Sends the signed partial key of the Diffie-Hellman process
		/// </summary>
		public void SendKeyPart(byte[] key_part)
		{
			//i copy buffers in order to view what is being transmitted in send/recieve scenarios
			
			dht.Buffer = new byte[key_part.Length];
			
			for (int x = 0; x < key_part.Length; x++)
				dht.Buffer[x] = key_part[x];
			
			dht.DHTSigned.SendTo(dht.Buffer, 0, dht.Buffer.Length, SocketFlags.None, dht.WorkingEndpoint);
			
		}
		/// <summary>
		/// Receives the public primes and bases of the Diffie-Hellman process
		/// </summary>
		public int[,] ReceivePrimesBases()
		{
			dht.Buffer = new byte[dht.BufferSize];
			int recvd =	dht.DHTSigned.ReceiveFrom(dht.Buffer, 0, dht.BufferSize, SocketFlags.None, ref dht.WorkingEndpoint);
			int[,] primes_bases = new int[recvd/5, 2];
			
			for (int f = 0, j = 0; f < primes_bases.GetLength(0); f++, j+=5)
			{
				primes_bases[f, 0] = BitConverter.ToInt32(dht.Buffer, j);
				primes_bases[f, 1] = Convert.ToInt32(dht.Buffer[j+4]);
			}
			
			return primes_bases;
		}
		/// <summary>
		/// Receives the signed partial key of the Diffie-Hellman process
		/// </summary>
		public byte[] ReceiveKeyPart()
		{
			dht.Buffer = new byte[dht.BufferSize];
			int recvd = dht.DHTSigned.ReceiveFrom(dht.Buffer, 0, dht.BufferSize, SocketFlags.None, ref dht.WorkingEndpoint);
			byte[] b_resize = new byte[recvd];
			
			for (int y = 0; y < recvd; y++)
				b_resize[y] = dht.Buffer[y];
			
			return b_resize;
		}
		/// <summary>
		/// Releases all resources used by this class
		/// </summary>
		public void Dispose()
		{
			dht.Dispose();
		}
	}
}
