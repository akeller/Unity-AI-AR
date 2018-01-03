using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using IBM.Watson.DeveloperCloud.Services.Conversation.v1;
using IBM.Watson.DeveloperCloud.Services.TextToSpeech.v1;
using IBM.Watson.DeveloperCloud.Services.SpeechToText.v1;
using IBM.Watson.DeveloperCloud.Widgets;
using IBM.Watson.DeveloperCloud.DataTypes;
using IBM.Watson.DeveloperCloud.Utilities;
using IBM.Watson.DeveloperCloud.Logging;
using IBM.Watson.DeveloperCloud.Connection;
using System.IO;
using FullSerializer;

public class SoldierConvo : MonoBehaviour {

	private int _recordingRoutine = 0;
	private string _microphoneID = null;
	private AudioClip _recording = null;
	private int _recordingBufferSize = 2;
	private int _recordingHZ = 22050;

	private string outputText = "";
	private Conversation _conversation;
	private SpeechToText _speechToText;
	private TextToSpeech _textToSpeech;
	private string workspace_id = ""; //enter Conversation workspace_id

	private fsSerializer _serializer = new fsSerializer();
	private Dictionary<string, object> _context = null;
	private bool stopListeningFlag = false;

	void Start()
	{
		InitializeServices();

		//enter workspace_id as string, this kicks off the conversation
		if (!_conversation.Message (OnMessage, OnFail, workspace_id, "Hi")) {
			Log.Debug ("ExampleConversation.Message()", "Failed to message!");
		}
	}

	private void OnMessage(object resp, Dictionary<string, object> customData)
	{
		fsData fsdata = null;
		fsResult r = _serializer.TrySerialize(resp.GetType(), resp, out fsdata);
		if (!r.Succeeded)
			throw new WatsonException(r.FormattedMessages);

		//  Convert fsdata to MessageResponse
		MessageResponse messageResponse = new MessageResponse();
		object obj = messageResponse;
		r = _serializer.TryDeserialize(fsdata, obj.GetType(), ref obj);
		if (!r.Succeeded)
			throw new WatsonException(r.FormattedMessages);

		//  Set context for next round of messaging
		object _tempContext = null;
		(resp as Dictionary<string, object>).TryGetValue("context", out _tempContext);

		if (_tempContext != null)
			_context = _tempContext as Dictionary<string, object>;
		else
			Log.Debug("ExampleConversation.OnMessage()", "Failed to get context");

		//if we get a response, do something with it (find the intents, output text, etc.)
		if (resp != null && (messageResponse.intents.Length > 0 || messageResponse.entities.Length > 0))
		{
			string intent = messageResponse.intents[0].intent;
			foreach (string WatsonResponse in messageResponse.output.text) {
				outputText += WatsonResponse + " ";
			}
			Debug.Log("Intent/Output Text: " + intent + "/" + outputText);
			if (intent.Contains("exit")) {
				stopListeningFlag = true;
			}
			CallTTS (outputText);
			outputText = "";
		}
	}

	private void OnSpeechInput(SpeechRecognitionEvent result)
	{
		if (result != null && result.results.Length > 0)
		{
			foreach (var res in result.results)
			{
				foreach (var alt in res.alternatives)
				{
					if (res.final && alt.confidence > 0)
					{
						string text = alt.transcript;
						Debug.Log("Result: " + text + " Confidence: " + alt.confidence);
						BuildSpokenRequest(text);
					}
				}
			}
		}
	}

	private void BuildSpokenRequest(string spokenText)
	{
		MessageRequest messageRequest = new MessageRequest()
		{
			input = new Dictionary<string, object>()
			{
				{ "text", spokenText }
			},
			context = _context
		};

		if (_conversation.Message(OnMessage, OnFail, workspace_id, messageRequest))
			Log.Debug("ExampleConversation.AskQuestion()", "Failed to message!");
	}

	private void CallTTS (string outputText)
	{
		//Call text to speech
		if(!_textToSpeech.ToSpeech(OnSynthesize, OnFail, outputText, false))
			Log.Debug("ExampleTextToSpeech.ToSpeech()", "Failed to synthesize!");
	}

	private void OnSynthesize(AudioClip clip, Dictionary<string, object> customData)
	{
		PlayClip(clip);

		if (!stopListeningFlag) {
			OnListen();
		}
	}

