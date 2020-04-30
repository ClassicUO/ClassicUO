using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace csmidi
{
    public static class MidiLoader
    {

        public static void loadFromStream(Stream midiStream, List<MidiTrack> midiTracks, ref ushort timeDivision)
        {
            midiTracks.Clear();     // remove all elements currently loaded
            // check if the Midi data is valid
            Debug.WriteLine("Verifying MIDI data...");
            verifyMidi(midiStream);   // this will raise an exception if the Midi data is invalid
            Debug.WriteLine("Identify MIDI data type...");
            int midiType = getMidiType(midiStream);
            Debug.WriteLine("The MIDI data type is #{0}", midiType.ToString());

            switch (midiType)
            {
                case 0:
                    Debug.WriteLine("Converting and loading MIDI...");
                    loadAndConvertTypeZero(midiStream, midiTracks, ref timeDivision);
                    break;
                case 1:
                    Debug.WriteLine("Loading MIDI...");
                    loadDirectly(midiStream, midiTracks, ref timeDivision);
                    break;
                case 2:
                    throw new Exception("MIDI type 2 is not supported by this program!");
                default:
                    throw new Exception("Invalid MIDI type!");
            }
        }

        public static void loadFromFile(string filePath, List<MidiTrack> midiTracks, ref ushort timeDivision)
        {
            using (FileStream midiFileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                loadFromStream(midiFileStream, midiTracks, ref timeDivision);
            }

        }

        private static void loadDirectly(Stream midiStream, List<MidiTrack> midiTracks, ref ushort timeDivision)              // returns the MIDI loaded in the List of all individual tracks
        {
            BinaryReader midiBinaryStream = new BinaryReader(midiStream);
            midiStream.Position = 0xA;      // seek to the amount of tracks in MIDI data
            int numTracks = midiBinaryStream.ReadByte() << 8 | midiBinaryStream.ReadByte();
            timeDivision = (ushort)(midiBinaryStream.ReadByte() << 8 | midiBinaryStream.ReadByte());
            // finished reading the header data, now continue transscribing the tracks

            for (int currentTrack = 0; currentTrack < numTracks; currentTrack++)
            {
                MidiTrack cTrk = new MidiTrack();
                midiTracks.Add(cTrk);     // we have to create the object of the track first and we can add it later to out track list if the track was transscribed into it's objects
                long currentTick = 0;
                NormalType lastEventType = NormalType.NoteOFF;
                byte lastMidiChannel = 0;
                // check if the track doesn't begin like expected with an MTrk string
                byte[] textString = new byte[4];
                midiBinaryStream.Read(textString, 0, 4);
                if (Encoding.ASCII.GetString(textString, 0, 4) != "MTrk") 
                    throw new Exception("Track doesn't start with MTrk string!");
                byte[] intArray = new byte[4];
                midiBinaryStream.Read(intArray, 0, 4);    // read the track length
                // this value isn't even needed, so we don't do further processing with it; I left it in the code for some usage in the future; no specific plan???

                // now do the event loop and load all the events
                #region EventLoop
                while (true)
                {
                    // first thing that is done is getting the next delta length value and add the value to the current position to calculate the absolute position of the event
                    currentTick += readVariableLengthValue(midiBinaryStream);
                    
                    // now check what event type is used and disassemble it

                    byte eventTypeByte = midiBinaryStream.ReadByte();

                    // do a jumptable for each event type

                    if (eventTypeByte == 0xFF)      // if META Event
                    {
                        byte metaType = (byte)midiStream.ReadByte();
                        long metaLength = readVariableLengthValue(midiBinaryStream);
                        byte[] metaData = new byte[metaLength];
                        midiBinaryStream.Read(metaData, 0, (int)metaLength);

                        if (metaType == 0x2F) 
                            break;        // if end of track is reached, break out of the loop, End of Track Events aren't written into the objects

                        cTrk.midiEvents.Add(new MetaMidiEvent(currentTick, metaType, metaData));
                    }
                    else if (eventTypeByte == 0xF0 || eventTypeByte == 0xF7)        // if SysEx Event
                    {
                        long sysexLength = readVariableLengthValue(midiBinaryStream);
                        byte[] sysexData = new byte[sysexLength];
                        midiBinaryStream.Read(sysexData, 0, (int)sysexLength);
                        cTrk.midiEvents.Add(new SysExMidiEvent(currentTick, eventTypeByte, sysexData));
                    }
                    else if (eventTypeByte >> 4 == 0x8)     // if Note OFF command
                    {
                        byte par1 = midiBinaryStream.ReadByte();
                        byte par2 = midiBinaryStream.ReadByte();
                        cTrk.midiEvents.Add(new MessageMidiEvent(currentTick, (byte)(eventTypeByte & 0xF), NormalType.NoteOFF, par1, par2));
                        // save the last event type and channel
                        lastEventType = NormalType.NoteOFF;
                        lastMidiChannel = (byte)(eventTypeByte & 0xF);
                    }
                    else if (eventTypeByte >> 4 == 0x9)     // if Note ON command
                    {
                        byte par1 = midiBinaryStream.ReadByte();
                        byte par2 = midiBinaryStream.ReadByte();
                        cTrk.midiEvents.Add(new MessageMidiEvent(currentTick, (byte)(eventTypeByte & 0xF), NormalType.NoteON, par1, par2));
                        // save the last event type and channel
                        lastEventType = NormalType.NoteON;
                        lastMidiChannel = (byte)(eventTypeByte & 0xF);
                    }
                    else if (eventTypeByte >> 4 == 0xA)     // if Aftertouch command
                    {
                        byte par1 = midiBinaryStream.ReadByte();
                        byte par2 = midiBinaryStream.ReadByte();
                        cTrk.midiEvents.Add(new MessageMidiEvent(currentTick, (byte)(eventTypeByte & 0xF), NormalType.NoteAftertouch, par1, par2));
                        // save the last event type and channel
                        lastEventType = NormalType.NoteAftertouch;
                        lastMidiChannel = (byte)(eventTypeByte & 0xF);
                    }
                    else if (eventTypeByte >> 4 == 0xB)     // if MIDI controller command
                    {
                        byte par1 = midiBinaryStream.ReadByte();
                        byte par2 = midiBinaryStream.ReadByte();
                        cTrk.midiEvents.Add(new MessageMidiEvent(currentTick, (byte)(eventTypeByte & 0xF), NormalType.Controller, par1, par2));
                        // save the last event type and channel
                        lastEventType = NormalType.Controller;
                        lastMidiChannel = (byte)(eventTypeByte & 0xF);
                    }
                    else if (eventTypeByte >> 4 == 0xC)     // if Preset command
                    {
                        byte par1 = midiBinaryStream.ReadByte();
                        byte par2 = 0x0;    // unused
                        cTrk.midiEvents.Add(new MessageMidiEvent(currentTick, (byte)(eventTypeByte & 0xF), NormalType.Program, par1, par2));
                        // save the last event type and channel
                        lastEventType = NormalType.Program;
                        lastMidiChannel = (byte)(eventTypeByte & 0xF);
                    }
                    else if (eventTypeByte >> 4 == 0xD)     // if Channel Aftertouch command
                    {
                        byte par1 = midiBinaryStream.ReadByte();
                        byte par2 = 0x0;    // unused
                        cTrk.midiEvents.Add(new MessageMidiEvent(currentTick, (byte)(eventTypeByte & 0xF), NormalType.ChannelAftertouch, par1, par2));
                        // save the last event type and channel
                        lastEventType = NormalType.ChannelAftertouch;
                        lastMidiChannel = (byte)(eventTypeByte & 0xF);
                    }
                    else if (eventTypeByte >> 4 == 0xE)     // if Pitch Bend command
                    {
                        byte par1 = midiBinaryStream.ReadByte();
                        byte par2 = midiBinaryStream.ReadByte();
                        cTrk.midiEvents.Add(new MessageMidiEvent(currentTick, (byte)(eventTypeByte & 0xF), NormalType.PitchBend, par1, par2));
                        // save the last event type and channel
                        lastEventType = NormalType.PitchBend;
                        lastMidiChannel = (byte)(eventTypeByte & 0xF);
                    }
                    else if (eventTypeByte >> 4 < 0x8)
                    {
                        byte par1 = eventTypeByte;
                        byte par2;
                        switch (lastEventType)
                        {
                            case NormalType.NoteOFF:
                                par2 = midiBinaryStream.ReadByte();
                                cTrk.midiEvents.Add(new MessageMidiEvent(currentTick, lastMidiChannel, NormalType.NoteOFF, par1, par2));
                                break;
                            case NormalType.NoteON:
                                par2 = midiBinaryStream.ReadByte();
                                cTrk.midiEvents.Add(new MessageMidiEvent(currentTick, lastMidiChannel, NormalType.NoteON, par1, par2));
                                break;
                            case NormalType.NoteAftertouch:
                                par2 = midiBinaryStream.ReadByte();
                                cTrk.midiEvents.Add(new MessageMidiEvent(currentTick, lastMidiChannel, NormalType.NoteAftertouch, par1, par2));
                                break;
                            case NormalType.Controller:
                                par2 = midiBinaryStream.ReadByte();
                                cTrk.midiEvents.Add(new MessageMidiEvent(currentTick, lastMidiChannel, NormalType.Controller, par1, par2));
                                break;
                            case NormalType.Program:
                                cTrk.midiEvents.Add(new MessageMidiEvent(currentTick, lastMidiChannel, NormalType.Program, par1, 0x0));
                                break;
                            case NormalType.ChannelAftertouch:
                                cTrk.midiEvents.Add(new MessageMidiEvent(currentTick, lastMidiChannel, NormalType.ChannelAftertouch, par1, 0x0));
                                break;
                            case NormalType.PitchBend:
                                par2 = midiBinaryStream.ReadByte();
                                cTrk.midiEvents.Add(new MessageMidiEvent(currentTick, lastMidiChannel, NormalType.PitchBend, par1, par2));
                                break;
                        }
                    }
                    else
                    {
                        throw new Exception("Bad MIDI event at 0x" + midiBinaryStream.BaseStream.Position.ToString("X8") + ": 0x" + eventTypeByte.ToString("X2"));
                    }
                }   // end of the event transscribing loop
                #endregion
            }   // end of the track loop
        }   // end of function loadDirectly

        private static void loadAndConvertTypeZero(Stream midiStream, List<MidiTrack> midiTracks, ref ushort timeDivision)    // returns the MIDI loaded in the List of all individual MIDI channels split up into 16 tracks
        {
            BinaryReader midiBinaryStream = new BinaryReader(midiStream);
            midiStream.Position = 0xC;      // seek to the amount of tracks in the MIDI data
            timeDivision = (ushort)(midiBinaryStream.ReadByte() << 8 | midiBinaryStream.ReadByte());
            // finished reading the header data, now continue transscribing the single track to multiple ones, depending on the channel

            for (int i = 0; i < 16; i++) 
                midiTracks.Add(new MidiTrack());     // we have to create tracks for each MIDI channel (i.e. 16)

            long currentTick = 0;

            NormalType lastEventType = NormalType.NoteOFF;
            byte lastMidiChannel = 0;
            // check if the track doesn't begin like expected with an MTrk string
            byte[] textString = new byte[4];
            midiBinaryStream.Read(textString, 0, 4);
            if (Encoding.ASCII.GetString(textString, 0, 4) != "MTrk") 
                throw new Exception("Track doesn't start with MTrk string!");
            byte[] intArray = new byte[4];
            midiBinaryStream.Read(intArray, 0, 4);    // read the track length
            // this value isn't even needed, so we don't do further processing with it; I left it in the code for some usage in the future; no specific plan???

            // now do the event loop and load all the events and remap the channels
            #region EventLoop
            while (true)
            {
                // first thing that is done is getting the next delta length value and add the value to the current position to calculate the absolute position of the event
                currentTick += readVariableLengthValue(midiBinaryStream);

                // now check what event type is used and disassemble it
                byte eventTypeByte = midiBinaryStream.ReadByte();

                // do a jumptable for each event type
                if (eventTypeByte == 0xFF)      // if META Event
                {
                    byte metaType = (byte)midiStream.ReadByte();
                    long metaLength = readVariableLengthValue(midiBinaryStream);
                    byte[] metaData = new byte[metaLength];
                    midiBinaryStream.Read(metaData, 0, (int)metaLength);

                    if (metaType == 0x2F)
                        break;        // End of track events aren't loaded into the objects

                    midiTracks[0].midiEvents.Add(
                            new MetaMidiEvent(currentTick, metaType, metaData));
                }
                else if (eventTypeByte == 0xF0 || eventTypeByte == 0xF7)        // if SysEx Event
                {
                    long sysexLength = readVariableLengthValue(midiBinaryStream);
                    byte[] sysexData = new byte[sysexLength];
                    midiBinaryStream.Read(sysexData, 0, (int)sysexLength);
                    midiTracks[0].midiEvents.Add(
                            new SysExMidiEvent(currentTick, eventTypeByte, sysexData));
                }
                else if (eventTypeByte >> 4 == 0x8)     // if Note OFF command
                {
                    byte par1 = midiBinaryStream.ReadByte();
                    byte par2 = midiBinaryStream.ReadByte();
                    midiTracks[eventTypeByte & 0xF].midiEvents.Add(
                            new MessageMidiEvent(currentTick, (byte)(eventTypeByte & 0xF), NormalType.NoteOFF, par1, par2));
                    // now backup channel and Normal Type for truncated commands
                    lastEventType = NormalType.NoteOFF;
                    lastMidiChannel = (byte)(eventTypeByte & 0xF);
                }
                else if (eventTypeByte >> 4 == 0x9)     // if Note ON command
                {
                    byte par1 = midiBinaryStream.ReadByte();
                    byte par2 = midiBinaryStream.ReadByte();
                    midiTracks[eventTypeByte & 0xF].midiEvents.Add(
                            new MessageMidiEvent(currentTick, (byte)(eventTypeByte & 0xF), NormalType.NoteON, par1, par2));
                    // now backup channel and Normal Type for truncated commands
                    lastEventType = NormalType.NoteON;
                    lastMidiChannel = (byte)(eventTypeByte & 0xF);
                }
                else if (eventTypeByte >> 4 == 0xA)     // if Aftertouch command
                {
                    byte par1 = midiBinaryStream.ReadByte();
                    byte par2 = midiBinaryStream.ReadByte();
                    midiTracks[eventTypeByte & 0xF].midiEvents.Add(
                            new MessageMidiEvent(currentTick, (byte)(eventTypeByte & 0xF), NormalType.NoteAftertouch, par1, par2));
                    // now backup channel and Normal Type for truncated commands
                    lastEventType = NormalType.NoteAftertouch;
                    lastMidiChannel = (byte)(eventTypeByte & 0xF);
                }
                else if (eventTypeByte >> 4 == 0xB)     // if MIDI controller command
                {
                    byte par1 = midiBinaryStream.ReadByte();
                    byte par2 = midiBinaryStream.ReadByte();
                    midiTracks[eventTypeByte & 0xF].midiEvents.Add(
                            new MessageMidiEvent(currentTick, (byte)(eventTypeByte & 0xF), NormalType.Controller, par1, par2));
                    // now backup channel and Normal Type for truncated commands
                    lastEventType = NormalType.Controller;
                    lastMidiChannel = (byte)(eventTypeByte & 0xF);
                }
                else if (eventTypeByte >> 4 == 0xC)     // if Preset command
                {
                    byte par1 = midiBinaryStream.ReadByte();
                    byte par2 = 0x0;
                    midiTracks[eventTypeByte & 0xF].midiEvents.Add(
                            new MessageMidiEvent(currentTick, (byte)(eventTypeByte & 0xF), NormalType.Program, par1, par2));
                    // now backup channel and Normal Type for truncated commands
                    lastEventType = NormalType.Program;
                    lastMidiChannel = (byte)(eventTypeByte & 0xF);
                }
                else if (eventTypeByte >> 4 == 0xD)     // if Channel Aftertouch command
                {
                    byte par1 = midiBinaryStream.ReadByte();
                    byte par2 = 0x0;
                    midiTracks[eventTypeByte & 0xF].midiEvents.Add(
                            new MessageMidiEvent(currentTick, (byte)(eventTypeByte & 0xF), NormalType.ChannelAftertouch, par1, par2));
                    // now backup channel and Normal Type for truncated commands
                    lastEventType = NormalType.ChannelAftertouch;
                    lastMidiChannel = (byte)(eventTypeByte & 0xF);
                }
                else if (eventTypeByte >> 4 == 0xE)     // if Pitch Bend command
                {
                    byte par1 = midiBinaryStream.ReadByte();
                    byte par2 = midiBinaryStream.ReadByte();
                    midiTracks[eventTypeByte & 0xF].midiEvents.Add(
                            new MessageMidiEvent(currentTick, (byte)(eventTypeByte & 0xF), NormalType.PitchBend, par1, par2));
                    // now backup channel and Normal Type for truncated commands
                    lastEventType = NormalType.PitchBend;
                    lastMidiChannel = (byte)(eventTypeByte & 0xF);
                }
                else if (eventTypeByte >> 4 < 0x8)
                {
                    byte par1 = eventTypeByte;
                    byte par2;
                    switch (lastEventType)
                    {
                        case NormalType.NoteOFF:
                            par2 = midiBinaryStream.ReadByte();
                            midiTracks[lastMidiChannel].midiEvents.Add(
                                    new MessageMidiEvent(currentTick, lastMidiChannel, NormalType.NoteOFF, par1, par2));
                            break;
                        case NormalType.NoteON:
                            par2 = midiBinaryStream.ReadByte();
                            midiTracks[lastMidiChannel].midiEvents.Add(
                                    new MessageMidiEvent(currentTick, lastMidiChannel, NormalType.NoteON, par1, par2));
                            break;
                        case NormalType.NoteAftertouch:
                            par2 = midiBinaryStream.ReadByte();
                            midiTracks[lastMidiChannel].midiEvents.Add(
                                    new MessageMidiEvent(currentTick, lastMidiChannel, NormalType.NoteAftertouch, par1, par2));
                            break;
                        case NormalType.Controller:
                            par2 = midiBinaryStream.ReadByte();
                            midiTracks[lastMidiChannel].midiEvents.Add(
                                    new MessageMidiEvent(currentTick, lastMidiChannel, NormalType.Controller, par1, par2));
                            break;
                        case NormalType.Program:
                            midiTracks[lastMidiChannel].midiEvents.Add(
                                    new MessageMidiEvent(currentTick, lastMidiChannel, NormalType.Program, par1, 0x0));
                            break;
                        case NormalType.ChannelAftertouch:
                            midiTracks[lastMidiChannel].midiEvents.Add(
                                    new MessageMidiEvent(currentTick, lastMidiChannel, NormalType.ChannelAftertouch, par1, 0x0));
                            break;
                        case NormalType.PitchBend:
                            par2 = midiBinaryStream.ReadByte();
                            midiTracks[lastMidiChannel].midiEvents.Add(
                                    new MessageMidiEvent(currentTick, lastMidiChannel, NormalType.PitchBend, par1, par2));
                            break;
                    }
                }
                else
                {
                    throw new Exception("Bad MIDI event at 0x" + midiBinaryStream.BaseStream.Position.ToString("X8") + ": 0x" + eventTypeByte.ToString("X2"));
                }
            }
            #endregion
        }

        private static void verifyMidi(Stream midiStream)     // throws an Exception if MIDI data is malformed ; FINISHED
        {
            byte[] midiHeaderString = new byte[4];
            midiStream.Position = 0;
            midiStream.Read(midiHeaderString, 0, 4);
            if (Encoding.ASCII.GetString(midiHeaderString, 0, 4) != "MThd") 
                throw new Exception("MThd string wasn't found in the MIDI header!");
            if (midiStream.ReadByte() != 0x0 || midiStream.ReadByte() != 0x0 || midiStream.ReadByte() != 0x0 || midiStream.ReadByte() != 0x6)
                throw new Exception("MThd chunk size not #0x6!");
            midiStream.Position = 0xA;
            int numTracks = midiStream.ReadByte() << 8 | midiStream.ReadByte();
            if (numTracks == 0) 
                throw new Exception("The MIDI has no tracks to convert!");
        }

        private static int getMidiType(Stream midiStream)     // returns the MIDI type automonously ; FINISHED
        {
            midiStream.Position = 9;    // position to the midi Type
            int returnValue = midiStream.ReadByte();
            return returnValue;
        }

        private static ushort loadBigEndianUshort(byte[] dataValues)    // returns an ushort by the Big Endian values in the Array ; FINISHED
        {
            return (ushort)(dataValues[0] << 8 | dataValues[1]);
        }

        private static int loadBigEndianInt(byte[] dataValues)       // returns an int by the Big Endian values in the Array ; FINISHED
        {
            return dataValues[0] << 24 | dataValues[1] << 16 | dataValues[2] << 8 | dataValues[3];
        }

        private static long readVariableLengthValue(BinaryReader midiBinaryStream)     // reads a variable Length value from the Filestream at its current position and extends the Stream position by the exact amount of bytes ; FINSHED
        {
            long backupPosition = midiBinaryStream.BaseStream.Position;
            int numBytes = 0;
            while (true)
            {
                numBytes++;
                if ((midiBinaryStream.ReadByte() & 0x80) == 0) 
                    break;
            }

            midiBinaryStream.BaseStream.Position = backupPosition;

            long returnValue = 0;

            for (int currentByte = 0; currentByte < numBytes; currentByte++)
            {
                returnValue = (returnValue << 7) | (byte)(midiBinaryStream.ReadByte() & 0x7F);
            }

            return returnValue;
        }
    }
}
