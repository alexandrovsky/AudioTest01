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

public struct MIDIHeader{
	public System.UInt32 MThd;
	public System.UInt32 header_length;
	public System.UInt16 format;
	public System.UInt16 number_of_tracks;
	public System.Int16 division;


	public override string ToString ()
	{
		return "MThd: " + MThd.ToString () 
			+ " header_length:" + header_length.ToString ()
			+ " format: " + format.ToString ()
			+ " # tracks: " + number_of_tracks.ToString ()
			+ " division" + division.ToString ();
	}
}

public struct MIDIEvent{
	public System.Byte v_time;
	public System.Byte midi_event;
	public System.Byte meta_event;
	public System.Byte sysex_event;
}

public struct MIDITrack{
	public System.UInt32 MTrk;
	public System.UInt32 track_length;
	public List<MIDIEvent> events;

}

public class MIDI : MonoBehaviour {

	byte[] midiFile;

	MIDIHeader header;
	List<MIDITrack> tracks;

	// Use this for initialization
	void Start () {
		string path = EditorUtility.OpenFilePanel ("Select a MIDI file", "./Assets/Audio", "mid");
		Debug.Log(path);

		midiFile = File.ReadAllBytes (path);

		header = ParseHeader(midiFile);

		int offset = 14;



		MIDITrack track = ParseTrack(midiFile, offset);
		tracks.Add(track);


	}



	MIDIHeader ParseHeader (byte[] midi){

		Debug.Log ("whole header" + BitConverter.ToString(midi, 0, 14)); 

		MIDIHeader header = new MIDIHeader ();

		string MThd = BitConverter.ToString(midi, 0, 4);
		Debug.Log("MThd: " + MThd);
		header.MThd = BitConverter.ToUInt32(midi, 0);

		string header_length = BitConverter.ToString(midi, 4, 4); 
		Debug.Log("header length: " + header_length);
		byte[] bla = new byte[4];
		bla [0] = midi [7];
		bla [1] = midi [6];
		bla [2] = midi [5];
		bla [3] = midi [4];
		header.header_length = BitConverter.ToUInt32(bla, 0); //(System.UInt32)(midi[4] << 24 | midi[5] << 16 | midi[6] << 8 | midi[7]);//

		string format = BitConverter.ToString(midi, 8, 2); 
		Debug.Log("format: " + format);
		header.format = (System.UInt16)(midi[8] << 8 | midi[9]); //BitConverter.ToUInt16(midi, 8);

		string num_of_tracks = BitConverter.ToString(midi, 10, 2); 
		Debug.Log("# tracks: " + num_of_tracks);
		header.number_of_tracks = (System.UInt16)(midi[10] << 8 | midi[11]); // BitConverter.ToUInt16(midi, 10);

		string division = BitConverter.ToString(midi, 12, 2); 
		Debug.Log("division: " + division);
		header.division = (System.Int16)(midi[12] << 8 | midi [13]); //

		//-------
		Debug.Log ("header" + header);
		return header;
	}


	MIDITrack ParseTrack (byte[] midi, int offset){
		MIDITrack track = new MIDITrack();

		Debug.Log ("track offset: " + offset);

		string MTrk = BitConverter.ToString(midi, offset, 4);
		Debug.Log("MTrk: " + MTrk);
		track.MTrk = BitConverter.ToUInt32(midi, offset);

		offset += 4;
		string track_length = BitConverter.ToString(midi, offset, 4);
		Debug.Log("track length: " + track_length);
		track.track_length = (System.UInt32)(midi[offset] << 24 | midi[offset+1] << 16 | midi[offset+2] << 8 | midi[offset+3]);

		offset += 4;
		int start = 0;
		while (start < track.track_length) {
			MIDIEvent e = ParseEvent(midi, start + offset);
		}



		return track;
	}


	MIDIEvent ParseEvent(byte[] midi, int offset){

		MIDIEvent e = new MIDIEvent();

		return e;

	}

	// Update is called once per frame
	void Update () {
	
	}
}
