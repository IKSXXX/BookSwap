namespace BookExchange.Web.Helpers;

public static class ImageHelper
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
    private const long MaxSize = 5 * 1024 * 1024;

    public static async Task<string?> SaveAsync(IFormFile? file, IWebHostEnvironment env, string subFolder)
    {
        if (file == null || file.Length == 0) return null;
        if (file.Length > MaxSize) return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext)) return null;

        var uploadsDir = Path.Combine(env.WebRootPath, subFolder);
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/{subFolder}/{fileName}";
    }
}
