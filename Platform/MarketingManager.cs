using Godot;
using System;
using System.Collections.Generic;

namespace TheSignal.Platform;

/// <summary>
/// E5: Marketing & Launch — press kit generator, trailer storyboard,
/// launch checklist, community roadmap.
/// </summary>
[GlobalClass]
public partial class MarketingManager : Node
{
    public static MarketingManager Instance { get; private set; }

    private LaunchState _launchState = LaunchState.PreProduction;
    private Dictionary<string, bool> _launchChecklist = new();
    private List<RoadmapItem> _roadmap = new();

    public event Action<LaunchState> OnLaunchStateChanged;
    public event Action<string> OnChecklistItemCompleted;
    public event Action<string> OnMilestoneReached;

    public override void _Ready()
    {
        Instance = this;
        InitializeChecklist();
        InitializeRoadmap();
    }

    private void InitializeChecklist()
    {
        string[] items = {
            "T-90: Finalize game title and logo",
            "T-90: Create Steam/EGS/GOG store pages",
            "T-60: Record gameplay trailer (60s)",
            "T-60: Write press release + fact sheet",
            "T-60: Build press kit (screenshots, logo, banners)",
            "T-45: Apply to Steam Next Fest",
            "T-45: Send demo to 50+ content creators",
            "T-30: Final QA pass — all platforms",
            "T-30: Achieve certification (Xbox/PS5/Switch)",
            "T-30: Set up Discord server + community guidelines",
            "T-21: Finalize pricing ($19.99 / €19.99)",
            "T-21: Prepare day-1 patch (v1.0.1)",
            "T-14: Send review codes (press + influencers)",
            "T-14: Pre-load available on all storefronts",
            "T-7: Launch stream scheduled + co-stream enabled",
            "T-3: Final build approved + locked",
            "T-1: Global launch \u2014 all platforms simultaneous",
            "T+1: Monitor server load + crash reports",
            "T+7: Post-launch hotfix (critical issues only)",
            "T+30: v1.1 content update (Act II preview)"
        };

        foreach (var item in items)
            _launchChecklist[item] = false;
    }

    private void InitializeRoadmap()
    {
        _roadmap = new List<RoadmapItem>
        {
            new() { Phase = "Pre-Production", Status = "Complete",
                Items = new List<string> {
                    "Core systems (17/17 complete)",
                    "Act I main quest (12 beats, 5 endings)",
                    "15 sector zones",
                    "12 companions with dialogue arcs",
                    "35 side quests (20 repeatable, 15 unique)",
                    "Base building (3 hubs, 15 buildings, 7 research nodes)",
                    "Nemesis system, NG+, Co-op (D1-D5 complete)"
                }},
            new() { Phase = "Alpha", Status = "In Development",
                Items = new List<string> {
                    "Steam/EGS/GOG store pages",
                    "Gameplay trailer (60s)",
                    "Press kit + fact sheet",
                    "Content creator demo build",
                    "Discord community setup",
                    "Steam Next Fest application"
                }},
            new() { Phase = "Beta", Status = "Planned",
                Items = new List<string> {
                    "Closed beta (5,000 players)",
                    "Bug tracker + crash reporting",
                    "Balance tuning pass",
                    "Localization review (9 locales)",
                    "Controller testing (all platforms)",
                    "Console certification submissions"
                }},
            new() { Phase = "Launch", Status = "Planned",
                Items = new List<string> {
                    "Global release (PC + console)",
                    "Day-1 patch (v1.0.1)",
                    "Launch stream + co-stream event",
                    "Review embargo lift",
                    "Marketing blitz (social, ads, PR)"
                }},
            new() { Phase = "Post-Launch v1.1", Status = "Planned",
                Items = new List<string> {
                    "Act II: The Truth Beneath (Sectors 4\u20130)",
                    "8 new zones",
                    "5 new companions",
                    "Act II main quest (14 beats, 5 endings)",
                    "New Game+ expansion (prestige 11\u201320)",
                    "Hard mode + Ironman mode"
                }},
            new() { Phase = "Post-Launch v1.2", Status = "Planned",
                Items = new List<string> {
                    "Faction War campaigns (3 faction storylines)",
                    "Base building expansion (space station hub)",
                    "Co-op raid bosses",
                    "Modding support (Steam Workshop)",
                    "Speedrun mode + leaderboards"
                }},
            new() { Phase = "Post-Launch v2.0", Status = "Future",
                Items = new List<string> {
                    "Act III: The Cure (Sector 0 finale)",
                    "True ending + epilogue",
                    "New game mode: Survival (roguelite)",
                    "Player housing customization",
                    "VR mode (PSVR2 / SteamVR)"
                }}
        };
    }

    // ========== PRESS KIT ==========

