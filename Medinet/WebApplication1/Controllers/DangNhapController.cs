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
                    con.Open();
                    SqlDataReader reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        Session["MaNguoiDung"] = reader["MaNguoiDung"];
                        Session["TenNguoiDung"] = reader["TenNguoiDung"];
                        Session["VaiTro"] = reader["VaiTro"];
                        Session["AnhDaiDien"] = reader["AnhDaiDien"] ?? "~/Content/Images/default-avatar.png"; ;
                        // Xử lý ghi nhớ đăng nhập
                        //if (rememberMe)
                        //{
                        //    HttpCookie cookie = new HttpCookie("UserLogin");
                        //    cookie.Values["Email"] = email;
                        //    cookie.Expires = DateTime.Now.AddDays(30);
                        //    Response.Cookies.Add(cookie);
                        //}
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
                            string vaiTro = reader["VaiTro"].ToString();
                            switch (vaiTro)
                            {
                                case "Admin":
                                    return RedirectToAction("UserManagement", "Admin");
                                case "Seller":
                                    return RedirectToAction("EditSellerProfile", "NguoiDungs");
                                case "Buyer":
                                    return RedirectToAction("Index", "Home");
                                    //default:
                                    //    return RedirectToAction("Index", "Home");
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
            Session.Clear();
            return RedirectToAction("DangNhap","DangNhap");
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
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    user = new NguoiDung()
                    {
                        TenNguoiDung = (string)reader["first_name"],
                        Email = (string)reader["email"],
                    };
                }
                reader.Close();
            }
            return user;
        }
    }
}