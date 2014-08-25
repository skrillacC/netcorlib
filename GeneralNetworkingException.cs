/*
 * Created by SharpDevelop.
 * User: D630
 * Date: 4/18/2013
 * Time: 5:32 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace netcorlib
{
	/// <summary>
	/// Throws a GeneralNetworkingException
	/// </summary>
	///
	
	[Serializable]
	public class GeneralNetworkingException : Exception
	{
		public GeneralNetworkingException()
		{}
		
		public GeneralNetworkingException(string message)
			: base(message)
		{}
		public GeneralNetworkingException(string message, Exception inner)
			: base(message, inner)
		{}
	}
}
