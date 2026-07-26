using Godot;
using System.Collections.Generic;

namespace TheSignal.Systems;

public partial class AudioManager : Node
{
    public static AudioManager Instance { get; private set; }

    private AudioStreamPlayer _musicPlayer;
    private AudioStreamPlayer _sfxPlayer;
    private AudioStreamPlayer _ambientPlayer;
    private Dictionary<string, AudioStream> _musicTracks = new();
    private Dictionary<string, AudioStream> _sfxClips = new();

    public override void _Ready()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;

        _musicPlayer = new AudioStreamPlayer { Bus = "Music" };
        _sfxPlayer = new AudioStreamPlayer { Bus = "SFX" };
        _ambientPlayer = new AudioStreamPlayer { Bus = "Ambient" };

        AddChild(_musicPlayer);
        AddChild(_sfxPlayer);
        AddChild(_ambientPlayer);

        LoadAudioDatabase();
    }

    private void LoadAudioDatabase()
    {
        // Music tracks
        _musicTracks["main_theme"] = GD.Load<AudioStream>("res://Assets/Audio/Music/main_theme.ogg");
        _musicTracks["combat_theme"] = GD.Load<AudioStream>("res://Assets/Audio/Music/combat_theme.ogg");
        _musicTracks["exploration"] = GD.Load<AudioStream>("res://Assets/Audio/Music/exploration.ogg");
        _musicTracks["hub_waystation"] = GD.Load<AudioStream>("res://Assets/Audio/Music/hub_waystation.ogg");
        _musicTracks["hub_grove"] = GD.Load<AudioStream>("res://Assets/Audio/Music/hub_grove.ogg");
        _musicTracks["hub_freeport"] = GD.Load<AudioStream>("res://Assets/Audio/Music/hub_freeport.ogg");
        _musicTracks["sector_map"] = GD.Load<AudioStream>("res://Assets/Audio/Music/sector_map_theme.ogg");
        _musicTracks["dialogue"] = GD.Load<AudioStream>("res://Assets/Audio/Music/dialogue_underscore.ogg");

        // SFX
        _sfxClips["footstep_metal"] = GD.Load<AudioStream>("res://Assets/Audio/SFX/footstep_metal.ogg");
        _sfxClips["footstep_dirt"] = GD.Load<AudioStream>("res://Assets/Audio/SFX/footstep_dirt.ogg");
        _sfxClips["weapon_swing"] = GD.Load<AudioStream>("res://Assets/Audio/SFX/weapon_swing.ogg");
        _sfxClips["weapon_impact"] = GD.Load<AudioStream>("res://Assets/Audio/SFX/weapon_impact.ogg");
        _sfxClips["gunshot"] = GD.Load<AudioStream>("res://Assets/Audio/SFX/gunshot.ogg");
        _sfxClips["resonance_pulse"] = GD.Load<AudioStream>("res://Assets/Audio/SFX/resonance_pulse.ogg");
        _sfxClips["ui_click"] = GD.Load<AudioStream>("res://Assets/Audio/SFX/ui_click.ogg");
        _sfxClips["ui_hover"] = GD.Load<AudioStream>("res://Assets/Audio/SFX/ui_hover.ogg");
        _sfxClips["level_up"] = GD.Load<AudioStream>("res://Assets/Audio/SFX/level_up.ogg");
        _sfxClips["quest_complete"] = GD.Load<AudioStream>("res://Assets/Audio/SFX/quest_complete.ogg");
        _sfxClips["item_pickup"] = GD.Load<AudioStream>("res://Assets/Audio/SFX/item_pickup.ogg");
        _sfxClips["companion_join"] = GD.Load<AudioStream>("res://Assets/Audio/SFX/companion_join.ogg");
        _sfxClips["mutation_gain"] = GD.Load<AudioStream>("res://Assets/Audio/SFX/mutation_gain.ogg");
        _sfxClips["corruption_tick"] = GD.Load<AudioStream>("res://Assets/Audio/SFX/corruption_tick.ogg");
        _sfxClips["save_game"] = GD.Load<AudioStream>("res://Assets/Audio/SFX/save_game.ogg");
        _sfxClips["load_game"] = GD.Load<AudioStream>("res://Assets/Audio/SFX/load_game.ogg");
    }

    public void PlayMusic(string trackName, float fadeTime = 1.0f)
    {
        if (!_musicTracks.TryGetValue(trackName, out var stream)) return;

        if (_musicPlayer.Playing && _musicPlayer.Stream == stream) return;

        var tween = CreateTween();
        if (_musicPlayer.Playing)
        {
            tween.TweenProperty(_musicPlayer, "volume_db", -80f, fadeTime * 0.5f);
            tween.TweenCallback(Callable.From(() => _musicPlayer.Stream = stream));
            tween.TweenProperty(_musicPlayer, "volume_db", 0f, fadeTime * 0.5f);
        }
        else
        {
            _musicPlayer.Stream = stream;
            _musicPlayer.VolumeDb = -80f;
            _musicPlayer.Play();
            tween.TweenProperty(_musicPlayer, "volume_db", 0f, fadeTime);
        }
    }

    public void StopMusic(float fadeTime = 1.0f)
    {
        if (!_musicPlayer.Playing) return;

        var tween = CreateTween();
        tween.TweenProperty(_musicPlayer, "volume_db", -80f, fadeTime);
        tween.TweenCallback(Callable.From(() => _musicPlayer.Stop()));
    }

    public void PlaySfx(string sfxName, float volumeDb = 0f, float pitchScale = 1f)
    {
        if (!_sfxClips.TryGetValue(sfxName, out var stream)) return;

        var player = new AudioStreamPlayer { Bus = "SFX", Stream = stream, VolumeDb = volumeDb, PitchScale = pitchScale };
        AddChild(player);
        player.Play();
        player.Finished += () => player.QueueFree();
    }

    public void PlaySfxAtPosition(string sfxName, Vector3 position, float volumeDb = 0f)
    {
        if (!_sfxClips.TryGetValue(sfxName, out var stream)) return;

        var player = new AudioStreamPlayer3D { Stream = stream, VolumeDb = volumeDb };
        player.GlobalPosition = position;
        AddChild(player);
        player.Play();
        player.Finished += () => player.QueueFree();
    }

    public void SetAmbient(string ambientName, float volumeDb = -10f)
    {
        if (!_musicTracks.TryGetValue(ambientName, out var stream)) return;

        _ambientPlayer.Stream = stream;
        _ambientPlayer.VolumeDb = volumeDb;
        _ambientPlayer.Play();
    }

    public void StopAmbient(float fadeTime = 2f)
    {
        if (!_ambientPlayer.Playing) return;

        var tween = CreateTween();
        tween.TweenProperty(_ambientPlayer, "volume_db", -80f, fadeTime);
        tween.TweenCallback(Callable.From(() => _ambientPlayer.Stop()));
    }

    public void SetMusicVolume(float volumeDb) => _musicPlayer.VolumeDb = volumeDb;
    public void SetSfxVolume(float volumeDb) => _sfxPlayer.VolumeDb = volumeDb;
    public void SetAmbientVolume(float volumeDb) => _ambientPlayer.VolumeDb = volumeDb;
}