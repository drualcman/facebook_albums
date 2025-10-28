namespace facebook_albums.Core;

public class FacebookAlbumExtractor
{
    private readonly AlbumProcessor _processor = new();
    private readonly ConcurrentBag<AlbumResult> _results = new();
    private int _processed = 0;
    private int _images = 0;
    private int _errors = 0;
    private readonly object _lock = new();

    public int Processed => _processed;
    public int Images => _images;

    public (ConcurrentBag<AlbumResult> results, int totalProcessed, int totalImages, int totalErrors)
        ExtractAlbums(string[] htmlFiles, string albumFolder, string mediaFolder)
    {
        Parallel.ForEach(htmlFiles, new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 4)
        }, htmlPath =>
        {
            try
            {
                var result = _processor.Process(htmlPath, albumFolder, mediaFolder);
                if (result.ImageCount > 0)
                    _results.Add(result);

                lock (_lock)
                {
                    _processed++;
                    _images += result.ImageCount;
                }
            }
            catch
            {
                lock (_lock)
                    _errors++;
            }
        });

        return (_results, _processed, _images, _errors);
    }

    public void WaitForCompletion() { } // No es necesario, pero para compatibilidad
}
