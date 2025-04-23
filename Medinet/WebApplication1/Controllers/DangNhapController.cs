using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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
        // GET: DangNhap
        public ActionResult DangNhap()
        {
            return View();
        }
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
        //, bool rememberMe
        public ActionResult DangNhap(string Email, string password, string returnUrl)
        {
            // Kiểm tra input
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.MessageLogin = "Email và mật khẩu không được để trống";
                return View();
            }
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                if (!string.IsNullOrEmpty(password))
                {
                    string MatKhauMaHoa = HashPassword(password);
                    string query = @"
                    SELECT
                            *
                        FROM
                            NguoiDung
                        WHERE
                            Email = @Email
                            AND MatKhauMaHoa = @MatKhauMaHoa
                            AND TrangThai IN ('Active', 'Upgrade')";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Email", Email);
                    cmd.Parameters.AddWithValue("@MatKhauMaHoa", MatKhauMaHoa);

                    int maNguoiDung = 0;
                    string tenNguoiDung = string.Empty;
                    string vaiTro = string.Empty;
                    string anhDaiDien = string.Empty;
                    bool isAuthenticated = false;

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isAuthenticated = true;
                            maNguoiDung = Convert.ToInt32(reader["MaNguoiDung"]);
                            tenNguoiDung = reader["TenNguoiDung"].ToString();
                            vaiTro = reader["VaiTro"].ToString();
                            anhDaiDien = reader["AnhDaiDien"] != DBNull.Value ? reader["AnhDaiDien"].ToString() : "~/Content/Images/default-avatar.png";
                        }
                    }

                    if (isAuthenticated)
                    {
                        // Lưu thông tin vào session
                        Session["MaNguoiDung"] = maNguoiDung;
                        Session["TenNguoiDung"] = tenNguoiDung;
                        Session["VaiTro"] = vaiTro;
                        Session["AnhDaiDien"] = anhDaiDien;

                        // Tính số lượng sản phẩm trong giỏ hàng
                        string cartQuery = @"
                            SELECT COUNT(*) as SoLoaiSanPham
                            FROM GioHang
                            WHERE MaNguoiDung = @MaNguoiDung";

                        SqlCommand cartCmd = new SqlCommand(cartQuery, con);
                        cartCmd.Parameters.AddWithValue("@MaNguoiDung", maNguoiDung);
                        object result = cartCmd.ExecuteScalar();

                        // Lưu số lượng sản phẩm vào session
                        Session["SoLuongGioHang"] = Convert.ToInt32(result);

                        // Thiết lập cookie xác thực để Forms Authentication hoạt động
                        System.Web.Security.FormsAuthentication.SetAuthCookie(Email, false);

                        //21/4/2025
                        // Thiết lập session để check stock nếu người dùng là người bán
                        if (vaiTro == "Seller")
                        {
                            Session["CheckStock"] = true;

                            // Kiểm tra sản phẩm hết hàng ngay khi đăng nhập
                            KiemTraSanPhamHetHang(maNguoiDung);
                        }
                        //21/4/2025
                        // Xử lý chuyển hướng sau đăng nhập
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            // Chuyển hướng theo vai trò nếu không có returnUrl
                            switch (vaiTro)
                            {
                                case "Admin":
                                    return RedirectToAction("Dashboard", "Admin");
                                case "Seller":
                                    return RedirectToAction("EditSellerProfile", "NguoiDungs");
                                case "Buyer":
                                    return RedirectToAction("Index", "Home");
                                default:
                                    return RedirectToAction("Index", "Home");
                            }
                        }
                    }
                    else
                    {
                        ViewBag.MessageLogin = "Thông tin đăng nhập không hợp lệ";
                    }

                    return View();
                }
                else
                {
                    // Xử lý trường hợp MatKhau null (ví dụ: thông báo lỗi cho người dùng)
                    ModelState.AddModelError("MatKhau", "Mật khẩu không được để trống.");
                    return RedirectToAction("DangKy", "DangKy"); // Trả về view đăng ký để người dùng nhập lại
                }
            }
        }

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
    }
}