public static class FFmpegHelper
{
    public static void ThrowExceptionIfError(this int error)
    {
        if (error < 0)
        {
            throw new ApplicationException($"FFmpeg error: {error}");
        }
    }
}