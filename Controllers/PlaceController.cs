using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_groupware.Data;
using web_groupware.Models;
using web_groupware.Utilities;

namespace web_groupware.Controllers
{
    [Authorize]
    public class PlaceController : BaseController
    {
        public PlaceController(IConfiguration configuration, ILogger<BaseController> logger, web_groupwareContext context, IWebHostEnvironment hostingEnvironment, IHttpContextAccessor httpContextAccessor) : base(configuration, logger, context, hostingEnvironment, httpContextAccessor) { }

        [HttpGet]
        public IActionResult Index()
        {
            try { 
                var model = new PlaceViewModel();
                model.placeList = _context.M_PLACE.Select(x => new PlaceDetailModel
                {
                    place_cd = x.place_cd,
                    place_name = x.place_name,
                    sort = x.sort
                }).ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PlaceDetailModel model)
        {
            if (_context.M_PLACE.Any(x => x.place_cd == model.place_cd))
            {
                ModelState.AddModelError("", "この施設コードはすでに存在する。");
                return View(model);
            }
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", Messages.IS_VALID_FALSE);
                return View(model);
            }

            try
            {
                var model_new = new M_PLACE
                {
                    place_cd = model.place_cd,
                    place_name = model.place_name,
                    sort = model.sort
                };
                _context.Add(model_new);

                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int place_cd)
        {
            try
            {
                var record = await _context.M_PLACE.FirstOrDefaultAsync(m => m.place_cd == place_cd);
                if (record == null)
                {
                    return RedirectToAction("Index");
                }
                var model = new PlaceDetailModel();
                model.place_cd = record.place_cd;
                model.place_name = record.place_name;
                model.sort = record.sort;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PlaceDetailModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", Message_change.FAILURE_001);
                return View(model);
            }
            var record = await _context.M_PLACE.FirstOrDefaultAsync(x => x.place_cd == model.place_cd);
            if (record == null)
            {
                ModelState.AddModelError("", "存在しないグループコードです。");
                return View(model);
            }

            try
            {
                // record.place_cd = model.place_cd;
                record.place_name = model.place_name;
                record.sort = model.sort;

                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int place_cd)
        {
            try
            {
                var item = await _context.M_PLACE.FirstOrDefaultAsync(x => x.place_cd == place_cd);
                if (item == null)
                {
                    return RedirectToAction("Index");
                }
                var model = new PlaceDetailModel
                { 
                    place_cd = place_cd,
                    place_name = item.place_name,
                    sort = item.sort
                };
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(PlaceDetailModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ModelState.AddModelError("", Message_delete.FAILURE_001);
                    return View(model);
                }
                var record = await _context.M_PLACE.FirstOrDefaultAsync(x => x.place_cd == model.place_cd);
                if (record == null)
                {
                    ModelState.AddModelError("", "存在しないグループコードです。");
                    return View(model);
                }
                _context.M_PLACE.Remove(record);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(Messages.ERROR_PREFIX + ex.Message);
                _logger.LogError(ex.StackTrace);
                throw;
            }
        }
    }
}
