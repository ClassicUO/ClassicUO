using System;
using System.Text;

namespace Majestic12
{
	/// <summary>
	/// Implements parsing of entities
	/// </summary>
	public class HTMLentities
	{
		/// <summary>
		/// Supported HTML entities
		/// </summary>
		FastHash oEntities=null;

		/// <summary>
		/// Supported HTML entities
		/// </summary>
		public static FastHash oAllEntities=null;

		/// <summary>
		/// Internal heuristics for entiries: these will be set to min and max string lengths of known HTML entities
		/// </summary>
		int iMinEntityLen=0,iMaxEntityLen=0;

		static int iAllMinEntityLen=0,iAllMaxEntityLen=0;

		/// <summary>
		/// Array to provide reverse lookup for entities
		/// </summary>
		public static string[] sEntityReverseLookup;

		/// <summary>
		/// If true then only minimal set of entities will be parsed, everything else including numbers based
		/// entities will be returned as is. This is useful for when HTML content needs to be extracted with subsequent parsing, in this case resolution of entities will be a problem
		/// </summary>
		internal bool bMiniEntities=false;

		/// <summary>
		/// If false then HTML entities (like "nbsp") will not be decoded
		/// </summary>
		internal bool bDecodeEntities=false;

		static HTMLentities()
		{
			oAllEntities=InitEntities(ref iAllMinEntityLen,ref iAllMaxEntityLen,out sEntityReverseLookup);
		}

