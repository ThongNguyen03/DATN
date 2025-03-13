using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class QuenMatKhauController : Controller
    {
        // Database connection string
        private string connectionString = "Data Source=DESKTOP-C6TH3H0;Initial Catalog=MediNet;Integrated Security=True";

        // GET: QuenMatKhau
        public ActionResult Index()
        {
            return View();
        }

        // POST: QuenMatKhau
        [HttpPost]
        public ActionResult Index(string TenNguoiDung, string Email, string SoDienThoai)
        {
            // Kiểm tra input
            if (string.IsNullOrWhiteSpace(TenNguoiDung) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(SoDienThoai))
            {
                ViewBag.Message = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            // Kiểm tra xem thông tin có tồn tại và khớp trong hệ thống không
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT
                        *
                    FROM
                        NguoiDung
                    WHERE
                        TenNguoiDung = @TenNguoiDung
                        AND Email = @Email
                        AND SoDienThoai = @SoDienThoai
                        AND TrangThai IN ('Active', 'Upgrade')";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@TenNguoiDung", TenNguoiDung);
                cmd.Parameters.AddWithValue("@Email", Email);
                cmd.Parameters.AddWithValue("@SoDienThoai", SoDienThoai);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    // Thông tin khớp, tạo mã xác nhận và thời gian hết hạn
                    string resetCode = GenerateResetCode();
                    DateTime expiryTime = DateTime.Now.AddHours(1); // Mã xác nhận có hiệu lực trong 1 giờ

                    reader.Close();

                    // Lưu mã xác nhận vào database
                    string updateQuery = @"
                        UPDATE NguoiDung 
                        SET MaXacNhan = @MaXacNhan, 
                            ThoiGianHetHan = @ThoiGianHetHan 
                        WHERE Email = @Email";

                    SqlCommand updateCmd = new SqlCommand(updateQuery, con);
                    updateCmd.Parameters.AddWithValue("@MaXacNhan", resetCode);
                    updateCmd.Parameters.AddWithValue("@ThoiGianHetHan", expiryTime);
                    updateCmd.Parameters.AddWithValue("@Email", Email);
                    updateCmd.ExecuteNonQuery();

                    // Gửi email
                    if (SendResetEmail(Email, resetCode))
                    {
                        // Chuyển hướng đến trang nhập mã xác nhận
                        Session["ResetEmail"] = Email;
                        return RedirectToAction("NhapMaXacNhan");
                    }
                    else
                    {
                        ViewBag.Message = "Có lỗi xảy ra khi gửi email. Vui lòng thử lại sau.";
                        return View();
                    }
                }
                else
                {
                    ViewBag.Message = "Thông tin không chính xác hoặc không tồn tại trong hệ thống";
                    return View();
                }
            }
        }

        // GET: QuenMatKhau/NhapMaXacNhan
        public ActionResult NhapMaXacNhan()
        {
            // Kiểm tra xem đã có email trong session chưa
            if (Session["ResetEmail"] == null)
            {
                return RedirectToAction("Index");
            }

            // Trong chế độ phát triển, hiển thị lại mã xác nhận
            if (TempData["DevMode"] != null && TempData["DevMode"].ToString() == "True" && TempData["ResetCode"] != null)
            {
                // Giữ lại TempData để có thể hiển thị
                TempData.Keep("DevMode");
                TempData.Keep("ResetCode");
            }

            return View();
        }

        // POST: QuenMatKhau/NhapMaXacNhan
        [HttpPost]
        public ActionResult NhapMaXacNhan(string MaXacNhan)
        {
            // Kiểm tra xem đã có email trong session chưa
            if (Session["ResetEmail"] == null)
            {
                return RedirectToAction("Index");
            }

            string email = Session["ResetEmail"].ToString();

            // Kiểm tra mã xác nhận
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT
                        *
                    FROM
                        NguoiDung
                    WHERE
                        Email = @Email
                        AND MaXacNhan = @MaXacNhan
                        AND ThoiGianHetHan > @ThoiGianHienTai";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.AddWithValue("@MaXacNhan", MaXacNhan);
                cmd.Parameters.AddWithValue("@ThoiGianHienTai", DateTime.Now);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    // Mã xác nhận hợp lệ, chuyển hướng đến trang đặt lại mật khẩu
                    reader.Close();
                    return RedirectToAction("DatLaiMatKhau");
                }
                else
                {
                    // Mã xác nhận không hợp lệ hoặc đã hết hạn
                    ViewBag.Message = "Mã xác nhận không hợp lệ hoặc đã hết hạn";

                    // Trong chế độ phát triển, hiển thị lại mã xác nhận để thuận tiện cho việc test
                    if (TempData["DevMode"] != null && TempData["DevMode"].ToString() == "True" && TempData["ResetCode"] != null)
                    {
                        TempData.Keep("DevMode");
                        TempData.Keep("ResetCode");
                    }

                    return View();
                }
            }
        }

        // GET: QuenMatKhau/DatLaiMatKhau
        public ActionResult DatLaiMatKhau()
        {
            // Kiểm tra xem đã có email trong session chưa
            if (Session["ResetEmail"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        // POST: QuenMatKhau/DatLaiMatKhau
        [HttpPost]
        public ActionResult DatLaiMatKhau(string MatKhauMoi, string XacNhanMatKhau)
        {
            // Kiểm tra xem đã có email trong session chưa
            if (Session["ResetEmail"] == null)
            {
                return RedirectToAction("Index");
            }

            // Kiểm tra mật khẩu
            if (string.IsNullOrWhiteSpace(MatKhauMoi) || string.IsNullOrWhiteSpace(XacNhanMatKhau))
            {
                ViewBag.Message = "Vui lòng nhập đầy đủ thông tin";
                return View();
            }

            if (MatKhauMoi != XacNhanMatKhau)
            {
                ViewBag.Message = "Mật khẩu xác nhận không khớp";
                return View();
            }

            // Kiểm tra độ phức tạp của mật khẩu
            if (!IsValidPassword(MatKhauMoi))
            {
                ViewBag.Message = "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường và số";
                return View();
            }

            string email = Session["ResetEmail"].ToString();
            string matKhauMaHoa = HashPassword(MatKhauMoi);

            // Cập nhật mật khẩu mới
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string updateQuery = @"
                    UPDATE NguoiDung 
                    SET MatKhauMaHoa = @MatKhauMaHoa,
                        MaXacNhan = NULL,
                        ThoiGianHetHan = NULL
                    WHERE Email = @Email";

                SqlCommand updateCmd = new SqlCommand(updateQuery, con);
                updateCmd.Parameters.AddWithValue("@MatKhauMaHoa", matKhauMaHoa);
                updateCmd.Parameters.AddWithValue("@Email", email);
                con.Open();
                int result = updateCmd.ExecuteNonQuery();

                if (result > 0)
                {
                    // Xóa session và chuyển hướng đến trang thành công
                    Session.Remove("ResetEmail");
                    return RedirectToAction("ThanhCong");
                }
                else
                {
                    ViewBag.Message = "Có lỗi xảy ra khi cập nhật mật khẩu";
                    return View();
                }
            }
        }

        // GET: QuenMatKhau/ThanhCong
        public ActionResult ThanhCong()
        {
            return View();
        }

        // Hàm mã hóa mật khẩu (giống với của DangNhapController)
        private string HashPassword(string MatKhau)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(MatKhau);
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        // Hàm tạo mã xác nhận ngẫu nhiên
        private string GenerateResetCode()
        {
            // Tạo mã ngẫu nhiên 6 chữ số
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        // Hàm gửi email đặt lại mật khẩu (phiên bản phát triển - không gửi email thật)
        private bool SendResetEmail(string email, string resetCode)
        {
            try
            {
                // Log mã xác nhận để dễ kiểm tra trong quá trình phát triển
                System.Diagnostics.Debug.WriteLine($"[DEV MODE] Reset code for {email}: {resetCode}");

                // Lưu mã xác nhận vào TempData để hiển thị trên giao diện
                TempData["ResetCode"] = resetCode;
                TempData["DevMode"] = true;

                // Luôn trả về true - giả lập gửi email thành công
                return true;
            }
            catch (Exception ex)
            {
                // Log lỗi
                System.Diagnostics.Debug.WriteLine("Error in SendResetEmail: " + ex.Message);
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine("Inner exception: " + ex.InnerException.Message);
                }

                // Vẫn lưu mã xác nhận để có thể hiển thị
                TempData["ResetCode"] = resetCode;
                TempData["DevMode"] = true;

                // Vẫn trả về true để tiếp tục quy trình
                return true;
            }
        }

        // Hàm kiểm tra mật khẩu hợp lệ (8 ký tự, có hoa, thường và số)
        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            bool hasUpperCase = false;
            bool hasLowerCase = false;
            bool hasDigit = false;

            foreach (char c in password)
            {
                if (char.IsUpper(c))
                    hasUpperCase = true;
                else if (char.IsLower(c))
                    hasLowerCase = true;
                else if (char.IsDigit(c))
                    hasDigit = true;
            }

            return hasUpperCase && hasLowerCase && hasDigit;
        }
    }
}