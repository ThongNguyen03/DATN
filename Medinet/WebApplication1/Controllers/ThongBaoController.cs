using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class ThongBaoController : Controller
    {
        private MedinetDATN db = new MedinetDATN();
        // GET: ThongBao
        public ActionResult Index()
        {
            int userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            // Lấy tất cả thông báo của người dùng hiện tại
            var thongBaos = db.ThongBaos
                .Where(t => t.MaNguoiDung == userId)
                .OrderByDescending(t => t.NgayTao)
                .ToList();

            var nguoidung = db.NguoiDungs.Find(userId);
            Session["TenNguoiDung"] = nguoidung.TenNguoiDung;
            return View(thongBaos);
        }
        public JsonResult GetUnreadNotificationCount()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { count = 0 }, JsonRequestBehavior.AllowGet);
            }

            int userId = GetCurrentUserId();

            // Lấy số lượng thông báo chưa đọc
            int unreadCount = db.ThongBaos
                .Count(t => t.MaNguoiDung == userId && t.TrangThai== "Chưa đọc");

            return Json(new { count = unreadCount }, JsonRequestBehavior.AllowGet);
        }

        // GET: ThongBao/GetAllNotifications
        public JsonResult GetAllNotifications()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            int userId = GetCurrentUserId();

            // Lấy tất cả thông báo, ưu tiên thông báo chưa đọc và sắp xếp theo thời gian
            var thongBaos = db.ThongBaos
                .Where(t => t.MaNguoiDung == userId)
                .OrderByDescending(t => t.TrangThai == "Chưa đọc") // Ưu tiên thông báo chưa đọc
                .ThenByDescending(t => t.NgayTao)
                .Select(t => new {
                    t.MaThongBao,
                    t.TieuDe,
                    t.TinNhan,
                    t.NgayTao,
                    t.TrangThai,
                    t.LoaiThongBao,
                    t.MucDoQuanTrong,
                    t.DuongDanChiTiet
                })
                .ToList();

            return Json(thongBaos, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult MarkAsRead(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Người dùng chưa đăng nhập" });
            }

            try
            {
                int userId = GetCurrentUserId();
                var thongBao = db.ThongBaos.FirstOrDefault(t => t.MaThongBao == id && t.MaNguoiDung == userId);

                if (thongBao != null)
                {
                    thongBao.TrangThai = "Đã đọc";
                    db.SaveChanges();

                    // Trả về đường dẫn chi tiết cùng với thông báo thành công
                    return Json(new
                    {
                        success = true,
                        duongDanChiTiet = thongBao.DuongDanChiTiet
                    });
                }

                return Json(new { success = false, message = "Không tìm thấy thông báo" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        // Thêm action này vào ThongBaoController

        /// <summary>
        /// Đánh dấu tất cả thông báo của người dùng hiện tại là đã đọc
        /// </summary>
        /// <returns>Json result</returns>
        [HttpPost]
        public ActionResult MarkAllAsRead()
        {
            try
            {
                // Lấy ID người dùng hiện tại
                var userId = GetCurrentUserId();

                // Lấy tất cả thông báo chưa đọc của người dùng
                var unreadNotifications = db.ThongBaos
                    .Where(n => n.MaNguoiDung == userId && n.TrangThai == "Chưa đọc")
                    .ToList();

                // Đánh dấu tất cả là đã đọc
                foreach (var notification in unreadNotifications)
                {
                    notification.TrangThai = "Đã đọc";
                    notification.NgayDoc = DateTime.Now;
                }

                // Lưu thay đổi vào cơ sở dữ liệu
                db.SaveChanges();

                return Json(new { success = true, count = unreadNotifications.Count });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        // Phương thức hỗ trợ lấy ID của người dùng hiện tại
        private int GetCurrentUserId()
        {
            // Lấy Email người dùng đã đăng nhập
            var userName = User.Identity.Name;

            // Tìm người dùng trong cơ sở dữ liệu theo Email
            var nguoiDung = db.NguoiDungs.FirstOrDefault(n => n.Email == userName);

            if (nguoiDung != null)
            {
                return nguoiDung.MaNguoiDung;
            }

            // Nếu không tìm thấy người dùng, thử lấy từ Session
            if (Session["MaNguoiDung"] != null)
            {
                return Convert.ToInt32(Session["MaNguoiDung"]);
            }

            // Nếu vẫn không có, trả về giá trị mặc định
            return 0; // Trả về 0 để biểu thị không tìm thấy người dùng
        }
    }
}