/*
 * Created by SharpDevelop.
 * User: skrillac
 * Date: 4/1/2013
 * Time: 2:30 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers
 *
 */
 
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;

 /*
  * UPDATES 
  * 
  * Encryption/Decryption support disabled for the time being
  * Essential variables moved into FSArgs information class
  * 
  * REWRITTEN: 8/3/14
  * To be implemented in sending files from defined point A to B, not key scheduling, machine location, etc...
  * 
  */

namespace netcorlib
{
	
	public delegate void ChangedEventHandler(object sender, FSEventArgs e);
	
	/// <summary>
	/// Sample EventArgs for FileSocket events
	/// </summary>
	public class FSEventArgs : EventArgs
	{
		private double currper = 0.0;
		private int id = 0;
		private string fname = "";
		private long fsize = 0L;
		private string ext = "";
		
		public FSEventArgs(int uuid)
		{
			id = uuid;
		}
		public FSEventArgs(string info, int uuid)
		{
			ext = info;
			id = uuid;
		}
		public FSEventArgs(double perc, int uuid)
		{
			currper = perc;
			id = uuid;
		}
		public FSEventArgs(string name, long size, int uuid)
		{
			fname = name;
			fsize = size;
			id = uuid;
		}
		public string Name
		{
			get {return fname;}
		}
		public string State
		{
			get {return ext;}
		}
		public long Size
		{
			get {return fsize;}
		}
		public int UUID
		{
			get {return id;}
		}
		public double Percentage
		{
			get {return	currper;}
		}
	}
	
	/// <summary>
	/// Information class for FileSocket. Implemented by the FileSocket
	/// </summary>
	public class FSArgs
	{
		private Random rand = new Random(((int)(DateTime.Now.Ticks & 0x0000FFFF)));
		internal bool ShowConsole = false;
		internal Socket TransferSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		internal Socket Background_Comm = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		public const int BlockSize = 1024;	//1k block
		public const int BackgroundBufferSize = 256; //quarter k block
		internal byte[] background_buffer = new byte[BackgroundBufferSize];
		internal byte[] File_Buffer = new byte[BlockSize];
		internal byte[] Plaintext = new byte[BlockSize];
		//private byte[] key = null;
		//private byte[] iv = null;
		internal EndPoint ep_background;
		internal EndPoint ep_background_out;
		internal string FileName;
		internal long FileSize;
		internal double TransferPercentage = 0.0;
		internal FileStream FileStream;
		public event ChangedEventHandler PercentageUpdate;
		public event ChangedEventHandler FileInfoUpdate;
		public event ChangedEventHandler Finished;
		internal int throttle = 1;
		internal int uuid;
		private bool UseForm = false;
		
		internal int rnd = 0;
		internal long tot = 0L;
		
		public FSArgs(bool form)
		{
			UseForm = form;
			GenerateUUID();
			//standard constructor
		}
		/*public byte[] Key
		{
			set {key = value;}
		}
		public byte[] IV
		{
			set {iv = value;}
		}
		*/
		/// <summary>
		/// Generates UUID for FSArgs class
		/// </summary>
		public void GenerateUUID()
		{
			uuid = rand.Next();
		}
		protected internal virtual void OnFileFinished(string hash)
		{
			if (Finished != null)
				Finished(this, new FSEventArgs(hash, uuid));
		}
		protected internal virtual void OnFileInfoChanged(string name, long size)
		{
			if (FileInfoUpdate != null)
				FileInfoUpdate(this, new FSEventArgs(name, size, uuid));
		}
		protected internal virtual void OnPercentageChanged(double d)
		{
			if (PercentageUpdate != null)
				PercentageUpdate(this, new FSEventArgs(d, uuid));
		}
		public bool Form
		{
			get {return UseForm;}
		}
		public bool Connected
		{
			get {return TransferSocket.Connected;}
		}
		public double Percentage
		{
			get {return TransferPercentage;}
		}
		public int Throttle
		{
			get {return throttle;}
			set 
			{
				if (throttle < 10)
					throttle = value;
				else
					throttle = 5;
			}
		}
		public long Size
		{
			get {return FileSize;}
		}
		public string Name
		{
			get {return FileName;}
		}
		public int BytesRound
		{
			get {return rnd;}
		}
		public long BytesTotal
		{
			get {return tot;}
		}
		public int UUID
		{
			get {return uuid;}
		}
		public void Dispose()
		{
			if (TransferSocket.Connected && TransferSocket != null)	//if socket in use then shutdown
				TransferSocket.Shutdown(SocketShutdown.Both);
			
			if (Background_Comm.IsBound && TransferSocket != null)	//if socket in use then shutdown
				Background_Comm.Shutdown(SocketShutdown.Both);
			
			TransferSocket.Close();
			Background_Comm.Close();
			FileStream.Close();
		}
	}
	
