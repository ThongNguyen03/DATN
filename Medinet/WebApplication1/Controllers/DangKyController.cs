//17/4/2025
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using WebApplication1.Services;
using System.Threading.Tasks;
using System.Configuration;

namespace WebApplication1.Controllers
{
    public class DangKyController : Controller
    {
        // Database connection string
        private string connectionString = "Data Source=DESKTOP-C6TH3H0;Initial Catalog=MediNet;Integrated Security=True";

        // Email service
        private EmailService _emailService;

        public DangKyController()
        {
            // Khởi tạo dịch vụ email
            _emailService = new EmailService();
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

        // Hàm tạo mã xác nhận ngẫu nhiên
        private string GenerateVerificationCode()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        [HttpGet]
        public ActionResult DangKy()
        {
            // Hiển thị form đăng ký
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> DangKy(string HoTen, string Email, string MatKhau)
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

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                // Kiểm tra email đã tồn tại chưa
                string checkQuery = "SELECT COUNT(*) FROM NguoiDung WHERE Email = @Email";

                try
                {
                    con.Open();
                    SqlCommand checkCmd = new SqlCommand(checkQuery, con);
                    checkCmd.Parameters.AddWithValue("@Email", Email);

                    int existingUserCount = (int)checkCmd.ExecuteScalar();
                    if (existingUserCount > 0)
                    {
                        ViewBag.MessageRegister = "Email đã tồn tại. Vui lòng thử lại.";
                        return View();
                    }

                    // Mã hóa mật khẩu
                    string hashedPassword = HashPassword(MatKhau);

                    // Tạo mã xác nhận
                    string verificationCode = GenerateVerificationCode();

                    // Thời gian hiện tại
                    DateTime now = DateTime.Now;

                    // Thời gian hết hạn (3 phút)
                    DateTime expiryTime = now.AddMinutes(3);

                    // Lưu thông tin xác nhận vào database
                    string insertVerificationQuery = @"
                    INSERT INTO XacNhanEmail (Email, MaCode, NgayTao, NgayHetHan, DaSuDung, HoTen, MatKhauMaHoa)
                    VALUES (@Email, @MaCode, @NgayTao, @NgayHetHan, 0, @HoTen, @MatKhauMaHoa)";

                    SqlCommand insertVerificationCmd = new SqlCommand(insertVerificationQuery, con);
                    insertVerificationCmd.Parameters.AddWithValue("@Email", Email);
                    insertVerificationCmd.Parameters.AddWithValue("@MaCode", verificationCode);
                    insertVerificationCmd.Parameters.AddWithValue("@NgayTao", now);
                    insertVerificationCmd.Parameters.AddWithValue("@NgayHetHan", expiryTime);
                    insertVerificationCmd.Parameters.AddWithValue("@HoTen", HoTen);
                    insertVerificationCmd.Parameters.AddWithValue("@MatKhauMaHoa", hashedPassword);

                    insertVerificationCmd.ExecuteNonQuery();

                    // Gửi email xác nhận
                    await _emailService.SendVerificationEmailAsync(Email, HoTen, verificationCode);

                    // Lưu thông tin đăng ký vào session để dùng ở bước xác nhận
                    Session["RegisterEmail"] = Email;
                    Session["ExpiryTime"] = expiryTime;
                    // Chuyển đến trang nhập mã xác nhận
                    return RedirectToAction("XacNhanEmail");
                }
                catch (Exception ex)
                {
                    ViewBag.MessageRegister = "Đã có lỗi xảy ra: " + ex.Message;
                    return View();
                }
            }
        }

        [HttpGet]
        public ActionResult XacNhanEmail()
        {
            // Kiểm tra xem có email đăng ký trong session không
            if (Session["RegisterEmail"] == null)
            {
                return RedirectToAction("DangKy");
            }

            // Hiển thị form nhập mã xác nhận
            ViewBag.Email = Session["RegisterEmail"];
            // Truyền thời gian hết hạn vào ViewBag
            if (Session["ExpiryTime"] != null)
            {
                DateTime expiryTime = (DateTime)Session["ExpiryTime"];
                TimeSpan remainingTime = expiryTime - DateTime.Now;
                ViewBag.RemainingSeconds = Math.Max(0, (int)remainingTime.TotalSeconds);
            }
            else
            {
                ViewBag.RemainingSeconds = 180; // Mặc định 3 phút
            }
            return View();
        }

        [HttpPost]
        public ActionResult XacNhanEmail(string VerificationCode)
        {
            // Lấy email từ session
            string email = Session["RegisterEmail"] as string;

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("DangKy");
            }

            // Kiểm tra thời gian hết hạn từ session
            bool isExpired = true;
            if (Session["ExpiryTime"] != null)
            {
                DateTime expiryTime = (DateTime)Session["ExpiryTime"];
                if (DateTime.Now <= expiryTime)
                {
                    isExpired = false;
                }
            }

