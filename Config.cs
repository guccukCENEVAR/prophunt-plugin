using CounterStrikeSharp.API.Core;
using System.Text.Json.Serialization;

namespace PropHunt;

public class PluginConfig : BasePluginConfig
{
    [JsonPropertyName("EnabledByDefault")]
    public bool EnabledByDefault { get; set; } = false;

    [JsonPropertyName("Prefix")]
    public string Prefix { get; set; } = "{lightblue}[PropHunt]";

    [JsonPropertyName("HidingTeam")]
    public string HidingTeam { get; set; } = "CT";

    [JsonPropertyName("HideTime")]
    public float HideTime { get; set; } = 60f;

    [JsonPropertyName("TeamScramble")]
    public bool TeamScramble { get; set; } = true;

    [JsonPropertyName("MinPlayers")]
    public int MinPlayers { get; set; } = 2;

    [JsonPropertyName("PropHealth")]
    public int PropHealth { get; set; } = 100;

    [JsonPropertyName("SeekerHealth")]
    public int SeekerHealth { get; set; } = 150;

    [JsonPropertyName("SwapLimit")]
    public int SwapLimit { get; set; } = 3;

    [JsonPropertyName("DecoyLimit")]
    public int DecoyLimit { get; set; } = 2;

    [JsonPropertyName("WhistleLimit")]
    public int WhistleLimit { get; set; } = 5;

    [JsonPropertyName("WhistleCooldown")]
    public float WhistleCooldown { get; set; } = 10f;

    [JsonPropertyName("TauntLimit")]
    public int TauntLimit { get; set; } = 5;

    [JsonPropertyName("TauntCooldown")]
    public float TauntCooldown { get; set; } = 15f;

    [JsonPropertyName("TauntSounds")]
    public List<string> TauntSounds { get; set; } = new()
    {
        "sounds/ambient/animal/bird15.vsnd",
        "sounds/ambient/animal/bird14.vsnd",
        "sounds/ambient/animal/bird13.vsnd"
    };

    [JsonPropertyName("PropDamageKill")]
    public bool PropDamageKill { get; set; } = true;

    [JsonPropertyName("SeekerDamagePerMiss")]
    public int SeekerDamagePerMiss { get; set; } = 5;

    [JsonPropertyName("SeekerWeapons")]
    public List<string> SeekerWeapons { get; set; } = new()
    {
        "weapon_knife",
        "weapon_p90",
        "weapon_deagle"
    };

    [JsonPropertyName("DefaultModels")]
    public List<string> DefaultModels { get; set; } = new()
    {
        "models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl",
        "models/props/de_inferno/claypot03.vmdl",
        "models/props/cs_office/trash_can.vmdl",
        "models/props/de_dust/hr_dust/dust_food_crate/dust_food_crate001.vmdl",
        "models/props/cs_office/chair_office.vmdl",
        "models/props/de_inferno/flower_barrel.vmdl",
        "models/props/cs_office/computer_monitor.vmdl",
        "models/props/de_dust/hr_dust/dust_metal_door/dust_metal_door001.vmdl"
    };

    // ── Key Bindings ────────────────────────────────────────
    // Saklanan oyuncular silah tasimadigi icin tuslar bos.
    // Kullanilabilir degerler: "Attack", "Attack2", "Use", "Reload", "None"
    // "None" = tus atanmaz (sadece chat komutu ile kullanilir)

    [JsonPropertyName("KeyTaunt")]
    public string KeyTaunt { get; set; } = "Attack";

    [JsonPropertyName("KeySwap")]
    public string KeySwap { get; set; } = "Attack2";

    [JsonPropertyName("KeyFreeze")]
    public string KeyFreeze { get; set; } = "Use";

    [JsonPropertyName("KeyDecoy")]
    public string KeyDecoy { get; set; } = "Reload";
}
