using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using System.Drawing;

namespace PropHunt;

public enum PropSize
{
    Small,
    Medium,
    Large
}

public static class Utils
{
    // ── Prop boyut siniflandirmasi ─────────────────────────────
    // Model dosya yolundaki anahtar kelimelere gore boyut belirlenir.

    private static readonly HashSet<string> SmallKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "bottle", "can001", "can002", "popcan", "shoe", "soccer_ball", "soccerball",
        "pottery", "claypot", "pot_big", "bucket", "bell", "hard_hat", "hat",
        "cone", "bag001", "wine_bottle", "coffee_mug", "phone", "keyboard",
        "paper_towels", "snowman", "fuel_can", "gas_can", "fire_extinguisher",
        "toolbox", "fruit", "garbage_bag", "garbage_metal", "garbage_plastic",
        "clay_pot", "stone_vase", "wooden_bucket"
    };

    private static readonly HashSet<string> LargeKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "door", "pallet", "wood_pile", "sofa", "bookshelf", "vending_machine",
        "dumpster", "wagon", "desk", "table", "shelves", "whiteboard",
        "cart", "locker", "carpet", "satellite_dish", "clothes_line",
        "bench", "file_cabinet_tall", "crate_style_01_large", "market_cart",
        "wheelbarrow", "cement_bag"
    };

    /// <summary>
    /// Model yoluna bakarak prop boyutunu belirler.
    /// </summary>
    public static PropSize ClassifyPropSize(string modelPath)
    {
        string lower = modelPath.ToLowerInvariant();

        foreach (var keyword in SmallKeywords)
        {
            if (lower.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return PropSize.Small;
        }

        foreach (var keyword in LargeKeywords)
        {
            if (lower.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return PropSize.Large;
        }

        return PropSize.Medium;
    }

    /// <summary>
    /// Prop boyutuna gore can degerini dondurur.
    /// </summary>
    public static int GetHealthForPropSize(PropSize size, int smallHp, int mediumHp, int largeHp)
    {
        return size switch
        {
            PropSize.Small => smallHp,
            PropSize.Large => largeHp,
            _ => mediumHp
        };
    }

    /// <summary>
    /// Parses a team string ("T", "CT") into a CsTeam enum.
    /// </summary>
    public static CsTeam ParseTeam(string team)
    {
        return team.Trim().ToUpper() switch
        {
            "T" => CsTeam.Terrorist,
            "CT" => CsTeam.CounterTerrorist,
            "TERRORIST" => CsTeam.Terrorist,
            "COUNTERTERRORIST" => CsTeam.CounterTerrorist,
            _ => CsTeam.None
        };
    }

    /// <summary>
    /// Gets the opposing team.
    /// </summary>
    public static CsTeam OppositeTeam(CsTeam team)
    {
        return team == CsTeam.Terrorist ? CsTeam.CounterTerrorist : CsTeam.Terrorist;
    }

    /// <summary>
    /// Check if a player is valid and alive.
    /// </summary>
    public static bool IsValidPlayer(CCSPlayerController? player)
    {
        return player != null
            && player.IsValid
            && !player.IsBot
            && !player.IsHLTV
            && player.PlayerPawn.IsValid
            && player.PlayerPawn.Value != null
            && player.PlayerPawn.Value.IsValid;
    }

    /// <summary>
    /// Check if a player is alive.
    /// </summary>
    public static bool IsPlayerAlive(CCSPlayerController player)
    {
        return player.PawnIsAlive && player.PlayerPawn.Value?.LifeState == (byte)LifeState_t.LIFE_ALIVE;
    }

    /// <summary>
    /// Freeze a player in place.
    /// </summary>
    public static void FreezePlayer(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        pawn.MoveType = MoveType_t.MOVETYPE_OBSOLETE;
        pawn.ActualMoveType = MoveType_t.MOVETYPE_OBSOLETE;
        Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 0);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
    }

    /// <summary>
    /// Unfreeze a player.
    /// </summary>
    public static void UnfreezePlayer(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        pawn.MoveType = MoveType_t.MOVETYPE_WALK;
        pawn.ActualMoveType = MoveType_t.MOVETYPE_WALK;
        Schema.SetSchemaValue(pawn.Handle, "CBaseEntity", "m_nActualMoveType", 2);
        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_MoveType");
    }

    /// <summary>
    /// Make a player's model invisible.
    /// </summary>
    public static void MakeInvisible(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        pawn.Render = Color.FromArgb(0, 255, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    /// <summary>
    /// Make a player's model visible again.
    /// </summary>
    public static void MakeVisible(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        pawn.Render = Color.FromArgb(255, 255, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
    }

    /// <summary>
    /// Set player health.
    /// </summary>
    public static void SetHealth(CCSPlayerController player, int health)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        player.Health = health;
        pawn.Health = health;

        if (health > 100)
        {
            player.MaxHealth = health;
            pawn.MaxHealth = health;
        }

        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
    }

    /// <summary>
    /// Disable shadow for a player.
    /// </summary>
    public static void DisableShadow(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        pawn.ShadowStrength = 0f;
    }

    /// <summary>
    /// Enable shadow for a player.
    /// </summary>
    public static void EnableShadow(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null) return;

        pawn.ShadowStrength = 1f;
    }

    /// <summary>
    /// Calculate position in front of a player.
    /// </summary>
    public static Vector CalculatePositionInFront(CCSPlayerController player, float offsetXY, float offsetZ = 0)
    {
        var pawn = player.PlayerPawn.Value!;
        float yaw = pawn.EyeAngles.Y;
        float yawRad = (float)(yaw * Math.PI / 180.0);

        float offsetX = offsetXY * (float)Math.Cos(yawRad);
        float offsetY = offsetXY * (float)Math.Sin(yawRad);

        return new Vector(
            pawn.AbsOrigin!.X + offsetX,
            pawn.AbsOrigin!.Y + offsetY,
            pawn.AbsOrigin!.Z + offsetZ
        );
    }

    /// <summary>
    /// Load models for a specific map from the models directory.
    /// Falls back to default models from config if no map file exists.
    /// </summary>
    public static List<string> LoadMapModels(string moduleDirectory, string mapName, List<string> defaultModels)
    {
        var models = new List<string>();

        // Try loading map-specific model list
        string mapFile = Path.Combine(moduleDirectory, "models", $"{mapName}.txt");
        if (File.Exists(mapFile))
        {
            try
            {
                foreach (string line in File.ReadAllLines(mapFile))
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("//") || trimmed.StartsWith("#"))
                        continue;

                    if (trimmed.EndsWith(".vmdl", StringComparison.OrdinalIgnoreCase))
                        models.Add(trimmed);
                }
            }
            catch (Exception) { }
        }

        // Try JSON format as well
        if (models.Count == 0)
        {
            string jsonFile = Path.Combine(moduleDirectory, "models", $"{mapName}.json");
            if (File.Exists(jsonFile))
            {
                try
                {
                    string json = File.ReadAllText(jsonFile);
                    var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                    if (dict != null)
                    {
                        models.AddRange(dict.Values);
                    }
                }
                catch (Exception) { }
            }
        }

        // Fallback to default models from config
        if (models.Count == 0)
        {
            models.AddRange(defaultModels);
        }

        return models;
    }

    /// <summary>
    /// Shuffle a list in place using Fisher-Yates algorithm.
    /// </summary>
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Shared.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
