using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebApplication1.Models;
using CompareAttribute = System.ComponentModel.DataAnnotations.CompareAttribute;

namespace WebApplication1.Controllers
{
    public class NguoiDungsController : Controller
    {
        private MedinetDATN db = new MedinetDATN();



        // GET: NguoiDungs/Profile
        [Authorize]
        public ActionResult Profile()
        {
            // Lấy người dùng hiện tại
            string email = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

            if (nguoiDung == null)
            {
                return RedirectToAction("Login");
            }

            var viewModel = new ProfileViewModel
            {
                MaNguoiDung = nguoiDung.MaNguoiDung,
                HoTen = nguoiDung.TenNguoiDung,
                Email = nguoiDung.Email,
                SoDienThoai = nguoiDung.SoDienThoai,
                GioiTinh = nguoiDung.GioiTinh,
                NgaySinh = nguoiDung.NgayThangNamSinh ?? DateTime.Now,
                AnhDaiDien = nguoiDung.AnhDaiDien,
                DiaChi = nguoiDung.DiaChi
            };

            return View("~/Views/NguoiDungs/Profile.cshtml", viewModel);
        }

        // POST: NguoiDungs/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Profile(ProfileViewModel model, HttpPostedFileBase anhDaiDien)
        {
            if (ModelState.IsValid)
            {
                string email = User.Identity.Name;
                var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

                if (nguoiDung == null)
                {
                    return RedirectToAction("Login");
                }

                // Cập nhật thông tin người dùng
                nguoiDung.TenNguoiDung = model.HoTen;
                nguoiDung.SoDienThoai = model.SoDienThoai;
                nguoiDung.GioiTinh = model.GioiTinh;
                nguoiDung.NgayThangNamSinh = model.NgaySinh;
                nguoiDung.NgayCapNhat = DateTime.Now;
                nguoiDung.DiaChi = model.DiaChi;

                // Xử lý upload ảnh đại diện nếu có
                if (anhDaiDien != null && anhDaiDien.ContentLength > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(anhDaiDien.FileName);
                    string path = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                    anhDaiDien.SaveAs(path);
                    nguoiDung.AnhDaiDien = "~/Content/Images/" + fileName;
                }

                // Thêm thông báo cho người dùng
                var thongBao = new ThongBao
                {
                    MaNguoiDung = nguoiDung.MaNguoiDung, // Sử dụng MaNguoiDung từ đối tượng nguoiDung
                    LoaiThongBao = "TaiKhoan",
                    TieuDe = "Cập nhật thông tin thành công",
                    TinNhan = "Thông tin cá nhân của bạn đã được cập nhật thành công.",
                    MucDoQuanTrong = 0,
                    DuongDanChiTiet = "/NguoiDungs/Profile",
                    NgayTao = DateTime.Now // Thêm ngày tạo nếu cần
                };

                // Thêm thông báo vào database
                db.ThongBaos.Add(thongBao);

                db.SaveChanges();

                // Cập nhật Session
                Session["TenNguoiDung"] = nguoiDung.TenNguoiDung;
                // Cập nhật Session với đường dẫn ảnh mới
                Session["AnhDaiDien"] = nguoiDung.AnhDaiDien;

                ViewBag.SuccessMessage = "Cập nhật thông tin thành công!";
                // Cập nhật model để hiển thị trong view
                model.AnhDaiDien = nguoiDung.AnhDaiDien;
                return View(model);
            }

            return View(model);
        }

        // GET: NguoiDungs/ChangePassword
        [Authorize]
        public ActionResult ChangePassword()
        {
            string email = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

            if (nguoiDung == null)
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            ViewBag.AnhDaiDien = nguoiDung.AnhDaiDien;
            ViewBag.TenNguoiDung = nguoiDung.TenNguoiDung;

            return View();
        }

        // POST: NguoiDungs/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                string email = User.Identity.Name;
                var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

                if (nguoiDung == null)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Kiểm tra mật khẩu hiện tại
                string currentPasswordHash = MaHoaMatKhau(model.MatKhauHienTai);
                if (nguoiDung.MatKhauMaHoa != currentPasswordHash)
                {
                    ModelState.AddModelError("MatKhauHienTai", "Mật khẩu hiện tại không đúng.");
                    return View(model);
                }

                // Cập nhật mật khẩu mới
                nguoiDung.MatKhauMaHoa = MaHoaMatKhau(model.MatKhauMoi);
                nguoiDung.NgayCapNhat = DateTime.Now;
                // Tạo thông báo khi đổi mật khẩu thành công
                var thongBao = new ThongBao
                {
                    MaNguoiDung = nguoiDung.MaNguoiDung,
                    LoaiThongBao = "TaiKhoan",
                    TieuDe = "Đổi mật khẩu thành công",
                    TinNhan = "Mật khẩu của bạn đã được thay đổi thành công.",
                    MucDoQuanTrong = 1, // Mức độ cao hơn vì liên quan đến bảo mật
                    DuongDanChiTiet = "/NguoiDungs/Profile",
                    NgayTao = DateTime.Now
                };
                // Thêm thông báo vào database
                db.ThongBaos.Add(thongBao);
                db.SaveChanges();
                ViewBag.AnhDaiDien = nguoiDung.AnhDaiDien;
                ViewBag.TenNguoiDung = nguoiDung.TenNguoiDung;
                ViewBag.SuccessMessage = "Đổi mật khẩu thành công!";
                return View(new ChangePasswordViewModel());
            }

