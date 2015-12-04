using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;


/*
A standard MIDI file is composed of "chunks". It starts with a header chunk and is followed by one or more track chunks. 
The header chunk contains data that pertains to the overall file. Each track chunk defines a logical track.

SMF = <header_chunk> + <track_chunk> [+ <track_chunk> ...]

----------------------------------------------------------------------------------------------------------------------------------------------


The header chunk consists of a literal string denoting the header, a length indicator, the format of the MIDI file, the number of tracks in the file, and a timing value specifying delta time units. Numbers larger than one byte are placed most significant byte first.
 
   header_chunk = "MThd" + <header_length> + <format> + <n> + <division>
 
"MThd" 4 bytes 
the literal string MThd, or in hexadecimal notation: 0x4d546864. These four characters at the start of the MIDI file indicate that this is a MIDI file.
<header_length> 4 bytes
length of the header chunk (always 6 bytes long--the size of the next three fields which are considered the header chunk).
<format> 2 bytes
0 = single track file format 
1 = multiple track file format 
2 = multiple song file format (i.e., a series of type 0 files)
<n> 2 bytes
number of track chunks that follow the header chunk
<division> 2 bytes
unit of time for delta timing. If the value is positive, then it represents the units per beat. For example, +96 would mean 96 ticks per beat. If the value is negative, delta times are in SMPTE compatible units.


----------------------------------------------------------------------------------------------------------------------------------------------

*/

// https://github.com/adamjmurray/MIDIFileReader.js/blob/master/src/MIDIFileReader.coffee
enum MIDI_EVENT_TYPES {
	HEADER_CHUNK_ID   = 0x4D546864, 	// MThd
	HEADER_CHUNK_SIZE = 0x06, 			// All MIDI headers include 6 bytes after the chunk ID and chunk size
	TRACK_CHUNK_ID = 0x4D54726B, 		// MTrk
	MICROSECONDS_PER_MINUTE = 60000000,


	META_EVENT  = 0xFF,
	SYSEX_EVENT = 0xF0,
	SYSEX_CHUNK = 0xF7, 				// a continuation of a normal SysEx event
	
	// Meta event types
	SEQ_NUMBER      = 0x00,
	TEXT            = 0x01,
	COPYRIGHT       = 0x02,
	SEQ_NAME        = 0x03,
	INSTRUMENT_NAME = 0x04,
	LYRICS          = 0x05,
	MARKER          = 0x06,
	CUE_POINT       = 0x07,
	CHANNEL_PREFIX  = 0x20,
	END_OF_TRACK    = 0x2F,
	TEMPO           = 0x51,
	SMPTE_OFFSET    = 0x54,
	TIME_SIGNATURE  = 0x58,
	KEY_SIGNATURE   = 0x59,
	SEQ_SPECIFIC    = 0x7F,
	
	// Channel event types
	NOTE_OFF           = 0x80,
	NOTE_ON            = 0x90,
	NOTE_AFTERTOUCH    = 0xA0,
	CONTROLLER         = 0xB0,
	PROGRAM_CHANGE     = 0xC0,
	CHANNEL_AFTERTOUCH = 0xD0,
	PITCH_BEND         = 0xE0,
}


enum KEY_VALUE_TO_NAME {
	C = 0,
	G = 1,
	D = 2,
	A = 3,
	E = 4,
	B = 5,
	F_SHARP = 6,
	C_SHARP = 7,
	F = -1,
	B_FLAT = -2,
	E_FLAT = -3,
	A_FLAT = -4,
	D_FLAT = -5,
	G_FLAT = -6,
	C_FLAT = -7
}
	

public struct MIDIHeader{
	public System.UInt32 MThd;
	public System.UInt32 header_length;
	public System.UInt16 format;
	public System.UInt16 number_of_tracks;
	public System.UInt16 division;
	public float tempo;

	public override string ToString()
	{
		return "MThd: " + MThd.ToString () 
			+ " header_length:" + header_length.ToString ()
			+ " format: " + format.ToString ()
			+ " # tracks: " + number_of_tracks.ToString ()
			+ " division" + division.ToString ();
	}
}

public class MIDIEvent{
	public int event_type;
	public int delta_time;
	public System.Object data;

	public MIDIEvent(){
		event_type = -1;
		delta_time = -1;
		data = -1;
	}

