// Document.cs
using System.Collections.Generic;
using UnityEngine;

public class Document : MonoBehaviour
{
    [SerializeField] DocumentData _documentData;
    [SerializeField] List<Question> answers;

    private void Start()
    {
        Initialize(answers);
    }

    public virtual void Initialize(List<Question> answers)
    {
        if (_documentData == null)
        {
            Debug.LogError("DocumentData is not assigned.", this);
            return;
        }

        if (_documentData.questions.Count != answers.Count)
        {
            Debug.LogError($"Answer count mismatch: {answers.Count} fields but {_documentData.questions.Count} data entries.", this);
            return;
        }

        for (int i = 0; i < answers.Count; i++)
        {
            answers[i]._questionData = _documentData.questions[i];
            answers[i].SetQuestion(); 

            if (answers[i] is MultipleChoiceController mc)
                mc.InitializeChoices();

            answers[i].LoadAnswer();
        }
    }
}