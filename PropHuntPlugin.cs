using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace PropHunt;

public partial class PropHuntPlugin : BasePlugin, IPluginConfig<PluginConfig>
{
    public override string ModuleName => "PropHunt";
    public override string ModuleVersion => "1.0.3";
    public override string ModuleAuthor => "PropHunt CS2";
    public override string ModuleDescription => "Prop Hunt gamemode for CS2 - Hiders disguise as props, Seekers hunt them down!";

    // ── Config ──────────────────────────────────────────────
    public PluginConfig Config { get; set; } = new();
    public string FormattedPrefix { get; set; } = string.Empty;

    public void OnConfigParsed(PluginConfig config)
    {
        Config = config;
        FormattedPrefix = StringExtensions.ReplaceColorTags(config.Prefix);
        PropHuntEnabled = config.EnabledByDefault;
    }

    // ── State ───────────────────────────────────────────────
    public bool PropHuntEnabled { get; set; } = false;
    public List<string> AvailableModels { get; set; } = new();
    public List<string> AvailableTauntSounds { get; set; } = new();
    public Dictionary<int, PlayerPropData> HiddenPlayers { get; set; } = new();
    public bool IsHidingPhase { get; set; } = false;
    public DateTime HidePhaseEndTime { get; set; } = DateTime.Now;
    public bool SeekersReleased { get; set; } = false;

    // ── Teams ───────────────────────────────────────────────
    public CsTeam HidingTeam => Utils.ParseTeam(Config.HidingTeam);
    public CsTeam SeekingTeam => Utils.OppositeTeam(HidingTeam);

    // ── Plugin Lifecycle ────────────────────────────────────
    public override void Load(bool hotReload)
    {
        Logger.LogInformation("[PropHunt] Plugin loaded.");

        // Register listeners
        RegisterListener<Listeners.OnMapStart>(OnMapStart);
        RegisterListener<Listeners.OnServerPrecacheResources>(OnServerPrecacheResources);
        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
        RegisterListener<Listeners.CheckTransmit>(OnCheckTransmit);

        // Register game events
        RegisterEventHandler<EventRoundPrestart>(OnRoundPrestart);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath, HookMode.Pre);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

        // Register prop damage hook
        HookEntityOutput("prop_dynamic", "OnTakeDamage", OnPropTakeDamage);

        // Hook sound events to hide footstep/movement sounds from hidden players
        HookUserMessage(208, OnSoundEvent, HookMode.Pre);

        // Register commands
        RegisterCommands();