	public override string ToString()
	{
		return "event type: " + (MIDI_EVENT_TYPES)event_type 
			 	+ " delta time: " + delta_time
				+ " data: " + data != null ? data.ToString() : "no data";
	}
}

public class MIDINote : MIDIEvent{
	public UInt32 note;
	public UInt32 velocity;
	public UInt32 channel;
	public float duration;
	public float absoluteStartTime;

	public MIDINote(MIDIEvent e){
		this.delta_time = e.delta_time;
		this.data = e.data;
		this.event_type = e.event_type;
	}
}


public class MIDITrack{
	public System.UInt32 MTrk;
	public System.UInt32 track_length; // in bytes
	public List<MIDIEvent> events;

	public MIDITrack(){
		events = new List<MIDIEvent>();
	}

}

public class MIDI : MonoBehaviour {

	FileStream midiFile;

	public MIDIHeader header;
	public List<MIDITrack> tracks;

	Dictionary<UInt32, MIDINote> currentNotes;
	long timeOffset = 0;



	// Use this for initialization
	void Start () {

	}


	void OnGUI(){
//		if (GUI.Button(new Rect(10, 70, 120, 30), "Open MIDI File")){
////			string path = "./Assets/Audio/When_the_Saints_Go_Marching_In.mid";
//			string path = EditorUtility.OpenFilePanel("Select a MIDI file", "./Assets/Audio", "mid");
//			ReadMIDIFile(path);
//		}
	}


	public void ReadMIDIFile(string path){
		Debug.Log(path);
		
		this.midiFile = File.OpenRead(path);
		this.header = ParseHeader(midiFile);
		
		this.tracks = new List<MIDITrack>();
		this.currentNotes = new Dictionary<uint, MIDINote>();
		for(int i = 0; i < header.number_of_tracks; i++){
			MIDITrack track = ParseTrack(midiFile);
			this.tracks.Add(track);
		}
	}

	MIDIHeader ParseHeader (FileStream inFile){

		MIDIHeader header = new MIDIHeader ();

		// MThd
		{
			byte[] _mthd = new byte[4];
			inFile.Read(_mthd, 0, 4);
			Array.Reverse(_mthd);
			header.MThd = BitConverter.ToUInt32(_mthd, 0);
			if(header.MThd != (UInt32)MIDI_EVENT_TYPES.HEADER_CHUNK_ID){
				throw new Exception("wrong midi header");
			}
		}

		//header length;
		{
			byte[] _length = new byte[4];
			inFile.Read(_length, 0, 4);
			Array.Reverse(_length);
			header.header_length = BitConverter.ToUInt32(_length, 0);
			if(header.header_length != (UInt32)MIDI_EVENT_TYPES.HEADER_CHUNK_SIZE){
				throw new Exception("wrong header chunk size");
			}
		}

		// format
		{
			byte[] _format = new byte[2];
			inFile.Read(_format, 0, 2);
			Array.Reverse(_format);
			header.format = BitConverter.ToUInt16(_format, 0);
		}

		// num_of_tracks
		{
			byte[] _num_of_tracks = new byte[2];
			inFile.Read(_num_of_tracks, 0, 2);
			Array.Reverse(_num_of_tracks);
			header.number_of_tracks = BitConverter.ToUInt16(_num_of_tracks, 0);
		}

		// division
		{
			byte[] _division = new byte[2];
			inFile.Read(_division, 0, 2);
			Array.Reverse(_division);
			header.division = BitConverter.ToUInt16(_division, 0);
		}

		return header;
	}

