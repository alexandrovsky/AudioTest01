using UnityEngine;
using System.Collections;


[RequireComponent(typeof(MIDI))]
[RequireComponent(typeof(MIDISampler))]
public class MIDISequencer : MonoBehaviour {

	MIDI midi;
	MIDISampler sampler;
	bool isPlaying = false;
	float currentTime;
	float timeSinceLasteEvent;
	int currentEventIdx;




	// Use this for initialization
	void Start () {
		midi = GetComponent<MIDI>();
		sampler = GetComponent<MIDISampler>();



	}


	void OnGUI(){
		if (GUI.Button(new Rect(10, 70, 120, 30), "Play")){
			AudioSource audioSrc = GetComponent<AudioSource>();
			
			MIDITrack track = midi.tracks[0];
			foreach(MIDIEvent e in track.events){
				Debug.Log("type of event" + e.GetType().ToString());
				if(e.GetType() != typeof(MIDINote)){
					continue;
				}
				
				
				MIDINote note = (MIDINote)e;
				Debug.Log("note:" + note.note);
				audioSrc.clip = sampler.samples[note.note];
				audioSrc.PlayScheduled((double)note.absoluteStartTime);
			}
		}
	}

	// Update is called once per frame
	void Update () {
	
//		if(!isPlaying) return; // ---- OUT ---->
//
//
//		bool getNextEvent = true;
//
//		while(getNextEvent){
//			MIDIEvent e = midi.tracks[0].events[currentEventIdx];
//			if(e.GetType() == typeof(MIDINote)){
//				MIDINote note = e as MIDINote;
//				if(currentNote.delta_time == 0){
//					sampler.PlayNote(currentNote.note);
//					currentEventIdx++;
//				}
//			}
//
//		}





		
	}



}
