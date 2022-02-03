using System;

namespace Majestic12
{
	/// <summary>
	/// Internal class used to parse tag itself from the point it was found in HTML
	/// The main reason for this class is to split very long HTMLparser file into parts that are reasonably
	/// self-contained
	/// </summary>
	internal class TagParser : IDisposable
	{
		HTMLparser oP=null;
		HTMLchunk oChunk=null;
		DynaString sText=null;
		byte[] bHTML=null;
		int iDataLength=0;

		/// <summary>
		/// Minimum data size for heuristics engine to kick in
		/// </summary>
		const int MIN_DATA_SIZE_FOR_HEURISTICS=8;

		/// <summary>
		/// Max data length for heuristical checks
		/// </summary>
		int iMaxHeuDataLength=0;

		//byte[] bWhiteSpace=null;
		HTMLentities oE=null;
		HTMLheuristics oHE=null;

		/// <summary>
		/// Tag char types lookup table: allows one off lookup to determine if char used in tag is acceptable
		/// </summary>
		static byte[] bTagCharTypes=new byte[256];

		/// <summary>
		/// If true then heuristics engine will be used to match tags quicker
		/// </summary>
		internal bool bEnableHeuristics=true;

		enum TagCharType
		{
            /// <summary>
            /// Unclassified
            /// </summary>
			Unknown = 0,
			
            /// <summary>
            /// Whitespace
            /// </summary>
            WhiteSpace = 1,
			
            /// <summary>
            /// Lower case or digit 
            /// </summary>
            LowerCasedASCIIorDigit =2,

            /// <summary>
            /// semicolon - used for namespaces, ie: <namespace:a>
            /// </summary>
            NameSpaceColon  = 3,
			
			// anything above will be uppercased ASCII returned value is a lower cased char
		}

		static TagParser()
		{
			// whitespace
			bTagCharTypes[' ']=(byte)TagCharType.WhiteSpace;
			bTagCharTypes['\t']=(byte)TagCharType.WhiteSpace;
			bTagCharTypes[13]=(byte)TagCharType.WhiteSpace;
			bTagCharTypes[10]=(byte)TagCharType.WhiteSpace;

            bTagCharTypes[(byte)':']=(byte)TagCharType.NameSpaceColon;

			for(int i=33; i<127; i++)
			{
				if(char.IsDigit((char)i) || (i>=(65+32) && i<=(90+32)))
				{
					bTagCharTypes[i]=(byte)TagCharType.LowerCasedASCIIorDigit;
					continue;
				}

				// UPPER CASED ASCII
				if(i>=65 && i<=90)
				{
					bTagCharTypes[i]=(byte) (i+32);
					continue;
				}
			}
		}

		internal TagParser()
		{
		}

		/// <summary>
		/// Inits tag parser
		/// </summary>
		/// <param name="p_oChunk"></param>
		/// <param name="p_sText"></param>
		internal void Init(HTMLparser p_oP,HTMLchunk p_oChunk,DynaString p_sText,byte[] p_bHTML,int p_iDataLength,HTMLentities p_oE,HTMLheuristics p_oHE)
		{
			oP=p_oP;
			oChunk=p_oChunk;
			sText=p_sText;
			bHTML=p_bHTML;
			iDataLength=p_iDataLength;

			// we don't want to be too close to end of data when dealing with heuristics
			iMaxHeuDataLength=iDataLength-MIN_DATA_SIZE_FOR_HEURISTICS;

			oE=p_oE;
			oHE=p_oHE;
		}

		/// <summary>
		/// Cleans up tag parser
		/// </summary>
		internal void CleanUp()
		{
			bHTML=null;
			iDataLength=0;
		}

