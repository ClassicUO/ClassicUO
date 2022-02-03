using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace Majestic12
{

	/// <summary>
	/// Type of parsed HTML chunk (token), each non-null returned chunk from HTMLparser will have oType set to 
	/// one of these values
	/// </summary>
	public enum HTMLchunkType
	{
		/// <summary>
		/// Text data from HTML
		/// </summary>
		Text		= 0,

		/// <summary>
		/// Open tag, possibly with attributes
		/// </summary>
		OpenTag		= 1,

		/// <summary>
		/// Closed tag (it may still have attributes)
		/// </summary>
		CloseTag	= 2,

		/// <summary>
		/// Comment tag (<!-- -->)depending on HTMLparser boolean flags you may have:
		/// a) nothing to oHTML variable - for faster performance, call SetRawHTML function in parser
		/// b) data BETWEEN tags (but not including comment tags themselves) - DEFAULT
		/// c) complete RAW HTML representing data between tags and tags themselves (same as you get in a) when
		/// you call SetRawHTML function)
        /// 
        /// Note: this can also be CDATA part of XML document - see sTag value to determine if its proper comment
        /// or CDATA or (in the future) something else
		/// </summary>
		Comment		= 3,

		/// <summary>
		/// Script tag (<!-- -->) depending on HTMLparser boolean flags
		/// a) nothing to oHTML variable - for faster performance, call SetRawHTML function in parser
		/// b) data BETWEEN tags (but not including comment tags themselves) - DEFAULT
		/// c) complete RAW HTML representing data between tags and tags themselves (same as you get in a) when
		/// you call SetRawHTML function)
		/// </summary>
		Script		= 4,
	};

	/// <summary>
	/// Parsed HTML token that is either text, comment, script, open or closed tag as indicated by the oType variable.
	/// </summary>
	public class HTMLchunk : IDisposable
	{	
		/// <summary>
		/// Maximum default capacity of buffer that will keep data
		/// </summary>
		/// <exclude/>
		public static int TEXT_CAPACITY=1024*256;

		/// <summary>
		/// Maximum number of parameters in a tag - should be high enough to fit most sensible cases
		/// </summary>
		/// <exclude/>
		public static int MAX_PARAMS=256;	

		/// <summary>
		/// Chunk type showing whether its text, open or close tag, comments or script.
		/// WARNING: if type is comments or script then you have to manually call Finalise(); method
		/// in order to have actual text of comments/scripts in oHTML variable
		/// </summary>
		public HTMLchunkType oType;

		/// <summary>
		/// If true then tag params will be kept in a hash rather than in a fixed size arrays. 
		/// This will be slow down parsing, but make it easier to use.
		/// </summary>
		public bool bHashMode=true;

		/// <summary>
		/// For TAGS: it stores raw HTML that was parsed to generate thus chunk will be here UNLESS
		/// HTMLparser was configured not to store it there as it can improve performance
		/// <p>
		/// For TEXT or COMMENTS: actual text or comments - you MUST call Finalise(); first.
		/// </p>
		/// </summary>
		public string oHTML="";

		/// <summary>
		/// Offset in bHTML data array at which this chunk starts
		/// </summary>
		public int iChunkOffset=0;

		/// <summary>
		/// Length of the chunk in bHTML data array
		/// </summary>
		public int iChunkLength=0;

		/// <summary>
		/// If its open/close tag type then this is where lowercased Tag will be kept
		/// </summary>
		public string sTag="";

		/// <summary>
		/// If true then it must be closed tag
		/// </summary>
		/// <exclude/>
		public bool bClosure=false;

		/// <summary>
		/// If true then it must be closed tag and closure sign / was at the END of tag, ie this is a SOLO
		/// tag 
		/// </summary>
		/// <exclude/>
		public bool bEndClosure=false;

		/// <summary>
		/// If true then it must be comments tag
		/// </summary>
		/// <exclude/>
		public bool bComments=false;
		
		/// <summary>
		/// True if entities were present (and transformed) in the original HTML
		/// </summary>
		/// <exclude/>
		public bool bEntities=false;

		/// <summary>
		/// Set to true if &lt; entity (tag start) was found 
		/// </summary>
		/// <exclude/>
		public bool bLtEntity=false;

		/// <summary>
		/// Hashtable with tag parameters: keys are param names and values are param values.
		/// ONLY used if bHashMode is set to TRUE.
		/// </summary>
		public Dictionary<string, string> oParams=null; 

		/// <summary>
		/// Number of parameters and values stored in sParams array, OR in oParams hashtable if
		/// bHashMode is true
		/// </summary>
		public int iParams=0;

		/// <summary>
		/// Param names will be stored here - actual number is in iParams.
		/// ONLY used if bHashMode is set to FALSE.
		/// </summary>
		public string[] sParams=new string[MAX_PARAMS];

		/// <summary>
		/// Param values will be stored here - actual number is in iParams.
		/// ONLY used if bHashMode is set to FALSE.
		/// </summary>
		public string[] sValues=new string[MAX_PARAMS];

		/// <summary>
		/// Character used to quote param's value: it is taken actually from parsed HTML
		/// </summary>
		public byte[] cParamChars=new byte[MAX_PARAMS];

		/// <summary>
		/// Encoder to be used for conversion of binary data into strings, Encoding.Default is used by default,
		/// but it can be changed if top level user of the parser detects that encoding was different
		/// </summary>
		public Encoding oEnc=Encoding.Default;

		bool bDisposed=false;

		/// <summary>
		/// This function will convert parameters stored in sParams/sValues arrays into oParams hash
		/// Useful if generally parsing is done when bHashMode is FALSE. Hash operations are not the fastest, so
		/// its best not to use this function.
		/// </summary>
		public void ConvertParamsToHash()
		{
            if (oParams != null)
                oParams.Clear();
            else
                oParams = new Dictionary<string, string>();

			for(int i=0; i<iParams; i++)
			{
				oParams[sParams[i]]=sValues[i];
			}

		}

		/// <summary>
		/// Sets encoding to be used for conversion of binary data into string
		/// </summary>
		/// <param name="p_oEnc">Encoding object</param>
		public void SetEncoding(Encoding p_oEnc)
		{
			oEnc=p_oEnc;
		}

		/// <summary>
		/// Generates HTML based on current chunk's data 
		/// Note: this is not a high performance method and if you want ORIGINAL HTML that was parsed to create
		/// this chunk then use relevant HTMLparser method to obtain such HTML then you should use
        /// function of parser: SetRawHTML
        /// 
		/// </summary>
		/// <returns>HTML equivalent of this chunk</returns>
		public string GenerateHTML()
		{
			string sHTML="";

			switch(oType)
			{
					// matched open tag, ie <a href="">
				case HTMLchunkType.OpenTag:
					sHTML+="<"+sTag;

					if(iParams>0)
						sHTML+=" "+GenerateParamsHTML();

					sHTML+=">";

					break;

					// matched close tag, ie </a>
				case HTMLchunkType.CloseTag:

					if(iParams>0 || bEndClosure)
					{
						sHTML+="<"+sTag;

						if(iParams>0)
							sHTML+=" "+GenerateParamsHTML();

						sHTML+="/>";
					}
					else
						sHTML+="</"+sTag+">";
					break;

				case HTMLchunkType.Script:

					if(oHTML.Length==0)
						sHTML="<script>n/a</script>";
					else
						sHTML=oHTML;
					
					break;

				case HTMLchunkType.Comment:

                    // note: we might have CDATA here that we treat as comments

                    if(sTag=="!--")
                    {

                        if(oHTML.Length==0)
                            sHTML="<!-- n/a -->";
                        else
                            sHTML="<!--"+oHTML+"-->";
                    }
                    else
                    {
                        // ref: http://www.w3schools.com/xml/xml_cdata.asp
                        if(sTag=="![CDATA[")
                        {
                            if(oHTML.Length==0)
                                sHTML="<![CDATA[ n/a \n]]>";
                            else
                                sHTML="<![CDATA["+oHTML+"]]>";
                        }

                    }


					break;

					// matched normal text
				case HTMLchunkType.Text:

					return oHTML;
			};

			return sHTML;
		}

		/// <summary>
		/// Returns value of a parameter
		/// </summary>
		/// <param name="sParam">Parameter</param>
		/// <returns>Parameter value or empty string</returns>
		public string GetParamValue(string sParam)
		{
			if(!bHashMode)
			{			
				for(int i=0; i<iParams; i++)
				{
					if(sParams[i]==sParam)
						return sValues[i];
				}
			}
			else
			{
				object oValue=oParams[sParam];

				if(oValue!=null)
					return (string)oValue;
			}

			return "";
		}

		/// <summary>
		/// Generates HTML for params in this chunk
		/// </summary>
		/// <returns>String with HTML corresponding to params</returns>
		public string GenerateParamsHTML()
		{
			string sParamHTML="";

			if(bHashMode)
			{
				if(oParams.Count>0)
				{
					foreach(string sParam in oParams.Keys)
					{
						string sValue=(string)oParams[sParam];

						if(sParamHTML.Length>0)
							sParamHTML+=" ";

						// FIXIT: this is really not correct as we do not use same char used
						sParamHTML+=GenerateParamHTML(sParam,sValue,'\'');
					}

				}
			}
			else
			{
				// this is alternative method of getting params -- it may look less convinient
				// but it saves a LOT of CPU ticks while parsing. It makes sense when you only need
				// params for a few
				if(iParams>0)
				{
					for(int i=0; i<iParams; i++)
					{
						if(sParamHTML.Length>0)
							sParamHTML+=" ";

						sParamHTML+=GenerateParamHTML(sParams[i],sValues[i],(char)cParamChars[i]);
					}

				}
						
			}

			return sParamHTML;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool bDisposing)
		{
			if(!bDisposed)
			{
				if(oParams!=null)
					oParams=null;
			
				sParams=null;
				sValues=null;
			}

			bDisposed=true;
		}

		/// <summary>
		/// Generates HTML for param/value pair
		/// </summary>
		/// <param name="sParam">Param</param>
		/// <param name="sValue">Value (empty if not specified)</param>
		/// <returns>String with HTML</returns>
		public string GenerateParamHTML(string sParam,string sValue,char cParamChar)
		{
			if(sValue.Length>0)
			{
				// check param's value for whitespace or quote chars, if they are not present, then
				// we can save 2 bytes by not generating quotes
				if(sValue.Length>20)
					return sParam+"="+cParamChar+MakeSafeParamValue(sValue,cParamChar)+cParamChar;

				for(int i=0; i<sValue.Length; i++)
				{
					switch(sValue[i])
					{
						case ' ':
						case '\t':
						case '\'':
						case '\"':
						case '\n':
						case '\r':
							return sParam+"='"+MakeSafeParamValue(sValue,'\'')+"'";

						default:
							break;
					};
				}

				return sParam+"="+sValue;
			}
			else
				return sParam;
		}

		/// <summary>
		/// Makes parameter value safe to be used in param - this will check for any conflicting quote chars,
		/// but not full entity-encoding
		/// </summary>
		/// <param name="sLine">Line of text</param>
		/// <param name="cQuoteChar">Quote char used in param - any such chars in text will be entity-encoded</param>
		/// <returns>Safe text to be used as param's value</returns>
		public static string MakeSafeParamValue(string sLine,char cQuoteChar)
		{
			// we speculatievly expect that in most cases we don't actually need to entity-encode string,

			for(int i=0; i<sLine.Length; i++)
			{
				if(sLine[i]==cQuoteChar)
				{
					// have to restart here
					StringBuilder oSB=new StringBuilder(sLine.Length+10);

					oSB.Append(sLine.Substring(0,i));

					for(int j=i; j<sLine.Length; j++)
					{
						char cChar=sLine[j];

						if(cChar==cQuoteChar)
							oSB.Append("&#"+((int)cChar).ToString()+";");
						else
							oSB.Append(cChar);
					}

					return oSB.ToString();
				}
			}
			

			return sLine;
		}

		/// <summary>
		/// Adds tag parameter to the chunk
		/// </summary>
		/// <param name="sParam">Parameter name (ie color)</param>
		/// <param name="sValue">Value of the parameter (ie white)</param>
		public void AddParam(string sParam,string sValue,byte cParamChar)
		{
			if(!bHashMode)
			{

				if(iParams<MAX_PARAMS)
				{
					sParams[iParams]=sParam;
					sValues[iParams]=sValue;
					cParamChars[iParams]=cParamChar;

					iParams++;
				}
			}
			else
			{
				iParams++;
				oParams[sParam]=sValue;
			}

		}

		/// <summary>
		/// Clears chunk preparing it for 
		/// </summary>
		public void Clear()
		{
			sTag=oHTML="";
			bLtEntity=bEntities=bComments=bClosure=bEndClosure=false;

			iParams=0;
			
			if(bHashMode)
			{
				if(oParams!=null)
					oParams.Clear();
			}

		}

		/// <summary>
		/// Initialises new HTMLchunk
		/// </summary>
		/// <param name="p_bHashMode">Sets <seealso cref="bHashMode"/></param>
		public HTMLchunk(bool p_bHashMode)
		{
			bHashMode=p_bHashMode;

            if (bHashMode)
                oParams = new Dictionary<string, string>();
		}

	}

}
