using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System.Net.Http.Headers;
using System.Collections;
using System.Net.Http;
using System;
using System.Threading.Tasks;

public class ImmersionPromptScript : MonoBehaviour
{
    [SerializeField] public Slider slider;
    [SerializeField] public TextMeshProUGUI sliderText;
    [SerializeField] public GameObject spinButton;
    public int sliderValue;

    public static int immersion_counter, immersion_val_1, immersion_val_2, immersion_val_3;

    // Use this for initialization
    void Start()
    {
         immersion_counter = 0;
        immersion_val_1 = 0;
        immersion_val_2 = 0;
        immersion_val_3 = 0;

        Debug.Log("Start of IPS");
        slider.onValueChanged.AddListener((value) =>
        {
            Debug.Log((int)value);
            sliderValue = (int)value;
            sliderText.text = ((int)value).ToString("");
            
        }
        );
    }

    // Update is called once per frame
    void Update()
    {

    }


    public async void OnButtonClick()   {
        immersion_counter += 1;

        if (immersion_counter == 1){
            immersion_val_1 = sliderValue;
        }
        else if (immersion_counter == 2){
            immersion_val_2 = sliderValue;
        }
        else{
            immersion_val_3 = sliderValue;
        }


        GameObject.Find("Immersion prompt").SetActive(false);
        
        slider.value=0;


        spinButton.SetActive(true);
        
        // await sendDataAsync();


        

        
    }

    async Task sendDataAsync()
    {
        string apiKey = "e912f1f8-b73b-4da0-89b5-4c3532d969ec";
        string surveyId = "SV_9NaUj1zSobY56Bw";
        string questionId = "QID1_TEXT";

        var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var jsonContent = new StringContent(
            "{ \"values\": { \"" + questionId + "\": \"" + sliderValue + "\" } }"
        );
        jsonContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await client.PostAsync(
            $"https://yul1.qualtrics.com/API/v3/surveys/{surveyId}/responses",
            jsonContent
        );

        if (response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            Debug.Log("Qualtrics response: " + responseBody);
        }
        else
        {
            Debug.LogError("Qualtrics API request failed: " + response.StatusCode);
        }
    }

}


