using Godot;
using YarnSpinnerGodot;
using TheSignal.Core;

namespace TheSignal.Systems;

public partial class DialogueManager : Node
{
    public static DialogueManager Instance { get; private set; }

    public DialogueRunner Runner { get; private set; }
    public VariableStorage VarStorage { get; private set; }
    public LineProvider LineProvider { get; private set; }

    public event Action<string> OnDialogueStarted;
    public event Action<string> OnDialogueEnded;
    public event Action<string, string[]> OnChoicesPresented;
    public event Action<string> OnLineDelivered;

    public override void _Ready()
    {
        Instance = this;
        Runner = GetNode<DialogueRunner>("DialogueRunner");
        VarStorage = GetNode<VariableStorage>("VariableStorage");
        LineProvider = GetNode<LineProvider>("LineProvider");

        Runner.onDialogueStart += OnDialogueStart;
        Runner.onDialogueComplete += OnDialogueComplete;
        Runner.onNodeStart += OnNodeStart;
        Runner.onNodeComplete += OnNodeComplete;
    }

    private void OnDialogueStart()
    {
        OnDialogueStarted?.Invoke(Runner.CurrentNodeName);
    }

    private void OnDialogueComplete()
    {
        GameManager.Instance.ReturnToPreviousState();
        OnDialogueEnded?.Invoke(Runner.CurrentNodeName);
    }

    private void OnNodeStart(string nodeName)
    {
        OnDialogueStarted?.Invoke(nodeName);
    }

    private void OnNodeComplete(string nodeName)
    {
        OnDialogueEnded?.Invoke(nodeName);
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

    public void SetVariable(string name, Variant value)
    {
        VarStorage.SetValue($"${name}", value);
    }

    public Variant GetVariable(string name)
    {
        return VarStorage.GetValue($"${name}");
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
        return lineId;
    }
}
