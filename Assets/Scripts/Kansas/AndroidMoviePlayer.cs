/************************************************************************************

Filename    :   AndroidMovicePlayer.cs
Content     :   An example of how to use the Moonlight video player
Created     :   July 12, 2014

Copyright   :   Copyright 2014 Oculus VR, LLC. All Rights reserved.

Use of this software is subject to the terms of the Oculus LLC license
agreement provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

************************************************************************************/

using UnityEngine;
using System.Collections;					// required for Coroutines
using System.Runtime.InteropServices;		// required for DllImport
using System;								// requred for IntPtr
using System.IO;							// required for File
using SimpleJSON;

public class AndroidMoviePlayer : MonoBehaviour {

	public GameObject rightPoster = null;
	public GameObject leftPoster = null;
	
	private bool	videoPaused = false;

	private IntPtr nativeTexturePtr = IntPtr.Zero;

	private AndroidJavaObject 	mediaPlayer = null;
	private Renderer 			mediaRenderer = null;

	private VideoData movieData;
	private VideoData currentVideo;

	private float timeLeft = 0.0f;
	private bool checkTime = false;

	private enum MediaSurfaceEventType {
		Initialize = 0,
		Shutdown = 1,
		Update = 2,
		Max_EventType
	};
		
	private static int _eventBase = 0;
	public static int eventBase {
		get { return _eventBase; }
		set {
			_eventBase = value;
			OVR_Media_Surface_SetEventBase(_eventBase);
		}
	}
		
	void Awake() {
		Debug.Log("MovieSample Awake");

		OVR_Media_Surface_Init();

		// Default to the main OVR plugin event max to avoid conflicts.
		eventBase = System.Enum.GetValues(typeof(RenderEventType)).Length;

		mediaRenderer = GetComponent<Renderer>();

		if (mediaRenderer.material == null || mediaRenderer.material.mainTexture == null) {
			Debug.LogError("Can't GetNativeTexturePtr() for movie surface");
		}

		nativeTexturePtr = mediaRenderer.material.mainTexture.GetNativeTexturePtr();

		IssuePluginEvent(MediaSurfaceEventType.Initialize);

		// Load media player
		mediaPlayer = new AndroidJavaObject("android/media/MediaPlayer");
	}

	void Start() {
		Debug.Log("MovieSample Start");
		LoadMovie();
	}
	
	void Update() {
		IssuePluginEvent(MediaSurfaceEventType.Update);

		if (currentVideo != null && checkTime) {
			timeLeft -= Time.deltaTime;
			if (timeLeft < 0.0f) {
				if (currentVideo == movieData) {
					Debug.Log("FetchAndStartAd");
					StartCoroutine(FetchAndStartAd());
				} else {
					Debug.Log("LoadMovieInternal");
					StartCoroutine(LoadMovieInternal(true));
				}
				checkTime = false;
			}
		}
	}

	void OnApplicationPause(bool wasPaused) {
		Debug.Log("OnApplicationPause: " + wasPaused);
		if (mediaPlayer != null) {
			videoPaused = wasPaused;
			try {
				mediaPlayer.Call((videoPaused) ? "pause" : "start");
			} catch (Exception e) {
				Debug.Log("Failed to start/pause mediaPlayer with message " + e.Message);
			}
		}
	}
	
	private void OnApplicationQuit() {
		Debug.Log("OnApplicationQuit");
		
		// This will trigger the shutdown on the render thread
		IssuePluginEvent(MediaSurfaceEventType.Shutdown);
	}

	private static void IssuePluginEvent(MediaSurfaceEventType eventType) {
		GL.IssuePluginEvent((int)eventType + eventBase);
	}
		
	[DllImport("OculusMediaSurface")]
	private static extern void OVR_Media_Surface_Init();

	[DllImport("OculusMediaSurface")]
	private static extern IntPtr OVR_Media_Surface(IntPtr surfaceTexId, int surfaceWidth, int surfaceHeight);
	
	[DllImport("OculusMediaSurface")]
	private static extern void OVR_Media_Surface_SetEventBase(int eventBase);

	public void LoadMovie() {
		StartCoroutine(LoadMovieInternal());
	}

	public void PlayMovie() {
		StartVideo();
	}
	