	private void PlayClip(AudioClip clip)
	{
		if (Application.isPlaying && clip != null)
		{
			GameObject audioObject = new GameObject("AudioObject");
			AudioSource source = audioObject.AddComponent<AudioSource>();
			source.spatialBlend = 0.0f;
			source.loop = false;
			source.clip = clip;
			source.Play();

			Destroy(audioObject, clip.length);
		}
	}

	private void OnListen()
	{
		Log.Debug("ExampleStreaming", "Start();");

		Active = true;

		StartRecording();
	}

	public bool Active
	{
		get { return _speechToText.IsListening; }
		set {
			if ( value && !_speechToText.IsListening )
			{
				_speechToText.DetectSilence = true;
				_speechToText.EnableWordConfidence = false;
				_speechToText.EnableTimestamps = false;
				_speechToText.SilenceThreshold = 0.03f;
				_speechToText.MaxAlternatives = 1;
				//_speechToText.EnableContinousRecognition = true;
				_speechToText.EnableInterimResults = true;
				_speechToText.OnError = OnError;
				_speechToText.StartListening( OnSpeechInput );
			}
			else if ( !value && _speechToText.IsListening )
			{
				_speechToText.StopListening();
			}
		}
	}

	private void StartRecording()
	{
		if (_recordingRoutine == 0)
		{
			UnityObjectUtil.StartDestroyQueue();
			_recordingRoutine = Runnable.Run(RecordingHandler());
		}
	}

	private void StopRecording()
	{
		if (_recordingRoutine != 0)
		{
			Microphone.End(_microphoneID);
			Runnable.Stop(_recordingRoutine);
			_recordingRoutine = 0;
		}
	}

	private void OnError( string error )
	{
		Active = false;

		Log.Debug("ExampleStreaming", "Error! {0}", error);
	}

	private IEnumerator RecordingHandler()
	{
		_recording = Microphone.Start(_microphoneID, true, _recordingBufferSize, _recordingHZ);
		yield return null;      // let m_RecordingRoutine get set..

		if (_recording == null)
		{
			StopRecording();
			yield break;
		}

		bool bFirstBlock = true;
		int midPoint = _recording.samples / 2;
		float[] samples = null;

		while (_recordingRoutine != 0 && _recording != null)
		{
			int writePos = Microphone.GetPosition(_microphoneID);
			if (writePos > _recording.samples || !Microphone.IsRecording(_microphoneID))
			{
				Log.Error("MicrophoneWidget", "Microphone disconnected.");

				StopRecording();
				yield break;
			}

			if ((bFirstBlock && writePos >= midPoint)
				|| (!bFirstBlock && writePos < midPoint))
			{
				// front block is recorded, make a RecordClip and pass it onto our callback.
				samples = new float[midPoint];
				_recording.GetData(samples, bFirstBlock ? 0 : midPoint);

				AudioData record = new AudioData();
				record.MaxLevel = Mathf.Max(samples);
				record.Clip = AudioClip.Create("Recording", midPoint, _recording.channels, _recordingHZ, false);
				record.Clip.SetData(samples, 0);

				_speechToText.OnListen(record);

				bFirstBlock = !bFirstBlock;
			}
			else
			{
				// calculate the number of samples remaining until we ready for a block of audio,
				// and wait that amount of time it will take to record.
				int remaining = bFirstBlock ? (midPoint - writePos) : (_recording.samples - writePos);
				float timeRemaining = (float)remaining / (float)_recordingHZ;

				yield return new WaitForSeconds(timeRemaining);
			}

		}

		yield break;
	}

	private void InitializeServices()
	{
		Credentials credentials = new Credentials (<username>, <password>, "https://gateway.watsonplatform.net/conversation/api");
		_conversation = new Conversation(credentials);
		//be sure to give it a Version Date
		_conversation.VersionDate = "2017-05-26";

		Credentials credentials2 = new Credentials(<username>, <password>, "https://stream.watsonplatform.net/text-to-speech/api");
		_textToSpeech = new TextToSpeech(credentials2);
		//give Watson a voice type
		_textToSpeech.Voice = VoiceType.en_US_Allison;

		Credentials credentials3 = new Credentials(<username>, <password>, "https://stream.watsonplatform.net/speech-to-text/api");
		_speechToText = new SpeechToText(credentials3);
	}


	private void OnFail(RESTConnector.Error error, Dictionary<string, object> customData)
	{
		Log.Error("ExampleTextToSpeech.OnFail()", "Error received: {0}", error.ToString());
	}
}
