namespace facebook_albums.Core;
public class FileNameSanitizer
{
    public string Sanitize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "";

        name = name.Replace("...", "_").Replace("..", "_");
        var invalid = Path.GetInvalidFileNameChars()
                          .Concat(Path.GetInvalidPathChars())
                          .Concat(new[] { '*', '?', '"', '<', '>', '|' });
        name = invalid.Aggregate(name, (c, ch) => c.Replace(ch, '_'));
        name = Regex.Replace(name, @"\s+", " ").Trim();
        return name.Length > 180 ? name.Substring(0, 180) : name;
    }

    public string GetUniqueName(string folder, string baseName, string extension)
    {
        string name = baseName;
        string path = Path.Combine(folder, name + extension);
        int count = 1;
        while (File.Exists(path))
        {
            name = $"{baseName} ({count})";
            path = Path.Combine(folder, name + extension);
            count++;
        }
        return name + extension;
    }
}