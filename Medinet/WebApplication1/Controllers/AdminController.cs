﻿using Org.BouncyCastle.Utilities.Net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private MedinetDATN db = new MedinetDATN();

        // GET: Admin/UserManagement
        [Authorize]
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
        [Authorize]
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
        //[Authorize]
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult RestoreUser(int id)
        //{
        //    try
        //    {
        //        var nguoiDung = db.NguoiDungs.Find(id);
        //        if (nguoiDung == null)
        //        {
        //            return Json(new { success = false, message = "Không tìm thấy người dùng." });
        //        }

        //        // Kiểm tra trạng thái hiện tại
        //        if (nguoiDung.TrangThai != "Inactive")
        //        {
        //            return Json(new { success = false, message = "Người dùng không ở trạng thái bị xóa." });
        //        }

        //        // Khôi phục trạng thái Active
        //        nguoiDung.TrangThai = "Active";
        //        db.SaveChanges();

        //        return Json(new { success = true });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Lỗi: " + ex.Message });
        //    }
        //}
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RestoreUser(int id)
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

                // Tạo thông báo cho người dùng
                var thongBao = new ThongBao
                {
                    MaNguoiDung = nguoiDung.MaNguoiDung,
                    LoaiThongBao = "TaiKhoan",
                    TieuDe = "Tài khoản đã được khôi phục",
                    TinNhan = "Chúc mừng! Tài khoản của bạn đã được khôi phục thành công. Bạn đã có thể đăng nhập và sử dụng tất cả các tính năng.",
                    MucDoQuanTrong = 2, // Mức độ quan trọng cao
                    DuongDanChiTiet = "/NguoiDungs/Profile",
                    NgayTao = DateTime.Now,
                    TrangThai = "Chưa đọc"
                };
                db.ThongBaos.Add(thongBao);
                db.SaveChanges();

                // Gửi email thông báo
                if (!string.IsNullOrEmpty(nguoiDung.Email))
                {
                    try
                    {
                        string emailContent = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9;'>
                        <div style='background-color: #5cb85c; padding: 20px; text-align: center; border-radius: 5px;'>
                            <h2 style='color: white; margin: 0;'>Tài khoản đã được khôi phục</h2>
                        </div>
                        
                        <div style='background-color: white; padding: 20px; margin-top: 20px; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                            <p>Kính gửi {nguoiDung.TenNguoiDung},</p>
                            
                            <p>Chúng tôi xin thông báo rằng tài khoản của bạn đã được khôi phục thành công.</p>
                            
                            <div style='background-color: #d1e7dd; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <h3 style='margin-top: 0; color: #0f5132;'>✓ Tài khoản đã kích hoạt</h3>
                                <p style='margin-bottom: 0;'>Bây giờ bạn có thể đăng nhập và sử dụng tất cả các tính năng của hệ thống.</p>
                            </div>
                            
                            <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với bộ phận hỗ trợ của chúng tôi.</p>
                            
                            <p>Trân trọng,<br/><strong>Ban Quản Trị</strong></p>
                        </div>
                        
                        <div style='text-align: center; margin-top: 20px; color: #6c757d; font-size: 12px;'>
                            <p>Đây là email tự động, vui lòng không trả lời email này.</p>
                        </div>
                    </div>";

                        await SendEmailAsync(nguoiDung.Email, "Thông báo khôi phục tài khoản", emailContent);
                        System.Diagnostics.Debug.WriteLine($"Email khôi phục tài khoản đã được gửi tới {nguoiDung.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        // Log lỗi nhưng vẫn trả về thành công vì tài khoản đã được khôi phục
                        System.Diagnostics.Debug.WriteLine($"Lỗi gửi email khôi phục tài khoản: {emailEx.Message}");
                    }
                }

                return Json(new { success = true, message = "Khôi phục tài khoản thành công và đã gửi thông báo." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/LockUser/5
        //[Authorize]
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult LockUser(int id)
        //{
        //    try
        //    {
        //        var nguoiDung = db.NguoiDungs.Find(id);
        //        if (nguoiDung == null)
        //        {
        //            return Json(new { success = false, message = "Không tìm thấy người dùng." });
        //        }

        //        // Kiểm tra xem có phải Admin không
        //        if (nguoiDung.VaiTro == "Admin")
        //        {
        //            return Json(new { success = false, message = "Không thể khóa tài khoản Admin." });
        //        }

        //        nguoiDung.TrangThai = "Banned";
        //        db.SaveChanges();

        //        return Json(new { success = true });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Lỗi: " + ex.Message });
        //    }
        //}

        // POST: Admin/UnlockUser/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> LockUser(int id)
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

                //// Tạo thông báo cho người dùng
                //var thongBao = new ThongBao
                //{
                //    MaNguoiDung = nguoiDung.MaNguoiDung,
                //    LoaiThongBao = "TaiKhoan",
                //    TieuDe = "Tài khoản đã bị khóa",
                //    TinNhan = "Tài khoản của bạn đã bị khóa do vi phạm quy định của hệ thống. Vui lòng liên hệ ban quản trị để biết thêm chi tiết.",
                //    MucDoQuanTrong = 3, // Mức độ khẩn cấp cao nhất
                //    DuongDanChiTiet = "#",
                //    NgayTao = DateTime.Now,
                //    TrangThai = "Chưa đọc"
                //};
                //db.ThongBaos.Add(thongBao);
                //db.SaveChanges();

                // Gửi email thông báo
                if (!string.IsNullOrEmpty(nguoiDung.Email))
                {
                    try
                    {
                        string emailContent = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9;'>
                        <div style='background-color: #dc3545; padding: 20px; text-align: center; border-radius: 5px;'>
                            <h2 style='color: white; margin: 0;'>Tài khoản đã bị khóa</h2>
                        </div>
                        
                        <div style='background-color: white; padding: 20px; margin-top: 20px; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                            <p>Kính gửi {nguoiDung.TenNguoiDung},</p>
                            
                            <p>Chúng tôi xin thông báo rằng tài khoản của bạn đã bị khóa do vi phạm các quy định của hệ thống.</p>
                            
                            <div style='background-color: #f8d7da; padding: 15px; border-radius: 5px; margin: 20px 0; border: 1px solid #f5c6cb;'>
                                <h3 style='margin-top: 0; color: #721c24;'>⚠️ Thông tin quan trọng</h3>
                                <ul style='margin-bottom: 0; padding-left: 20px;'>
                                    <li>Ngày khóa: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}</li>
                                    <li>Trạng thái: Tài khoản đã bị khóa</li>
                                    <li>Tác động: Bạn không thể đăng nhập vào hệ thống</li>
                                </ul>
                            </div>
                            
                            <h3 style='color: #dc3545;'>Các bước tiếp theo:</h3>
                            <p>1. Liên hệ với ban quản trị để biết lý do cụ thể</p>
                            <p>2. Cung cấp thông tin cần thiết để xem xét khôi phục tài khoản</p>
                            <p>3. Cải thiện hành vi để tuân thủ quy định hệ thống</p>
                            
                            <div style='background-color: #cce5ff; padding: 15px; border-radius: 5px; margin: 20px 0; border: 1px solid #b8daff;'>
                                <h3 style='margin-top: 0; color: #004085;'>📞 Thông tin liên hệ hỗ trợ</h3>
                                <p style='margin-bottom: 0;'>Email: medinetpro183@gmail.com</p>
                                <p style='margin-bottom: 0;'>Hotline: 19006666</p>
                            </div>
                            
                            <p>Chúng tôi rất tiếc về sự bất tiện này và mong rằng bạn sẽ hiểu và hợp tác với chúng tôi.</p>
                            
                            <p>Trân trọng,<br/><strong>Ban Quản Trị</strong></p>
                        </div>
                        
                        <div style='text-align: center; margin-top: 20px; color: #6c757d; font-size: 12px;'>
                            <p>Đây là email tự động, vui lòng không trả lời email này.</p>
                        </div>
                    </div>";

                        await SendEmailAsync(nguoiDung.Email, "THÔNG BÁO QUAN TRỌNG: Tài khoản đã bị khóa", emailContent);
                        System.Diagnostics.Debug.WriteLine($"Email thông báo khóa tài khoản đã được gửi tới {nguoiDung.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        // Log lỗi nhưng vẫn trả về thành công vì tài khoản đã được khóa
                        System.Diagnostics.Debug.WriteLine($"Lỗi gửi email thông báo khóa tài khoản: {emailEx.Message}");
                    }
                }

                return Json(new { success = true, message = "Khóa tài khoản thành công và đã gửi thông báo." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        //[Authorize]
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult UnlockUser(int id)
        //{
        //    try
        //    {
        //        var nguoiDung = db.NguoiDungs.Find(id);
        //        if (nguoiDung == null)
        //        {
        //            return Json(new { success = false, message = "Không tìm thấy người dùng." });
        //        }

        //        // Chỉ mở khóa nếu trạng thái hiện tại là Banned
        //        if (nguoiDung.TrangThai != "Banned")
        //        {
        //            return Json(new { success = false, message = "Người dùng không ở trạng thái bị khóa." });
        //        }

        //        nguoiDung.TrangThai = "Active";
        //        db.SaveChanges();

        //        return Json(new { success = true });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Lỗi: " + ex.Message });
        //    }
        //}

        // POST: Admin/UpgradeToSeller/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> UnlockUser(int id)
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

                // Tạo thông báo cho người dùng
                var thongBao = new ThongBao
                {
                    MaNguoiDung = nguoiDung.MaNguoiDung,
                    LoaiThongBao = "TaiKhoan",
                    TieuDe = "Tài khoản đã được mở khóa",
                    TinNhan = "Tài khoản của bạn đã được mở khóa thành công. Giờ đây bạn có thể đăng nhập và sử dụng tất cả các tính năng của hệ thống.",
                    MucDoQuanTrong = 2, // Mức độ quan trọng cao
                    DuongDanChiTiet = "/NguoiDungs/Profile",
                    NgayTao = DateTime.Now,
                    TrangThai = "Chưa đọc"
                };
                db.ThongBaos.Add(thongBao);
                db.SaveChanges();

                // Gửi email thông báo
                if (!string.IsNullOrEmpty(nguoiDung.Email))
                {
                    try
                    {
                        string emailContent = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9;'>
                        <div style='background-color: #28a745; padding: 20px; text-align: center; border-radius: 5px;'>
                            <h2 style='color: white; margin: 0;'>Tài khoản đã được mở khóa</h2>
                        </div>
                        
                        <div style='background-color: white; padding: 20px; margin-top: 20px; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                            <p>Kính gửi {nguoiDung.TenNguoiDung},</p>
                            
                            <p>Chúng tôi xin thông báo rằng tài khoản của bạn đã được mở khóa thành công.</p>
                            
                            <div style='background-color: #d4edda; padding: 15px; border-radius: 5px; margin: 20px 0; border: 1px solid #c3e6cb;'>
                                <h3 style='margin-top: 0; color: #155724;'>✓ Trạng thái hiện tại</h3>
                                <ul style='margin-bottom: 0; padding-left: 20px;'>
                                    <li>Ngày mở khóa: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}</li>
                                    <li>Trạng thái: Tài khoản đã kích hoạt</li>
                                    <li>Quyền truy cập: Đã được khôi phục</li>
                                </ul>
                            </div>
                            
                            <h3 style='color: #28a745;'>Bạn có thể:</h3>
                            <p>• Đăng nhập vào hệ thống</p>
                            <p>• Sử dụng tất cả các tính năng như trình bày</p>
                            <p>• Tham gia lại các hoạt động bình thường</p>
                            
                            <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0; border: 1px solid #ffeeba;'>
                                <h3 style='margin-top: 0; color: #856404;'>⚠️ Lưu ý quan trọng</h3>
                                <p style='margin-bottom: 0;'>Vui lòng tuân thủ các quy định của hệ thống để tránh bị khóa lại tài khoản.</p>
                            </div>
                            
                            <p>Cảm ơn sự hiểu biết và hợp tác của bạn!</p>
                            
                            <p>Trân trọng,<br/><strong>Ban Quản Trị</strong></p>
                        </div>
                        
                        <div style='text-align: center; margin-top: 20px; color: #6c757d; font-size: 12px;'>
                            <p>Đây là email tự động, vui lòng không trả lời email này.</p>
                        </div>
                    </div>";

                        await SendEmailAsync(nguoiDung.Email, "Thông báo: Tài khoản đã được mở khóa", emailContent);
                        System.Diagnostics.Debug.WriteLine($"Email thông báo mở khóa tài khoản đã được gửi tới {nguoiDung.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        // Log lỗi nhưng vẫn trả về thành công vì tài khoản đã được mở khóa
                        System.Diagnostics.Debug.WriteLine($"Lỗi gửi email thông báo mở khóa tài khoản: {emailEx.Message}");
                    }
                }

                return Json(new { success = true, message = "Mở khóa tài khoản thành công và đã gửi thông báo." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        [Authorize]
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

                // Tạo thông báo cho người dùng khi được nâng cấp thành người bán
                var thongBao = new ThongBao
                {
                    MaNguoiDung = nguoiDung.MaNguoiDung,
                    LoaiThongBao = "TaiKhoan",
                    TieuDe = "Nâng cấp thành người bán thành công",
                    TinNhan = "Chúc mừng! Tài khoản của bạn đã được nâng cấp thành người bán. Bây giờ bạn có thể bắt đầu đăng bán sản phẩm trên hệ thống.",
                    MucDoQuanTrong = 2, // Mức độ quan trọng cao
                    DuongDanChiTiet = "/NguoiDungs/EditSellerProfile",
                    NgayTao = DateTime.Now
                };

                // Thêm thông báo vào database
                db.ThongBaos.Add(thongBao);

                db.SaveChanges();
                return Json(new { success = true, message = "Nâng cấp thành người bán thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/DeleteUser/5
        //[Authorize]
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult DeleteUser(int id)
        //{
        //    try
        //    {
        //        var nguoiDung = db.NguoiDungs.Find(id);
        //        if (nguoiDung == null)
        //        {
        //            return Json(new { success = false, message = "Không tìm thấy người dùng." });
        //        }

        //        // Kiểm tra xem có phải Admin không
        //        if (nguoiDung.VaiTro == "Admin")
        //        {
        //            return Json(new { success = false, message = "Không thể xóa tài khoản Admin." });
        //        }

        //        // Đánh dấu là đã xóa thay vì xóa thật sự
        //        nguoiDung.TrangThai = "Inactive";
        //        db.SaveChanges();

        //        return Json(new { success = true });
        //    }
        //    catch (Exception ex)
        //    {
        //        return Json(new { success = false, message = "Lỗi: " + ex.Message });
        //    }
        //}

        // POST: Admin/RejectUpgradeToSeller/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteUser(int id)
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

                //// Tạo thông báo cho người dùng
                //var thongBao = new ThongBao
                //{
                //    MaNguoiDung = nguoiDung.MaNguoiDung,
                //    LoaiThongBao = "TaiKhoan",
                //    TieuDe = "Tài khoản đã bị vô hiệu hóa",
                //    TinNhan = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ ban quản trị để biết thêm chi tiết và yêu cầu khôi phục nếu cần.",
                //    MucDoQuanTrong = 3, // Mức độ khẩn cấp cao nhất
                //    DuongDanChiTiet = "#",
                //    NgayTao = DateTime.Now,
                //    TrangThai = "Chưa đọc"
                //};
                //db.ThongBaos.Add(thongBao);
                //db.SaveChanges();

                // Gửi email thông báo
                if (!string.IsNullOrEmpty(nguoiDung.Email))
                {
                    try
                    {
                        string emailContent = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9;'>
                        <div style='background-color: #ffc107; padding: 20px; text-align: center; border-radius: 5px;'>
                            <h2 style='color: #000; margin: 0;'>Tài khoản đã bị vô hiệu hóa</h2>
                        </div>
                        
                        <div style='background-color: white; padding: 20px; margin-top: 20px; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                            <p>Kính gửi {nguoiDung.TenNguoiDung},</p>
                            
                            <p>Chúng tôi xin thông báo rằng tài khoản của bạn đã bị vô hiệu hóa theo quyết định của Ban Quản Trị.</p>
                            
                            <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; margin: 20px 0; border: 1px solid #ffeeba;'>
                                <h3 style='margin-top: 0; color: #856404;'>⚠️ Trạng thái tài khoản</h3>
                                <ul style='margin-bottom: 0; padding-left: 20px;'>
                                    <li>Ngày vô hiệu hóa: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}</li>
                                    <li>Trạng thái: Không hoạt động</li>
                                    <li>Quyền truy cập: Đã bị hạn chế</li>
                                </ul>
                            </div>
                            
                            <h3 style='color: #bd8415;'>Những gì sẽ xảy ra:</h3>
                            <p>• Bạn không thể đăng nhập vào hệ thống</p>
                            <p>• Dữ liệu và thông tin cá nhân sẽ được bảo mật</p>
                            <p>• Tất cả hoạt động liên quan đã bị tạm dừng</p>
                            
                            <div style='background-color: #d1ecf1; padding: 15px; border-radius: 5px; margin: 20px 0; border: 1px solid #bee5eb;'>
                                <h3 style='margin-top: 0; color: #0c5460;'>📋 Để khôi phục tài khoản</h3>
                                <p>1. Gửi email đến: medinetpro183@gmail.com</p>
                                <p>2. Cung cấp đầy đủ thông tin xác thực</p>
                                <p>3. Giải thích lý do yêu cầu khôi phục</p>
                                <p style='margin-bottom: 0;'>4. Chờ phản hồi từ Ban Quản Trị (3-5 ngày làm việc)</p>
                            </div>
                            
                            <p>Chúng tôi rất tiếc vì sự bất tiện này. Nếu bạn cảm thấy có nhầm lẫn, vui lòng liên hệ ngay với ban quản trị.</p>
                            
                            <p>Trân trọng,<br/><strong>Ban Quản Trị</strong></p>
                        </div>
                       
                    </div>";

                        await SendEmailAsync(nguoiDung.Email, "THÔNG BÁO: Tài khoản đã bị vô hiệu hóa", emailContent);
                        System.Diagnostics.Debug.WriteLine($"Email thông báo vô hiệu hóa tài khoản đã được gửi tới {nguoiDung.Email}");
                    }
                    catch (Exception emailEx)
                    {
                        // Log lỗi nhưng vẫn trả về thành công vì tài khoản đã được xóa
                        System.Diagnostics.Debug.WriteLine($"Lỗi gửi email thông báo xóa tài khoản: {emailEx.Message}");
                    }
                }

                return Json(new { success = true, message = "Xóa tài khoản thành công và đã gửi thông báo." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }
        [Authorize]
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

                // Tạo thông báo cho người dùng khi bị từ chối nâng cấp thành người bán
                var thongBao = new ThongBao
                {
                    MaNguoiDung = nguoiDung.MaNguoiDung,
                    LoaiThongBao = "TaiKhoan",
                    TieuDe = "Yêu cầu nâng cấp thành người bán bị từ chối",
                    TinNhan = "Yêu cầu trở thành người bán của bạn đã bị từ chối. Vui lòng kiểm tra lại thông tin và thử lại sau 30 ngày.",
                    MucDoQuanTrong = 2, // Mức độ quan trọng cao
                    DuongDanChiTiet = "/NguoiDungs/Profile",
                    NgayTao = DateTime.Now
                };

                // Thêm thông báo vào database
                db.ThongBaos.Add(thongBao);

                db.SaveChanges();

                return Json(new { success = true, message = "Đã từ chối nâng cấp và xóa dữ liệu liên quan." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // GET: Admin/UserStatistics
        [Authorize]
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



        // GET: Admin/ProductManagement
        [Authorize]
        public ActionResult ProductManagement(int page = 1, string category = "", string status = "")
        {
            int pageSize = 10; // Số sản phẩm hiển thị trên mỗi trang
            var query = db.SanPhams.AsQueryable();

            // Áp dụng bộ lọc danh mục
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.MaDanhMuc.ToString() == category);
            }

            // Áp dụng bộ lọc trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Đã phê duyệt" || status == "Đang chờ xử lý" || status == "Bị từ chối")
                {
                    query = query.Where(p => p.TrangThai == status);
                }
            }

            // Tính toán tổng số trang
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Đảm bảo trang hiện tại hợp lệ
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            // Lấy dữ liệu cho trang hiện tại
            var products = query
                .OrderByDescending(p => p.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Chuẩn bị thêm dữ liệu cho mỗi sản phẩm
            var danhSachAnh = new Dictionary<int, string>();
            foreach (var product in products)
            {
                // Lấy ảnh đầu tiên của sản phẩm (không quan tâm đến LaChinh)
                var anhDauTien = db.AnhSanPhams.Where(a => a.MaSanPham == product.MaSanPham)
                                               .OrderBy(a => a.MaAnh)
                                               .FirstOrDefault();
                var duongDanAnh = anhDauTien != null ? anhDauTien.DuongDanAnh : "~/Content/Images/default-product.png";
                danhSachAnh[product.MaSanPham] = duongDanAnh;
            }

            ViewBag.DanhSachAnh = danhSachAnh;

            // Truyền dữ liệu phân trang vào ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;

            // Truyền các tham số lọc vào ViewBag để duy trì trạng thái khi phân trang
            ViewBag.Category = category;
            ViewBag.Status = status;

            // Lấy danh sách danh mục để hiển thị trong dropdown lọc
            ViewBag.Categories = db.DanhMucSanPhams.ToList();

            return View(products);
        }

        // GET: Admin/ProductDetails/5
        [Authorize]
        public ActionResult ProductDetails(int id)
        {
            var sanPham = db.SanPhams.Find(id);
            if (sanPham == null)
            {
                return HttpNotFound();
            }

            // Lấy thông tin danh mục
            var danhMuc = db.DanhMucSanPhams.Find(sanPham.MaDanhMuc);
            ViewBag.DanhMuc = danhMuc;

            // Lấy thông tin người bán
            var nguoiBan = db.NguoiBans.Find(sanPham.MaNguoiBan);
            ViewBag.NguoiBan = nguoiBan;

            // Lấy danh sách ảnh sản phẩm theo thứ tự
            var danhSachAnh = db.AnhSanPhams
                .Where(a => a.MaSanPham == sanPham.MaSanPham)
                .OrderBy(a => a.MaAnh)
                .ToList();
            ViewBag.DanhSachAnh = danhSachAnh;

            return View(sanPham);
        }

        // POST: Admin/ApproveProduct/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveProduct(int id)
        {
            try
            {
                var sanPham = db.SanPhams.Find(id);
                if (sanPham == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                }

                // Kiểm tra trạng thái hiện tại
                if (sanPham.TrangThai == "Đã phê duyệt")
                {
                    return Json(new { success = false, message = "Sản phẩm đã được duyệt." });
                }

                // Duyệt sản phẩm
                sanPham.TrangThai = "Đã phê duyệt";
                sanPham.NgayCapNhat = DateTime.Now;

                // Tìm thông tin người bán để gửi thông báo
                var nguoiBan = db.NguoiBans.Find(sanPham.MaNguoiBan);
                if (nguoiBan != null)
                {
                    // Tạo thông báo cho người bán khi sản phẩm được phê duyệt
                    var thongBao = new ThongBao
                    {
                        MaNguoiDung = nguoiBan.MaNguoiDung,
                        LoaiThongBao = "SanPham",
                        TieuDe = "Sản phẩm đã được phê duyệt",
                        TinNhan = "Sản phẩm \"" + sanPham.TenSanPham + "\" của bạn đã được phê duyệt và hiển thị trên hệ thống.",
                        MucDoQuanTrong = 1,
                        DuongDanChiTiet = "/NguoiBans/QuanLySanPham/" + nguoiBan.MaNguoiBan,
                        NgayTao = DateTime.Now
                    };

                    // Thêm thông báo vào database
                    db.ThongBaos.Add(thongBao);
                }


                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/RejectProduct/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectProduct(int id)
        {
            try
            {
                var sanPham = db.SanPhams.Find(id);
                if (sanPham == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                }

                // Kiểm tra trạng thái hiện tại
                if (sanPham.TrangThai == "Bị từ chối")
                {
                    return Json(new { success = false, message = "Sản phẩm đã bị từ chối." });
                }

                // Từ chối sản phẩm
                sanPham.TrangThai = "Bị từ chối";
                sanPham.NgayCapNhat = DateTime.Now;

                // Tìm thông tin người bán để gửi thông báo
                var nguoiBan = db.NguoiBans.Find(sanPham.MaNguoiBan);
                if (nguoiBan != null)
                {
                    // Tạo thông báo cho người bán khi sản phẩm bị từ chối
                    var thongBao = new ThongBao
                    {
                        MaNguoiDung = nguoiBan.MaNguoiDung,
                        LoaiThongBao = "SanPham",
                        TieuDe = "Sản phẩm không được phê duyệt",
                        TinNhan = "Sản phẩm \"" + sanPham.TenSanPham + "\" của bạn không được phê duyệt. Vui lòng kiểm tra lại thông tin sản phẩm và cập nhật để đăng lại.",
                        MucDoQuanTrong = 2, // Mức độ quan trọng cao
                        DuongDanChiTiet = "/NguoiBans/QuanLySanPham/" + nguoiBan.MaNguoiBan,
                        NgayTao = DateTime.Now
                    };

                    // Thêm thông báo vào database
                    db.ThongBaos.Add(thongBao);
                }


                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/DeleteProduct/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProduct(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var sanPham = db.SanPhams.Find(id);
                    if (sanPham == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                    }

                    // 1. Xóa hết các dữ liệu liên quan đến sản phẩm này

                    // Xóa các chi tiết đơn hàng liên quan
                    var chiTietDonHangs = db.ChiTietDonHangs.Where(c => c.MaSanPham == id).ToList();
                    db.ChiTietDonHangs.RemoveRange(chiTietDonHangs);

                    // Xóa các mục giỏ hàng liên quan
                    var gioHangs = db.GioHangs.Where(g => g.MaSanPham == id).ToList();
                    db.GioHangs.RemoveRange(gioHangs);

                    // Xóa các bình luận/đánh giá liên quan
                    var danhGias = db.DanhGiaSanPhams.Where(d => d.MaSanPham == id).ToList();
                    db.DanhGiaSanPhams.RemoveRange(danhGias);

                    // Xóa các ảnh sản phẩm
                    var anhSanPhams = db.AnhSanPhams.Where(a => a.MaSanPham == id).ToList();
                    db.AnhSanPhams.RemoveRange(anhSanPhams);

                    // 2. Xóa sản phẩm
                    db.SanPhams.Remove(sanPham);

                    // Lưu thông tin người bán và tên sản phẩm trước khi xóa để tạo thông báo sau này
                    int maNguoiBan = sanPham.MaNguoiBan;
                    var nguoiBan = db.NguoiBans.Find(maNguoiBan);
                    string tenSanPham = sanPham.TenSanPham;
                    // 3. Tạo thông báo cho người bán nếu nguoiBan không null
                    if (nguoiBan != null)
                    {
                        var thongBao = new ThongBao
                        {
                            MaNguoiDung = nguoiBan.MaNguoiDung,
                            LoaiThongBao = "SanPham",
                            TieuDe = "Sản phẩm đã bị xóa",
                            TinNhan = "Sản phẩm \"" + tenSanPham + "\" của bạn đã bị xóa khỏi hệ thống.",
                            MucDoQuanTrong = 2, // Mức độ quan trọng cao
                            DuongDanChiTiet = "/NguoiBans/QuanLySanPham/" + nguoiBan.MaNguoiBan,
                            NgayTao = DateTime.Now
                        };

                        // Thêm thông báo vào database
                        db.ThongBaos.Add(thongBao);
                    }

                    // 3. Lưu thay đổi và commit transaction
                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }
            }
        }



        // GET: Admin/CategoryManagement
        [Authorize]
        public ActionResult CategoryManagement(int page = 1, string searchTerm = "")
        {
            int pageSize = 10; // Number of categories per page
            var query = db.DanhMucSanPhams.AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.TenDanhMuc.Contains(searchTerm));
            }

            // Calculate total pages
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Ensure current page is valid
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            // Get data for the current page
            var categories = query
                .OrderByDescending(c => c.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Pass pagination data to ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchTerm = searchTerm;

            // Get product count for each category
            var categoryCounts = new Dictionary<int, int>();
            foreach (var category in categories)
            {
                categoryCounts[category.MaDanhMuc] = db.SanPhams.Count(p => p.MaDanhMuc == category.MaDanhMuc);
            }
            ViewBag.CategoryCounts = categoryCounts;

            return View(categories);
        }

        // GET: Admin/CategoryDetails/5
        [Authorize]
        public ActionResult CategoryDetails(int id)
        {
            var danhMuc = db.DanhMucSanPhams.Find(id);
            if (danhMuc == null)
            {
                return HttpNotFound();
            }

            // Lấy thông tin danh mục cha nếu có
            if (danhMuc.MaDanhMucCha.HasValue)
            {
                var danhMucCha = db.DanhMucSanPhams.Find(danhMuc.MaDanhMucCha.Value);
                ViewBag.ParentCategory = danhMucCha;
            }

            // Lấy danh sách các danh mục con
            var danhMucCon = db.DanhMucSanPhams.Where(c => c.MaDanhMucCha == id).ToList();
            ViewBag.ChildCategories = danhMucCon;

            // Lấy sản phẩm trong danh mục
            var sanPhamTrongDanhMuc = db.SanPhams.Where(p => p.MaDanhMuc == id).ToList();
            ViewBag.SanPhamTrongDanhMuc = sanPhamTrongDanhMuc;
            ViewBag.SoLuongSanPham = sanPhamTrongDanhMuc.Count();

            return View(danhMuc);
        }

        // GET: Admin/CreateCategory
        [Authorize]
        public ActionResult CreateCategory()
        {
            // Get list of categories for parent selection
            ViewBag.ParentCategories = db.DanhMucSanPhams.ToList();

            return View();
        }

        // POST: Admin/CreateCategory
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCategory([Bind(Include = "TenDanhMuc,MaDanhMucCha")] DanhMucSanPham danhMuc, HttpPostedFileBase categoryImage)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload hình ảnh nếu có
                if (categoryImage != null && categoryImage.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(categoryImage.FileName);
                    string extension = Path.GetExtension(fileName);

                    // Tạo tên file mới để tránh trùng lặp
                    string newFileName = "category_" + Guid.NewGuid().ToString().Substring(0, 8) + extension;

                    // Tạo đường dẫn lưu file
                    string relativePath = "~/Content/Images/Categories/" + newFileName;
                    string physicalPath = Server.MapPath(relativePath);

                    // Đảm bảo thư mục tồn tại
                    string directoryPath = Path.GetDirectoryName(physicalPath);
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    // Lưu file
                    categoryImage.SaveAs(physicalPath);

                    // Cập nhật đường dẫn ảnh cho danh mục
                    danhMuc.AnhDanhMucSanPham = relativePath;
                }

                danhMuc.NgayTao = DateTime.Now;
                db.DanhMucSanPhams.Add(danhMuc);
                db.SaveChanges();

                return RedirectToAction("CategoryManagement");
            }

            // Nếu không thành công, hiển thị lại form với thông báo lỗi
            ViewBag.ParentCategories = db.DanhMucSanPhams.ToList();
            return View(danhMuc);
        }

        // GET: Admin/EditCategory/5

        [Authorize]
        public ActionResult EditCategory(int id)
        {
            var danhMuc = db.DanhMucSanPhams.Find(id);
            if (danhMuc == null)
            {
                return HttpNotFound();
            }

            // Get all categories except the current one and its children for parent selection
            ViewBag.ParentCategories = db.DanhMucSanPhams
                .Where(c => c.MaDanhMuc != id)
                .ToList();

            return View(danhMuc);
        }

        // POST: Admin/EditCategory/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCategory([Bind(Include = "MaDanhMuc,TenDanhMuc,AnhDanhMucSanPham,MaDanhMucCha,NgayTao")] DanhMucSanPham danhMuc, HttpPostedFileBase categoryImage, bool? removeImage = false)
        {
            if (ModelState.IsValid)
            {
                var existingCategory = db.DanhMucSanPhams.Find(danhMuc.MaDanhMuc);
                if (existingCategory != null)
                {
                    existingCategory.TenDanhMuc = danhMuc.TenDanhMuc;
                    existingCategory.MaDanhMucCha = danhMuc.MaDanhMucCha;

                    // Xử lý ảnh danh mục
                    if (removeImage == true)
                    {
                        // Xóa file ảnh cũ nếu có
                        if (!string.IsNullOrEmpty(existingCategory.AnhDanhMucSanPham))
                        {
                            string physicalPath = Server.MapPath(existingCategory.AnhDanhMucSanPham);
                            if (System.IO.File.Exists(physicalPath))
                            {
                                try
                                {
                                    System.IO.File.Delete(physicalPath);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine("Không thể xóa file: " + ex.Message);
                                    // Ghi log lỗi
                                }
                            }
                        }

                        existingCategory.AnhDanhMucSanPham = null;
                    }

                    // Xử lý upload hình ảnh mới nếu có
                    if (categoryImage != null && categoryImage.ContentLength > 0)
                    {
                        // Xóa file ảnh cũ nếu có
                        if (!string.IsNullOrEmpty(existingCategory.AnhDanhMucSanPham))
                        {
                            string oldPhysicalPath = Server.MapPath(existingCategory.AnhDanhMucSanPham);
                            if (System.IO.File.Exists(oldPhysicalPath))
                            {
                                try
                                {
                                    System.IO.File.Delete(oldPhysicalPath);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine("Không thể xóa file: " + ex.Message);
                                }
                            }
                        }

                        string fileName = Path.GetFileName(categoryImage.FileName);
                        string extension = Path.GetExtension(fileName);

                        // Tạo tên file mới để tránh trùng lặp
                        string newFileName = "category_" + Guid.NewGuid().ToString().Substring(0, 8) + extension;

                        // Tạo đường dẫn lưu file
                        string relativePath = "~/Content/Images/Categories/" + newFileName;
                        string physicalPath = Server.MapPath(relativePath);

                        // Đảm bảo thư mục tồn tại
                        string directoryPath = Path.GetDirectoryName(physicalPath);
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }

                        // Lưu file
                        categoryImage.SaveAs(physicalPath);

                        // Cập nhật đường dẫn ảnh cho danh mục
                        existingCategory.AnhDanhMucSanPham = relativePath;
                    }

                    db.SaveChanges();
                    return RedirectToAction("CategoryDetails", new { id = danhMuc.MaDanhMuc });
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.ParentCategories = db.DanhMucSanPhams
                .Where(c => c.MaDanhMuc != danhMuc.MaDanhMuc)
                .ToList();

            return View(danhMuc);
        }

        // POST: Admin/DeleteCategory/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCategory(int id)
        {
            try
            {
                var danhMuc = db.DanhMucSanPhams.Find(id);
                if (danhMuc == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục." });
                }

                // Check if there are products in this category
                bool hasProducts = db.SanPhams.Any(p => p.MaDanhMuc == id);
                if (hasProducts)
                {
                    return Json(new { success = false, message = "Không thể xóa danh mục này vì có sản phẩm thuộc danh mục." });
                }

                // Check if there are child categories
                bool hasChildren = db.DanhMucSanPhams.Any(c => c.MaDanhMucCha == id);
                if (hasChildren)
                {
                    return Json(new { success = false, message = "Không thể xóa danh mục này vì có danh mục con." });
                }

                db.DanhMucSanPhams.Remove(danhMuc);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult EmptyForm()
        {
            // This is just an empty action to support the CSRF token form
            return new EmptyResult();
        }



        // GET: Admin/QuanLyKyQuy
        [Authorize]
        public ActionResult QuanLyKyQuy()
        {
            try
            {
                var escrows = db.Escrows
                    .Include(e => e.DonHang)
                    .Include(e => e.NguoiBan)
                    .OrderByDescending(e => e.NgayTao)
                    .ToList();

                return View(escrows);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tải dữ liệu ký quỹ: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải dữ liệu. Vui lòng thử lại sau!";
                return RedirectToAction("Index", "Admin");
            }
        }

        // GET: Admin/ChiTietKyQuy/5
        [Authorize]
        public ActionResult ChiTietKyQuy(int id)
        {
            try
            {
                var escrow = db.Escrows
                    .Include(e => e.DonHang)
                    .Include(e => e.DonHang.NguoiDung)
                    .Include(e => e.NguoiBan)
                    .Include(e => e.DonHang.ChiTietDonHangs)
                    .Include(e => e.DonHang.ChiTietDonHangs.Select(c => c.SanPham))
                    .FirstOrDefault(e => e.MaKyQuy == id);

                if (escrow == null)
                {
                    return HttpNotFound();
                }

                return View(escrow);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tải chi tiết ký quỹ: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải thông tin. Vui lòng thử lại sau!";
                return RedirectToAction("QuanLyKyQuy");
            }
        }

        // POST: Admin/GiaiNganKyQuy/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GiaiNganKyQuy(int id)
        {
            try
            {
                var escrow = db.Escrows.Find(id);
                if (escrow == null)
                {
                    return HttpNotFound();
                }

                if (escrow.TrangThai != "Đang giữ")
                {
                    TempData["Error"] = "Tiền ký quỹ đã được giải ngân hoặc hoàn trả!";
                    return RedirectToAction("ChiTietKyQuy", new { id = id });
                }

                // Cập nhật trạng thái
                escrow.TrangThai = "Đã giải ngân";
                escrow.NgayGiaiNgan = DateTime.Now;

                // Cập nhật đơn hàng
                var donHang = db.DonHangs.Find(escrow.MaDonHang);
                if (donHang != null)
                {
                    donHang.DaGiaiNganChoSeller = true;
                    donHang.TrangThaiDonHang = "Đã hoàn thành";
                    db.Entry(donHang).State = EntityState.Modified;

                    // Tạo thông báo cho người bán
                    var nguoiBan = db.NguoiBans.Find(donHang.MaNguoiBan);
                    if (nguoiBan != null)
                    {
                        var thongBaoNguoiBan = new ThongBao
                        {
                            MaNguoiDung = nguoiBan.MaNguoiDung,
                            LoaiThongBao = "TaiChinh",
                            TieuDe = "Đã nhận tiền từ đơn hàng",
                            TinNhan = $"Tiền từ đơn hàng #{donHang.MaDonHang} đã được giải ngân vào tài khoản của bạn với số tiền {escrow.TienChuyenChoNguoiBan:N0} VNĐ.",
                            MucDoQuanTrong = 2, // Quan trọng cao vì liên quan đến tài chính
                            DuongDanChiTiet = "/GiaoDich/ViNguoiBan",
                            NgayTao = DateTime.Now
                        };
                        db.ThongBaos.Add(thongBaoNguoiBan);
                    }

                }

                db.Entry(escrow).State = EntityState.Modified;
                await db.SaveChangesAsync();

                TempData["Success"] = $"Đã giải ngân {escrow.TienChuyenChoNguoiBan:N0} VNĐ cho người bán thành công!";
                return RedirectToAction("QuanLyKyQuy");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi giải ngân: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi giải ngân. Vui lòng thử lại sau!";
                return RedirectToAction("ChiTietKyQuy", new { id = id });
            }
        }

        // POST: Admin/HoanTienKyQuy/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> HoanTienKyQuy(int id)
        {
            try
            {
                var escrow = db.Escrows.Find(id);
                if (escrow == null)
                {
                    return HttpNotFound();
                }

                if (escrow.TrangThai != "Đang giữ")
                {
                    TempData["Error"] = "Tiền ký quỹ đã được giải ngân hoặc hoàn trả!";
                    return RedirectToAction("ChiTietKyQuy", new { id = id });
                }

                // Cập nhật trạng thái
                escrow.TrangThai = "Đã hoàn tiền";
                escrow.NgayGiaiNgan = DateTime.Now;

                // Cập nhật đơn hàng
                var donHang = db.DonHangs.Find(escrow.MaDonHang);
                if (donHang != null)
                {
                    donHang.TrangThaiDonHang = "Đã hủy";
                    db.Entry(donHang).State = EntityState.Modified;

                    // Tạo thông báo cho người bán
                    var nguoiBan = db.NguoiBans.Find(donHang.MaNguoiBan);
                    if (nguoiBan != null)
                    {
                        var thongBaoNguoiBan = new ThongBao
                        {
                            MaNguoiDung = nguoiBan.MaNguoiDung,
                            LoaiThongBao = "DonHang",
                            TieuDe = "Đơn hàng đã bị hủy",
                            TinNhan = $"Đơn hàng #{donHang.MaDonHang} đã bị hủy và tiền đã được hoàn trả cho tài khoản của bạn.",
                            MucDoQuanTrong = 2, // Quan trọng cao vì đơn hàng bị hủy
                            DuongDanChiTiet = "//GiaoDich/ViNguoiBan",
                            NgayTao = DateTime.Now
                        };
                        db.ThongBaos.Add(thongBaoNguoiBan);
                    }
                }

                db.Entry(escrow).State = EntityState.Modified;
                await db.SaveChangesAsync();

                TempData["Success"] = $"Đã hoàn {escrow.TongTien:N0} VNĐ cho người mua thành công!";
                return RedirectToAction("QuanLyKyQuy");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi hoàn tiền: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi hoàn tiền. Vui lòng thử lại sau!";
                return RedirectToAction("ChiTietKyQuy", new { id = id });
            }
        }

        // GET: Admin/ThongKeDoanhThu
        [Authorize]
        public ActionResult ThongKeDoanhThu(DateTime? tuNgay, DateTime? denNgay)
        {
            try
            {
                // Xử lý ngày mặc định nếu không có ngày truyền vào
                if (!tuNgay.HasValue)
                {
                    tuNgay = DateTime.Now.AddMonths(-1);
                }

                if (!denNgay.HasValue)
                {
                    denNgay = DateTime.Now;
                }

                // Lấy dữ liệu từ bảng Escrow
                var escrows = db.Escrows
                    .Where(e => e.TrangThai == "Đã giải ngân" &&
                              e.NgayGiaiNgan >= tuNgay.Value &&
                              e.NgayGiaiNgan <= denNgay.Value.AddDays(1))
                    .ToList();

                // Tính toán thống kê
                var tongDoanhThu = escrows.Sum(e => e.TongTien);
                var tongPhiNenTang = escrows.Sum(e => e.PhiNenTang);
                var tongTienChuyenChoNguoiBan = escrows.Sum(e => e.TienChuyenChoNguoiBan);

                // Thống kê theo ngày
                var thongKeTheoNgay = escrows
                    .GroupBy(e => e.NgayGiaiNgan.Value.Date)
                    .Select(g => new ThongKeNgayViewModel
                    {
                        Ngay = g.Key,
                        DoanhThu = g.Sum(e => e.TongTien),
                        PhiNenTang = g.Sum(e => e.PhiNenTang),
                        TienChuyenChoNguoiBan = g.Sum(e => e.TienChuyenChoNguoiBan)
                    })
                    .OrderByDescending(t => t.Ngay)
                    .ToList();

                var viewModel = new ThongKeDoanhThuViewModel
                {
                    TuNgay = tuNgay.Value,
                    DenNgay = denNgay.Value,
                    TongDoanhThu = tongDoanhThu,
                    TongPhiNenTang = tongPhiNenTang,
                    TongTienChuyenChoNguoiBan = tongTienChuyenChoNguoiBan,
                    ThongKeTheoNgay = thongKeTheoNgay
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi thống kê doanh thu: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tạo báo cáo. Vui lòng thử lại sau!";
                return RedirectToAction("QuanLyKyQuy");
            }
        }

        // GET: Admin/ThongKeNguoiBan
        [Authorize]
        public ActionResult ThongKeNguoiBan(DateTime? tuNgay, DateTime? denNgay)
        {
            try
            {
                // Xử lý ngày mặc định
                if (!tuNgay.HasValue)
                {
                    tuNgay = DateTime.Now.AddMonths(-1);
                }

                if (!denNgay.HasValue)
                {
                    denNgay = DateTime.Now;
                }

                // Lấy dữ liệu từ bảng Escrow
                var escrows = db.Escrows
                    .Where(e => e.TrangThai == "Đã giải ngân" &&
                              e.NgayGiaiNgan >= tuNgay.Value &&
                              e.NgayGiaiNgan <= denNgay.Value.AddDays(1))
                    .Include(e => e.NguoiBan)
                    .ToList();

                // Thống kê theo người bán
                var thongKeTheoNguoiBan = escrows
                    .GroupBy(e => e.MaNguoiBan)
                    .Select(g => new ThongKeNguoiBanViewModel
                    {
                        MaNguoiBan = g.Key,
                        TenCuaHang = g.First().NguoiBan?.TenCuaHang ?? "Không xác định",
                        SoDonHang = g.Count(),
                        TongDoanhThu = g.Sum(e => e.TongTien),
                        TongPhiNenTang = g.Sum(e => e.PhiNenTang),
                        TongTienNhan = g.Sum(e => e.TienChuyenChoNguoiBan)
                    })
                    .OrderByDescending(t => t.TongDoanhThu)
                    .ToList();

                ViewBag.TuNgay = tuNgay.Value;
                ViewBag.DenNgay = denNgay.Value;

                return View(thongKeTheoNguoiBan);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi thống kê theo người bán: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tạo báo cáo. Vui lòng thử lại sau!";
                return RedirectToAction("QuanLyKyQuy");
            }
        }


        //27/4/2025
        // GET: Admin/QuanLyBaoCaoDonHang
        [Authorize]
        // GET: Admin/QuanLyBaoCaoDonHang
        [Authorize]
        public ActionResult QuanLyBaoCaoDonHang(int page = 1, string trangThai = "", string tuNgay = "", string denNgay = "")
        {
            try
            {
                int pageSize = 10; // Số báo cáo hiển thị trên mỗi trang
                var query = db.BaoCaoDonHangs.AsQueryable();

                // Áp dụng bộ lọc trạng thái
                if (!string.IsNullOrEmpty(trangThai))
                {
                    query = query.Where(r => r.TrangThai == trangThai);
                }

                // Áp dụng bộ lọc thời gian
                if (!string.IsNullOrEmpty(tuNgay) && DateTime.TryParse(tuNgay, out DateTime tuNgayDate))
                {
                    query = query.Where(r => r.NgayTao >= tuNgayDate);
                }

                if (!string.IsNullOrEmpty(denNgay) && DateTime.TryParse(denNgay, out DateTime denNgayDate))
                {
                    // Đảm bảo đến hết ngày
                    denNgayDate = denNgayDate.AddDays(1).AddSeconds(-1);
                    query = query.Where(r => r.NgayTao <= denNgayDate);
                }

                // Tính toán tổng số trang
                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // Đảm bảo trang hiện tại hợp lệ
                page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

                // Lấy dữ liệu cho trang hiện tại
                var reports = query
                    .OrderByDescending(r => r.NgayTao)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList(); // Tách thành ToList() trước khi include

                // Tải thủ công các thuộc tính navigation nếu cần
                foreach (var report in reports)
                {
                    if (report.MaDonHang > 0)
                        db.Entry(report).Reference(r => r.DonHang).Load();

                    if (report.MaNguoiDung > 0)
                        db.Entry(report).Reference(r => r.NguoiDung).Load();

                    if (report.MaNguoiBan > 0)
                        db.Entry(report).Reference(r => r.NguoiBan).Load();
                }

                // Truyền dữ liệu phân trang vào ViewBag
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalItems = totalItems;
                ViewBag.PageSize = pageSize;

                // Truyền các tham số lọc vào ViewBag để duy trì trạng thái khi phân trang
                ViewBag.TrangThai = trangThai;
                ViewBag.TuNgay = tuNgay;
                ViewBag.DenNgay = denNgay;

                return View(reports);
            }
            catch (Exception ex)
            {
                var innerException = ex.InnerException;
                string innerMessage = innerException != null ? innerException.Message : "Không có inner exception";
                System.Diagnostics.Debug.WriteLine("Lỗi quản lý báo cáo: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Inner exception: " + innerMessage);
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách báo cáo: " + ex.Message;
                return RedirectToAction("Dashboard", "Admin");
            }
        }

        // GET: Admin/ChiTietBaoCao/5
        [Authorize]
        public ActionResult ChiTietBaoCao(int id)
        {
            try
            {
                var baoCao = db.BaoCaoDonHangs
                    .Include(r => r.DonHang)
                    .Include(r => r.DonHang.NguoiDung)
                    .Include(r => r.DonHang.NguoiBan)
                    .Include(r => r.DonHang.ChiTietDonHangs)
                    .Include(r => r.DonHang.ChiTietDonHangs.Select(c => c.SanPham))
                    .FirstOrDefault(r => r.MaBaoCao == id);

                if (baoCao == null)
                {
                    return HttpNotFound();
                }

                return View(baoCao);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi xem chi tiết báo cáo: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải thông tin báo cáo. Vui lòng thử lại sau!";
                return RedirectToAction("QuanLyBaoCaoDonHang", "Admin");
            }
        }

        // POST: Admin/CapNhatTrangThaiBaoCao
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CapNhatTrangThaiBaoCao(int id, string trangThai, string ketQuaXuLy)
        {
            try
            {
                // Thêm log để theo dõi luồng xử lý
                System.Diagnostics.Debug.WriteLine($"Bắt đầu cập nhật trạng thái báo cáo ID: {id}, trạng thái: {trangThai}");

                var baoCao = db.BaoCaoDonHangs.Find(id);
                if (baoCao == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Không tìm thấy báo cáo ID: {id}");
                    return HttpNotFound();
                }

                System.Diagnostics.Debug.WriteLine($"Tìm thấy báo cáo - MaDonHang: {baoCao.MaDonHang}");

                // Lấy thông tin đơn hàng để tính phạt
                var donHang = db.DonHangs.Find(baoCao.MaDonHang);
                if (donHang == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Không tìm thấy đơn hàng ID: {baoCao.MaDonHang}");
                    TempData["Error"] = "Không tìm thấy thông tin đơn hàng!";
                    return RedirectToAction("ChiTietBaoCao", new { id = id });
                }

                System.Diagnostics.Debug.WriteLine($"Trạng thái hiện tại của đơn hàng: {donHang.TrangThaiDonHang}");

                // Lấy thông tin người bán
                var nguoiBan = db.NguoiBans.Find(donHang.MaNguoiBan);
                if (nguoiBan == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Không tìm thấy thông tin người bán ID: {donHang.MaNguoiBan}");
                    TempData["Error"] = "Không tìm thấy thông tin người bán!";
                    return RedirectToAction("ChiTietBaoCao", new { id = id });
                }

                // Lấy thông tin người mua và người bán (người dùng)
                var nguoiDungMua = db.NguoiDungs.Find(donHang.MaNguoiDung);
                var nguoiDungBan = db.NguoiDungs.Find(nguoiBan.MaNguoiDung);

                // CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG THÀNH "ĐÃ HỦY" NẾU BÁO CÁO LÀ "ĐÃ XỬ LÝ"
                // Chú ý: thực hiện việc này TRƯỚC khi cập nhật các thông tin khác
                if (trangThai == "Đã xử lý")
                {
                    System.Diagnostics.Debug.WriteLine($"Bắt đầu cập nhật trạng thái đơn hàng sang Đã hủy - MaDonHang: {donHang.MaDonHang}");

                    try
                    {
                        // Lấy đơn hàng mới từ database để tránh vấn đề theo dõi
                        var orderToUpdate = new DonHang();

                        // Lấy đơn hàng từ database bằng DbSet.Find
                        orderToUpdate = db.DonHangs.Find(donHang.MaDonHang);

                        if (orderToUpdate != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Đã tìm thấy đơn hàng #{orderToUpdate.MaDonHang}, trạng thái hiện tại: {orderToUpdate.TrangThaiDonHang}");

                            // Cập nhật trạng thái
                            orderToUpdate.TrangThaiDonHang = "Đã hủy";
                            orderToUpdate.NgayHuy = DateTime.Now;
                            orderToUpdate.LyDoHuy = "Đơn hàng bị hủy do người bán vi phạm quy định: " + ketQuaXuLy;

                            // Lưu thay đổi
                            db.Entry(orderToUpdate).State = EntityState.Modified;
                            int rowsAffected = db.SaveChanges();
                            System.Diagnostics.Debug.WriteLine($"Cập nhật trạng thái đơn hàng: {rowsAffected} dòng bị ảnh hưởng");

                            // Lấy lại đơn hàng để kiểm tra trạng thái
                            var checkOrder = db.DonHangs.Find(donHang.MaDonHang);
                            System.Diagnostics.Debug.WriteLine($"Trạng thái của đơn hàng sau khi cập nhật: {checkOrder.TrangThaiDonHang}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"CẢNH BÁO: Không tìm thấy đơn hàng {donHang.MaDonHang}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"LỖI khi cập nhật trạng thái đơn hàng: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Chi tiết lỗi: {ex.StackTrace}");
                        // Không throw exception ở đây, cho phép tiếp tục quy trình
                    }

                    // CẬP NHẬT TRẠNG THÁI GIAO DỊCH THÀNH "KHÔNG THÀNH CÔNG"
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"Bắt đầu cập nhật trạng thái giao dịch cho đơn hàng {donHang.MaDonHang}");

                        // Tìm tất cả giao dịch liên quan đến đơn hàng này
                        var giaoDichList = db.GiaoDichs.Where(g => g.MaDonHang == donHang.MaDonHang
                                                              && g.TrangThaiGiaoDich != "Không thành công").ToList();

                        if (giaoDichList.Any())
                        {
                            foreach (var giaoDich in giaoDichList)
                            {
                                System.Diagnostics.Debug.WriteLine($"Cập nhật giao dịch MaGiaoDich: {giaoDich.MaGiaoDich}");

                                giaoDich.TrangThaiGiaoDich = "Không thành công";
                                giaoDich.ThongTinGiaoDich = "Đơn hàng bị hủy do vi phạm quy định: " + ketQuaXuLy;
                                db.Entry(giaoDich).State = EntityState.Modified;
                            }

                            // Lưu thay đổi
                            int rowsAffected = db.SaveChanges();
                            System.Diagnostics.Debug.WriteLine($"Đã cập nhật {rowsAffected} giao dịch");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Không tìm thấy giao dịch nào cho đơn hàng {donHang.MaDonHang}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"LỖI khi cập nhật trạng thái giao dịch: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Chi tiết lỗi: {ex.StackTrace}");
                        // Không throw exception ở đây, cho phép tiếp tục quy trình
                    }
                    // Rest of your code for processing "Đã xử lý" reports...
                    // (Phần còn lại của mã xử lý phạt, gửi email, v.v.)

                    // Kiểm tra phương thức thanh toán
                    bool isVNPAY = donHang.PhuongThucThanhToan == "VNPAY";

                    // Tính số tiền phạt dựa trên phương thức thanh toán
                    decimal tienHoanTraNguoiMua = isVNPAY ? donHang.TongSoTien : 0; // 100% nếu VNPAY, 0 nếu COD
                    decimal phiChoNenTang = donHang.TongSoTien * 0.5m; // 50% giá trị đơn hàng
                    decimal tongTienPhat = tienHoanTraNguoiMua + phiChoNenTang; // Tổng tiền phạt

                    // Kiểm tra số dư của người bán
                    if (nguoiBan.SoDuVi < tongTienPhat)
                    {
                        TempData["Error"] = "Số dư của người bán không đủ để thực hiện phạt. Vui lòng kiểm tra lại!";
                        return RedirectToAction("ChiTietBaoCao", new { id = id });
                    }

                    // Cập nhật số dư của người bán
                    nguoiBan.SoDuVi -= tongTienPhat;
                    db.Entry(nguoiBan).State = EntityState.Modified;

                    // Tạo ghi chép ví hoàn tiền cho người mua (nếu là VNPAY)
                    GhiChepVi ghiChepViHoanTien = null;
                    if (isVNPAY)
                    {
                        ghiChepViHoanTien = new GhiChepVi
                        {
                            MaNguoiBan = nguoiBan.MaNguoiBan,
                            SoTien = -tienHoanTraNguoiMua,
                            LoaiGiaoDich = "Hoàn tiền người mua",
                            MoTa = $"Hoàn tiền 100% giá trị đơn hàng #{donHang.MaDonHang} cho người mua do vi phạm quy định",
                            NgayGiaoDich = DateTime.Now,
                            TrangThai = "Thành công",
                            MaDonHang = donHang.MaDonHang
                        };
                        db.GhiChepVis.Add(ghiChepViHoanTien);
                    }

                    // Tạo ghi chép ví phí nền tảng
                    var ghiChepViPhiNenTang = new GhiChepVi
                    {
                        MaNguoiBan = nguoiBan.MaNguoiBan,
                        SoTien = -phiChoNenTang,
                        LoaiGiaoDich = "Phí nền tảng",
                        MoTa = $"Phí phạt 50% giá trị đơn hàng #{donHang.MaDonHang} cho nền tảng do vi phạm quy định",
                        NgayGiaoDich = DateTime.Now,
                        TrangThai = "Thành công",
                        MaDonHang = donHang.MaDonHang
                    };
                    db.GhiChepVis.Add(ghiChepViPhiNenTang);

                    // Lưu các bản ghi để lấy MaGhiChep
                    db.SaveChanges();

                    // Tạo thông báo cho người bán
                    if (isVNPAY && ghiChepViHoanTien != null)
                    {
                        var thongBaoHoanTienNguoiBan = new ThongBao
                        {
                            MaNguoiDung = nguoiBan.MaNguoiDung,
                            LoaiThongBao = "Hoàn tiền",
                            TieuDe = "Thông báo xử phạt vi phạm - Hoàn tiền người mua",
                            TinNhan = $"Số tiền {tienHoanTraNguoiMua.ToString("N0")} VND đã được trừ từ ví của bạn để hoàn trả cho người mua theo quy định xử lý vi phạm trên đơn hàng #{donHang.MaDonHang}.",
                            MucDoQuanTrong = 3,
                            DuongDanChiTiet = "/GiaoDich/ChiTietGhiChepViNguoiBan/" + ghiChepViHoanTien.MaGhiChep,
                            NgayTao = DateTime.Now
                        };
                        db.ThongBaos.Add(thongBaoHoanTienNguoiBan);
                    }

                    var thongBaoPhiNenTangNguoiBan = new ThongBao
                    {
                        MaNguoiDung = nguoiBan.MaNguoiDung,
                        LoaiThongBao = "Phí nền tảng",
                        TieuDe = "Thông báo xử phạt vi phạm - Phí nền tảng",
                        TinNhan = $"Số tiền {phiChoNenTang.ToString("N0")} VND đã được trừ từ ví của bạn làm phí xử lý vi phạm trên đơn hàng #{donHang.MaDonHang}.\nLý do: {ketQuaXuLy}",
                        MucDoQuanTrong = 3,
                        DuongDanChiTiet = "/GiaoDich/ChiTietGhiChepViNguoiBan/" + ghiChepViPhiNenTang.MaGhiChep,
                        NgayTao = DateTime.Now
                    };
                    db.ThongBaos.Add(thongBaoPhiNenTangNguoiBan);

                    // Thông báo hoàn tiền cho người mua (chỉ với VNPAY)
                    if (isVNPAY && nguoiDungMua != null)
                    {
                        var thongBaoHoanTien = new ThongBao
                        {
                            MaNguoiDung = donHang.MaNguoiDung,
                            LoaiThongBao = "HoanTien",
                            TieuDe = "Thông báo hoàn tiền đơn hàng",
                            TinNhan = $"Chúng tôi đã xử lý hoàn tiền {tienHoanTraNguoiMua.ToString("N0")} VND cho đơn hàng #{donHang.MaDonHang} do người bán vi phạm quy định.",
                            MucDoQuanTrong = 2,
                            DuongDanChiTiet = "/DonHang/ChiTiet/" + baoCao.MaDonHang,
                            NgayTao = DateTime.Now
                        };
                        db.ThongBaos.Add(thongBaoHoanTien);
                    }

                    // Xử lý Escrow cho cả COD và VNPAY
                    var escrow = db.Escrows.FirstOrDefault(e => e.MaDonHang == donHang.MaDonHang);
                    if (escrow != null && escrow.TrangThai == "Đang giữ")
                    {
                        // Cập nhật trạng thái Escrow thành "Đã hoàn tiền" cho cả COD và VNPAY
                        escrow.TrangThai = "Đã hoàn tiền";
                        escrow.NgayGiaiNgan = DateTime.Now;
                        db.Entry(escrow).State = EntityState.Modified;

                        // Xử lý hoàn tiền đặt cọc cho người bán nếu là COD
                        if (!isVNPAY)
                        {
                            // Tìm ghi chép ví đặt cọc ban đầu để lấy số tiền
                            var ghiChepDatCoc = db.GhiChepVis
                                .FirstOrDefault(g => g.MaDonHang == donHang.MaDonHang
                                    && g.LoaiGiaoDich == "Đặt cọc"
                                    && g.TrangThai == "Thành công");

                            if (ghiChepDatCoc != null)
                            {
                                // Lấy số tiền đặt cọc từ ghi chép ví (giá trị tuyệt đối vì có thể lưu âm)
                                var tienDatCoc = Math.Abs(ghiChepDatCoc.SoTien);

                                // Hoàn trả tiền đặt cọc cho người bán
                                nguoiBan.SoDuVi += tienDatCoc;
                                db.Entry(nguoiBan).State = EntityState.Modified;

                                // Tạo ghi chép ví hoàn trả tiền đặt cọc
                                var ghiChepViHoanDatCoc = new GhiChepVi
                                {
                                    MaNguoiBan = nguoiBan.MaNguoiBan,
                                    SoTien = tienDatCoc,
                                    LoaiGiaoDich = "Hoàn tiền đặt cọc",
                                    MoTa = $"Hoàn tiền đặt cọc cho đơn hàng #{donHang.MaDonHang} do báo cáo vi phạm được xử lý",
                                    NgayGiaoDich = DateTime.Now,
                                    TrangThai = "Thành công",
                                    MaDonHang = donHang.MaDonHang
                                };
                                db.GhiChepVis.Add(ghiChepViHoanDatCoc);

                                // Lưu để lấy MaGhiChep
                                db.SaveChanges();

                                // Tạo thông báo hoàn tiền đặt cọc cho người bán
                                var thongBaoHoanDatCoc = new ThongBao
                                {
                                    MaNguoiDung = nguoiBan.MaNguoiDung,
                                    LoaiThongBao = "Hoàn tiền đặt cọc",
                                    TieuDe = "Hoàn tiền đặt cọc do đơn hàng bị hủy",
                                    TinNhan = $"Số tiền đặt cọc {tienDatCoc.ToString("N0")} VND của đơn hàng #{donHang.MaDonHang} đã được hoàn trả do đơn hàng bị hủy bởi vi phạm quy định.",
                                    MucDoQuanTrong = 2,
                                    DuongDanChiTiet = "/GiaoDich/ChiTietGhiChepViNguoiBan/" + ghiChepViHoanDatCoc.MaGhiChep,
                                    NgayTao = DateTime.Now
                                };
                                db.ThongBaos.Add(thongBaoHoanDatCoc);
                            }
                        }
                    }

                    // Gửi email thông báo cho người bán
                    if (nguoiDungBan != null && !string.IsNullOrEmpty(nguoiDungBan.Email))
                    {
                        try
                        {
                            string emailContent;
                            if (isVNPAY)
                            {
                                emailContent = $@"
                                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                        <h2 style='color: #d9534f;'>Thông báo xử phạt vi phạm</h2>
                                        <p>Kính gửi {nguoiDungBan.TenNguoiDung},</p>
                                        <p>Chúng tôi xin thông báo về quyết định xử phạt đối với đơn hàng <strong>#{donHang.MaDonHang}</strong> do vi phạm quy định của nền tảng.</p>
                    
                                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                            <h3 style='margin-top: 0;'>Chi tiết xử phạt:</h3>
                                            <ul style='list-style-type: none; padding-left: 0;'>
                                                <li>✓ Tổng số tiền phạt: <strong>{tongTienPhat.ToString("N0")} VND</strong></li>
                                                <li>✓ Hoàn trả người mua: <strong>{tienHoanTraNguoiMua.ToString("N0")} VND</strong></li>
                                                <li>✓ Phí xử phạt nền tảng: <strong>{phiChoNenTang.ToString("N0")} VND</strong></li>
                                            </ul>
                                        </div>
                    
                                        <p><strong>Lý do vi phạm:</strong> {ketQuaXuLy}</p>
                                        <p>Số tiền phạt đã được trừ trực tiếp từ ví của bạn.</p>
                    
                                        <p>Chúng tôi khuyến nghị bạn tuân thủ các quy định và chính sách của nền tảng để tránh các trường hợp tương tự trong tương lai.</p>
                    
                                        <p style='margin-top: 30px;'>Trân trọng,<br/><strong>Ban Quản Trị</strong></p>
                                    </div>";
                            }
                            else // COD
                            {
                                emailContent = $@"
                                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                        <h2 style='color: #d9534f;'>Thông báo xử phạt vi phạm</h2>
                                        <p>Kính gửi {nguoiDungBan.TenNguoiDung},</p>
                                        <p>Chúng tôi xin thông báo về quyết định xử phạt đối với đơn hàng <strong>#{donHang.MaDonHang}</strong> do vi phạm quy định của nền tảng.</p>
                    
                                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                            <h3 style='margin-top: 0;'>Chi tiết xử phạt:</h3>
                                            <ul style='list-style-type: none; padding-left: 0;'>
                                                <li>✓ Phí xử phạt nền tảng: <strong>{phiChoNenTang.ToString("N0")} VND</strong></li>
                                            </ul>
                                            <p style='margin-bottom: 0;'><em>Lưu ý: Do đây là đơn hàng COD và người mua chưa thanh toán, không có khoản hoàn tiền nào được thực hiện.</em></p>
                                        </div>
                    
                                        <p><strong>Lý do vi phạm:</strong> {ketQuaXuLy}</p>
                                        <p>Số tiền phạt đã được trừ trực tiếp từ ví của bạn.</p>
                    
                                        <p>Chúng tôi khuyến nghị bạn tuân thủ các quy định và chính sách của nền tảng để tránh các trường hợp tương tự trong tương lai.</p>
                    
                                        <p style='margin-top: 30px;'>Trân trọng,<br/><strong>Ban Quản Trị</strong></p>
                                    </div>";
                            }

                            await SendEmailAsync(nguoiDungBan.Email, "Thông báo xử phạt vi phạm", emailContent);
                        }
                        catch (Exception emailEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Lỗi khi gửi email cho người bán: {emailEx.Message}");
                        }
                    }

                    // Gửi email thông báo cho người mua
                    if (nguoiDungMua != null && !string.IsNullOrEmpty(nguoiDungMua.Email))
                    {
                        try
                        {
                            string emailContent;
                            if (isVNPAY)
                            {
                                // Email cho VNPAY - có hoàn tiền
                                emailContent = $@"
                                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                        <h2 style='color: #5cb85c;'>Thông báo xử lý báo cáo thành công</h2>
                                        <p>Kính gửi {nguoiDungMua.TenNguoiDung},</p>
                                        <p>Chúng tôi xin thông báo rằng báo cáo của bạn về đơn hàng <strong>#{donHang.MaDonHang}</strong> đã được xử lý.</p>
                    
                                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                            <h3 style='margin-top: 0;'>Kết quả xử lý:</h3>
                                            <p>✓ Trạng thái: <strong style='color: #5cb85c;'>{trangThai}</strong></p>
                                            <p>✓ Chi tiết: {ketQuaXuLy}</p>
                                            <p>✓ Số tiền hoàn trả: <strong>{tienHoanTraNguoiMua.ToString("N0")} VND</strong></p>
                                        </div>
                    
                                        <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; border: 1px solid #ffeeba;'>
                                            <h3 style='margin-top: 0; color: #856404;'>Thông tin nhận hoàn tiền:</h3>
                                            <p>Để nhận khoản hoàn trả, vui lòng cung cấp thông tin tài khoản ngân hàng:</p>
                                            <ul>
                                                <li>Tên ngân hàng</li>
                                                <li>Số tài khoản</li>
                                                <li>Tên chủ tài khoản</li>
                                            </ul>
                                            <p><strong>Vui lòng phản hồi email này với thông tin trên.</strong></p>
                                            <p>Thời gian xử lý: 3-5 ngày làm việc sau khi nhận được thông tin.</p>
                                        </div>
                    
                                        <p>Cảm ơn bạn đã đóng góp vào việc xây dựng cộng đồng an toàn và uy tín.</p>
                    
                                        <p style='margin-top: 30px;'>Trân trọng,<br/><strong>Ban Quản Trị</strong></p>
                                    </div>";
                            }
                            else // COD
                            {
                                // Email cho COD - không có hoàn tiền
                                emailContent = $@"
                                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                                        <h2 style='color: #5cb85c;'>Thông báo xử lý báo cáo thành công</h2>
                                        <p>Kính gửi {nguoiDungMua.TenNguoiDung},</p>
                                        <p>Chúng tôi xin thông báo rằng báo cáo của bạn về đơn hàng <strong>#{donHang.MaDonHang}</strong> đã được xử lý.</p>
                    
                                        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                            <h3 style='margin-top: 0;'>Kết quả xử lý:</h3>
                                            <p>✓ Trạng thái: <strong style='color: #5cb85c;'>{trangThai}</strong></p>
                                            <p>✓ Chi tiết: {ketQuaXuLy}</p>
                                        </div>
                    
                                        <div style='background-color: #d1ecf1; padding: 15px; border-radius: 5px; border: 1px solid #bee5eb;'>
                                            <p style='margin: 0;'><strong>Lưu ý:</strong> Do đây là đơn hàng thanh toán khi nhận hàng (COD) và bạn chưa thanh toán, không có khoản hoàn tiền nào được thực hiện. Người bán đã bị xử phạt theo quy định.</p>
                                        </div>
                    
                                        <p>Cảm ơn bạn đã đóng góp vào việc xây dựng cộng đồng an toàn và uy tín.</p>
                    
                                        <p style='margin-top: 30px;'>Trân trọng,<br/><strong>Ban Quản Trị</strong></p>
                                    </div>";
                            }

                            await SendEmailAsync(
                                nguoiDungMua.Email,
                                isVNPAY ? "Thông báo xử lý báo cáo và hướng dẫn hoàn tiền" : "Thông báo xử lý báo cáo đơn hàng",
                                emailContent
                            );
                        }
                        catch (Exception emailEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"Lỗi khi gửi email cho người mua: {emailEx.Message}");
                        }
                    }
                }
                else if (trangThai == "Đã huỷ")
                {
                    // Xử lý logic khi báo cáo bị hủy
                    // ...
                }

                // Cập nhật trạng thái báo cáo
                baoCao.TrangThai = trangThai;
                baoCao.KetQuaXuLy = ketQuaXuLy;
                baoCao.NgayXuLy = DateTime.Now;
                db.Entry(baoCao).State = EntityState.Modified;

                // Tạo thông báo cho người mua
                var thongBaoNguoiMua = new ThongBao
                {
                    MaNguoiDung = donHang.MaNguoiDung,
                    LoaiThongBao = "BaoCao",
                    TieuDe = "Báo cáo đơn hàng đã được xử lý",
                    TinNhan = $"Báo cáo cho đơn hàng #{baoCao.MaDonHang} đã được xử lý. Kết quả: {trangThai}. {ketQuaXuLy}",
                    MucDoQuanTrong = 2, // Quan trọng cao
                    DuongDanChiTiet = "/DonHang/ChiTiet/" + baoCao.MaDonHang,
                    NgayTao = DateTime.Now
                };
                db.ThongBaos.Add(thongBaoNguoiMua);

                // Lưu tất cả các thay đổi
                try
                {
                    System.Diagnostics.Debug.WriteLine("Bắt đầu lưu các thay đổi vào cơ sở dữ liệu");
                    int changes = await db.SaveChangesAsync();
                    System.Diagnostics.Debug.WriteLine($"Đã lưu thành công: {changes} thay đổi");

                    // Kiểm tra lại trạng thái đơn hàng sau khi lưu
                    var updatedOrder = db.DonHangs.Find(donHang.MaDonHang);
                    System.Diagnostics.Debug.WriteLine($"Trạng thái đơn hàng sau khi lưu: {updatedOrder.TrangThaiDonHang}");
                }
                catch (Exception saveEx)
                {
                    System.Diagnostics.Debug.WriteLine($"LỖI khi lưu các thay đổi: {saveEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"Chi tiết lỗi: {saveEx.StackTrace}");
                    throw; // Ném lại ngoại lệ để xử lý ở bên ngoài
                }

                TempData["Success"] = "Cập nhật trạng thái báo cáo thành công!";
                return RedirectToAction("ChiTietBaoCao", new { id = id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi cập nhật trạng thái báo cáo: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Chi tiết lỗi: " + ex.StackTrace);
                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật trạng thái báo cáo: " + ex.Message;
                return RedirectToAction("ChiTietBaoCao", new { id = id });
            }
        }

        // Phương thức gửi email
        private async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            try
            {
                // Đọc cấu hình SMTP từ AppSettings
                string smtpHost = ConfigurationManager.AppSettings["SmtpHost"];
                int smtpPort = Convert.ToInt32(ConfigurationManager.AppSettings["SmtpPort"]);
                string smtpEmail = ConfigurationManager.AppSettings["SmtpEmail"];
                string smtpPassword = ConfigurationManager.AppSettings["SmtpPassword"];
                bool enableSsl = Convert.ToBoolean(ConfigurationManager.AppSettings["SmtpEnableSSL"]);
                string fromName = ConfigurationManager.AppSettings["EmailFromName"];

                var message = new MailMessage();
                message.To.Add(new MailAddress(email));
                message.From = new MailAddress(smtpEmail, fromName);
                message.Subject = subject;
                message.Body = htmlMessage;
                message.IsBodyHtml = true;

                using (var client = new SmtpClient())
                {
                    client.Host = smtpHost;
                    client.Port = smtpPort;
                    client.EnableSsl = enableSsl;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(smtpEmail, smtpPassword);

                    await client.SendMailAsync(message);
                    System.Diagnostics.Debug.WriteLine("Email đã được gửi thành công đến " + email);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi gửi email: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Chi tiết lỗi: " + ex.ToString());
                // Không ném lại ngoại lệ để không gián đoạn luồng chính
            }
        }
        //27/4/2025

        // GET: Admin/Dashboard
        [Authorize]
        public ActionResult Dashboard()
        {
            try
            {
                var viewModel = new DashboardViewModel();

                // Statistics for Users
                viewModel.TotalUsers = db.NguoiDungs.Count();
                viewModel.ActiveUsers = db.NguoiDungs.Count(u => u.TrangThai == "Active");
                viewModel.InactiveUsers = db.NguoiDungs.Count(u => u.TrangThai == "Inactive" || u.TrangThai == "Banned");
                viewModel.PendingUpgradeUsers = db.NguoiDungs.Count(u => u.XetDuyetThanhNguoiBan == true);
                viewModel.Buyers = db.NguoiDungs.Count(u => u.VaiTro == "Buyer");
                viewModel.Sellers = db.NguoiDungs.Count(u => u.VaiTro == "Seller");

                // Statistics for Products
                viewModel.TotalProducts = db.SanPhams.Count();
                viewModel.ApprovedProducts = db.SanPhams.Count(p => p.TrangThai == "Đã phê duyệt");
                viewModel.PendingProducts = db.SanPhams.Count(p => p.TrangThai == "Đang chờ xử lý");
                viewModel.RejectedProducts = db.SanPhams.Count(p => p.TrangThai == "Bị từ chối");

                // Statistics for Orders
                viewModel.TotalOrders = db.DonHangs.Count();
                viewModel.NewOrders = db.DonHangs.Count(o => o.TrangThaiDonHang == "Đang chờ xử lý" || o.TrangThaiDonHang == "Chờ thanh toán" || o.TrangThaiDonHang == "Đã thanh toán"|| o.TrangThaiDonHang == "Đã vận chuyển"|| o.TrangThaiDonHang == "Đã xác nhận nhận hàng");
                viewModel.DeliveredOrders = db.DonHangs.Count(o => o.TrangThaiDonHang == "Đã giao" || o.TrangThaiDonHang == "Đã hoàn thành");
                viewModel.CancelledOrders = db.DonHangs.Count(o => o.TrangThaiDonHang == "Đã hủy");

                // Statistics for Orders by Date (for chart)
                DateTime today = DateTime.Now.Date;
                DateTime startDate = today.AddDays(-6); // Last 7 days

                // Calculate the end date explicitly to avoid using AddDays in LINQ
                DateTime endDatePlusOne = today.AddDays(1);

                // Lấy tất cả đơn hàng trong khoảng thời gian
                var allOrdersInRange = db.DonHangs
                    .Where(o => o.NgayTao >= startDate && o.NgayTao <= endDatePlusOne)
                    .ToList();

                // Nhóm đơn hàng theo ngày và tính toán số lượng đơn hàng
                var orderCountByDate = allOrdersInRange
                    .GroupBy(o => o.NgayTao.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                // Tính doanh thu chỉ từ đơn hàng hợp lệ (đã thanh toán, đã giao hoặc đã hoàn thành)
                var revenueByDate = allOrdersInRange
                    .Where(o => 
                                o.TrangThaiDonHang == "Đã giao" ||
                                o.TrangThaiDonHang == "Đã hoàn thành")
                    .GroupBy(o => o.NgayTao.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Revenue = g.Sum(o => o.TongSoTien)
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                // Generate labels for the last 7 days
                var last7Days = Enumerable.Range(0, 7)
                    .Select(i => today.AddDays(-i))
                    .OrderBy(d => d)
                    .ToList();

                viewModel.OrderChartData = new List<ChartDataViewModel>();

                foreach (var day in last7Days)
                {
                    var countData = orderCountByDate.FirstOrDefault(o => o.Date.Date == day.Date);
                    var revenueData = revenueByDate.FirstOrDefault(o => o.Date.Date == day.Date);

                    viewModel.OrderChartData.Add(new ChartDataViewModel
                    {
                        Date = day.ToString("dd/MM"),
                        Count = countData?.Count ?? 0,
                        Revenue = revenueData?.Revenue ?? 0
                    });
                }
                // Doanh thu nên là tổng giá trị đơn hàng đã hoàn thành
                viewModel.TotalRevenue = db.DonHangs
                    .Where(o => o.TrangThaiDonHang == "Đã hoàn thành" || o.TrangThaiDonHang == "Đã giao")
                    .Sum(o => (decimal?)o.TongSoTien) ?? 0;

                // Trong phương thức Dashboard, thêm bộ lọc thời gian cho TotalCommission
                DateTime todays = DateTime.Now.Date;
                DateTime startDates = todays.AddDays(-6); // Last 7 days
                DateTime endDatePlusOnes = todays.AddDays(1);

                // Lấy tất cả các bản ghi Escrow đã giải ngân
                var allEscrows = db.Escrows
                    .Where(e => e.TrangThai == "Đã giải ngân")
                    .ToList();

                //// Sau đó thực hiện lọc và tính tổng trong bộ nhớ
                //viewModel.TotalCommission = allEscrows
                //    .Where(e => e.NgayGiaiNgan >= startDates.Date && e.NgayGiaiNgan <= endDatePlusOnes.Date)
                //    .Sum(e => e.PhiNenTang);
                // Tính tổng phí nền tảng từ Escrows đã giải ngân trong khoảng thời gian
                var escrowCommission = allEscrows
                    .Where(e => e.NgayGiaiNgan >= startDates.Date && e.NgayGiaiNgan <= endDatePlusOnes.Date)
                    .Sum(e => e.PhiNenTang);

                // Lấy phí phạt từ GhiChepVi (các giao dịch phí nền tảng với mô tả bắt đầu bằng "Phí phạt 50%")
                var penaltyCommission = db.GhiChepVis
                    .Where(g => g.LoaiGiaoDich == "Phí nền tảng"
                        && g.TrangThai == "Thành công"
                        && g.MoTa.StartsWith("Phí phạt 50%")
                        && g.NgayGiaoDich >= startDates.Date
                        && g.NgayGiaoDich <= endDatePlusOnes.Date)
                    .ToList()
                    .Sum(g => Math.Abs(g.SoTien));

                // Tổng cộng cả hai loại phí
                viewModel.TotalCommission = escrowCommission + penaltyCommission;
                // Recent Orders
                var recentOrders = db.DonHangs
                    .Include(o => o.NguoiDung)
                    .Include(o => o.NguoiBan)
                    .OrderByDescending(o => o.NgayTao)
                    .Take(10)
                    .ToList();

                viewModel.RecentOrders = recentOrders.Select(o => new OrderViewModel
                {
                    OrderId = o.MaDonHang,
                    CustomerName = o.NguoiDung?.TenNguoiDung ?? "Không xác định",
                    TotalAmount = o.TongSoTien,
                    OrderStatus = o.TrangThaiDonHang,
                    OrderDate = o.NgayTao
                }).ToList();

                // Pending Approvals
                var pendingProducts = db.SanPhams
                    .Include(p => p.NguoiBan)
                    .Where(p => p.TrangThai == "Đang chờ xử lý")
                    .OrderByDescending(p => p.NgayTao)
                    .Take(3)
                    .ToList();

                viewModel.PendingProductApprovals = pendingProducts.Select(p => new ProductViewModel
                {
                    ProductId = p.MaSanPham,
                    ProductName = p.TenSanPham,
                    SellerName = p.NguoiBan?.TenCuaHang ?? "Không xác định",
                    SellerId = p.MaNguoiBan
                }).ToList();

                var pendingUpgrades = db.NguoiDungs
                    .Where(u => u.XetDuyetThanhNguoiBan == true)
                    .OrderByDescending(u => u.NgayTao)
                    .Take(3)
                    .ToList();

                viewModel.PendingUpgrades = pendingUpgrades.Select(u => new UserViewModel
                {
                    UserId = u.MaNguoiDung,
                    UserName = u.TenNguoiDung,
                    Email = u.Email
                }).ToList();

                // Top Sellers
                var topSellersData = db.DonHangs
                    .Where(o => o.TrangThaiDonHang == "Đã hoàn thành" || o.TrangThaiDonHang == "Đã xác nhận nhận hàng")
                    .GroupBy(o => o.MaNguoiBan)
                    .Select(g => new
                    {
                        SellerId = g.Key,
                        OrderCount = g.Count(),
                        Revenue = g.Sum(o => o.TongSoTien)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(5)
                    .ToList();

                viewModel.TopSellers = new List<SellerStatsViewModel>();

                foreach (var item in topSellersData)
                {
                    var seller = db.NguoiBans.FirstOrDefault(n => n.MaNguoiBan == item.SellerId);

                    viewModel.TopSellers.Add(new SellerStatsViewModel
                    {
                        SellerId = item.SellerId,
                        SellerName = seller?.TenCuaHang ?? "Không xác định",
                        OrderCount = item.OrderCount,
                        Revenue = item.Revenue
                    });
                }

                // Recent Transactions
                var recentTransactions = db.GiaoDichs
                    .OrderByDescending(g => g.NgayGiaoDich)
                    .Take(5)
                    .ToList();

                viewModel.RecentTransactions = recentTransactions.Select(t => new TransactionViewModel
                {
                    TransactionId = t.MaGiaoDich,
                    TransactionStatus = t.TrangThaiGiaoDich,
                    Amount = t.TongTien
                }).ToList();

                // Lấy hoạt động gần đây
                viewModel.RecentActivities = new List<ActivityViewModel>();

                // 1. Thêm người dùng mới
                var recentUsers = db.NguoiDungs
                    .OrderByDescending(u => u.NgayTao)
                    .Take(3)
                    .ToList();

                foreach (var user in recentUsers)
                {
                    viewModel.RecentActivities.Add(new ActivityViewModel
                    {
                        Id = user.MaNguoiDung,
                        ActivityType = "NewUser",
                        Message = $"Người dùng mới đăng ký: {user.TenNguoiDung}",
                        ActivityTime = user.NgayTao,
                        IconClass = "fas fa-user", // Dùng Font Awesome thay vì bi-person
                        ColorClass = "bg-primary"
                    });
                }

                // 2. Thêm đơn hàng mới
                var recentOrder = db.DonHangs
                    .OrderByDescending(o => o.NgayTao)
                    .Take(3)
                    .ToList();

                foreach (var order in recentOrders)
                {
                    var customerName = db.NguoiDungs
                        .Where(u => u.MaNguoiDung == order.MaNguoiDung)
                        .Select(u => u.TenNguoiDung)
                        .FirstOrDefault() ?? "Không xác định";

                    viewModel.RecentActivities.Add(new ActivityViewModel
                    {
                        Id = order.MaDonHang,
                        ActivityType = "NewOrder",
                        Message = $"Đơn hàng mới #{order.MaDonHang} từ {customerName}",
                        ActivityTime = order.NgayTao,
                        IconClass = "fas fa-shopping-cart", // Dùng Font Awesome thay vì bi-cart
                        ColorClass = "bg-success"
                    });
                }

                // 3. Thêm sản phẩm mới
                var recentProducts = db.SanPhams
                    .OrderByDescending(p => p.NgayTao)
                    .Take(3)
                    .ToList();

                foreach (var product in recentProducts)
                {
                    var sellerName = db.NguoiBans
                        .Where(s => s.MaNguoiBan == product.MaNguoiBan)
                        .Select(s => s.TenCuaHang)
                        .FirstOrDefault() ?? "Không xác định";

                    viewModel.RecentActivities.Add(new ActivityViewModel
                    {
                        Id = product.MaSanPham,
                        ActivityType = "NewProduct",
                        Message = $"Sản phẩm mới: {product.TenSanPham} từ {sellerName}",
                        ActivityTime = product.NgayTao,
                        IconClass = "fas fa-box", // Dùng Font Awesome thay vì bi-box
                        ColorClass = "bg-warning"
                    });
                }

                // Sắp xếp tất cả hoạt động theo thời gian giảm dần và lấy 5 hoạt động gần nhất
                viewModel.RecentActivities = viewModel.RecentActivities
                    .OrderByDescending(a => a.ActivityTime)
                    .Take(3)
                    .ToList();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi dashboard: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải dữ liệu dashboard. Vui lòng thử lại sau!";
                return RedirectToAction("Index", "Home");
            }
        }

        [Authorize]
        [HttpGet]
        public JsonResult GetDashboardData(string timeRange)
        {
            try
            {
                DateTime endDate = DateTime.Now.Date;
                DateTime startDate;

                switch (timeRange)
                {
                    case "today":
                        startDate = endDate;
                        break;
                    case "week":
                        startDate = endDate.AddDays(-6);
                        break;
                    case "month":
                        startDate = endDate.AddDays(-29);
                        break;
                    case "all":
                        // Lấy ngày đầu tiên từ cơ sở dữ liệu hoặc một ngày cố định trong quá khứ
                        var firstOrderDate = db.DonHangs.OrderBy(o => o.NgayTao).Select(o => o.NgayTao).FirstOrDefault();

                        // Nếu không có đơn hàng nào, mặc định lấy 1 năm
                        startDate = firstOrderDate != default(DateTime) ? firstOrderDate.Date : endDate.AddYears(-1);
                        break;
                    default:
                        startDate = endDate.AddDays(-6);
                        break;
                }

                // Tính toán endDatePlusOne một lần
                DateTime endDatePlusOne = endDate.AddDays(1);

                // Statistics for Users (không thay đổi theo khoảng thời gian)
                int totalUsers = db.NguoiDungs.Count();
                int activeUsers = db.NguoiDungs.Count(u => u.TrangThai == "Active");
                int inactiveUsers = db.NguoiDungs.Count(u => u.TrangThai == "Inactive" || u.TrangThai == "Banned");
                int pendingUpgradeUsers = db.NguoiDungs.Count(u => u.XetDuyetThanhNguoiBan == true);
                int buyers = db.NguoiDungs.Count(u => u.VaiTro == "Buyer");
                int sellers = db.NguoiDungs.Count(u => u.VaiTro == "Seller");

                // Statistics for Products (không thay đổi theo khoảng thời gian)
                int totalProducts = db.SanPhams.Count();
                int approvedProducts = db.SanPhams.Count(p => p.TrangThai == "Đã phê duyệt");
                int pendingProducts = db.SanPhams.Count(p => p.TrangThai == "Đang chờ xử lý");
                int rejectedProducts = db.SanPhams.Count(p => p.TrangThai == "Bị từ chối");

                // Lấy dữ liệu từ startDate đến endDate
                var allOrdersInRange = db.DonHangs
                    .Where(o => o.NgayTao >= startDate && o.NgayTao <= endDatePlusOne)
                    .ToList();

                // Tính toán số lượng đơn hàng theo ngày
                var orderCountByDate = allOrdersInRange
                    .GroupBy(o => o.NgayTao.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                // Tính toán doanh thu theo ngày - chỉ với đơn hàng hợp lệ
                var revenueByDate = allOrdersInRange
                    .Where(o => 
                                o.TrangThaiDonHang == "Đã giao" ||
                                o.TrangThaiDonHang == "Đã hoàn thành")
                    .GroupBy(o => o.NgayTao.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Revenue = g.Sum(o => o.TongSoTien)
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                // Tạo danh sách các ngày cần hiển thị
                List<DateTime> dateRange = new List<DateTime>();

                // Với trường hợp "all", có thể có nhiều ngày nên chúng ta cần phân nhóm theo tháng/tuần
                if (timeRange == "all" && (endDate - startDate).TotalDays > 90)
                {
                    // Nếu khoảng thời gian dài hơn 90 ngày, nhóm theo tháng
                    var currentDate = new DateTime(startDate.Year, startDate.Month, 1);
                    while (currentDate <= endDate)
                    {
                        dateRange.Add(currentDate);
                        currentDate = currentDate.AddMonths(1);
                    }

                    // Nhóm dữ liệu theo tháng
                    var orderCountByMonth = allOrdersInRange
                        .GroupBy(o => new { o.NgayTao.Year, o.NgayTao.Month })
                        .Select(g => new
                        {
                            Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                            Count = g.Count()
                        })
                        .OrderBy(x => x.Date)
                        .ToList();

                    var revenueByMonth = allOrdersInRange
                        .Where(o =>
                                    o.TrangThaiDonHang == "Đã giao" ||
                                    o.TrangThaiDonHang == "Đã hoàn thành")
                        .GroupBy(o => new { o.NgayTao.Year, o.NgayTao.Month })
                        .Select(g => new
                        {
                            Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                            Revenue = g.Sum(o => o.TongSoTien)
                        })
                        .OrderBy(x => x.Date)
                        .ToList();

                    orderCountByDate = orderCountByMonth;
                    revenueByDate = revenueByMonth;
                }
                else
                {
                    // Thêm từng ngày vào khoảng thời gian
                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        dateRange.Add(date);
                    }
                }

                // Chuẩn bị dữ liệu cho biểu đồ
                var chartLabels = new List<string>();
                var orderData = new List<int>();
                var revenueData = new List<decimal>();

                foreach (var date in dateRange)
                {
                    var dateFormatString = timeRange == "all" && (endDate - startDate).TotalDays > 90
                        ? "MM/yyyy"
                        : "dd/MM";

                    var countData = orderCountByDate.FirstOrDefault(o =>
                        timeRange == "all" && (endDate - startDate).TotalDays > 90
                            ? o.Date.Month == date.Month && o.Date.Year == date.Year
                            : o.Date.Date == date.Date);

                    var revenueItem = revenueByDate.FirstOrDefault(o =>
                        timeRange == "all" && (endDate - startDate).TotalDays > 90
                            ? o.Date.Month == date.Month && o.Date.Year == date.Year
                            : o.Date.Date == date.Date);

                    chartLabels.Add(date.ToString(dateFormatString));
                    orderData.Add(countData?.Count ?? 0);
                    revenueData.Add(revenueItem?.Revenue ?? 0);
                }

                // Statistics for Orders trong khoảng thời gian đã chọn
                int totalOrders = allOrdersInRange.Count;
                int newOrders = allOrdersInRange.Count(o => o.TrangThaiDonHang == "Đang chờ xử lý" || o.TrangThaiDonHang == "Chờ thanh toán" || o.TrangThaiDonHang == "Đã thanh toán" || o.TrangThaiDonHang == "Đã vận chuyển" || o.TrangThaiDonHang == "Đã xác nhận nhận hàng");
                int deliveredOrders = allOrdersInRange.Count(o => o.TrangThaiDonHang == "Đã giao" || o.TrangThaiDonHang == "Đã hoàn thành");
                int cancelledOrders = allOrdersInRange.Count(o => o.TrangThaiDonHang == "Đã hủy");

                // Tính doanh thu chỉ từ đơn hàng đã hoàn thành/đã giao
                decimal totalRevenue = allOrdersInRange
                    .Where(o => 
                                o.TrangThaiDonHang == "Đã giao" ||
                                o.TrangThaiDonHang == "Đã hoàn thành")
                    .Sum(o => o.TongSoTien);

                // Lấy tất cả các bản ghi Escrow đã giải ngân
                var allEscrows = db.Escrows
                    .Where(e => e.TrangThai == "Đã giải ngân")
                    .ToList();

                // Sau đó thực hiện lọc và tính tổng trong bộ nhớ
                //decimal totalCommission = allEscrows
                //    .Where(e => e.NgayGiaiNgan >= startDate.Date && e.NgayGiaiNgan <= endDatePlusOne.Date)
                //    .Sum(e => e.PhiNenTang);
                // Tính tổng phí nền tảng từ Escrows đã giải ngân trong khoảng thời gian
                var escrowCommission = allEscrows
                    .Where(e => e.NgayGiaiNgan >= startDate.Date && e.NgayGiaiNgan <= endDatePlusOne.Date)
                    .Sum(e => e.PhiNenTang);

                // Lấy phí phạt từ GhiChepVi (các giao dịch phí nền tảng với mô tả bắt đầu bằng "Phí phạt 50%")
                var penaltyCommission = db.GhiChepVis
                    .Where(g => g.LoaiGiaoDich == "Phí nền tảng"
                        && g.TrangThai == "Thành công"
                        && g.MoTa.StartsWith("Phí phạt 50%")
                        && g.NgayGiaoDich >= startDate.Date
                        && g.NgayGiaoDich <= endDatePlusOne.Date)
                    .ToList()
                    .Sum(g => Math.Abs(g.SoTien));

                // Tổng cộng cả hai loại phí
                var totalCommission = escrowCommission + penaltyCommission;
                return Json(new
                {
                    // Thống kê người dùng
                    totalUsers,
                    activeUsers,
                    inactiveUsers,
                    pendingUpgradeUsers,
                    buyers,
                    sellers,

                    // Thống kê sản phẩm
                    totalProducts,
                    approvedProducts,
                    pendingProducts,
                    rejectedProducts,

                    // Thống kê đơn hàng
                    totalOrders,
                    newOrders,
                    deliveredOrders,
                    cancelledOrders,

                    // Dữ liệu biểu đồ
                    chartLabels,
                    orderData,
                    revenueData,

                    // Thống kê doanh thu
                    totalRevenue,
                    totalCommission,

                    // Thông tin về khoảng thời gian
                    startDate = startDate.ToString("dd/MM/yyyy"),
                    endDate = endDate.ToString("dd/MM/yyyy"),
                    timeRange
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi GetDashboardData: " + ex.Message);
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }



        #region ViewModels
        // Định nghĩa các ViewModel như các lớp lồng nhau trong controller
        public class ActivityViewModel
        {
            public int Id { get; set; }
            public string ActivityType { get; set; } // Loại hoạt động: NewUser, NewOrder, NewProduct, etc.
            public string Message { get; set; } // Mô tả hoạt động
            public DateTime ActivityTime { get; set; } // Thời gian hoạt động
            public string IconClass { get; set; } // Class CSS cho biểu tượng
            public string ColorClass { get; set; } // Class CSS cho màu sắc
        }
        public class DashboardViewModel
        {
            // Thống kê người dùng
            public int TotalUsers { get; set; }
            public int ActiveUsers { get; set; }
            public int InactiveUsers { get; set; }
            public int PendingUpgradeUsers { get; set; }
            public int Buyers { get; set; }
            public int Sellers { get; set; }

            // Thống kê sản phẩm
            public int TotalProducts { get; set; }
            public int ApprovedProducts { get; set; }
            public int PendingProducts { get; set; }
            public int RejectedProducts { get; set; }

            // Thống kê đơn hàng
            public int TotalOrders { get; set; }
            public int NewOrders { get; set; }
            public int DeliveredOrders { get; set; }
            public int CancelledOrders { get; set; }

            // Dữ liệu biểu đồ
            public List<ChartDataViewModel> OrderChartData { get; set; }

            // Thống kê doanh thu
            public decimal TotalRevenue { get; set; }
            public decimal TotalCommission { get; set; }

            // Danh sách các mục
            public List<OrderViewModel> RecentOrders { get; set; }
            public List<ProductViewModel> PendingProductApprovals { get; set; }
            public List<UserViewModel> PendingUpgrades { get; set; }
            public List<SellerStatsViewModel> TopSellers { get; set; }
            public List<TransactionViewModel> RecentTransactions { get; set; }
            public List<ActivityViewModel> RecentActivities { get; set; }
        }

        public class ChartDataViewModel
        {
            public string Date { get; set; }
            public int Count { get; set; }
            public decimal Revenue { get; set; }
        }

        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public string CustomerName { get; set; }
            public decimal TotalAmount { get; set; }
            public string OrderStatus { get; set; }
            public DateTime OrderDate { get; set; }
        }

        public class ProductViewModel
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public string SellerName { get; set; }
            public int SellerId { get; set; }
        }

        public class UserViewModel
        {
            public int UserId { get; set; }
            public string UserName { get; set; }
            public string Email { get; set; }
        }

        public class SellerStatsViewModel
        {
            public int SellerId { get; set; }
            public string SellerName { get; set; }
            public int OrderCount { get; set; }
            public decimal Revenue { get; set; }
        }

        public class TransactionViewModel
        {
            public int TransactionId { get; set; }
            public string TransactionStatus { get; set; }
            public decimal Amount { get; set; }
        }
        #endregion

    }
}