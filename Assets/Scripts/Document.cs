// Document.cs
using System.Collections.Generic;
using UnityEngine;

public class Document : MonoBehaviour
{
    [SerializeField] DocumentData _documentData;
    [SerializeField] List<Answer> answers;

    private void Start()
    {
        Initialize(answers);
    }

    public virtual void Initialize(List<Answer> answers)
    {
        if (_documentData == null)
        {
            Debug.LogError("DocumentData is not assigned.", this);
            return;
        }

        if (_documentData.answers.Count != answers.Count)
        {
            Debug.LogError($"Answer count mismatch: {answers.Count} fields but {_documentData.answers.Count} data entries.", this);
            return;
        }

        for (int i = 0; i < answers.Count; i++)
        {
            answers[i]._answerData = _documentData.answers[i];

            if (answers[i] is MultipleChoiceController mc)
                mc.InitializeChoices();

            answers[i].LoadAnswer();
        }
    }
}