	public void PlayAd() {
		StartCoroutine(FetchAndStartAd());
	}

	private IEnumerator LoadMovieInternal(bool startVideo = false) {
		string streamingMediaPath = Application.streamingAssetsPath + "/HenryShort.mp4";
		string persistentPath = Application.persistentDataPath + "/HenryShort.mp4";
		if (!File.Exists(persistentPath)) {
			WWW wwwReader = new WWW(streamingMediaPath);
			yield return wwwReader;

			if (wwwReader.error != null) {
				Debug.LogError("wwwReader error: " + wwwReader.error);
			}

			System.IO.File.WriteAllBytes(persistentPath, wwwReader.bytes);
		}
		
		if (movieData == null) {
			movieData = new VideoData(persistentPath);
		}
	
		StartCoroutine(LoadVideo(movieData, startVideo));
	}

	private IEnumerator FetchAndStartAd() {
		WWW www = new WWW("https://fake-ads.herokuapp.com/ad");
		yield return www;
		var responseJSON = JSON.Parse(www.text);
		
		string videoURL = responseJSON["videoUrl"];
		string rightPosterURL = responseJSON["rightPoster"];
		string leftPosterURL = responseJSON["leftPoster"];
		
		StartCoroutine(LoadTexture(rightPoster, rightPosterURL));
		StartCoroutine(LoadTexture(leftPoster, leftPosterURL));

		if (videoURL != string.Empty) {
			Debug.Log ("Showing ad: " + videoURL);
			VideoData data = new VideoData(videoURL);
			StartCoroutine(LoadVideo(data, true));
		} else {
			Debug.LogError("No media file name provided");
		}
	}
	
	private IEnumerator LoadTexture(GameObject go, string url) {
		WWW www = new WWW(url);
		yield return www;
		go.GetComponent<Renderer>().material.mainTexture = www.texture;
	}

	private IEnumerator LoadVideo(VideoData video, bool startVideo = false) {
		// Stop video if playing
		if (currentVideo != null) {
			Debug.Log("Stopping current video: " + currentVideo.MediaPath);
			mediaPlayer.Call("stop");
			if (currentVideo == movieData) {
				movieData.Playing = false;
				movieData.CurrentPosition = mediaPlayer.Call<int>("getCurrentPosition") - 2;
			}
			// Release media from player
			mediaPlayer.Call("release");
		}

		currentVideo = video;
		
		yield return null; // delay 1 frame to allow MediaSurfaceInit from the render thread.
		Debug.Log("Loading video: " + video.MediaPath);
		
		IntPtr androidSurface = OVR_Media_Surface(nativeTexturePtr, 2880, 1440);

		mediaPlayer = new AndroidJavaObject("android/media/MediaPlayer");
		
		// Set surface view
		IntPtr setSurfaceMethodId = AndroidJNI.GetMethodID(mediaPlayer.GetRawClass(),"setSurface","(Landroid/view/Surface;)V");
		jvalue[] parameters = new jvalue[1];
		parameters[0] = new jvalue();
		parameters[0].l = androidSurface;
		AndroidJNI.CallObjectMethod(mediaPlayer.GetRawObject(), setSurfaceMethodId, parameters);
		
		try {
			// Set source
			mediaPlayer.Call("setDataSource", currentVideo.MediaPath);
			mediaPlayer.Call("prepare");
			if (currentVideo.CurrentPosition > 0 ) {
				mediaPlayer.Call("seekTo", currentVideo.CurrentPosition);
			}
			if (startVideo) {
				StartVideo();
			}
		} catch (Exception e) {
			Debug.Log("Failed to load mediaPlayer with message " + e.Message);
		}
	}

	private void StartVideo() {
		try {
			Debug.Log("Starting video: " + currentVideo.MediaPath);
			if (currentVideo == movieData) {
				timeLeft = 30.0f;
			} else {
				timeLeft = (float) (mediaPlayer.Call<int>("getDuration") / 1000);
			}
			checkTime = true;
			Debug.Log("timeLeft: " + timeLeft);
			mediaPlayer.Call("start");
		} catch (Exception e) {
			Debug.Log("Failed to start mediaPlayer with message " + e.Message);
		}
	}
	
}
