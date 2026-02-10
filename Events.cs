using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Drawing;

namespace PropHunt;

public partial class PropHuntPlugin
{
    // ── Map Events ──────────────────────────────────────────

    private void OnMapStart(string mapName)
    {
        // Always load models and taunt sounds (so they're ready when mode is enabled)
        AvailableModels = Utils.LoadMapModels(ModuleDirectory, mapName, Config.DefaultModels);
        AvailableTauntSounds = Utils.LoadTauntSounds(ModuleDirectory, Config.TauntSounds);
        Logger.LogInformation($"[PropHunt] Loaded {AvailableModels.Count} models, {AvailableTauntSounds.Count} taunt sounds for map: {mapName}");

        // Cleanup state
        HiddenPlayers.Clear();
        IsHidingPhase = false;
        SeekersReleased = false;

        if (!PropHuntEnabled) return;

        // Server commands for prophunt
        Server.ExecuteCommand("mp_give_player_c4 0");
        Server.ExecuteCommand("mp_death_drop_gun 0");
        Server.ExecuteCommand("mp_death_drop_grenade 0");
        Server.ExecuteCommand("mp_death_drop_defuser 0");
        Server.ExecuteCommand("sv_disable_radar 1");

        // Set team names
        if (HidingTeam == CsTeam.Terrorist)
        {
            Server.ExecuteCommand("mp_teamname_1 Hiders");
            Server.ExecuteCommand("mp_teamname_2 Seekers");
        }
        else
        {
            Server.ExecuteCommand("mp_teamname_1 Seekers");
            Server.ExecuteCommand("mp_teamname_2 Hiders");
        }
    }

    private void OnServerPrecacheResources(ResourceManifest manifest)
    {
        // Precache all model resources
        foreach (var model in AvailableModels)
        {
            if (!string.IsNullOrEmpty(model))
                manifest.AddResource(model);
        }

        // Precache default models from config
        foreach (var model in Config.DefaultModels)
        {
            if (!string.IsNullOrEmpty(model))
                manifest.AddResource(model);
        }

        // Precache taunt sounds (from folder + config)
        foreach (var sound in AvailableTauntSounds)
        {
            if (!string.IsNullOrEmpty(sound))
                manifest.AddResource(sound);
        }
    }

    private void OnEntitySpawned(CEntityInstance entity)
    {
        // Auto-discover physics props on the map and add them to available models
        if (entity.DesignerName == "prop_physics_multiplayer")
        {
            Server.NextFrame(() =>
            {
                try
                {
                    var prop = new CPhysicsPropMultiplayer(entity.Handle);
                    string model = prop.CBodyComponent?.SceneNode?.GetSkeletonInstance().ModelState.ModelName ?? string.Empty;

                    if (!string.IsNullOrEmpty(model) && !AvailableModels.Contains(model))
                    {
                        AvailableModels.Add(model);
                    }
                }
                catch (Exception) { }
            });
        }

        // Remove buy zones
        if (entity.DesignerName == "func_buyzone")
        {
            Server.NextFrame(() => entity.Remove());
        }
    }

    // ── Round Events ────────────────────────────────────────

