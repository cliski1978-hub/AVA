using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace AVA.Memory.Tests.Core.Utilities
{
    /// <summary>
    /// Provides structured access to Excel test plan sheets.
    /// Reads .xlsx workbooks and exposes rows as key/value dictionaries
    /// with typed accessors for test data parsing.
    /// </summary>
    internal sealed class ExcelDataReader : IDisposable
    {
        private readonly XLWorkbook _workbook;
        private bool _disposed;

        public ExcelDataReader(string excelPath)
        {
            if (string.IsNullOrWhiteSpace(excelPath))
                throw new ArgumentNullException(nameof(excelPath));

            if (!File.Exists(excelPath))
                throw new FileNotFoundException($"Excel input file not found: {excelPath}");

            _workbook = new XLWorkbook(excelPath);
        }

        /// <summary>
        /// Returns all worksheet names in the workbook.
        /// </summary>
        public IReadOnlyList<string> GetSheetNames()
        {
            return _workbook.Worksheets.Select(ws => ws.Name).ToList();
        }

        /// <summary>
        /// Reads all rows from a sheet as a list of ExcelRowData (key/value dictionary with helpers).
        /// The first row is assumed to be the header row.
        /// </summary>
        public List<ExcelRowData> ReadSheet(string sheetName)
        {
            if (string.IsNullOrWhiteSpace(sheetName))
                throw new ArgumentNullException(nameof(sheetName));

            var sheet = _workbook.Worksheet(sheetName);
            if (sheet == null)
                throw new ArgumentException($"Sheet '{sheetName}' not found in workbook.");

            var rows = new List<ExcelRowData>();
            var headers = sheet.FirstRowUsed().Cells().Select(c => c.GetValue<string>().Trim()).ToList();

            foreach (var row in sheet.RowsUsed().Skip(1)) // skip header
            {
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < headers.Count; i++)
                {
                    var header = headers[i];
                    var cell = row.Cell(i + 1);
                    dict[header] = cell?.GetValue<string>()?.Trim() ?? string.Empty;
                }
                rows.Add(new ExcelRowData(dict));
            }

            return rows;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _workbook?.Dispose();
            _disposed = true;
        }
    }

    /// <summary>
    /// Represents a single Excel row (key/value pair set) with
    /// typed accessors for convenience in test logic.
    /// </summary>
    internal sealed class ExcelRowData
    {
        private readonly Dictionary<string, string> _cells;

        public ExcelRowData(Dictionary<string, string> cells)
        {
            _cells = cells ?? throw new ArgumentNullException(nameof(cells));
        }

        public string GetString(string key)
        {
            if (_cells.TryGetValue(key, out var val))
                return val;
            return string.Empty;
        }

        public bool GetBoolOrDefault(string key, bool defaultValue = false)
        {
            if (_cells.TryGetValue(key, out var val) && bool.TryParse(val, out var b))
                return b;
            return defaultValue;
        }

        public int GetIntOrDefault(string key, int defaultValue = 0)
        {
            if (_cells.TryGetValue(key, out var val) && int.TryParse(val, out var i))
                return i;
            return defaultValue;
        }

        public float GetFloatOrDefault(string key, float defaultValue = 0.0f)
        {
            if (_cells.TryGetValue(key, out var val) &&
                float.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
                return f;
            return defaultValue;
        }

        public double GetDoubleOrDefault(string key, double defaultValue = 0.0)
        {
            if (_cells.TryGetValue(key, out var val) &&
                double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                return d;
            return defaultValue;
        }

        public string[]? GetCsvArray(string key)
        {
            if (_cells.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                return val.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return Array.Empty<string>();
        }

        public override string ToString()
        {
            return string.Join(", ", _cells.Select(kv => $"{kv.Key}={kv.Value}"));
        }
    }
}
