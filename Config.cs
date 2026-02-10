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

    [JsonPropertyName("PropHealthSmall")]
    public int PropHealthSmall { get; set; } = 30;

    [JsonPropertyName("PropHealthMedium")]
    public int PropHealthMedium { get; set; } = 100;

    [JsonPropertyName("PropHealthLarge")]
    public int PropHealthLarge { get; set; } = 300;

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
        // Kucuk
        "models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl",
        "models/props/de_inferno/claypot02.vmdl",
        "models/props/de_inferno/claypot03.vmdl",
        "models/props/de_inferno/pot_big.vmdl",
        "models/props/de_dust/hr_dust/dust_pottery/dust_pottery_02.vmdl",
        "models/props/de_dust/hr_dust/dust_pottery/dust_pottery_03.vmdl",
        "models/props_junk/garbage_plasticbottle001a.vmdl",
        "models/props_junk/garbage_metalcan001a.vmdl",
        "models/props_junk/garbage_metalcan002a.vmdl",
        "models/props_junk/popcan01a.vmdl",
        "models/props_junk/shoe001a.vmdl",
        "models/props_junk/garbage_bag001a.vmdl",
        "models/props/de_dust/hr_dust/dust_rusty_bucket/dust_rusty_bucket.vmdl",
        // Orta
        "models/props/de_dust/hr_dust/dust_food_crate/dust_food_crate001.vmdl",
        "models/props/de_dust/hr_dust/dust_crates/dust_crate_style_01_small.vmdl",
        "models/props/cs_office/trash_can.vmdl",
        "models/props/cs_office/chair_office.vmdl",
        "models/props/cs_office/computer_monitor.vmdl",
        "models/props/cs_office/fire_extinguisher.vmdl",
        "models/props/cs_office/plant01.vmdl",
        "models/props/cs_office/microwave.vmdl",
        "models/props/de_inferno/flower_barrel.vmdl",
        "models/props/de_inferno/de_inferno_bucket.vmdl",
        "models/props_junk/plasticcrate01a.vmdl",
        "models/props_junk/wood_crate001a.vmdl",
        "models/props_junk/wood_crate002a.vmdl",
        "models/props_junk/cardboard_box001a.vmdl",
        "models/props_junk/cardboard_box002a.vmdl",
        "models/props_junk/cardboard_box003a.vmdl",
        "models/props_junk/cardboard_box004a.vmdl",
        "models/props/de_dust/hr_dust/dust_oil_drum/dust_oil_drum.vmdl",
        "models/props/de_dust/hr_dust/dust_tire/dust_tire.vmdl",
        "models/props/de_dust/hr_dust/dust_gas_can/dust_gas_can.vmdl",
        "models/props/de_dust/hr_dust/dust_wooden_barrel/dust_wooden_barrel.vmdl",
        "models/props_c/emachine01.vmdl",
        // Buyuk
        "models/props/de_dust/hr_dust/dust_metal_door/dust_metal_door001.vmdl",
        "models/props/de_dust/hr_dust/dust_crates/dust_crate_style_01_large.vmdl",
        "models/props_junk/wood_pallet001a.vmdl",
        "models/props/de_inferno/hr_i/inferno_wood_pile/inferno_wood_pile.vmdl",
        "models/props/de_inferno/bench_wood.vmdl",
        "models/props/cs_office/sofa.vmdl",
        "models/props/cs_office/bookshelf1.vmdl",
        "models/props/cs_office/vending_machine01.vmdl"
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
