using System.Text;
using DeviceFX.NfcApp.Abstractions;
using DeviceFX.NfcApp.Model;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using SQLite;
using Cell = DocumentFormat.OpenXml.Spreadsheet.Cell;

namespace DeviceFX.NfcApp.Services;

public class InventoryService : IInventoryService
{
    private readonly SQLiteAsyncConnection database;
    public InventoryService()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "phoneInventory.db3");
        database = new SQLiteAsyncConnection(dbPath);
        database.CreateTableAsync<PhoneDetails>().Wait();
    }
    public async Task AddPhoneAsync(PhoneDetails phone, bool merge)
    {
        if (merge)
        {
            var existing = await database.FindAsync<PhoneDetails>(phone.Id);
            if (existing != null)
            {
                phone.Mode ??= existing.Mode;
                phone.ActivationCode ??= existing.ActivationCode;
                phone.DisplayName ??= existing.DisplayName;
                phone.DisplayNumber ??= existing.DisplayNumber;
            }
        }
        await database.InsertOrReplaceAsync(phone);
    }

    public async Task ClearAsync()
    {
        await database.DeleteAllAsync<PhoneDetails>();
    }

    public async Task<IList<PhoneDetails>> GetPhonesAsync()
    {
        return await database.Table<PhoneDetails>().OrderByDescending(p => p.Updated).ToListAsync();
    }
    
    public async Task<string?> ExportAsync(string format = "csv")
    {
        var records = await database.Table<PhoneDetails>().ToListAsync();
        if(records == null || records.Count == 0) return null;
        string fileName = $"Export_{DateTime.Now:yyyyMMddHHmmss}.{format}";
        string filePath = Path.Combine(FileSystem.Current.CacheDirectory, fileName);
        if (format == "csv")
        {
            // Manually create CSV content
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Id,Mac,Pid,WifiMac,Serial,Vid,TagSerial,NfcVersion,AssetTag,Mode,ActivationCode,Name,Number,Latitude,Longitude,Postcode,Country,Updated");
            foreach (var record in records)
            {
                sb.AppendLine($"\"{record.Id}\",\"{record.Mac}\",\"{record.Pid}\",\"{record.WifiMac}\",\"{record.Serial}\",\"{record.Vid}\",\"{record.TagSerial}\",\"{record.NfcVersion}\",\"{record.AssetTag}\",\"{record.Mode}\",\"{record.ActivationCode}\",\"{record.DisplayName}\",\"{record.DisplayNumber}\",\"{record.Latitude}\",\"{record.Longitude}\",\"{record.Postcode}\",\"{record.Country}\",\"{record.Updated:yyyy-MM-ddTHH:mm:ssZ}\"");
            }
            await File.WriteAllTextAsync(filePath, sb.ToString());
        }
        else if(format == "xlsx")
        {
            using (SpreadsheetDocument spreadsheetDocument =
                   SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
            {
                WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();
                workbookPart.Workbook = new Workbook();

                WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                worksheetPart.Worksheet = new Worksheet(new SheetData());

                Sheets sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
                Sheet sheet = new Sheet() { Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Sheet1" };
                sheets.Append(sheet);

               
                var stylesheet = new Stylesheet();
                
                var numberingFormats = new NumberingFormats();
                numberingFormats.Append(new NumberingFormat()
                {
                    NumberFormatId = 164,
                    FormatCode = StringValue.FromString("dd/mm/yyyy hh:mm:ss")
                });
                
                var cellFormats = new CellFormats();
                cellFormats.Append(new CellFormat());
                cellFormats.Append(new CellFormat()
                {
                    NumberFormatId = 164,
                    FontId = 0,
                    FillId = 0,
                    BorderId = 0,
                    FormatId = 0,
                    ApplyNumberFormat = BooleanValue.FromBoolean(true)
                });
                
                stylesheet.Append(cellFormats);
                stylesheet.Append(numberingFormats);
                
                var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
                stylesPart.Stylesheet = stylesheet;
                stylesPart.Stylesheet.Save();
                
                SheetData sheetData = worksheetPart.Worksheet.GetFirstChild<SheetData>();        
                Row headerRow = new Row();
                headerRow.Append(CreateCell("A1", "Id"));
                headerRow.Append(CreateCell("B1", "Mac"));
                headerRow.Append(CreateCell("C1", "Pid"));
                headerRow.Append(CreateCell("D1", "WifiMac"));
                headerRow.Append(CreateCell("E1", "Serial"));
                headerRow.Append(CreateCell("F1", "Vid"));
                headerRow.Append(CreateCell("G1", "TagSerial"));
                headerRow.Append(CreateCell("H1", "NfcVersion"));
                headerRow.Append(CreateCell("I1", "AssetTag"));
                headerRow.Append(CreateCell("J1", "Mode"));
                headerRow.Append(CreateCell("K1", "ActivationCode"));
                headerRow.Append(CreateCell("L1", "Name"));
                headerRow.Append(CreateCell("M1", "Number"));
                headerRow.Append(CreateCell("N1", "Latitude"));
                headerRow.Append(CreateCell("O1", "Longitude"));
                headerRow.Append(CreateCell("P1", "Postcode"));
                headerRow.Append(CreateCell("Q1", "Country"));
                headerRow.Append(CreateCell("R1", "Updated", CellValues.Date));
                sheetData.Append(headerRow);
                uint rowIndex = 2;
                foreach (var record in records)
                {
                    Row dataRow = new Row();
                    dataRow.Append(CreateCell($"A{rowIndex}", record.Id, CellValues.String));
                    dataRow.Append(CreateCell($"B{rowIndex}", record.Mac, CellValues.String));
                    dataRow.Append(CreateCell($"C{rowIndex}", record.Pid, CellValues.String));
                    if(record.WifiMac != null) dataRow.Append(CreateCell($"D{rowIndex}", record.WifiMac, CellValues.String));
                    dataRow.Append(CreateCell($"E{rowIndex}", record.Serial, CellValues.String));
                    if(record.WifiMac != null) dataRow.Append(CreateCell($"F{rowIndex}", record.Vid, CellValues.String));
                    if(record.TagSerial != null) dataRow.Append(CreateCell($"G{rowIndex}", record.TagSerial, CellValues.String));
                    if(record.NfcVersion != null) dataRow.Append(CreateCell($"H{rowIndex}", record.NfcVersion, CellValues.String));
                    if(record.AssetTag != null) dataRow.Append(CreateCell($"I{rowIndex}", record.AssetTag, CellValues.String));
                    if(record.Mode != null) dataRow.Append(CreateCell($"J{rowIndex}", record.Mode, CellValues.String));
                    if(record.ActivationCode != null) dataRow.Append(CreateCell($"K{rowIndex}", record.ActivationCode, CellValues.String));
                    if(record.DisplayName != null) dataRow.Append(CreateCell($"L{rowIndex}", record.DisplayName, CellValues.String));
                    if(record.DisplayNumber != null) dataRow.Append(CreateCell($"M{rowIndex}", record.DisplayNumber, CellValues.String));
                    if(record.Latitude != null) dataRow.Append(CreateCell($"N{rowIndex}", record.Latitude, CellValues.String));
                    if(record.Longitude != null) dataRow.Append(CreateCell($"O{rowIndex}", record.Longitude, CellValues.String));
                    if(record.Postcode != null) dataRow.Append(CreateCell($"P{rowIndex}", record.Postcode, CellValues.String));
                    if(record.Country != null) dataRow.Append(CreateCell($"Q{rowIndex}", record.Country, CellValues.String));
                    dataRow.Append(CreateDateCell($"R{rowIndex}", record.Updated));
                    sheetData.Append(dataRow);
                    rowIndex++;
                }
                workbookPart.Workbook.Save();
                spreadsheetDocument.Save();
            }
        }
        else return null;
        return filePath;
        Cell CreateCell(string cellReference, string value, CellValues? dataType = null)
        {
            return new Cell()
            {
                CellReference = cellReference,
                DataType = dataType ?? CellValues.String,
                CellValue = new CellValue(value)
            };
        }
        Cell CreateDateCell(string cellReference, DateTime value)
        {
            return new Cell()
            {
                CellReference = cellReference,
                DataType = CellValues.Date,
                CellValue = new CellValue(value),
                StyleIndex = 1
            };
        }
    }
}
