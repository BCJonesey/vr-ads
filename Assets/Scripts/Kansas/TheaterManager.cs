using UnityEngine;
using System.IO;
using System.Collections;
using SimpleJSON;

public class TheaterManager : MonoBehaviour {

	public AndroidMoviePlayer featureFilm = null;
	public AndroidMoviePlayer advertisment = null;

	public Poster rightPoster = null;
	public Poster leftPoster = null;

	public float timeLeftBeforeAd = 60.0f;
	private bool decrementTimeLeftBeforeAd = false;

	void Start() {
		// Load featureFilm
		StartCoroutine(LoadFeatureFilmData());

		// Load ad
		StartCoroutine(LoadAdData());
	}

	private IEnumerator LoadFeatureFilmData() {
		string streamingMediaPath = Application.streamingAssetsPath + "/HenryShort.mp4";
		string persistentPath = Application.persistentDataPath + "/HenryShort.mp4";

		// Cache if doesn't exist
		if (!File.Exists(persistentPath)) {
			WWW wwwReader = new WWW(streamingMediaPath);
			yield return wwwReader;

			if (wwwReader.error != null) {
				Debug.LogError("wwwReader error: " + wwwReader.error);
			}

			System.IO.File.WriteAllBytes(persistentPath, wwwReader.bytes);
		}

		VideoData movieData = new VideoData(persistentPath);
		featureFilm.LoadVideo(movieData, FeatureFilmLoaded);
	}

	private IEnumerator LoadAdData() {
		WWW www = new WWW("https://fake-ads.herokuapp.com/ad");
		yield return www;
		var responseJSON = JSON.Parse(www.text);

		string videoURL = responseJSON["videoUrl"];
		string rightPosterURL = responseJSON["rightPoster"];
		string leftPosterURL = responseJSON["leftPoster"];
	
		if (videoURL != string.Empty) {
			Debug.Log ("Loading ad: " + videoURL);
			AdData data = new AdData(videoURL, rightPosterURL, leftPosterURL);
			advertisment.LoadVideo(data.AdVideoData, AdLoaded);
		} else {
			Debug.LogError("No media file name provided");
		}
	}

	public void FeatureFilmLoaded(bool loaded) {
		Debug.Log("Feature Film Loaded!");
		featureFilm.StartVideo();
	}

	public void AdLoaded(bool loaded) {
		Debug.Log("Ad Loaded!");
	}

	// Update is called once per frame
	void Update () {
		if (decrementTimeLeftBeforeAd) {
			timeLeftBeforeAd -= Time.deltaTime;
			if (timeLeftBeforeAd < 0.0f) {
				// Play ad
				decrementTimeLeftBeforeAd = false;
			}
		}
	}

	void OnApplicationPause(bool wasPaused) {
		Debug.Log("OnApplicationPause: " + wasPaused);
	}
}
