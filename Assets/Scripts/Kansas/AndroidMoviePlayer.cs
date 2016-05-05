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
	private IntPtr nativeTexturePtr = IntPtr.Zero;

	private AndroidJavaObject 	mediaPlayer = null;
	private Renderer 			mediaRenderer = null;

	private VideoData movieData;
	private VideoData currentVideo;

	private enum MediaSurfaceEventType {
		Initialize = 0,
		Shutdown = 1,
		Update = 2,
		Max_EventType
	};

	private static int _instanceCount = 0;
		
	private static int _eventBase = 0;
	public static int eventBase {
		get { return _eventBase; }
		set {
			_eventBase = value;
			OVR_Media_Surface_SetEventBase(_eventBase);
		}
	}
		
	void Awake() {
		Debug.Log("AndroidMoviePlayer Awake");

		OVR_Media_Surface_Init();

		// Default to the main OVR plugin event max to avoid conflicts.
		eventBase = System.Enum.GetValues(typeof(RenderEventType)).Length + _instanceCount;
		_instanceCount++;

		mediaRenderer = GetComponent<Renderer>();

		if (mediaRenderer.material == null || mediaRenderer.material.mainTexture == null) {
			Debug.LogError("Can't GetNativeTexturePtr() for movie surface");
		}

		nativeTexturePtr = mediaRenderer.material.mainTexture.GetNativeTexturePtr();

		IssuePluginEvent(MediaSurfaceEventType.Initialize);

		// Load media player
		mediaPlayer = new AndroidJavaObject("android/media/MediaPlayer");
	}
	
	void Update() {
		IssuePluginEvent(MediaSurfaceEventType.Update);
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

	public void LoadVideo(VideoData videoData, VideoLoadedCallback callback = null, bool autoPlay = false) {
		StartCoroutine(LoadVideoInternal(videoData, callback, autoPlay));
	}

	public void StartVideo() {
		try {
			mediaPlayer.Call("start");
		} catch (Exception e) {
			Debug.Log("Failed to start mediaPlayer with message " + e.Message);
		}
	}

	public void TogglePlay() {
		if (mediaPlayer != null && currentVideo != null) {
			currentVideo.Playing = !currentVideo.Paused;
			try {
				mediaPlayer.Call((currentVideo.Paused) ? "pause" : "start");
			} catch (Exception e) {
				Debug.Log("Failed to start/pause mediaPlayer with message " + e.Message);
			}
		}
	}

	public delegate void VideoLoadedCallback(bool loaded);
		
	private IEnumerator LoadVideoInternal(VideoData video, VideoLoadedCallback callback = null, bool autoPlay = false) {
		// Stop and unload video if one is already playing
		if (currentVideo != null) {
			Debug.Log("Stopping current video: " + currentVideo.MediaPath);
			mediaPlayer.Call("stop");
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
			if (callback != null) {
				callback(true);
			}
			if (autoPlay) {
				StartVideo();
			}
		} catch (Exception e) {
			Debug.Log("Failed to load mediaPlayer with message " + e.Message);
			if (callback != null) {
				callback(false);
			}
		}
	}
}
