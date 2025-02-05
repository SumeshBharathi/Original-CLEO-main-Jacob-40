using UnityEngine;
using UnityEngine.UI;

public class ButtonHandler : MonoBehaviour
{

    [SerializeField] public Slider slider;

    public float sliderValue;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Immersion Prompt Slider Initialized");
        // script for the slider
        slider.onValueChanged.AddListener((value) =>
        {
            Debug.Log("data");
            Debug.Log(value);
            sliderValue=value;
            OnSliderValueChanged(value);
        }
        );

    }


    public void OnSliderValueChanged(float value)
    {
        sliderValue = value;
        Debug.Log("slider value is " + value);
    }

    public void sendDataToQualtrics()
    {
        Debug.Log(sliderValue);
    }

}
