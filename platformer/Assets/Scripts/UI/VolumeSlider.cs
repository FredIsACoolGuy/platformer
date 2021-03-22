using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    private Slider volSlider;

    // Start is called before the first frame update
    void Start()
    {
        volSlider = GetComponent<Slider>();    
    }

    // Update is called once per frame
    void Update()
    {
        AudioListener.volume = volSlider.value;
    }
}
