using System;
using System.Collections;

/*

Copyright (c) Alex Chudnovsky, Majestic-12 Ltd (UK). 2005+ All rights reserved
Web:		http://www.majestic12.co.uk
E-mail:		alexc@majestic12.co.uk

Redistribution and use in source and binary forms, with or without modification, are 
permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice, this list of conditions 
		and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions 
		and the following disclaimer in the documentation and/or other materials provided with the distribution.
    * Neither the name of the Majestic-12 nor the names of its contributors may be used to endorse or 
		promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, 
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE 
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, 
WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE 
USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

*/


namespace Majestic12
{
	/// <summary>
	/// FastHash: class provides fast look ups at the expense of memory (at least 128k per object).
	/// Its designed primarily for those hashes where majority of lookups are unsuccessful 
	/// (ie object is not present)
	/// 
	/// Status of this work is EXPERIMENTAL, do not make any untested assumptions.
	/// 
	/// History:	15/12/06 Added range check in GetXY
	///				sometime in 2005: initial imlpementation
	/// 
	/// </summary>
	///<exclude/>
	public class FastHash : IDisposable
	{
		/// <summary>
		/// Maximum number of chars to be taken into account
		/// </summary>
		const int MAX_CHARS=256;

		/// <summary>
		/// Maximum number of keys to be stored
		/// </summary>
		const int MAX_KEYS=32*1024;

		/// <summary>
		/// Value indicating there are multiple keys stored in a given position
		/// </summary>
		const ushort MULTIPLE_KEYS=ushort.MaxValue;

		/// <summary>
		/// Hash that will contain keys and will be used at the last resort as looksup are too slow
		/// </summary>
		Hashtable oHash=new Hashtable();

		/// <summary>
		/// Minimum key length 
		/// </summary>
		int iMinLen=int.MaxValue;

		/// <summary>
		/// Maximum key length
		/// </summary>
		int iMaxLen=int.MinValue;

		/// <summary>
		/// Array in which we will keep char hints
		/// </summary>
		ushort[,] usChars=new ushort[MAX_CHARS,MAX_CHARS];

		/// <summary>
		/// Keys
		/// </summary>
		string[] sKeys=new string[MAX_KEYS];

		/// <summary>
		/// Values of keys
		/// </summary>
		object[] iValues=new object[MAX_KEYS];

		/// <summary>
		/// Number of keys stored
		/// </summary>
		ushort usCount=0;

		/// <summary>
		/// Gets keys in this hash
		/// </summary>
		public ICollection Keys
		{
			get
			{
				return oHash.Keys;
			}
		}

		public FastHash()
		{

		}

		/// <summary>
		/// Adds key to the fast hash
		/// </summary>
		/// <param name="sKey">Key</param>
		public void Add(string sKey)
		{
			Add(sKey,0);
		}

		/// <summary>
		/// Adds key and its value to the fast hash
		/// </summary>
		/// <param name="sKey">Key</param>
		/// <param name="iValue">Value</param>
		public void Add(string sKey,object iValue)
		{
			if(usCount>=ushort.MaxValue)
				throw new Exception("Fast hash is full and can't add more keys!");

			if(sKey.Length==0)
				return;

			iMinLen=(int)Math.Min(sKey.Length,iMinLen);
			iMaxLen=(int)Math.Max(sKey.Length,iMaxLen);

			int iX,iY;

			GetXY(sKey,out iX,out iY);

			if(iX<MAX_CHARS && iY<MAX_CHARS)
			{
				ushort usCutPos=usChars[iX,iY];

				if(usCutPos==0)
				{
					usChars[iX,iY]=(ushort)(usCount+1);

					iValues[usCount]=iValue;
					sKeys[usCount]=sKey;
				}
				else
				{
					// mark this entry with maxvalue indicating that there is more than one key stored there
					usChars[iX,iY]=MULTIPLE_KEYS;
				}

				usCount++;
			}

			oHash[sKey]=iValue;
		}

		public static void GetXY(string sKey,out int iX,out int iY)
		{
			// most likely scenario is that we have at least 2 chars
			if(sKey.Length>=2)
			{
				iX=(int)sKey[0];
				iY=(int)sKey[1];
			}
			else
			{
				if(sKey.Length==0)
				{
					iX=MAX_CHARS+1;
					iY=MAX_CHARS+1;
					return;
				}

				iX=(int)sKey[0];
				iY=0;
			}

			//Console.WriteLine("{0}: {1}-{2}",sKey,iX,iY);
		}

