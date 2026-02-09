using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace PropHunt;

public partial class PropHuntPlugin
{
    private void RegisterCommands()
    {
        // ── Admin commands ──────────────────────────────────
        AddCommand("css_prophunt", "PropHunt modunu ac/kapat (Admin)", CommandTogglePropHunt);
        AddCommand("css_ph", "PropHunt modunu ac/kapat (Admin)", CommandTogglePropHunt);
        AddCommand("css_prophunt_enable", "PropHunt modunu ac (Admin)", CommandEnablePropHunt);
        AddCommand("css_prophunt_disable", "PropHunt modunu kapat (Admin)", CommandDisablePropHunt);

        // Server console command (RCON)
        AddCommand("sv_prophunt", "PropHunt modunu ac/kapat (0/1)", CommandServerPropHunt);

        // ── Prop model selection menu ───────────────────────
        AddCommand("css_prop", "Prop secim menusu ac", CommandProp);
        AddCommand("css_props", "Prop secim menusu ac", CommandProp);
        AddCommand("css_model", "Prop secim menusu ac", CommandProp);

        // ── Swap prop model (random) ────────────────────────
        AddCommand("css_swap", "Rastgele model degistir", CommandSwap);

        // ── Freeze/Unfreeze ─────────────────────────────────
        AddCommand("css_freeze", "Prop'u dondur/coz", CommandFreeze);
        AddCommand("css_don", "Prop'u dondur/coz", CommandFreeze);

        // ── Place decoy prop ────────────────────────────────
        AddCommand("css_decoy", "Sahte prop yerlestir", CommandDecoy);

        // ── Third person toggle ─────────────────────────────
        AddCommand("css_tp", "3. sahis gorunum", CommandThirdPerson);
        AddCommand("css_thirdperson", "3. sahis gorunum", CommandThirdPerson);

        // ── Whistle (makes a sound) ─────────────────────────
        AddCommand("css_whistle", "Islik cal (ses cikar)", CommandWhistle);

        // ── Taunt (play sound at prop position) ─────────────
        AddCommand("css_taunt", "Taunt sesi cal", CommandTaunt);

        // ── Prevent team switching ──────────────────────────
        AddCommandListener("jointeam", OnJoinTeam, HookMode.Pre);
    }

    // ── Admin Commands ──────────────────────────────────────

    [RequiresPermissions("@css/rcon")]
    private void CommandTogglePropHunt(CCSPlayerController? player, CommandInfo info)
    {
        if (PropHuntEnabled)
            DisablePropHunt();
        else
            EnablePropHunt();
    }

    [RequiresPermissions("@css/rcon")]
    private void CommandEnablePropHunt(CCSPlayerController? player, CommandInfo info)
    {
        if (PropHuntEnabled)
        {
            var msg = $"{ChatColors.Yellow}PropHunt modu zaten acik!";
            if (player != null) PrintToChat(player, msg);
            else Logger.LogInformation("[PropHunt] Already enabled.");
            return;
        }
        EnablePropHunt();
    }

    [RequiresPermissions("@css/rcon")]
    private void CommandDisablePropHunt(CCSPlayerController? player, CommandInfo info)
    {
        if (!PropHuntEnabled)
        {
            var msg = $"{ChatColors.Yellow}PropHunt modu zaten kapali!";
            if (player != null) PrintToChat(player, msg);
            else Logger.LogInformation("[PropHunt] Already disabled.");
            return;
        }
        DisablePropHunt();
    }

    /// <summary>
    /// Server console command: sv_prophunt 0/1
    /// Can be used from RCON or server.cfg
    /// </summary>
    private void CommandServerPropHunt(CCSPlayerController? player, CommandInfo info)
    {
        // Only allow from server console (player == null) or admin
        if (player != null)
        {
            if (!AdminManager.PlayerHasPermissions(player, "@css/rcon"))
            {
                PrintToChat(player, $"{ChatColors.Red}Bu komutu kullanma yetkin yok!");
                return;
            }
        }

        string arg = info.GetArg(1).Trim();

        if (string.IsNullOrEmpty(arg))
        {
            // No argument - show current state
            string state = PropHuntEnabled ? "1 (ACIK)" : "0 (KAPALI)";
            info.ReplyToCommand($"[PropHunt] sv_prophunt = {state}");
            return;
        }

        if (arg == "1" || arg.Equals("on", StringComparison.OrdinalIgnoreCase))
        {
            EnablePropHunt();
        }
        else if (arg == "0" || arg.Equals("off", StringComparison.OrdinalIgnoreCase))
        {
            DisablePropHunt();
        }
        else
        {
            info.ReplyToCommand("[PropHunt] Kullanim: sv_prophunt <0/1>");
        }
    }