		/// <summary>
		/// Internal: parses tag that started from current position
		/// </summary>
		/// <returns>HTMLchunk with tag information</returns>
		internal HTMLchunk ParseTag(ref int iCurPos)
		{
			/*
			 *  WARNING: this code was optimised for performance rather than for readability, 
			 *  so be extremely careful at changing it -- your changes could easily result in wrongly parsed HTML
			 * 
			 *  This routine takes about 60% of CPU time, in theory its the best place to gain extra speed,
			 *  but I've spent plenty of time doing it, so it won't be easy... and if it is easy then please post
			 *  your changes for everyone to enjoy!
			 * 
			 * 
			 * */

			//bool bWhiteSpaceHere=false;
			
			//bool bParamValue=false;
			byte cChar=0;
			byte cPeek=0;

			// if true it means we have parsed complete tag
			//bool bGotTag=false;

			//int iEqualIdx=0;

			// we reach this function immediately after tag's byte (<) was
			// detected, so we need to save it in order to keep correct HTML copy
			// oChunk.Append((byte)'<'); // (byte)'<'
			
			/*
			oChunk.bBuffer[0]=60;
			oChunk.iBufPos=1;
			oChunk.iHTMLen=1;
			*/

			// initialise peeked char - this will point to the next after < character
			if(iCurPos<iDataLength)
			{
				cPeek=bHTML[iCurPos];

				// in case of comments ! must follow immediately after <
				if(cPeek==(byte)'!')
				{
					if(iCurPos+2<iDataLength && 
						bHTML[iCurPos+1]==(byte)'-' && bHTML[iCurPos+2]==(byte)'-')
					{
						// we detected start of comments here, instead of parsing the rest here we will
						// call special function tuned to do the job much more effectively
						oChunk.sTag="!--";
						oChunk.oType=HTMLchunkType.Comment;
						oChunk.bComments=true;
						// oChunk.Append((byte)'!');
						// oChunk.Append((byte)'-');
						// oChunk.Append((byte)'-');
						iCurPos+=3;
						bool bFullTag;
						oChunk=ParseComments(ref iCurPos,out bFullTag);

						oChunk.iChunkLength=iCurPos-oChunk.iChunkOffset;

						if(oP.bAutoKeepComments || oP.bKeepRawHTML)
						{
							if(!oP.bAutoExtractBetweenTagsOnly)
								oChunk.oHTML=GetString(oChunk.iChunkOffset,oChunk.iChunkLength);
							else
							{
								oChunk.oHTML=GetString(oChunk.iChunkOffset+4,oChunk.iChunkLength-(bFullTag ? 7 : 4));
							}

						}

						return oChunk;
					}

                    // ok we might have here CDATA element of XML:
                    // ref: http://www.w3schools.com/xml/xml_cdata.asp
                    if(iCurPos+7<iDataLength && 
						bHTML[iCurPos+1]==(byte)'[' && 
                        bHTML[iCurPos+2]==(byte)'C' &&
                        bHTML[iCurPos+3]==(byte)'D' &&
                        bHTML[iCurPos+4]==(byte)'A' &&
                        bHTML[iCurPos+5]==(byte)'T' &&
                        bHTML[iCurPos+6]==(byte)'A' &&                        
                        bHTML[iCurPos+7]==(byte)'[' 
                        )
                    {
                        // we detected start of comments here, instead of parsing the rest here we will
                        // call special function tuned to do the job much more effectively
                        oChunk.sTag="![CDATA[";
                        oChunk.oType=HTMLchunkType.Comment;
                        oChunk.bComments=true;
                        // oChunk.Append((byte)'!');
                        // oChunk.Append((byte)'-');
                        // oChunk.Append((byte)'-');
                        iCurPos+=8;
                        bool bFullTag;
                        oChunk=ParseCDATA(ref iCurPos,out bFullTag);

                        oChunk.iChunkLength=iCurPos-oChunk.iChunkOffset;

                        if(oP.bAutoKeepComments || oP.bKeepRawHTML)
                        {
                            if(!oP.bAutoExtractBetweenTagsOnly)
                                oChunk.oHTML=GetString(oChunk.iChunkOffset,oChunk.iChunkLength);
                            else
                            {
                                oChunk.oHTML=GetString(oChunk.iChunkOffset+4+5,
                                    oChunk.iChunkLength-(bFullTag ? 7+5 : 4+5));
                            }

                        }

                        return oChunk;
                    }

				}

			}
			else
			{
				// empty tag but its not closed, so we will call it open...
				oChunk.oType=HTMLchunkType.OpenTag;
				// end of data... before it started
				return oChunk;
			}

			// tag ID, non-zero if matched by heuristics engine
			int iTagID=0;

			// STAGE 0: lets try some heuristics to see if we can quickly identify most common tags
			// that should be present most of the time, this should save a lot of looping and string creation
			if(bEnableHeuristics && iCurPos<iMaxHeuDataLength)
			{
				// check if we have got closure of the tag
				if(cPeek==(byte)'/')
				{
					oChunk.bClosure=true;
					oChunk.bEndClosure=false;
					oChunk.oType=HTMLchunkType.CloseTag;
					iCurPos++;
					cPeek=bHTML[iCurPos];
				}

				cChar=bHTML[iCurPos+1];

				// probability of having a match is very high (or so we expect)
				iTagID=oHE.MatchTag(cPeek,cChar);

				if(iTagID!=0)
				{
					if(iTagID<0)
					{
						iTagID*=-1;
						// single character tag
						oChunk.sTag=oHE.GetString(iTagID);

						// see if we got fully closed tag
						if(cChar==(byte)'>')
						{
							iCurPos+=2;
							goto ReturnChunk;
						}

						cPeek=cChar;
						iCurPos++;

						// everything else means we need to continue scanning as we may have params and stuff
						goto AttributeParsing;
					}
					else
					{
						// ok, we have here 2 or more character string that we need to check further
						// often when we have full 2 char match the next char will be >, if that's the case
						// then we definately matched our tag
						byte cNextChar=bHTML[iCurPos+2];

						if(cNextChar==(byte)'>')
						{
							//oChunk.sTag=oHE.GetString(iTagID);
							oChunk.sTag=oHE.GetTwoCharString(cPeek,cChar);
							iCurPos+=3;

							goto ReturnChunk;
						}

						// ok, check next char for space, if that's the case we still got our tag
						// but need to skip to attribute parsing
						if(cNextChar==(byte)' ')
						{
							//oChunk.sTag=oHE.GetString(iTagID);
							oChunk.sTag=oHE.GetTwoCharString(cPeek,cChar);
							iCurPos+=2;

							cPeek=cNextChar;


							goto AttributeParsing;
						}

						// ok, we are not very lucky, but it is still worth fighting for
						// now we need to check fully long string against what we have matched, maybe
						// we got exact match and we can avoid full parsing of the tag
						byte[] bTag=oHE.GetStringData(iTagID);

						if(iCurPos+bTag.Length+5>=iDataLength)
							goto TagParsing;

						// in a loop (and this is not an ideal solution, but still)
						for(int i=2; i<bTag.Length; i++)
						{
							// if a single char is not matched, then we 
							if(bTag[i]!=bHTML[iCurPos+i])
							{
								goto TagParsing;
							}
						}

						// ok we matched full long word, but we need to be sure that char
						// after the word is ' ' or '>' as otherwise we may have matched prefix of even longer
						// word
						cNextChar=bHTML[iCurPos+bTag.Length];

						if(cNextChar==(byte)'>')
						{
							oChunk.sTag=oHE.GetString(iTagID);
							iCurPos+=bTag.Length+1;

							goto ReturnChunk;
						}

						if(cNextChar==(byte)' ')
						{
							cPeek=cNextChar;
							oChunk.sTag=oHE.GetString(iTagID);
							iCurPos+=bTag.Length;

							goto AttributeParsing;							
						}

						// no luck: we need to parse tag fully as our heuristical matching failed miserably :'o(
					}

				}
			}

			TagParsing:

			sText.Clear();

			byte bCharType=0;

			// STAGE 1: parse tag (anything until > or /> or whitespace leading to start of attribute)
			while(cPeek!=0)
			{
				bCharType=bTagCharTypes[cPeek];

				//if(cPeek<=32 && bWhiteSpace[cPeek]==1)
				if(bCharType==(byte)TagCharType.WhiteSpace)
				{
					iCurPos++;

					// speculative loop unroll -- we have a very good chance of seeing non-space char next
					// so instead of setting up loop we will just read it directly, this should save ticks
					// on having to prepare while() loop
					if(iCurPos<iDataLength)
						cChar=bHTML[iCurPos++];
					else
						cChar=0;

					bCharType=bTagCharTypes[cChar];

					//if(cChar==' ' || cChar=='\t' || cChar==13 || cChar==10)
					//if(cChar<=32 && bWhiteSpace[cChar]==1)
					if(bCharType==(byte)TagCharType.WhiteSpace)
					{

						while(iCurPos<iDataLength)
						{
							cChar=bHTML[iCurPos++];

							bCharType=bTagCharTypes[cChar];
							if(bCharType==(byte)TagCharType.WhiteSpace)
							//if(cChar!=' ' && cChar!='\t' && cChar!=13 && cChar!=10)
							{
								//cPeek=bHTML[iCurPos];
								continue;
							}

							break;
						}

						if(iCurPos>=iDataLength)
							cChar=0;
					}

					//bWhiteSpaceHere=true;

					// now, if we have already got tag it means that we are most likely
					// going to need to parse tag attributes
					if(sText.iBufPos>0)
					{
						oChunk.sTag=sText.SetToStringASCII();

						// oChunk.Append((byte)' ');

						iCurPos--;

						if(iCurPos<iDataLength)
							cPeek=bHTML[iCurPos];
						else
							cPeek=0;

						break;
					}

				}
				else
				{
					// reuse Peeked char from previous run
					//cChar=cPeek; iCurPos++;
					if(iCurPos<iDataLength)
						cChar=bHTML[iCurPos++];
					else
						cChar=0;
				}

				if(iCurPos<iDataLength)
					cPeek=bHTML[iCurPos];
				else
					cPeek=0;

				// most likely we should have lower-cased ASCII char
				if(bCharType==(byte)TagCharType.LowerCasedASCIIorDigit)
				{
					sText.bBuffer[sText.iBufPos++]=cChar;
					// oChunk.Append(cChar);
					continue;
				}
				
				// tag end - we did not have any params
				if(cChar==(byte)'>')
				{
					if(sText.iBufPos>0)
						oChunk.sTag=sText.SetToStringASCII();

					if(!oChunk.bClosure)
						oChunk.oType=HTMLchunkType.OpenTag;

					return oChunk;
				}

				// closure of tag sign
				if(cChar==(byte)'/')
				{
					oChunk.bClosure=true;
					oChunk.bEndClosure=(sText.iBufPos>0);
					oChunk.oType=HTMLchunkType.CloseTag;
					continue;
				}

                // 03/08/08 XML support: ?xml tags - grrr
                if(cChar==(byte)'?')
                {
                    sText.bBuffer[sText.iBufPos++]=cChar;
                    continue;
                }

				// nope, we have got upper cased ASCII char	- this seems to be LESS likely than > and /
				//if(cChar>=65 && cChar<=90)
				if(bCharType>32)
				{
					// bCharType in this case contains already lower-cased char
					sText.bBuffer[sText.iBufPos++]=bCharType;
					// oChunk.Append(bCharType);
					continue;
				}

                // we might have namespace : sign here - all text before would have to be
                // saved as namespace and we will need to continue parsing actual tag
                if(bCharType==(byte)TagCharType.NameSpaceColon)
                {
                    // ok here we got a choice - we can just continue and treat the whole
                    // thing as a single tag with namespace stuff prefixed, OR
                    // we can separate first part into namespace and keep tag as normal
                    sText.bBuffer[sText.iBufPos++]=(byte)':';
                    continue;
                }

				// ok, we have got some other char - we break out to deal with it in attributes part
				break;

			}

			if(cPeek==0)
			{
				return oChunk;
			}

			// if true then equal sign was found 
			//bool bEqualsSign=false;

			// STAGE 2: parse attributes (if any available)
			// attribute name can be standalone or with value after =
			// attribute itself can't have entities or anything like this - we expect it to be in ASCII characters

		AttributeParsing:

			string sAttrName;

			if(iTagID!=0)
			{
			

				// first, skip whitespace:
				if(cPeek<=32 && bTagCharTypes[cPeek]==(byte)TagCharType.WhiteSpace)
				{
					// most likely next char is not-whitespace
					iCurPos++;

					if(iCurPos>=iDataLength)
						goto ReturnChunk;

					cPeek=bHTML[iCurPos];

					if(cPeek<=32 && bTagCharTypes[cPeek]==(byte)TagCharType.WhiteSpace)
					{
						// ok long loop here then
						while(iCurPos<iDataLength)
						{
							cPeek=bHTML[iCurPos++];

							if(cPeek<=32 && bTagCharTypes[cPeek]==(byte)TagCharType.WhiteSpace)
								continue;

							break;
						}
						
						if(cPeek==(byte)'>')
							goto ReturnChunk;

						iCurPos--;

						if(iCurPos>=iDataLength)
							goto ReturnChunk;
					}

					if(iCurPos>=iDataLength)
						goto ReturnChunk;

				}

				// ok we have got matched tag, it is possible that we might be able to quickly match
				// attribute name known to be used for that tag:
				int iAttrID=oHE.MatchAttr(cPeek,iTagID);

				if(iAttrID>0)
				{
					byte[] bAttr=oHE.GetAttrData(iAttrID);

					if(iCurPos+bAttr.Length+2>=iDataLength)
						goto ActualAttributeParsing;

					// in a loop (and this is not an ideal solution, but still)
					for(int i=1; i<bAttr.Length; i++)
					{
						// if a single char is not matched, then we 
						if(bAttr[i]!=bHTML[iCurPos+i])
						{
							goto ActualAttributeParsing;
						}
					}

					byte cNextChar=bHTML[iCurPos+bAttr.Length];

					// ok, we expect next symbol to be =
					if(cNextChar==(byte)'=')
					{
						sAttrName=oHE.GetAttr(iAttrID);
						iCurPos+=bAttr.Length+1;
						cPeek=bHTML[iCurPos];

						goto AttributeValueParsing;
					}


				}

			}

			ActualAttributeParsing:

			sText.Clear();

			// doing exactly the same thing as in tag parsing
			while(cPeek!=0)
			{
				bCharType=bTagCharTypes[cPeek];

				//if(cPeek<=32 && bWhiteSpace[cPeek]==1)
				if(bCharType==(byte)TagCharType.WhiteSpace)
				{
					iCurPos++;

					// speculative loop unroll -- we have a very good chance of seeing non-space char next
					// so instead of setting up loop we will just read it directly, this should save ticks
					// on having to prepare while() loop
					if(iCurPos<iDataLength)
						cChar=bHTML[iCurPos++];
					else
					{
						cPeek=0;
						break;
					}

					bCharType=bTagCharTypes[cChar];

					//if(cChar==' ' || cChar=='\t' || cChar==13 || cChar==10)
					//if(cChar<=32 && bWhiteSpace[cChar]==1)
					if(bCharType==(byte)TagCharType.WhiteSpace)
					{

						while(iCurPos<iDataLength)
						{
							cChar=bHTML[iCurPos++];

							bCharType=bTagCharTypes[cChar];
							if(bCharType==(byte)TagCharType.WhiteSpace)
							//if(cChar!=' ' && cChar!='\t' && cChar!=13 && cChar!=10)
							{
								//cPeek=bHTML[iCurPos];
								continue;
							}

							//if(cChar==(byte)'>')
							//	goto ReturnChunk;

							//iCurPos--;
							break;
						}

						if(iCurPos>=iDataLength)
						{
							cChar=0;
							cPeek=0;
							break;
						}
					}

					//bWhiteSpaceHere=true;

					// now, if we have already got attribute name it means that we need to go to parse value (which may not be present)
					if(sText.iBufPos>0)
					{
						// oChunk.Append((byte)' ');

						iCurPos--;

						if(iCurPos<iDataLength)
							cPeek=bHTML[iCurPos];
						else
							cPeek=0;

						// ok, we have got attribute name and now we have got next char there

						// most likely we have got = here  and then value
						if(cPeek==(byte)'=')
						{
							//bEqualsSign=true;

							// move forward one char
							iCurPos++;

							if(iCurPos<iDataLength)
								cPeek=bHTML[iCurPos];
							else
								cPeek=0;

							break;
						}

						// or we can have end of tag itself, doh!
						if(cPeek==(byte)'>')
						{
							// move forward one char
							iCurPos++;

							if(sText.iBufPos>0)
								oChunk.AddParam(sText.SetToStringASCII(),"",(byte)' ');

							if(!oChunk.bClosure)
								oChunk.oType=HTMLchunkType.OpenTag;

							return oChunk;
						}

						// closure
						if(cPeek==(byte)'/')
						{
							oChunk.bClosure=true;
							oChunk.bEndClosure=true;
							oChunk.oType=HTMLchunkType.CloseTag;
							continue;
						}

						// ok, we have got new char starting after current attribute name is fully parsed
						// this means the attribute name is on its own and the char we found is start
						// of a new attribute
						oChunk.AddParam(sText.SetToStringASCII(),"",(byte)' ');
						sText.Clear();
						goto AttributeParsing;
					}

				}
				else
				{
					// reuse Peeked char from previous run
					//cChar=cPeek; iCurPos++;
					if(iCurPos<iDataLength)
						cChar=bHTML[iCurPos++];
					else
						cChar=0;
				}

				if(iCurPos<iDataLength)
					cPeek=bHTML[iCurPos];
				else
					cPeek=0;

				// most likely we should have lower-cased ASCII char here
				if(bCharType==(byte)TagCharType.LowerCasedASCIIorDigit)
				{
					sText.bBuffer[sText.iBufPos++]=cChar;
					// oChunk.Append(cChar);
					continue;
				}

				// = with attribute value to follow
				if(cChar==(byte)'=')
				{
					//bEqualsSign=true;
					break;
				}

				// nope, we have got upper cased ASCII char	- this seems to be LESS likely than > and /
				//if(cChar>=65 && cChar<=90)
				if(bCharType>32)
				{
					// bCharType in this case contains already lower-cased char
					sText.bBuffer[sText.iBufPos++]=bCharType;
					// oChunk.Append(bCharType);
					continue;
				}

				// tag end - we did not have any params
				if(cChar==(byte)'>')
				{
					if(sText.iBufPos>0)
						oChunk.AddParam(sText.SetToStringASCII(),"",(byte)' ');

					if(!oChunk.bClosure)
						oChunk.oType=HTMLchunkType.OpenTag;

					return oChunk;
				}

				// closure of tag sign
				if(cChar==(byte)'/')
				{
					oChunk.bClosure=true;
					oChunk.bEndClosure=true;
					oChunk.oType=HTMLchunkType.CloseTag;
					continue;
				}

				// some other char
				sText.bBuffer[sText.iBufPos++]=cChar;
				// oChunk.Append(cChar);
			}

			if(cPeek==0)
			{
				if(sText.iBufPos>0)
					oChunk.AddParam(sText.SetToStringASCII(),"",(byte)' ');

				if(!oChunk.bClosure)
					oChunk.oType=HTMLchunkType.OpenTag;
	
				return oChunk;
			}

			sAttrName=sText.SetToStringASCII();

			AttributeValueParsing:

			/// ***********************************************************************
			/// STAGE 3: parse attribute value
			/// ***********************************************************************

			// the value could be just string, or in quotes (single or double)
			// or we can have next attribute name start, in which case we will jump back to attribute parsing

			// for tracking quotes purposes
			byte cQuotes=cPeek;

			int iValueStartOffset;

			// skip whitespace if any
			if(cPeek<=32 && bTagCharTypes[cPeek]==(byte)TagCharType.WhiteSpace)
			{
				iCurPos++;

				// speculative loop unroll -- we have a very good chance of seeing non-space char next
				// so instead of setting up loop we will just read it directly, this should save ticks
				// on having to prepare while() loop
				if(iCurPos<iDataLength)
					cPeek=bHTML[iCurPos];
				else
				{
					iValueStartOffset=iCurPos-1;
					goto AttributeValueEnd;
				}

				//if(cChar==' ' || cChar=='\t' || cChar==13 || cChar==10)
				//if(cChar<=32 && bWhiteSpace[cChar]==1)
				if(cPeek<=32 && bTagCharTypes[cPeek]==(byte)TagCharType.WhiteSpace)
				{

					while(iCurPos<iDataLength)
					{
						cPeek=bHTML[iCurPos++];

						if(cPeek<=32 && bTagCharTypes[cPeek]==(byte)TagCharType.WhiteSpace)
						//if(cChar!=' ' && cChar!='\t' && cChar!=13 && cChar!=10)
						{
							//cPeek=bHTML[iCurPos];
							continue;
						}

						iCurPos--;
						break;
					}

					if(iCurPos>=iDataLength)
					{
						iValueStartOffset=iCurPos-1;
						goto AttributeValueEnd;
					}
				}

				cQuotes=cPeek;
			}
			
				

			// because we deal with VALUE of the attribute it means we can't lower-case it, 
			// or skip whitespace (if in quotes), which in practice means that we don't need to copy
			// it to temporary string buffer, we can just remember starting offset and then create string from
			// data in bHTML

			// ok, first char can be one of the quote chars or something else
			if(cPeek!='\"' && cPeek!='\'')
			{
				iValueStartOffset=iCurPos;

				cQuotes=(byte)' ';
				// any other char here means we have value up until next whitespace or end of tag
				// this gives us good opportunity to scan fairly quickly without otherwise redundant
				// checks - this should happen fairly rarely, however loop dealing with data between quotes
				// will happen often enough and its best to eliminate as much stuff from it as possible
				//sText.bBuffer[sText.iBufPos++]=cPeek;

				// move to next char
				if(iCurPos<iDataLength)
					cPeek=bHTML[iCurPos++];
				else
				{
					goto AttributeValueEnd;
				}

				while(cPeek!=0)
				{
					// if whitespace then we got our value and need to go back to param
					if(cPeek<=32 && bTagCharTypes[cPeek]==(byte)TagCharType.WhiteSpace)
					{
						oChunk.AddParam(sAttrName,GetString(iValueStartOffset,iCurPos-iValueStartOffset-1),(byte)' ');
						iCurPos--;
						goto AttributeParsing;
					}

					// end of tag?
					if(cPeek==(byte)'>')
					{
						//iCurPos--;
						break;
					}

					if(iCurPos<iDataLength)
						cPeek=bHTML[iCurPos++];
					else
					{
						iCurPos=iDataLength+1;
						goto AttributeValueEnd;
					}
				}
				
				// ok we are done, add outstanding attribute
				oChunk.AddParam(sAttrName,GetString(iValueStartOffset,iCurPos-iValueStartOffset-1),(byte)' ');

				goto ReturnChunk;
			}

			// move one step forward
			iCurPos++;

			iValueStartOffset=iCurPos;

			if(iCurPos<iDataLength)
				cPeek=bHTML[iCurPos++];
			else
			{
				
				goto AttributeValueEnd;
			}

			// attribute value parsing from between two quotes
			while(cPeek!=0)
			{
				// check whether we have got possible entity (can be anything starting with &)
				if(cPeek==38)
				{
					int iPrevPos=iCurPos;

					char cEntityChar=oE.CheckForEntity(bHTML,ref iCurPos,iDataLength);

					// restore current symbol
					if(cEntityChar==0)
					{
						if(iCurPos<iDataLength)
							cPeek=bHTML[iCurPos++];
						else
							break;

						//sText.bBuffer[sText.iBufPos++]=38; //(byte)'&';;
						continue;
					}
					else
					{
						// okay we have got an entity, our hope of not having to copy stuff into variable
						// is over, we have to continue in a slower fashion :(
						// but thankfully this should happen very rarely, so, annoying to code, but
						// most codepaths will run very fast!
						int iPreEntLen=iPrevPos-iValueStartOffset-1;

						// 14/05/08 need to clear text - it contains attribute name text
						sText.Clear();						

						// copy previous data
						if(iPreEntLen>0)
						{
							Array.Copy(bHTML,iValueStartOffset,sText.bBuffer,0,iPreEntLen);
							sText.iBufPos=iPreEntLen;
						}
		
						// we have to skip now to next byte, since 
						// some converted chars might well be control chars like >
						oChunk.bEntities=true;

						if(cChar==(byte)'<')
							oChunk.bLtEntity=true;

						// unless is space we will ignore it
						// note that this won't work if &nbsp; is defined as it should
						// byte int value of 160, rather than 32.
						//if(cChar!=' ')
						sText.Append(cEntityChar);

						if(iCurPos<iDataLength)
							cPeek=bHTML[iCurPos++];
						else
						{
							
							goto AttributeValueEnd;
						}

						// okay, we continue here using in effect new inside loop as we might have more entities here
						// attribute value parsing from between two quotes
						while(cPeek!=0)
						{
							// check whether we have got possible entity (can be anything starting with &)
							if(cPeek==38)
							{
								char cNewEntityChar=oE.CheckForEntity(bHTML,ref iCurPos,iDataLength);

								// restore current symbol
								if(cNewEntityChar!=0)
								{
									if(cNewEntityChar==(byte)'<')
										oChunk.bLtEntity=true;

									sText.Append(cNewEntityChar);

									if(iCurPos<iDataLength)
										cPeek=bHTML[iCurPos++];
									else
										goto AttributeValueEnd;

									continue;
								}
							}

							// check if is end of quotes
							if(cPeek==cQuotes)
							{
								// ok we finished scanning it: add param with value and then go back to param name parsing
								oChunk.AddParam(sAttrName,sText.SetToString(),cQuotes);

								if(iCurPos<iDataLength)
									cPeek=bHTML[iCurPos];
								else
									break;

								goto AttributeParsing;
							}

							sText.bBuffer[sText.iBufPos++]=cPeek;
							//sText.Append(cPeek);

							if(iCurPos<iDataLength)
								cPeek=bHTML[iCurPos++];
							else
								break;
						}

						oChunk.AddParam(sAttrName,sText.SetToString(),cQuotes);
						goto ReturnChunk;
					}
				}

				// check if is end of quotes
				if(cPeek==cQuotes)
				{
					// ok we finished scanning it: add param with value and then go back to param name parsing
					//sText.Clear();
					
					oChunk.AddParam(sAttrName,GetString(iValueStartOffset,iCurPos-iValueStartOffset-1),cQuotes);


					if(iCurPos<iDataLength)
						cPeek=bHTML[iCurPos];
					else
					{
						//iCurPos++;
						break;
					}
				 
					goto AttributeParsing;
				}

				if(iCurPos<iDataLength)
					cPeek=bHTML[iCurPos++];
				else
				{
					//iCurPos++;
					break;
				}
			}

			AttributeValueEnd:

			

			// ok we are done, add outstanding attribute
			int iLen=iCurPos-iValueStartOffset-1;
			if(iLen>0)
				oChunk.AddParam(sAttrName,GetString(iValueStartOffset,iLen),cQuotes);
			else
				oChunk.AddParam(sAttrName,"",cQuotes);

			ReturnChunk:

			if(oChunk.bClosure)
			{
				oChunk.oType=HTMLchunkType.CloseTag;
			}
			else
				oChunk.oType=HTMLchunkType.OpenTag;

			return oChunk;
		}

