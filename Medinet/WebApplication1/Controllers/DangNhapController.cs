using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class DangNhapController : Controller
    {
        // Database connection string
        private string connectionString = "Data Source=DESKTOP-C6TH3H0;Initial Catalog=MediNet;Integrated Security=True";
        private MedinetDATN db = new MedinetDATN(); // Thêm DbContext

        // Hàm mã hóa mật khẩu
        private string HashPassword(string MatKhau)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(MatKhau);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        // POST: DangNhap
        [HttpPost]
        //21/4/2025
        // Hàm kiểm tra sản phẩm hết hàng
        private void KiemTraSanPhamHetHang(int maNguoiDung)
        {
            try
            {
                // Lấy thông tin người bán
                var nguoiBan = db.NguoiBans.FirstOrDefault(n => n.MaNguoiDung == maNguoiDung);
                if (nguoiBan == null)
                {
                    return;
                }

                // Lấy danh sách sản phẩm đã hết hàng
                var sanPhamHetHang = db.SanPhams
                    .Where(s => s.MaNguoiBan == nguoiBan.MaNguoiBan && s.SoLuongTonKho == 0 && s.TrangThai == "Đã phê duyệt")
                    .ToList();

                // Lấy danh sách sản phẩm sắp hết hàng
                var sanPhamGanHet = db.SanPhams
                    .Where(s => s.MaNguoiBan == nguoiBan.MaNguoiBan && s.SoLuongTonKho > 0 && s.SoLuongTonKho <= 5 && s.TrangThai == "Đã phê duyệt")
                    .ToList();

                // Tạo thông báo cho sản phẩm hết hàng
                foreach (var sanPham in sanPhamHetHang)
                {
                    // Kiểm tra xem đã gửi thông báo cho sản phẩm này trong 24h qua chưa
                    // Tính toán thời gian trước khi sử dụng trong truy vấn
                    DateTime ngayHomQua = DateTime.Now.AddHours(-24);

                    // Sử dụng biến trong truy vấn
                    var daGuiThongBao = db.ThongBaos
                        .Any(tb => tb.MaNguoiDung == maNguoiDung
                                && tb.LoaiThongBao == "SanPham"
                                && tb.TieuDe.Contains("hết hàng")
                                && tb.TinNhan.Contains(sanPham.MaSanPham.ToString())
                                && tb.NgayTao >= ngayHomQua);

                    if (!daGuiThongBao)
                    {
                        var thongBao = new ThongBao
                        {
                            MaNguoiDung = maNguoiDung,
                            LoaiThongBao = "SanPham",
                            TieuDe = "Sản phẩm hết hàng",
                            TinNhan = $"Sản phẩm {sanPham.TenSanPham} (Mã: {sanPham.MaSanPham}) đã hết hàng. Vui lòng cập nhật số lượng để tiếp tục bán hàng.",
                            MucDoQuanTrong = 2, // Quan trọng
                            DuongDanChiTiet = $"/NguoiBans/QuanLySanPham/{nguoiBan.MaNguoiBan}",
                            NgayTao = DateTime.Now,
                            TrangThai = "Chưa đọc"
                        };

                        db.ThongBaos.Add(thongBao);
                    }
                }

                // Tạo thông báo cho sản phẩm sắp hết hàng
                foreach (var sanPham in sanPhamGanHet)
                {
                    // Kiểm tra xem đã gửi thông báo cho sản phẩm này trong 24h qua chưa
                    // Tính toán thời gian trước khi sử dụng trong truy vấn
                    DateTime ngayHomQua = DateTime.Now.AddHours(-24);

                    // Sử dụng biến trong truy vấn
                    var daGuiThongBao = db.ThongBaos
                        .Any(tb => tb.MaNguoiDung == maNguoiDung
                                && tb.LoaiThongBao == "SanPham"
                                && tb.TieuDe.Contains("hết hàng")
                                && tb.TinNhan.Contains(sanPham.MaSanPham.ToString())
                                && tb.NgayTao >= ngayHomQua);

                    if (!daGuiThongBao)
                    {
                        var thongBao = new ThongBao
                        {
                            MaNguoiDung = maNguoiDung,
                            LoaiThongBao = "SanPham",
                            TieuDe = "Sản phẩm sắp hết hàng",
                            TinNhan = $"Sản phẩm {sanPham.TenSanPham} (Mã: {sanPham.MaSanPham}) chỉ còn {sanPham.SoLuongTonKho} sản phẩm. Vui lòng cập nhật số lượng kịp thời.",
                            MucDoQuanTrong = 1, // Thông thường
                            DuongDanChiTiet = $"/NguoiBans/QuanLySanPham/{nguoiBan.MaNguoiBan}",
                            NgayTao = DateTime.Now,
                            TrangThai = "Chưa đọc"
                        };

                        db.ThongBaos.Add(thongBao);
                    }
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                // Log lỗi
                System.Diagnostics.Debug.WriteLine("Lỗi kiểm tra hàng tồn kho: " + ex.Message);
            }
        }
        //21/4/2025
        // GET: DangXuat
        public ActionResult DangXuat()
        {
            // Lưu thời gian đăng nhập cuối cùng trước khi xóa session
            if (Session["MaNguoiDung"] != null)
            {
                int maNguoiDung = Convert.ToInt32(Session["MaNguoiDung"]);
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string updateQuery = "UPDATE NguoiDung SET DangNhapCuoiCung = @NgayGio WHERE MaNguoiDung = @MaNguoiDung";
                    SqlCommand updateCmd = new SqlCommand(updateQuery, con);
                    updateCmd.Parameters.AddWithValue("@NgayGio", DateTime.Now);
                    updateCmd.Parameters.AddWithValue("@MaNguoiDung", maNguoiDung);
                    updateCmd.ExecuteNonQuery();
                }
            }

            System.Web.Security.FormsAuthentication.SignOut();
            Session.Clear();
            Session.Abandon();
            // Xóa cookie xác thực
            if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
            {
                HttpCookie cookie = new HttpCookie(FormsAuthentication.FormsCookieName);
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }

            // Xóa cookie Session ID
            if (Request.Cookies["ASP.NET_SessionId"] != null)
            {
                HttpCookie cookie = new HttpCookie("ASP.NET_SessionId");
                cookie.Expires = DateTime.Now.AddDays(-1);
                Response.Cookies.Add(cookie);
            }

            // Thiết lập cache headers để ngăn lưu trữ trang trong bộ đệm
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
            Response.Cache.SetNoStore();
            return RedirectToAction("Index", "Home");
        }


        //21/4/2025
        [HttpPost]
        public ActionResult ClearCheckStockSession()
        {
            Session["CheckStock"] = false;
            return Json(new { success = true });
        }
        //21/4/2025


        //23/4/2025
        // Action GET cho trang đăng nhập
        public ActionResult DangNhap(string returnUrl, string action = "", string productId = "", string quantity = "")
        {
            // Lưu thông tin về hành động vào ViewBag để có thể sử dụng nếu cần
            ViewBag.ReturnUrl = returnUrl;
            ViewBag.Action = action;
            ViewBag.ProductId = productId;
            ViewBag.Quantity = quantity;

            // Nếu người dùng đã đăng nhập, chuyển họ đến trang sau đăng nhập
            if (Request.IsAuthenticated)
            {
                return RedirectToAction("AfterLogin", new
                {
                    returnUrl = returnUrl,
                    action = action,
                    productId = productId,
                    quantity = quantity
                });
            }

            return View();
        }

        // Action POST cho trang đăng nhập
        [HttpPost]
        public ActionResult DangNhap(DangNhapViewModel model, string returnUrl = "", string action = "", string productId = "", string quantity = "")
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Lấy kết nối đến database
            MedinetDATN db = new MedinetDATN();

            // Kiểm tra thông tin đăng nhập trong bảng NguoiDung
            var nguoiDung = db.NguoiDungs.FirstOrDefault(u => u.Email == model.Email);

            if (nguoiDung != null)
            {
                // Mã hóa mật khẩu người dùng nhập để so sánh với mật khẩu đã mã hóa trong DB
                string matKhauMaHoa = HashPassword(model.Password);

                // Kiểm tra mật khẩu đã mã hóa
                bool passwordCorrect = matKhauMaHoa == nguoiDung.MatKhauMaHoa;

                if (passwordCorrect)
                {
                    // Kiểm tra trạng thái tài khoản trước khi cho đăng nhập
                    if (nguoiDung.TrangThai != "Active" && nguoiDung.TrangThai != "Upgrade")
                    {
                        // Tài khoản bị khóa hoặc không hoạt động
                        string errorMessage = "";
                        switch (nguoiDung.TrangThai)
                        {
                            case "Inactive":
                                errorMessage = "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ quản trị viên.";
                                break;
                            case "Banned":
                                errorMessage = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.";
                                break;
                            default:
                                errorMessage = "Tài khoản của bạn không thể đăng nhập. Vui lòng liên hệ quản trị viên.";
                                break;
                        }
                        ModelState.AddModelError("", errorMessage);
                        return View(model);
                    }
                    // Tạo cookie xác thực
                    FormsAuthentication.SetAuthCookie(model.Email, model.RememberMe);

                    // Cập nhật Session
                    Session["UserName"] = nguoiDung.TenNguoiDung;
                    Session["UserID"] = nguoiDung.MaNguoiDung;
                    Session["MaNguoiDung"] = nguoiDung.MaNguoiDung;
                    Session["TenNguoiDung"] = nguoiDung.TenNguoiDung;
                    Session["VaiTro"] = nguoiDung.VaiTro;
                    Session["AnhDaiDien"] = !string.IsNullOrEmpty(nguoiDung.AnhDaiDien) ?
                        nguoiDung.AnhDaiDien : "~/Content/Images/default-avatar.png";

                    // Tính số lượng sản phẩm trong giỏ hàng
                    var soLuongGioHang = db.GioHangs.Count(g => g.MaNguoiDung == nguoiDung.MaNguoiDung);
                    Session["SoLuongGioHang"] = soLuongGioHang;

                    if (nguoiDung.VaiTro == "Seller")
                    {
                        Session["CheckStock"] = true;
                        KiemTraSanPhamHetHang(nguoiDung.MaNguoiDung);

                        // Đối với Seller, chuyển hướng đến trang quản lý sản phẩm sau khi đăng nhập
                        return RedirectToAction("EditSellerProfile", "NguoiDungs", new { id = nguoiDung.MaNguoiDung });
                    }
                    else if (nguoiDung.VaiTro == "Admin")
                    {
                        // Đối với Admin, chuyển hướng đến trang dashboard sau khi đăng nhập
                        return RedirectToAction("Dashboard", "Admin");
                    }
                    else
                    {
                        // Chỉ áp dụng luồng mua hàng đặc biệt cho Buyer
                        return RedirectToAction("AfterLogin", new
                        {
                            returnUrl = returnUrl,
                            action = action,
                            productId = productId,
                            quantity = quantity
                        });
                    }
                }
            }

            // Xử lý đăng nhập không thành công
            ModelState.AddModelError("", "Email hoặc mật khẩu không đúng.");
            return View(model);
        }

        // Action xử lý sau khi đăng nhập thành công
        public ActionResult AfterLogin(string returnUrl, string action = "", string productId = "", string quantity = "")
        {
            try
            {
                // Kiểm tra xem có hành động cụ thể cần thực hiện không
                if (!string.IsNullOrEmpty(action) && !string.IsNullOrEmpty(productId))
                {
                    int maSanPham;
                    int soLuong = 1; // Mặc định

                    // Chuyển đổi productId thành số nguyên
                    if (int.TryParse(productId, out maSanPham))
                    {
                        // Chuyển đổi quantity thành số nguyên nếu có
                        if (!string.IsNullOrEmpty(quantity))
                        {
                            int.TryParse(quantity, out soLuong);
                        }

                        // Xử lý các hành động khác nhau
                        if (action.Equals("addToCart", StringComparison.OrdinalIgnoreCase))
                        {
                            // Thêm vào giỏ hàng và chuyển hướng về trang trước đó
                            return RedirectToAction("ProcessAddToCart", new
                            {
                                maSanPham = maSanPham,
                                soLuong = soLuong,
                                returnUrl = returnUrl
                            });
                        }
                        else if (action.Equals("buyNow", StringComparison.OrdinalIgnoreCase))
                        {
                            // Mua ngay và chuyển hướng đến trang thanh toán
                            return RedirectToAction("ProcessBuyNow", new
                            {
                                maSanPham = maSanPham,
                                soLuong = soLuong
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi nếu cần
                System.Diagnostics.Debug.WriteLine("Lỗi trong AfterLogin: " + ex.Message);

                // Thêm thông báo lỗi vào TempData
                TempData["ErrorMessage"] = "Đã xảy ra lỗi khi xử lý. Vui lòng thử lại.";
            }

            // Nếu không có hành động đặc biệt hoặc có lỗi, quay lại trang trước đó
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Mặc định là quay về trang chủ
            return RedirectToAction("Index", "Home");
        }

        // Action xử lý thêm vào giỏ hàng sau đăng nhập
        public ActionResult ProcessAddToCart(int maSanPham, int soLuong, string returnUrl)
        {
            try
            {
                // Kiểm tra xem người dùng đã đăng nhập chưa
                if (!Request.IsAuthenticated)
                {
                    TempData["ErrorMessage"] = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng";
                    return RedirectToAction("DangNhap", new { returnUrl = returnUrl });
                }

                // Tạo đối tượng HomeController mới
                var homeController = DependencyResolver.Current.GetService<HomeController>() ?? new HomeController();

                // Sao chép ControllerContext để có thể truy cập Session, User, v.v.
                homeController.ControllerContext = new ControllerContext(this.Request.RequestContext, homeController);

                // Gọi action ThemVaoGio từ HomeController
                var result = homeController.ThemVaoGio(maSanPham, soLuong);

                // Lưu thông báo thành công vào TempData để hiển thị
                TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng";

                // Quay lại trang trước đó
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine("Lỗi trong ProcessAddToCart: " + ex.Message);

                // Xử lý lỗi
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi thêm sản phẩm vào giỏ hàng";

                // Quay lại trang trước đó nếu có
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
        }

        // Action xử lý mua ngay sau đăng nhập
        public ActionResult ProcessBuyNow(int maSanPham, int soLuong)
        {
            try
            {
                // Kiểm tra xem người dùng đã đăng nhập chưa
                if (!Request.IsAuthenticated)
                {
                    TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiếp tục mua hàng";
                    return RedirectToAction("DangNhap");
                }

                // Tạo đối tượng ThanhToanController mới
                var thanhToanController = DependencyResolver.Current.GetService<ThanhToanController>() ?? new ThanhToanController();

                // Sao chép ControllerContext để có thể truy cập Session, User, v.v.
                thanhToanController.ControllerContext = new ControllerContext(this.Request.RequestContext, thanhToanController);

                // Gọi MuaNgay từ ThanhToanController
                var result = thanhToanController.MuaNgay(maSanPham, soLuong);

                // Xử lý kết quả từ MuaNgay
                var jsonResult = result as JsonResult;
                if (jsonResult != null)
                {
                    try
                    {
                        // Lấy data từ JsonResult
                        dynamic jsonData = jsonResult.Data;
                        if (jsonData != null && jsonData.success == true && jsonData.redirectUrl != null)
                        {
                            // Chuyển hướng đến URL từ kết quả
                            return Redirect(jsonData.redirectUrl.ToString());
                        }
                    }
                    catch
                    {
                        // Xử lý lỗi khi truy cập dữ liệu JSON
                        TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý dữ liệu";
                    }
                }

                // Mặc định là chuyển đến trang giỏ hàng
                return RedirectToAction("GioHang", "Home");
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine("Lỗi trong ProcessBuyNow: " + ex.Message);

                // Xử lý lỗi
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý mua ngay";
                return RedirectToAction("Index", "Home");
            }
        }
        //23/4/2025
        public NguoiDung GetUserById(int MaNguoiDung)
        {
            NguoiDung user = null;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT
                        *
                    FROM
                        NguoiDung
                    WHERE
                        MaNguoiDung = @MaNguoiDung";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@MaNguoiDung", MaNguoiDung);

                con.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        user = new NguoiDung()
                        {
                            TenNguoiDung = (string)reader["first_name"],
                            Email = (string)reader["email"],
                        };
                    }
                }
            }
            return user;
        }

        //23/4/2025
        // Định nghĩa DangNhapViewModel ngay trong controller
        public class DangNhapViewModel
        {
            [Required(ErrorMessage = "Vui lòng nhập email")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; }

            [Display(Name = "Nhớ đăng nhập")]
            public bool RememberMe { get; set; }
        }
        //23/4/2025
    }
}