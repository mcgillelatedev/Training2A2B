using System.Collections.Generic;

[System.Serializable]
public class InstructionGroup
{
    public string groupName;
    public bool isEnabled = true;
    public List<InstructionStep> steps = new List<InstructionStep>();
}
