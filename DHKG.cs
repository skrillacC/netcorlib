/*
 * Created by SharpDevelop.
 * User: skrillac
 * Date: 6/8/2013
 * Time: 12:38 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */ 
 
using System;
using System.Security.Cryptography;

namespace netcorlib
{
	/// <summary>
	/// Generates signed key parts for the Diffie-Hellman Key Exchange routine
	/// </summary>
	public class DHKG
	{	
		private Random r;
		private int[,] base_prime_secret;
		private SHA256 s256 = SHA256.Create();
		
		public DHKG(int[,] prime_base_agreed)
		{
			r = new Random((int)(DateTime.Now.Ticks & 0x0000FFFF));
			base_prime_secret = new int[prime_base_agreed.GetLength(0),3];
			
			for (int x = 0; x < prime_base_agreed.GetLength(0); x++)
			{
				//first val is the prime
				base_prime_secret[x, 0] = prime_base_agreed[x, 0];
				//second val is the base
				base_prime_secret[x, 1] = prime_base_agreed[x, 1];
				//third val is a randomly generated secret number
				
				switch(prime_base_agreed[x,1])
				{									//generating secret numbers 
					case 2:
						base_prime_secret[x, 2] = r.Next(28) + 2;
						break;
					case 3:
						base_prime_secret[x, 2] = r.Next(17) + 2;
						break;
					case 4:
						base_prime_secret[x, 2] = r.Next(13) + 2;
						break;
					case 5:
						base_prime_secret[x, 2] = r.Next(11) + 2;
						break;
					case 6:
						base_prime_secret[x, 2] = r.Next(9) + 2;
						break;
					case 7:
						base_prime_secret[x, 2] = r.Next(9) + 2;
						break;
					case 8:
						base_prime_secret[x, 2] = r.Next(8) + 2;
						break;
					case 9:
						base_prime_secret[x, 2] = r.Next(7) + 2;
						break;
					default:
						//this should never evaluate
						break;
				}
				
			}
		}
		private int exp(int x , int y)
		{
			int ans = x;
			for (int a = 0; a < y; a++)
				ans *= x;
			return x;
		}
		private int mod(int prime, int base_mod, int pow)
		{
			//this method is controlled by the class so i don't need to throw an exception
			
			return (exp(base_mod, pow) % prime);
		}
		/// <summary>
		/// This method pools prime, base, and secret integers to sign the first part of the key
		/// </summary>
		public byte[] SignKeyPart()
		{
			byte[] cexport = new byte[base_prime_secret.GetLength(0)];
			
			for (int x = 0; x < cexport.Length; x++)
				cexport[x] = Convert.ToByte(mod(base_prime_secret[x, 0], base_prime_secret[x, 1], base_prime_secret[x, 2]));
			
			return cexport;
		}
		/// <summary>
		/// Signs the received key part to produce a finished key
		/// </summary>
		public byte[] FinishKey(byte[] recvdkpart)
		{
			byte[] sdh256 = new byte[base_prime_secret.GetLength(0)];
			byte[] finished_key = new byte[base_prime_secret.GetLength(0)];
			
			if (finished_key.Length != recvdkpart.Length)
				throw new GeneralNetworkingException("Diffie-Hellman Exchange received key part not usable");

			for (int x = 0; x < finished_key.Length; x++)
				finished_key[x] = Convert.ToByte(mod(base_prime_secret[x, 0], Convert.ToInt32(recvdkpart[x]), base_prime_secret[x, 2]));
			
			sdh256 = s256.ComputeHash(finished_key, 0, finished_key.Length);	//hashing
		
			for (int y = 0; y < finished_key.Length; y++)	//copying keylength bytes from hash
				finished_key[y] = sdh256[y];
					
			return finished_key;	
		}
	}
}
