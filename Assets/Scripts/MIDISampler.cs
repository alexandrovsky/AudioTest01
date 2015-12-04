using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class MIDISampler : MonoBehaviour {

//	MIDI midi;
	public AudioClip[] samples;
	AudioSource audioSrc;
	void Start () {
//		midi = GetComponent<MIDI>();
		audioSrc = GetComponent<AudioSource>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}


	public void PlayNote(uint note){
		AudioClip clip = samples[note];
		if(clip!= null){
			audioSrc.PlayOneShot(clip);
		}else{
			Debug.LogWarning("no sample for note set");
		}

	}
}
