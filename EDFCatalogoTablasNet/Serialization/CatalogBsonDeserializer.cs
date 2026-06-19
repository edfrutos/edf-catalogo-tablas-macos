using System.Globalization;
using EDFCatalogoTablasNet.Models;
using EDFCatalogoTablasNet;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace EDFCatalogoTablasNet.Serialization;

/// <summary>
/// Deserialización tolerante de catálogos BSON legacy (camelCase/PascalCase, fechas como string, rows con forma distinta).
/// </summary>
internal static class CatalogBsonDeserializer
{
    internal static Catalog? TryDeserialize(BsonDocument source, ILogger logger)
    {
        var doc = new BsonDocument(source);

        var created = ExtractDateTime(doc, "createdAt", "CreatedAt");
        var updated = ExtractDateTime(doc, "updatedAt", "UpdatedAt");

        foreach (var key in new[] { "createdAt", "CreatedAt", "updatedAt", "UpdatedAt" })
            doc.Remove(key);

        // Filas legacy a menudo rompen el mapeo a List<CatalogRow>
        doc.Remove("rows");
        doc.Remove("Rows");

        Catalog? catalog;
        try
        {
            catalog = BsonSerializer.Deserialize<Catalog>(doc);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[CatalogBson] Deserialización base fallida para _id={Id}", source.GetValue("_id", BsonNull.Value));
            return null;
        }

        if (catalog == null)
            return null;

        if (created.HasValue)
            catalog.CreatedAt = created.Value;
        if (updated.HasValue)
            catalog.UpdatedAt = updated.Value;

        ApplyFieldPatches(source, catalog);
        // Las filas se quitan del doc antes de Deserialize<Catalog> para evitar fallos en formas legacy;
        // hay que rehidratarlas desde el BSON original o UpdateRow/DeleteRow y la UI verían Rows vacío.
        TryApplyRowsFromSource(source, catalog, logger);
        return catalog;
    }

    private static void TryApplyRowsFromSource(BsonDocument source, Catalog catalog, ILogger logger)
    {
        if (!TryGetRowsBsonArray(source, out var rowsArr) || rowsArr.Count == 0)
            return;

        var list = new List<CatalogRow>();
        foreach (var el in rowsArr)
        {
            if (!el.IsBsonDocument)
                continue;
            var bd = el.AsBsonDocument;
            try
            {
                list.Add(BsonSerializer.Deserialize<CatalogRow>(bd));
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "[CatalogBson] Fila no mapeable a CatalogRow, se omite");
            }
        }

        if (list.Count > 0)
            catalog.Rows = list;
    }

    private static bool TryGetRowsBsonArray(BsonDocument source, out BsonArray rowsArr)
    {
        rowsArr = [];
        foreach (var key in new[] { "rows", "Rows" })
        {
            if (!source.TryGetValue(key, out var v) || !v.IsBsonArray)
                continue;
            rowsArr = v.AsBsonArray;
            return true;
        }

        return false;
    }

    private static void ApplyFieldPatches(BsonDocument doc, Catalog c)
    {
        if (string.IsNullOrWhiteSpace(c.Name))
        {
            var n = GetString(doc, "name", "Name");
            if (n != null) c.Name = n;
        }

        if (string.IsNullOrWhiteSpace(c.Description))
        {
            var d = GetString(doc, "description", "Description");
            if (d != null) c.Description = d;
        }

        if (string.IsNullOrWhiteSpace(c.Category))
        {
            var cat = GetString(doc, "category", "Category");
            if (cat != null) c.Category = cat;
        }

        if (string.IsNullOrWhiteSpace(c.CreatedBy))
        {
            var cb = GetString(doc, "createdBy", "CreatedBy");
            if (cb != null) c.CreatedBy = cb;
        }

        if (string.IsNullOrWhiteSpace(c.Owner))
        {
            var ow = GetString(doc, "owner", "Owner");
            c.Owner = ow ?? c.CreatedBy;
        }

        if (c.Headers == null || c.Headers.Length == 0)
        {
            foreach (var key in new[] { "columns", "Columns", "headers", "Headers" })
            {
                if (!TryGetStringArray(doc, key, out var arr))
                    continue;
                c.Headers = arr;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(c.DocumentoUrl))
        {
            var u = GetString(doc, "documentoUrl", "DocumentoUrl");
            if (u != null) c.DocumentoUrl = u;
        }

        if (string.IsNullOrWhiteSpace(c.MultimediaUrl))
        {
            var u = GetString(doc, "multimediaUrl", "MultimediaUrl");
            if (u != null) c.MultimediaUrl = u;
        }

        if (string.IsNullOrWhiteSpace(c.ImagenUrl))
        {
            var u = GetString(doc, "imagenUrl", "ImagenUrl");
            if (u != null) c.ImagenUrl = u;
        }

        if (string.IsNullOrWhiteSpace(c.Miniatura))
        {
            var u = GetString(doc, "miniatura", "Miniatura");
            if (u != null) c.Miniatura = u;
        }
    }

    private static string? GetString(BsonDocument doc, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!doc.TryGetValue(key, out var v))
                continue;
            if (v.IsString) return v.AsString;
            if (!v.IsBsonNull) return v.ToString();
        }

        return null;
    }

    private static bool TryGetStringArray(BsonDocument doc, string key, out string[] value)
    {
        value = [];
        if (!doc.TryGetValue(key, out var v) || !v.IsBsonArray)
            return false;
        var arr = v.AsBsonArray;
        if (arr.Count == 0)
            return false;
        value = arr.Select(e => e.IsString ? e.AsString : e.ToString() ?? string.Empty).ToArray();
        return value.Length > 0;
    }

    private static DateTime? ExtractDateTime(BsonDocument doc, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!doc.TryGetValue(key, out var v) || v.IsBsonNull)
                continue;
            switch (v.BsonType)
            {
                case BsonType.String:
                    if (DateTime.TryParse(v.AsString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                        return dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime();
                    break;
                case BsonType.DateTime:
                    return DateTime.SpecifyKind(v.ToUniversalTime(), DateTimeKind.Utc);
                case BsonType.Int64:
                    return DateTimeOffset.FromUnixTimeMilliseconds(v.AsInt64).UtcDateTime;
            }
        }

        return null;
    }

    internal static FilterDefinition<BsonDocument> BuildIdFilter(string id)
    {
        if (ObjectId.TryParse(id, out var oid))
            return Builders<BsonDocument>.Filter.Eq("_id", oid);
        return Builders<BsonDocument>.Filter.Eq("_id", id);
    }

    /// <summary>Filtro de acceso por usuario (variantes de nombre de campo en BSON legacy).</summary>
    internal static FilterDefinition<BsonDocument> BuildUserOwnershipFilter(string userEmail)
    {
        var esc = System.Text.RegularExpressions.Regex.Escape(userEmail);
        var rx = new BsonRegularExpression($"^{esc}$", "i");
        return Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Regex("CreatedBy", rx),
            Builders<BsonDocument>.Filter.Regex("createdBy", rx),
            Builders<BsonDocument>.Filter.Regex("Owner", rx),
            Builders<BsonDocument>.Filter.Regex("owner", rx));
    }

    internal static FilterDefinition<BsonDocument> BuildAccessFilter(string? userEmail, string? userRole)
    {
        if (RoleHelper.IsAdmin(userRole))
            return Builders<BsonDocument>.Filter.Empty;

        if (!string.IsNullOrEmpty(userEmail))
            return BuildUserOwnershipFilter(userEmail);

        return Builders<BsonDocument>.Filter.Eq("_id", ObjectId.Empty);
    }
}