    public PressKit GeneratePressKit()
    {
        return new PressKit
        {
            Title = "The Signal",
            Tagline = "The last AI broadcasts the Cure. The Rust consumes the world. You are the Walker.",
            Description = "A post-apocalyptic sci-fantasy turn-based tactical RPG. Recruit companions, explore corrupted sectors, and follow the Signal to find the truth.",
            Genre = "Turn-Based Tactical RPG",
            Platforms = "PC (Steam, EGS, GOG), Xbox Series X|S, PlayStation 5, Nintendo Switch",
            ReleaseDate = "Q4 2026 (Target)",
            Price = "$19.99 / \u20ac19.99",
            Developer = "ViralSaaS Studio",
            Publisher = "ViralSaaS Studio (Self-Published)",
            Engine = "Godot 4.3 (.NET 8 / C#)",
            Features = new List<string>
            {
                "XCOM-style tactical combat with cover, overwatch, flanking",
                "12 recruitable companions with loyalty arcs and synergy combos",
                "15 corrupted sectors to explore with dynamic faction war",
                "Classless Signal Node skill tree (5 branches + mutations)",
                "Base building: upgrade hubs, research, passive bonuses",
                "Nemesis system: enemies remember, rank up, hunt you",
                "New Game+ with 10 prestige levels and exclusive content",
                "2-player drop-in/drop-out co-op with shared progression",
                "35+ side quests + 12-story-beat main quest with 5 endings",
                "9 fully translated locales (EN/ES/FR/DE/JA/ZH/KO/PT/RU)",
                "Yarn Spinner branching dialogue with companion reactivity",
                "Dual-axis corruption system with narrative consequences"
            },
            Screenshots = new List<string>
            {
                "screenshot_tactical_combat_01.png",
                "screenshot_sector_map_02.png",
                "screenshot_dialogue_03.png",
                "screenshot_base_building_04.png",
                "screenshot_nemesis_encounter_05.png",
                "screenshot_coop_06.png"
            },
            Logos = new List<string>
            {
                "logo_the_signal_full.png",
                "logo_the_signal_icon.png",
                "logo_viralsaas_studio.png"
            },
            Team = new List<TeamMember>
            {
                new() { Name = "Ahmad", Role = "Creative Director / Lead Developer", Bio = "Founder of ViralSaaS Studio. Architect of The Signal's systems, narrative, and design." },
                new() { Name = "ORACLE", Role = "In-Universe AI Narrator", Bio = "The last functioning AI. Broadcasts the Cure. May have its own agenda." }
            },
            Quotes = new List<ReviewQuote>
            {
                new() { Source = "Preview Build", Text = "\"The Signal combines XCOM combat depth with a haunting post-apocalyptic world. The corruption system alone is worth the price of admission.\"" },
                new() { Source = "Developer Diary", Text = "\"Every companion has a full loyalty arc. The choices you make in Act I echo through all 12 story beats.\"" }
            },
            Contact = new PressContact
            {
                Email = "press@viralsaas.studio",
                Website = "https://thesignal.game",
                Discord = "https://discord.gg/thesignal",
                Twitter = "@TheSignalGame"
            }
        };
    }

    // ========== TRAILER STORYBOARD ==========

    public TrailerStoryboard GetLaunchTrailerStoryboard()
    {
        return new TrailerStoryboard
        {
            Title = "The Signal — Official Launch Trailer",
            Duration = "60 seconds",
            Scenes = new List<TrailerScene>
            {
                new() { Time = "0:00-0:05", Visual = "Black screen. Static crackle. A voice whispers: 'Walker...'", Audio = "Low hum, static interference", Text = "Five words: THE SIGNAL" },
                new() { Time = "0:05-0:12", Visual = "Montage: ruined city, Rust creeping up walls, corrupted creatures", Audio = "Music swells — melancholic orchestra", Text = "" },
                new() { Time = "0:12-0:20", Visual = "Tactical combat: cover system, overwatch, flanking maneuver", Audio = "Gunfire, ability SFX, tactical beeps", Text = "XCOM-STYLE TACTICAL COMBAT" },
                new() { Time = "0:20-0:28", Visual = "Companion introductions: Kael, Mara, Vex, Echo, Sloane, Jinx", Audio = "Each says one line of dialogue", Text = "12 RECRUITABLE COMPANIONS" },
                new() { Time = "0:28-0:35", Visual = "Sector map travel: node graph, corruption spread, faction borders shifting", Audio = "Map sounds, wind, distant Signal", Text = "15 CORRUPTED SECTORS" },
                new() { Time = "0:35-0:42", Visual = "Base building: upgrading Waystation, research tree, companion quarters", Audio = "Construction, crafting sounds", Text = "BUILD. RESEARCH. SURVIVE." },
                new() { Time = "0:42-0:50", Visual = "Nemesis encounter: named enemy appears, taunts player, unique abilities fire", Audio = "Boss music, dramatic impact", Text = "ENEMIES THAT REMEMBER" },
                new() { Time = "0:50-0:55", Visual = "Co-op split: two Walkers flanking a Rust Behemoth together", Audio = "Co-op callouts, synchronized attack", Text = "2-PLAYER CO-OP" },
                new() { Time = "0:55-0:60", Visual = "ORACLE speaks: 'The Cure is waiting. Reach me.' Screen cuts to black.", Audio = "ORACLE's voice, fading static", Text = "COMING Q4 2026" },
                new() { Time = "0:60", Visual = "Logo + storefront logos (Steam, EGS, GOG, Xbox, PS5, Switch)", Audio = "Final orchestral hit", Text = "WISHLIST NOW" }
            }
        };
    }

