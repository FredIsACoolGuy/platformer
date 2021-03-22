using Platformer.Mechanics;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecretZone : MonoBehaviour
{
    bool _found;

    void OnTriggerEnter2D(Collider2D collider) {
        var p = collider.gameObject.GetComponent<PlayerController>();
        if (p != null && !_found) {
            Counters.Increment(Counters.CounterType.SecretFind);
            _found = true;
        }
    }
}
