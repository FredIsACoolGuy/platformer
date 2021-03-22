using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class TelemetryLogger : MonoBehaviour
{
    public static void LogSessionStart() {
        TelemetryEvent data = default;
        data.eventType = "SessionStart";
        _instance.LogTelemetry(data);
    }

    public static void LogPlayerDeath(string causeOfDeath, Vector3 position, string killer="", Vector3 killerPosition = default)
    {
        TelemetryEvent data = default;
        data.eventType = "Death";
        data.toAttribute = causeOfDeath;
        data.toPosition = position;

        data.fromAttribute = killer;
        data.fromPosition = killerPosition;

        data.numberAttribute = Counters.GetCurrentCount(Counters.CounterType.Death) + 1;

        _instance.LogTelemetry(data);
    }

    private const float MIN_TIME_BETWEEN_LOGS = 0.1f;

    private static TelemetryLogger _instance;
    private static bool _serverAwake;
    private static uint _sessionID;
    private static Queue<TelemetryEvent> _dataToSend = new Queue<TelemetryEvent>();
    private static float _lastSendTime = float.NegativeInfinity;

    public enum App {
        platformer,
		fps,
        other
    }
    public App app;
    public string levelName = "Custom";

    public string serverURL = "http://dd-telemetry.herokuapp.com/";

    public bool enableLogging = true;

    private bool EnableLogging {
        get {
#if UNITY_EDITOR
            return false;
#else
            return enableLogging;
#endif
        }
    }

    private Coroutine _logger;

    private void Awake() {
        if (_instance != null) {
            Debug.LogWarning("You have two TelemetryLoggers active at the same time. Please ensure you have only one at a time.");
            Destroy(this);
            return;
        }

        _instance = this;

        if (_serverAwake == false)
            StartCoroutine(WakeServer());       
    }

    private IEnumerator WakeServer() {
        _sessionID = (uint)(Random.Range(0, 0xFFFF) << 2) | (uint)Random.Range(0, 0xFFFF);

        LogSessionStart();

        if (EnableLogging) {

            string url = $"{serverURL}awake";
            Debug.Log(url);

            int attemptCount = 0;
            while (_serverAwake == false) {
                yield return null;
                Debug.Log($"Attempting to connect to telemetry server, attempt {++attemptCount}...");

                using (var request = UnityWebRequest.Get(url)) {
                    request.timeout = 5;

                    yield return request.SendWebRequest();

                    if (!request.isNetworkError && request.responseCode == 200) {
                        _serverAwake = true;
                    } else {
                        Debug.Log(request.responseCode + " " + request.downloadHandler?.text);
                    }
                }
            }
        } else {
            _serverAwake = true;
        }
        
        Debug.Log($"Server is awake.");
    }

    private void LogTelemetry(TelemetryEvent data) {
        data.session = _sessionID;
        data.time = System.DateTimeOffset.UtcNow;
        data.level = levelName;

        _dataToSend.Enqueue(data);

        if (_serverAwake && _logger == null) {
            _logger = StartCoroutine(LoggerCoroutine());
        }
    }

    private UnityWebRequest MakeJSONRequest<T>(T data) {
        string json = JsonUtility.ToJson(data);

        var request = UnityWebRequest.Put($"{serverURL}log-{app}", json);
        request.method = UnityWebRequest.kHttpVerbPOST;
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");
        request.timeout = 5;

        return request;
    }

    private IEnumerator LoggerCoroutine() {
        while (_dataToSend.Count > 0) {

            float sinceLastSend = Time.time - _lastSendTime;
            if (sinceLastSend < MIN_TIME_BETWEEN_LOGS) {
                if (_dataToSend.Count > 10) {
                    Debug.LogWarning($"Sending telemetry too frequently. Items in queue: {_dataToSend.Count}");
                }

                yield return new WaitForSeconds(MIN_TIME_BETWEEN_LOGS - sinceLastSend);
            }

            var data = _dataToSend.Dequeue();

            _lastSendTime = Time.time;
            if (EnableLogging) {
                using (var request = MakeJSONRequest(data)) {
                    
                    yield return request.SendWebRequest();

                    string error = null;

                    if (request.isNetworkError || request.isHttpError) {
                        if (request.downloadHandler != null)
                            error = request.downloadHandler.text;
                        if (string.IsNullOrWhiteSpace(error))
                            error = request.error;

                        Debug.LogError($"Telemetry error {request.responseCode}: {error}");
                    } else if (request.responseCode >= 200 && request.responseCode < 300) {
                        Debug.Log($"Succesfully logged event {data}");
                    }
                }
            } else {
                Debug.Log($"Simulated logging of {data}");
            }
        }

        _logger = null;
    }
}
