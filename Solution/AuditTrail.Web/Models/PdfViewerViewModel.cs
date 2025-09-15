using System;

namespace AuditTrail.Web.Models
{
    public class PdfViewerViewModel
    {
        public Guid FileId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileSizeFormatted => FormatFileSize(FileSize);
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }
        public string FilePath { get; set; } = string.Empty;
        
        // Additional metadata
        public DateTime? ModifiedDate { get; set; }
        public string ModifiedBy { get; set; } = string.Empty;
        public int Version { get; set; } = 1;
        public string FileExtension { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string FolderPath { get; set; } = string.Empty;
        public string Checksum { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
        
        // Computed properties
        public string LastUpdatedBy => !string.IsNullOrEmpty(ModifiedBy) ? ModifiedBy : UploadedBy;
        public DateTime LastUpdatedDate => ModifiedDate ?? UploadedDate;
        public string FileType => GetFileTypeName(FileExtension);

        private string FormatFileSize(long bytes)
        {
            if (bytes == 0) return "0 Bytes";
            
            string[] sizes = { "Bytes", "KB", "MB", "GB" };
            int i = (int)Math.Floor(Math.Log(bytes) / Math.Log(1024));
            return Math.Round(bytes / Math.Pow(1024, i), 2) + " " + sizes[i];
        }
        
        private string GetFileTypeName(string extension)
        {
            return extension?.ToLower() switch
            {
                ".pdf" => "Documento PDF",
                ".doc" or ".docx" => "Documento Word",
                ".xls" or ".xlsx" => "Hoja de Excel",
                ".ppt" or ".pptx" => "Presentación PowerPoint",
                ".txt" => "Archivo de Texto",
                _ => "Documento"
            };
        }
    }
}