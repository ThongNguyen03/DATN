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

        //16/4/2025
        // POST: QuenMatKhau
        [HttpPost]
        public ActionResult Index(string Email)
        {
            // Kiểm tra input
            if (string.IsNullOrWhiteSpace(Email))
            {
                ViewBag.Message = "Vui lòng nhập email";
                return View();
            }

            // Kiểm tra xem email có tồn tại trong hệ thống không
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"
            SELECT
                *
            FROM
                NguoiDung
            WHERE
                Email = @Email
                AND TrangThai IN ('Active', 'Upgrade')";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Email", Email);
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    // Email tồn tại, tạo mã xác nhận và thời gian hết hạn
                    string resetCode = GenerateResetCode();
                    DateTime expiryTime = DateTime.Now.AddMinutes(3); // Mã xác nhận có hiệu lực trong 3 phút

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
                        // Lưu email vào session và hiển thị thông báo thành công
                        Session["ResetEmail"] = Email;
                        ViewBag.Success = "Mã xác nhận đã được gửi đến email của bạn. Vui lòng kiểm tra hộp thư đến hoặc thư rác.";
                        return View();
                    }
                    else
                    {
                        ViewBag.Message = "Có lỗi xảy ra khi gửi email. Vui lòng thử lại sau.";
                        return View();
                    }
                }
                else
                {
                    ViewBag.Message = "Email không tồn tại trong hệ thống hoặc tài khoản đã bị khóa";
                    return View();
                }
            }
        }

        public ActionResult NhapMaXacNhan()
        {
            // Kiểm tra xem đã có email trong session chưa
            if (Session["ResetEmail"] == null)
            {
                return RedirectToAction("Index");
            }

            string email = Session["ResetEmail"].ToString();

            try
            {
                // Chỉ lấy thời gian hết hạn từ database, KHÔNG đặt lại
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = @"
                SELECT
                    ThoiGianHetHan
                FROM
                    NguoiDung
                WHERE
                    Email = @Email";

                    SqlCommand cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@Email", email);
                    con.Open();
                    var result = cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        DateTime expiryTime = (DateTime)result;
                        // Chuyển đổi sang chuỗi ISO 8601 chuẩn
                        ViewBag.ExpiryTime = expiryTime.ToString("yyyy-MM-ddTHH:mm:ss");
                    }
                    // Không thiết lập thời gian hết hạn mặc định - nếu không có, thì mã đã hết hạn
                }
            }
            catch (Exception ex)
            {
                // Log lỗi
                System.Diagnostics.Debug.WriteLine("Error getting expiry time: " + ex.Message);
            }

            return View();
        }

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
                // Trước tiên, lấy thời gian hết hạn hiện tại (để hiển thị đếm ngược)
                string getExpiryQuery = "SELECT ThoiGianHetHan FROM NguoiDung WHERE Email = @Email";
                SqlCommand getExpiryCmd = new SqlCommand(getExpiryQuery, con);
                getExpiryCmd.Parameters.AddWithValue("@Email", email);
                con.Open();

                // Lấy thời gian hết hạn
                var expiryTimeResult = getExpiryCmd.ExecuteScalar();
                if (expiryTimeResult != null && expiryTimeResult != DBNull.Value)
                {
                    DateTime expiryTime = (DateTime)expiryTimeResult;
                    ViewBag.ExpiryTime = expiryTime.ToString("yyyy-MM-ddTHH:mm:ss");
                }

                // Kiểm tra mã xác nhận
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
                    return View();
                }
            }
        }
        //16/4/2025
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
                    string getUserQuery = "SELECT MaNguoiDung FROM NguoiDung WHERE Email = @Email";
                    SqlCommand getUserCmd = new SqlCommand(getUserQuery, con);
                    getUserCmd.Parameters.AddWithValue("@Email", email);
                    int userId = (int)getUserCmd.ExecuteScalar();
                    // Thêm thông báo về việc đổi mật khẩu thành công
                    string insertNotificationQuery = @"
                        INSERT INTO ThongBao (MaNguoiDung, LoaiThongBao, TieuDe, TinNhan, TrangThai, NgayTao, MucDoQuanTrong, DuongDanChiTiet)
                        VALUES (@MaNguoiDung, @LoaiThongBao, @TieuDe, @TinNhan, @TrangThai, @NgayTao, @MucDoQuanTrong, @DuongDanChiTiet)";

                    SqlCommand notificationCmd = new SqlCommand(insertNotificationQuery, con);
                    notificationCmd.Parameters.AddWithValue("@MaNguoiDung", userId);
                    notificationCmd.Parameters.AddWithValue("@LoaiThongBao", "TaiKhoan");
                    notificationCmd.Parameters.AddWithValue("@TieuDe", "Đổi mật khẩu thành công");
                    notificationCmd.Parameters.AddWithValue("@TinNhan", "Mật khẩu của bạn đã được thay đổi thành công. Nếu không phải bạn thực hiện, vui lòng liên hệ với chúng tôi ngay.");
                    notificationCmd.Parameters.AddWithValue("@TrangThai", "Chưa đọc");
                    notificationCmd.Parameters.AddWithValue("@NgayTao", DateTime.Now);
                    notificationCmd.Parameters.AddWithValue("@MucDoQuanTrong", 1);
                    notificationCmd.Parameters.AddWithValue("@DuongDanChiTiet", "/NguoiDungs/Profile");

                    notificationCmd.ExecuteNonQuery();
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

        //16/4/2025
        private bool SendResetEmail(string email, string resetCode)
        {
            try
            {
                // Lấy thông tin cấu hình SMTP từ Web.config
                string smtpHost = System.Configuration.ConfigurationManager.AppSettings["SmtpHost"];
                int smtpPort = int.Parse(System.Configuration.ConfigurationManager.AppSettings["SmtpPort"]);
                string smtpEmail = System.Configuration.ConfigurationManager.AppSettings["SmtpEmail"];
                string smtpPassword = System.Configuration.ConfigurationManager.AppSettings["SmtpPassword"];
                bool enableSSL = bool.Parse(System.Configuration.ConfigurationManager.AppSettings["SmtpEnableSSL"]);
                string emailFromName = System.Configuration.ConfigurationManager.AppSettings["EmailFromName"];

                // Tạo đối tượng SmtpClient
                SmtpClient smtpClient = new SmtpClient(smtpHost, smtpPort);
                smtpClient.EnableSsl = enableSSL;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = new NetworkCredential(smtpEmail, smtpPassword);

                // Tạo email
                MailMessage mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(smtpEmail, emailFromName);
                mailMessage.To.Add(email);
                mailMessage.Subject = "Mã xác nhận đặt lại mật khẩu MediNet";

                // Tạo nội dung email sử dụng HTML
                string emailBody = $@"
        <html>
        <head>
            <style>
                body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                .container {{ max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 5px; }}
                .header {{ background-color: #0066cc; color: white; padding: 10px; text-align: center; border-radius: 5px 5px 0 0; }}
                .content {{ padding: 20px; }}
                .code {{ font-size: 24px; font-weight: bold; text-align: center; padding: 15px; margin: 20px 0; background-color: #f5f5f5; border-radius: 5px; letter-spacing: 5px; }}
                .footer {{ text-align: center; padding-top: 20px; font-size: 12px; color: #777; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <h2>MediNet - Đặt lại mật khẩu</h2>
                </div>
                <div class='content'>
                    <p>Kính gửi Quý khách,</p>
                    <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu tài khoản MediNet của bạn. Vui lòng sử dụng mã xác nhận dưới đây để hoàn tất quá trình:</p>
                    <div class='code'>{resetCode}</div>
                    <p>Mã xác nhận có hiệu lực trong vòng 60 phút. Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này.</p>
                    <p>Trân trọng,<br/>Đội ngũ MediNet</p>
                </div>
                <div class='footer'>
                    <p>Email này được gửi tự động, vui lòng không trả lời. Nếu bạn cần hỗ trợ, vui lòng liên hệ với chúng tôi qua hệ thống hỗ trợ của MediNet.</p>
                </div>
            </div>
        </body>
        </html>";

                mailMessage.Body = emailBody;
                mailMessage.IsBodyHtml = true;

                // Gửi email
                smtpClient.Send(mailMessage);

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

                return false;
            }
        }
        //16/4/2025

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