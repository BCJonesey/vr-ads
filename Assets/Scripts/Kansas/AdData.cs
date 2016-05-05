using UnityEngine;
using System.Collections;

public class AdData : MonoBehaviour {

	private VideoData videoData;
	public VideoData AdVideoData {
		get {
			return videoData;
		}
		set {
			videoData = value;
		}
	}

	private Texture rightPosterTexture;
	public Texture RightPosterTexture {
		get {
			return rightPosterTexture;
		}
		set {
			rightPosterTexture = value;
		}
	}

	private Texture leftPosterTexture;
	public Texture LeftPosterTexture {
		get {
			return leftPosterTexture;
		}
		set {
			leftPosterTexture = value;
		}
	}

	private string rightPosterURL;
	public string RightPosterURL {
		get {
			return rightPosterURL;
		}
		set {
			rightPosterURL = value;
		}
	}

	private string leftPosterURL;
	public string LeftPosterURL {
		get {
			return leftPosterURL;
		}
		set {
			leftPosterURL = value;
		}
	}

	public AdData(string videoURL, string leftPosterURL, string rightPosterURL) {
		this.videoData = new VideoData(videoURL);
		this.leftPosterURL = leftPosterURL;
		this.rightPosterURL = rightPosterURL;
		/*
		if (leftPosterURL != null) {
			StartCoroutine(LoadTexture(leftPosterURL, SaveLeftPosterTexture));
		}

		if (rightPosterURL != null) {
			StartCoroutine(LoadTexture(rightPosterURL, SaveRightPosterTexture));
		}
		*/
	}

	delegate void TextureLoaded(Texture tex);

	private void SaveLeftPosterTexture(Texture tex) {
		leftPosterTexture = tex;
	}

	private void SaveRightPosterTexture(Texture tex) {
		rightPosterTexture = tex;
	}

	private IEnumerator LoadTexture(string url, TextureLoaded callback) {
		WWW www = new WWW(url);
		yield return www;
		callback(www.texture);
	}

}