		/// <summary>
		/// Finishes parsing of comments tag
		/// </summary>
		/// <returns>HTMLchunk object</returns>
		internal HTMLchunk ParseComments(ref int iCurPos,out bool bFullTag)
		{
			//byte cChar=0;

			while(iCurPos<iDataLength)
			{
				if(bHTML[iCurPos++]==62)
				{
					if(iCurPos>=3)
					{
						if(bHTML[iCurPos-2]==(byte)'-' && bHTML[iCurPos-3]==(byte)'-')
						{
							bFullTag=true;
							return oChunk;
						}
					}
				}

			}

			bFullTag=false;
			return oChunk;
		}

        /// <summary>
        /// Finishes parsing of CDATA component
        /// </summary>
        /// <param name="iCurPos"></param>
        /// <param name="bFullTag"></param>
        /// <returns></returns>
        internal HTMLchunk ParseCDATA(ref int iCurPos,out bool bFullTag)
        {
            // 19/07/08 yes this is copy/paste - moving to same function would make source code 
            // look nice but such function won't be inlined by compiler as it would be too complex for it
            // so this is manual inlining, well that and lack of time and desire to do it!
            while(iCurPos<iDataLength)
            {
                if(bHTML[iCurPos++]==62)
                {
                    if(iCurPos>=3)
                    {
                        if(bHTML[iCurPos-2]==(byte)']' && bHTML[iCurPos-3]==(byte)']')
                        {
                            bFullTag=true;
                            return oChunk;
                        }
                    }
                }

            }

            bFullTag=false;
            return oChunk;
        }