            // Nếu đã hết hạn, không cho phép xác nhận
            if (isExpired)
            {
                ViewBag.Email = email;
                ViewBag.MessageVerification = "Mã xác nhận đã hết hạn. Vui lòng bấm 'Gửi lại mã' để nhận mã mới.";
                ViewBag.RemainingSeconds = 0;
                return View();
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(VerificationCode))
            {
                ViewBag.Email = email;
                ViewBag.MessageVerification = "Vui lòng nhập mã xác nhận";
                // Truyền lại thời gian còn lại
                if (Session["ExpiryTime"] != null)
                {
                    DateTime expiryTime = (DateTime)Session["ExpiryTime"];
                    TimeSpan remainingTime = expiryTime - DateTime.Now;

                    if (remainingTime.TotalSeconds > 0)
                    {
                        ViewBag.RemainingSeconds = (int)remainingTime.TotalSeconds;
                    }
                    else
                    {
                        ViewBag.RemainingSeconds = 0;
                    }
                }
                return View();
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                // Kiểm tra mã xác nhận
                string checkVerificationQuery = @"
                SELECT MaXacNhan, HoTen, MatKhauMaHoa, NgayHetHan, DaSuDung
                FROM XacNhanEmail 
                WHERE Email = @Email AND MaCode = @MaCode";

                SqlCommand checkVerificationCmd = new SqlCommand(checkVerificationQuery, con);
                checkVerificationCmd.Parameters.AddWithValue("@Email", email);
                checkVerificationCmd.Parameters.AddWithValue("@MaCode", VerificationCode);

                SqlDataReader reader = checkVerificationCmd.ExecuteReader();

                if (reader.Read())
                {
                    int maXacNhan = reader.GetInt32(0);
                    string hoTen = reader.GetString(1);
                    string matKhauMaHoa = reader.GetString(2);
                    DateTime ngayHetHan = reader.GetDateTime(3);
                    bool daSuDung = reader.GetBoolean(4);

                    reader.Close();

                    // Kiểm tra mã đã được sử dụng chưa
                    if (daSuDung)
                    {
                        ViewBag.Email = email;
                        ViewBag.MessageVerification = "Mã xác nhận đã được sử dụng";
                        return View();
                    }

                    // Kiểm tra mã còn hiệu lực không
                    if (ngayHetHan < DateTime.Now)
                    {
                        ViewBag.Email = email;
                        ViewBag.MessageVerification = "Mã xác nhận đã hết hạn";
                        return View();
                    }

                    // Đánh dấu mã đã sử dụng
                    string updateVerificationQuery = "UPDATE XacNhanEmail SET DaSuDung = 1 WHERE MaXacNhan = @MaXacNhan";
                    SqlCommand updateVerificationCmd = new SqlCommand(updateVerificationQuery, con);
                    updateVerificationCmd.Parameters.AddWithValue("@MaXacNhan", maXacNhan);
                    updateVerificationCmd.ExecuteNonQuery();

                    // Tạo tài khoản người dùng
                    DateTime today = DateTime.Now;
                    string insertUserQuery = @"
                    INSERT INTO NguoiDung (TenNguoiDung, Email, MatKhauMaHoa, VaiTro, NgayTao)
                    VALUES (@HoTen, @Email, @MatKhauMaHoa, 'Buyer', @NgayTao)";

                    SqlCommand insertUserCmd = new SqlCommand(insertUserQuery, con);
                    insertUserCmd.Parameters.AddWithValue("@HoTen", hoTen);
                    insertUserCmd.Parameters.AddWithValue("@Email", email);
                    insertUserCmd.Parameters.AddWithValue("@MatKhauMaHoa", matKhauMaHoa);
                    insertUserCmd.Parameters.AddWithValue("@NgayTao", today);
                    insertUserCmd.ExecuteNonQuery();

                    // Lấy ID của người dùng vừa tạo
                    string getIdQuery = "SELECT MaNguoiDung FROM NguoiDung WHERE Email = @Email";
                    SqlCommand getIdCmd = new SqlCommand(getIdQuery, con);
                    getIdCmd.Parameters.AddWithValue("@Email", email);
                    int newUserId = (int)getIdCmd.ExecuteScalar();

                    // Thêm thông báo cho người dùng mới
                    string insertNotificationQuery = @"
                        INSERT INTO ThongBao (MaNguoiDung, LoaiThongBao, TieuDe, TinNhan, TrangThai, NgayTao, MucDoQuanTrong, DuongDanChiTiet)
                        VALUES (@MaNguoiDung, @LoaiThongBao, @TieuDe, @TinNhan, @TrangThai, @NgayTao, @MucDoQuanTrong, @DuongDanChiTiet)";

                    SqlCommand notificationCmd = new SqlCommand(insertNotificationQuery, con);
                    notificationCmd.Parameters.AddWithValue("@MaNguoiDung", newUserId);
                    notificationCmd.Parameters.AddWithValue("@LoaiThongBao", "TaiKhoan");
                    notificationCmd.Parameters.AddWithValue("@TieuDe", "Đăng ký tài khoản thành công");
                    notificationCmd.Parameters.AddWithValue("@TinNhan", "Chào mừng bạn đến với MediNetPro! Tài khoản của bạn đã được tạo thành công.");
                    notificationCmd.Parameters.AddWithValue("@TrangThai", "Chưa đọc");
                    notificationCmd.Parameters.AddWithValue("@NgayTao", DateTime.Now);
                    notificationCmd.Parameters.AddWithValue("@MucDoQuanTrong", 1);
                    notificationCmd.Parameters.AddWithValue("@DuongDanChiTiet", "/NguoiDungs/Profile");
                    notificationCmd.ExecuteNonQuery();

                    // Xóa thông tin từ session
                    Session.Remove("RegisterEmail");

                    // Chuyển đến trang đăng nhập
                    TempData["RegistrationSuccess"] = "Đăng ký tài khoản thành công. Vui lòng đăng nhập.";
                    return RedirectToAction("DangNhap", "DangNhap");
                }
                else
                {
                    reader.Close();
                    // Nếu mã xác nhận không đúng
                    ViewBag.Email = email;
                    ViewBag.MessageVerification = "Mã xác nhận không chính xác";

                    // Truyền lại thời gian còn lại
                    if (Session["ExpiryTime"] != null)
                    {
                        DateTime expiryTime = (DateTime)Session["ExpiryTime"];
                        TimeSpan remainingTime = expiryTime - DateTime.Now;
                        ViewBag.RemainingSeconds = Math.Max(0, (int)remainingTime.TotalSeconds);
                    }
                    return View();
                }
            }
        }

