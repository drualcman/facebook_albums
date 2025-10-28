namespace facebook_albums.Core;

public class AlbumProcessor
{
    private readonly FileNameSanitizer _sanitizer = new();

    public AlbumResult Process(string htmlPath, string albumFolder, string mediaFolder)
    {
        var doc = new HtmlDocument();
        doc.Load(htmlPath);

        string title = _sanitizer.Sanitize(doc.DocumentNode.SelectSingleNode("//h1")?.InnerText.Trim() ?? "Álbum sin título");
        var sections = doc.DocumentNode.SelectNodes("//section[contains(@class, '_a6-g')]");

        if (sections == null || !sections.Any())
            return new AlbumResult(title, 0);

        string outputFolder = Path.Combine(albumFolder, title);
        if (!TryCreateDirectory(outputFolder))
            return new AlbumResult(title, 0);

        int saved = 0;
        int index = 1;

        foreach (var sec in sections)
        {
            try
            {
                string src = sec.SelectSingleNode(".//img")?.GetAttributeValue("src", "");
                string comment = sec.SelectSingleNode(".//div[contains(@class, '_3-95')]")?.InnerText.Trim();

                if (string.IsNullOrEmpty(src) || !src.Contains("your_facebook_activity"))
                    continue;

                string imagePath = Path.Combine(mediaFolder, src.Replace("your_facebook_activity/", "").Replace("posts/media/", ""));
                if (!File.Exists(imagePath))
                    continue;

                string fileName = !string.IsNullOrWhiteSpace(comment)
                    ? _sanitizer.Sanitize(comment)
                    : $"{title} - {index:D3}";

                string dest = Path.Combine(outputFolder, _sanitizer.GetUniqueName(outputFolder, fileName, Path.GetExtension(imagePath)));

                try
                {
                    File.Copy(imagePath, dest, true);
                    saved++;
                }
                catch
                {
                    // Silencioso: no interrumpe, solo no cuenta
                }

                index++;
            }
            catch
            {
                // Silencioso
            }
        }

        return new AlbumResult(title, saved);
    }

    private static bool TryCreateDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return Directory.Exists(path);
        }
        catch
        {
            return false;
        }
    }
}
