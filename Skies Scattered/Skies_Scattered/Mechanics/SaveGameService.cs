using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Skies_Scattered.UI;

namespace Skies_Scattered.Mechanics;

public sealed class SavedPosition
{
    public float X { get; set; }
    public float Y { get; set; }
    public SavedPosition() { }
    public SavedPosition(Vector2 value) { X = value.X; Y = value.Y; }
    public Vector2 ToVector() => new Vector2(X, Y);
}

public sealed class SaveGameData
{
    public int Version { get; set; } = 2;
    public string MapId { get; set; } = "mapa2";
    public HashSet<string> OpenedGifts { get; set; } = new();
    public bool EndingIntroSeen { get; set; }
    public string SavePointId { get; set; }
    public DateTimeOffset SavedAt { get; set; }
    public SavedPosition Yoko { get; set; }
    public SavedPosition Kyoko { get; set; }
    public List<CharacterStats> Party { get; set; }
    public List<string> Inventory { get; set; }
    public ChoiceSystem.Snapshot Choices { get; set; }
    public Dictionary<string, ZoneProgress> Zones { get; set; } = new() { ["mapa2"] = new ZoneProgress() };
}

public sealed class SaveGameService
{
    private readonly string _path;
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };
    private static readonly HashSet<string> KnownSpells = new HashSet<string> {
        "FireBeta", "FireGamma", "FreezeAlpha", "FreezeOmega", "DefenseDownOmega", "ShieldBeta"
    };
    public bool HasSave { get; private set; }
    public string LastError { get; private set; } = "";
    public string FilePath => _path;

    public SaveGameService(string path = null)
    {
        _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SkiesScattered", "save.json");
        TryLoad(out _);
    }

    public bool TrySave(SaveGameData data)
    {
        string temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Validate(data);
            data.Inventory = GameItem.ShareInventory(data.Party, data.Inventory);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_path)));
            File.WriteAllText(temporary, JsonSerializer.Serialize(data, JsonOptions));
            File.Move(temporary, _path, true);
            HasSave = true;
            LastError = "";
            return true;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
            error is JsonException || error is InvalidDataException)
        {
            LastError = "No se pudo guardar la partida.";
            return false;
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    public bool TryLoad(out SaveGameData data)
    {
        data = null;
        if (!File.Exists(_path)) { HasSave = false; LastError = "No hay una partida guardada."; return false; }
        try
        {
            var candidate = JsonSerializer.Deserialize<SaveGameData>(File.ReadAllText(_path), JsonOptions);
            if (candidate?.Version == 1) MigrateVersionOne(candidate);
            Validate(candidate);
            candidate.Inventory = GameItem.ShareInventory(candidate.Party, candidate.Inventory);
            data = candidate;
            HasSave = true;
            LastError = "";
            return true;
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
            error is JsonException || error is InvalidDataException)
        {
            HasSave = false;
            LastError = "No se pudo cargar: el guardado no es vÃ¡lido o no estÃ¡ disponible.";
            return false;
        }
    }

    private static void Validate(SaveGameData data)
    {
        if (data == null || data.Version != 2 || (data.MapId != "mapa2" && data.MapId != "mapa3") ||
            string.IsNullOrWhiteSpace(data.SavePointId) || data.Party == null || data.Party.Count != 2 ||
            !PositionValid(data.Yoko, data.MapId) || !PositionValid(data.Kyoko, data.MapId) || data.Choices == null || !data.Choices.IsValid())
            throw new InvalidDataException("Guardado incompatible.");
        if (data.Inventory != null && (data.Inventory.Count > 400 ||
            data.Inventory.Exists(item => string.IsNullOrWhiteSpace(item) || item.Length > 256)))
            throw new InvalidDataException("Inventario invalido.");
        if (data.OpenedGifts == null || data.OpenedGifts.Count > 6 || data.OpenedGifts.Any(id =>
            id == null || !Enumerable.Range(1, 6).Any(i => id == $"mapa2-regalo-{i:00}")))
            throw new InvalidDataException("Estado de regalos invalido.");
        if (data.Zones == null || !data.Zones.ContainsKey(data.MapId) || data.Zones.Count > 100 ||
            data.Zones.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Key.Length > 64 || pair.Value == null || !pair.Value.IsValid()))
            throw new InvalidDataException("Progreso de zona invÃ¡lido.");
        for (int i = 0; i < data.Party.Count; i++)
        {
            var hero = data.Party[i];
            if (hero == null || hero.Name != (i == 0 ? "Yoko" : "Kyoko") ||
                hero.MaxHP < 1 || hero.MaxHP > 999 || hero.HP < 0 || hero.HP > hero.MaxHP ||
                hero.MaxPP < 0 || hero.MaxPP > 999 || hero.PP < 0 || hero.PP > hero.MaxPP ||
                hero.AP < 0 || hero.AP > 1000000 || hero.CT < 0 || hero.CT > 1000000 ||
                hero.EnemiesDefeated < 0 || hero.EnemiesSpared < 0 ||
                hero.Items == null || hero.Items.Count > 400 || hero.Items.Exists(item => string.IsNullOrWhiteSpace(item) || item.Length > 256) ||
                hero.Spells == null || hero.Spells.Count > 20 || hero.Spells.Exists(spell => !KnownSpells.Contains(spell)))
                throw new InvalidDataException("EstadÃ­sticas invÃ¡lidas.");
        }
        if (data.Party.TrueForAll(hero => hero.HP <= 0)) throw new InvalidDataException("El grupo estÃ¡ derrotado.");
    }

    private static bool PositionValid(SavedPosition position, string mapId) => position != null &&
        float.IsFinite(position.X) && float.IsFinite(position.Y) &&
        position.X >= 0 && position.Y >= 0 && position.X < (mapId == "mapa3" ? 1536 : 2048) && position.Y < (mapId == "mapa3" ? 1472 : 2560);

    private static void MigrateVersionOne(SaveGameData data)
    {
        if (data.Party == null || data.Party.Count != 2 || data.Party.Exists(hero => hero == null))
            throw new InvalidDataException("Guardado antiguo invÃ¡lido.");
        var zone = new ZoneProgress();
        foreach (var hero in data.Party)
        {
            if (string.IsNullOrEmpty(hero.Name) || !zone.APGains.ContainsKey(hero.Name) || hero.EnemiesDefeated < 0 || hero.EnemiesSpared < 0)
                throw new InvalidDataException("Guardado antiguo invÃ¡lido.");
            int kills = Math.Min(ZoneProgress.Population - zone.Defeated, hero.EnemiesDefeated);
            zone.Defeated += kills;
            int ct = Math.Min(ZoneProgress.CatharsisLimitPerHero, Math.Min(hero.EnemiesSpared, ZoneProgress.GrowthBudget - zone.GrowthUsed));
            zone.CTGains[hero.Name] = ct;
            zone.APGains[hero.Name] = Math.Min(kills, ZoneProgress.GrowthBudget - zone.GrowthUsed);
            zone.Spared = (int)Math.Min(int.MaxValue, (long)zone.Spared + hero.EnemiesSpared);
        }
        data.Zones = new Dictionary<string, ZoneProgress> { [data.MapId ?? "mapa2"] = zone };
        data.Version = 2;
    }
}
