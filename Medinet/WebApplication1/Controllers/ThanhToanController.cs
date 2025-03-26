using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
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
        private EscrowService escrowService = new EscrowService();

        // GET: ThanhToan
        public ActionResult Index()
        {
            try
            {
                // Get current user ID
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Get selected items from session
                var selectedItems = Session["SelectedItems"] as List<int>;
                var cartItems = Session["CartItems"] as List<GioHang>;
                bool isMuaNgay = Session["IsMuaNgay"] != null && (bool)Session["IsMuaNgay"];

                if ((selectedItems == null || !selectedItems.Any() || cartItems == null || !cartItems.Any()) && !isMuaNgay)
                {
                    TempData["Error"] = "Không có sản phẩm nào được chọn để thanh toán!";
                    return RedirectToAction("Index", "GioHangs");
                }

                // Initialize ViewModel
                var viewModel = GetThanhToanViewModel(maNguoiDung, cartItems);

                // Save selected cart item IDs (if any)
                if (selectedItems != null && selectedItems.Any())
                {
                    viewModel.SelectedCartItems = string.Join(",", selectedItems);
                }
                else if (isMuaNgay && cartItems != null && cartItems.Any())
                {
                    // For "Buy Now" case, save product ID in SelectedCartItems
                    viewModel.SelectedCartItems = cartItems[0].MaSanPham.ToString();
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine("Checkout page error: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải trang thanh toán. Vui lòng thử lại sau!";
                return RedirectToAction("Index", "GioHangs");
            }
        }

        // POST: ThanhToan - Receives data directly from cart form
        [HttpPost]
        public ActionResult Index(string selectedItems)
        {
            try
            {
                // Get current user ID
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Check if no products were selected
                if (string.IsNullOrEmpty(selectedItems))
                {
                    TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm để thanh toán!";
                    return RedirectToAction("Index", "GioHangs");
                }

                // Convert ID list from string to int array
                var selectedIds = selectedItems.Split(',').Select(int.Parse).ToList();

                // Get all items in user's cart
                var allCartItems = db.GioHangs
                    .Where(g => g.MaNguoiDung == maNguoiDung)
                    .Include(g => g.SanPham)
                    .Include(g => g.SanPham.NguoiBan)
                    .Include(g => g.SanPham.AnhSanPhams)
                    .ToList();

                // Filter selected items for checkout
                var cartItemsToCheckout = allCartItems.Where(item => selectedIds.Contains(item.MaGioHang)).ToList();

                if (cartItemsToCheckout.Count == 0)
                {
                    TempData["Error"] = "Không tìm thấy sản phẩm đã chọn trong giỏ hàng!";
                    return RedirectToAction("Index", "GioHangs");
                }

                // Save checkout cart items to session for use on checkout page
                Session["CartItems"] = cartItemsToCheckout;
                Session["SelectedItems"] = selectedIds;

                // Remove selected products from cart
                foreach (var item in cartItemsToCheckout)
                {
                    db.GioHangs.Remove(item);
                }
                db.SaveChanges();

                // Update remaining cart count
                int remainingCartCount = allCartItems.Count - cartItemsToCheckout.Count;
                Session["SoLuongGioHang"] = remainingCartCount;

                // Initialize ViewModel
                var viewModel = GetThanhToanViewModel(maNguoiDung, cartItemsToCheckout);

                // Save selected cart item IDs
                viewModel.SelectedCartItems = selectedItems;

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine("Checkout page error: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải trang thanh toán. Vui lòng thử lại sau!";
                return RedirectToAction("Index", "GioHangs");
            }
        }

        private string TaoUrlThanhToanVNPay(int maDonHang, decimal tongTien)
        {
            // Get configuration values from Web.config
            string vnp_Returnurl = ConfigurationManager.AppSettings["vnp_Returnurl"];
            string vnp_Url = ConfigurationManager.AppSettings["vnp_Url"];
            string vnp_TmnCode = ConfigurationManager.AppSettings["vnp_TmnCode"];
            string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];

            // Create VnPayLibrary object
            VnPayLibrary vnpay = new VnPayLibrary();

            // Add payment information
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", Convert.ToInt64(tongTien * 100).ToString()); // Amount * 100 (VND)

            // Order information
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", VnPayUtil.GetIpAddress());
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang: " + maDonHang);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", maDonHang.ToString());

            // Optional: Payment expiration time
            DateTime expirationTime = DateTime.Now.AddMinutes(15);
            vnpay.AddRequestData("vnp_ExpireDate", expirationTime.ToString("yyyyMMddHHmmss"));

            // Create payment URL
            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            return paymentUrl;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> XacNhanDatHang(ThanhToanViewModel model)
        {
            using (var dbTransaction = db.Database.BeginTransaction())
            {
                try
                {
                    // Get current user ID
                    int maNguoiDung = GetCurrentUserId();
                    if (maNguoiDung <= 0)
                    {
                        return RedirectToAction("DangNhap", "DangNhap");
                    }

                    // Get selected items from session
                    var cartItems = Session["CartItems"] as List<GioHang>;
                    bool isMuaNgay = Session["IsMuaNgay"] != null && (bool)Session["IsMuaNgay"];

                    if (cartItems == null || !cartItems.Any())
                    {
                        TempData["Error"] = "Không có sản phẩm nào được chọn để thanh toán!";
                        return RedirectToAction("Index", "GioHangs");
                    }

                    // Check shipping address
                    var nguoiDung = db.NguoiDungs.Find(maNguoiDung);
                    if (nguoiDung == null || string.IsNullOrEmpty(nguoiDung.DiaChi) || string.IsNullOrEmpty(nguoiDung.SoDienThoai))
                    {
                        TempData["Error"] = "Vui lòng cập nhật địa chỉ giao hàng và số điện thoại!";
                        return RedirectToAction("Index");
                    }

                    // Create order list by store
                    var danhSachDonHang = new List<DonHang>();

                    // Group products by seller
                    var sanPhamTheoNguoiBan = cartItems
                        .GroupBy(c => c.SanPham.MaNguoiBan)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    // Create order for each seller
                    foreach (var nguoiBan in sanPhamTheoNguoiBan)
                    {
                        // Calculate total for this store
                        decimal tongTien = nguoiBan.Value.Sum(c => c.SoLuong * c.SanPham.GiaSanPham);

                        // Create new order
                        var donHang = new DonHang
                        {
                            MaNguoiDung = maNguoiDung,
                            MaNguoiBan = nguoiBan.Key,
                            TongSoTien = tongTien,
                            PhuongThucThanhToan = model.PhuongThucThanhToan,
                            // For COD, keep default status. For VNPAY, set to waiting for payment
                            TrangThaiDonHang = "Đang chờ xử lý" ,
                            NgayTao = DateTime.Now,
                            GhiChu = model.GhiChu,
                            ChiTietDonHangs = new List<ChiTietDonHang>()
                        };

                        // Add order details
                        foreach (var cartItem in nguoiBan.Value)
                        {
                            // Check stock
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

                            // Update stock
                            sanPham.SoLuongTonKho -= cartItem.SoLuong;
                            db.Entry(sanPham).State = EntityState.Modified;
                        }

                        // Add order to list
                        danhSachDonHang.Add(donHang);
                        db.DonHangs.Add(donHang);
                    }
                    System.Diagnostics.Debug.WriteLine("trước khi save");
                    // Save orders and update stock
                    db.SaveChanges();
                    System.Diagnostics.Debug.WriteLine("sau khi save");
                    // Sau đó tạo giao dịch với MaDonHang đã có
                    System.Diagnostics.Debug.WriteLine("bắt đầu tạo giao dịch");
                    // Create transaction for each order
                    foreach (var donHang in danhSachDonHang)
                    {
                        // Create transaction
                        var giaoDich = new GiaoDich
                        {
                            MaDonHang = donHang.MaDonHang,
                            TrangThaiGiaoDich = "Đang chờ xử lý" ,
                            PhuongThucThanhToan = model.PhuongThucThanhToan,
                            TongTien = donHang.TongSoTien,
                            NgayGiaoDich = DateTime.Now
                        };

                        db.GiaoDichs.Add(giaoDich);
                    }

                    System.Diagnostics.Debug.WriteLine("trước khi save don hang");
                    // Save orders and update stock
                    db.SaveChanges();
                    System.Diagnostics.Debug.WriteLine("sau khi save don hang");

                    //// Tạo ký quỹ cho các đơn hàng
                    //if (model.PhuongThucThanhToan == "COD")
                    //{
                    //    try
                    //    {
                    //        // Tạo ký quỹ bất đồng bộ
                    //        var maDonHangList = danhSachDonHang.Select(d => d.MaDonHang).ToList();
                    //        var escrows = await escrowService.CreateMultipleEscrowsAsync(maDonHangList);

                    //        System.Diagnostics.Debug.WriteLine($"Đã tạo {escrows.Count} ký quỹ cho các đơn hàng");
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        System.Diagnostics.Debug.WriteLine($"Lỗi khi tạo ký quỹ: {ex.Message}");
                    //        // Ghi log lỗi nhưng vẫn tiếp tục
                    //    }
                    //}


                    // Clear session
                    Session.Remove("CartItems");
                    Session.Remove("SelectedItems");
                    Session.Remove("AppliedVoucher");
                    Session.Remove("VoucherDiscount");
                    Session.Remove("IsMuaNgay");
                    System.Diagnostics.Debug.WriteLine("trươc khi commit");

                    // Commit transaction only once here
                    dbTransaction.Commit();
                    System.Diagnostics.Debug.WriteLine("sau khi commit");

                    // Sau đó tạo ký quỹ riêng
                    if (model.PhuongThucThanhToan == "COD")
                    {
                        try
                        {
                            var maDonHangList = danhSachDonHang.Select(d => d.MaDonHang).ToList();

                            // Chạy bất đồng bộ nhưng không đợi kết quả để tránh ảnh hưởng đến thanh toán
                            Task.Run(async () => {
                                try
                                {
                                    var result = await escrowService.CreateMultipleEscrowsAsync(maDonHangList);
                                    System.Diagnostics.Debug.WriteLine($"Đã tạo {result.Count} ký quỹ bất đồng bộ");
                                }
                                catch (Exception innerEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Lỗi tạo ký quỹ bất đồng bộ: {innerEx.Message}");
                                }
                            });

                            System.Diagnostics.Debug.WriteLine($"Đã bắt đầu quá trình tạo ký quỹ bất đồng bộ");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Lỗi khi bắt đầu tạo ký quỹ: {ex.Message}");
                        }
                    }

                    // Check payment method
                    if (model.PhuongThucThanhToan == "VNPAY")
                    {
                       
                        // Save data to session for use on VNPAY payment page
                        if (danhSachDonHang.Any())
                        {
                           
                            int firstOrderId = danhSachDonHang.First().MaDonHang;
                            Session["MaDonHangVNPAY"] = firstOrderId;
                            Session["DanhSachMaDonHang"] = string.Join(",", danhSachDonHang.Select(d => d.MaDonHang));

                            // Log đơn hàng trước khi chuyển hướng
                            System.Diagnostics.Debug.WriteLine($"Chuyển hướng đến VNPAY với đơn hàng: {firstOrderId}");
                            System.Diagnostics.Debug.WriteLine($"Danh sách đơn hàng: {string.Join(",", danhSachDonHang.Select(d => d.MaDonHang))}");

                            // Create VNPAY payment URL and redirect directly
                            string vnpayUrl = TaoUrlThanhToanVNPay(firstOrderId, danhSachDonHang.Sum(d => d.TongSoTien));
                            return Redirect(vnpayUrl);
                        }
                    }

                    // Redirect to thank you page
                    TempData["Success"] = "Đặt hàng thành công!";
                    return RedirectToAction("DatHangThanhCong", new { maDonHang = string.Join(",", danhSachDonHang.Select(d => d.MaDonHang)) });
                }
                catch (Exception ex)
                {
                    // Rollback transaction
                    dbTransaction.Rollback();

                    // Find innermost exception
                    Exception innermost = ex;
                    while (innermost.InnerException != null)
                    {
                        innermost = innermost.InnerException;
                    }

                    // Log detailed error
                    System.Diagnostics.Debug.WriteLine("Order error: " + ex.Message);
                    if (ex.InnerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Inner exception: " + ex.InnerException.Message);
                    }

                    TempData["Error"] = "Đã xảy ra lỗi khi đặt hàng: " + innermost.Message;
                    return RedirectToAction("Index");
                }
            }
        }

        // GET: ThanhToan/DatHangThanhCong
        public ActionResult DatHangThanhCong(string maDonHang)
        {
            // If maDonHang not in parameters, try to get from Session
            if (string.IsNullOrEmpty(maDonHang) && Session["DanhSachMaDonHang"] != null)
            {
                maDonHang = Session["DanhSachMaDonHang"].ToString();
                Session.Remove("DanhSachMaDonHang"); // Clear session after use
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
                // Get current user ID
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để tiếp tục." });
                }

                // Get product information
                var sanPham = db.SanPhams
                    .Include(s => s.NguoiBan)
                    .Include(s => s.AnhSanPhams)
                    .FirstOrDefault(s => s.MaSanPham == maSanPham);

                if (sanPham == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin sản phẩm!" });
                }

                // Check stock
                if (sanPham.SoLuongTonKho < soLuong)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Số lượng tồn kho không đủ. Hiện tại chỉ còn {sanPham.SoLuongTonKho} sản phẩm."
                    });
                }

                // Create temporary cart item
                var tempCartItem = new GioHang
                {
                    MaNguoiDung = maNguoiDung,
                    MaSanPham = maSanPham,
                    SoLuong = soLuong,
                    NgayTao = DateTime.Now,
                    SanPham = sanPham
                };

                // Save to session
                List<GioHang> cartItems = new List<GioHang> { tempCartItem };
                List<int> selectedIds = new List<int> { 0 }; // Use temporary ID

                Session["CartItems"] = cartItems;
                Session["SelectedItems"] = selectedIds;
                Session["IsMuaNgay"] = true; // Mark as Buy Now flow

                // Return success and redirect URL
                return Json(new
                {
                    success = true,
                    redirectUrl = Url.Action("Index", "ThanhToan")
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("BuyNow error: " + ex.Message);
                return Json(new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi xử lý yêu cầu. Vui lòng thử lại sau."
                });
            }
        }

        // Phương thức nhận và xử lý kết quả thanh toán từ VNPay
        public async Task<ActionResult> KetQuaThanhToan()
        {
            try
            {
                // Debug log
                System.Diagnostics.Debug.WriteLine("Received callback from VNPAY");

                // Get parameters from VNPAY
                string vnp_ResponseCode = Request.QueryString["vnp_ResponseCode"];
                string vnp_TransactionStatus = Request.QueryString["vnp_TransactionStatus"];
                string vnp_TxnRef = Request.QueryString["vnp_TxnRef"]; // First order ID
                string vnp_Amount = Request.QueryString["vnp_Amount"];
                string vnp_TransactionNo = Request.QueryString["vnp_TransactionNo"]; // VNPAY transaction ID
                string vnp_PayDate = Request.QueryString["vnp_PayDate"]; // Payment time
                string vnp_BankCode = Request.QueryString["vnp_BankCode"]; // Bank code
                string vnp_CardType = Request.QueryString["vnp_CardType"]; // Card type
                string vnp_SecureHash = Request.QueryString["vnp_SecureHash"];

                // Log received parameters
                System.Diagnostics.Debug.WriteLine("ResponseCode: " + vnp_ResponseCode);
                System.Diagnostics.Debug.WriteLine("TxnRef (First order ID): " + vnp_TxnRef);
                System.Diagnostics.Debug.WriteLine("Amount: " + vnp_Amount);
                System.Diagnostics.Debug.WriteLine("VNPAY transaction ID: " + vnp_TransactionNo);

                // Get VNPAY configuration
                string vnp_HashSecret = ConfigurationManager.AppSettings["vnp_HashSecret"];

                // Verify signature
                VnPayLibrary vnpay = new VnPayLibrary();
                foreach (string s in Request.QueryString)
                {
                    // Skip hash for verification
                    if (!s.StartsWith("vnp_SecureHash"))
                    {
                        vnpay.AddResponseData(s, Request.QueryString[s]);
                    }
                }

                bool isValidSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

                // Check payment result
                bool paymentSuccess = isValidSignature && (vnp_ResponseCode == "00");

                if (paymentSuccess)
                {
                    // Use separate try-catch for DB updates
                    try
                    {

                        // Get order ID list from session
                        string danhSachMaDonHang = Session["DanhSachMaDonHang"] as string;

                        // If not in session, use vnp_TxnRef
                        if (string.IsNullOrEmpty(danhSachMaDonHang))
                        {
                            danhSachMaDonHang = vnp_TxnRef;
                            System.Diagnostics.Debug.WriteLine("Order ID list not found in session, using vnp_TxnRef: " + vnp_TxnRef);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Order ID list found in session: " + danhSachMaDonHang);
                        }

                        // Convert order ID string to array
                        var maDonHangList = danhSachMaDonHang.Split(',')
                            .Where(s => !string.IsNullOrEmpty(s))
                            .Select(s => int.Parse(s))
                            .ToList();

                        System.Diagnostics.Debug.WriteLine($"Number of orders to process: {maDonHangList.Count}");

                        //// Process each order
                        foreach (var maDonHang in maDonHangList)
                        {
                            using (var dbTransaction = db.Database.BeginTransaction())
                            {
                                try
                                {
                                    System.Diagnostics.Debug.WriteLine($"Processing order: {maDonHang}");

                                    // 1. Find order
                                    var donHang = db.DonHangs.Find(maDonHang);
                                    System.Diagnostics.Debug.WriteLine($"Find order {maDonHang}: {(donHang != null ? "Success" : "Not found")}");

                                    if (donHang != null)
                                    {
                                        // 2. Update order status
                                        donHang.TrangThaiDonHang = "Đã thanh toán";
                                        db.Entry(donHang).State = EntityState.Modified;
                                        db.SaveChanges();
                                        System.Diagnostics.Debug.WriteLine($"Updated order {maDonHang} successfully");

                                        // 3. Find and update transaction
                                        var giaoDich = db.GiaoDichs.FirstOrDefault(g => g.MaDonHang == maDonHang);
                                        System.Diagnostics.Debug.WriteLine($"Find transaction for order {maDonHang}: {(giaoDich != null ? "Success" : "Not found")}");

                                        if (giaoDich != null)
                                        {
                                            // Update transaction status
                                            giaoDich.TrangThaiGiaoDich = "Đã hoàn thành";

                                            // Update VNPAY transaction ID (only for first order)
                                            if (!string.IsNullOrEmpty(vnp_TransactionNo))
                                            {
                                                if (maDonHang.ToString() == vnp_TxnRef)
                                                {
                                                    giaoDich.MaGiaoDichVNPay = vnp_TransactionNo.Substring(0, Math.Min(vnp_TransactionNo.Length, 50));
                                                }
                                                else
                                                {
                                                    // For other orders, add reference information
                                                    giaoDich.MaGiaoDichVNPay = $"REF-{vnp_TransactionNo.Substring(0, Math.Min(vnp_TransactionNo.Length, 45))}";
                                                }
                                            }

                                            // Update time
                                            giaoDich.NgayGiaoDich = DateTime.Now;

                                            // Update transaction information
                                            string thongTinGD = string.Format(
                                                "Ngân hàng: {0}, Loại thẻ: {1}, Ngày thanh toán: {2}",
                                                vnp_BankCode ?? "Không có",
                                                vnp_CardType ?? "Không có",
                                                vnp_PayDate ?? "Không có"
                                            );
                                            giaoDich.ThongTinGiaoDich = thongTinGD;

                                            db.Entry(giaoDich).State = EntityState.Modified;
                                            db.SaveChanges();

                                            System.Diagnostics.Debug.WriteLine($"Updated transaction for order {maDonHang} successfully");
                                        }


                                        // Sau khi xử lý tất cả đơn hàng thành công, chạy tạo ký quỹ bất đồng bộ
                                        System.Diagnostics.Debug.WriteLine($"Bắt đầu quá trình tạo kí quỹ bất đồng bộ cho các đơn hàng VNPAY");

                                        // Tạo ký quỹ trong một task riêng biệt mà không đợi kết quả
                                        foreach (var orderID in maDonHangList)  // Đổi tên biến từ maDonHang thành orderID
                                        {
                                            int currentOrderID = orderID;  // Tạo biến cục bộ để tránh xung đột
                                            Task.Run(async () => {
                                                try
                                                {
                                                    System.Diagnostics.Debug.WriteLine($"Task bắt đầu tạo kí quỹ cho đơn hàng VNPAY {currentOrderID}");

                                                    // Tạo context mới để tránh lỗi context đã dispose
                                                    using (var newDb = new MedinetDATN())
                                                    {
                                                        var escrowService = new EscrowService();
                                                        var result = await escrowService.CreateEscrowAsync(currentOrderID);
                                                        System.Diagnostics.Debug.WriteLine($"Đã tạo kí quỹ thành công cho đơn hàng VNPAY {currentOrderID}, ID kí quỹ: {result.MaKyQuy}");
                                                    }
                                                }
                                                catch (Exception escrowEx)
                                                {
                                                    System.Diagnostics.Debug.WriteLine($"Lỗi khi tạo kí quỹ cho đơn hàng VNPAY {currentOrderID}: {escrowEx.Message}");
                                                    if (escrowEx.InnerException != null)
                                                    {
                                                        System.Diagnostics.Debug.WriteLine($"Inner exception: {escrowEx.InnerException.Message}");
                                                    }
                                                }
                                            });

                                            // Delay nhỏ giữa các lần tạo task để tránh quá tải
                                            await Task.Delay(200);
                                        }

                                    }

                                    // Commit transaction for this order
                                    dbTransaction.Commit();
                                    System.Diagnostics.Debug.WriteLine($"Transaction committed successfully for order {maDonHang}");
                                }
                                catch (Exception ex)
                                {
                                    // Rollback transaction if error for this order
                                    dbTransaction.Rollback();
                                    // Log chi tiết lỗi
                                    System.Diagnostics.Debug.WriteLine("DbUpdateException: " + ex.Message);

                                    if (ex.InnerException != null)
                                    {
                                        System.Diagnostics.Debug.WriteLine("Inner Exception: " + ex.InnerException.Message);

                                        if (ex.InnerException.InnerException != null)
                                        {
                                            System.Diagnostics.Debug.WriteLine("Inner Inner Exception: " + ex.InnerException.InnerException.Message);
                                        }
                                    }

                                    System.Diagnostics.Debug.WriteLine($"Transaction rollback for order {maDonHang}: " + ex.Message);

                                    // Detailed error logging
                                    Exception currentEx = ex;
                                    int level = 0;
                                    while (currentEx.InnerException != null)
                                    {
                                        currentEx = currentEx.InnerException;
                                        level++;
                                        System.Diagnostics.Debug.WriteLine($"Inner exception level {level}: {currentEx.Message}");
                                    }

                                    // Log error but continue processing next order
                                    System.Diagnostics.Debug.WriteLine($"Error with order {maDonHang} but continuing to process other orders");
                                }
                            }
                        }

                        // Clear session after processing all orders
                        Session.Remove("DanhSachMaDonHang");
                        Session.Remove("MaDonHangVNPAY");

                        TempData["Success"] = "Thanh toán thành công!";
                        return RedirectToAction("DatHangThanhCong", new { maDonHang = danhSachMaDonHang });
                    }
                    catch (Exception dbEx)
                    {
                        System.Diagnostics.Debug.WriteLine("Overall error processing DB: " + dbEx.Message);
                        if (dbEx.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Inner exception: " + dbEx.InnerException.Message);
                        }

                        // Still redirect to success page because payment was OK
                        string danhSachMaDonHang = Session["DanhSachMaDonHang"] as string ?? vnp_TxnRef;
                        TempData["Success"] = "Thanh toán thành công! Hệ thống sẽ cập nhật đơn hàng của bạn trong thời gian sớm nhất.";
                        return RedirectToAction("DatHangThanhCong", new { maDonHang = danhSachMaDonHang });
                    }
                }
                else
                {
                    // Payment failed
                    System.Diagnostics.Debug.WriteLine("Payment failed! Error code: " + vnp_ResponseCode);
                    TempData["Error"] = "Thanh toán không thành công! Mã lỗi: " + vnp_ResponseCode;
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine("Error processing VNPAY callback: " + ex.Message);
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
                    if (item.SanPham.AnhSanPhams != null && item.SanPham.AnhSanPhams.Any())
                    {
                        anhSanPham = item.SanPham.AnhSanPhams.First().DuongDanAnh;
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