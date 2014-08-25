/*
 * Created by SharpDevelop.
 * User: skrillac
 * Date: 6/11/2013
 * Time: 12:15 AM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
 
using System;

namespace netcorlib
{
	/// <summary>
	/// Generates random primes and bases for the Diffie-Hellman Key Exchange routine
	/// </summary>
	public class DHPBG
	{
		private int[] primes;
		private int[,] primes_bases;
		private Random r;
		
		public DHPBG(int key_len)
		{	
			primes = new int[256];	//fitting space contraints of 1K
			primes_bases = new int[key_len,2];
		}
		/// <summary>
		/// Prime number test borrowed from dotnetperls.com
		/// </summary>
		public static bool IsPrime(int input)
		{
			// Test whether the parameter is a prime number.
			if ((input & 1) == 0)
			{
	    		if (input == 2)
					return true;
	    		else
					return false;
			}
			for (int i = 3; (i * i) <= input; i += 2)
			{
	    		if ((input % i) == 0)
					return false;
			}
			
			return input != 1;
		}
		/// <summary>
		/// Generates Primes and Bases to be used for the Diffie-Hellman process
		/// </summary>
		public int[,] GeneratePrimesBases()
		{	
			
			int nprime = 0;
			r = new Random((int)(DateTime.Now.Ticks & 0x0000FFFF));
			for (int x = 0; x < 256; x+=0)	//index not evaluated at loop but in loop
			{
				nprime = r.Next(10000);	//range here is 0-10000
				if (DHPBG.IsPrime(nprime)) { primes[x++] = nprime; }
			}
			
			r = new Random((int)(DateTime.Now.Ticks & 0x0000FFFF));
			for (int y = 0; y < primes_bases.GetLength(0); y++)
			{
				primes_bases[y, 0] = primes[r.Next(256)];	//picking random index of randomly generated primes list
				primes_bases[y, 1] = r.Next(9); //need output to be between 0 and 9 here
			}
			
			return primes_bases;
		}
	}
}