    private HookResult OnRoundPrestart(EventRoundPrestart @event, GameEventInfo info)
    {
        if (!PropHuntEnabled) return HookResult.Continue;
        if (!Config.TeamScramble) return HookResult.Continue;

        var players = Utilities.GetPlayers()
            .Where(p => p.IsValid && !p.IsBot && !p.IsHLTV &&
                       (p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist))
            .ToList();

        if (players.Count < 2) return HookResult.Continue;

        // Shuffle players between teams
        players.Shuffle();

        int halfCount = (players.Count + 1) / 2;

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            if (i < halfCount)
                player.SwitchTeam(HidingTeam);
            else
                player.SwitchTeam(SeekingTeam);
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // Cleanup previous round
        CleanupAllProps();

        if (!PropHuntEnabled) return HookResult.Continue;

        // Set hide phase
        IsHidingPhase = true;
        SeekersReleased = false;
        HidePhaseEndTime = DateTime.Now.AddSeconds(Config.HideTime);

        // Remove C4 from the map
        var bombs = Utilities.FindAllEntitiesByDesignerName<CBaseEntity>("weapon_c4");
        foreach (var bomb in bombs)
        {
            if (bomb.IsValid) bomb.Remove();
        }

        // Setup all players
        var players = Utilities.GetPlayers()
            .Where(p => p.IsValid && !p.IsBot && p.PawnIsAlive)
            .ToList();

        foreach (var player in players)
        {
            if (player.Team == HidingTeam)
            {
                // Hider setup
                Server.NextFrame(() =>
                {
                    if (!player.IsValid || !player.PawnIsAlive) return;

                    SpawnPropForPlayer(player);

                    PrintToChat(player, $"{ChatColors.Green}Saklanma zamani! {ChatColors.Yellow}{Config.HideTime} saniye{ChatColors.Default} saklanma suresi.");
                    var keyInfo = BuildKeyBindingChat();
                    if (!string.IsNullOrEmpty(keyInfo))
                        PrintToChat(player, keyInfo);
                    PrintToChat(player, $"{ChatColors.Grey}Komutlar: {ChatColors.LightBlue}!prop {ChatColors.Grey}(model sec) | {ChatColors.LightBlue}!tp {ChatColors.Grey}(3. sahis) | {ChatColors.LightBlue}!taunt !whistle !decoy");
                });
            }
            else if (player.Team == SeekingTeam)
            {
                // Seeker setup - freeze them during hide phase
                Server.NextFrame(() =>
                {
                    if (!player.IsValid || !player.PawnIsAlive) return;

                    player.RemoveWeapons();
                    Utils.FreezePlayer(player);

                    Utils.SetHealth(player, Config.SeekerHealth);

                    PrintToChat(player, $"{ChatColors.Red}Arayici takimindasin! {ChatColors.Yellow}{Config.HideTime} saniye{ChatColors.Default} sonra serbest birakilacaksin.");
                });
            }
        }

        // Timer to end hide phase and release seekers
        AddTimer(Config.HideTime, () =>
        {
            EndHidingPhase();
        }, TimerFlags.STOP_ON_MAPCHANGE);

        // Countdown warnings
        float[] warnings = { 10f, 30f };
        foreach (var warningTime in warnings)
        {
            if (Config.HideTime > warningTime)
            {
                AddTimer(Config.HideTime - warningTime, () =>
                {
                    PrintToChatAll($"{ChatColors.Yellow}Arayicilar {warningTime} saniye icinde serbest birakilacak!");
                }, TimerFlags.STOP_ON_MAPCHANGE);
            }
        }

        return HookResult.Continue;
    }

    private void EndHidingPhase()
    {
        IsHidingPhase = false;
        SeekersReleased = true;

        PrintToChatAll($"{ChatColors.Red}Arayicilar serbest birakildi! {ChatColors.Green}AV BASLADI!");

        var seekers = Utilities.GetPlayers()
            .Where(p => p.IsValid && !p.IsBot && p.PawnIsAlive && p.Team == SeekingTeam)
            .ToList();

        foreach (var seeker in seekers)
        {
            Utils.UnfreezePlayer(seeker);

            // Give seekers their weapons
            seeker.RemoveWeapons();
            foreach (var weapon in Config.SeekerWeapons)
            {
                seeker.GiveNamedItem(weapon);
            }
        }
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        IsHidingPhase = false;
        SeekersReleased = false;

        if (!PropHuntEnabled) return HookResult.Continue;

        // Check win conditions
        var hidersAlive = Utilities.GetPlayers()
            .Count(p => p.IsValid && !p.IsBot && p.PawnIsAlive && p.Team == HidingTeam);

        if (hidersAlive > 0)
        {
            PrintToChatAll($"{ChatColors.Green}Saklananlar kazandi! {ChatColors.Yellow}{hidersAlive} saklanan hayatta kaldi.");
        }
        else
        {
            PrintToChatAll($"{ChatColors.Red}Arayicilar kazandi! {ChatColors.Default}Tum saklananlar bulundu.");
        }

        return HookResult.Continue;
    }

    // ── Player Events ───────────────────────────────────────

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        if (!PropHuntEnabled) return HookResult.Continue;
        var player = @event.Userid;
        if (player == null || !Utils.IsValidPlayer(player)) return HookResult.Continue;

