using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace WebApplication1.Controllers
{
    public class DangKyController : Controller
    {
        // Database connection string
        private string connectionString = "Data Source=DESKTOP-C6TH3H0;Initial Catalog=MediNet;Integrated Security=True";
        // GET: Đăng kí
        public ActionResult DangKi()
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
        //POST: Đăng kí
        public ActionResult DangKy(string HoTen, string Email, string MatKhau)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(HoTen))
            {
                ViewBag.MessageRegister = "Họ tên không được để trống";
                return View();
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                ViewBag.MessageRegister = "Email không được để trống";
                return View();
            }

            if (string.IsNullOrWhiteSpace(MatKhau))
            {
                ViewBag.MessageRegister = "Mật khẩu không được để trống";
                return View();
            }

            // Validate email format
            try
            {
                var addr = new System.Net.Mail.MailAddress(Email);
            }
            catch
            {
                ViewBag.MessageRegister = "Địa chỉ email không hợp lệ";
                return View();
            }
            // Validate password strength
            if (MatKhau.Length < 8 ||
                !MatKhau.Any(char.IsUpper) ||
                !MatKhau.Any(char.IsLower) ||
                !MatKhau.Any(char.IsDigit))
            {
                ViewBag.MessageRegister = "Mật khẩu phải dài ít nhất 8 ký tự và chứa ít nhất 1 chữ hoa, 1 chữ thường và 1 số";
                return View();
            }
            if (!string.IsNullOrEmpty(MatKhau))
            {
                string hash = HashPassword(MatKhau);
                DateTime today = DateTime.Now;
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string checkQuery = @"
                    SELECT
                        COUNT(*)
                    FROM
                        NguoiDung
                    WHERE
                        Email = @Email";

                    string insertQuery = @"
                    INSERT INTO
                        NguoiDung (TenNguoiDung, Email, MatKhauMaHoa,VaiTro,NgayTao)
                    VALUES
                        (@HoTen, @Email, @Hash, 'Buyer', @today)";

                    try
                    {
                        SqlCommand checkCmd = new SqlCommand(checkQuery, con);
                        checkCmd.Parameters.AddWithValue("@Email", Email);

                        con.Open();

                        int existingUserCount = (int)checkCmd.ExecuteScalar();
                        if (existingUserCount > 0)
                        {
                            ViewBag.MessageRegister = "Email đã tồn tại. Vui lòng thử lại.";
                            return View();
                        }

                        SqlCommand cmd = new SqlCommand(insertQuery, con);
                        cmd.Parameters.AddWithValue("@HoTen", HoTen);
                        cmd.Parameters.AddWithValue("@Email", Email);
                        cmd.Parameters.AddWithValue("@Hash", hash);
                        cmd.Parameters.AddWithValue("@today", today);

                        cmd.ExecuteNonQuery();

                        // Chuyển đến trang đăng nhập sau khi đăng ký thành công
                        TempData["RegistrationSuccess"] = "Đăng ký tài khoản thành công. Vui lòng đăng nhập.";
                        return RedirectToAction("DangNhap", "DangNhap");
                    }
                    catch (Exception ex)
                    {
                        ViewBag.MessageRegister = "Đã có lỗi xảy ra: " + ex.Message;
                        return View();
                    }
                    finally
                    {
                        con.Close();
                    }
                }
            }
            else
            {
                // Xử lý trường hợp MatKhau null (ví dụ: thông báo lỗi cho người dùng)
                ModelState.AddModelError("MatKhau", "Mật khẩu không được để trống.");
                return View(); // Trả về view đăng ký để người dùng nhập lại
            }
        }
    }
}