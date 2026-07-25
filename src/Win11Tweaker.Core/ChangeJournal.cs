using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Win32;

namespace Win11Tweaker.Core;

public sealed class JournalRecord
{
    public required string Id { get; init; }

    public required DateTimeOffset At { get; init; }

    public required string TweakId { get; init; }

    public required string TweakTitle { get; init; }

    public required RegistryRoot Root { get; init; }

    public required string Key { get; init; }

    public string? Name { get; init; }

    public RegistryValueKind Kind { get; init; }

    public bool PresenceOnly { get; init; }

    public string? Before { get; init; }

    public string? After { get; init; }

    public bool Reverted { get; set; }

    [JsonIgnore]
    public string Target => (Root == RegistryRoot.CurrentUser ? "HKCU\\" : "HKLM\\")
        + Key + (Name is null ? string.Empty : "\\" + Name);

    public RegistryWrite ToWrite() => new()
    {
        Root = Root,
        Key = Key,
        Name = Name,
        Kind = Kind,
        PresenceOnly = PresenceOnly,
        OnValue = string.Empty
    };
}

public sealed class ChangeJournal
{
    static readonly JsonSerializerOptions Format = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    readonly string path;
    readonly List<JournalRecord> records;

    public ChangeJournal(string? overridePath = null)
    {
        path = overridePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Win11Tweaker",
            "journal.json");

        records = Load();
    }

    public IReadOnlyList<JournalRecord> Records => records;

    public void Append(TweakDefinition tweak, IReadOnlyList<Change> changes)
    {
        foreach (var change in changes)
            records.Add(new JournalRecord
            {
                Id = Guid.NewGuid().ToString("n"),
                At = DateTimeOffset.Now,
                TweakId = tweak.Id,
                TweakTitle = tweak.Title,
                Root = change.Write.Root,
                Key = change.Write.Key,
                Name = change.Write.Name,
                Kind = change.Write.Kind,
                PresenceOnly = change.Write.PresenceOnly,
                Before = change.Before,
                After = change.After
            });

        Save();
    }

    public void Revert(JournalRecord record)
    {
        if (record.Reverted)
            return;

        RegistryTweakEngine.Restore(record.ToWrite(), record.Before);
        record.Reverted = true;
        Save();
    }

    public IEnumerable<JournalRecord> Recent(int limit = 200)
        => records.AsEnumerable().Reverse().Take(limit);

    List<JournalRecord> Load()
    {
        if (!File.Exists(path))
            return [];

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<List<JournalRecord>>(json, Format) ?? [];
        }
        catch (Exception)
        {
            var salvage = path + ".broken-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            try { File.Move(path, salvage); } catch (IOException) { }
            return [];
        }
    }

    void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var staging = path + ".tmp";
        File.WriteAllText(staging, JsonSerializer.Serialize(records, Format));
        File.Move(staging, path, overwrite: true);
    }
}