	/// <summary>
	/// Socket implementation that sends files between two machines on the same subnet of a local area network. Wraps a connected TCP socket
	/// </summary>
	public class FileSocket
	{
		public FSArgs fsa;
		private readonly string path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory).ToString();
		
		//private TripleDESCryptoServiceProvider tdes;
		//private ICryptoTransform ictransform;
		
		//standard constructor for use with a connected socket
		public FileSocket(Socket TCP_CONNECTED_SOCK, bool showconsole, bool useform)
		{
			if (((IPEndPoint)TCP_CONNECTED_SOCK.LocalEndPoint).Address == IPAddress.Loopback)
				throw new GeneralNetworkingException("Loopback Address not supported!");
			
			if (TCP_CONNECTED_SOCK.ProtocolType != ProtocolType.Tcp)
				throw new GeneralNetworkingException("ProtocolType.Tcp required!");
			
			if (!TCP_CONNECTED_SOCK.Connected)
				throw new GeneralNetworkingException("Socket is not connected to a host!");
			
			//UDP background communication is across the same port as the TCP connected socket
			
			fsa = new FSArgs(useform);
			
			if (showconsole)
				fsa.ShowConsole = true;
			
			fsa.TransferSocket = TCP_CONNECTED_SOCK;
			fsa.ep_background = (EndPoint) TCP_CONNECTED_SOCK.LocalEndPoint;
			fsa.ep_background_out = (EndPoint) TCP_CONNECTED_SOCK.RemoteEndPoint;
			
			try
			{
				fsa.Background_Comm.Bind(fsa.ep_background);
			}
			catch (Exception e)
			{
				fsa.Dispose();
				throw new GeneralNetworkingException("FileSocket Background_Comm bind() failed!", e);
			}
		}
		/// <summary>
		/// Begins receiving a file. This method is synchronous
		/// </summary>
		public void ReceiveFile()
		{	
			if (fsa.ShowConsole)
				Console.Clear();
			
			//Here I receive a filesize
			fsa.background_buffer = new byte[FSArgs.BackgroundBufferSize];
			
			try
			{
				fsa.Background_Comm.ReceiveFrom(fsa.background_buffer, 0, FSArgs.BackgroundBufferSize, SocketFlags.None, ref fsa.ep_background_out);
			}
			catch (Exception e)
			{
				fsa.Dispose();
				throw new GeneralNetworkingException("FileSocket filesize receive_from() failed!", e);
			}
			
			fsa.FileSize = BitConverter.ToInt64(fsa.background_buffer, 0);
			
			//Here I receive a filename
			fsa.background_buffer = new byte[FSArgs.BackgroundBufferSize];
			int filename_recvd = 0;
			
			try
			{
				filename_recvd = fsa.Background_Comm.ReceiveFrom(fsa.background_buffer, 0, FSArgs.BackgroundBufferSize, SocketFlags.None, ref fsa.ep_background_out);
			}
			catch (Exception e)
			{
				fsa.Dispose();
				throw new GeneralNetworkingException("FileSocket filename receive_from() failed!", e);
			}
			
			fsa.FileName = Encoding.ASCII.GetString(fsa.background_buffer, 0, filename_recvd);
			
			if (fsa.Form)
				fsa.OnFileInfoChanged(fsa.Name, fsa.Size);
			
			if (!Directory.Exists(path+"\\Downloads"))
				Directory.CreateDirectory(path+"\\Downloads");
			
			fsa.FileStream = new FileStream(path+"\\Downloads\\"+fsa.FileName, FileMode.CreateNew, FileAccess.Write, FileShare.Write, FSArgs.BlockSize);
			
			int x = 0;
			
			do
			{
				//reinitializing is extraneous, as Receive() calls are overwriting
				try
				{
					fsa.rnd = fsa.TransferSocket.Receive(fsa.File_Buffer, 0, FSArgs.BlockSize, SocketFlags.None);
				}
				catch (Exception e)
				{
					fsa.Dispose();
					throw new GeneralNetworkingException("FileSocket receive() failed!", e);
				}
				
				fsa.tot += Convert.ToInt64(fsa.rnd);
				
				if ((((int)((fsa.tot/(double)fsa.Size) * 100)) - ((int)fsa.Percentage)) > 1 && fsa.Form)	//only throw event when change is detectable by an existing form
				{
					fsa.TransferPercentage = fsa.tot/((double)fsa.Size) * 100;
					fsa.OnPercentageChanged(fsa.Percentage);	//throw event for form
				}
					
				
				//fsa.Plaintext = Transform(fsa.File_Buffer, 0, FSArgs.BlockSize);
				
				//after transformation, the data array will have up to 8 bytes, depending on the padding
				//FileStream.Write(fsa.Plaintext, 0, fsa.Plaintext.Length);
				fsa.FileStream.Write(fsa.File_Buffer, 0, fsa.rnd);
				
				if (fsa.ShowConsole)
				{
					Console.Clear();
					Console.WriteLine(":: " + fsa.BytesRound + " KBS received: ROUND " + ++x + " :: " + fsa.BytesTotal + " KBS received!" + " :: " + fsa.Percentage + "%\n");
				}
				
			} while (fsa.tot < fsa.FileSize); //computes while there are still bytes to receive
				
			fsa.TransferSocket.Shutdown(SocketShutdown.Both);
			fsa.TransferSocket.Close();
			
			if (fsa.Form)
			{
				//computing checksum
				MD5 m = MD5.Create();
				StringBuilder sb = new StringBuilder();
				byte[] md5_buffer = new byte[16];
				
				if (!fsa.FileStream.CanRead)
				{
					fsa.FileStream.Close();
					FileStream fs = new FileStream(path+"\\Downloads\\"+fsa.FileName, FileMode.Open, FileAccess.Read, FileShare.Read, FSArgs.BlockSize);
					fs.Position = fsa.BytesTotal-16;
					fs.Read(md5_buffer, 0, md5_buffer.Length);
					md5_buffer = m.TransformFinalBlock(md5_buffer, 0, 16);
					for (int i = 0; i < md5_buffer.Length; i++)
					{
						sb.Append(md5_buffer[i].ToString("x2"));
					}
					fs.Close();
					fsa.OnFileFinished(sb.ToString());
					return;
				}
				
				fsa.FileStream.Position-=16;
				fsa.FileStream.Read(md5_buffer, 0, md5_buffer.Length);
				md5_buffer = m.TransformFinalBlock(md5_buffer, 0, 16);
				for (int i = 0; i < md5_buffer.Length; i++)
				{
					sb.Append(md5_buffer[i].ToString("x2"));
				}
			
				fsa.OnFileFinished(sb.ToString());
			}
			
			fsa.FileStream.Close();
			
			//Here the method ends and the File has been written to the disk
		}
		/// <summary>
		/// Begins receiving a file to a certain path. This method is synchronous.
		/// </summary>
		public void ReceiveFile(string fullpathtofolder)
		{	
			if (fsa.ShowConsole)
				Console.Clear();
			
			//Here I receive a filesize
			fsa.background_buffer = new byte[FSArgs.BackgroundBufferSize];
			try
			{
				fsa.Background_Comm.ReceiveFrom(fsa.background_buffer, 0, FSArgs.BackgroundBufferSize, SocketFlags.None, ref fsa.ep_background_out);
			}
			catch (Exception e)
			{
				fsa.Dispose();
				throw new GeneralNetworkingException("FileSocket filesize receive_from() failed!", e);
			}
			
			fsa.FileSize = BitConverter.ToInt64(fsa.background_buffer, 0);
			
			//Here I receive a filename
			fsa.background_buffer = new byte[FSArgs.BackgroundBufferSize];
			int filename_recvd;
			try
			{
				filename_recvd = fsa.Background_Comm.ReceiveFrom(fsa.background_buffer, 0, FSArgs.BackgroundBufferSize, SocketFlags.None, ref fsa.ep_background_out);
			}
			catch (Exception e)
			{
				fsa.Dispose();
				throw new GeneralNetworkingException("FileSocket filename receive_from() failed!", e);
			}
			
			fsa.FileName = Encoding.ASCII.GetString(fsa.background_buffer, 0, filename_recvd);
			
			if (fsa.Form)
				fsa.OnFileInfoChanged(fsa.Name, fsa.Size);
			
			if (!Directory.Exists(fullpathtofolder))
				Directory.CreateDirectory(fullpathtofolder);
			
			fsa.FileStream = new FileStream(fullpathtofolder+"\\"+fsa.FileName, FileMode.CreateNew, FileAccess.Write, FileShare.Write, FSArgs.BlockSize);
			
			int x = 0;
			
			do
			{
				//reinitializing is extraneous, as Receive() calls are overwriting
				try
				{
					fsa.rnd = fsa.TransferSocket.Receive(fsa.File_Buffer, 0, FSArgs.BlockSize, SocketFlags.None);
				}
				catch (Exception e)
				{
					fsa.Dispose();
					throw new GeneralNetworkingException("FileSocket receive() failed!", e);
				}
				
				fsa.tot += Convert.ToInt64(fsa.rnd);
				
				if ((((int)((fsa.tot/(double)fsa.Size) * 100)) - ((int)fsa.Percentage)) > 1 && fsa.Form)	//only throw event when change is detectable by an existing form
				{
					fsa.TransferPercentage = fsa.tot/((double)fsa.Size) * 100;
					fsa.OnPercentageChanged(fsa.Percentage);	//throw event for form
				}
				
				//fsa.Plaintext = Transform(fsa.File_Buffer, 0, FSArgs.BlockSize);
				
				//after transformation, the data array will have up to 8 bytes, depending on the padding
				//FileStream.Write(fsa.Plaintext, 0, fsa.Plaintext.Length);
				fsa.FileStream.Write(fsa.File_Buffer, 0, fsa.rnd);
				
				if (fsa.ShowConsole)
				{
					Console.Clear();
					Console.WriteLine(":: " + fsa.BytesRound + " KBS received: ROUND " + ++x + " :: " + fsa.BytesTotal + " KBS received!" + " :: " + fsa.Percentage + "%\n");
				}
				
			} while (fsa.tot < fsa.FileSize); //not sure if TransferSocket.Available is a reliable parameter
				
			fsa.TransferSocket.Shutdown(SocketShutdown.Both);
			fsa.TransferSocket.Close();
			
			if (fsa.Form)
			{
				//computing checksum
				MD5 m = MD5.Create();
				StringBuilder sb = new StringBuilder();
				byte[] md5_buffer = new byte[16];
				
				if (!fsa.FileStream.CanRead)
				{
					fsa.FileStream.Close();
					FileStream fs = new FileStream(path+"\\Downloads\\"+fsa.FileName, FileMode.Open, FileAccess.Read, FileShare.Read, FSArgs.BlockSize);
					fs.Position = fsa.BytesTotal-16;
					fs.Read(md5_buffer, 0, md5_buffer.Length);
					md5_buffer = m.TransformFinalBlock(md5_buffer, 0, 16);
					for (int i = 0; i < md5_buffer.Length; i++)
					{
						sb.Append(md5_buffer[i].ToString("x2"));
					}
					fs.Close();
					fsa.OnFileFinished(sb.ToString());
					return;
				}
				
				fsa.FileStream.Position-=16;
				fsa.FileStream.Read(md5_buffer, 0, md5_buffer.Length);
				md5_buffer = m.TransformFinalBlock(md5_buffer, 0, 16);
				for (int i = 0; i < md5_buffer.Length; i++)
				{
					sb.Append(md5_buffer[i].ToString("x2"));
				}
			
				fsa.OnFileFinished(sb.ToString());
			}
			
			fsa.FileStream.Close();
				
			//Here the method ends and the File has been written to the disk
		}
		/// <summary>
		/// Begins sending the selected file. This method is not asynchronous
		/// </summary>
		public void SendFile(string fullfilepath)
		{		
			if (fsa.ShowConsole)
				Console.Clear();
			
			FileInfo fi = new FileInfo(fullfilepath);
			fsa.FileSize = fi.Length;
			fsa.background_buffer = new byte[8];	//8 bytes for a long
			fsa.background_buffer = BitConverter.GetBytes(fsa.FileSize);
			try
			{
				fsa.Background_Comm.SendTo(fsa.background_buffer, 0, 8, SocketFlags.None, fsa.ep_background_out);
			}
			catch (Exception e)
			{
				fsa.Dispose();
				throw new GeneralNetworkingException("FileSocket filesize send_to() failed!", e);
			}
			
			fsa.FileName = fi.Name;
			fsa.background_buffer = new byte[fsa.FileName.Length];
			fsa.background_buffer = Encoding.ASCII.GetBytes(fsa.FileName);
			try
			{
				fsa.Background_Comm.SendTo(fsa.background_buffer, 0, fsa.FileName.Length, SocketFlags.None, fsa.ep_background_out);
			}
			catch (Exception e)
			{
				fsa.Dispose();
				throw new GeneralNetworkingException("FileSocket filename send_to() failed!", e);
			}
			
			if (fsa.Form)
				fsa.OnFileInfoChanged(fsa.Name, fsa.Size);
			
			fsa.FileStream = new FileStream(fullfilepath, FileMode.Open, FileAccess.Read, FileShare.Read, FSArgs.BlockSize);
			
			int x = 0;
			
			do
			{
				//reinitializing is extraneous, as Receive() calls are overwriting
				int read = fsa.FileStream.Read(fsa.File_Buffer, 0, FSArgs.BlockSize);
				//The length of file_buffer is not always 8, so I have to account for the last packet
				//plaintext = Transform(file_buffer, 0, read);
				//transformation creates an 8 byte packet everytime
				try
				{
					fsa.rnd = fsa.TransferSocket.Send(fsa.File_Buffer, 0, read, SocketFlags.None);
				}
				catch (Exception e)
				{
					fsa.Dispose();
					throw new GeneralNetworkingException("FileSocket send() failed!", e);
				}
				
				fsa.tot += Convert.ToInt64(fsa.rnd);

				if ((((int)((fsa.tot/(double)fsa.Size) * 100)) - ((int)fsa.Percentage)) > 1 && fsa.Form)	//only throw event when change is detectable by an existing form
				{
					fsa.TransferPercentage = fsa.tot/((double)fsa.Size) * 100;
					fsa.OnPercentageChanged(fsa.Percentage);	//throw event for form
				}
				
				if (fsa.ShowConsole)
				{
					Console.Clear();
					Console.WriteLine(":: " + fsa.BytesRound + " KBS sent: ROUND " + ++x + " :: " + fsa.BytesTotal + " KBS sent!" + " :: " + fsa.Percentage + "%\n");
				}
				
				if (fsa.Throttle > 0)
					System.Threading.Thread.Sleep(fsa.Throttle);
				
			} while (fsa.tot < fsa.FileSize); //Will terminate when number of sent bytes equals the file size
			
			fsa.TransferSocket.Shutdown(SocketShutdown.Both);
			fsa.TransferSocket.Close();
			
			if (fsa.Form)
			{
				//computing checksum
				MD5 m = MD5.Create();
				StringBuilder sb = new StringBuilder();
				byte[] md5_buffer = new byte[16];
				
				if (!fsa.FileStream.CanRead)
				{
					fsa.FileStream.Close();
					FileStream fs = new FileStream(fullfilepath, FileMode.Open, FileAccess.Read, FileShare.Read, FSArgs.BlockSize);
					fs.Position = fsa.BytesTotal-16;
					fs.Read(md5_buffer, 0, md5_buffer.Length);
					md5_buffer = m.TransformFinalBlock(md5_buffer, 0, 16);
					for (int i = 0; i < md5_buffer.Length; i++)
					{
						sb.Append(md5_buffer[i].ToString("x2"));
					}
					fs.Close();
					fsa.OnFileFinished(sb.ToString());
					return;
				}
				
				fsa.FileStream.Position-=16;
				fsa.FileStream.Read(md5_buffer, 0, md5_buffer.Length);
				md5_buffer = m.TransformFinalBlock(md5_buffer, 0, 16);
				for (int i = 0; i < md5_buffer.Length; i++)
				{
					sb.Append(md5_buffer[i].ToString("x2"));
				}
			
				fsa.OnFileFinished(sb.ToString());
			}
			
			fsa.FileStream.Close();
			
			//Here the method ends and the file has been transmitted
		}/*
		private byte[] Transform(byte[] plainorcipher, int start, int length)
		{
			//i don't differentiate between encryption or decryption because the
			//ictransform object is initialized with an enum value that will determine the Transform() call
			//also, I don't want a decryption call placed on a encryption-initialized ICryptoTransform object, or vice-versa
			
			try
			{
				return ictransform.TransformFinalBlock(plainorcipher, start, length);
			}
			catch (CryptographicException ce)
			{
				throw new GeneralNetworkingException("Transformation failed!\n\n ", ce);
			}
		}*/
	}
}
