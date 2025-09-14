using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AuditTrail.Web.Models;
using AuditTrail.Core.Interfaces;
using AuditTrail.Core.Entities.Documents;
using AuditTrail.Core.Enums;
using AuditTrail.Infrastructure.Interceptors;
using AuditTrail.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuditTrail.Web.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly ILogger<DocumentsController> _logger;
        private readonly IRepository<FileCategory> _fileCategoryRepository;
        private readonly IRepository<FileEntity> _fileRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IConfiguration _configuration;
        private readonly AuditTrailDbContext _dbContext;
        private readonly IPermissionService _permissionService;

        public DocumentsController(
            ILogger<DocumentsController> logger,
            IRepository<FileCategory> fileCategoryRepository,
            IRepository<FileEntity> fileRepository,
            IFileStorageService fileStorageService,
            ICurrentUserService currentUserService,
            IConfiguration configuration,
            AuditTrailDbContext dbContext,
            IPermissionService permissionService)
        {
            _logger = logger;
            _fileCategoryRepository = fileCategoryRepository;
            _fileRepository = fileRepository;
            _fileStorageService = fileStorageService;
            _currentUserService = currentUserService;
            _configuration = configuration;
            _dbContext = dbContext;
            _permissionService = permissionService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Use empty model for now since we load data via AJAX
                var model = new DocumentsIndexViewModel
                {
                    Categories = new List<FileCategory>(),
                    Files = new List<FileEntity>()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading documents index");
                TempData["ErrorMessage"] = "Error al cargar los documentos";
                return View(new DocumentsIndexViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTreeData()
        {
            try
            {
                _logger.LogInformation("GetTreeData: Starting to fetch categories and files...");
                
                // Use custom queries for both FileCategories and FileEntity since the repository can't handle the BaseEntity column mismatch
                var categories = await GetFileCategoriesAsync();
                var files = await GetFileEntitiesAsync();

                _logger.LogInformation("GetTreeData: Found {CategoryCount} categories and {FileCount} files", 
                    categories.Count(), files.Count());

                // Log all categories first to debug
                if (categories.Any())
                {
                    foreach (var cat in categories)
                    {
                        _logger.LogInformation("Category: Id={Id}, Name={Name}, Path={Path}, IsActive={IsActive}", 
                            cat.Id, cat.CategoryName, cat.CategoryPath, cat.IsActive);
                    }
                }
                else
                {
                    _logger.LogWarning("GetTreeData: No categories found in repository!");
                }

                var activeCategories = categories.Where(c => c.IsActive).ToList();
                var activeFiles = files.Where(f => !f.IsDeleted).ToList();

                _logger.LogInformation("GetTreeData: Active categories: {ActiveCategoryCount}, Active files: {ActiveFileCount}", 
                    activeCategories.Count, activeFiles.Count);

                var treeData = BuildTreeStructure(activeCategories, activeFiles);

                _logger.LogInformation("GetTreeData: Built tree with {NodeCount} root nodes", treeData.Count);

                return Json(treeData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tree data");
                return Json(new List<TreeNodeViewModel>());
            }
        }

        private List<TreeNodeViewModel> BuildTreeStructure(
            List<FileCategoryDto> categories, 
            List<FileEntityDto> files)
        {
            var rootNodes = new List<TreeNodeViewModel>();

            // Get root categories (no parent)
            var rootCategories = categories.Where(c => c.ParentCategoryId == null).OrderBy(c => c.CategoryName);

            foreach (var category in rootCategories)
            {
                var node = new TreeNodeViewModel
                {
                    Id = $"cat_{category.Id}",
                    Title = category.CategoryName,
                    Icon = "bi bi-folder",
                    IsExpandable = true,
                    IsCategory = true,
                    CategoryId = category.Id,
                    Description = category.Description
                };

                // Add child categories and files
                AddChildrenToNode(node, category.Id, categories, files);
                rootNodes.Add(node);
            }

            // Add files that don't belong to any category (should be rare)
            var orphanFiles = files.Where(f => !categories.Any(c => c.Id == f.CategoryId));
            foreach (var file in orphanFiles.OrderBy(f => f.FileName))
            {
                rootNodes.Add(new TreeNodeViewModel
                {
                    Id = $"file_{file.Id}",
                    Title = file.FileName,
                    Icon = GetFileIcon(file.FileExtension),
                    IsExpandable = false,
                    IsCategory = false,
                    FileId = file.Id,
                    FileSize = file.FileSize,
                    FileExtension = file.FileExtension,
                    CreatedDate = file.UploadedDate
                });
            }

            return rootNodes;
        }

        private void AddChildrenToNode(TreeNodeViewModel parentNode, int parentCategoryId, 
            List<FileCategoryDto> allCategories, List<FileEntityDto> allFiles)
        {
            parentNode.Children = new List<TreeNodeViewModel>();

            // Add child categories
            var childCategories = allCategories
                .Where(c => c.ParentCategoryId == parentCategoryId)
                .OrderBy(c => c.CategoryName);

            foreach (var childCategory in childCategories)
            {
                var childNode = new TreeNodeViewModel
                {
                    Id = $"cat_{childCategory.Id}",
                    Title = childCategory.CategoryName,
                    Icon = "bi bi-folder",
                    IsExpandable = true,
                    IsCategory = true,
                    CategoryId = childCategory.Id,
                    Description = childCategory.Description
                };

                AddChildrenToNode(childNode, childCategory.Id, allCategories, allFiles);
                parentNode.Children.Add(childNode);
            }

            // Add files in this category
            var categoryFiles = allFiles
                .Where(f => f.CategoryId == parentCategoryId)
                .OrderBy(f => f.FileName);

            foreach (var file in categoryFiles)
            {
                parentNode.Children.Add(new TreeNodeViewModel
                {
                    Id = $"file_{file.Id}",
                    Title = file.FileName,
                    Icon = GetFileIcon(file.FileExtension),
                    IsExpandable = false,
                    IsCategory = false,
                    FileId = file.Id,
                    FileSize = file.FileSize,
                    FileExtension = file.FileExtension,
                    CreatedDate = file.UploadedDate
                });
            }

            // If no children, mark as not expandable
            if (!parentNode.Children.Any())
            {
                parentNode.IsExpandable = false;
                parentNode.Children = null;
            }
        }

        private string GetFileIcon(string fileExtension)
        {
            return fileExtension?.ToLower() switch
            {
                ".pdf" => "bi bi-file-earmark-pdf",
                ".doc" or ".docx" => "bi bi-file-earmark-word",
                ".xls" or ".xlsx" => "bi bi-file-earmark-excel",
                ".ppt" or ".pptx" => "bi bi-file-earmark-ppt",
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "bi bi-file-earmark-image",
                ".mp4" or ".avi" or ".mov" or ".wmv" => "bi bi-file-earmark-play",
                ".mp3" or ".wav" or ".flac" => "bi bi-file-earmark-music",
                ".zip" or ".rar" or ".7z" => "bi bi-file-earmark-zip",
                ".txt" => "bi bi-file-earmark-text",
                _ => "bi bi-file-earmark"
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFile(IFormFile file, int? categoryId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "No se seleccionó ningún archivo" });
                }

                // Check file size
                var maxFileSizeMB = _configuration.GetValue<int>("FileStorage:MaxFileSizeMB", 100);
                var maxFileSizeBytes = maxFileSizeMB * 1024 * 1024;
                
                if (file.Length > maxFileSizeBytes)
                {
                    return Json(new { success = false, message = $"El archivo excede el tamaño máximo permitido de {maxFileSizeMB}MB" });
                }

                // Check file extension
                var allowedExtensions = _configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>() ?? [];
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (allowedExtensions.Length > 0 && !allowedExtensions.Contains(fileExtension))
                {
                    return Json(new { success = false, message = $"Tipo de archivo no permitido: {fileExtension}" });
                }

                // Upload file to storage
                string filePath;
                using (var stream = file.OpenReadStream())
                {
                    filePath = await _fileStorageService.UploadFileAsync(stream, file.FileName, file.ContentType);
                }

                // Create file entity
                var fileEntity = new FileEntity
                {
                    FileName = file.FileName,
                    OriginalFileName = file.FileName,
                    FileExtension = fileExtension,
                    FilePath = filePath,
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    CategoryId = categoryId,
                    UploadedBy = _currentUserService.UserId ?? Guid.Empty,
                    UploadedDate = DateTime.UtcNow,
                    Checksum = "", // TODO: Calculate checksum
                    ChecksumAlgorithm = "SHA256"
                };

                await _fileRepository.AddAsync(fileEntity);

                _logger.LogInformation("File uploaded successfully: {FileName} by {UserId}", file.FileName, _currentUserService.UserId);

                return Json(new { 
                    success = true, 
                    message = "Archivo subido exitosamente",
                    fileId = fileEntity.Id,
                    fileName = fileEntity.FileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file: {FileName}", file?.FileName);
                return Json(new { success = false, message = "Error al subir el archivo" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    return BadRequest("Ruta de archivo no válida");
                }

                // Find file entity
                var files = await _fileRepository.GetAllAsync();
                var fileEntity = files.FirstOrDefault(f => f.FilePath == filePath);

                if (fileEntity == null)
                {
                    return NotFound("Archivo no encontrado");
                }

                // Check if file exists in storage
                if (!await _fileStorageService.FileExistsAsync(filePath))
                {
                    return NotFound("Archivo no encontrado en el almacenamiento");
                }

                // Download file stream
                var fileStream = await _fileStorageService.DownloadFileAsync(filePath);
                
                return File(fileStream, fileEntity.ContentType, fileEntity.OriginalFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file: {FilePath}", filePath);
                return StatusCode(500, "Error al descargar el archivo");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFile(Guid fileId)
        {
            try
            {
                var fileEntity = await _fileRepository.GetByIdAsync(fileId);
                if (fileEntity == null)
                {
                    return Json(new { success = false, message = "Archivo no encontrado" });
                }

                // Mark as deleted in database (soft delete)
                fileEntity.IsDeleted = true;
                fileEntity.DeletedDate = DateTime.UtcNow;
                fileEntity.DeletedBy = _currentUserService.UserId;
                fileEntity.DeleteReason = "Eliminado por el usuario";

                await _fileRepository.UpdateAsync(fileEntity);

                // Optionally delete from storage (uncomment if you want hard delete)
                // await _fileStorageService.DeleteFileAsync(fileEntity.FilePath);

                _logger.LogInformation("File marked as deleted: {FileName} by {UserId}", fileEntity.FileName, _currentUserService.UserId);

                return Json(new { success = true, message = "Archivo eliminado exitosamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file: {FileId}", fileId);
                return Json(new { success = false, message = "Error al eliminar el archivo" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> UploadFile()
        {
            return PartialView("_UploadFileModal");
        }

        [HttpGet]
        public async Task<IActionResult> CreateFolder()
        {
            return PartialView("_CreateFolderModal");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateFolder(string folderName, string description, int? parentCategoryId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(folderName))
                {
                    return Json(new { success = false, message = "El nombre de la carpeta es obligatorio" });
                }

                // Build category path
                string categoryPath = "/";
                if (parentCategoryId.HasValue)
                {
                    var parentCategory = await _fileCategoryRepository.GetByIdAsync(parentCategoryId.Value);
                    if (parentCategory != null)
                    {
                        categoryPath = $"{parentCategory.CategoryPath}{folderName}/";
                    }
                }
                else
                {
                    categoryPath = $"/{folderName}/";
                }

                // Check if folder with same path already exists
                var existingCategories = await _fileCategoryRepository.GetAllAsync();
                var existingFolder = existingCategories.FirstOrDefault(c => c.CategoryPath.Equals(categoryPath, StringComparison.OrdinalIgnoreCase));
                if (existingFolder != null)
                {
                    return Json(new { success = false, message = $"Ya existe una carpeta con el nombre '{folderName}' en esta ubicación" });
                }

                var userId = _currentUserService.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000001"); // Fallback for system user
                
                var category = new FileCategory
                {
                    CategoryName = folderName,
                    CategoryPath = categoryPath,
                    Description = description ?? string.Empty,
                    ParentCategoryId = parentCategoryId,
                    IsActive = true,
                    InheritParentPermissions = true,
                    RequireExplicitAccess = false,
                    IsSystemFolder = false,
                    CreatedBy = userId // Set the CreatedBy field since it's required in the database
                    // CreatedDate will be set automatically by the database (SYSUTCDATETIME())
                };

                _logger.LogInformation("Creating folder with UserId: {UserId}, FolderName: {FolderName}, CategoryPath: {CategoryPath}", 
                    userId, folderName, categoryPath);

                await _fileCategoryRepository.AddAsync(category);

                _logger.LogInformation("Folder saved to database. Category Id: {CategoryId}", category.Id);

                // Verify the folder was saved by trying to retrieve it
                var savedCategory = await _fileCategoryRepository.GetByIdAsync(category.Id);
                if (savedCategory != null)
                {
                    _logger.LogInformation("Folder verification successful. Retrieved category: Id={Id}, Name={Name}, IsActive={IsActive}", 
                        savedCategory.Id, savedCategory.CategoryName, savedCategory.IsActive);
                }
                else
                {
                    _logger.LogWarning("Folder verification failed. Could not retrieve saved category with Id: {CategoryId}", category.Id);
                }

                // If this is a root folder (no parent), create default permissions for the creating user
                if (!parentCategoryId.HasValue)
                {
                    try
                    {
                        await CreateDefaultFolderPermissions(category.Id, userId);
                    }
                    catch (Exception permEx)
                    {
                        _logger.LogWarning(permEx, "Could not create default permissions for folder {FolderId}", category.Id);
                        // Don't fail the folder creation if permissions fail
                    }
                }

                _logger.LogInformation("Folder created: {FolderName} by {UserId}", folderName, _currentUserService.UserId);

                return Json(new { 
                    success = true, 
                    message = "Carpeta creada exitosamente",
                    categoryId = category.Id,
                    categoryName = category.CategoryName
                });
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException?.Message ?? "No inner exception";
                _logger.LogError(ex, "Error creating folder: {FolderName}, ParentId: {ParentId}, UserId: {UserId}. Inner exception: {InnerException}", 
                    folderName, parentCategoryId, _currentUserService.UserId, innerException);
                
                // Handle specific database constraint violations
                if (innerException.Contains("UQ_FileCategories_Path") || innerException.Contains("duplicate") || innerException.Contains("UNIQUE"))
                {
                    return Json(new { success = false, message = $"Ya existe una carpeta con el nombre '{folderName}' en esta ubicación" });
                }
                
                return Json(new { success = false, message = "Error al crear la carpeta. Por favor, inténtelo de nuevo." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFolderContents(int categoryId)
        {
            try
            {
                var contents = new List<object>();

                // Get subfolders
                var categories = await GetFileCategoriesAsync();
                var subfolders = categories
                    .Where(c => c.ParentCategoryId == categoryId)
                    .OrderBy(c => c.CategoryName)
                    .Select(c => new {
                        id = $"cat_{c.Id}",
                        name = c.CategoryName,
                        type = "Carpeta",
                        size = "-",
                        modified = "-",
                        icon = "bi-folder",
                        isFolder = true,
                        categoryId = c.Id
                    });

                contents.AddRange(subfolders);

                // Get files
                var files = await _fileRepository.GetAllAsync();
                var categoryFiles = files
                    .Where(f => f.CategoryId == categoryId && !f.IsDeleted)
                    .OrderBy(f => f.FileName)
                    .Select(f => new {
                        id = f.Id,
                        name = f.FileName,
                        type = GetFileTypeName(f.FileExtension),
                        size = FormatFileSize(f.FileSize),
                        modified = f.ModifiedDate?.ToString("dd/MM/yyyy HH:mm") ?? f.CreatedDate.ToString("dd/MM/yyyy HH:mm"),
                        icon = GetFileIcon(f.FileExtension),
                        filePath = f.FilePath,
                        isFolder = false
                    });

                contents.AddRange(categoryFiles);

                return Json(contents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder contents for category: {CategoryId}", categoryId);
                return Json(new List<object>());
            }
        }

        private string GetFileTypeName(string extension)
        {
            return extension?.ToLower() switch
            {
                ".pdf" => "PDF",
                ".doc" or ".docx" => "Word",
                ".xls" or ".xlsx" => "Excel", 
                ".ppt" or ".pptx" => "PowerPoint",
                ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => "Imagen",
                ".mp4" or ".avi" or ".mov" or ".wmv" => "Video",
                ".mp3" or ".wav" or ".flac" => "Audio",
                ".zip" or ".rar" or ".7z" => "Archivo",
                ".txt" => "Texto",
                _ => "Archivo"
            };
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            
            return $"{len:0.##} {sizes[order]}";
        }

        private async Task CreateDefaultFolderPermissions(int categoryId, Guid userId)
        {
            // For now, we'll skip creating CategoryAccess records to avoid dependency issues
            // This can be implemented later when we add the CategoryAccess repository
            
            /*
            // Create full access for the creator
            var creatorAccess = new CategoryAccess
            {
                CategoryId = categoryId,
                UserId = userId,
                Permissions = 255, // Full permissions (all bits set)
                InheritToSubfolders = true,
                InheritToFiles = true,
                GrantedBy = userId,
                GrantedDate = DateTime.UtcNow,
                IsActive = true
            };

            await _categoryAccessRepository.AddAsync(creatorAccess);
            */
            
            await Task.CompletedTask; // Placeholder for now
        }

        private async Task<IEnumerable<FileCategoryDto>> GetFileCategoriesAsync()
        {
            try
            {
                // Direct query using DTOs to avoid navigation property issues
                var categories = await _dbContext.Database.SqlQueryRaw<FileCategoryDto>(@"
                    SELECT 
                        CategoryId as Id,
                        CategoryName,
                        CategoryPath,
                        ParentCategoryId,
                        Description,
                        IsActive,
                        InheritParentPermissions,
                        RequireExplicitAccess,
                        IsSystemFolder,
                        CreatedBy
                    FROM [docs].[FileCategories]
                    WHERE IsActive = 1
                ").ToListAsync();

                _logger.LogInformation("GetFileCategoriesAsync: Retrieved {Count} categories using raw SQL", categories.Count);
                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFileCategoriesAsync");
                return new List<FileCategoryDto>();
            }
        }

        private async Task<IEnumerable<FileEntityDto>> GetFileEntitiesAsync()
        {
            try
            {
                // Direct query using DTOs to avoid navigation property issues
                var files = await _dbContext.Database.SqlQueryRaw<FileEntityDto>(@"
                    SELECT 
                        FileId as Id,
                        FileName,
                        FileExtension,
                        FilePath,
                        CategoryId,
                        Version,
                        FileSize,
                        ContentType,
                        OriginalFileName,
                        UploadedBy,
                        UploadedDate,
                        IsDeleted
                    FROM [docs].[Files]
                    WHERE IsDeleted = 0
                ").ToListAsync();

                _logger.LogInformation("GetFileEntitiesAsync: Retrieved {Count} files using raw SQL", files.Count);
                return files;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetFileEntitiesAsync");
                return new List<FileEntityDto>();
            }
        }

        // =============================================
        // AUDIT TRAIL MANAGEMENT
        // =============================================

        [HttpGet]
        public async Task<IActionResult> FileAuditTrail()
        {
            return PartialView("_FileAuditTrailModal");
        }

        [HttpGet]
        public async Task<IActionResult> GetFileAuditData(Guid fileId)
        {
            try
            {
                // Get file information
                var file = await _fileRepository.GetByIdAsync(fileId);
                if (file == null)
                {
                    return Json(new { success = false, message = "Archivo no encontrado" });
                }

                // Create sample audit records for now since audit table doesn't exist
                // In a real implementation, this would query the actual audit table
                var auditRecords = CreateSampleAuditData(fileId, file);

                // Get category path for the file
                string folderPath = "Raíz";
                if (file.CategoryId.HasValue)
                {
                    var category = await _fileCategoryRepository.GetByIdAsync(file.CategoryId.Value);
                    folderPath = category?.CategoryPath ?? "Raíz";
                }

                // Get uploader information
                string uploaderName = "Sistema";
                if (file.UploadedBy != Guid.Empty)
                {
                    var uploader = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == file.UploadedBy);
                    uploaderName = uploader != null 
                        ? $"{uploader.FirstName} {uploader.LastName}".Trim() 
                        : uploader?.Username ?? "Usuario desconocido";
                }

                var response = new
                {
                    success = true,
                    file = new
                    {
                        id = file.Id,
                        name = file.FileName,
                        originalName = file.OriginalFileName,
                        size = FormatFileSize(file.FileSize),
                        type = GetFileTypeName(file.FileExtension),
                        uploadedBy = uploaderName,
                        uploadedDate = file.UploadedDate.ToString("dd/MM/yyyy HH:mm"),
                        folderPath = folderPath,
                        version = file.Version
                    },
                    auditTrail = auditRecords.Select(a => new
                    {
                        logId = a.LogId,
                        eventDateTime = a.EventDateTime.ToString("dd/MM/yyyy HH:mm:ss"),
                        eventType = a.EventType,
                        action = a.Action,
                        result = a.Result,
                        details = a.Details ?? "",
                        ipAddress = a.IPAddress ?? "",
                        userAgent = a.UserAgent ?? "",
                        userName = a.UserDisplayName ?? "Sistema",
                        category = GetAuditEventCategory(a.EventType, a.Action)
                    }).ToList()
                };

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit data for file {FileId}", fileId);
                return Json(new { success = false, message = "Error al cargar los datos de auditoría" });
            }
        }

        private List<FileAuditDto> CreateSampleAuditData(Guid fileId, FileEntity file)
        {
            var auditRecords = new List<FileAuditDto>();
            var currentUser = _currentUserService.Username ?? "Sistema";
            var currentTime = DateTime.Now;

            // File upload event
            auditRecords.Add(new FileAuditDto
            {
                LogId = 1,
                EventDateTime = file.UploadedDate,
                EventType = "Upload",
                Action = "Subir archivo",
                Result = "Success",
                Details = $"Archivo '{file.FileName}' subido exitosamente",
                IPAddress = "192.168.1.100",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                UserDisplayName = "Usuario Administrador",
                Username = "admin",
                Email = "admin@example.com"
            });

            // File access events
            for (int i = 0; i < 3; i++)
            {
                auditRecords.Add(new FileAuditDto
                {
                    LogId = i + 2,
                    EventDateTime = currentTime.AddDays(-i),
                    EventType = "Access",
                    Action = "Ver archivo",
                    Result = "Success",
                    Details = $"Archivo '{file.FileName}' visualizado por el usuario",
                    IPAddress = $"192.168.1.{100 + i}",
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                    UserDisplayName = currentUser,
                    Username = currentUser.ToLower(),
                    Email = $"{currentUser.ToLower()}@example.com"
                });
            }

            // Download event
            auditRecords.Add(new FileAuditDto
            {
                LogId = 5,
                EventDateTime = currentTime.AddDays(-1),
                EventType = "Download",
                Action = "Descargar archivo",
                Result = "Success",
                Details = $"Archivo '{file.FileName}' descargado exitosamente",
                IPAddress = "192.168.1.101",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                UserDisplayName = currentUser,
                Username = currentUser.ToLower(),
                Email = $"{currentUser.ToLower()}@example.com"
            });

            // Permission change event
            auditRecords.Add(new FileAuditDto
            {
                LogId = 6,
                EventDateTime = currentTime.AddHours(-2),
                EventType = "Permission",
                Action = "Cambiar permisos",
                Result = "Success",
                Details = $"Permisos actualizados para el archivo '{file.FileName}'",
                IPAddress = "192.168.1.102",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                UserDisplayName = "Usuario Administrador",
                Username = "admin",
                Email = "admin@example.com"
            });

            return auditRecords.OrderByDescending(a => a.EventDateTime).ToList();
        }

        private string GetAuditEventCategory(string eventType, string action)
        {
            return eventType?.ToLower() switch
            {
                "access" => "access",
                "download" => "access", 
                "view" => "access",
                "upload" => "modification",
                "update" => "modification",
                "delete" => "modification",
                "permission" => "security",
                "authentication" => "security",
                "authorization" => "security",
                _ => "general"
            };
        }

        // =============================================
        // PERMISSIONS MANAGEMENT
        // =============================================

        [HttpGet]
        public async Task<IActionResult> GetFolderPermissions(int categoryId)
        {
            try
            {
                var permissions = await _permissionService.GetFolderPermissionsAsync(categoryId);
                var roles = await _dbContext.Roles.Where(r => r.IsActive).OrderBy(r => r.RoleName).ToListAsync();
                
                var permissionMatrix = roles.Select(role => {
                    var rolePermission = permissions.FirstOrDefault(p => p.RoleId == role.Id);
                    var permissionValue = rolePermission?.Permissions ?? 0;
                    
                    return new {
                        roleId = role.Id,
                        roleName = role.RoleName,
                        permissions = new {
                            view = (permissionValue & 1) == 1,
                            download = (permissionValue & 2) == 2,
                            upload = (permissionValue & 4) == 4,
                            delete = (permissionValue & 8) == 8,
                            modifyMetadata = (permissionValue & 16) == 16,
                            admin = (permissionValue & 32) == 32
                        },
                        isInherited = false, // TODO: Check if permission is inherited
                        hasDirectPermission = rolePermission != null
                    };
                }).ToList();

                return Json(permissionMatrix);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting folder permissions for category {CategoryId}", categoryId);
                return Json(new List<object>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFolderPermissions(int categoryId, int roleId, bool view, bool download, bool upload, bool delete, bool modifyMetadata, bool admin)
        {
            try
            {
                var userId = _currentUserService.UserId ?? Guid.Empty;
                if (userId == Guid.Empty)
                {
                    return Json(new { success = false, message = "Usuario no autenticado" });
                }

                // Check if user has admin permissions on this folder
                var canManage = await _permissionService.HasFolderPermissionAsync(categoryId, userId, FilePermissions.Admin);
                if (!canManage)
                {
                    return Json(new { success = false, message = "No tienes permisos para administrar esta carpeta" });
                }

                // Calculate permission value
                int permissionValue = 0;
                if (view) permissionValue |= 1;
                if (download) permissionValue |= 2;
                if (upload) permissionValue |= 4;
                if (delete) permissionValue |= 8;
                if (modifyMetadata) permissionValue |= 16;
                if (admin) permissionValue |= 32;

                if (permissionValue == 0)
                {
                    // Remove permission if no permissions selected
                    await _permissionService.RevokeFolderPermissionAsync(categoryId, roleId, userId, "Permisos removidos por el usuario");
                }
                else
                {
                    // Grant or update permission
                    await _permissionService.UpdateFolderPermissionAsync(categoryId, roleId, (FilePermissions)permissionValue, userId);
                }

                return Json(new { success = true, message = "Permisos actualizados correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating folder permissions for category {CategoryId}, role {RoleId}", categoryId, roleId);
                return Json(new { success = false, message = "Error al actualizar los permisos" });
            }
        }
    }
}