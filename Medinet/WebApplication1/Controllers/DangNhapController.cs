using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
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
                                    return RedirectToAction("UserManagement", "Admin");
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
            return RedirectToAction("DangNhap", "DangNhap");
        }

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