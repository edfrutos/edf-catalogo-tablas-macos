using Microsoft.AspNetCore.Components.Forms;

namespace EDFCatalogoTablasNet.Services
{
    /// <summary>
    /// Servicio para gestionar la carga y almacenamiento de archivos
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Guarda un archivo subido y devuelve la ruta relativa
        /// </summary>
        /// <param name="file">Archivo a guardar</param>
        /// <returns>Ruta relativa del archivo guardado</returns>
        Task<string> SaveFileAsync(IBrowserFile file);

        /// <summary>
        /// Elimina un archivo del sistema
        /// </summary>
        /// <param name="filePath">Ruta del archivo a eliminar</param>
        /// <returns>True si se eliminó correctamente</returns>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// Verifica si un archivo existe
        /// </summary>
        /// <param name="filePath">Ruta del archivo</param>
        /// <returns>True si el archivo existe</returns>
        bool FileExists(string filePath);

        /// <summary>
        /// Obtiene el tipo de contenido de un archivo
        /// </summary>
        /// <param name="fileName">Nombre del archivo</param>
        /// <returns>Tipo de contenido MIME</returns>
        string GetContentType(string fileName);

        /// <summary>
        /// Valida si el archivo es válido para el tipo especificado
        /// </summary>
        /// <param name="file">Archivo a validar</param>
        /// <param name="fileType">Tipo de archivo (documento, multimedia, imagen)</param>
        /// <returns>True si es válido</returns>
        bool IsValidFile(IBrowserFile file, string fileType);
    }
}