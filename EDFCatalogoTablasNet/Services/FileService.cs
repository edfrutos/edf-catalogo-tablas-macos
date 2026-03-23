using Microsoft.AspNetCore.Components.Forms;

namespace EDFCatalogoTablasNet.Services
{
    /// <summary>
    /// Implementación del servicio de gestión de archivos
    /// </summary>
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string _uploadsPath;

        // Tamaños máximos permitidos (en bytes)
        private const long MaxImageSize = 5 * 1024 * 1024; // 5MB
        private const long MaxDocumentSize = 10 * 1024 * 1024; // 10MB
        private const long MaxMultimediaSize = 50 * 1024 * 1024; // 50MB

        // Extensiones permitidas por tipo
        private readonly string[] _imageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp", ".ico" };
        private readonly string[] _documentExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf", ".md", ".csv", ".json", ".xml" };
        private readonly string[] _multimediaExtensions = { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mp3", ".wav", ".ogg", ".m4a", ".aac", ".flac" };

        public FileService(IWebHostEnvironment environment)
        {
            _environment = environment;
            _uploadsPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "uploads");
            
            // Crear directorios si no existen
            EnsureDirectoriesExist();
        }

        public async Task<string> SaveFileAsync(IBrowserFile file)
        {
            try
            {
                // Generar nombre único para el archivo
                var extension = Path.GetExtension(file.Name).ToLowerInvariant();
                var fileName = $"{Guid.NewGuid()}{extension}";
                
                // Determinar subdirectorio basado en el tipo de archivo
                var subDirectory = GetSubDirectory(extension);
                var directoryPath = Path.Combine(_uploadsPath, subDirectory);
                
                // Asegurar que el directorio existe
                Directory.CreateDirectory(directoryPath);
                
                var filePath = Path.Combine(directoryPath, fileName);
                
                // Guardar el archivo
                await using var stream = new FileStream(filePath, FileMode.Create);
                await file.OpenReadStream(GetMaxSizeForExtension(extension)).CopyToAsync(stream);
                
                // Retornar ruta relativa
                return $"/uploads/{subDirectory}/{fileName}";
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error al guardar el archivo: {ex.Message}", ex);
            }
        }

        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                // Convertir ruta relativa a absoluta
                var absolutePath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, 
                    filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (File.Exists(absolutePath))
                {
                    await Task.Run(() => File.Delete(absolutePath));
                    return true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool FileExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var absolutePath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, 
                filePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                
            return File.Exists(absolutePath);
        }

        public string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                ".mov" => "video/quicktime",
                ".wmv" => "video/x-ms-wmv",
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".ogg" => "audio/ogg",
                _ => "application/octet-stream"
            };
        }

        public bool IsValidFile(IBrowserFile file, string fileType)
        {
            if (file == null)
                return false;

            var extension = Path.GetExtension(file.Name).ToLowerInvariant();
            var size = file.Size;

            return fileType.ToLower() switch
            {
                "imagen" => _imageExtensions.Contains(extension) && size <= MaxImageSize,
                "documento" => _documentExtensions.Contains(extension) && size <= MaxDocumentSize,
                "multimedia" => _multimediaExtensions.Contains(extension) && size <= MaxMultimediaSize,
                _ => false
            };
        }

        private void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(_uploadsPath);
            Directory.CreateDirectory(Path.Combine(_uploadsPath, "images"));
            Directory.CreateDirectory(Path.Combine(_uploadsPath, "documents"));
            Directory.CreateDirectory(Path.Combine(_uploadsPath, "multimedia"));
        }

        private string GetSubDirectory(string extension)
        {
            if (_imageExtensions.Contains(extension))
                return "images";
            if (_documentExtensions.Contains(extension))
                return "documents";
            if (_multimediaExtensions.Contains(extension))
                return "multimedia";
                
            return "others";
        }

        private long GetMaxSizeForExtension(string extension)
        {
            if (_imageExtensions.Contains(extension))
                return MaxImageSize;
            if (_documentExtensions.Contains(extension))
                return MaxDocumentSize;
            if (_multimediaExtensions.Contains(extension))
                return MaxMultimediaSize;
                
            return MaxDocumentSize; // Por defecto
        }
    }
}