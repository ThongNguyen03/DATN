using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Authorize] // Yêu cầu đăng nhập để truy cập trang thanh toán
    public class ThanhToanController : Controller
    {
        private MedinetDATN db = new MedinetDATN();

        // GET: ThanhToan
        public ActionResult Index()
        {
            try
            {
                // Lấy ID người dùng hiện tại
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Lấy danh sách sản phẩm đã chọn từ session
                var selectedItems = Session["SelectedItems"] as List<int>;
                var cartItems = Session["CartItems"] as List<GioHang>;
                bool isMuaNgay = Session["IsMuaNgay"] != null && (bool)Session["IsMuaNgay"];

                if ((selectedItems == null || cartItems == null || !cartItems.Any()) && !isMuaNgay)
                {
                    TempData["Error"] = "Không có sản phẩm nào được chọn để thanh toán!";
                    return RedirectToAction("Index", "GioHangs");
                }

                // Khởi tạo ViewModel
                var viewModel = GetThanhToanViewModel(maNguoiDung, cartItems);

                // Lưu danh sách mã giỏ hàng đã chọn (nếu có)
                if (selectedItems != null && selectedItems.Any())
                {
                    viewModel.SelectedCartItems = string.Join(",", selectedItems);
                }
                else if (isMuaNgay && cartItems != null && cartItems.Any())
                {
                    // Trường hợp mua ngay, lưu mã sản phẩm vào SelectedCartItems
                    viewModel.SelectedCartItems = cartItems[0].MaSanPham.ToString();
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine("Lỗi trang thanh toán: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải trang thanh toán. Vui lòng thử lại sau!";
                return RedirectToAction("Index", "GioHangs");
            }
        }

        // POST: ThanhToan - Nhận dữ liệu trực tiếp từ form trong giỏ hàng
        [HttpPost]
        public ActionResult Index(string selectedItems)
        {
            try
            {
                // Lấy ID người dùng hiện tại
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Kiểm tra nếu không có sản phẩm nào được chọn
                if (string.IsNullOrEmpty(selectedItems))
                {
                    TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm để thanh toán!";
                    return RedirectToAction("Index", "GioHangs");
                }

                // Chuyển đổi danh sách ID từ chuỗi sang mảng số nguyên
                var selectedIds = selectedItems.Split(',').Select(int.Parse).ToList();

                // Lấy tất cả các mục trong giỏ hàng của người dùng
                var allCartItems = db.GioHangs
                    .Where(g => g.MaNguoiDung == maNguoiDung)
                    .Include(g => g.SanPham)
                    .Include(g => g.SanPham.NguoiBan)
                    .Include(g => g.SanPham.DanhSachAnhSanPham)
                    .ToList();

                // Lọc các mục đã chọn để thanh toán
                var cartItemsToCheckout = allCartItems.Where(item => selectedIds.Contains(item.MaGioHang)).ToList();

                if (cartItemsToCheckout.Count == 0)
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm đã chọn trong giỏ hàng!";
                    return RedirectToAction("Index", "GioHangs");
                }

                // Lưu thông tin giỏ hàng đã chọn vào session để sử dụng ở trang thanh toán
                Session["CartItems"] = cartItemsToCheckout;
                Session["SelectedItems"] = selectedIds;

                // Xóa các sản phẩm đã chọn khỏi giỏ hàng
                foreach (var item in cartItemsToCheckout)
                {
                    db.GioHangs.Remove(item);
                }
                db.SaveChanges();

                // Cập nhật số lượng sản phẩm còn lại trong giỏ hàng
                int remainingCartCount = allCartItems.Count - cartItemsToCheckout.Count;
                Session["SoLuongGioHang"] = remainingCartCount;

                // Khởi tạo ViewModel
                var viewModel = GetThanhToanViewModel(maNguoiDung, cartItemsToCheckout);

                // Lưu danh sách mã giỏ hàng đã chọn
                viewModel.SelectedCartItems = selectedItems;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine("Lỗi trang thanh toán: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải trang thanh toán. Vui lòng thử lại sau!";
                return RedirectToAction("Index", "GioHangs");
            }
        }


        private string TaoUrlThanhToanVNPay(int maDonHang, decimal tongTien)
        {
            string vnp_Returnurl = System.Configuration.ConfigurationManager.AppSettings["vnp_Returnurl"];
            string vnp_Url = System.Configuration.ConfigurationManager.AppSettings["vnp_Url"];
            string vnp_TmnCode = System.Configuration.ConfigurationManager.AppSettings["vnp_TmnCode"];
            string vnp_HashSecret = System.Configuration.ConfigurationManager.AppSettings["vnp_HashSecret"];

            // Tạo đối tượng VnPayLibrary
            VnPayLibrary vnpay = new VnPayLibrary();

            // Thêm thông tin thanh toán
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", Convert.ToInt64(tongTien * 100).ToString()); // Số tiền * 100 (VNĐ)

            // Thông tin đơn hàng
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", VnPayUtil.GetIpAddress());
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang: " + maDonHang);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", maDonHang.ToString());

            // Tùy chọn thêm: Thời gian hết hạn thanh toán
            DateTime expirationTime = DateTime.Now.AddMinutes(15);
            vnpay.AddRequestData("vnp_ExpireDate", expirationTime.ToString("yyyyMMddHHmmss"));

            // Tạo URL thanh toán
            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return paymentUrl;
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult XacNhanDatHang(ThanhToanViewModel model)
        {
            using (var dbTransaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Lấy ID người dùng hiện tại
                    int maNguoiDung = GetCurrentUserId();
                    if (maNguoiDung <= 0)
                    {
                        return RedirectToAction("DangNhap", "DangNhap");
                    }

                    // Lấy danh sách sản phẩm đã chọn từ session
                    var cartItems = Session["CartItems"] as List<GioHang>;
                    bool isMuaNgay = Session["IsMuaNgay"] != null && (bool)Session["IsMuaNgay"];

                    if (cartItems == null || !cartItems.Any())
                    {
                        TempData["Error"] = "Không có sản phẩm nào được chọn để thanh toán!";
                        return RedirectToAction("Index", "GioHangs");
                    }

                    // Kiểm tra địa chỉ giao hàng
                    var nguoiDung = db.NguoiDungs.Find(maNguoiDung);
                    if (nguoiDung == null || string.IsNullOrEmpty(nguoiDung.DiaChi) || string.IsNullOrEmpty(nguoiDung.SoDienThoai))
                    {
                        TempData["Error"] = "Vui lòng cập nhật địa chỉ giao hàng và số điện thoại!";
                        return RedirectToAction("Index");
                    }

                    // Tạo danh sách đơn hàng theo từng cửa hàng
                    var danhSachDonHang = new List<DonHang>();

                    // Nhóm sản phẩm theo người bán
                    var sanPhamTheoNguoiBan = cartItems
                        .GroupBy(c => c.SanPham.MaNguoiBan)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    // Tạo đơn hàng cho từng người bán
                    foreach (var nguoiBan in sanPhamTheoNguoiBan)
                    {
                        // Tính tổng tiền cho cửa hàng này
                        decimal tongTien = nguoiBan.Value.Sum(c => c.SoLuong * c.SanPham.GiaSanPham);

                        // Tạo đơn hàng mới
                        var donHang = new DonHang
                        {
                            MaNguoiDung = maNguoiDung,
                            MaNguoiBan = nguoiBan.Key,
                            TongSoTien = tongTien,
                            PhuongThucThanhToan = model.PhuongThucThanhToan,
                            TrangThaiDonHang = "Đang chờ xử lý",
                            NgayTao = DateTime.Now,
                            GhiChu = model.GhiChu, // Thêm ghi chú từ model
                            ChiTietDonHangs = new List<ChiTietDonHang>()
                        };

                        // Thêm chi tiết đơn hàng
                        foreach (var cartItem in nguoiBan.Value)
                        {
                            // Kiểm tra tồn kho
                            var sanPham = db.SanPhams.Find(cartItem.MaSanPham);
                            if (sanPham == null || sanPham.SoLuongTonKho < cartItem.SoLuong)
                            {
                                TempData["Error"] = $"Sản phẩm {sanPham?.TenSanPham ?? "không xác định"} không đủ số lượng tồn kho!";
                                return RedirectToAction("Index");
                            }

                            var chiTiet = new ChiTietDonHang
                            {
                                MaSanPham = cartItem.MaSanPham,
                                SoLuong = cartItem.SoLuong,
                                Gia = cartItem.SanPham.GiaSanPham
                            };
                            donHang.ChiTietDonHangs.Add(chiTiet);

                            // Cập nhật số lượng tồn kho
                            sanPham.SoLuongTonKho -= cartItem.SoLuong;
                            db.Entry(sanPham).State = EntityState.Modified;
                        }

                        // Thêm đơn hàng vào danh sách
                        danhSachDonHang.Add(donHang);
                        db.DonHangs.Add(donHang);
                    }

                    // Lưu đơn hàng và cập nhật tồn kho
                    db.SaveChanges();

                    // Tạo giao dịch cho từng đơn hàng
                    foreach (var donHang in danhSachDonHang)
                    {
                        // Trong tạo giao dịch
                        var giaoDich = new GiaoDich
                        {
                            MaDonHang = donHang.MaDonHang,
                            TrangThaiGiaoDich = "Đang chờ xử lý", // Luôn dùng giá trị hợp lệ
                            PhuongThucThanhToan = model.PhuongThucThanhToan,
                            TongTien = donHang.TongSoTien,
                            NgayGiaoDich = DateTime.Now
                        };

                        db.GiaoDichs.Add(giaoDich);
                    }

                    // SaveChanges riêng cho giao dịch
                    try
                    {
                        db.SaveChanges();
                    }
                    catch (Exception saveEx)
                    {
                        System.Diagnostics.Debug.WriteLine("Lỗi khi lưu giao dịch: " + saveEx.Message);
                        throw; // Re-throw để rollback transaction
                    }

                    // Xóa session
                    Session.Remove("CartItems");
                    Session.Remove("SelectedItems");
                    Session.Remove("AppliedVoucher");
                    Session.Remove("VoucherDiscount");
                    Session.Remove("IsMuaNgay");

                    // CHỈ commit transaction ở đây một lần
                    dbTransaction.Commit();

                    // Kiểm tra phương thức thanh toán
                    if (model.PhuongThucThanhToan == "VNPAY")
                    {
                        // Lưu dữ liệu vào session để sử dụng ở trang thanh toán VNPAY
                        if (danhSachDonHang.Any())
                        {
                            int firstOrderId = danhSachDonHang.First().MaDonHang;
                            Session["MaDonHangVNPAY"] = firstOrderId;
                            Session["DanhSachMaDonHang"] = string.Join(",", danhSachDonHang.Select(d => d.MaDonHang));

                            // Tạo URL thanh toán VNPAY và chuyển hướng trực tiếp
                            string vnpayUrl = TaoUrlThanhToanVNPay(firstOrderId, danhSachDonHang.Sum(d => d.TongSoTien));
                            return Redirect(vnpayUrl);
                        }
                    }

                    // Chuyển hướng đến trang cảm ơn
                    TempData["Success"] = "Đặt hàng thành công!";
                    return RedirectToAction("DatHangThanhCong", new { maDonHang = string.Join(",", danhSachDonHang.Select(d => d.MaDonHang)) });
                }
                catch (Exception ex)
                {
                    // Rollback transaction
                    dbTransaction.Rollback();

                    // Truy tìm đến inner exception sâu nhất
                    Exception innermost = ex;
                    while (innermost.InnerException != null)
                    {
                        innermost = innermost.InnerException;
                    }

                    // Ghi log lỗi chi tiết
                    System.Diagnostics.Debug.WriteLine("Lỗi đặt hàng: " + ex.Message);

                    TempData["Error"] = "Đã xảy ra lỗi khi đặt hàng: " + innermost.Message;
                    return RedirectToAction("Index");
                }
            }
        }

        // GET: ThanhToan/DatHangThanhCong
        public ActionResult DatHangThanhCong(string maDonHang)
        {
            // Nếu không có maDonHang trong tham số, thử lấy từ Session
            if (string.IsNullOrEmpty(maDonHang) && Session["DanhSachMaDonHang"] != null)
            {
                maDonHang = Session["DanhSachMaDonHang"].ToString();
                Session.Remove("DanhSachMaDonHang"); // Xóa session sau khi sử dụng
            }

            if (string.IsNullOrEmpty(maDonHang))
            {
                return RedirectToAction("Index", "Home");
            }

            var maDonHangs = maDonHang.Split(',').Select(int.Parse).ToList();
            var donHangs = db.DonHangs
                .Include(d => d.NguoiBan)
                .Where(d => maDonHangs.Contains(d.MaDonHang))
                .ToList();

            return View(donHangs);
        }

        // POST: ThanhToan/MuaNgay
        [HttpPost]
        [Authorize]
        public ActionResult MuaNgay(int maSanPham, int soLuong)
        {
            try
            {
                // Lấy ID người dùng hiện tại
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để tiếp tục." });
                }

                // Lấy thông tin sản phẩm
                var sanPham = db.SanPhams
                    .Include(s => s.NguoiBan)
                    .Include(s => s.DanhSachAnhSanPham)
                    .FirstOrDefault(s => s.MaSanPham == maSanPham);

                if (sanPham == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin sản phẩm!" });
                }

                // Kiểm tra số lượng tồn kho
                if (sanPham.SoLuongTonKho < soLuong)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Số lượng tồn kho không đủ. Hiện tại chỉ còn {sanPham.SoLuongTonKho} sản phẩm."
                    });
                }

                // Tạo giỏ hàng tạm thời
                var tempCartItem = new GioHang
                {
                    MaNguoiDung = maNguoiDung,
                    MaSanPham = maSanPham,
                    SoLuong = soLuong,
                    NgayTao = DateTime.Now,
                    SanPham = sanPham
                };

                // Lưu vào session
                List<GioHang> cartItems = new List<GioHang> { tempCartItem };
                List<int> selectedIds = new List<int> { 0 }; // Dùng ID tạm thời

                Session["CartItems"] = cartItems;
                Session["SelectedItems"] = selectedIds;
                Session["IsMuaNgay"] = true; // Đánh dấu đây là luồng mua ngay

                // Trả về thành công và URL chuyển hướng
                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action("Index", "ThanhToan")
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi MuaNgay: " + ex.Message);
                return Json(new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau."
                });
            }
        }

        // Phương thức bổ sung để xử lý thông tin từ ViewModel và tạo/lấy đơn hàng
        private DonHang XuLyThongTinDonHang(ThanhToanViewModel model)
        {
            try
            {
                // Lấy ID người dùng hiện tại
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return null;
                }

                // Nếu có mã đơn hàng từ model
                if (!string.IsNullOrEmpty(model.SelectedCartItems))
                {
                    var selectedIds = model.SelectedCartItems.Split(',').Select(int.Parse).ToList();

                    // Kiểm tra xem có đơn hàng nào đã được tạo trước đó không
                    var donHang = db.DonHangs
                        .Include(d => d.NguoiBan)
                        .Include(d => d.ChiTietDonHangs)
                        .OrderByDescending(d => d.MaDonHang)
                        .FirstOrDefault(d => d.MaNguoiDung == maNguoiDung);

                    return donHang;
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi XuLyThongTinDonHang: " + ex.Message);
                return null;
            }
        }

        // Sửa lại phương thức KetQuaThanhToan để tạo ký quỹ khi thanh toán thành công
        //public ActionResult KetQuaThanhToan()
        //{
        //    try
        //    {
        //        // Ghi log để debug
        //        System.Diagnostics.Debug.WriteLine("Đã nhận callback từ VNPAY");

        //        // Lấy các tham số trả về từ VNPAY
        //        string vnp_ResponseCode = Request.QueryString["vnp_ResponseCode"];
        //        string vnp_TransactionStatus = Request.QueryString["vnp_TransactionStatus"];
        //        string vnp_TxnRef = Request.QueryString["vnp_TxnRef"]; // Mã đơn hàng
        //        string vnp_Amount = Request.QueryString["vnp_Amount"];
        //        string vnp_TransactionNo = Request.QueryString["vnp_TransactionNo"]; // Mã giao dịch VNPAY
        //        string vnp_PayDate = Request.QueryString["vnp_PayDate"]; // Thời gian thanh toán
        //        string vnp_BankCode = Request.QueryString["vnp_BankCode"]; // Mã ngân hàng
        //        string vnp_CardType = Request.QueryString["vnp_CardType"]; // Loại thẻ
        //        string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];

        //        // Ghi log các tham số nhận được
        //        System.Diagnostics.Debug.WriteLine("ResponseCode: " + vnp_ResponseCode);
        //        System.Diagnostics.Debug.WriteLine("TxnRef (Mã đơn hàng): " + vnp_TxnRef);
        //        System.Diagnostics.Debug.WriteLine("Số tiền: " + vnp_Amount);
        //        System.Diagnostics.Debug.WriteLine("Mã giao dịch VNPAY: " + vnp_TransactionNo);

        //        // Lấy cấu hình VNPAY
        //        string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];

        //        // Xác thực chữ ký
        //        VnPayLibrary vnpay = new VnPayLibrary();
        //        foreach (string s in Request.QueryString)
        //        {
        //            // Bỏ qua hash để xác thực
        //            if (!s.StartsWith("vnp_SecureHash"))
        //            {
        //                vnpay.AddResponseData(s, Request.QueryString[s]);
        //            }
        //        }

        //        bool isValidSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

        //        // Kiểm tra kết quả thanh toán
        //        bool thanhToanThanhCong = isValidSignature && (vnp_ResponseCode == "00");

        //        if (thanhToanThanhCong)
        //        {
        //            // Sử dụng try-catch riêng cho phần cập nhật DB
        //            try
        //            {
        //                if (!string.IsNullOrEmpty(vnp_TxnRef) && int.TryParse(vnp_TxnRef, out int maDonHang))
        //                {
        //                    using (var dbTransaction = db.Database.BeginTransaction())
        //                    {
        //                        try
        //                        {
        //                            // 1. Kiểm tra và tìm đơn hàng
        //                            var donHang = db.DonHangs.Find(maDonHang);
        //                            System.Diagnostics.Debug.WriteLine($"Tìm đơn hàng: {(donHang != null ? "Thành công" : "Không tìm thấy")}");

        //                            if (donHang != null)
        //                            {
        //                                try
        //                                {
        //                                    // 2. Cập nhật trạng thái đơn hàng
        //                                    donHang.TrangThaiDonHang = "Đã thanh toán";
        //                                    db.Entry(donHang).State = EntityState.Modified;
        //                                    db.SaveChanges();
        //                                    System.Diagnostics.Debug.WriteLine("Cập nhật đơn hàng thành công");
        //                                }
        //                                catch (Exception donHangEx)
        //                                {
        //                                    System.Diagnostics.Debug.WriteLine("Lỗi cập nhật đơn hàng: " + donHangEx.Message);
        //                                    if (donHangEx.InnerException != null)
        //                                        System.Diagnostics.Debug.WriteLine("Inner error đơn hàng: " + donHangEx.InnerException.Message);
        //                                    throw;
        //                                }

        //                                try
        //                                {
        //                                    // 3. Tìm và cập nhật giao dịch
        //                                    var giaoDich = db.GiaoDichs.FirstOrDefault(g => g.MaDonHang == maDonHang);
        //                                    System.Diagnostics.Debug.WriteLine($"Tìm giao dịch: {(giaoDich != null ? "Thành công" : "Không tìm thấy")}");

        //                                    if (giaoDich != null)
        //                                    {
        //                                        // Log giá trị trước khi cập nhật
        //                                        System.Diagnostics.Debug.WriteLine($"MaGiaoDichVNPay hiện tại: '{giaoDich.MaGiaoDichVNPay}'");
        //                                        System.Diagnostics.Debug.WriteLine($"vnp_TransactionNo nhận được: '{vnp_TransactionNo}', length: {vnp_TransactionNo?.Length ?? 0}");

        //                                        // Cập nhật từng trường riêng biệt
        //                                        giaoDich.TrangThaiGiaoDich = "Đã hoàn thành";
        //                                        db.Entry(giaoDich).State = EntityState.Modified;
        //                                        db.SaveChanges();
        //                                        System.Diagnostics.Debug.WriteLine("Cập nhật trạng thái giao dịch thành công");

        //                                        // Cập nhật mã giao dịch VNPay
        //                                        if (!string.IsNullOrEmpty(vnp_TransactionNo))
        //                                        {
        //                                            giaoDich.MaGiaoDichVNPay = vnp_TransactionNo.Substring(0, Math.Min(vnp_TransactionNo.Length, 50)); // Giới hạn 50 ký tự
        //                                        }
        //                                        db.Entry(giaoDich).State = EntityState.Modified;
        //                                        db.SaveChanges();
        //                                        System.Diagnostics.Debug.WriteLine("Cập nhật mã giao dịch VNPAY thành công");

        //                                        // Cập nhật thời gian
        //                                        giaoDich.NgayGiaoDich = DateTime.Now;
        //                                        db.Entry(giaoDich).State = EntityState.Modified;
        //                                        db.SaveChanges();
        //                                        System.Diagnostics.Debug.WriteLine("Cập nhật ngày giao dịch thành công");

        //                                        // Cập nhật thông tin giao dịch
        //                                        string thongTinGD = string.Format(
        //                                            "Ngân hàng: {0}, Loại thẻ: {1}, Ngày thanh toán: {2}",
        //                                            vnp_BankCode ?? "Không có",
        //                                            vnp_CardType ?? "Không có",
        //                                            vnp_PayDate ?? "Không có"
        //                                        );
        //                                        giaoDich.ThongTinGiaoDich = thongTinGD;
        //                                        db.Entry(giaoDich).State = EntityState.Modified;
        //                                        db.SaveChanges();
        //                                        System.Diagnostics.Debug.WriteLine("Cập nhật thông tin giao dịch thành công");
        //                                    }
        //                                }
        //                                catch (Exception giaoDichEx)
        //                                {
        //                                    System.Diagnostics.Debug.WriteLine("Lỗi cập nhật giao dịch: " + giaoDichEx.Message);
        //                                    if (giaoDichEx.InnerException != null)
        //                                        System.Diagnostics.Debug.WriteLine("Inner error giao dịch: " + giaoDichEx.InnerException.Message);
        //                                    throw;
        //                                }
        //                            }

        //                            var escrowService = new EscrowService();
        //                            Task.Run(async () =>
        //                            {
        //                                try
        //                                {
        //                                    await escrowService.CreateEscrow(maDonHang);
        //                                    System.Diagnostics.Debug.WriteLine($"Đã tạo ký quỹ cho đơn hàng {maDonHang}");
        //                                }
        //                                catch (Exception ex)
        //                                {
        //                                    System.Diagnostics.Debug.WriteLine("Lỗi khi tạo ký quỹ: " + ex.Message);
        //                                }
        //                            });
        //                            System.Diagnostics.Debug.WriteLine($"Đã tạo ký quỹ cho đơn hàng {maDonHang}");

        //                            // 4. Commit transaction
        //                            dbTransaction.Commit();
        //                            System.Diagnostics.Debug.WriteLine("Transaction committed thành công");

        //                        }
        //                        catch (Exception ex)
        //                        {
        //                            // 5. Rollback nếu lỗi
        //                            dbTransaction.Rollback();
        //                            System.Diagnostics.Debug.WriteLine("Transaction rollback: " + ex.Message);

        //                            // Đào sâu nội dung lỗi
        //                            Exception currentEx = ex;
        //                            int level = 0;
        //                            while (currentEx.InnerException != null)
        //                            {
        //                                currentEx = currentEx.InnerException;
        //                                level++;
        //                                System.Diagnostics.Debug.WriteLine($"Inner exception level {level}: {currentEx.Message}");
        //                                System.Diagnostics.Debug.WriteLine($"Exception type: {currentEx.GetType().FullName}");
        //                            }

        //                            System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);
        //                        }
        //                    }

        //                }

        //                // Lấy danh sách mã đơn hàng từ session
        //                string danhSachMaDonHang = Session["DanhSachMaDonHang"] as string;
        //                if (string.IsNullOrEmpty(danhSachMaDonHang))
        //                {
        //                    danhSachMaDonHang = vnp_TxnRef;
        //                }

        //                TempData["Success"] = "Thanh toán thành công!";
        //                return RedirectToAction("DatHangThanhCong", new { maDonHang = danhSachMaDonHang });
        //            }
        //            catch (Exception dbEx)
        //            {
        //                System.Diagnostics.Debug.WriteLine("Lỗi không xác định khi xử lý DB: " + dbEx.Message);
        //                if (dbEx.InnerException != null)
        //                {
        //                    System.Diagnostics.Debug.WriteLine("Inner exception: " + dbEx.InnerException.Message);
        //                }

        //                // Vẫn chuyển hướng đến trang thành công vì thanh toán đã OK
        //                TempData["Success"] = "Thanh toán thành công! Hệ thống sẽ cập nhật đơn hàng của bạn trong thời gian sớm nhất.";
        //                return RedirectToAction("DatHangThanhCong", new { maDonHang = vnp_TxnRef });
        //            }
        //        }
        //        else
        //        {
        //            // Thanh toán không thành công
        //            System.Diagnostics.Debug.WriteLine("Thanh toán không thành công! Mã lỗi: " + vnp_ResponseCode);
        //            TempData["Error"] = "Thanh toán không thành công! Mã lỗi: " + vnp_ResponseCode;
        //            return RedirectToAction("Index", "Home");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Ghi log lỗi
        //        System.Diagnostics.Debug.WriteLine("Lỗi xử lý callback VNPAY: " + ex.Message);
        //        if (ex.InnerException != null)
        //        {
        //            System.Diagnostics.Debug.WriteLine("Inner exception: " + ex.InnerException.Message);
        //        }
        //        System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);

        //        TempData["Error"] = "Đã xảy ra lỗi khi xử lý kết quả thanh toán!";
        //        return RedirectToAction("Index", "Home");
        //    }
        //}


        // Phương thức lấy thông tin người dùng hiện tại

        public ActionResult KetQuaThanhToan()
        {
            try
            {
                // Ghi log để debug
                System.Diagnostics.Debug.WriteLine("Đã nhận callback từ VNPAY");

                // Lấy các tham số trả về từ VNPAY
                string vnp_ResponseCode = Request.QueryString["vnp_ResponseCode"];
                string vnp_TransactionStatus = Request.QueryString["vnp_TransactionStatus"];
                string vnp_TxnRef = Request.QueryString["vnp_TxnRef"]; // Mã đơn hàng đầu tiên
                string vnp_Amount = Request.QueryString["vnp_Amount"];
                string vnp_TransactionNo = Request.QueryString["vnp_TransactionNo"]; // Mã giao dịch VNPAY
                string vnp_PayDate = Request.QueryString["vnp_PayDate"]; // Thời gian thanh toán
                string vnp_BankCode = Request.QueryString["vnp_BankCode"]; // Mã ngân hàng
                string vnp_CardType = Request.QueryString["vnp_CardType"]; // Loại thẻ
                string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];

                // Ghi log các tham số nhận được
                System.Diagnostics.Debug.WriteLine("ResponseCode: " + vnp_ResponseCode);
                System.Diagnostics.Debug.WriteLine("TxnRef (Mã đơn hàng đầu tiên): " + vnp_TxnRef);
                System.Diagnostics.Debug.WriteLine("Số tiền: " + vnp_Amount);
                System.Diagnostics.Debug.WriteLine("Mã giao dịch VNPAY: " + vnp_TransactionNo);

                // Lấy cấu hình VNPAY
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];

                // Xác thực chữ ký
                VnPayLibrary vnpay = new VnPayLibrary();
                foreach (string s in Request.QueryString)
                {
                    // Bỏ qua hash để xác thực
                    if (!s.StartsWith("vnp_SecureHash"))
                    {
                        vnpay.AddResponseData(s, Request.QueryString[s]);
                    }
                }

                bool isValidSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

                // Kiểm tra kết quả thanh toán
                bool thanhToanThanhCong = isValidSignature && (vnp_ResponseCode == "00");

                if (thanhToanThanhCong)
                {
                    // Sử dụng try-catch riêng cho phần cập nhật DB
                    try
                    {
                        // Lấy danh sách mã đơn hàng từ session
                        string danhSachMaDonHang = Session["DanhSachMaDonHang"] as string;

                        // Nếu không có trong session, sử dụng vnp_TxnRef
                        if (string.IsNullOrEmpty(danhSachMaDonHang))
                        {
                            danhSachMaDonHang = vnp_TxnRef;
                            System.Diagnostics.Debug.WriteLine("Không tìm thấy danh sách mã đơn hàng trong session, sử dụng vnp_TxnRef: " + vnp_TxnRef);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Tìm thấy danh sách mã đơn hàng trong session: " + danhSachMaDonHang);
                        }

                        // Chuyển đổi chuỗi mã đơn hàng thành mảng
                        var maDonHangList = danhSachMaDonHang.Split(',')
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Select(s => int.Parse(s))
                            .ToList();

                        System.Diagnostics.Debug.WriteLine($"Số đơn hàng cần xử lý: {maDonHangList.Count}");

                        // Xử lý từng đơn hàng
                        foreach (var maDonHang in maDonHangList)
                        {
                            using (var dbTransaction = db.Database.BeginTransaction())
                            {
                                try
                                {
                                    System.Diagnostics.Debug.WriteLine($"Đang xử lý đơn hàng: {maDonHang}");

                                    // 1. Kiểm tra và tìm đơn hàng
                                    var donHang = db.DonHangs.Find(maDonHang);
                                    System.Diagnostics.Debug.WriteLine($"Tìm đơn hàng {maDonHang}: {(donHang != null ? "Thành công" : "Không tìm thấy")}");

                                    if (donHang != null)
                                    {
                                        try
                                        {
                                            // 2. Cập nhật trạng thái đơn hàng
                                            donHang.TrangThaiDonHang = "Đã thanh toán";
                                            db.Entry(donHang).State = EntityState.Modified;
                                            db.SaveChanges();
                                            System.Diagnostics.Debug.WriteLine($"Cập nhật đơn hàng {maDonHang} thành công");
                                        }
                                        catch (Exception donHangEx)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Lỗi cập nhật đơn hàng {maDonHang}: " + donHangEx.Message);
                                            if (donHangEx.InnerException != null)
                                                System.Diagnostics.Debug.WriteLine("Inner error đơn hàng: " + donHangEx.InnerException.Message);
                                            throw;
                                        }

                                        try
                                        {
                                            // 3. Tìm và cập nhật giao dịch
                                            var giaoDich = db.GiaoDichs.FirstOrDefault(g => g.MaDonHang == maDonHang);
                                            System.Diagnostics.Debug.WriteLine($"Tìm giao dịch cho đơn hàng {maDonHang}: {(giaoDich != null ? "Thành công" : "Không tìm thấy")}");

                                            if (giaoDich != null)
                                            {
                                                // Cập nhật trạng thái giao dịch
                                                giaoDich.TrangThaiGiaoDich = "Đã hoàn thành";
                                                db.Entry(giaoDich).State = EntityState.Modified;
                                                db.SaveChanges();

                                                // Cập nhật mã giao dịch VNPay (chỉ cho đơn hàng đầu tiên)
                                                if (!string.IsNullOrEmpty(vnp_TransactionNo) && maDonHang.ToString() == vnp_TxnRef)
                                                {
                                                    giaoDich.MaGiaoDichVNPay = vnp_TransactionNo.Substring(0, Math.Min(vnp_TransactionNo.Length, 50));
                                                    db.Entry(giaoDich).State = EntityState.Modified;
                                                    db.SaveChanges();
                                                }
                                                else if (!string.IsNullOrEmpty(vnp_TransactionNo))
                                                {
                                                    // Cho các đơn hàng khác, thêm thông tin tham chiếu
                                                    giaoDich.MaGiaoDichVNPay = $"REF-{vnp_TransactionNo.Substring(0, Math.Min(vnp_TransactionNo.Length, 45))}";
                                                    db.Entry(giaoDich).State = EntityState.Modified;
                                                    db.SaveChanges();
                                                }

                                                // Cập nhật thời gian
                                                giaoDich.NgayGiaoDich = DateTime.Now;

                                                // Cập nhật thông tin giao dịch
                                                string thongTinGD = string.Format(
                                                    "Ngân hàng: {0}, Loại thẻ: {1}, Ngày thanh toán: {2}",
                                                    vnp_BankCode ?? "Không có",
                                                    vnp_CardType ?? "Không có",
                                                    vnp_PayDate ?? "Không có"
                                                );
                                                giaoDich.ThongTinGiaoDich = thongTinGD;
                                                db.Entry(giaoDich).State = EntityState.Modified;
                                                db.SaveChanges();

                                                System.Diagnostics.Debug.WriteLine($"Cập nhật giao dịch cho đơn hàng {maDonHang} thành công");
                                            }
                                        }
                                        catch (Exception giaoDichEx)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"Lỗi cập nhật giao dịch cho đơn hàng {maDonHang}: " + giaoDichEx.Message);
                                            if (giaoDichEx.InnerException != null)
                                                System.Diagnostics.Debug.WriteLine("Inner error giao dịch: " + giaoDichEx.InnerException.Message);
                                            throw;
                                        }

                                        // Tạo ký quỹ cho đơn hàng
                                        var escrowService = new EscrowService();
                                        Task.Run(async () =>
                                        {
                                            try
                                            {
                                                await escrowService.CreateEscrow(maDonHang);
                                                System.Diagnostics.Debug.WriteLine($"Đã tạo ký quỹ cho đơn hàng {maDonHang}");
                                            }
                                            catch (Exception ex)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"Lỗi khi tạo ký quỹ cho đơn hàng {maDonHang}: " + ex.Message);
                                            }
                                        });
                                    }

                                    // Commit transaction cho đơn hàng này
                                    dbTransaction.Commit();
                                    System.Diagnostics.Debug.WriteLine($"Transaction committed thành công cho đơn hàng {maDonHang}");
                                }
                                catch (Exception ex)
                                {
                                    // Rollback transaction nếu có lỗi cho đơn hàng này
                                    dbTransaction.Rollback();
                                    System.Diagnostics.Debug.WriteLine($"Transaction rollback cho đơn hàng {maDonHang}: " + ex.Message);

                                    // Đào sâu nội dung lỗi
                                    Exception currentEx = ex;
                                    int level = 0;
                                    while (currentEx.InnerException != null)
                                    {
                                        currentEx = currentEx.InnerException;
                                        level++;
                                        System.Diagnostics.Debug.WriteLine($"Inner exception level {level}: {currentEx.Message}");
                                    }

                                    // Ghi log chi tiết lỗi nhưng không dừng vòng lặp, tiếp tục xử lý đơn hàng tiếp theo
                                    System.Diagnostics.Debug.WriteLine($"Đã xảy ra lỗi với đơn hàng {maDonHang} nhưng vẫn tiếp tục xử lý các đơn hàng khác");
                                }
                            }
                        }

                        // Xóa session sau khi xử lý xong tất cả đơn hàng
                        Session.Remove("DanhSachMaDonHang");
                        Session.Remove("MaDonHangVNPAY");

                        TempData["Success"] = "Thanh toán thành công!";
                        return RedirectToAction("DatHangThanhCong", new { maDonHang = danhSachMaDonHang });
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine("Lỗi tổng thể khi xử lý DB: " + dbEx.Message);
                        if (dbEx.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Inner exception: " + dbEx.InnerException.Message);
                        }

                        // Vẫn chuyển hướng đến trang thành công vì thanh toán đã OK
                        string danhSachMaDonHang = Session["DanhSachMaDonHang"] as string ?? vnp_TxnRef;
                        TempData["Success"] = "Thanh toán thành công! Hệ thống sẽ cập nhật đơn hàng của bạn trong thời gian sớm nhất.";
                        return RedirectToAction("DatHangThanhCong", new { maDonHang = danhSachMaDonHang });
                    }
                }
                else
                {
                    // Thanh toán không thành công
                    System.Diagnostics.Debug.WriteLine("Thanh toán không thành công! Mã lỗi: " + vnp_ResponseCode);
                    TempData["Error"] = "Thanh toán không thành công! Mã lỗi: " + vnp_ResponseCode;
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine("Lỗi xử lý callback VNPAY: " + ex.Message);
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine("Inner exception: " + ex.InnerException.Message);
                }
                System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);

                TempData["Error"] = "Đã xảy ra lỗi khi xử lý kết quả thanh toán!";
                return RedirectToAction("Index", "Home");
            }
        }

        private int GetCurrentUserId()
        {
            var userName = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(n => n.Email == userName);

            if (nguoiDung != null)
            {
                return nguoiDung.MaNguoiDung;
            }

            if (Session["MaNguoiDung"] != null)
            {
                return Convert.ToInt32(Session["MaNguoiDung"]);
            }

            return 0;
        }

        // Phương thức tạo ViewModel cho thanh toán - đã xử lý null safety
        private ThanhToanViewModel GetThanhToanViewModel(int maNguoiDung, List<GioHang> cartItems)
        {
            try
            {
                // Lấy thông tin người dùng
                var nguoiDung = db.NguoiDungs.Find(maNguoiDung);

                // Kiểm tra và đảm bảo cartItems không null
                if (cartItems == null)
                {
                    cartItems = new List<GioHang>();
                }

                // Chuẩn bị danh sách sản phẩm
                var items = new List<CartItemViewModel>();

                foreach (var item in cartItems)
                {
                    // Kiểm tra null cho SanPham
                    if (item.SanPham == null)
                    {
                        // Bỏ qua item này hoặc lấy thông tin sản phẩm từ DB
                        var sanPham = db.SanPhams.Find(item.MaSanPham);
                        if (sanPham == null) continue; // Bỏ qua nếu không tìm thấy sản phẩm

                        // Cập nhật tham chiếu SanPham
                        item.SanPham = sanPham;
                    }

                    // Lấy đường dẫn ảnh đầu tiên hoặc ảnh mặc định
                    string anhSanPham = "/Content/Images/default-product.jpg";
                    if (item.SanPham.DanhSachAnhSanPham != null && item.SanPham.DanhSachAnhSanPham.Any())
                    {
                        anhSanPham = item.SanPham.DanhSachAnhSanPham.First().DuongDanAnh;
                    }

                    // Lấy thông tin người bán
                    string tenCuaHang = "Medinet Shop";
                    if (item.SanPham.NguoiBan != null)
                    {
                        tenCuaHang = item.SanPham.NguoiBan.TenCuaHang;
                    }
                    else if (item.SanPham.MaNguoiBan > 0)
                    {
                        // Nếu NguoiBan null nhưng có MaNguoiBan, load từ DB
                        var nguoiBan = db.NguoiBans.Find(item.SanPham.MaNguoiBan);
                        if (nguoiBan != null)
                        {
                            tenCuaHang = nguoiBan.TenCuaHang;
                        }
                    }

                    items.Add(new CartItemViewModel
                    {
                        MaGioHang = item.MaGioHang,
                        MaSanPham = item.MaSanPham,
                        TenSanPham = item.SanPham.TenSanPham,
                        SoLuong = item.SoLuong,
                        GiaSanPham = item.SanPham.GiaSanPham,
                        ThanhTien = item.SoLuong * item.SanPham.GiaSanPham,
                        AnhSanPham = anhSanPham,
                        TenCuaHang = tenCuaHang
                    });
                }

                // Tính toán tổng tiền
                decimal tongTienHang = items.Sum(i => i.ThanhTien);
                decimal phiVanChuyen = items.Any() ? 30000 : 0; // Phí vận chuyển cố định
                decimal giamGiaVanChuyen = phiVanChuyen > 0 ? -phiVanChuyen : 0; // Giảm giá vận chuyển

                // Lấy giảm giá voucher từ session nếu có
                decimal giamGiaVoucher = 0;
                if (Session["AppliedVoucher"] != null && Session["VoucherDiscount"] != null)
                {
                    decimal.TryParse(Session["VoucherDiscount"].ToString(), out giamGiaVoucher);
                }

                // Phân nhóm giỏ hàng theo cửa hàng (với xử lý an toàn khi items trống)
                var groupedCartItems = new List<ShopCartGroup>();
                if (items.Any())
                {
                    groupedCartItems = items.GroupBy(i => i.TenCuaHang)
                        .Select(g => new ShopCartGroup
                        {
                            TenCuaHang = g.Key,
                            Items = g.ToList()
                        }).ToList();
                }

                return new ThanhToanViewModel
                {
                    CartItems = items,
                    ShopGroups = groupedCartItems,
                    TongTienHang = tongTienHang,
                    PhiVanChuyen = phiVanChuyen,
                    GiamGiaVanChuyen = giamGiaVanChuyen,
                    GiamGiaVoucher = giamGiaVoucher,
                    TongThanhToan = tongTienHang + phiVanChuyen + giamGiaVanChuyen - giamGiaVoucher,
                    DiaChi = nguoiDung?.DiaChi ?? "Chưa có địa chỉ",
                    SoDienThoai = nguoiDung?.SoDienThoai ?? "Chưa có số điện thoại",
                    TenNguoiDung = nguoiDung?.TenNguoiDung ?? User.Identity.Name,
                    PhuongThucThanhToan = "COD" // Mặc định là COD
                };
            }
            catch (Exception ex)
            {
                // Log lỗi
                System.Diagnostics.Debug.WriteLine("Lỗi trong GetThanhToanViewModel: " + ex.Message);

                // Trả về ViewModel trống để tránh lỗi
                return new ThanhToanViewModel
                {
                    CartItems = new List<CartItemViewModel>(),
                    ShopGroups = new List<ShopCartGroup>(),
                    TongTienHang = 0,
                    PhiVanChuyen = 0,
                    GiamGiaVanChuyen = 0,
                    GiamGiaVoucher = 0,
                    TongThanhToan = 0,
                    DiaChi = "Chưa có địa chỉ",
                    SoDienThoai = "Chưa có số điện thoại",
                    TenNguoiDung = User.Identity.Name,
                    PhuongThucThanhToan = "COD"
                };
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


        // Sửa lại phương thức TaoThanhToanVNPay để thêm xử lý lỗi chi tiết
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TaoThanhToanVNPay(int maDonHang, string selectedBank)
        {
            try
            {
                // Ghi log bắt đầu quá trình
                System.Diagnostics.Debug.WriteLine("Bắt đầu tạo thanh toán VNPAY cho đơn hàng: " + maDonHang);

                // Lấy thông tin đơn hàng
                var donHang = db.DonHangs.Find(maDonHang);
                if (donHang == null)
                {
                    System.Diagnostics.Debug.WriteLine("Không tìm thấy đơn hàng với mã: " + maDonHang);
                    TempData["Error"] = "Không tìm thấy thông tin đơn hàng!";
                    return RedirectToAction("Index", "Home");
                }

                // Ghi log thông tin đơn hàng
                System.Diagnostics.Debug.WriteLine($"Thông tin đơn hàng: MaDonHang={donHang.MaDonHang}, TongSoTien={donHang.TongSoTien}");

                // Kiểm tra quyền truy cập
                int maNguoiDung = GetCurrentUserId();
                if (donHang.MaNguoiDung != maNguoiDung)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi quyền: MaNguoiDung hiện tại = {maNguoiDung}, MaNguoiDung của đơn hàng = {donHang.MaNguoiDung}");
                    TempData["Error"] = "Bạn không có quyền truy cập đơn hàng này!";
                    return RedirectToAction("Index", "Home");
                }

                // Lấy thông tin cấu hình từ Web.config
                string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"];
                string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"];
                string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"];
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];

                // Ghi log cấu hình
                System.Diagnostics.Debug.WriteLine($"Cấu hình VNPAY: URL={vnp_Url}, ReturnURL={vnp_Returnurl}, TmnCode={vnp_TmnCode}");

                // Kiểm tra cấu hình VNPAY
                if (string.IsNullOrEmpty(vnp_Url) || string.IsNullOrEmpty(vnp_TmnCode) ||
                    string.IsNullOrEmpty(vnp_HashSecret) || string.IsNullOrEmpty(vnp_Returnurl))
                {
                    System.Diagnostics.Debug.WriteLine("Thiếu cấu hình VNPAY!");
                    TempData["Error"] = "Cấu hình thanh toán VNPAY chưa đầy đủ!";
                    return RedirectToAction("ThanhToanVNPay", new { maDonHang = maDonHang });
                }

                // Tạo đối tượng VnPayLibrary
                VnPayLibrary vnpay = new VnPayLibrary();

                // Tạo thời gian hiện tại theo định dạng yyyyMMddHHmmss
                string createDate = DateTime.Now.ToString("yyyyMMddHHmmss");

                // Cấu hình thông tin thanh toán
                vnpay.AddRequestData("vnp_Version", "2.1.0");
                vnpay.AddRequestData("vnp_Command", "pay");
                vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);

                // Đảm bảo số tiền là số nguyên và nhân 100 (đơn vị VND)
                long amount = (long)(donHang.TongSoTien * 100);
                vnpay.AddRequestData("vnp_Amount", amount.ToString());
                System.Diagnostics.Debug.WriteLine($"Số tiền thanh toán: {donHang.TongSoTien} -> {amount}");

                // Thêm mã ngân hàng nếu được chọn
                if (!string.IsNullOrEmpty(selectedBank))
                {
                    vnpay.AddRequestData("vnp_BankCode", selectedBank);
                    System.Diagnostics.Debug.WriteLine($"Ngân hàng đã chọn: {selectedBank}");
                }

                // Thiết lập thêm thông tin
                //string ipAddress = VnPayUtil.GetIpAddress();
                string ipAddress = "127.0.0.1"; // Địa chỉ localhost cố định
                vnpay.AddRequestData("vnp_CreateDate", createDate);
                vnpay.AddRequestData("vnp_CurrCode", "VND");
                vnpay.AddRequestData("vnp_IpAddr", ipAddress);
                vnpay.AddRequestData("vnp_Locale", "vn");
                vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang #" + maDonHang);
                vnpay.AddRequestData("vnp_OrderType", "other");
                vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
                vnpay.AddRequestData("vnp_TxnRef", maDonHang.ToString());

                System.Diagnostics.Debug.WriteLine($"IP người dùng: {ipAddress}");

                // Tạo URL thanh toán
                string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
                System.Diagnostics.Debug.WriteLine("URL thanh toán VNPAY: " + paymentUrl);

                // Lưu thông tin giao dịch
                var giaoDich = db.GiaoDichs.FirstOrDefault(g => g.MaDonHang == maDonHang);
                if (giaoDich != null)
                {
                    giaoDich.TrangThaiGiaoDich = "Đang thanh toán";
                    giaoDich.NgayGiaoDich = DateTime.Now;
                    db.Entry(giaoDich).State = EntityState.Modified;
                    db.SaveChanges();
                    System.Diagnostics.Debug.WriteLine($"Đã cập nhật giao dịch ID={giaoDich.MaGiaoDich} sang trạng thái 'Đang thanh toán'");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Không tìm thấy giao dịch cho đơn hàng ID={maDonHang}");
                }

                // Chuyển hướng đến trang thanh toán VNPay
                System.Diagnostics.Debug.WriteLine("Chuyển hướng đến trang thanh toán VNPAY");
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                // Ghi log chi tiết lỗi
                System.Diagnostics.Debug.WriteLine("Lỗi tạo thanh toán VNPAY: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack trace: " + ex.StackTrace);

                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine("Inner exception: " + ex.InnerException.Message);
                }

                TempData["Error"] = "Đã xảy ra lỗi khi tạo thanh toán: " + ex.Message;
                return RedirectToAction("ThanhToanVNPay", new { maDonHang = maDonHang });
            }
        }
    }

    // View Model cho trang thanh toán
    public class ThanhToanViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; }
        public List<ShopCartGroup> ShopGroups { get; set; }
        public decimal TongTienHang { get; set; }
        public decimal PhiVanChuyen { get; set; }
        public decimal GiamGiaVanChuyen { get; set; }
        public decimal GiamGiaVoucher { get; set; }
        public decimal TongThanhToan { get; set; }
        public string DiaChi { get; set; }
        public string SoDienThoai { get; set; }
        public string TenNguoiDung { get; set; }
        public string GhiChu { get; set; }
        public string PhuongThucThanhToan { get; set; }
        public string SelectedCartItems { get; set; }
    }
}