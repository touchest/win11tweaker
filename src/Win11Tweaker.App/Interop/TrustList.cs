using System.IO;
using System.Text.Json;

namespace Win11Tweaker.App.Interop;

public sealed class TrustList
{
    readonly string path;
    readonly HashSet<string> exceptions;

    public TrustList(string? overridePath = null)
    {
        path = overridePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Win11Tweaker", "trustlist.json");

        exceptions = Load();
    }

    public IReadOnlyCollection<string> Exceptions => exceptions;

    public bool IsException(string name) => exceptions.Contains(name);

    public void Add(string name)
    {
        if (exceptions.Add(name.Trim()))
            Save();
    }

    public void Remove(string name)
    {
        if (exceptions.Remove(name))
            Save();
    }

    HashSet<string> Load()
    {
        if (!File.Exists(path))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var saved = JsonSerializer.Deserialize<List<string>>(File.ReadAllText(path)) ?? [];
            return new HashSet<string>(saved, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var staging = path + ".tmp";
        File.WriteAllText(staging, JsonSerializer.Serialize(exceptions.ToList(),
            new JsonSerializerOptions { WriteIndented = true }));
        File.Move(staging, path, overwrite: true);
    }
}
