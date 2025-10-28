namespace facebook_albums.UI;

public class ProgressAnimator
{
    private readonly char[] _spinner = { '|', '/', '-', '\\' };
    private bool _running;
    private Task _task;
    private int _line;

    public void Start(int line, int totalAlbums, Func<(int processed, int images)> getStats)
    {
        _line = line;
        _running = true;

        _task = Task.Run(() =>
        {
            int index = 0;
            while (_running)
            {
                var (processed, images) = getStats();
                string status = $"Procesando... {_spinner[index]} ({processed}/{totalAlbums} álbumes, {images} imágenes)";
                lock (Console.Out)
                {
                    Console.SetCursorPosition(0, _line);
                    Console.Write(status.PadRight(Console.WindowWidth - 1));
                }
                index = (index + 1) % _spinner.Length;
                Thread.Sleep(200);
            }
        });
    }

    public void Stop()
    {
        _running = false;
        _task?.Wait(500);
        lock (Console.Out)
        {
            Console.SetCursorPosition(0, _line);
            Console.Write(new string(' ', Console.WindowWidth - 1));
            Console.SetCursorPosition(0, _line);
        }
    }
}