    // ── Command Handlers ────────────────────────────────────

    private void CommandProp(CCSPlayerController? player, CommandInfo info)
    {
        if (!PropHuntEnabled) return;
        if (player == null || !Utils.IsValidPlayer(player)) return;

        if (player.Team != HidingTeam)
        {
            PrintToChat(player, $"{ChatColors.Red}Bu komut sadece saklananlar icin!");
            return;
        }

        if (!player.PawnIsAlive)
        {
            PrintToChat(player, $"{ChatColors.Red}Hayatta olman gerekiyor!");
            return;
        }

        OpenPropMenu(player);
    }

    private void CommandSwap(CCSPlayerController? player, CommandInfo info)
    {
        if (!PropHuntEnabled) return;
        if (player == null || !Utils.IsValidPlayer(player)) return;

        if (player.Team != HidingTeam)
        {
            PrintToChat(player, $"{ChatColors.Red}Bu komut sadece saklananlar icin!");
            return;
        }

        if (!player.PawnIsAlive)
        {
            PrintToChat(player, $"{ChatColors.Red}Hayatta olman gerekiyor!");
            return;
        }

        if (!HiddenPlayers.ContainsKey(player.Slot))
        {
            SpawnPropForPlayer(player);
            return;
        }

        SwapPropModel(player);
    }

    private void CommandFreeze(CCSPlayerController? player, CommandInfo info)
    {
        if (!PropHuntEnabled) return;
        if (player == null || !Utils.IsValidPlayer(player)) return;

        if (player.Team != HidingTeam)
        {
            PrintToChat(player, $"{ChatColors.Red}Bu komut sadece saklananlar icin!");
            return;
        }

        if (!player.PawnIsAlive)
        {
            PrintToChat(player, $"{ChatColors.Red}Hayatta olman gerekiyor!");
            return;
        }

        if (!HiddenPlayers.ContainsKey(player.Slot))
        {
            PrintToChat(player, $"{ChatColors.Red}Once bir prop secmelisin!");
            return;
        }

        ToggleFreeze(player);
    }

    private void CommandDecoy(CCSPlayerController? player, CommandInfo info)
    {
        if (!PropHuntEnabled) return;
        if (player == null || !Utils.IsValidPlayer(player)) return;

        if (player.Team != HidingTeam)
        {
            PrintToChat(player, $"{ChatColors.Red}Bu komut sadece saklananlar icin!");
            return;
        }

        if (!player.PawnIsAlive)
        {
            PrintToChat(player, $"{ChatColors.Red}Hayatta olman gerekiyor!");
            return;
        }

        if (!HiddenPlayers.ContainsKey(player.Slot))
        {
            PrintToChat(player, $"{ChatColors.Red}Once bir prop secmelisin!");
            return;
        }

        PlaceDecoy(player);
    }

    private void CommandThirdPerson(CCSPlayerController? player, CommandInfo info)
    {
        if (!PropHuntEnabled) return;
        if (player == null || !Utils.IsValidPlayer(player)) return;

        if (player.Team != HidingTeam)
        {
            PrintToChat(player, $"{ChatColors.Red}Bu komut sadece saklananlar icin!");
            return;
        }

        if (!player.PawnIsAlive)
        {
            PrintToChat(player, $"{ChatColors.Red}Hayatta olman gerekiyor!");
            return;
        }

        if (!HiddenPlayers.ContainsKey(player.Slot))
        {
            PrintToChat(player, $"{ChatColors.Red}Once bir prop secmelisin!");
            return;
        }

        ToggleThirdPerson(player);
    }

