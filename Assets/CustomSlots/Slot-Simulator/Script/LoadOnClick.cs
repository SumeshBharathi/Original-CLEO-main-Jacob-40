using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CSFramework {

    public class LoadOnClick : MonoBehaviour
    {
        public InputField idInputField;
        public InputField sessionInputField;

        public void LoadScene(int level)
        {
            GameInfo.idString = idInputField.text;
            GameInfo.sessionIdString = sessionInputField.text;
            SceneManager.LoadScene(level);
        }
    }
}