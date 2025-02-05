using UnityEngine;
using UnityEngine.UI;


public class SumeshButton : MonoBehaviour
{


    void Start()
    {
        Debug.Log("Start of SumeshBtn");

    }

    void Update()
    {
        Debug.Log("Update of SumeshBtn");
    }

    public void OnButtonClick()
    {
        Debug.Log("Submit Button Clicked!");

        //maybe send the response to qualtrics or change the slot parameters

        FindObjectOfType<ButtonHandler>().sendDataToQualtrics();


        GameObject.Find("Immersion prompt").SetActive(false);

    }
}


