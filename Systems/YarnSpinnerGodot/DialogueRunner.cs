using Godot;

namespace YarnSpinnerGodot
{
    public partial class DialogueRunner : Node
    {
        public bool IsDialogueRunning;
        public string CurrentNodeName;

        public void StartDialogue(string node)
        {
        }

        public void Stop()
        {
        }

        public event System.Action OnDialogueComplete;
        public event System.Action<Line> OnLineDeliveryComplete;
    }
}