            return View(model);
        }

        // GET: NguoiDungs/SellerUpgrade
        [Authorize]
        public ActionResult SellerUpgrade()
        {
            string email = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

            if (nguoiDung == null)
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            ViewBag.AnhDaiDien = nguoiDung.AnhDaiDien;
            ViewBag.TenNguoiDung = nguoiDung.TenNguoiDung;

            // Kiểm tra xem người dùng đã là người bán chưa
            var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);

            // Nếu người dùng đang trong quá trình đăng ký (ví dụ: có thể có refresh trang) 
            // hoặc đã là người bán, nạp thông tin chứng chỉ
            if (nguoiBan != null)
            {
                // Nạp thông tin chứng chỉ nếu có
                var danhSachChungChi = db.AnhChungChis
                    .Where(c => c.MaNguoiBan == nguoiBan.MaNguoiBan)
                    .ToList();

                // Truyền danh sách chứng chỉ vào ViewBag
                ViewBag.DanhSachChungChi = danhSachChungChi;
            }

            // Truyền trạng thái từ chối vào view
            ViewBag.BiTuChoiNangCap = nguoiDung.BiTuChoiNangCap;
            ViewBag.NgayTuChoiNangCap = nguoiDung.NgayTuChoiNangCap;

            ViewBag.TenNguoiDung = nguoiDung.TenNguoiDung;
            ViewBag.AnhDaiDien = nguoiDung.AnhDaiDien;

            return View(nguoiBan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult SellerUpgrade(NguoiBan model, bool isAjax = false)
        {
            string email = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

            if (nguoiDung == null)
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "Người dùng không hợp lệ." });
                }
                return RedirectToAction("DangNhap", "DangNhap");
            }

            // Kiểm tra xem đã bị từ chối nâng cấp hay chưa
            if (nguoiDung.BiTuChoiNangCap)
            {
                ViewBag.BiTuChoiNangCap = true;
                ViewBag.NgayTuChoiNangCap = nguoiDung.NgayTuChoiNangCap;
            }

            // Đối với AJAX request từ bước 1 - chỉ lưu thông tin cơ bản
            if (isAjax)
            {
                try
                {
                    // Kiểm tra dữ liệu đầu vào
                    if (string.IsNullOrEmpty(model.TenCuaHang) || string.IsNullOrEmpty(model.SoDienThoaiCuaHang) ||
                        string.IsNullOrEmpty(model.DiaChiCuaHang) || string.IsNullOrEmpty(model.MoTaCuaHang))
                    {
                        return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin cửa hàng." });
                    }

                    // Kiểm tra định dạng số điện thoại
                    if (!System.Text.RegularExpressions.Regex.IsMatch(model.SoDienThoaiCuaHang, @"^\d{10}$"))
                    {
                        return Json(new { success = false, message = "Số điện thoại phải có 10 chữ số." });
                    }

                    // Tạo hoặc cập nhật thông tin người bán
                    var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
                    if (nguoiBan == null)
                    {
                        nguoiBan = new NguoiBan
                        {
                            MaNguoiDung = nguoiDung.MaNguoiDung,
                            TenCuaHang = model.TenCuaHang,
                            MoTaCuaHang = model.MoTaCuaHang,
                            DiaChiCuaHang = model.DiaChiCuaHang,
                            SoDienThoaiCuaHang = model.SoDienThoaiCuaHang,
                            NgayTao = DateTime.Now
                        };
                        db.NguoiBans.Add(nguoiBan);
                    }
                    else
                    {
                        nguoiBan.TenCuaHang = model.TenCuaHang;
                        nguoiBan.MoTaCuaHang = model.MoTaCuaHang;
                        nguoiBan.DiaChiCuaHang = model.DiaChiCuaHang;
                        nguoiBan.SoDienThoaiCuaHang = model.SoDienThoaiCuaHang;
                    }

                    // Lưu bản ghi đầu tiên - chưa đánh dấu đã xét duyệt
                    db.SaveChanges();

                    return Json(new
                    {
                        success = true,
                        message = "Lưu thông tin cửa hàng thành công.",
                        maNguoiBan = nguoiBan.MaNguoiBan
                    });
                }
                catch (Exception ex)
                {
                    // Log lỗi
                    System.Diagnostics.Debug.WriteLine("Error in SellerUpgrade AJAX: " + ex.Message);

                    // Trả về thông báo lỗi
                    return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
                }
            }

            // Nếu là form submit bình thường (bước 3 - xác nhận cuối cùng)
            if (ModelState.IsValid)
            {
                // Tạo hoặc cập nhật thông tin người bán
                var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
                if (nguoiBan == null)
                {
                    nguoiBan = new NguoiBan
                    {
                        MaNguoiDung = nguoiDung.MaNguoiDung,
                        TenCuaHang = model.TenCuaHang,
                        MoTaCuaHang = model.MoTaCuaHang,
                        DiaChiCuaHang = model.DiaChiCuaHang,
                        SoDienThoaiCuaHang = model.SoDienThoaiCuaHang,
                        NgayTao = DateTime.Now
                    };
                    db.NguoiBans.Add(nguoiBan);
                }
                else
                {
                    nguoiBan.TenCuaHang = model.TenCuaHang;
                    nguoiBan.MoTaCuaHang = model.MoTaCuaHang;
                    nguoiBan.DiaChiCuaHang = model.DiaChiCuaHang;
                    nguoiBan.SoDienThoaiCuaHang = model.SoDienThoaiCuaHang;
                }

                // Cập nhật trạng thái đăng ký thành người bán
                nguoiDung.XetDuyetThanhNguoiBan = true;
                nguoiDung.TrangThai = "Upgrade";

                // Tạo thông báo cho người dùng khi yêu cầu nâng cấp thành seller
                var thongBao = new ThongBao
                {
                    MaNguoiDung = nguoiDung.MaNguoiDung,
                    LoaiThongBao = "TaiKhoan",
                    TieuDe = "Đã gửi yêu cầu trở thành người bán",
                    TinNhan = "Yêu cầu trở thành người bán của bạn đã được gửi và đang chờ xét duyệt. Chúng tôi sẽ thông báo kết quả trong thời gian sớm nhất.",
                    MucDoQuanTrong = 2, // Mức độ quan trọng cao
                    DuongDanChiTiet = "/NguoiDungs/Profile",
                    NgayTao = DateTime.Now
                };

                // Thêm thông báo vào database
                db.ThongBaos.Add(thongBao);

                // Tạo thông báo cho admin về yêu cầu trở thành người bán
                var adminList = db.NguoiDungs.Where(n => n.VaiTro == "Admin").ToList();
                foreach (var admin in adminList)
                {
                    var thongBaoAdmin = new ThongBao
                    {
                        MaNguoiDung = admin.MaNguoiDung,
                        LoaiThongBao = "SellerRequest",
                        TieuDe = "Yêu cầu trở thành người bán mới",
                        TinNhan = $"Người dùng {nguoiDung.TenNguoiDung} (ID: {nguoiDung.MaNguoiDung}) đã gửi yêu cầu trở thành người bán. Vui lòng xem xét và phê duyệt.",
                        MucDoQuanTrong = 2, // Mức độ quan trọng cao
                        DuongDanChiTiet = "/Admin/UserManagement",
                        NgayTao = DateTime.Now
                    };
                    db.ThongBaos.Add(thongBaoAdmin);
                }

                db.SaveChanges();

                ViewBag.SuccessMessage = "Thông tin đã được gửi. Chúng tôi sẽ xem xét và phê duyệt trong thời gian sớm nhất.";

                ViewBag.AnhDaiDien = nguoiDung.AnhDaiDien;
                ViewBag.TenNguoiDung = nguoiDung.TenNguoiDung;

                return View(nguoiBan);
            }

            ViewBag.AnhDaiDien = nguoiDung.AnhDaiDien;
            ViewBag.TenNguoiDung = nguoiDung.TenNguoiDung;

            return View(model);
        }

        // GET: NguoiDungs/ReRegisterSeller
        public ActionResult ReRegisterSeller()
        {
            int maNguoiDung = Convert.ToInt32(Session["MaNguoiDung"]);
            var nguoiDung = db.NguoiDungs.Find(maNguoiDung);

            if (nguoiDung == null)
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            // Đặt lại trạng thái từ chối
            nguoiDung.BiTuChoiNangCap = false;
            nguoiDung.NgayTuChoiNangCap = null;
            db.SaveChanges();

            // Chuyển hướng đến trang đăng ký seller
            return RedirectToAction("SellerUpgrade");
        }

        // GET: NguoiDungs/DeleteAccount
        [Authorize]
        public ActionResult DeleteAccount()
        {
            string email = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

            if (nguoiDung == null)
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            ViewBag.AnhDaiDien = nguoiDung.AnhDaiDien;
            ViewBag.TenNguoiDung = nguoiDung.TenNguoiDung;
            ViewBag.UserEmail = nguoiDung.Email;

            return View(new DeleteAccountViewModel());
        }

        // POST: NguoiDungs/DeleteAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult DeleteAccount(DeleteAccountViewModel model)
        {
            string email = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

            if (nguoiDung == null)
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            if (ModelState.IsValid)
            {
                // Đánh dấu tài khoản là đã bị xóa hoặc không hoạt động
                nguoiDung.TrangThai = "Inactive";
                db.SaveChanges();

                // Đăng xuất người dùng
                FormsAuthentication.SignOut();
                Session.Clear();
                Session.Abandon();

                return RedirectToAction("DangNhap","DangNhap", new { message = "Tài khoản của bạn đã được yêu cầu xóa." });
            }

            ViewBag.AnhDaiDien = nguoiDung.AnhDaiDien;
            ViewBag.TenNguoiDung = nguoiDung.TenNguoiDung;
            ViewBag.UserEmail = nguoiDung.Email;
            return View(model);
        }

        // Hàm mã hóa mật khẩu
        private string MaHoaMatKhau(string matKhau)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(matKhau);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }


        // Thêm các action sau vào NguoiDungsController

        // GET: NguoiDungs/SellerProfile
        [Authorize]
        public ActionResult SellerProfile()
        {
            string email = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

            if (nguoiDung == null)
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            // Kiểm tra xem người dùng có phải là người bán không
            var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
            if (nguoiBan == null)
            {
                // Nếu không phải người bán, chuyển hướng đến trang nâng cấp tài khoản
                return RedirectToAction("SellerUpgrade", "NguoiDungs");
            }

            ViewBag.AnhDaiDien = nguoiDung.AnhDaiDien;
            ViewBag.TenNguoiDung = nguoiDung.TenNguoiDung;

            return View(nguoiBan);
        }

        // GET: NguoiDungs/EditSellerProfile
        [Authorize]
        public ActionResult EditSellerProfile()
        {
            string email = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

            if (nguoiDung == null)
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            // Kiểm tra xem người dùng có phải là người bán không
            var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
            //var nguoiBan = db.Database.SqlQuery<NguoiBan>(
            //    "SELECT * FROM NguoiBan WHERE MaNguoiDung = @p0",
            //    nguoiDung.MaNguoiDung
            //).FirstOrDefault();
            if (nguoiBan == null)
            {
                // Nếu không phải người bán, chuyển hướng đến trang nâng cấp tài khoản
                return RedirectToAction("SellerUpgrade", "NguoiDungs");
            }

            // Tạo ViewModel kết hợp thông tin người dùng và người bán
            var viewModel = new SellerProfileViewModel
            {
                // Thông tin người dùng
                MaNguoiDung = nguoiDung.MaNguoiDung,
                TenNguoiDung = nguoiDung.TenNguoiDung,
                Email = nguoiDung.Email,
                SoDienThoai = nguoiDung.SoDienThoai,
                GioiTinh = nguoiDung.GioiTinh,
                NgaySinh = nguoiDung.NgayThangNamSinh ?? DateTime.Now,
                AnhDaiDien = nguoiDung.AnhDaiDien,

                // Thông tin người bán/cửa hàng
                MaNguoiBan = nguoiBan.MaNguoiBan,
                TenCuaHang = nguoiBan.TenCuaHang,
                MoTaCuaHang = nguoiBan.MoTaCuaHang,
                DiaChiCuaHang = nguoiBan.DiaChiCuaHang,
                SoDienThoaiCuaHang = nguoiBan.SoDienThoaiCuaHang
            };
            // Nạp danh sách chứng chỉ
            var danhSachChungChi = db.AnhChungChis
               .Where(c => c.MaNguoiBan == nguoiBan.MaNguoiBan)
               .ToList() // Thực thi truy vấn trước
               .Select(c => new AnhChungChi // Sau đó thực hiện chiếu trong bộ nhớ
               {
                   MaAnhChungChi = c.MaAnhChungChi,
                   TenChungChi = c.TenChungChi,
                   DuongDanAnh = c.DuongDanAnh,
                   NgayCapNhat = c.NgayCapNhat
               })
               .ToList();

            viewModel.DanhSachChungChi = danhSachChungChi;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult EditSellerProfile(SellerProfileViewModel model, HttpPostedFileBase anhDaiDien, IEnumerable<HttpPostedFileBase> AnhChungChi)
        {
            // Debug - kiểm tra giá trị trước khi lưu
            System.Diagnostics.Debug.WriteLine("DiaChiCuaHang: " + model.DiaChiCuaHang);
            System.Diagnostics.Debug.WriteLine("SoDienThoaiCuaHang: " + model.SoDienThoaiCuaHang);

            if (ModelState.IsValid)
            {
                string email = User.Identity.Name;
                var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

                if (nguoiDung == null)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
                if (nguoiBan == null)
                {
                    return RedirectToAction("SellerUpgrade", "NguoiDungs");
                }

                // Cập nhật thông tin người dùng
                nguoiDung.TenNguoiDung = model.TenNguoiDung;
                nguoiDung.SoDienThoai = model.SoDienThoai;
                nguoiDung.GioiTinh = model.GioiTinh;
                nguoiDung.NgayThangNamSinh = model.NgaySinh;
                nguoiDung.NgayCapNhat = DateTime.Now;

                // Xử lý upload ảnh đại diện nếu có
                if (anhDaiDien != null && anhDaiDien.ContentLength > 0)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(anhDaiDien.FileName);
                    string path = Path.Combine(Server.MapPath("~/Content/Images"), fileName);
                    anhDaiDien.SaveAs(path);
                    nguoiDung.AnhDaiDien = "~/Content/Images/" + fileName;
                    Session["AnhDaiDien"] = nguoiDung.AnhDaiDien;
                }

                // Cập nhật thông tin người bán - ĐẢM BẢO THÔNG TIN ĐƯỢC LƯU
                nguoiBan.TenCuaHang = model.TenCuaHang;
                nguoiBan.MoTaCuaHang = model.MoTaCuaHang;
                nguoiBan.DiaChiCuaHang = model.DiaChiCuaHang;
                nguoiBan.SoDienThoaiCuaHang = model.SoDienThoaiCuaHang;


                // Tạo thông báo cho người bán khi cập nhật thông tin cửa hàng
                var thongBao = new ThongBao
                {
                    MaNguoiDung = nguoiDung.MaNguoiDung,
                    LoaiThongBao = "CuaHang",
                    TieuDe = "Cập nhật thông tin cửa hàng",
                    TinNhan = "Thông tin cửa hàng của bạn đã được cập nhật thành công.",
                    MucDoQuanTrong = 1,
                    DuongDanChiTiet = "/NguoiDungs/EditSellerProfile",
                    NgayTao = DateTime.Now
                };

                // Thêm thông báo vào database
                db.ThongBaos.Add(thongBao);



                // Danh sách chứng chỉ mới để trả về cho AJAX
                List<object> newCertificates = new List<object>();
                // Xử lý upload chứng chỉ
                if (AnhChungChi != null)
                {
                    foreach (var file in AnhChungChi.Where(f => f != null && f.ContentLength > 0))
                    {
                        // Tạo tên file duy nhất
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string path = Path.Combine(Server.MapPath("~/Content/Images/Certificates"), fileName);

                        // Lưu file
                        file.SaveAs(path);

                        // Thêm vào cơ sở dữ liệu
                        var chungChiMoi = new AnhChungChi
                        {
                            MaNguoiBan = nguoiBan.MaNguoiBan,
                            TenChungChi = Path.GetFileNameWithoutExtension(file.FileName), // hoặc có thể là tham số từ form
                            DuongDanAnh = "~/Content/Images/Certificates/" + fileName,
                            NgayCapNhat = DateTime.Now // hoặc có thể là tham số từ form
                        };

                        db.AnhChungChis.Add(chungChiMoi);

                        // Lưu lại để trả về
                        newCertificates.Add(new
                        {
                            Id = chungChiMoi.MaAnhChungChi,
                            Ten = chungChiMoi.TenChungChi,
                            Anh = chungChiMoi.DuongDanAnh,
                            Ngay = chungChiMoi.NgayCapNhat
                        });
                    }
                    // Đánh dấu entity đã thay đổi
                    db.Entry(nguoiBan).State = EntityState.Modified;

                    try
                    {
                        db.SaveChanges();
                        ////  Nạp lại danh sách chứng chỉ sau khi lưu
                        //model.DanhSachChungChi = db.AnhChungChis
                        //    .Where(c => c.MaNguoiBan == nguoiBan.MaNguoiBan)
                        //    .ToList();

                        // Cập nhật lại ID cho các chứng chỉ mới sau khi SaveChanges
                        var savedCertificates = db.AnhChungChis
                            .Where(c => c.MaNguoiBan == nguoiBan.MaNguoiBan)
                            .OrderByDescending(c => c.MaAnhChungChi)
                            .Take(newCertificates.Count)
                            .ToList();

                        // Kiểm tra nếu request là AJAX
                        if (Request.IsAjaxRequest())
                        {
                            // Trả về danh sách chứng chỉ mới dưới dạng JSON
                            return Json(new
                            {
                                success = true,
                                message = "Tải lên thành công",
                                certificates = savedCertificates.Select(c => new {
                                    Id = c.MaAnhChungChi,
                                    Ten = c.TenChungChi,
                                    Anh = Url.Content(c.DuongDanAnh),
                                    Ngay = c.NgayCapNhat.ToString("dd/MM/yyyy")
                                })
                            });
                        }

                        // Nếu không phải AJAX, nạp lại danh sách và trả về view
                        model.DanhSachChungChi = db.AnhChungChis
                            .Where(c => c.MaNguoiBan == nguoiBan.MaNguoiBan)
                            .ToList();

                        // Kiểm tra sau khi lưu
                        var checkAfterSave = db.NguoiBans.FirstOrDefault(s => s.MaNguoiBan == nguoiBan.MaNguoiBan);
                        System.Diagnostics.Debug.WriteLine("Sau khi lưu - DiaChiCuaHang: " + checkAfterSave.DiaChiCuaHang);
                        System.Diagnostics.Debug.WriteLine("Sau khi lưu - SoDienThoaiCuaHang: " + checkAfterSave.SoDienThoaiCuaHang);

                        // Kiểm tra số lượng chứng chỉ sau khi lưu
                        var countCertificates = db.AnhChungChis.Count(c => c.MaNguoiBan == nguoiBan.MaNguoiBan);
                        System.Diagnostics.Debug.WriteLine("Số lượng chứng chỉ đã lưu: " + countCertificates);

                        // Cập nhật Session
                        Session["TenNguoiDung"] = nguoiDung.TenNguoiDung;
                        Session["AnhDaiDien"] = nguoiDung.AnhDaiDien;
                        ViewBag.SuccessMessage = "Cập nhật thông tin thành công!";

                        // Cập nhật model để hiển thị lại
                        model.AnhDaiDien = nguoiDung.AnhDaiDien;

                        return View(model);
                    }
                    catch (Exception ex)
                    {
                        // ... xử lý lỗi ...
                        if (Request.IsAjaxRequest())
                        {
                            return Json(new { success = false, message = "Lỗi: " + ex.Message });
                        }

                        ModelState.AddModelError("", "Lỗi khi lưu dữ liệu: " + ex.Message);
                        // Log lỗi chi tiết
                        System.Diagnostics.Debug.WriteLine("Lỗi DB: " + ex.ToString());
                    }
                }
                else
                {
                    //// Nếu không có chứng chỉ mới, vẫn cần nạp lại danh sách chứng chỉ hiện có
                    //db.SaveChanges();
                    //model.DanhSachChungChi = db.AnhChungChis
                    //    .Where(c => c.MaNguoiBan == nguoiBan.MaNguoiBan)
                    //    .ToList();
                    // In ra lỗi validation
                    foreach (var modelState in ModelState.Values)
                    {
                        foreach (var error in modelState.Errors)
                        {
                            System.Diagnostics.Debug.WriteLine("Lỗi validation: " + error.ErrorMessage);
                        }
                    }

                }

                return View(model);
            }
            return View(model);
        }
        // GET: NguoiDungs/SellerProducts
        [Authorize]
        public ActionResult SellerProducts()
        {
            string email = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

            if (nguoiDung == null)
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            // Kiểm tra xem người dùng có phải là người bán không
            var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
            if (nguoiBan == null)
            {
                // Nếu không phải người bán, chuyển hướng đến trang nâng cấp tài khoản
                return RedirectToAction("SellerUpgrade", "NguoiDungs");
            }

            ViewBag.AnhDaiDien = nguoiDung.AnhDaiDien;
            ViewBag.TenNguoiDung = nguoiDung.TenNguoiDung;

            // Lấy danh sách sản phẩm của người bán (code tương ứng với model của bạn)
            // var products = db.SanPhams.Where(p => p.MaNguoiBan == nguoiBan.MaNguoiBan).ToList();

            return View();
        }

        // GET: NguoiDungs/SellerOrders
        [Authorize]
        public ActionResult SellerOrders()
        {
            string email = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

            if (nguoiDung == null)
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            // Kiểm tra xem người dùng có phải là người bán không
            var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
            if (nguoiBan == null)
            {
                // Nếu không phải người bán, chuyển hướng đến trang nâng cấp tài khoản
                return RedirectToAction("SellerUpgrade", "NguoiDungs");
            }

            ViewBag.AnhDaiDien = nguoiDung.AnhDaiDien;
            ViewBag.TenNguoiDung = nguoiDung.TenNguoiDung;

            // Lấy danh sách đơn hàng của người bán (code tương ứng với model của bạn)
            // var orders = db.DonHangs.Where(o => o.MaNguoiBan == nguoiBan.MaNguoiBan).ToList();

            return View();
        }

        // GET: NguoiDungs/SellerStatistics
        [Authorize]
        public ActionResult SellerStatistics()
        {
            string email = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

            if (nguoiDung == null)
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            // Kiểm tra xem người dùng có phải là người bán không
            var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
            if (nguoiBan == null)
            {
                // Nếu không phải người bán, chuyển hướng đến trang nâng cấp tài khoản
                return RedirectToAction("SellerUpgrade", "NguoiDungs");
            }

            ViewBag.AnhDaiDien = nguoiDung.AnhDaiDien;
            ViewBag.TenNguoiDung = nguoiDung.TenNguoiDung;

            // Tính toán thống kê (code tương ứng với model của bạn)

            return View();
        }

        // GET: NguoiDungs/SellerChangePassword
        [Authorize]
        public ActionResult SellerChangePassword()
        {
            string email = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

            if (nguoiDung == null)
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            // Kiểm tra xem người dùng có phải là người bán không
            var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
            if (nguoiBan == null)
            {
                // Nếu không phải người bán, chuyển hướng đến trang nâng cấp tài khoản
                return RedirectToAction("SellerUpgrade", "NguoiDungs");
            }

            ViewBag.AnhDaiDien = nguoiDung.AnhDaiDien;
            ViewBag.TenNguoiDung = nguoiDung.TenNguoiDung;

            // Create the view model with the seller ID
            var model = new ChangePasswordViewModel
            {
                MaNguoiBan = nguoiBan.MaNguoiBan
            };

            return View(model);
        }

        // POST: NguoiDungs/SellerChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult SellerChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                string email = User.Identity.Name;
                var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

                if (nguoiDung == null)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Kiểm tra xem người dùng có phải là người bán không
                var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
                if (nguoiBan == null)
                {
                    return RedirectToAction("SellerUpgrade", "NguoiDungs");
                }

                // Kiểm tra mật khẩu hiện tại
                string currentPasswordHash = MaHoaMatKhau(model.MatKhauHienTai);
                if (nguoiDung.MatKhauMaHoa != currentPasswordHash)
                {
                    ModelState.AddModelError("MatKhauHienTai", "Mật khẩu hiện tại không đúng.");
                    return View(model);
                }

                // Cập nhật mật khẩu mới
                nguoiDung.MatKhauMaHoa = MaHoaMatKhau(model.MatKhauMoi);
                nguoiDung.NgayCapNhat = DateTime.Now;

                // Tạo thông báo khi người bán đổi mật khẩu thành công
                var thongBao = new ThongBao
                {
                    MaNguoiDung = nguoiDung.MaNguoiDung,
                    LoaiThongBao = "TaiKhoan",
                    TieuDe = "Đổi mật khẩu thành công",
                    TinNhan = "Mật khẩu tài khoản người bán của bạn đã được thay đổi thành công.",
                    MucDoQuanTrong = 1, // Mức độ cao vì liên quan đến bảo mật
                    DuongDanChiTiet = "/NguoiDungs/EditSellerProfile",
                    NgayTao = DateTime.Now
                };

                // Thêm thông báo vào database
                db.ThongBaos.Add(thongBao);


                db.SaveChanges();

                ViewBag.SuccessMessage = "Đổi mật khẩu thành công!";
                return View(new ChangePasswordViewModel());
            }

            return View(model);
        }

        // GET: NguoiDungs/SellerDeleteAccount
        [Authorize]
        public ActionResult SellerDeleteAccount()
        {
            string email = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

            if (nguoiDung == null)
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            // Kiểm tra xem người dùng có phải là người bán không
            var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
            if (nguoiBan == null)
            {
                // Nếu không phải người bán, chuyển hướng đến trang nâng cấp tài khoản
                return RedirectToAction("SellerUpgrade", "NguoiDungs");
            }

            ViewBag.AnhDaiDien = nguoiDung.AnhDaiDien;
            ViewBag.TenNguoiDung = nguoiDung.TenNguoiDung;
            ViewBag.UserEmail = nguoiDung.Email;

            // Tạo model và đặt MaNguoiBan
            var model = new DeleteAccountViewModel
            {
                MaNguoiBan = nguoiBan.MaNguoiBan
                // Khởi tạo các thuộc tính khác nếu cần
            };
            return View(model);
        }

        // POST: NguoiDungs/SellerDeleteAccount
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult SellerDeleteAccount(DeleteAccountViewModel model)
        {
            string email = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

            if (nguoiDung == null)
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            // Kiểm tra xem người dùng có phải là người bán không
            var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
            if (nguoiBan == null)
            {
                return RedirectToAction("SellerUpgrade", "NguoiDungs");
            }

            if (ModelState.IsValid)
            {
                // Kiểm tra email xác nhận
                if (model.Email != nguoiDung.Email)
                {
                    ModelState.AddModelError("Email", "Email không khớp với tài khoản của bạn.");
                    ViewBag.UserEmail = nguoiDung.Email;
                    return View(model);
                }

                // Xóa thông tin người bán
                db.NguoiBans.Remove(nguoiBan);

                // Đánh dấu tài khoản là không còn là người bán
                nguoiDung.XetDuyetThanhNguoiBan = false;
                nguoiDung.VaiTro = "Buyer";

                // Tùy chọn: đánh dấu tài khoản là không hoạt động nếu muốn vô hiệu hóa hoàn toàn
                // nguoiDung.TrangThai = "Inactive";

                db.SaveChanges();

                // Đăng xuất người dùng
                FormsAuthentication.SignOut();
                Session.Clear();
                Session.Abandon();

                return RedirectToAction("DangNhap", "DangNhap", new { message = "Tài khoản người bán của bạn đã được xóa thành công." });
            }

            ViewBag.AnhDaiDien = nguoiDung.AnhDaiDien;
            ViewBag.TenNguoiDung = nguoiDung.TenNguoiDung;
            ViewBag.UserEmail = nguoiDung.Email;
            return View(model);
        }

        // Thêm action để xóa chứng chỉ
        [HttpPost]
        [Authorize]
        public ActionResult DeleteCertificate(int id)
        {
            string email = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);

            if (nguoiDung == null)
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            // Tìm chứng chỉ
            var chungChi = db.AnhChungChis.Find(id);

            // Kiểm tra quyền sở hữu
            if (chungChi != null && chungChi.NguoiBan.MaNguoiDung == nguoiDung.MaNguoiDung)
            {
                // Xóa file hình ảnh
                if (!string.IsNullOrEmpty(chungChi.DuongDanAnh))
                {
                    string fullPath = Server.MapPath(chungChi.DuongDanAnh);
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

                // Xóa record từ DB
                db.AnhChungChis.Remove(chungChi);
                db.SaveChanges();

                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Không tìm thấy chứng chỉ hoặc bạn không có quyền xóa." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult UploadCertificates(int MaNguoiBan, IEnumerable<HttpPostedFileBase> AnhChungChi)
        {
            if (AnhChungChi == null || !AnhChungChi.Any(f => f != null && f.ContentLength > 0))
            {
                return Json(new { success = false, message = "Không có file nào được chọn." });
            }

            string email = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == email);
            if (nguoiDung == null)
            {
                return Json(new { success = false, message = "Người dùng không hợp lệ." });
            }

            var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiBan == MaNguoiBan);
            if (nguoiBan == null || nguoiBan.MaNguoiDung != nguoiDung.MaNguoiDung)
            {
                return Json(new { success = false, message = "Người bán không hợp lệ." });
            }

            List<AnhChungChi> savedCertificates = new List<AnhChungChi>();

            try
            {
                // Đảm bảo thư mục tồn tại
                string certificatesDirectory = Server.MapPath("~/Content/Images/Certificates");
                if (!Directory.Exists(certificatesDirectory))
                {
                    Directory.CreateDirectory(certificatesDirectory);
                }

                foreach (var file in AnhChungChi.Where(f => f != null && f.ContentLength > 0))
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string path = Path.Combine(certificatesDirectory, fileName);

                    try
                    {
                        file.SaveAs(path);

                        var chungChiMoi = new AnhChungChi
                        {
                            MaNguoiBan = nguoiBan.MaNguoiBan,
                            TenChungChi = Path.GetFileNameWithoutExtension(file.FileName),
                            DuongDanAnh = "~/Content/Images/Certificates/" + fileName,
                            NgayCapNhat = DateTime.Now
                        };

                        db.AnhChungChis.Add(chungChiMoi);
                        savedCertificates.Add(chungChiMoi);
                    }
                    catch (Exception fileEx)
                    {
                        System.Diagnostics.Debug.WriteLine("Lỗi lưu file: " + fileEx.Message);
                        // Nếu có lỗi với một file cụ thể, tiếp tục với file tiếp theo
                        continue;
                    }
                }

                if (savedCertificates.Count > 0)
                {
                    db.SaveChanges();

                    return Json(new
                    {
                        success = true,
                        message = "Tải lên thành công",
                        certificates = savedCertificates.Select(c => new {
                            Id = c.MaAnhChungChi,
                            Ten = c.TenChungChi,
                            Anh = Url.Content(c.DuongDanAnh),
                            Ngay = c.NgayCapNhat.ToString("dd/MM/yyyy")
                        })
                    }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    return Json(new { success = false, message = "Không thể lưu bất kỳ file nào." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tổng thể: " + ex.Message);
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine("Inner Exception: " + ex.InnerException.Message);
                }

                return Json(new { success = false, message = "Lỗi xử lý: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // Thêm phương thức này vào NguoiDungsController

        // POST: NguoiDungs/CapNhatDiaChi
        [HttpPost]
        public ActionResult CapNhatDiaChi(string diaChi, string soDienThoai)
        {
            try
            {
                // Lấy ID người dùng hiện tại
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện chức năng này!" });
                }

                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrEmpty(diaChi))
                {
                    return Json(new { success = false, message = "Địa chỉ không được để trống!" });
                }

                // Kiểm tra số điện thoại
                if (string.IsNullOrEmpty(soDienThoai) || !System.Text.RegularExpressions.Regex.IsMatch(soDienThoai, @"^[0-9]{10}$"))
                {
                    return Json(new { success = false, message = "Số điện thoại không hợp lệ!" });
                }

                // Tìm người dùng trong cơ sở dữ liệu
                var nguoiDung = db.NguoiDungs.Find(maNguoiDung);
                if (nguoiDung == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng!" });
                }

                // Cập nhật thông tin địa chỉ và số điện thoại
                nguoiDung.DiaChi = diaChi;
                nguoiDung.SoDienThoai = soDienThoai;

                // Lưu thay đổi vào cơ sở dữ liệu
                db.Entry(nguoiDung).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Cập nhật địa chỉ và số điện thoại thành công!",
                    diaChi = diaChi,
                    soDienThoai = soDienThoai
                });
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine("Lỗi CapNhatDiaChi: " + ex.Message);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi cập nhật thông tin. Vui lòng thử lại sau!" });
            }
        }

        // Phương thức lấy ID người dùng hiện tại
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
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    // ViewModel cho thông tin cá nhân
    public class ProfileViewModel
    {
        public int MaNguoiDung { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [Display(Name = "Họ tên")]
        public string HoTen { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string SoDienThoai { get; set; }

        [Display(Name = "Giới tính")]
        public string GioiTinh { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime NgaySinh { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string AnhDaiDien { get; set; }
        [Display(Name = "Địa chỉ")]
        public string DiaChi { get; set; }
    }

    // ViewModel cho đổi mật khẩu
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string MatKhauHienTai { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [StringLength(100, ErrorMessage = "Mật khẩu phải có ít nhất {2} ký tự.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string MatKhauMoi { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Xác nhận mật khẩu mới")]
        [Compare("MatKhauMoi", ErrorMessage = "Mật khẩu mới và xác nhận mật khẩu không khớp.")]
        public string XacNhanMatKhauMoi { get; set; }

        public int MaNguoiBan { get; set; }
    }

    // ViewModel cho xóa tài khoản
    public class DeleteAccountViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn lý do xóa tài khoản.")]
        [Display(Name = "Lý do")]
        public string LyDo { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        public string Email { get; set; }
        public int MaNguoiBan { get; set; }
    }

    public class SellerProfileViewModel
    {
        // Thông tin người dùng
        public int MaNguoiDung { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [Display(Name = "Họ tên")]
        public string TenNguoiDung { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Số điện thoại")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string SoDienThoai { get; set; }

        [Display(Name = "Giới tính")]
        public string GioiTinh { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        public DateTime NgaySinh { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string AnhDaiDien { get; set; }

        // Thông tin người bán/cửa hàng
        public int MaNguoiBan { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên cửa hàng.")]
        [Display(Name = "Tên cửa hàng")]
        public string TenCuaHang { get; set; }

        [Display(Name = "Mô tả cửa hàng")]
        public string MoTaCuaHang { get; set; }

        [Display(Name = "Địa chỉ cửa hàng")]
        public string DiaChiCuaHang { get; set; }

        [Display(Name = "Số điện thoại cửa hàng")]
        [RegularExpression(@"^\d{10}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        public string SoDienThoaiCuaHang { get; set; }


        // Thêm thuộc tính cho chứng chỉ
        public List<AnhChungChi> DanhSachChungChi { get; set; }

        // Constructor
        public SellerProfileViewModel()
        {
            DanhSachChungChi = new List<AnhChungChi>();
        }
    }
}
