using Godot;
using YarnSpinnerGodot;
using TheSignal.Core;

namespace TheSignal.Systems;

public partial class DialogueManager : Node
{
    public static DialogueManager Instance { get; private set; }

    public DialogueRunner Runner { get; private set; }
    public VariableStorage VariableStorage { get; private set; }
    public LineProvider LineProvider { get; private set; }

    public event Action<string> OnDialogueStarted;
    public event Action<string> OnDialogueEnded;
    public event Action<string, string[]> OnChoicesPresented;
    public event Action<string> OnLineDelivered;

    public override void _Ready()
    {
        Instance = this;
        Runner = GetNode<DialogueRunner>("DialogueRunner");
        VariableStorage = GetNode<VariableStorage>("VariableStorage");
        LineProvider = GetNode<LineProvider>("LineProvider");

        Runner.OnDialogueComplete += OnDialogueComplete;
        Runner.OnLineDeliveryComplete += OnLineDeliveryComplete;
    }

    public void StartDialogue(string yarnNode)
    {
        if (Runner.IsDialogueRunning) return;

        GameManager.Instance.ChangeState(GameState.Dialogue);
        Runner.StartDialogue(yarnNode);
        OnDialogueStarted?.Invoke(yarnNode);
    }

    public void StopDialogue()
    {
        Runner.Stop();
        GameManager.Instance.ReturnToPreviousState();
        OnDialogueEnded?.Invoke("");
    }

    private void OnDialogueComplete()
    {
        GameManager.Instance.ReturnToPreviousState();
        OnDialogueEnded?.Invoke(Runner.CurrentNodeName);
    }

    private void OnLineDeliveryComplete(Line line)
    {
        OnLineDelivered?.Invoke(line.Text);
    }

    public void SetVariable(string name, Variant value)
    {
        VariableStorage.SetValue($"${name}", value);
    }

    public Variant GetVariable(string name)
    {
        return VariableStorage.GetValue($"${name}");
    }

    public void AddChoice(string text, string targetNode, string condition = "")
    {
        // Choices are handled by Yarn scripts
    }
}

[GlobalClass]
public partial class VariableStorage : InMemoryVariableStorage
{
    public void SetGameVariable(string key, Variant value)
    {
        SetValue($"${key}", value);
    }

    public Variant GetGameVariable(string key)
    {
        return GetValue($"${key}");
    }
}

[GlobalClass]
public partial class LineProvider : Node
{
    [Export] public string LocaleCode { get; set; } = "en";

    public string GetLocalizedLine(string lineId)
    {
        // In production, load from CSV/JSON string tables
        return lineId;
    }
}