	MIDITrack ParseTrack(FileStream inFile){

		MIDITrack track = new MIDITrack();


		// MThd
		{
			byte[] _mtrk = new byte[4];
			inFile.Read(_mtrk, 0, 4);
			Array.Reverse(_mtrk);
			track.MTrk = BitConverter.ToUInt32(_mtrk, 0);
			if(track.MTrk != (UInt32)MIDI_EVENT_TYPES.TRACK_CHUNK_ID){
				throw new Exception("wrong track header");
			}
		}

		// track length
		{
			byte[] _len = new byte[4];
			inFile.Read(_len, 0, 4);
			Array.Reverse(_len);
			track.track_length = BitConverter.ToUInt32(_len, 0);
		}

		// start reading events
		long pos = inFile.Position;
		this.timeOffset = 0;
		while(inFile.Position < pos + track.track_length){
			int deltaTime = ParseVarLen(inFile);
			this.timeOffset += deltaTime;


			MIDIEvent midi_event = new MIDIEvent();
			midi_event.delta_time = deltaTime;
			midi_event.event_type = inFile.ReadByte();

			switch(midi_event.event_type){
			case (int)MIDI_EVENT_TYPES.META_EVENT:
				ParseMetaEvent(inFile, midi_event);
//				Debug.Log("Meta event");
				break;
			case (int)MIDI_EVENT_TYPES.SYSEX_EVENT:
			case (int)MIDI_EVENT_TYPES.SYSEX_CHUNK:
				ParseSysexEvent(inFile, midi_event);
//				Debug.Log("Sysex event");
				break;
			default:
				midi_event = ParseChannelEvent(inFile, midi_event);
//				Debug.Log("channel event");
				break;
			}

			Debug.Log(midi_event);
			track.events.Add(midi_event);

		}


		return track;
	}


	MIDIEvent ParseChannelEvent(FileStream inFile, MIDIEvent midi_event){
		int event_type_mask = (midi_event.event_type & 0xF0);
		//int event_channel = (midi_event.event_type & 0x0f) + 1;

		var midi_event_new = midi_event;
		switch(event_type_mask){
		case (int)MIDI_EVENT_TYPES.NOTE_ON:
			Debug.Log("note on");
			midi_event_new = ParseNoteOnEvent(inFile, midi_event);
			break;
		case (int)MIDI_EVENT_TYPES.NOTE_OFF:
			midi_event_new = ParseNoteOffEvent(inFile, midi_event);
			Debug.Log("note off");
			break;
		case (int)MIDI_EVENT_TYPES.NOTE_AFTERTOUCH:
			Debug.Log("after touch");
			break;
		case (int)MIDI_EVENT_TYPES.CONTROLLER:
			Debug.Log("controller");
			break;
		case (int)MIDI_EVENT_TYPES.PROGRAM_CHANGE:
			Debug.Log("program change");
			break;
		case (int)MIDI_EVENT_TYPES.CHANNEL_AFTERTOUCH:
			Debug.Log("channel aftertouch");
			break;
		case (int)MIDI_EVENT_TYPES.PITCH_BEND:
			Debug.Log("pitchbend");
			break;
		}

		return midi_event_new;

	}

	MIDINote ParseNoteOnEvent(FileStream inFile, MIDIEvent midi_event){
		MIDINote note = new MIDINote(midi_event);


		note.note = (UInt32)inFile.ReadByte();
		note.velocity = (UInt32)inFile.ReadByte();
		note.absoluteStartTime = CurrentTime();

		Debug.Log(midi_event);
		if(note.velocity == 0){ // note off
			ParseNoteOffEvent(null, note);
		}else{
			if(!currentNotes.ContainsKey(note.note)){
				currentNotes.Add(note.note, note);
			}
		}

		return note;

	}

	MIDINote ParseNoteOffEvent(FileStream inFile, MIDIEvent midi_event){

		MIDINote note;

		if(inFile != null){
			note = new MIDINote(midi_event);
			note.note = (UInt32)inFile.ReadByte();
			note.velocity = (UInt32)inFile.ReadByte();
		}else{
			note = (MIDINote)midi_event;
		}
		if(this.currentNotes.ContainsKey(note.note) ){
			this.currentNotes[note.note].duration = CurrentTime() - note.absoluteStartTime;
			this.currentNotes.Remove(note.note);
		}else{
			Debug.LogWarning("try to access not existing note: "  + note.note);
		}

		return note;

	}

	void ParseSysexEvent(FileStream inFile, MIDIEvent midi_event){
		int length = ParseVarLen(inFile);
		byte[] data = new byte[length];
		inFile.Read(data, 0, length);
		Array.Reverse(data);
		midi_event.data = data;
	}

