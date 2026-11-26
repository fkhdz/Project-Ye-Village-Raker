using UnityEngine;

[CreateAssetMenu(fileName = "DialogueSequence", menuName = "Ye Village/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    public string sequenceId;
    public DialogueLine[] lines;
}