        // Chức năng gửi lại mã xác nhận
        [HttpGet]
        public async Task<ActionResult> GuiLaiMaXacNhan()
        {
            string email = Session["RegisterEmail"] as string;

            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("DangKy");
            }

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                // Lấy thông tin đăng ký từ bảng XacNhanEmail
                string getInfoQuery = @"
        SELECT TOP 1 HoTen, MatKhauMaHoa 
        FROM XacNhanEmail 
        WHERE Email = @Email 
        ORDER BY NgayTao DESC";

                SqlCommand getInfoCmd = new SqlCommand(getInfoQuery, con);
                getInfoCmd.Parameters.AddWithValue("@Email", email);

                SqlDataReader reader = getInfoCmd.ExecuteReader();

                if (reader.Read())
                {
                    string hoTen = reader.GetString(0);
                    string matKhauMaHoa = reader.GetString(1);

                    reader.Close();

                    // Tạo mã xác nhận mới
                    string verificationCode = GenerateVerificationCode();

                    // Thời gian hiện tại
                    DateTime now = DateTime.Now;

                    // Thời gian hết hạn (3 phút)
                    DateTime expiryTime = now.AddMinutes(3);

                    // Lưu thời gian hết hạn vào session để sử dụng khi hiển thị đếm ngược
                    Session["ExpiryTime"] = expiryTime;

                    // Cập nhật trạng thái đã sử dụng cho các mã cũ
                    string updateOldCodesQuery = "UPDATE XacNhanEmail SET DaSuDung = 1 WHERE Email = @Email";
                    SqlCommand updateOldCodesCmd = new SqlCommand(updateOldCodesQuery, con);
                    updateOldCodesCmd.Parameters.AddWithValue("@Email", email);
                    updateOldCodesCmd.ExecuteNonQuery();

                    // Lưu thông tin xác nhận mới vào database
                    string insertVerificationQuery = @"
                        INSERT INTO XacNhanEmail (Email, MaCode, NgayTao, NgayHetHan, DaSuDung, HoTen, MatKhauMaHoa)
                        VALUES (@Email, @MaCode, @NgayTao, @NgayHetHan, 0, @HoTen, @MatKhauMaHoa)";

                    SqlCommand insertVerificationCmd = new SqlCommand(insertVerificationQuery, con);
                    insertVerificationCmd.Parameters.AddWithValue("@Email", email);
                    insertVerificationCmd.Parameters.AddWithValue("@MaCode", verificationCode);
                    insertVerificationCmd.Parameters.AddWithValue("@NgayTao", now);
                    insertVerificationCmd.Parameters.AddWithValue("@NgayHetHan", expiryTime);
                    insertVerificationCmd.Parameters.AddWithValue("@HoTen", hoTen);
                    insertVerificationCmd.Parameters.AddWithValue("@MatKhauMaHoa", matKhauMaHoa);

                    insertVerificationCmd.ExecuteNonQuery();

                    // Gửi email xác nhận
                    await _emailService.SendVerificationEmailAsync(email, hoTen, verificationCode);

                    TempData["ResendSuccess"] = "Đã gửi lại mã xác nhận. Vui lòng kiểm tra email của bạn.";
                }
                else
                {
                    reader.Close();
                    TempData["ResendError"] = "Không tìm thấy thông tin đăng ký.";
                }
            }

            return RedirectToAction("XacNhanEmail");
        }
    }
}