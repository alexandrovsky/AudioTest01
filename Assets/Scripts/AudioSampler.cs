using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class AudioSampler : MonoBehaviour {




	// Use this for initialization
	void Start () {
		AudioSource audio = GetComponent<AudioSource>();


		Debug.Log(audio.clip.GetType());

		audio.Play();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
