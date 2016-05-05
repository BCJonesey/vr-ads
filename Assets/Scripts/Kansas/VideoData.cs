using UnityEngine;
using System;
using System.Collections;

public class VideoData {

	private string mediaPath;
	public string MediaPath {
		get {
			return mediaPath;
		}
		set {
			mediaPath = value;
		}
	}

	private int currentPosition = 0;
	public int CurrentPosition {
		get {
			return currentPosition;
		}
		set {
			currentPosition = value;
		}
	}
		
	private bool started = false;
	public bool Started {
		get {
			return started;
		}
		set {
			started = value;
		}
	}

	private bool playing = false;
	public bool Playing { 
		get {
			return playing;
		}
		set {
			playing = value;
		}
	}

	public bool Paused { 
		get {
			return !playing;
		}
	}

	public VideoData(string url) {
		mediaPath = url;
	}

	public static bool operator ==(VideoData a, VideoData b)
	{
		// If both are null, or both are same instance, return true.
		if (System.Object.ReferenceEquals(a, b)) {
			return true;
		}
		
		// If one is null, but not both, return false.
		if (((object)a == null) || ((object)b == null)) {
			return false;
		}
		
		// Return true if the fields match:
		return a.MediaPath == b.MediaPath;
	}

	public static bool operator !=(VideoData a, VideoData b) {
		return !(a == b);
	}
		
}