	void ParseMetaEvent(FileStream inFile, MIDIEvent midi_event){
		int type = inFile.ReadByte();
		midi_event.event_type = type;
		byte[] data;
		switch(type){
		case (int)MIDI_EVENT_TYPES.SEQ_NUMBER:
			midi_event.data = ParseMetaValue(inFile);
			break;
		case (int)MIDI_EVENT_TYPES.TEXT:
			midi_event.data = ParseMetaText(inFile);
			break;
		case (int)MIDI_EVENT_TYPES.COPYRIGHT:
			midi_event.data = ParseMetaText(inFile);
			break;
		case (int)MIDI_EVENT_TYPES.SEQ_NAME:
			midi_event.data = ParseMetaText(inFile);
			break;
		case (int)MIDI_EVENT_TYPES.INSTRUMENT_NAME:
			midi_event.data = ParseMetaText(inFile);
			break;
		case (int)MIDI_EVENT_TYPES.LYRICS:
			midi_event.data = ParseMetaText(inFile);
			break;
		case (int)MIDI_EVENT_TYPES.MARKER:
			midi_event.data = ParseMetaText(inFile);
			break;
		case (int)MIDI_EVENT_TYPES.CUE_POINT:
			midi_event.data = ParseMetaText(inFile);
			break;
		case (int)MIDI_EVENT_TYPES.CHANNEL_PREFIX:
			midi_event.data = ParseMetaValue(inFile);
			break;
		case (int)MIDI_EVENT_TYPES.END_OF_TRACK:
			midi_event.data = ParseMetaData(inFile);
			break;
		case (int)MIDI_EVENT_TYPES.TEMPO:
			int nomenator = (int)MIDI_EVENT_TYPES.MICROSECONDS_PER_MINUTE;
			int denomenator = (int)ParseMetaValue(inFile);
			float tempo = (float)nomenator/ (float)denomenator; // bpm
			midi_event.data = tempo;
			this.header.tempo = tempo; // set song tempo here!!!!
			break;
		case (int)MIDI_EVENT_TYPES.SMPTE_OFFSET:

			data = ParseMetaData(inFile);
			// TODO: parse [firstByte, minute, second, frame, subframe]
			midi_event.data = data;
			break;
		case (int)MIDI_EVENT_TYPES.TIME_SIGNATURE:
			data = ParseMetaData(inFile);
			Dictionary<String,Int32> signature = new Dictionary<string, int>();
			signature.Add("numerator", data[0]);
			signature.Add("denumerator", (int)Math.Pow((double)data[1], (double)2.0));
			midi_event.data = signature;
			break;
		case (int)MIDI_EVENT_TYPES.KEY_SIGNATURE:
			data = ParseMetaData(inFile);
			int _key = (data[0] ^ 128) - 128; // convert from unsigned byte to signed byte
			int _scale = data[1]; // if 0 -> major if 1 -> minor
			Dictionary<string, string> key = new Dictionary<string, string>();
			key.Add("key", ((KEY_VALUE_TO_NAME)_key).ToString());
			key.Add ("scale", _scale == 0 ? "major" : "minor");
			midi_event.data = key;
			break;
		case (int)MIDI_EVENT_TYPES.SEQ_SPECIFIC:
			midi_event.data = ParseMetaData(inFile);
			break;
		default:
			Debug.LogWarning("ignoring unknown meta event on track number");
			midi_event.data = ParseMetaData(inFile);
			break;
		}

	}

	int ParseVarLen(FileStream inFile){
		int data = 0;
		int _byte = inFile.ReadByte();
		while ((_byte & 0x80) != 0){
			data = (data << 7) + (_byte & 0x7F);
			_byte = inFile.ReadByte();
		}
			
		data = (data << 7) + (_byte & 0x7F);
		return data;
	}

	System.Object ParseMetaValue(FileStream inFile){
		int length = ParseVarLen(inFile);

		byte[] data = new byte[length];
		inFile.Read(data, 0, length);

		int result = 0;

		for(int i = 0; i < length; i++){
			result = (result << 8) + data[i];
		}

		return result;

	}

	String ParseMetaText(FileStream inFile){
		int length = ParseVarLen(inFile);
		byte[] _data = new byte[length];
		inFile.Read(_data, 0, length);
		String data = System.Text.Encoding.ASCII.GetString(_data);

		
		return data;
	}

	byte[] ParseMetaData(FileStream inFile){
		int length = ParseVarLen(inFile);

		byte[] _data = new byte[length];
		inFile.Read(_data, 0, length);

		return _data;
	}

	float CurrentTime(){
		return (float)this.timeOffset/(float)this.header.division;
	}





}
