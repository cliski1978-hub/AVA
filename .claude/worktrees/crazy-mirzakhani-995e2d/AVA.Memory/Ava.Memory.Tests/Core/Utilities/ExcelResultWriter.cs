using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace AVA.Memory.Tests.Core.Utilities
{
    /// <summary>
    /// Provides structured Excel output for test results.
    /// Automatically creates workbook and sheets as needed,
    /// and appends test results row by row.
    /// </summary>
    internal sealed class ExcelResultWriter : IDisposable
    {
        private readonly string _path;
        private readonly XLWorkbook _workbook;
        private bool _disposed;

        public ExcelResultWriter(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentNullException(nameof(outputPath));

            _path = outputPath;

            if (File.Exists(outputPath))
            {
                _workbook = new XLWorkbook(outputPath);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                _workbook = new XLWorkbook();
            }
        }

        /// <summary>
        /// Appends a row of result data to a target worksheet.
        /// Automatically creates the sheet and headers if they do not exist.
        /// </summary>
        public async Task AppendRowAsync(string sheetName, object data)
        {
            if (string.IsNullOrWhiteSpace(sheetName))
                throw new ArgumentNullException(nameof(sheetName));

            if (data == null)
                throw new ArgumentNullException(nameof(data));

            var ws = _workbook.Worksheets.FirstOrDefault(s =>
                s.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase))
                ?? _workbook.AddWorksheet(sheetName);

            var props = data.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToList();

            // Create header row if not already there
            if (ws.FirstRowUsed() == null || ws.FirstRowUsed().Cell(1).IsEmpty())
            {
                for (int i = 0; i < props.Count; i++)
                    ws.Cell(1, i + 1).Value = props[i].Name;
            }

            // Determine the next available row
            var nextRow = ws.LastRowUsed()?.RowNumber() + 1 ?? 2;

            for (int i = 0; i < props.Count; i++)
            {
                var value = props[i].GetValue(data);

                var cell = ws.Cell(nextRow, i + 1);
                cell.Value = value switch
                {
                    null => string.Empty,
                    int v => v,
                    long v => v,
                    float v => v,
                    double v => v,
                    decimal v => v,
                    bool v => v,
                    DateTime v => v,
                    _ => value.ToString() ?? string.Empty
                };
            }


            await Task.Run(() => SaveWorkbookSafe());
        }

        /// <summary>
        /// Writes multiple rows in a single batch.
        /// </summary>
        public async Task AppendRowsAsync(string sheetName, IEnumerable<object> rows)
        {
            foreach (var row in rows)
                await AppendRowAsync(sheetName, row);
        }

        private void SaveWorkbookSafe()
        {
            try
            {
                _workbook.SaveAs(_path);
            }
            catch (IOException)
            {
                // Retry on file lock (Excel open)
                var tmpPath = Path.Combine(
                    Path.GetDirectoryName(_path)!,
                    $"{Path.GetFileNameWithoutExtension(_path)}_temp{Path.GetExtension(_path)}");
                _workbook.SaveAs(tmpPath);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            SaveWorkbookSafe();
            _workbook?.Dispose();
            _disposed = true;
        }
    }
}
