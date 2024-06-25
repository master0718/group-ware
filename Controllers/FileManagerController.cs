using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using web_groupware.Models;
using web_groupware.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using web_groupware.Utilities;

#pragma warning disable CS8600,CS8601,CS8602,CS8604,CS8618,CS8629

namespace web_groupware.Controllers
{
    public class FileManagerController : BaseController
    {
        private readonly IWebHostEnvironment _environment;
        private const int FILE_TYPE_FILE = 0;
        private const int FILE_TYPE_FOLDER = 1;
        private string _uploadPath = "";
        private string storageDirectory = "";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        /// <param name="hostingEnvironment"></param>
        /// <param name="httpContextAccessor"></param>
        public FileManagerController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor)
            : base(configuration, logger, context, hostingEnvironment, httpContextAccessor)
        {
            _environment = hostingEnvironment;

            var t_dic = _context.M_DIC
                .FirstOrDefault(m => m.dic_kb == 700 && m.dic_cd == "3");
            if (t_dic == null || t_dic.content == null)
            {
                _uploadPath = _environment.WebRootPath;
            }
            else
            {
                _uploadPath = t_dic.content;
            }

        }

        [HttpGet]
        [Route("FileManager/{**folder}")]
        [Authorize]
        public async Task<IActionResult> Index(string folder)
        {
            if (_context.M_STAFF == null)
            {
                return Problem("Entity set 'web_groupwareContext.FileDetail'  is null.");
            }

            FileDetailViewModel model = new FileDetailViewModel();

            folder = folder ?? "共有フォルダ";
            if (folder.EndsWith("/"))
            {
                folder = folder.Substring(0, folder.Length - 1);
            }
            var items = _context.T_FILEINFO.Where(item => item.path.Equals(folder)).ToList();
            foreach (var item in items)
            {
                var t_STAFFM = await _context.M_STAFF
                    .FirstOrDefaultAsync(m => m.staf_cd.ToString() == item.update_user);

                model.fileList.Add(new FileDetail
                {
                    file_no = item.file_no,
                    name = item.name,
                    icon = item.icon,
                    size = item.size,
                    type = item.type,
                    update_user = t_STAFFM != null ? t_STAFFM.staf_name : "",
                    update_date = item.update_date
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(List<IFormFile> fileList, string currentDirectory)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", Message_register.FAILURE_001);
                return RedirectToAction("Index");
            }

            storageDirectory = currentDirectory == "共有フォルダ" ? "" : currentDirectory;
            foreach (var file in fileList)
            {
                if (file != null)
                {
                    var uploadDir = _uploadPath;
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }
                    bool existDuplicatedFile = false;
                    var fileName = file.FileName;
                    do
                    {
                        var duplicateFile = await _context.T_FILEINFO
                            .FirstOrDefaultAsync(m => m.name == fileName && m.path == currentDirectory);
                        if(duplicateFile != null)
                        {
                            existDuplicatedFile = true;
                            int idx = duplicateFile.name.LastIndexOf(".");
                            fileName = duplicateFile.name[..(idx)] + "(1)" + duplicateFile.name[(idx)..];
                        }
                        else
                        {
                            existDuplicatedFile = false;
                        }
                    }
                    while (existDuplicatedFile);
                    var fileToUpload = Path.Combine(_uploadPath, storageDirectory, fileName);
                    using (var fileStream = new FileStream(fileToUpload, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                
                    using (IDbContextTransaction tran = _context.Database.BeginTransaction())
                    {
                        try
                        {
                            int idx = file.FileName.LastIndexOf('.');
                            var extension = "";
                            if (idx >= 0)
                                extension = file.FileName[(idx + 1)..];

                            var lastItem = _context.T_FILEINFO.OrderByDescending(u => u.file_no).FirstOrDefault();
                            var lastId = lastItem == null ? 1 : lastItem.file_no + 1;
                            var record_new = new T_FILEINFO
                            {
                                file_no = lastId,
                                name = fileName,
                                icon = extension.IsNullOrEmpty() ? "" : extension + ".svg",
                                size = (int) file.Length,
                                type = FILE_TYPE_FILE,
                                path = currentDirectory,
                                update_user = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value,
                                update_date = DateTime.Now
                            };

                            _context.T_FILEINFO.Add(record_new);
                            await _context.SaveChangesAsync();

                            tran.Commit();

                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            _logger.LogError(ex.Message);
                            _logger.LogError(ex.StackTrace);
                            tran.Dispose();
                            ModelState.AddModelError("", Message_register.FAILURE_001);
                        }
                    }
                }
            }

            return RedirectToAction("Index", new { folder = storageDirectory });
        }

        public async Task<IActionResult> CreateFolder(int id, string newName, string currentDirectory)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    storageDirectory = currentDirectory == "共有フォルダ" ? "" : currentDirectory;
                    var t_STAFFM = await _context.T_FILEINFO
                        .FirstOrDefaultAsync(m => m.name == newName && m.path == currentDirectory);
                    if (t_STAFFM != null)
                    {
                        ModelState.AddModelError("", Messages.FOLDER_DUPLICATE);
                        return RedirectToAction("Index");
                    }
                    var newDir = Path.Combine(_uploadPath, storageDirectory, newName);
                    Directory.CreateDirectory(newDir);

                    var lastItem = _context.T_FILEINFO.OrderByDescending(u => u.file_no).FirstOrDefault();
                    var lastId = lastItem == null ? 1 : lastItem.file_no + 1;

                    var detail = new T_FILEINFO
                    {
                        file_no = lastId,
                        name = newName,
                        icon = "folder.svg",
                        size = 0,
                        type = FILE_TYPE_FOLDER,
                        path = currentDirectory,
                        update_user = HttpContext.User.FindFirst(Utilities.ClaimTypes.STAF_CD).Value,
                        update_date = DateTime.Now
                    };

                    _context.T_FILEINFO.Add(detail);
                    await _context.SaveChangesAsync();
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex.Message);
                    _logger.LogError(ex.StackTrace);
                    ModelState.AddModelError("", Message_register.FAILURE_001);
                }
            }
            else
            {
                ModelState.AddModelError("", Message_register.FAILURE_001);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Update(int? id, string newName, string currentDirectory)
        {
            if (id == null || _context.T_FILEINFO == null)
            {
                return Problem("Entity set 'web_groupwareContext.FileDetail'  is null.");
            }

            var fileDetail = await _context.T_FILEINFO.FindAsync(id);
            if (fileDetail != null)
            {
                using (IDbContextTransaction tran = _context.Database.BeginTransaction())
                {
                    try
                    {
                        storageDirectory = currentDirectory == "共有フォルダ" ? "" : currentDirectory;
                        var subPath = "";
                        if (fileDetail.path == "共有フォルダ")
                        {
                            subPath = fileDetail.name;
                        }
                        else
                        {
                            subPath = $"{fileDetail.path}/{fileDetail.name}";
                        }
                        if (fileDetail.type == FILE_TYPE_FILE)
                        {
                            var oldFilePath = Path.Combine(_uploadPath, storageDirectory, fileDetail.name);
                            var newFilePath = Path.Combine(_uploadPath, storageDirectory, newName);
                            System.IO.File.Move(oldFilePath, newFilePath);
                        }
                        else
                        {
                            var oldFolderPath = Path.Combine(_uploadPath, storageDirectory, fileDetail.name);
                            var newFolderPath = Path.Combine(_uploadPath, storageDirectory, newName);
                            Directory.Move(oldFolderPath, newFolderPath);

                            var fileInfoList = _context.T_FILEINFO.AsEnumerable().Where(item => $"{ item.path }/".StartsWith($"{ subPath }/")).ToList();
                            foreach (var fileInfo in fileInfoList ) 
                            {
                                int oldFolderlastIndex = subPath.LastIndexOf(fileDetail.name);
                                int newFolderIndex = fileInfo.path.IndexOf(subPath);
                                fileInfo.path = fileInfo.path.Remove(newFolderIndex, subPath.Length).Insert(newFolderIndex, subPath.Remove(oldFolderlastIndex, fileDetail.name.Length).Insert(oldFolderlastIndex, newName));
                                _context.T_FILEINFO.Update(fileInfo);
                            }
                        }
                        fileDetail.name = newName;

                        _context.T_FILEINFO.Update(fileDetail);
                        await _context.SaveChangesAsync();

                        tran.Commit();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                        _logger.LogError(ex.Message);
                        _logger.LogError(ex.StackTrace);
                        tran.Dispose();
                        ModelState.AddModelError("", Message_change.FAILURE_001);
                    }
                }
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> FindFolderToMove(List<int> fileNoList)
        {
            var allFolders = _context.T_FILEINFO.Where(item => item.type == 1).ToList();
            var allFolderList = new List<string>();
            foreach(var folder in allFolders)
            {
                if(folder.path == "共有フォルダ")
                {
                    if(!allFolderList.Contains(folder.name))
                    {
                        allFolderList.Add(folder.name);
                    }
                }
                else
                {
                    if (!allFolderList.Contains($"{folder.path}/{folder.name}"))
                    {
                        allFolderList.Add($"{folder.path}/{folder.name}");
                    }
                }
            }
            allFolderList.Add("共有フォルダ");

            foreach(var fileNo in fileNoList)
            {
                var file = await _context.T_FILEINFO.FindAsync(fileNo);
                allFolderList = allFolderList.Where(folder => folder != file.path).ToList();
                if(file.type == 1)
                {
                    var subFolderList = _context.T_FILEINFO.AsEnumerable().Where(item => $"{ item.path }/".StartsWith($"{ file.path }/{ file.name }/".Replace("共有フォルダ/", "")) && item.type == 1).ToList();
                    if(subFolderList.Count > 0)
                    {
                        foreach(var subFolder in subFolderList)
                        {
                            allFolderList = allFolderList.Where(folder => !folder.StartsWith(subFolder.path)).ToList();
                        }
                    }
                    else
                    {
                        allFolderList = allFolderList.Where(folder => folder != $"{ file.path }/{ file.name }".Replace("共有フォルダ/", "")).ToList();
                    }
                }
            }
            return Json(allFolderList);
        }

        public async Task<IActionResult> MoveToFolder(string destinationFolder, List<int> fileNoList)
        {
            storageDirectory = destinationFolder == "共有フォルダ" ? "" : destinationFolder;
            using (IDbContextTransaction tran = _context.Database.BeginTransaction())
            {
                try
                {
                    foreach (var fileNo in fileNoList)
                    {
                        var file = await _context.T_FILEINFO.FindAsync(fileNo);
                        var path = file.path == "共有フォルダ" ? "" : file.path;
                        var subPath = "";
                        if (file.path == "共有フォルダ")
                        {
                            subPath = file.name;
                        }
                        else
                        {
                            subPath = $"{file.path}/{file.name}";
                        }
                        if (file.type == FILE_TYPE_FILE)
                        {
                            var oldFilePath = Path.Combine(_uploadPath, path, file.name);
                            var newFilePath = Path.Combine(_uploadPath, storageDirectory, file.name);
                            System.IO.File.Move(oldFilePath, newFilePath);
                        }
                        else
                        {
                            var oldFolderPath = Path.Combine(_uploadPath, path, file.name);
                            var newFolderPath = Path.Combine(_uploadPath, storageDirectory, file.name);
                            Directory.Move(oldFolderPath, newFolderPath);

                            var fileInfoList = _context.T_FILEINFO.AsEnumerable().Where(item => $"{ item.path }/".StartsWith($"{ subPath }/")).ToList();
                            foreach (var fileInfo in fileInfoList)
                            {
                                if (file.path == "共有フォルダ")
                                {
                                    fileInfo.path = $"{destinationFolder}/{fileInfo.path}";
                                }
                                else
                                {
                                    var folderPathIndex = fileInfo.path.IndexOf(file.path);
                                    fileInfo.path = fileInfo.path.Remove(folderPathIndex, file.path.Length).Insert(folderPathIndex, destinationFolder).Replace("共有フォルダ/", "");
                                }
                                _context.T_FILEINFO.Update(fileInfo);
                            }
                        }
                        file.path = destinationFolder;
                        _context.T_FILEINFO.Update(file);
                    }
                    await _context.SaveChangesAsync();
                    tran.Commit();
                }
                catch (Exception ex)
                {
                    tran.Rollback();
                    _logger.LogError(ex.Message);
                    _logger.LogError(ex.StackTrace);
                    tran.Dispose();
                    ModelState.AddModelError("", Message_change.FAILURE_001);
                }
            }
            return Json("success");
        }

        public async Task<IActionResult> Delete(List<int> fileNoList)
        {
            if (fileNoList == null || _context.T_FILEINFO == null)
            {
                return Problem("Entity set 'web_groupwareContext.FileDetail'  is null.");
            }

            foreach(var fileNo in fileNoList)
            {
                var fileDetail = await _context.T_FILEINFO.FindAsync(fileNo);
                storageDirectory = fileDetail.path == "共有フォルダ" ? "" : fileDetail.path;
                if (fileDetail != null)
                {
                    try
                    {
                        var path = Path.Combine(_uploadPath, storageDirectory, fileDetail.name);
                        var subPath = "";
                        if(fileDetail.path == "共有フォルダ") 
                        {
                            subPath = fileDetail.name;
                        }
                        else 
                        {
                            subPath = $"{fileDetail.path}/{fileDetail.name}";
                        }
                        if (fileDetail.type == FILE_TYPE_FILE)
                        {
                            var fileDel = new FileInfo(path);
                            fileDel.Delete();
                        }
                        else
                        {
                            Directory.Delete(path, true);
                            var fileInfoList = _context.T_FILEINFO.AsEnumerable().Where(item => $"{ item.path }/".StartsWith($"{ subPath }/")).ToList();
                            _context.T_FILEINFO.RemoveRange(fileInfoList);
                        }
                        _context.T_FILEINFO.Remove(fileDetail);
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                    }
                }
            }

            return RedirectToAction("Index");
        }
        
        public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
        public async Task<IActionResult> DownloadFile(List<int> fileNoList)
        {
            try
            {
                var downloadDirectory = $"{this._environment.WebRootPath}\\downloads";
                if(!Directory.Exists(downloadDirectory))
                {
                    Directory.CreateDirectory(downloadDirectory);
                }
                var groupwareDirectory = $"{ downloadDirectory }\\Groupware";
                Directory.CreateDirectory(groupwareDirectory);

                foreach (var fileNo in fileNoList)
                {
                    var file = await _context.T_FILEINFO.FindAsync(fileNo);
                    var path = file.path == "共有フォルダ" ? "" : file.path;
                    if (file.type == FILE_TYPE_FILE)
                    {
                        var oldFilePath = Path.Combine(_uploadPath, path, file.name);
                        var newFilePath = Path.Combine(groupwareDirectory, file.name);
                        System.IO.File.Copy(oldFilePath, newFilePath);
                    }
                    else
                    {
                        var oldFolderPath = Path.Combine(_uploadPath, path, file.name);
                        var newFolderPath = Path.Combine(groupwareDirectory, file.name);
                        CopyDirectory(oldFolderPath, newFolderPath, true);
                    }
                }
                var destinationZipFile = $"{downloadDirectory}\\Groupware.zip";
                if(System.IO.File.Exists(destinationZipFile))
                {
                    System.IO.File.Delete(destinationZipFile);
                }
                ZipFile.CreateFromDirectory(groupwareDirectory, destinationZipFile, CompressionLevel.Fastest, true);
                Directory.Delete(groupwareDirectory, true);
                return Json("success");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
            return BadRequest();            
        }
    }
}