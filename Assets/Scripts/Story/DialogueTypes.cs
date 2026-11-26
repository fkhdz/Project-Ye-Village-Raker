using UnityEngine;

public enum DialogueSpeaker
{
    KepalaDesa,
    TetuaDesa
}

[System.Serializable]
public struct DialogueLine
{
    public DialogueSpeaker speaker;
    [TextArea(2, 8)] public string text;
}
