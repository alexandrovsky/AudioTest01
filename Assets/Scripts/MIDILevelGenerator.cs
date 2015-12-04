using UnityEngine;
using System.Collections;


[RequireComponent (typeof (MIDI))]
public class MIDILevelGenerator : MonoBehaviour {

	public GameObject [] platforms;
	public string midiFilePath = "./Assets/Audio/beat02.mid";
	public bool generateOnlyFromFirstTrack = false;


	MIDI midi;

	// Use this for initialization
	void Start () {
	
		midi = GetComponent<MIDI>();
		midi.ReadMIDIFile(midiFilePath);

		int trackCount = midi.tracks.Count;
		if(generateOnlyFromFirstTrack){
			trackCount = 1;
		}

		for(int i = 0; i < trackCount; i++){
			MIDITrack track = midi.tracks[i];
			foreach(MIDIEvent e in track.events){
				Debug.Log("type of event" + e.GetType().ToString());
				if(e.GetType() != typeof(MIDINote)){
					continue;
				}


				MIDINote note = (MIDINote)e;
				GameObject platform;
				if(this.platforms.Length > 0){
					int idx = Random.Range(0, this.platforms.Length);
					platform = GameObject.Instantiate(this.platforms[idx]) as GameObject;
				}else{
					platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
				}


				Vector3 pos = new Vector3(note.absoluteStartTime, (float)note.note, 0.0f);
				platform.transform.position = pos;
			}
		}


	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
