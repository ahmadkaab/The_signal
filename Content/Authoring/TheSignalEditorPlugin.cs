using Godot;
using Godot.Collections;
using TheSignal.Data;
using System.Collections.Generic;

namespace TheSignal.Content.Authoring;

[Tool]
public partial class TheSignalEditorPlugin : EditorPlugin
{
    public override void _EnterTree()
    {
        AddCustomType("SignalNodeResource", "Resource",
            GD.Load<Script>("res://Data/EditorResources.cs"),
            GD.Load<Texture2D>("res://Assets/Art/Icons/app_icon.svg"));

        AddCustomType("MutationResource", "Resource",
            GD.Load<Script>("res://Data/EditorResources.cs"),
            GD.Load<Texture2D>("res://Assets/Art/Icons/app_icon.svg"));

        AddCustomType("CompanionSynergyResource", "Resource",
            GD.Load<Script>("res://Data/EditorResources.cs"),
            GD.Load<Texture2D>("res://Assets/Art/Icons/app_icon.svg"));

        AddCustomType("ZoneEventResource", "Resource",
            GD.Load<Script>("res://Data/EditorResources.cs"),
            GD.Load<Texture2D>("res://Assets/Art/Icons/app_icon.svg"));
    }

    public override void _ExitTree()
    {
        RemoveCustomType("SignalNodeResource");
        RemoveCustomType("MutationResource");
        RemoveCustomType("CompanionSynergyResource");
        RemoveCustomType("ZoneEventResource");
    }

    public override string _GetPluginName() => "The Signal Editor Tools";
}

[Tool]
public partial class SignalNodeEditorPlugin : EditorInspectorPlugin
{
    public override bool _CanHandle(GodotObject @object) => @object is SignalNodeResource;

    public override void _ParseBegin(GodotObject @object)
    {
        var node = @object as SignalNodeResource;
        if (node == null) return;
        AddCustomControl(new SignalNodePreview(node));
    }
}

[Tool]
public partial class SignalNodePreview : Control
{
    private SignalNodeResource _node;
    public SignalNodePreview(SignalNodeResource node) { _node = node; }
    public override void _Ready()
    {
        AddChild(new Label { Text = $"Node: {_node.DisplayName}\nTier: {_node.Tier}\nCost: {_node.Cost} Signal Points" });
    }
}