		/// <summary>
		/// Checks if given key is present in the hash
		/// </summary>
		/// <param name="sKey">Key</param>
		/// <returns>True if key is present</returns>
		public bool Contains(string sKey)
		{
			// if requested string is too short or too long then we can quickly return false
			// NOTE: seems to use too much CPU for the amount of useful work it does
			
			// NOTE 2: better do it than get nasty excepton...
			if(sKey.Length<iMinLen || sKey.Length>iMaxLen)
				return false;

			int iX,iY;

			GetXY(sKey,out iX,out iY);

			if(iX<MAX_CHARS && iY<MAX_CHARS)
			{

				ushort usPos=usChars[iX,iY];

				if(usPos==0)
					return false;

				// now check if its just one key
				if(usPos!=MULTIPLE_KEYS && sKeys[usPos-1]==sKey)
					return true;
			}

			// finally we have no choice but to do a proper hash lookup
			return oHash[sKey]!=null;
		}

		/// <summary>
		/// Access to values via indexer
		/// </summary>
		public object this [string sKey]
		{
			get
			{
				return GetValue(sKey);
			}
			set
			{
				if(!Contains(sKey))
					Add(sKey,value);
			}
		}

		/// <summary>
		/// Returns value associated with the key or null if key not present
		/// </summary>
		/// <param name="sKey">Key</param>
		/// <returns>Null or object convertable to integer as value</returns>
		public object GetValue(string sKey)
		{
			// if requested string is too short or too long then we can quickly return false
			if(sKey.Length<iMinLen || sKey.Length>iMaxLen)
				return null;

			int iX,iY;

			GetXY(sKey,out iX,out iY);

			if(iX<MAX_CHARS && iY<MAX_CHARS)
			{
				ushort usPos=usChars[iX,iY];

				if(usPos==0)
					return null;

				// now check string in list of keys
				if(usPos!=MULTIPLE_KEYS && sKeys[usPos-1]==sKey)
					return iValues[usPos-1];
			}

			// finally we have no choice but to do a proper hash lookup
			//Console.WriteLine("Have to use hash... :(");
			return oHash[sKey];
		}

		/// <summary>
		/// Returns value of a key that is VERY likely to be present - this avoids doing some checks that
		/// are most likely to be pointless thus making overall faster function
		/// </summary>
		/// <param name="sKey">Key</param>
		/// <returns>Null if no value or value itself</returns>
		public object GetLikelyPresentValue(string sKey)
		{
			if(sKey.Length<iMinLen || sKey.Length>iMaxLen)
				return null;

			int iX,iY;

			GetXY(sKey,out iX,out iY);

			if(iX<MAX_CHARS && iY<MAX_CHARS)
			{
				ushort usPos=usChars[iX,iY];

				if(usPos==0)
					return null;

				// now check string is the only one
				if(usPos!=MULTIPLE_KEYS && sKeys[usPos-1]==sKey)
					return iValues[usPos-1];
			}

			// finally we have no choice but to do a proper hash lookup
			return oHash[sKey];
		}

		/// <summary>
		/// Returns value for likely present keys using first chars (byte)
		/// </summary>
		/// <param name="iX">Byte 1 denoting char 1</param>
		/// <param name="iY">Byte 2 denoting char 2 (0 if not present)</param>
		/// <returns>Non-null value if it was found, or null if full search for key is required</returns>
		public object GetLikelyPresentValue(byte iX,byte iY)
		{
			ushort usPos=usChars[iX,iY];

			if(usPos!=MULTIPLE_KEYS && usPos!=0)
				return iValues[usPos-1];

			// finally we have no choice but to do a proper hash lookup
			return null;
		}

		/// <summary>
		/// Quickly checks if given chars POSSIBLY refer to a stored key.
		/// </summary>
		/// <param name="cChar1">Char 1</param>
		/// <param name="cChar2">Char 2</param>
		/// <param name="iLength">Length of string</param>
		/// <returns>False is string is DEFINATELY NOT present, or true if it MAY be present</returns>
		public bool PossiblyContains(char cChar1,char cChar2,int iLength)
		{
			ushort usPos=usChars[(int)cChar1,(int)cChar2];

			if(usPos==0)
				return false;

			return true;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		bool bDisposed=false;

		private void Dispose(bool bDisposing)
		{
			if(!bDisposed)
			{
				oHash=null;
				usChars=null;
				iValues=null;
			}

		}

	}
}