    // ========== LAUNCH CHECKLIST ==========

    public bool IsChecklistItemComplete(string item)
    {
        return _launchChecklist.GetValueOrDefault(item, false);
    }

    public void CompleteChecklistItem(string item)
    {
        if (_launchChecklist.ContainsKey(item) && !_launchChecklist[item])
        {
            _launchChecklist[item] = true;
            OnChecklistItemCompleted?.Invoke(item);
            GD.Print($"[Launch] Checklist complete: {item}");
        }
    }

    public float GetChecklistProgress()
    {
        if (_launchChecklist.Count == 0) return 0f;
        return (float)_launchChecklist.Count(kvp => kvp.Value) / _launchChecklist.Count;
    }

    public List<string> GetPendingChecklistItems()
    {
        return _launchChecklist.Where(kvp => !kvp.Value).Select(kvp => kvp.Key).ToList();
    }

    public List<string> GetCompletedChecklistItems()
    {
        return _launchChecklist.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
    }

    // ========== ROADMAP ==========

    public List<RoadmapItem> GetRoadmap() => _roadmap;
    public string GetCurrentPhase()
    {
        foreach (var phase in _roadmap)
        {
            if (phase.Status == "In Development") return phase.Phase;
        }
        return "Pre-Production";
    }

    public void SetPhaseComplete(string phaseName)
    {
        var phase = _roadmap.Find(p => p.Phase == phaseName);
        if (phase != null)
        {
            phase.Status = "Complete";
            OnMilestoneReached?.Invoke(phaseName);
            GD.Print($"[Roadmap] Phase complete: {phaseName}");
        }
    }

    // ========== LAUNCH STATE ==========

    public void SetLaunchState(LaunchState state)
    {
        _launchState = state;
        OnLaunchStateChanged?.Invoke(state);
        GD.Print($"[Launch] State: {state}");
    }

    public LaunchState GetLaunchState() => _launchState;

    public string GetEstimatedLaunchDate()
    {
        return "Q4 2026";
    }

    public string GetPricingTier()
    {
        return "$19.99 / \u20ac19.99 (Standard Edition)";
    }

    public string[] GetStorefronts()
    {
        return new[] { "Steam", "Epic Games Store", "GOG.com" };
    }

    public string[] GetConsolePlatforms()
    {
        return new[] { "Xbox Series X|S", "PlayStation 5", "Nintendo Switch" };
    }
}

// ========== DATA CLASSES ==========

public enum LaunchState { PreProduction, Alpha, Beta, LaunchReady, Shipped, PostLaunch }

public class PressKit
{
    public string Title { get; set; } = "";
    public string Tagline { get; set; } = "";
    public string Description { get; set; } = "";
    public string Genre { get; set; } = "";
    public string Platforms { get; set; } = "";
    public string ReleaseDate { get; set; } = "TBD";
    public string Price { get; set; } = "TBD";
    public string Developer { get; set; } = "";
    public string Publisher { get; set; } = "";
    public string Engine { get; set; } = "";
    public List<string> Features { get; set; } = new();
    public List<string> Screenshots { get; set; } = new();
    public List<string> Logos { get; set; } = new();
    public List<TeamMember> Team { get; set; } = new();
    public List<ReviewQuote> Quotes { get; set; } = new();
    public PressContact Contact { get; set; } = new();
}

public class TeamMember
{
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    public string Bio { get; set; } = "";
}

public class ReviewQuote
{
    public string Source { get; set; } = "";
    public string Text { get; set; } = "";
}

public class PressContact
{
    public string Email { get; set; } = "";
    public string Website { get; set; } = "";
    public string Discord { get; set; } = "";
    public string Twitter { get; set; } = "";
}

public class TrailerStoryboard
{
    public string Title { get; set; } = "";
    public string Duration { get; set; } = "";
    public List<TrailerScene> Scenes { get; set; } = new();
}

public class TrailerScene
{
    public string Time { get; set; } = "";
    public string Visual { get; set; } = "";
    public string Audio { get; set; } = "";
    public string Text { get; set; } = "";
}

public class RoadmapItem
{
    public string Phase { get; set; } = "";
    public string Status { get; set; } = "Planned";
    public List<string> Items { get; set; } = new();
}