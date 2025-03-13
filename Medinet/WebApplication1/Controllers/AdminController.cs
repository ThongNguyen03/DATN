using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private MedinetDATN db = new MedinetDATN();

        // GET: Admin/UserManagement
        public ActionResult UserManagement(int page = 1, string role = "", string status = "")
        {
            int pageSize = 10; // Số người dùng hiển thị trên mỗi trang
            var query = db.NguoiDungs.AsQueryable();

            // Áp dụng bộ lọc vai trò
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.VaiTro == role);
            }

            // Áp dụng bộ lọc trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Active" || status == "Inactive" || status == "Banned")
                {
                    query = query.Where(u => u.TrangThai == status);
                }
                else if (status == "Upgrade")
                {
                    // Lọc những người đang chờ nâng cấp thành người bán
                    query = query.Where(u => u.VaiTro != "Seller" && u.XetDuyetThanhNguoiBan == true);
                }
            }

            // Tính toán tổng số trang
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Đảm bảo trang hiện tại hợp lệ
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            // Lấy dữ liệu cho trang hiện tại
            var users = query
                .OrderByDescending(u => u.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Truyền dữ liệu phân trang vào ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;

            // Truyền các tham số lọc vào ViewBag để duy trì trạng thái khi phân trang
            ViewBag.Role = role;
            ViewBag.Status = status;

            return View(users);
        }

        // GET: Admin/UserDetails/5
        public ActionResult UserDetails(int id)
        {
            var nguoiDung = db.NguoiDungs.Find(id);
            if (nguoiDung == null)
            {
                return HttpNotFound();
            }

            // Nếu là người bán hoặc đang chờ nâng cấp
            if (nguoiDung.VaiTro == "Seller" || nguoiDung.TrangThai == "Upgrade" || nguoiDung.XetDuyetThanhNguoiBan == true)
            {
                // Tìm thông tin người bán (có thể đã được tạo khi đăng ký nâng cấp)
                var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
                ViewBag.NguoiBan = nguoiBan;

                if (nguoiBan != null)
                {
                    // Lấy danh sách chứng chỉ của người bán
                    var danhSachChungChi = db.AnhChungChis
                        .Where(c => c.MaNguoiBan == nguoiBan.MaNguoiBan)
                        .ToList();

                    // In thông tin về số lượng chứng chỉ tìm thấy để debug
                    System.Diagnostics.Debug.WriteLine("Tìm thấy " + danhSachChungChi.Count + " chứng chỉ cho người bán ID: " + nguoiBan.MaNguoiBan);

                    ViewBag.DanhSachChungChi = danhSachChungChi;
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("Không tìm thấy thông tin người bán cho người dùng ID: " + nguoiDung.MaNguoiDung);
                    // Xử lý khi không có thông tin người bán nhưng đang xin nâng cấp
                    ViewBag.DanhSachChungChi = new List<WebApplication1.Models.AnhChungChi>();
                }
            }

            return View(nguoiDung);
        }

        // POST: Admin/RestoreUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RestoreUser(int id)
        {
            try
            {
                var nguoiDung = db.NguoiDungs.Find(id);
                if (nguoiDung == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Kiểm tra trạng thái hiện tại
                if (nguoiDung.TrangThai != "Inactive")
                {
                    return Json(new { success = false, message = "Người dùng không ở trạng thái bị xóa." });
                }

                // Khôi phục trạng thái Active
                nguoiDung.TrangThai = "Active";
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/LockUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LockUser(int id)
        {
            try
            {
                var nguoiDung = db.NguoiDungs.Find(id);
                if (nguoiDung == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Kiểm tra xem có phải Admin không
                if (nguoiDung.VaiTro == "Admin")
                {
                    return Json(new { success = false, message = "Không thể khóa tài khoản Admin." });
                }

                nguoiDung.TrangThai = "Banned";
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/UnlockUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UnlockUser(int id)
        {
            try
            {
                var nguoiDung = db.NguoiDungs.Find(id);
                if (nguoiDung == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Chỉ mở khóa nếu trạng thái hiện tại là Banned
                if (nguoiDung.TrangThai != "Banned")
                {
                    return Json(new { success = false, message = "Người dùng không ở trạng thái bị khóa." });
                }

                nguoiDung.TrangThai = "Active";
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/UpgradeToSeller/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpgradeToSeller(int id)
        {
            try
            {
                var nguoiDung = db.NguoiDungs.Find(id);
                if (nguoiDung == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }
                // Cập nhật vai trò người dùng
                nguoiDung.VaiTro = "Seller";
                nguoiDung.XetDuyetThanhNguoiBan = false;
                nguoiDung.TrangThai = "Active";
                db.SaveChanges();
                return Json(new { success = true, message = "Nâng cấp thành người bán thành công." });
                //// Kiểm tra xem người dùng có phải là người bán hay không
                //var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
                //if (nguoiBan != null)
                //{
                //    // Cập nhật vai trò người dùng
                //    nguoiDung.VaiTro = "Seller";
                //    nguoiDung.XetDuyetThanhNguoiBan = false;
                //    nguoiDung.TrangThai = "Active";
                //    return Json(new { success = true, message = "Nâng cấp thành người bán thành công." });
                //}
                ////else
                ////{
                ////    // Kiểm tra xem đã có thông tin người bán chưa
                ////    var nguoiban = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
                ////    if (nguoiban == null)
                ////    {
                ////        // Tạo thông tin người bán mặc định
                ////        nguoiban = new WebApplication1.Models.NguoiBan
                ////        {
                ////            MaNguoiDung = nguoiDung.MaNguoiDung,
                ////            TenCuaHang = nguoiDung.TenNguoiDung + "'s Shop",
                ////            MoTaCuaHang = "Cửa hàng mới",
                ////            DiaChiCuaHang = "Chưa cập nhật",
                ////            SoDienThoaiCuaHang = nguoiDung.SoDienThoai,
                ////            NgayTao = DateTime.Now
                ////        };

                ////        db.NguoiBans.Add(nguoiban);
                ////        return Json(new { success = true, message = "Nâng cấp thành người bán thành công." });

                ////    }
                ////}

                //db.SaveChanges();

                //return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/DeleteUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int id)
        {
            try
            {
                var nguoiDung = db.NguoiDungs.Find(id);
                if (nguoiDung == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Kiểm tra xem có phải Admin không
                if (nguoiDung.VaiTro == "Admin")
                {
                    return Json(new { success = false, message = "Không thể xóa tài khoản Admin." });
                }

                // Đánh dấu là đã xóa thay vì xóa thật sự
                nguoiDung.TrangThai = "Inactive";
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/RejectUpgradeToSeller/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectUpgradeToSeller(int id)
        {
            try
            {
                var nguoiDung = db.NguoiDungs.Find(id);
                if (nguoiDung == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Kiểm tra xem người dùng có yêu cầu nâng cấp không
                if (nguoiDung.XetDuyetThanhNguoiBan != true)
                {
                    return Json(new { success = false, message = "Người dùng không có yêu cầu nâng cấp." });
                }

                // Tìm thông tin người bán
                var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);

                if (nguoiBan != null)
                {
                    // Tìm và xóa tất cả chứng chỉ của người bán
                    var danhSachChungChi = db.AnhChungChis.Where(c => c.MaNguoiBan == nguoiBan.MaNguoiBan).ToList();

                    foreach (var chungChi in danhSachChungChi)
                    {
                        // Xóa file ảnh chứng chỉ nếu có
                        if (!string.IsNullOrEmpty(chungChi.DuongDanAnh))
                        {
                            var duongDanFile = Server.MapPath(chungChi.DuongDanAnh);
                            if (System.IO.File.Exists(duongDanFile))
                            {
                                try
                                {
                                    System.IO.File.Delete(duongDanFile);
                                }
                                catch (Exception ex)
                                {
                                    // Ghi log lỗi nếu không xóa được file
                                    System.Diagnostics.Debug.WriteLine("Không thể xóa file: " + ex.Message);
                                }
                            }
                        }

                        // Xóa chứng chỉ khỏi database
                        db.AnhChungChis.Remove(chungChi);
                    }

                    // Xóa thông tin người bán
                    db.NguoiBans.Remove(nguoiBan);
                }


                // Hủy trạng thái chờ nâng cấp
                nguoiDung.XetDuyetThanhNguoiBan = false;

                // Đảm bảo vai trò và trạng thái phù hợp
                if (nguoiDung.VaiTro != "Admin")
                {
                    nguoiDung.VaiTro = "Buyer";
                }

                if (nguoiDung.TrangThai == "Upgrade")
                {
                    nguoiDung.TrangThai = "Active";
                }

                // Đánh dấu đã bị từ chối để hiển thị thông báo
                nguoiDung.BiTuChoiNangCap = true;
                nguoiDung.NgayTuChoiNangCap = DateTime.Now;

                db.SaveChanges();

                return Json(new { success = true, message = "Đã từ chối nâng cấp và xóa dữ liệu liên quan." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // GET: Admin/UserStatistics
        public ActionResult UserStatistics()
        {
            ViewBag.TotalUsers = db.NguoiDungs.Count();
            ViewBag.ActiveUsers = db.NguoiDungs.Count(u => u.TrangThai == "Active");
            ViewBag.InactiveUsers = db.NguoiDungs.Count(u => u.TrangThai == "Inactive");
            ViewBag.LockedUsers = db.NguoiDungs.Count(u => u.TrangThai == "Locked");
            ViewBag.Buyers = db.NguoiDungs.Count(u => u.VaiTro == "Buyer");
            ViewBag.Sellers = db.NguoiDungs.Count(u => u.VaiTro == "Seller");
            ViewBag.Admins = db.NguoiDungs.Count(u => u.VaiTro == "Admin");

            return View();
        }

        [HttpPost]
        public ActionResult EmptyForm()
        {
            // This is just an empty action to support the CSRF token form
            return new EmptyResult();
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