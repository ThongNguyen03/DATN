using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    // ViewModel cho trang Home
    public class HomeViewModel
    {
        public List<DanhMucSanPham> DanhMucSanPhams { get; set; }
        public List<SanPham> SanPhamDeXuats { get; set; }
    }
    public class HomeController : Controller
    {
        private MedinetDATN db = new MedinetDATN();

        // GET: Home
        [Authorize]
        public ActionResult Index()
        {
            // Lấy danh sách danh mục sản phẩm
            var danhMucSanPhams = db.DanhMucSanPhams.ToList();

            // Lấy danh sách sản phẩm được đề xuất (sắp xếp theo số lượt mua giảm dần)
            var sanPhamDeXuats = db.SanPhams
                .OrderByDescending(s => s.SoLuotMua)
                .Take(12)
                .ToList();

            // Tạo ViewModel để truyền dữ liệu sang View
            var viewModel = new HomeViewModel
            {
                DanhMucSanPhams = danhMucSanPhams,
                SanPhamDeXuats = sanPhamDeXuats
            };

            return View(viewModel);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}