    private void CommandWhistle(CCSPlayerController? player, CommandInfo info)
    {
        if (!PropHuntEnabled) return;
        if (player == null || !Utils.IsValidPlayer(player)) return;

        if (player.Team != HidingTeam)
        {
            PrintToChat(player, $"{ChatColors.Red}Bu komut sadece saklananlar icin!");
            return;
        }

        if (!player.PawnIsAlive)
        {
            PrintToChat(player, $"{ChatColors.Red}Hayatta olman gerekiyor!");
            return;
        }

        if (!HiddenPlayers.TryGetValue(player.Slot, out var data))
        {
            PrintToChat(player, $"{ChatColors.Red}Once bir prop secmelisin!");
            return;
        }

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

        // Announce whistle to all players
        PrintToChatAll($"{ChatColors.Yellow}Bir saklanan islik caldi! {ChatColors.Grey}(Onu bul!)");

        PrintToChat(player, $"{ChatColors.Green}Islik caldin! {ChatColors.Grey}Kalan: {ChatColors.Yellow}{data.WhistlesLeft}");
    }

    private void CommandTaunt(CCSPlayerController? player, CommandInfo info)
    {
        if (!PropHuntEnabled) return;
        if (player == null || !Utils.IsValidPlayer(player)) return;

        if (player.Team != HidingTeam)
        {
            PrintToChat(player, $"{ChatColors.Red}Bu komut sadece saklananlar icin!");
            return;
        }

        if (!player.PawnIsAlive)
        {
            PrintToChat(player, $"{ChatColors.Red}Hayatta olman gerekiyor!");
            return;
        }

        if (!HiddenPlayers.ContainsKey(player.Slot))
        {
            PrintToChat(player, $"{ChatColors.Red}Once bir prop secmelisin!");
            return;
        }

        PlayTaunt(player);
    }

    // ── Join Team Prevention ────────────────────────────────

    private HookResult OnJoinTeam(CCSPlayerController? player, CommandInfo info)
    {
        if (!PropHuntEnabled) return HookResult.Continue;
        if (player == null || !Utils.IsValidPlayer(player)) return HookResult.Continue;

        // During active game, prevent manual team switches
        if (IsHidingPhase || SeekersReleased)
        {
            PrintToChat(player, $"{ChatColors.Red}Aktif oyun sirasinda takim degistiremezsin!");
            return HookResult.Handled;
        }

        return HookResult.Continue;
    }

    // ── Prop Selection Menu ─────────────────────────────────

    private void OpenPropMenu(CCSPlayerController player)
    {
        if (AvailableModels.Count == 0)
        {
            PrintToChat(player, $"{ChatColors.Red}Bu haritada kullanilabilir model yok!");
            return;
        }

        var menu = new ChatMenu("Prop Sec (Model)");

        // Add random option first
        menu.AddMenuOption("🎲 Rastgele", (p, option) =>
        {
            if (!p.IsValid || !p.PawnIsAlive) return;
            SwapPropModel(p);
        });

        // Add each model as an option (with a shorter display name)
        foreach (var model in AvailableModels)
        {
            string displayName = GetModelDisplayName(model);
            string modelCapture = model; // capture for closure

            menu.AddMenuOption(displayName, (p, option) =>
            {
                if (!p.IsValid || !p.PawnIsAlive) return;

                if (!HiddenPlayers.TryGetValue(p.Slot, out var data))
                {
                    SpawnPropForPlayer(p, modelCapture);
                }
                else
                {
                    // Check swap limits
                    if (!IsHidingPhase)
                    {
                        if (data.SwapsLeft <= 0)
                        {
                            PrintToChat(p, $"{ChatColors.Red}Model degistirme hakkin kalmadi!");
                            return;
                        }
                        data.SwapsLeft--;
                    }

                    data.ModelPath = modelCapture;
                    if (data.PropEntity != null && data.PropEntity.IsValid)
                    {
                        data.PropEntity.SetModel(modelCapture);
                    }

                    PrintToChat(p, $"{ChatColors.Green}Model secildi: {ChatColors.Yellow}{displayName}");
                }
            });
        }

        MenuManager.OpenChatMenu(player, menu);
    }

    /// <summary>
    /// Extracts a friendly display name from a model path.
    /// </summary>
    private string GetModelDisplayName(string modelPath)
    {
        // "models/props/de_inferno/claypot03.vmdl" -> "claypot03"
        string fileName = Path.GetFileNameWithoutExtension(modelPath);
        if (string.IsNullOrEmpty(fileName)) return modelPath;

        // Clean up underscores and numbers for readability
        return fileName.Replace("_", " ").Trim();
    }
}
