using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using web_groupware.Models;
using web_groupware.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Data;
using System.IO;
using System.Linq;
using System.Data.SqlClient;
using Syncfusion.XlsIO.Implementation.Security;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Spreadsheet;
using static Syncfusion.XlsIO.Parser.Biff_Records.BoundSheetRecord;
using System.Reflection;
using System.Configuration;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Drawing.Charts;
using Excel = Microsoft.Office.Interop.Excel;
using Microsoft.Extensions.Hosting.Internal;

namespace web_groupware.Controllers
{
    public class AttendanceController : BaseController
    {
        private new readonly web_groupwareContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public AttendanceController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor) : base(configuration, logger, context, hostingEnvironment, httpContextAccessor)
        {
            _context = context;
            _webHostEnvironment = hostingEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? date)
        {
            date = date ?? DateTime.Now;
            var viewModel = new Attendance_StaffAndYearModel
            {
                StaffMembers = await _context.M_STAFF.ToListAsync(),
                Year = date.Value.Year,
                Month = date.Value.Month,
                Day = date.Value.Day
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Index(int? year, int? month, int? days, int[] state, string? staff_name, int? staff_num)
        {
            if (year == null || month == null || days == null || state == null || string.IsNullOrEmpty(staff_name) || staff_num==null )
            {
                return BadRequest("無効なフォームデータ");
            }

            // Create the request_date from parameters
            DateTime requestDate = new DateTime(year.Value, month.Value, days.Value, 0, 0, 0);

            // Check if a record with the same staff_name and request_date already exists
            var existingRecord = _context.T_ATTENDANCE_DATE.FirstOrDefault(a => a.staf_cd == staff_num && a.request_date == requestDate);

            if (existingRecord != null)
            {
                // Update the existing record
                existingRecord.state_num = string.Join(",", state);

                // Update any other fields as needed

                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Attendance");
            }
            else
            {
                // Convert the state array to a comma-separated string
                var stateString = string.Join(",", state);

                // Create a new T_AttendanceDate instance
                var saveData = new T_ATTENDANCE_DATE
                { 
                    /*id = GetNextNo(Utilities.DataTypes.WORKFLOW_NO),*/
                    staf_cd = staff_num.Value,
                    staf_name = staff_name,
                    state_num = stateString,
                    request_date = requestDate
                };

                // Add the new record to the context and save changes
                _context.T_ATTENDANCE_DATE.Add(saveData);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "Attendance");
            }
        }

        public async Task<ActionResult> ExportToExcel(int staf_cd, int year)
        {
            // Get the web root path
            var webRootPath = _webHostEnvironment.WebRootPath;

            string filePath = Path.GetFullPath(Path.Combine(webRootPath, "Files", "Attendance.xlsx"));

            var fileInfo = new FileInfo(filePath);

            if (!fileInfo.Exists)
            {
                return NotFound();
            }

            /*var data = await _context.T_AttendanceDate.FirstOrDefaultAsync(a => a.staf_cd == staf_cd && a.request_date.Year == year);*/
            var data = await _context.T_ATTENDANCE_DATE
                .Where(a => a.staf_cd == staf_cd && a.request_date.Year == year)
                .ToListAsync();

            if (data == null|| data.Count ==0)
            {
                return NotFound();
            }

            var status_data = await _context.M_DIC
                .Where(a => a.dic_kb == web_groupware.Utilities.DIC_KB.ATTENDANCE_STATUS)
                .ToListAsync();

            // Load the existing workbook
            using (var workbook = new XLWorkbook(filePath))
            {
                
                var worksheet = workbook.Worksheet(1);
                int startRow = 8;
                int currentRow = 0;
                int reiwa = year - 2018;
                int COUNT2 = 0;
                int COUNT3 = 0;
                int COUNT4 = 0;
                
                worksheet.Cells("A5").Value = year+"(R"+reiwa+ "年)";
                DateTime currentDate = DateTime.Now;
                worksheet.Cells("Ak5").Value = currentDate;
                // Add your data to the worksheet as needed
                for (int i = 0; i < data.Count; i++)
                {
                    string state_number = data[i].state_num;
                    string[] numbers = state_number.Split(',');
                    int days = data[i].request_date.Day;
                    COUNT2 = 0;
                    COUNT3 = 0;
                    COUNT4 = 0;
                    if (data[i].request_date.Month == 1)
                    {
                        for (int j = 0; j < days && j < numbers.Length; j++)
                        {
                            currentRow = startRow + j;
                            if (numbers[j] == "1")
                            {
                                worksheet.Cells("D"+currentRow).Value = "";
                            }
                            if (numbers[j] == "2")
                            {
                                int num = int.Parse(numbers[j]);
                                /*worksheet.Cells("D" + currentRow).Value = "有休";*/
                                worksheet.Cells("D" + currentRow).Value = status_data[num].content;
                                COUNT2++;
                            }
                            if (numbers[j] == "3")
                            {
                                int num = int.Parse(numbers[j]);
                                /*worksheet.Cells("D" + currentRow).Value = "遅刻";*/
                                worksheet.Cells("D" + currentRow).Value = status_data[num].content;
                                COUNT3++;
                            }
                            if (numbers[j] == "4")
                            {
                                int num = int.Parse(numbers[j]);
                                /*worksheet.Cells("D" + currentRow).Value = "早退";*/
                                worksheet.Cells("D" + currentRow).Value = status_data[num].content;
                                COUNT4++;
                            }
                            if (numbers[j] == "5")
                            {
                                int num = int.Parse(numbers[j]);
                                /*worksheet.Cells("D" + currentRow).Value = "夏季休暇";*/
                                worksheet.Cells("D" + currentRow).Value = status_data[num].content;
                            }
                        }
                        if (COUNT2 != 0)
                        {
                            worksheet.Cells("D39").Value = COUNT2;
                        }
                        if(COUNT3 != 0)
                        {
                            worksheet.Cells("D45").Value = COUNT3;
                        }
                        if(COUNT4 != 0)
                        {
                            worksheet.Cells("D46").Value = COUNT4;
                        }
                    }
                    if (data[i].request_date.Month == 2)
                    {
                        for (int j = 0; j < days && j < numbers.Length; j++)
                        {
                            currentRow = startRow + j;
                            if (numbers[j] == "1")
                            {
                                worksheet.Cells("G" + currentRow).Value = "";
                            }
                            if (numbers[j] == "2")
                            {
                                worksheet.Cells("G" + currentRow).Value = "有休";
                                COUNT2++;
                            }
                            if (numbers[j] == "3")
                            {
                                worksheet.Cells("G" + currentRow).Value = "遅刻";
                                COUNT3++;
                            }
                            if (numbers[j] == "4")
                            {
                                worksheet.Cells("G" + currentRow).Value = "早退";
                                COUNT4++;
                            }
                            if (numbers[j] == "5")
                            {
                                worksheet.Cells("G" + currentRow).Value = "夏季休暇";
                            }
                        }
                        if (COUNT2 != 0)
                        {
                            worksheet.Cells("G39").Value = COUNT2;
                        }
                        if (COUNT3 != 0)
                        {
                            worksheet.Cells("G45").Value = COUNT3;
                        }
                        if (COUNT4 != 0)
                        {
                            worksheet.Cells("G46").Value = COUNT4;
                        }
                    }
                    if (data[i].request_date.Month == 3)
                    {
                        for (int j = 0; j < days && j < numbers.Length; j++)
                        {
                            currentRow = startRow + j;
                            if (numbers[j] == "1")
                            {
                                worksheet.Cells("J" + currentRow).Value = "";
                            }
                            if (numbers[j] == "2")
                            {
                                worksheet.Cells("J" + currentRow).Value = "有休";
                                COUNT2++;
                            }
                            if (numbers[j] == "3")
                            {
                                worksheet.Cells("J" + currentRow).Value = "遅刻";
                                COUNT3++;
                            }
                            if (numbers[j] == "4")
                            {
                                worksheet.Cells("J" + currentRow).Value = "早退";
                                COUNT4++;
                            }
                            if (numbers[j] == "5")
                            {
                                worksheet.Cells("J" + currentRow).Value = "夏季休暇";
                            }
                        }
                        if (COUNT2 != 0)
                        {
                            worksheet.Cells("J39").Value = COUNT2;
                        }
                        if (COUNT3 != 0)
                        {
                            worksheet.Cells("J45").Value = COUNT3;
                        }
                        if (COUNT4 != 0)
                        {
                            worksheet.Cells("J46").Value = COUNT4;
                        }
                    }
                    if (data[i].request_date.Month == 4)
                    {
                        for (int j = 0; j < days && j < numbers.Length; j++)
                        {
                            currentRow = startRow + j;
                            if (numbers[j] == "1")
                            {
                                worksheet.Cells("M" + currentRow).Value = "";
                            }
                            if (numbers[j] == "2")
                            {
                                worksheet.Cells("M" + currentRow).Value = "有休";
                                COUNT2++;
                            }
                            if (numbers[j] == "3")
                            {
                                worksheet.Cells("M" + currentRow).Value = "遅刻";
                                COUNT3++;
                            }
                            if (numbers[j] == "4")
                            {
                                worksheet.Cells("M" + currentRow).Value = "早退";
                                COUNT4++;
                            }
                            if (numbers[j] == "5")
                            {
                                worksheet.Cells("M" + currentRow).Value = "夏季休暇";
                            }
                        }
                        if (COUNT2 != 0)
                        {
                            worksheet.Cells("M39").Value = COUNT2;
                        }
                        if (COUNT3 != 0)
                        {
                            worksheet.Cells("M45").Value = COUNT3;
                        }
                        if (COUNT4 != 0)
                        {
                            worksheet.Cells("M46").Value = COUNT4;
                        }
                    }
                    if (data[i].request_date.Month == 5)
                    {
                        for (int j = 0; j < days && j < numbers.Length; j++)
                        {
                            currentRow = startRow + j;
                            if (numbers[j] == "1")
                            {
                                worksheet.Cells("P" + currentRow).Value = "";
                            }
                            if (numbers[j] == "2")
                            {
                                worksheet.Cells("P" + currentRow).Value = "有休";
                                COUNT2++;
                            }
                            if (numbers[j] == "3")
                            {
                                worksheet.Cells("P" + currentRow).Value = "遅刻";
                                COUNT3++;
                            }
                            if (numbers[j] == "4")
                            {
                                worksheet.Cells("P" + currentRow).Value = "早退";
                                COUNT4++;
                            }
                            if (numbers[j] == "5")
                            {
                                worksheet.Cells("P" + currentRow).Value = "夏季休暇";
                            }
                        }
                        if (COUNT2 != 0)
                        {
                            worksheet.Cells("P39").Value = COUNT2;
                        }
                        if (COUNT3 != 0)
                        {
                            worksheet.Cells("P45").Value = COUNT3;
                        }
                        if (COUNT4 != 0)
                        {
                            worksheet.Cells("P46").Value = COUNT4;
                        }
                    }
                    if (data[i].request_date.Month == 6)
                    {
                        for (int j = 0; j < days && j < numbers.Length; j++)
                        {
                            currentRow = startRow + j;
                            if (numbers[j] == "1")
                            {
                                worksheet.Cells("S" + currentRow).Value = "";
                            }
                            if (numbers[j] == "2")
                            {
                                worksheet.Cells("S" + currentRow).Value = "有休";
                                COUNT2++;
                            }
                            if (numbers[j] == "3")
                            {
                                worksheet.Cells("S" + currentRow).Value = "遅刻";
                                COUNT3++;
                            }
                            if (numbers[j] == "4")
                            {
                                worksheet.Cells("S" + currentRow).Value = "早退";
                                COUNT4++;
                            }
                            if (numbers[j] == "5")
                            {
                                worksheet.Cells("S" + currentRow).Value = "夏季休暇";
                            }
                        }
                        if (COUNT2 != 0)
                        {
                            worksheet.Cells("S39").Value = COUNT2;
                        }
                        if (COUNT3 != 0)
                        {
                            worksheet.Cells("S45").Value = COUNT3;
                        }
                        if (COUNT4 != 0)
                        {
                            worksheet.Cells("S46").Value = COUNT4;
                        }
                    }
                    if (data[i].request_date.Month == 7)
                    {
                        for (int j = 0; j < days && j < numbers.Length; j++)
                        {
                            currentRow = startRow + j;
                            if (numbers[j] == "1")
                            {
                                worksheet.Cells("Y" + currentRow).Value = "";
                            }
                            if (numbers[j] == "2")
                            {
                                worksheet.Cells("Y" + currentRow).Value = "有休";
                                COUNT2++;
                            }
                            if (numbers[j] == "3")
                            {
                                worksheet.Cells("Y" + currentRow).Value = "遅刻";
                                COUNT3++;
                            }
                            if (numbers[j] == "4")
                            {
                                worksheet.Cells("Y" + currentRow).Value = "早退";
                                COUNT4++;
                            }
                            if (numbers[j] == "5")
                            {
                                worksheet.Cells("Y" + currentRow).Value = "夏季休暇";
                            }
                        }
                        if (COUNT2 != 0)
                        {
                            worksheet.Cells("Y39").Value = COUNT2;
                        }
                        if (COUNT3 != 0)
                        {
                            worksheet.Cells("Y45").Value = COUNT3;
                        }
                        if (COUNT4 != 0)
                        {
                            worksheet.Cells("Y46").Value = COUNT4;
                        }
                    }
                    if (data[i].request_date.Month == 8)
                    {
                        for (int j = 0; j < days && j < numbers.Length; j++)
                        {
                            currentRow = startRow + j;
                            if (numbers[j] == "1")
                            {
                                worksheet.Cells("AB" + currentRow).Value = "";
                            }
                            if (numbers[j] == "2")
                            {
                                worksheet.Cells("AB" + currentRow).Value = "有休";
                                COUNT2++;
                            }
                            if (numbers[j] == "3")
                            {
                                worksheet.Cells("AB" + currentRow).Value = "遅刻";
                                COUNT3++;
                            }
                            if (numbers[j] == "4")
                            {
                                worksheet.Cells("AB" + currentRow).Value = "早退";
                                COUNT4++;
                            }
                            if (numbers[j] == "5")
                            {
                                worksheet.Cells("AB" + currentRow).Value = "夏季休暇";
                            }
                        }
                        if (COUNT2 != 0)
                        {
                            worksheet.Cells("AB39").Value = COUNT2;
                        }
                        if (COUNT3 != 0)
                        {
                            worksheet.Cells("AB45").Value = COUNT3;
                        }
                        if (COUNT4 != 0)
                        {
                            worksheet.Cells("AB46").Value = COUNT4;
                        }
                    }
                    if (data[i].request_date.Month == 9)
                    {
                        for (int j = 0; j < days && j < numbers.Length; j++)
                        {
                            currentRow = startRow + j;
                            if (numbers[j] == "1")
                            {
                                worksheet.Cells("AE" + currentRow).Value = "";
                            }
                            if (numbers[j] == "2")
                            {
                                worksheet.Cells("AE" + currentRow).Value = "有休";
                                COUNT2++;
                            }
                            if (numbers[j] == "3")
                            {
                                worksheet.Cells("AE" + currentRow).Value = "遅刻";
                                COUNT3++;
                            }
                            if (numbers[j] == "4")
                            {
                                worksheet.Cells("AE" + currentRow).Value = "早退";
                                COUNT4++;
                            }
                            if (numbers[j] == "5")
                            {
                                worksheet.Cells("AE" + currentRow).Value = "夏季休暇";
                            }
                        }
                        if (COUNT2 != 0)
                        {
                            worksheet.Cells("AE39").Value = COUNT2;
                        }
                        if (COUNT3 != 0)
                        {
                            worksheet.Cells("AE45").Value = COUNT3;
                        }
                        if (COUNT4 != 0)
                        {
                            worksheet.Cells("AE46").Value = COUNT4;
                        }
                    }
                    if (data[i].request_date.Month == 10)
                    {
                        for (int j = 0; j < days && j < numbers.Length; j++)
                        {
                            currentRow = startRow + j;
                            if (numbers[j] == "1")
                            {
                                worksheet.Cells("AH" + currentRow).Value = "";
                            }
                            if (numbers[j] == "2")
                            {
                                worksheet.Cells("AH" + currentRow).Value = "有休";
                                COUNT2++;
                            }
                            if (numbers[j] == "3")
                            {
                                worksheet.Cells("AH" + currentRow).Value = "遅刻";
                                COUNT3++;
                            }
                            if (numbers[j] == "4")
                            {
                                worksheet.Cells("AH" + currentRow).Value = "早退";
                                COUNT4++;
                            }
                            if (numbers[j] == "5")
                            {
                                worksheet.Cells("AH" + currentRow).Value = "夏季休暇";
                            }
                        }
                        if (COUNT2 != 0)
                        {
                            worksheet.Cells("AH39").Value = COUNT2;
                        }
                        if (COUNT3 != 0)
                        {
                            worksheet.Cells("AH45").Value = COUNT3;
                        }
                        if (COUNT4 != 0)
                        {
                            worksheet.Cells("AH46").Value = COUNT4;
                        }
                    }
                    if (data[i].request_date.Month == 11)
                    {
                        for (int j = 0; j < days && j < numbers.Length; j++)
                        {
                            currentRow = startRow + j;
                            if (numbers[j] == "1")
                            {
                                worksheet.Cells("AK" + currentRow).Value = "";
                            }
                            if (numbers[j] == "2")
                            {
                                worksheet.Cells("AK" + currentRow).Value = "有休";
                                COUNT2++;
                            }
                            if (numbers[j] == "3")
                            {
                                worksheet.Cells("AK" + currentRow).Value = "遅刻";
                                COUNT3++;
                            }
                            if (numbers[j] == "4")
                            {
                                worksheet.Cells("AK" + currentRow).Value = "早退";
                                COUNT4++;
                            }
                            if (numbers[j] == "5")
                            {
                                worksheet.Cells("AK" + currentRow).Value = "夏季休暇";
                            }
                        }
                        if (COUNT2 != 0)
                        {
                            worksheet.Cells("AK39").Value = COUNT2;
                        }
                        if (COUNT3 != 0)
                        {
                            worksheet.Cells("AK45").Value = COUNT3;
                        }
                        if (COUNT4 != 0)
                        {
                            worksheet.Cells("AK46").Value = COUNT4;
                        }
                    }
                    if (data[i].request_date.Month == 12)
                    {
                        for (int j = 0; j < days && j < numbers.Length; j++)
                        {
                            currentRow = startRow + j;
                            if (numbers[j] == "1")
                            {
                                worksheet.Cells("AN" + currentRow).Value = "";
                            }
                            if (numbers[j] == "2")
                            {
                                worksheet.Cells("AN" + currentRow).Value = "有休";
                                COUNT2++;
                            }
                            if (numbers[j] == "3")
                            {
                                worksheet.Cells("AN" + currentRow).Value = "遅刻";
                                COUNT3++;
                            }
                            if (numbers[j] == "4")
                            {
                                worksheet.Cells("AN" + currentRow).Value = "早退";
                                COUNT4++;
                            }
                            if (numbers[j] == "5")
                            {
                                worksheet.Cells("AN" + currentRow).Value = "夏季休暇";
                            }
                        }
                        if (COUNT2 != 0)
                        {
                            worksheet.Cells("AN39").Value = COUNT2;
                        }
                        if (COUNT3 != 0)
                        {
                            worksheet.Cells("AN45").Value = COUNT3;
                        }
                        if (COUNT4 != 0)
                        {
                            worksheet.Cells("AN46").Value = COUNT4;
                        }
                    }
                }
                // Save the changes to the existing file
                workbook.SaveAs(filePath);
            }

            // Redirect back to the view or any other desired action
            return RedirectToAction("Index", "Attendance");
        }
        
    }
}