        Server.NextFrame(() =>
        {
            if (!player.IsValid || !player.PawnIsAlive) return;

            if (player.Team == HidingTeam)
            {
                // Setup as hider
                player.RemoveWeapons();
                SpawnPropForPlayer(player);
            }
            else if (player.Team == SeekingTeam)
            {
                // Setup as seeker
                player.RemoveWeapons();
                Utils.SetHealth(player, Config.SeekerHealth);

                if (!SeekersReleased)
                {
                    // Still in hiding phase, freeze them
                    Utils.FreezePlayer(player);
                }
                else
                {
                    foreach (var weapon in Config.SeekerWeapons)
                    {
                        player.GiveNamedItem(weapon);
                    }
                }
            }
        });

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null) return HookResult.Continue;

        // Clean up prop for dead hiders
        if (HiddenPlayers.ContainsKey(player.Slot))
        {
            Server.NextFrame(() =>
            {
                // Reset the player model to a default before removing to prevent crashes
                var pawn = player.PlayerPawn.Value;
                if (pawn != null && pawn.IsValid)
                {
                    Utils.MakeVisible(player);
                    Utils.EnableShadow(player);
                }

                RemovePropForPlayer(player);
            });
        }

        // Check if all hiders are dead
        Server.NextFrame(() =>
        {
            if (!SeekersReleased) return;

            var hidersAlive = Utilities.GetPlayers()
                .Count(p => p.IsValid && !p.IsBot && p.PawnIsAlive && p.Team == HidingTeam);

            if (hidersAlive == 1)
            {
                var lastHider = Utilities.GetPlayers()
                    .FirstOrDefault(p => p.IsValid && !p.IsBot && p.PawnIsAlive && p.Team == HidingTeam);

                if (lastHider != null)
                {
                    PrintToChatAll($"{ChatColors.LightRed}Son saklanan: {ChatColors.Yellow}{lastHider.PlayerName}!");
                }
            }
        });

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null) return HookResult.Continue;

        if (HiddenPlayers.ContainsKey(player.Slot))
        {
            RemovePropForPlayer(player);
        }

        return HookResult.Continue;
    }

    // ── OnTick ──────────────────────────────────────────────

    private void OnTick()
    {
        if (!PropHuntEnabled) return;

        // Update prop positions and handle key bindings for hidden players
        foreach (var kvp in HiddenPlayers.ToList())
        {
            var slot = kvp.Key;
            var data = kvp.Value;

            var player = Utilities.GetPlayerFromSlot(slot);
            if (player == null || !player.IsValid || !player.PawnIsAlive)
                continue;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null) continue;

            // Move prop with the player (unless frozen)
            if (!data.IsFrozen && data.PropEntity != null && data.PropEntity.IsValid)
            {
                data.PropEntity.Teleport(pawn.AbsOrigin, pawn.AbsRotation);
            }

            // Update third person camera position
            if (data.IsThirdPerson && data.CameraProp != null && data.CameraProp.IsValid)
            {
                var cameraPos = Utils.CalculatePositionInFront(player, -110, 90);
                data.CameraProp.Teleport(cameraPos, pawn.V_angle);
            }

            // ── Key binding checks (one-press detection) ────
            var buttons = player.Buttons;
            ProcessKeyBinding(buttons, Config.KeyTaunt, ref data._btnTauntDown, () => PlayTaunt(player));
            ProcessKeyBinding(buttons, Config.KeySwap, ref data._btnSwapDown, () => SwapPropModel(player));
            ProcessKeyBinding(buttons, Config.KeyFreeze, ref data._btnFreezeDown, () => ToggleFreeze(player));
            ProcessKeyBinding(buttons, Config.KeyWhistle, ref data._btnWhistleDown, () => PlayWhistle(player));
            ProcessKeyBinding(buttons, Config.KeyDecoy, ref data._btnDecoyDown, () => PlaceDecoy(player));
        }

        // Display hiding phase countdown
        if (IsHidingPhase)
        {
            var remaining = HidePhaseEndTime - DateTime.Now;
            if (remaining.TotalSeconds > 0)
            {
                string timeLeft = remaining.ToString(@"mm\:ss");

                foreach (var player in Utilities.GetPlayers())
                {
                    if (!player.IsValid || player.IsBot) continue;

                    if (player.Team == HidingTeam)
                    {
                        player.PrintToCenter($"Saklanma suresi: {timeLeft} | {BuildKeyBindingCenter()}");
                    }
                    else if (player.Team == SeekingTeam)
                    {
                        player.PrintToCenter($"Serbest birakilma: {timeLeft}");
                    }
                }
            }
        }
        else if (SeekersReleased)
        {
            // Show status to hidden players
            foreach (var kvp in HiddenPlayers)
            {
                var player = Utilities.GetPlayerFromSlot(kvp.Key);
                if (player == null || !player.IsValid || player.IsBot) continue;

                var data = kvp.Value;
                string frozenStatus = data.IsFrozen ? "DONMUS" : "HAREKET";
                player.PrintToCenter($"Swap: {data.SwapsLeft} | Decoy: {data.DecoysLeft} | Taunt: {data.TauntsLeft} | Islik: {data.WhistlesLeft} | {frozenStatus}\n{BuildKeyBindingCenter()}");
            }
        }
    }

    /// <summary>
    /// Processes a key binding with one-press detection.
    /// Maps a config string ("Attack", "Attack2", "Use", "Reload")
    /// to the actual PlayerButtons flag. Triggers the action only once
    /// per key press (not while held down).
    /// </summary>
    private static void ProcessKeyBinding(PlayerButtons currentButtons, string configKey,
        ref bool wasDown, Action action)
    {
        // Map config string to the actual button
        PlayerButtons mappedButton = configKey.ToLower() switch
        {
            "attack" => PlayerButtons.Attack,
            "attack2" => PlayerButtons.Attack2,
            "use" => PlayerButtons.Use,
            "reload" => PlayerButtons.Reload,
            _ => 0 // "none" or invalid = disabled
        };

        if (mappedButton == 0) return;

        bool isDown = (currentButtons & mappedButton) != 0;

        if (isDown && !wasDown)
        {
            wasDown = true;
            action();
        }
        else if (!isDown)
        {
            wasDown = false;
        }
    }

    // ── CheckTransmit - True Player Pawn Hiding ─────────────

    /// <summary>
    /// Completely hides hidden players' pawn entities from other clients.
    /// Unlike alpha=0 (which can still be seen via outlines/shadows/glitches),
    /// this prevents the pawn data from being transmitted to other players at all.
    /// Spectators can still see hidden players.
    /// </summary>
    private void OnCheckTransmit(CCheckTransmitInfoList infoList)
    {
        if (!PropHuntEnabled) return;
        if (HiddenPlayers.Count == 0) return;

        // Collect hidden player pawns once (avoid repeated lookups per viewer)
        var hiddenPawns = new List<(int slot, CBaseEntity pawn)>();
        foreach (var slot in HiddenPlayers.Keys)
        {
            var hiddenPlayer = Utilities.GetPlayerFromSlot(slot);
            if (hiddenPlayer == null || !hiddenPlayer.IsValid || !hiddenPlayer.PawnIsAlive)
                continue;

            var pawn = hiddenPlayer.Pawn.Value;
            if (pawn == null || !pawn.IsValid) continue;

            hiddenPawns.Add((slot, pawn));
        }

        if (hiddenPawns.Count == 0) return;

        // For each viewer, remove all hidden player pawns from their transmit list
        foreach ((CCheckTransmitInfo info, CCSPlayerController? viewer) in infoList)
        {
            if (viewer == null || viewer.IsBot || !viewer.PawnIsAlive)
                continue;

            // Check if viewer is a spectator - spectators should see hidden players
            var viewerPawn = viewer.Pawn.Value?.As<CCSPlayerPawn>();
            if (viewerPawn != null && viewerPawn.PlayerState == CSPlayerState.STATE_OBSERVER_MODE)
                continue;

            foreach (var (slot, hiddenPawn) in hiddenPawns)
            {
                // Don't hide the player from themselves
                if (viewer.Slot == slot) continue;

                // Remove the hidden player's pawn from this viewer's transmit entities
                info.TransmitEntities.Remove(hiddenPawn);
            }
        }
    }

    // ── Sound Event Hiding ──────────────────────────────────

    /// <summary>
    /// Intercepts sound events (footsteps, landing, etc.) from hidden players
    /// and removes all other players from the recipients list.
    /// The hidden player can still hear their own sounds.
    /// Message ID 208 = CMsgSosStartSoundEvent
    /// </summary>
    private HookResult OnSoundEvent(UserMessage um)
    {
        if (!PropHuntEnabled) return HookResult.Continue;
        if (HiddenPlayers.Count == 0) return HookResult.Continue;

        try
        {
            // Read which entity produced the sound
            int entIndex = um.ReadInt("source_entity_index");
            nint entHandle = NativeAPI.GetEntityFromIndex(entIndex);

            if (entHandle == IntPtr.Zero) return HookResult.Continue;

            // Check if the sound source is a player pawn
            var pawn = new CBasePlayerPawn(entHandle);
            if (pawn == null || !pawn.IsValid || pawn.DesignerName != "player")
                return HookResult.Continue;

            // Get the player controller from the pawn
            var controller = pawn.Controller?.Value?.As<CCSPlayerController>();
            if (controller == null || !controller.IsValid)
                return HookResult.Continue;

            // Check if this player is hidden
            if (!HiddenPlayers.ContainsKey(controller.Slot))
                return HookResult.Continue;

            // Remove all other players from the recipients
            // Only the hidden player themselves will hear the sound
            foreach (var target in Utilities.GetPlayers().Where(p => !p.IsBot))
            {
                if (target.Slot == controller.Slot) continue;
                um.Recipients.Remove(target);
            }
        }
        catch
        {
            // Silently ignore any errors - don't break sound for everyone
        }

        return HookResult.Continue;
    }
}
