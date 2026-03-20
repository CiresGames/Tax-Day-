using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputFieldController : MonoBehaviour
{

    [SerializeField] TMP_InputField inputField;
    [SerializeField] string key;
    [SerializeField] Image validationFeedback;
    [SerializeField] Sprite[] feedbackSprites;



    public bool CompareInput()
    {
        if (inputField == null) return false;
        return key == inputField.text.Trim();
    }


    public void UpdateUI()
    {
        if (CompareInput())
        {
            validationFeedback.sprite = feedbackSprites[0];
            validationFeedback.color = Color.green;
        }

        else
        {
            validationFeedback.sprite = feedbackSprites[1];
            validationFeedback.color = Color.red;
        }

    }


}
