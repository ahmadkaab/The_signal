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

    public string CurrentNodeName { get; private set; } = string.Empty;

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
        OnDialogueStarted?.Invoke(CurrentNodeName);
    }

    private void OnDialogueComplete()
    {
        GameManager.Instance.ReturnToPreviousState();
        OnDialogueEnded?.Invoke(CurrentNodeName);
    }

    private void OnNodeStart(string nodeName)
    {
        CurrentNodeName = nodeName;
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
        CurrentNodeName = yarnNode;
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
        // InMemoryVariableStorage.SetValue accepts string/float/bool, not Variant
        string varName = $"${name}";
        switch (value.VariantType)
        {
            case Variant.Type.Bool:
                VarStorage.SetValue(varName, value.AsBool());
                break;
            default:
                VarStorage.SetValue(varName, value.AsString());
                break;
        }
    }

    public Variant GetVariable(string name)
    {
        return VarStorage.GetVariantValue($"${name}");
    }
}

[GlobalClass]
public partial class VariableStorage : InMemoryVariableStorage
{
    public void SetGameVariable(string key, Variant value)
    {
        string varName = $"${key}";
        switch (value.VariantType)
        {
            case Variant.Type.Bool:
                SetValue(varName, value.AsBool());
                break;
            default:
                SetValue(varName, value.AsString());
                break;
        }
    }

    public Variant GetGameVariable(string key)
    {
        return GetVariantValue($"${key}");
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
