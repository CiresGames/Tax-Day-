
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DocumentData", menuName = "Scriptable Objects/DocumentData")]
public class DocumentData : ScriptableObject
{

    public string uid;
    public List<QuestionData> questions; 

        
}
