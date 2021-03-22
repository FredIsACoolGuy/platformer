using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class Counters : MonoBehaviour
{
    public enum CounterType {
        TokenCollect,
        EnemyKill,
        Death,
        SecretFind
    }

    [System.Serializable]
    public struct Countable {
        [HideInInspector]
        public string name;
        public CounterType type;
        public TextMeshProUGUI readout;
        public int count;
    }

    static Counters _instance;

    public TextMeshProUGUI timeReadout;

    [SerializeField]
    private Countable[] _counters;

    float _startTime;
    float _endTime;

    public float time { get { return Mathf.Min(Time.time, _endTime) - _startTime; } }

    private void Awake() {
        _instance = this;
        _startTime = Time.time;
        _endTime = float.PositiveInfinity;
    }

    private void OnValidate() {
        if (_counters == null) return;

        for (int i = 0; i < _counters.Length; i++) {
            var counter = _counters[i];
            counter.name = counter.type.ToString();
            _counters[i] = counter;
        }
    }

    public void IncrementInstance(CounterType type) {
        int index = System.Array.FindIndex(_counters, c => c.type == type);
        if (index >= 0) {
            var counter = _counters[index];
            counter.count++;
            counter.readout.text = counter.count.ToString();
            _counters[index] = counter;
        }
    }

    public static void Increment(CounterType type) {
        if (_instance != null)
            _instance.IncrementInstance(type);
    }

    private void Update() {
        var span = System.TimeSpan.FromSeconds(time);
        timeReadout.text = span.ToString(@"mm\:ss");
    }

    public static int GetCurrentCount(CounterType type) {
        int index = System.Array.FindIndex(_instance._counters, c => c.type == type);
        if (index >= 0) {
            return _instance._counters[index].count;
        }

        return -1;
    }

    public static void StopTimer() {
        _instance._endTime = Time.time;
    }

    public static float GetTime() { return _instance.time; }
}
