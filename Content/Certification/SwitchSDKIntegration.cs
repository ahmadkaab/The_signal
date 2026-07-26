using Godot;
using System;

namespace TheSignal.Content.Certification;

/// <summary>
/// Nintendo Switch SDK integration stub. Requires NVN/NX SDK.
/// Handles: save data, touch input, sleep/resume, performance profiles.
/// </summary>
[Tool]
public partial class SwitchSDKIntegration : Node
{
    public static SwitchSDKIntegration Instance { get; private set; }

    public bool IsInitialized { get; private set; } = false;
    public bool IsHandheldMode { get; private set; } = true;
    public bool IsDockedMode => !IsHandheldMode;

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public void Initialize()
    {
        GD.Print("[Switch SDK] Initializing...");
        // nvInit();
        IsInitialized = true;
        DetectMode();
        GD.Print($"[Switch SDK] Initialized. Mode: {(IsHandheldMode ? "Handheld" : "Docked")}");
    }

    private void DetectMode()
    {
        // appletGetOperationMode()
        IsHandheldMode = true; // placeholder
    }

    public void OnSleepResume()
    {
        GD.Print("[Switch SDK] Sleep/resume detected — restoring game state");
        // SaveManager.Instance?.QuickSave();
    }

    public void HandleTouchInput(Vector2 touchPos, bool pressed)
    {
        if (pressed)
        {
            GD.Print($"[Switch SDK] Touch at: {touchPos}");
        }
    }

    public void SetHandheldProfile()
    {
        IsHandheldMode = true;
        GD.Print("[Switch SDK] Applying handheld performance profile");
        // PerformanceManager.Instance?.ApplyProfile("Mobile");
    }

    public void SetDockedProfile()
    {
        IsHandheldMode = false;
        GD.Print("[Switch SDK] Applying docked performance profile");
        // PerformanceManager.Instance?.ApplyProfile("Console");
    }

    public bool ValidateSaveSize(long byteSize)
    {
        // Switch save limit is 128MB
        const long maxBytes = 128L * 1024 * 1024;
        if (byteSize > maxBytes)
        {
            GD.PrintErr($"[Switch SDK] Save too large: {byteSize / 1024 / 1024}MB > 128MB");
            return false;
        }
        return true;
    }

    public void ShowSoftwareKeyboard(string placeholder, Action<string> callback)
    {
        GD.Print($"[Switch SDK] Showing keyboard (placeholder: {placeholder})");
        // swkbdShow()
        callback?.Invoke("PlayerName");
    }
}