		internal HTMLentities()
		{
			oEntities=InitEntities(ref iMinEntityLen,ref iMaxEntityLen,out sEntityReverseLookup);	
			bMiniEntities=false;
		}
		/// <summary>
		/// This function will be called when & is found, and it will
		/// peek forward to check if its entity, should there be a success
		/// indicated by non-zero returned, the pointer will be left at the new byte
		/// after entity
		/// </summary>
		/// <returns>Char (not byte) that corresponds to the entity or 0 if it was not entity</returns>
#if UNSAFE_CODE
		internal unsafe char CheckForEntity(byte[] bHTML,ref int iCurPos)
#else
		internal char CheckForEntity(byte[] bHTML,ref int iCurPos,int iDataLength)
#endif
		{
			if(!bDecodeEntities && !bMiniEntities)
				return (char)0;

			int iChars=0;
			byte cChar;
			//string sEntity="";

			// if true it means we are getting hex or decimal value of the byte
			bool bCharCode=false;
			bool bCharCodeHex=false;

			int iEntLen=0;

			int iFrom=iCurPos;

			string sEntity;

			try
			{

				/*
				while(!Eof())
				{
					cChar=NextChar();
				*/
				while(iCurPos<iDataLength)
				{
					cChar=bHTML[iCurPos++];

					// 21/10/05: not necessary
					//if(cChar==0)
					//	break;

					if(++iChars<=2)
					{

						// the first byte for numbers should be #
						if(iChars==1)
						{
							if(cChar=='#')
							{
								iFrom++;
								bCharCode=true;
								continue;
							}
						}
						else
						{

							if(bCharCode && cChar=='x')
							{
								iFrom++;
								iEntLen--;
								bCharCodeHex=true;
							}
						}
					}

					//Console.WriteLine("Got entity end: {0}",sEntity);
					// Break on:
					// 1) ; - proper end of entity
					// 2) number 10-based entity but current byte is not a number
					//if(cChar==';' || (bCharCode && !bCharCodeHex && !char.IsNumber((char)cChar)))
					
					// TODO: browsers appear to be lax about ; requirement for end of entity 
					// we should really do the same and treat whitespace as termination of entity
					if(cChar==';' || (bCharCode && !bCharCodeHex && !(cChar>='0' && cChar<='9')))
					{
						// lets try speculative quick lookup using just first 2 chars
						// this should be successful in almost all cases thus removing need for
						// expensive creation of a string 
						if(!bCharCode && iEntLen>1)
						{
							object oChar=oEntities.GetLikelyPresentValue(bHTML[iFrom],bHTML[iFrom+1]);

							if(oChar!=null)
							{
								return (char)((int)oChar);
							}
						}

						// check if its int - this way we can avoid having to create string 
						if(bCharCode && iEntLen>0 && !bCharCodeHex)
						{
							// if mini entities mode is set then we will ignore all numerics
							if(bMiniEntities)
								break;

							// we have to backdown one char in case when entity did not end with ; 
							// otherwise we will lose next char in the stream, this correction suggested by Kurt Carlson! 
							if(cChar != ';') 
							    iCurPos--;
	
							return (char) ParseUInt(bHTML,iFrom,iEntLen);
						}

#if UNSAFE_CODE
						fixed(byte* pBuffer=bHTML)
						{
							sEntity=new String((sbyte*)pBuffer,iFrom,iEntLen,System.Text.Encoding.Default);
#else
					
						sEntity=System.Text.Encoding.Default.GetString(bHTML,iFrom,iEntLen);
#endif

						if(bCharCode)
						{
							// NOTE: this may fail due to wrong data format,
							// in which case we will return 0, and entity will be
							// ignored
							if(iEntLen>0)
							{
								// if mini entities mode is set then we will ignore all numerics
								if(bMiniEntities)
									break;

								int iChar;

								if(!bCharCodeHex)
								{
#if DOTNET20
									// we want to avoid exceptions if possible as they are slow
									if(!int.TryParse(sEntity,out iChar))
									{
										if(iChars>0)
										{
											if((iCurPos-iChars)>=0)
												iCurPos-=iChars;

											//PutChars(iChars);
										}

										return (char)(0);
									}
#else
									iChar=int.Parse(sEntity);
#endif
								}
								else
								{
#if DOTNET20
									// we want to avoid exceptions if possible as they are very slow
									if(!int.TryParse(sEntity,System.Globalization.NumberStyles.HexNumber,null,out iChar))
									{
										if(iChars>0)
										{
											if((iCurPos-iChars)>=0)
												iCurPos-=iChars;

											//PutChars(iChars);
										}
										return (char)(0);
									}
#else

									iChar=int.Parse(sEntity,System.Globalization.NumberStyles.HexNumber);
#endif
								}
								
								return (char)iChar;
							}
						}
								
						if(iEntLen>=iMinEntityLen && iEntLen<=iMaxEntityLen)
						{
							object oChar=oEntities.GetLikelyPresentValue(sEntity);

							if(oChar!=null)
								return (char)((int)oChar);
						}
					}

					//break;
					

					// as soon as entity length exceed max length of entity known to us
					// we break up the loop and return nothing found

					// NOTE: removed due to entities being generally correct and this code costs 10% of CPU in this function
					
					if(iEntLen>iMaxEntityLen)
						break;
					
					iEntLen++;
				}
			}
			catch //(Exception oEx)
			{
				//Console.WriteLine("Entity parsing exception: "+oEx.ToString());
			}

			// if we have not found squat, then we will need to put point back
			// to where it was before this function was called
			if(iChars>0)
			{
				if((iCurPos-iChars)>=0)
					iCurPos-=iChars;
				
				//PutChars(iChars);
			}

			return (char)(0);
		}

		/// <summary>
		/// This function will decode any entities found in a string - not fast!
		/// </summary>
		/// <returns>Possibly decoded string</returns>
#if UNSAFE_CODE
		internal static unsafe char DecodeEntities()
#else
		internal static string DecodeEntities(string sData)
#endif
		{
			char cChar;

			StringBuilder oSB=new StringBuilder(sData.Length);

			string sEntity="";

			try
			{
				for(int i=0; i<sData.Length; i++)
				{
					cChar=sData[i];

					if(cChar!='&' || (i+1>=sData.Length))
					{
						oSB.Append(cChar);
					}
					else
					{
						// if true it means we are getting hex or decimal value of the byte
						bool bCharCode=false;
						bool bCharCodeHex=false;
						int iEntLen=0;
						int iChars=0;

						int j=i+1;

						int iFrom=i+1;

						for(; j<sData.Length; j++)
						{
							cChar=sData[j];

							if(++iChars<=2)
							{

								// the first byte for numbers should be #
								if(iChars==1)
								{
									if(cChar=='#')
									{
										iFrom++;
										bCharCode=true;
										continue;
									}
								}
								else
								{

									if(bCharCode && cChar=='x' && !bCharCodeHex)
									{
										iFrom++;
										//iEntLen--;
										bCharCodeHex=true;
										continue;
									}
								}
							}

							//Console.WriteLine("Got entity end: {0}",sEntity);
							// Break on:
							// 1) ; - proper end of entity
							// 2) number 10-based entity but current byte is not a number
							//if(cChar==';' || (bCharCode && !bCharCodeHex && !char.IsNumber((char)cChar)))
							bool bLastChar=j+1>=sData.Length;

							if(cChar==';' || (bCharCode && !bCharCodeHex && !(cChar>='0' && cChar<='9')) || (bCharCode && bLastChar))
							{
								// end of string 
								if(bLastChar && cChar!=';')
									iEntLen++;

								// lets try speculative quick lookup using just first 2 chars
								// this should be successful in almost all cases thus removing need for
								// expensive creation of a string 
								if(!bCharCode && iEntLen>1)
								{
									// make sure we aint at the end of string
									if(i+2<sData.Length)
									{

										object oChar=oAllEntities.GetLikelyPresentValue((byte)sData[i+1],(byte)sData[i+2]);

										if(oChar!=null)
										{
											oSB.Append((char)((int)oChar));
											break;
										}

									}

								}

								// check if its int - this way we can avoid having to create string 
								if(bCharCode && iEntLen>0 && !bCharCodeHex)
								{
									sEntity=sData.Substring(iFrom,iEntLen);

									int iChar=0;
									bool bSuccess=false;

									try
									{
										iChar=(int)uint.Parse(sEntity);
										bSuccess=true;
									}
									catch	   
									{
									}

									if(bSuccess)
									{
										oSB.Append((char)iChar);

										// move back once when we got number done without ; at the end
										// of it - Firefox and IE do it this way
										if(cChar!=';' && !bLastChar)
											j--;

										break;
									}
									else
									{
										// this will force to add entity as is - probably broken
										// or maybe not entity at all
										oSB.Append('&');
										j=i;
										break;
									}

								}
#if UNSAFE_CODE
						fixed(byte* pBuffer=bHTML)
						{
							sEntity=new String((sbyte*)pBuffer,iFrom,iEntLen,System.Text.Encoding.Default);
#else
					
								sEntity=sData.Substring(iFrom,iEntLen);
#endif

								if(bCharCode)
								{
									// NOTE: this may fail due to wrong data format,
									// in which case we will return 0, and entity will be
									// ignored
									if(iEntLen>0)
									{
										int iChar=0;
										bool bSuccess=false;

#if DOTNET20 && false
										if(!bCharCodeHex) 
											bSuccess=int.TryParse(sEntity,out iChar);
										else
											bSuccess=int.TryParse(sEntity,System.Globalization.NumberStyles.HexNumber,out iChar);
#else
										try
										{
											if(!bCharCodeHex)
												iChar=int.Parse(sEntity);
											else
												iChar=int.Parse(sEntity,System.Globalization.NumberStyles.HexNumber);

											bSuccess=true;
										}
										catch
										{
											
											// some numbers might not be parsed correctly so we will ignore them
										}
#endif
										if(bSuccess)
										{
											oSB.Append((char)iChar);
											break;
										}
										else
										{
											// this will force to add entity as is - probably broken
											// or maybe not entity at all
											iEntLen=iAllMaxEntityLen+1;
										}

									}
								}
								
								if(iEntLen>=iAllMinEntityLen && iEntLen<=iAllMaxEntityLen)
								{
									object oChar=oAllEntities.GetLikelyPresentValue(sEntity);

									if(oChar!=null)
									{
										oSB.Append((char)((int)oChar));
										break;
									}
									else
									{
										// this will force to add entity as is - probably broken
										// or maybe not entity at all
										iEntLen=iAllMaxEntityLen+1;
										//Utils.Write("");
									}
								}
							}

							//break;
					

							// as soon as entity length exceed max length of entity known to us
							// we break up the loop and return nothing found

							// NOTE: removed due to entities being generally correct and this code costs 10% of CPU in this function

							if(iEntLen>iAllMaxEntityLen || bLastChar)
							{
								// append char that triggered entity thingy in the first place
								oSB.Append('&');
								j=i;
								break;
							}
					
							iEntLen++;
						}

						i=j;
					}
				}
			}
			catch(Exception oEx)
			{
				Console.WriteLine("Entity parsing exception: "+oEx.ToString());

				return sData;
			}

			return oSB.ToString();
		}

		/// <summary>
		/// Multipliers for base 10 
		/// </summary>
		static uint[] iDecMultipliers={ 1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000 };

		/// <summary>
		/// Parses an unsigned integer number from byte buffer
		/// </summary>
		/// <param name="bBuf">Buffer to parse from</param>
		/// <param name="iFrom">Start parsing from this point</param>
		/// <param name="iLength">Length of data to parse</param>
		/// <returns>Unsigned integer number</returns>
		uint ParseUInt(byte[] bBuf,int iFrom,int iLength)
		{
			uint iNum=0;

			int iOrder=0;

			for(int i=iFrom+iLength-1; i>=iFrom; i--)
			{
				byte b=bBuf[i];

				if(b<(byte)'0' || b>(byte)'9')
					break;

				//Console.WriteLine("Order={0}, Byte={1}",iOrder,b);

				iNum+=(uint) (iDecMultipliers[iOrder++]*(b-(byte)'0'));
			}

			return iNum;
		}

		/// <summary>
		/// Initialises list of entities
		/// </summary>
		private static FastHash InitEntities(ref int iMinEntityLen,ref int iMaxEntityLen,out string[] sEntityReverseLookup)
		{
			FastHash oEntities=new FastHash();

			// FIXIT: we will treat non-breakable space... as space!?!
			// perhaps it would be better to have separate return types for entities?
			oEntities.Add("nbsp",32); //oEntities.Add("nbsp",160);
			oEntities.Add("iexcl",161);
			oEntities.Add("cent",162);
			oEntities.Add("pound",163);
			oEntities.Add("curren",164);
			oEntities.Add("yen",165);
			oEntities.Add("brvbar",166);
			oEntities.Add("sect",167);
			oEntities.Add("uml",168);
			oEntities.Add("copy",169);
			oEntities.Add("ordf",170);
			oEntities.Add("laquo",171);
			oEntities.Add("not",172);
			oEntities.Add("shy",173);
			oEntities.Add("reg",174);
			oEntities.Add("macr",175);
			oEntities.Add("deg",176);
			oEntities.Add("plusmn",177);
			oEntities.Add("sup2",178);
			oEntities.Add("sup3",179);
			oEntities.Add("acute",180);
			oEntities.Add("micro",181);
			oEntities.Add("para",182);
			oEntities.Add("middot",183);
			oEntities.Add("cedil",184);
			oEntities.Add("sup1",185);
			oEntities.Add("ordm",186);
			oEntities.Add("raquo",187);
			oEntities.Add("frac14",188);
			oEntities.Add("frac12",189);
			oEntities.Add("frac34",190);
			oEntities.Add("iquest",191);
			oEntities.Add("Agrave",192);
			oEntities.Add("Aacute",193);
			oEntities.Add("Acirc",194);
			oEntities.Add("Atilde",195);
			oEntities.Add("Auml",196);
			oEntities.Add("Aring",197);
			oEntities.Add("AElig",198);
			oEntities.Add("Ccedil",199);
			oEntities.Add("Egrave",200);
			oEntities.Add("Eacute",201);
			oEntities.Add("Ecirc",202);
			oEntities.Add("Euml",203);
			oEntities.Add("Igrave",204);
			oEntities.Add("Iacute",205);
			oEntities.Add("Icirc",206);
			oEntities.Add("Iuml",207);
			oEntities.Add("ETH",208);
			oEntities.Add("Ntilde",209);
			oEntities.Add("Ograve",210);
			oEntities.Add("Oacute",211);
			oEntities.Add("Ocirc",212);
			oEntities.Add("Otilde",213);
			oEntities.Add("Ouml",214);
			oEntities.Add("times",215);
			oEntities.Add("Oslash",216);
			oEntities.Add("Ugrave",217);
			oEntities.Add("Uacute",218);
			oEntities.Add("Ucirc",219);
			oEntities.Add("Uuml",220);
			oEntities.Add("Yacute",221);
			oEntities.Add("THORN",222);
			oEntities.Add("szlig",223);
			oEntities.Add("agrave",224);
			oEntities.Add("aacute",225);
			oEntities.Add("acirc",226);
			oEntities.Add("atilde",227);
			oEntities.Add("auml",228);
			oEntities.Add("aring",229);
			oEntities.Add("aelig",230);
			oEntities.Add("ccedil",231);
			oEntities.Add("egrave",232);
			oEntities.Add("eacute",233);
			oEntities.Add("ecirc",234);
			oEntities.Add("euml",235);
			oEntities.Add("igrave",236);
			oEntities.Add("iacute",237);
			oEntities.Add("icirc",238);
			oEntities.Add("iuml",239);
			oEntities.Add("eth",240);
			oEntities.Add("ntilde",241);
			oEntities.Add("ograve",242);
			oEntities.Add("oacute",243);
			oEntities.Add("ocirc",244);
			oEntities.Add("otilde",245);
			oEntities.Add("ouml",246);
			oEntities.Add("divide",247);
			oEntities.Add("oslash",248);
			oEntities.Add("ugrave",249);
			oEntities.Add("uacute",250);
			oEntities.Add("ucirc",251);
			oEntities.Add("uuml",252);
			oEntities.Add("yacute",253);
			oEntities.Add("thorn",254);
			oEntities.Add("yuml",255);
			oEntities.Add("quot",34);

            // NOTE: this is a not a proper entity but a fairly common mistake - & is important symbol
            // and we don't want to lose it even if webmaster used upper case instead of lower
            oEntities.Add("AMP",38);
            oEntities.Add("REG",174);

			oEntities.Add("amp",38);
            oEntities.Add("reg",174);
			

			oEntities.Add("lt",60);
			oEntities.Add("gt",62);
			// ' - apparently does not work in IE
			oEntities.Add("apos",39);

			// unicode supported by default
			if(true)
			{
				oEntities.Add("OElig",338);
				oEntities.Add("oelig",339);
				oEntities.Add("Scaron",352);
				oEntities.Add("scaron",353);
				oEntities.Add("Yuml",376);
				oEntities.Add("circ",710);
				oEntities.Add("tilde",732);
				oEntities.Add("ensp",8194);
				oEntities.Add("emsp",8195);
				oEntities.Add("thinsp",8201);
				oEntities.Add("zwnj",8204);
				oEntities.Add("zwj",8205);
				oEntities.Add("lrm",8206);
				oEntities.Add("rlm",8207);
				oEntities.Add("ndash",8211);
				oEntities.Add("mdash",8212);
				oEntities.Add("lsquo",8216);
				oEntities.Add("rsquo",8217);
				oEntities.Add("sbquo",8218);
				oEntities.Add("ldquo",8220);
				oEntities.Add("rdquo",8221);
				oEntities.Add("bdquo",8222);
				oEntities.Add("dagger",8224);
				oEntities.Add("Dagger",8225);
				oEntities.Add("permil",8240);
				oEntities.Add("lsaquo",8249);
				oEntities.Add("rsaquo",8250);
				oEntities.Add("euro",8364);
				oEntities.Add("fnof",402);
				oEntities.Add("Alpha",913);
				oEntities.Add("Beta",914);
				oEntities.Add("Gamma",915);
				oEntities.Add("Delta",916);
				oEntities.Add("Epsilon",917);
				oEntities.Add("Zeta",918);
				oEntities.Add("Eta",919);
				oEntities.Add("Theta",920);
				oEntities.Add("Iota",921);
				oEntities.Add("Kappa",922);
				oEntities.Add("Lambda",923);
				oEntities.Add("Mu",924);
				oEntities.Add("Nu",925);
				oEntities.Add("Xi",926);
				oEntities.Add("Omicron",927);
				oEntities.Add("Pi",928);
				oEntities.Add("Rho",929);
				oEntities.Add("Sigma",931);
				oEntities.Add("Tau",932);
				oEntities.Add("Upsilon",933);
				oEntities.Add("Phi",934);
				oEntities.Add("Chi",935);
				oEntities.Add("Psi",936);
				oEntities.Add("Omega",937);
				oEntities.Add("alpha",945);
				oEntities.Add("beta",946);
				oEntities.Add("gamma",947);
				oEntities.Add("delta",948);
				oEntities.Add("epsilon",949);
				oEntities.Add("zeta",950);
				oEntities.Add("eta",951);
				oEntities.Add("theta",952);
				oEntities.Add("iota",953);
				oEntities.Add("kappa",954);
				oEntities.Add("lambda",955);
				oEntities.Add("mu",956);
				oEntities.Add("nu",957);
				oEntities.Add("xi",958);
				oEntities.Add("omicron",959);
				oEntities.Add("pi",960);
				oEntities.Add("rho",961);
				oEntities.Add("sigmaf",962);
				oEntities.Add("sigma",963);
				oEntities.Add("tau",964);
				oEntities.Add("upsilon",965);
				oEntities.Add("phi",966);
				oEntities.Add("chi",967);
				oEntities.Add("psi",968);
				oEntities.Add("omega",969);
				oEntities.Add("thetasym",977);
				oEntities.Add("upsih",978);
				oEntities.Add("piv",982);
				oEntities.Add("bull",8226);
				oEntities.Add("hellip",8230);
				oEntities.Add("prime",8242);
				oEntities.Add("Prime",8243);
				oEntities.Add("oline",8254);
				oEntities.Add("frasl",8260);
				oEntities.Add("weierp",8472);
				oEntities.Add("image",8465);
				oEntities.Add("real",8476);
				oEntities.Add("trade",8482);
				oEntities.Add("alefsym",8501);
				oEntities.Add("larr",8592);
				oEntities.Add("uarr",8593);
				oEntities.Add("rarr",8594);
				oEntities.Add("darr",8595);
				oEntities.Add("harr",8596);
				oEntities.Add("crarr",8629);
				oEntities.Add("lArr",8656);
				oEntities.Add("uArr",8657);
				oEntities.Add("rArr",8658);
				oEntities.Add("dArr",8659);
				oEntities.Add("hArr",8660);
				oEntities.Add("forall",8704);
				oEntities.Add("part",8706);
				oEntities.Add("exist",8707);
				oEntities.Add("empty",8709);
				oEntities.Add("nabla",8711);
				oEntities.Add("isin",8712);
				oEntities.Add("notin",8713);
				oEntities.Add("ni",8715);
				oEntities.Add("prod",8719);
				oEntities.Add("sum",8721);
				oEntities.Add("minus",8722);
				oEntities.Add("lowast",8727);
				oEntities.Add("radic",8730);
				oEntities.Add("prop",8733);
				oEntities.Add("infin",8734);
				oEntities.Add("ang",8736);
				oEntities.Add("and",8743);
				oEntities.Add("or",8744);
				oEntities.Add("cap",8745);
				oEntities.Add("cup",8746);
				oEntities.Add("int",8747);
				oEntities.Add("there4",8756);
				oEntities.Add("sim",8764);
				oEntities.Add("cong",8773);
				oEntities.Add("asymp",8776);
				oEntities.Add("ne",8800);
				oEntities.Add("equiv",8801);
				oEntities.Add("le",8804);
				oEntities.Add("ge",8805);
				oEntities.Add("sub",8834);
				oEntities.Add("sup",8835);
				oEntities.Add("nsub",8836);
				oEntities.Add("sube",8838);
				oEntities.Add("supe",8839);
				oEntities.Add("oplus",8853);
				oEntities.Add("otimes",8855);
				oEntities.Add("perp",8869);
				oEntities.Add("sdot",8901);
				oEntities.Add("lceil",8968);
				oEntities.Add("rceil",8969);
				oEntities.Add("lfloor",8970);
				oEntities.Add("rfloor",8971);
				oEntities.Add("lang",9001);
				oEntities.Add("rang",9002);
				oEntities.Add("loz",9674);
				oEntities.Add("spades",9824);
				oEntities.Add("clubs",9827);
				oEntities.Add("hearts",9829);
				oEntities.Add("diams",9830);
			}

			sEntityReverseLookup=new string[10000];
				
			// calculate min/max lenght of known entities
			foreach(string sKey in oEntities.Keys)
			{
				if(sKey.Length<iMinEntityLen || iMinEntityLen==0)
					iMinEntityLen=sKey.Length;

				if(sKey.Length>iMaxEntityLen || iMaxEntityLen==0)
					iMaxEntityLen=sKey.Length;

				// remember key at given offset
                if(sKey!="AMP" && sKey!="REG")
				    sEntityReverseLookup[(int)oEntities[sKey]]=sKey;
			}

			// we don't want to change spaces
			sEntityReverseLookup[32]=null;

			return oEntities;
		}


		/// <summary>
		/// Parses line and changes known entiry characters into proper HTML entiries
		/// </summary>
		/// <param name="sLine">Line of text</param>
		/// <param name="iFrom">Char from which scanning should start</param>
		/// <returns>Line of text with proper HTML entities</returns>
		internal string ChangeToEntities(string sLine,int iFrom,bool bChangeDangerousCharsOnly)
		{													   
			StringBuilder oSB=new StringBuilder(sLine.Length);

			if(iFrom>0)
				oSB.Append(sLine.Substring(0,iFrom));

			for(int i=iFrom; i<sLine.Length; i++)
			{				
				char cChar=sLine[i];

				// yeah I know its lame but its 3:30am and I had v.long debugging session :-/
				switch((int)cChar)
				{
					case 39:
					case 145:
					case 146:
					case 147:
					case 148:
						oSB.Append("&#"+((int)cChar).ToString()+";");
						continue;

					default:

						if(cChar<32) // || (bChangeAllNonASCII && cChar>127))
						{
							//cChar=(char)0x01;
							//Utils.Write("");
							goto case 148;
						}

						break;
				};

				if(cChar<sEntityReverseLookup.Length)
				{
		
					if(sEntityReverseLookup[(int)cChar]!=null)
					{
						// 04/07/07 This seems to provide greater compatibility with crappy parsers
						// like that used in PHP 4: it was choking on &raquo;	
						if(bChangeDangerousCharsOnly)
						{
							switch(cChar)
							{
								case '>':
								case '<':
								case '\"':
								case '\'':
								case '/':
								case '&':
									break;

									// ignore most of chars then - they should be encoded using encoding then, not entities
								default:

									if(cChar<127)
										break;

									oSB.Append(cChar);
									continue;
							};
						}

                        // 14/05/08 we use numeric entities above ASCII level
                        // this is safer way - PHP XML parser was dieing on proper entities
                        if(!bChangeDangerousCharsOnly)
                        {
                            oSB.Append("&");
                            oSB.Append(sEntityReverseLookup[(int)cChar]);
                            oSB.Append(";");
                        }
                        else
                        {
                            oSB.Append("&#"+((int)cChar).ToString()+";");
                        }
						/*
						oSB.Append("&");
						oSB.Append(sEntityReverseLookup[(int)cChar]);
						oSB.Append(";");
						*/
						continue;
					}
				}

				oSB.Append(cChar);
			}		

			return oSB.ToString();
		}

		/// <summary>
		/// Inits mini-entities mode: only "nbsp" will be converted into space, all other entities 
		/// will be left as is
		/// </summary>
		internal void InitMiniEntities()
		{
			oEntities=new FastHash();

			oEntities.Add("nbsp",32);
			bMiniEntities=true;
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
				bDisposed=true;

				if(oEntities!=null)
				{
					oEntities.Dispose();
					oEntities=null;
				}
			}

			
		}
	}
}