		/// <summary>
		/// /script sequence indicating end of script tag
		/// </summary>
		static byte[] bClosedScriptTag={ (byte)'/', (byte)'s', (byte)'c', (byte)'r', (byte)'i', (byte)'p', (byte)'t', (byte)'>'}; 

		/// <summary>
		/// Finishes parsing of data after scripts tag - makes extra checks to avoid being broken
		/// with >'s used to denote comparison
		/// </summary>
		/// <returns>HTMLchunk object</returns>
		internal HTMLchunk ParseScript(ref int iCurPos)
		{
			byte cChar=0;

			int iStart=iCurPos;
			int iLastPos=-1;

			while(iCurPos<iDataLength)
			{
				cChar=bHTML[iCurPos++];

				if(cChar==(byte)'<')
				{
					iLastPos=iCurPos;

					int iPos=0;

					// check here if its an HTML comment

					if(iCurPos<iDataLength)
					{
						if(bHTML[iCurPos]==(byte)'!')
						{
							if((iCurPos+3)<iDataLength)
							{
								if(bHTML[iCurPos+1]==(byte)'-' && bHTML[iCurPos+2]==(byte)'-')
								{
									// FIXIT: perhaps it is more correct here to return straight away?
									bool bFullTag;
									ParseComments(ref iCurPos,out bFullTag);
									continue;
									//Console.WriteLine("");
								}
							}
							
						}
					}

					while(iCurPos<iDataLength)
					{
						cChar=bHTML[iCurPos++];

						//if(cChar==' ' || cChar=='\t' || cChar==13 || cChar==10)
						//if(cChar<=32 && bWhiteSpace[cChar]==1)
						if(cChar<=32 && bTagCharTypes[cChar]==(byte)TagCharType.WhiteSpace)
							continue;

						if(cChar>=65 && cChar<=90)
							cChar+=32;

						// if next char it out of expected sequence then we will ignore it
						// and restart scanning from previous position
						if(cChar!=bClosedScriptTag[iPos])
						{
							//iCurPos=iLastPos;
							iLastPos=iCurPos;
							break;
						}

						// if we got whole length then we found end of script tag and can return back
						if(++iPos==bClosedScriptTag.Length)
						{
							// don't ever tell me about usage of goto...
							goto ReturnChunk;
						}

					}
					
				}

				// oChunk.Append(cChar);
			}

			// ok we run out of space for scripts - must be broken HTML, we will take all we can
			iLastPos=iDataLength+1;

			ReturnChunk:

			oChunk.iChunkLength=iCurPos-oChunk.iChunkOffset;

			if(oP.bAutoKeepScripts || oP.bKeepRawHTML)
			{
				if(!oP.bAutoExtractBetweenTagsOnly)
					oChunk.oHTML=GetString(oChunk.iChunkOffset,oChunk.iChunkLength);
				else
				{
					if(iLastPos==-1)
						iLastPos=iCurPos+1;

					oChunk.oHTML=GetString(iStart,iLastPos-iStart-1);
				}
			}

			return oChunk;
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

				bHTML=null;
				oChunk=null;
				sText=null;
				oE=null;
				oP=null;
			}

		}

		string GetString(int iOffset,int iLen)
		{
			// check boundaries: sometimes they are exceeded
			if(iOffset>=iDataLength || iLen==0)
				return "";

			if(iOffset+iLen>iDataLength)
				iLen=bHTML.Length-iOffset;

			try
			{
				return oChunk.oEnc.GetString(bHTML,iOffset,iLen);
			}
			catch
			{
				return "";
			}

		}
	}
}