        // If hot reloading, load models and taunt sounds for current map
        if (hotReload)
        {
            AvailableModels = Utils.LoadMapModels(ModuleDirectory, Server.MapName, Config.DefaultModels);
            AvailableTauntSounds = Utils.LoadTauntSounds(ModuleDirectory, Config.TauntSounds);
            Logger.LogInformation($"[PropHunt] Hot reload: loaded {AvailableModels.Count} models, {AvailableTauntSounds.Count} taunt sounds");
        }
    }

    public override void Unload(bool hotReload)
    {
        DisablePropHunt();
        Logger.LogInformation("[PropHunt] Plugin unloaded.");
    }

    /// <summary>
    /// Enable PropHunt mode.
    /// </summary>
    public void EnablePropHunt()
    {
        if (PropHuntEnabled) return;

        PropHuntEnabled = true;
        Logger.LogInformation("[PropHunt] Mode ENABLED.");
        PrintToChatAll($"{ChatColors.Green}PropHunt modu ACILDI! {ChatColors.Default}Sonraki round basladiginda aktif olacak.");
    }

    /// <summary>
    /// Disable PropHunt mode and clean up everything.
    /// </summary>
    public void DisablePropHunt()
    {
        if (!PropHuntEnabled && HiddenPlayers.Count == 0) return;

        PropHuntEnabled = false;
        IsHidingPhase = false;
        SeekersReleased = false;

        // Clean up all props
        CleanupAllProps();

        // Restore all players to normal state
        foreach (var player in Utilities.GetPlayers())
        {
            if (!player.IsValid || player.IsBot) continue;

            Utils.MakeVisible(player);
            Utils.EnableShadow(player);
            Utils.UnfreezePlayer(player);
        }

        Logger.LogInformation("[PropHunt] Mode DISABLED.");
        PrintToChatAll($"{ChatColors.Red}PropHunt modu KAPATILDI! {ChatColors.Default}Normal oyun devam ediyor.");
    }

    // ── Helper Methods ──────────────────────────────────────

    /// <summary>
    /// Print a message to all players with the plugin prefix.
    /// </summary>
    public void PrintToChatAll(string message)
    {
        Server.PrintToChatAll($" {FormattedPrefix} {ChatColors.Default}{message}");
    }

    /// <summary>
    /// Print a message to a specific player with the plugin prefix.
    /// </summary>
    public void PrintToChat(CCSPlayerController player, string message)
    {
        player.PrintToChat($" {FormattedPrefix} {ChatColors.Default}{message}");
    }

    /// <summary>
    /// Returns the display name for a key config value.
    /// "Attack" -> "Sol Tik", "Attack2" -> "Sag Tik", "Use" -> "E", "Reload" -> "R", "None" -> null
    /// </summary>
    public static string? GetKeyDisplayName(string configKey)
    {
        return configKey.ToLower() switch
        {
            "attack" => "Sol Tik",
            "attack2" => "Sag Tik",
            "use" => "E",
            "reload" => "R",
            _ => null
        };
    }

    /// <summary>
    /// Builds a dynamic key binding info string for chat from current config.
    /// Only shows bindings that are not "None".
    /// </summary>
    public string BuildKeyBindingChat()
    {
        var parts = new List<string>();

        void Add(string key, string action)
        {
            var name = GetKeyDisplayName(key);
            if (name != null)
                parts.Add($"{ChatColors.LightBlue}{name} {ChatColors.Grey}({action})");
        }

        Add(Config.KeyTaunt, "taunt");
        Add(Config.KeySwap, "swap");
        Add(Config.KeyFreeze, "don");
        Add(Config.KeyWhistle, "islik");
        Add(Config.KeyDecoy, "decoy");

        return parts.Count > 0
            ? $"{ChatColors.Grey}Tuslar: " + string.Join($" {ChatColors.Grey}| ", parts)
            : string.Empty;
    }

    /// <summary>
    /// Builds a short key binding string for center HUD display.
    /// </summary>
    public string BuildKeyBindingCenter()
    {
        var parts = new List<string>();

        void Add(string key, string action)
        {
            var name = GetKeyDisplayName(key);
            if (name != null)
                parts.Add($"{name}: {action}");
        }

        Add(Config.KeyTaunt, "Taunt");
        Add(Config.KeySwap, "Swap");
        Add(Config.KeyFreeze, "Don");
        Add(Config.KeyWhistle, "Islik");
        Add(Config.KeyDecoy, "Decoy");

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Spawn a prop for a hiding player.
    /// </summary>
    public void SpawnPropForPlayer(CCSPlayerController player, string? specificModel = null)
    {
        if (!Utils.IsValidPlayer(player) || !player.PawnIsAlive) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        // Determine model
        string model;
        if (!string.IsNullOrEmpty(specificModel))
        {
            model = specificModel;
        }
        else if (AvailableModels.Count > 0)
        {
            model = AvailableModels[Random.Shared.Next(AvailableModels.Count)];
        }
        else
        {
            return;
        }

        // Remove existing prop if any
        RemovePropForPlayer(player);

        // Create the prop entity
        var prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (prop == null) return;

        prop.SetModel(model);
        prop.DispatchSpawn();
        prop.Teleport(pawn.AbsOrigin, pawn.AbsRotation);

        // Disable collision so it doesn't block the player
        if (prop.Collision != null)
        {
            prop.Collision.CollisionGroup = 2; // COLLISION_GROUP_TRIGGER
            prop.Collision.CollisionAttribute.CollisionGroup = 2;
        }

        // Classify prop size and determine health
        var propSize = Utils.ClassifyPropSize(model);
        int propHealth = Utils.GetHealthForPropSize(propSize, Config.PropHealthSmall, Config.PropHealthMedium, Config.PropHealthLarge);

        // Create player prop data
        var propData = new PlayerPropData(Config.SwapLimit, Config.DecoyLimit, Config.WhistleLimit, Config.TauntLimit)
        {
            PropEntity = prop,
            ModelPath = model,
            Size = propSize
        };

        HiddenPlayers[player.Slot] = propData;

        // Make the actual player invisible
        Utils.MakeInvisible(player);
        Utils.DisableShadow(player);

        // Remove all weapons from hiders
        player.RemoveWeapons();

        // Set health based on prop size
        Utils.SetHealth(player, propHealth);

        // Notify player about their prop size and health
        string sizeText = propSize switch
        {
            PropSize.Small => "Kucuk",
            PropSize.Large => "Buyuk",
            _ => "Orta"
        };
        player.PrintToChat($" {Config.Prefix} Prop boyutu: {{yellow}}{sizeText}{{default}} | Can: {{green}}{propHealth}{{default}}");
    }

    /// <summary>
    /// Remove a player's prop.
    /// </summary>
    public void RemovePropForPlayer(CCSPlayerController player)
    {
        if (HiddenPlayers.TryGetValue(player.Slot, out var data))
        {
            // Remove main prop
            if (data.PropEntity != null && data.PropEntity.IsValid)
            {
                data.PropEntity.Remove();
            }

            // Remove camera prop (third person)
            if (data.CameraProp != null && data.CameraProp.IsValid)
            {
                data.CameraProp.Remove();
            }

            // Remove decoy props
            foreach (var decoy in data.DecoyProps)
            {
                if (decoy != null && decoy.IsValid)
                {
                    decoy.Remove();
                }
            }

            HiddenPlayers.Remove(player.Slot);
        }
    }

    /// <summary>
    /// Cleanup all props in the game.
    /// </summary>
    public void CleanupAllProps()
    {
        foreach (var kvp in HiddenPlayers)
        {
            var data = kvp.Value;
            if (data.PropEntity != null && data.PropEntity.IsValid)
                data.PropEntity.Remove();

            if (data.CameraProp != null && data.CameraProp.IsValid)
                data.CameraProp.Remove();

            foreach (var decoy in data.DecoyProps)
            {
                if (decoy != null && decoy.IsValid)
                    decoy.Remove();
            }
        }

        HiddenPlayers.Clear();
    }

    /// <summary>
    /// Toggle third person view for a player.
    /// </summary>
    public void ToggleThirdPerson(CCSPlayerController player)
    {
        if (!HiddenPlayers.TryGetValue(player.Slot, out var data)) return;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        if (!data.IsThirdPerson)
        {
            // Enable third person
            var cameraProp = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
            if (cameraProp == null) return;

            cameraProp.DispatchSpawn();
            cameraProp.Render = System.Drawing.Color.FromArgb(0, 255, 255, 255);
            Utilities.SetStateChanged(cameraProp, "CBaseModelEntity", "m_clrRender");

            var cameraPos = Utils.CalculatePositionInFront(player, -110, 90);
            cameraProp.Teleport(cameraPos, pawn.V_angle);

            pawn.CameraServices!.ViewEntity.Raw = cameraProp.EntityHandle.Raw;
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");

            data.CameraProp = cameraProp;
            data.IsThirdPerson = true;
        }
        else
        {
            // Disable third person
            pawn.CameraServices!.ViewEntity.Raw = uint.MaxValue;
            Utilities.SetStateChanged(pawn, "CBasePlayerPawn", "m_pCameraServices");

            if (data.CameraProp != null && data.CameraProp.IsValid)
            {
                data.CameraProp.Remove();
            }
            data.CameraProp = null;
            data.IsThirdPerson = false;
        }
    }

    /// <summary>
    /// Swap a player's prop model.
    /// </summary>
    public void SwapPropModel(CCSPlayerController player)
    {
        if (!HiddenPlayers.TryGetValue(player.Slot, out var data)) return;

        // During hide phase, unlimited swaps
        if (!IsHidingPhase)
        {
            if (data.SwapsLeft <= 0)
            {
                PrintToChat(player, $"{ChatColors.Red}Model degistirme hakkin kalmadi!");
                return;
            }
            data.SwapsLeft--;
        }

        if (AvailableModels.Count == 0) return;

        // Pick a different model
        string newModel;
        int attempts = 0;
        do
        {
            newModel = AvailableModels[Random.Shared.Next(AvailableModels.Count)];
            attempts++;
        } while (newModel == data.ModelPath && AvailableModels.Count > 1 && attempts < 10);

        data.ModelPath = newModel;

        if (data.PropEntity != null && data.PropEntity.IsValid)
        {
            data.PropEntity.SetModel(newModel);
        }

        // Update prop size and adjust health accordingly
        var newSize = Utils.ClassifyPropSize(newModel);
        var oldSize = data.Size;
        data.Size = newSize;

        int newMaxHealth = Utils.GetHealthForPropSize(newSize, Config.PropHealthSmall, Config.PropHealthMedium, Config.PropHealthLarge);
        int oldMaxHealth = Utils.GetHealthForPropSize(oldSize, Config.PropHealthSmall, Config.PropHealthMedium, Config.PropHealthLarge);

        // Scale current health proportionally to new max health
        var pawn = player.PlayerPawn.Value;
        if (pawn != null)
        {
            int currentHealth = pawn.Health;
            int scaledHealth = (int)Math.Ceiling((double)currentHealth / oldMaxHealth * newMaxHealth);
            scaledHealth = Math.Clamp(scaledHealth, 1, newMaxHealth);
            Utils.SetHealth(player, scaledHealth);
        }

        string sizeText = newSize switch
        {
            PropSize.Small => "Kucuk",
            PropSize.Large => "Buyuk",
            _ => "Orta"
        };

        if (!IsHidingPhase)
            PrintToChat(player, $"{ChatColors.Green}Model degistirildi! {ChatColors.Grey}Boyut: {ChatColors.Yellow}{sizeText} {ChatColors.Grey}| Kalan: {ChatColors.Yellow}{data.SwapsLeft}");
        else
            PrintToChat(player, $"{ChatColors.Green}Model degistirildi! {ChatColors.Grey}Boyut: {ChatColors.Yellow}{sizeText}");
    }

    /// <summary>
    /// Place a decoy prop at the player's current position.
    /// </summary>
    public void PlaceDecoy(CCSPlayerController player)
    {
        if (!HiddenPlayers.TryGetValue(player.Slot, out var data)) return;

        if (data.DecoysLeft <= 0)
        {
            PrintToChat(player, $"{ChatColors.Red}Decoy hakkin kalmadi!");
            return;
        }

        data.DecoysLeft--;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        var decoy = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (decoy == null) return;

        decoy.SetModel(data.ModelPath);
        decoy.DispatchSpawn();
        decoy.Teleport(pawn.AbsOrigin, pawn.AbsRotation);

        if (decoy.Collision != null)
        {
            decoy.Collision.CollisionGroup = 2;
            decoy.Collision.CollisionAttribute.CollisionGroup = 2;
        }

        data.DecoyProps.Add(decoy);

        PrintToChat(player, $"{ChatColors.Green}Decoy yerlestirildi! {ChatColors.Grey}Kalan: {ChatColors.Yellow}{data.DecoysLeft}");
    }

    /// <summary>
    /// Toggle freeze for a prop player. When frozen, the prop becomes solid.
    /// </summary>
    public void ToggleFreeze(CCSPlayerController player)
    {
        if (!HiddenPlayers.TryGetValue(player.Slot, out var data)) return;

        data.IsFrozen = !data.IsFrozen;

        if (data.IsFrozen)
        {
            Utils.FreezePlayer(player);
            PrintToChat(player, $"{ChatColors.LightBlue}Donmus durumdasin! {ChatColors.Grey}(Tekrar yaz: cozulursun)");
        }
        else
        {
            Utils.UnfreezePlayer(player);
            PrintToChat(player, $"{ChatColors.Yellow}Cozuldun! Hareket edebilirsin.");
        }
    }

    // ── Prop Damage System ──────────────────────────────────

    /// <summary>
    /// Called when any prop_dynamic takes damage. Checks if the prop belongs
    /// to a hidden player and transfers the kill.
    /// </summary>
    private HookResult OnPropTakeDamage(CEntityIOOutput output, string name,
        CEntityInstance activator, CEntityInstance caller, CVariant value, float delay)
    {
        if (!PropHuntEnabled || !Config.PropDamageKill) return HookResult.Continue;
        if (activator == null || caller == null) return HookResult.Continue;
        if (!activator.IsValid || !caller.IsValid) return HookResult.Continue;

        // Find the hidden player who owns this prop
        int? ownerSlot = null;
        bool isDecoy = false;

        foreach (var kvp in HiddenPlayers)
        {
            var data = kvp.Value;

            // Check if the damaged entity is the player's main prop
            if (data.PropEntity != null && data.PropEntity.IsValid
                && data.PropEntity.Handle == caller.Handle)
            {
                ownerSlot = kvp.Key;
                isDecoy = false;
                break;
            }

            // Check if the damaged entity is a decoy prop
            foreach (var decoy in data.DecoyProps)
            {
                if (decoy != null && decoy.IsValid && decoy.Handle == caller.Handle)
                {
                    ownerSlot = kvp.Key;
                    isDecoy = true;
                    break;
                }
            }

            if (ownerSlot != null) break;
        }

        // Not one of our managed props - might be a map prop, ignore
        if (ownerSlot == null) return HookResult.Continue;

        if (isDecoy)
        {
            // Decoy destroyed - just remove it
            Server.NextFrame(() =>
            {
                if (caller.IsValid) caller.Remove();

                if (HiddenPlayers.TryGetValue(ownerSlot.Value, out var ownerData))
                {
                    ownerData.DecoyProps.RemoveAll(d => !d.IsValid || d.Handle == caller.Handle);
                }

                // Try to find attacker name for the message
                string attackerName = GetAttackerName(activator);
                if (!string.IsNullOrEmpty(attackerName))
                {
                    PrintToChatAll($"{ChatColors.Grey}{attackerName} {ChatColors.Yellow}bir sahte prop'u kirdi!");
                }
            });

            return HookResult.Continue;
        }

        // Main prop hit - kill the hidden player
        var targetPlayer = Utilities.GetPlayerFromSlot(ownerSlot.Value);
        if (targetPlayer == null || !targetPlayer.IsValid || !targetPlayer.PawnIsAlive)
            return HookResult.Continue;

        Server.NextFrame(() =>
        {
            if (!targetPlayer.IsValid || !targetPlayer.PawnIsAlive) return;

            string attackerName = GetAttackerName(activator);

            // Remove the prop first
            RemovePropForPlayer(targetPlayer);

            // Make visible before killing to prevent crashes
            Utils.MakeVisible(targetPlayer);
            Utils.EnableShadow(targetPlayer);

            // Kill the player
            targetPlayer.PlayerPawn.Value?.CommitSuicide(false, true);

            // Announce the kill
            if (!string.IsNullOrEmpty(attackerName))
            {
                PrintToChatAll($"{ChatColors.Red}{attackerName} {ChatColors.Default}-> {ChatColors.Green}{targetPlayer.PlayerName} {ChatColors.Grey}(Prop vuruldu!)");
            }
            else
            {
                PrintToChatAll($"{ChatColors.Green}{targetPlayer.PlayerName} {ChatColors.Grey}bulundu! (Prop vuruldu)");
            }
        });

        return HookResult.Continue;
    }

    /// <summary>
    /// Try to get the attacker's player name from an activator entity instance.
    /// </summary>
    private string GetAttackerName(CEntityInstance activator)
    {
        try
        {
            if (activator == null || !activator.IsValid) return string.Empty;
            if (activator.DesignerName != "player") return string.Empty;

            // The activator is a player pawn
            var pawn = new CCSPlayerPawn(activator.Handle);
            if (pawn == null || !pawn.IsValid) return string.Empty;

            var controller = pawn.OriginalController?.Value;
            if (controller == null || !controller.IsValid) return string.Empty;

            return controller.PlayerName;
        }
        catch
        {
            return string.Empty;
        }
    }

    // ── Taunt Sound System ──────────────────────────────────

    /// <summary>
    /// Play a taunt sound at the player's prop position.
    /// All players can hear it, giving seekers a clue.
    /// </summary>
    public void PlayTaunt(CCSPlayerController player)
    {
        if (!HiddenPlayers.TryGetValue(player.Slot, out var data)) return;

        if (AvailableTauntSounds.Count == 0)
        {
            PrintToChat(player, $"{ChatColors.Red}Taunt sesleri yapilandirilmamis!");
            return;
        }

        if (data.TauntsLeft <= 0)
        {
            PrintToChat(player, $"{ChatColors.Red}Taunt hakkin kalmadi!");
            return;
        }

        float currentTime = Server.CurrentTime;
        if (currentTime - data.LastTauntTime < Config.TauntCooldown)
        {
            float remaining = Config.TauntCooldown - (currentTime - data.LastTauntTime);
            PrintToChat(player, $"{ChatColors.Red}Taunt bekleme suresi: {remaining:F0} saniye");
            return;
        }

        data.TauntsLeft--;
        data.LastTauntTime = currentTime;

        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        // Pick a random taunt sound
        string sound = AvailableTauntSounds[Random.Shared.Next(AvailableTauntSounds.Count)];

        // Get the position to play the sound at (prop position or player position)
        var soundPos = data.PropEntity != null && data.PropEntity.IsValid
            ? data.PropEntity.AbsOrigin
            : pawn.AbsOrigin;

        // Create a sound event entity at the prop's position
        var soundEntity = Utilities.CreateEntityByName<CSoundEventEntity>("point_soundevent");
        if (soundEntity != null)
        {
            soundEntity.SoundName = sound;
            soundEntity.StartOnSpawn = true;
            soundEntity.DispatchSpawn();
            soundEntity.Teleport(soundPos);

            // Clean up the entity after the sound plays
            AddTimer(5.0f, () =>
            {
                if (soundEntity.IsValid)
                    soundEntity.Remove();
            }, TimerFlags.STOP_ON_MAPCHANGE);
        }

        PrintToChat(player, $"{ChatColors.Green}Taunt caldi! {ChatColors.Grey}Kalan: {ChatColors.Yellow}{data.TauntsLeft}");
        PrintToChatAll($"{ChatColors.Magenta}Bir saklanan taunt kullandi! {ChatColors.Grey}Sesi takip edin!");
    }

    // ── Whistle System ────────────────────────────────────────

    /// <summary>
    /// Play a whistle sound. All players get notified.
    /// </summary>
    public void PlayWhistle(CCSPlayerController player)
    {
        if (!HiddenPlayers.TryGetValue(player.Slot, out var data)) return;

        if (data.WhistlesLeft <= 0)
        {
            PrintToChat(player, $"{ChatColors.Red}Islik hakkin kalmadi!");
            return;
        }

        float currentTime = Server.CurrentTime;
        if (currentTime - data.LastWhistleTime < Config.WhistleCooldown)
        {
            float remaining = Config.WhistleCooldown - (currentTime - data.LastWhistleTime);
            PrintToChat(player, $"{ChatColors.Red}Islik bekleme suresi: {remaining:F0} saniye");
            return;
        }

        data.WhistlesLeft--;
        data.LastWhistleTime = currentTime;

        PrintToChatAll($"{ChatColors.Yellow}Bir saklanan islik caldi! {ChatColors.Grey}(Onu bul!)");
        PrintToChat(player, $"{ChatColors.Green}Islik caldin! {ChatColors.Grey}Kalan: {ChatColors.Yellow}{data.WhistlesLeft}");
    }

    // ── Seeker Damage Penalty ───────────────────────────────

    /// <summary>
    /// Apply damage penalty to a seeker who shot a non-player prop (miss penalty).
    /// Encourages seekers not to spam-shoot everything.
    /// </summary>
    public void ApplySeekerMissPenalty(CCSPlayerController seeker)
    {
        if (Config.SeekerDamagePerMiss <= 0) return;
        if (!seeker.IsValid || !seeker.PawnIsAlive) return;

        var pawn = seeker.PlayerPawn.Value;
        if (pawn == null) return;

        int newHealth = pawn.Health - Config.SeekerDamagePerMiss;
        if (newHealth <= 0)
        {
            pawn.CommitSuicide(false, true);
            PrintToChatAll($"{ChatColors.Red}{seeker.PlayerName} {ChatColors.Grey}cok fazla bos yere ates etti ve oldu!");
        }
        else
        {
            Utils.SetHealth(seeker, newHealth);
            PrintToChat(seeker, $"{ChatColors.Red}-{Config.SeekerDamagePerMiss} HP! {ChatColors.Grey}Bos prop'a atesettin. Kalan: {ChatColors.Yellow}{newHealth} HP");
        }
    }
}
