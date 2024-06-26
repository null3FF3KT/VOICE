public class FFmpegBinariesHelper
{
    internal static string GetFFmpegPath()
    {
        if (Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT") == "Development")
        {
            // Local development: use system FFmpeg
            return "ffmpeg";
        }
        else
        {
            // Azure environment: use the copied FFmpeg binary
            var assemblyLocation = typeof(FFmpegBinariesHelper).Assembly.Location;
            var assemblyDir = Path.GetDirectoryName(assemblyLocation);
						if(assemblyDir == null) throw new ApplicationException("Assembly directory is null");
            return Path.Combine(assemblyDir, "FFmpeg", "linux-x64", "ffmpeg");
        }
    }
}