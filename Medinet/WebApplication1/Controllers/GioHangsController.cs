using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;
using System.Threading.Tasks;

namespace WebApplication1.Controllers
{
    [Authorize] // Yêu cầu đăng nhập để truy cập giỏ hàng
    public class GioHangsController : Controller
    {
        private MedinetDATN db = new MedinetDATN();

        // GET: GioHangs - Hiển thị giỏ hàng của người dùng hiện tại
        public ActionResult Index()
        {
            try
            {
                // Lấy ID người dùng hiện tại
                int maNguoiDung = GetCurrentUserId();

                // Kiểm tra nếu không tìm thấy người dùng, chuyển hướng đến trang đăng nhập
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Lấy thông tin giỏ hàng
                var viewModel = GetGioHangViewModel(maNguoiDung);

                // Cập nhật Session để hiển thị số lượng giỏ hàng trên biểu tượng
                Session["SoLuongGioHang"] = viewModel.CartItems.Count;

                // Lấy địa chỉ người dùng
                var nguoiDung = db.NguoiDungs.Find(maNguoiDung);
                if (nguoiDung != null && !string.IsNullOrEmpty(nguoiDung.DiaChi))
                {
                    // Phân tách địa chỉ thành các thành phần (giả sử định dạng: Quận/Huyện, Thành phố/Tỉnh)
                    string[] diaChiParts = nguoiDung.DiaChi.Split(',');
                    if (diaChiParts.Length > 1)
                    {
                        viewModel.DiaChi = diaChiParts[diaChiParts.Length - 1].Trim();
                    }
                    else
                    {
                        viewModel.DiaChi = nguoiDung.DiaChi;
                    }
                }
                else
                {
                    viewModel.DiaChi = "Chưa có địa chỉ";
                }

                // Thêm xử lý số điện thoại
                viewModel.SoDienThoai = !string.IsNullOrEmpty(nguoiDung?.SoDienThoai)
                    ? nguoiDung.SoDienThoai
                    : "Chưa có số điện thoại";

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine("Lỗi Index: " + ex.Message);

                // Khởi tạo ViewModel rỗng để tránh lỗi
                return View(new GioHangViewModel
                {
                    CartItems = new List<CartItemViewModel>(),
                    ShopGroups = new List<ShopCartGroup>(),
                    TongTienHang = 0,
                    PhiVanChuyen = 30000,
                    GiamGiaVanChuyen = -30000,
                    GiamGiaVoucher = 0,
                    TongThanhToan = 0,
                    DiaChi = "Chưa có địa chỉ",
                    SoDienThoai = "Chưa có số điện thoại",
                });
            }
        }

        // POST: GioHangs/ThemVaoGio - Thêm sản phẩm vào giỏ hàng
        [HttpPost]
        public ActionResult ThemVaoGio(int maSanPham, int soLuong = 1)
        {
            int maNguoiDung = GetCurrentUserId();

            // Kiểm tra xem sản phẩm có tồn tại không
            var sanPham = db.SanPhams.Find(maSanPham);
            if (sanPham == null)
            {
                TempData["Error"] = "Sản phẩm không tồn tại!";
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra số lượng tồn kho
            if (decimal.Parse(sanPham.SoLuongTonKho.ToString()) < soLuong)
            {
                TempData["Error"] = "Số lượng sản phẩm trong kho không đủ!";
                return RedirectToAction("ChiTiet", "SanPham", new { id = maSanPham });
            }

            // Kiểm tra xem sản phẩm đã có trong giỏ hàng chưa
            var gioHangItem = db.GioHangs
                .FirstOrDefault(g => g.MaNguoiDung == maNguoiDung && g.MaSanPham == maSanPham);

            if (gioHangItem != null)
            {
                // Nếu đã có, tăng số lượng
                gioHangItem.SoLuong += soLuong;
                db.Entry(gioHangItem).State = EntityState.Modified;
            }
            else
            {
                // Nếu chưa có, tạo mới
                var gioHangMoi = new GioHang
                {
                    MaNguoiDung = maNguoiDung,
                    MaSanPham = maSanPham,
                    SoLuong = soLuong,
                    NgayTao = DateTime.Now
                };
                db.GioHangs.Add(gioHangMoi);
            }

            db.SaveChanges();
            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng!";

            // Nếu là yêu cầu Ajax, trả về JSON
            if (Request.IsAjaxRequest())
            {
                var soLuongGioHang = db.GioHangs
                    .Where(g => g.MaNguoiDung == maNguoiDung)
                    .Sum(g => g.SoLuong);

                return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng!", soLuongGioHang });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: GioHangs/CapNhatSoLuong - Cập nhật số lượng sản phẩm trong giỏ hàng
        [HttpPost]
        public ActionResult CapNhatSoLuong(int maGioHang, int soLuong)
        {
            var gioHangItem = db.GioHangs.Find(maGioHang);
            if (gioHangItem == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra người dùng có quyền cập nhật mục này không
            if (gioHangItem.MaNguoiDung != GetCurrentUserId())
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            // Kiểm tra số lượng tồn kho
            var sanPham = db.SanPhams.Find(gioHangItem.MaSanPham);
            if (sanPham != null && decimal.Parse(sanPham.SoLuongTonKho.ToString()) < soLuong)
            {
                return Json(new { success = false, message = "Số lượng sản phẩm trong kho không đủ!" });
            }

            if (soLuong <= 0)
            {
                db.GioHangs.Remove(gioHangItem);
            }
            else
            {
                gioHangItem.SoLuong = soLuong;
                db.Entry(gioHangItem).State = EntityState.Modified;
            }

            db.SaveChanges();

            // Nếu là yêu cầu Ajax, tính toán lại tổng tiền và trả về JSON
            if (Request.IsAjaxRequest())
            {
                decimal thanhTien = 0;
                if (soLuong > 0 && sanPham != null)
                {
                    thanhTien = soLuong * sanPham.GiaSanPham;
                }

                return Json(new
                {
                    success = true,
                    thanhTien = thanhTien.ToString("N0"),
                    maGioHang = maGioHang
                });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: GioHangs/XoaSanPham - Xóa sản phẩm khỏi giỏ hàng
        [HttpPost]
        public ActionResult XoaSanPham(int maGioHang)
        {
            var gioHangItem = db.GioHangs.Find(maGioHang);
            if (gioHangItem == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra người dùng có quyền xóa mục này không
            if (gioHangItem.MaNguoiDung != GetCurrentUserId())
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            db.GioHangs.Remove(gioHangItem);
            db.SaveChanges();

            // Nếu là yêu cầu Ajax, tính toán lại tổng tiền và trả về JSON
            if (Request.IsAjaxRequest())
            {
                int maNguoiDung = GetCurrentUserId();
                var viewModel = GetGioHangViewModel(maNguoiDung);

                return Json(new
                {
                    success = true,
                    tongTienHang = viewModel.TongTienHang.ToString("N0"),
                    tongThanhToan = viewModel.TongThanhToan.ToString("N0"),
                    soLuongGioHang = viewModel.CartItems?.Sum(i => i.SoLuong) ?? 0
                });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: GioHangs/XoaNhieu - Xóa nhiều sản phẩm cùng lúc
        [HttpPost]
        public ActionResult XoaNhieu(int[] maGioHangs)
        {
            if (maGioHangs == null || maGioHangs.Length == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int maNguoiDung = GetCurrentUserId();

            foreach (var maGioHang in maGioHangs)
            {
                var gioHangItem = db.GioHangs.Find(maGioHang);
                if (gioHangItem != null && gioHangItem.MaNguoiDung == maNguoiDung)
                {
                    db.GioHangs.Remove(gioHangItem);
                }
            }

            db.SaveChanges();

            if (Request.IsAjaxRequest())
            {
                var viewModel = GetGioHangViewModel(maNguoiDung);
                return Json(new
                {
                    success = true,
                    tongTienHang = viewModel.TongTienHang.ToString("N0"),
                    tongThanhToan = viewModel.TongThanhToan.ToString("N0"),
                    soLuongGioHang = viewModel.CartItems?.Sum(i => i.SoLuong) ?? 0
                });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: GioHangs/ApDungVoucher - Áp dụng mã giảm giá
        [HttpPost]
        public ActionResult ApDungVoucher(string maVoucher)
        {
            int maNguoiDung = GetCurrentUserId();

            // TODO: Thêm logic xử lý voucher thực tế từ cơ sở dữ liệu
            // Đây là mẫu giả định
            decimal giamGia = 0;
            bool voucherHopLe = false;

            if (!string.IsNullOrEmpty(maVoucher))
            {
                // Kiểm tra voucher trong cơ sở dữ liệu (giả định)
                if (maVoucher.ToUpper() == "MEDINET10")
                {
                    giamGia = 10000;
                    voucherHopLe = true;
                }
                else if (maVoucher.ToUpper() == "MEDINET20")
                {
                    giamGia = 20000;
                    voucherHopLe = true;
                }

                if (voucherHopLe)
                {
                    // Lưu voucher đã sử dụng vào session
                    Session["AppliedVoucher"] = maVoucher;
                    Session["VoucherDiscount"] = giamGia.ToString();
                }
            }

            if (Request.IsAjaxRequest())
            {
                var viewModel = GetGioHangViewModel(maNguoiDung);
                viewModel.GiamGiaVoucher = giamGia;

                return Json(new
                {
                    success = voucherHopLe,
                    message = voucherHopLe ? "Áp dụng voucher thành công!" : "Voucher không hợp lệ hoặc đã hết hạn!",
                    giamGiaVoucher = giamGia.ToString("N0"),
                    tongThanhToan = viewModel.TongThanhToan.ToString("N0")
                });
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: GioHangs/TienHanhThanhToan - Tiến hành thanh toán chỉ với các sản phẩm được chọn
        [HttpPost]
        public ActionResult TienHanhThanhToan(string selectedItems, string paymentMethod = "COD")
        {
            int maNguoiDung = GetCurrentUserId();

            // Kiểm tra nếu không có sản phẩm nào được chọn
            if (string.IsNullOrEmpty(selectedItems))
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một sản phẩm để thanh toán!";
                return RedirectToAction(nameof(Index));
            }

            // Chuyển đổi danh sách ID từ chuỗi sang mảng số nguyên
            var selectedIds = selectedItems.Split(',').Select(int.Parse).ToList();

            // Lấy các mục đã chọn từ giỏ hàng
            var cartItems = db.GioHangs
                .Where(g => g.MaNguoiDung == maNguoiDung && selectedIds.Contains(g.MaGioHang))
                .Include(g => g.SanPham)
                .Include(g => g.SanPham.NguoiBan)
                .Include(g => g.SanPham.DanhSachAnhSanPham)
                .ToList();

            if (cartItems.Count == 0)
            {
                TempData["Error"] = "Không tìm thấy sản phẩm đã chọn trong giỏ hàng!";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra địa chỉ và số điện thoại của người dùng
            var nguoiDung = db.NguoiDungs.Find(maNguoiDung);
            if (nguoiDung == null || string.IsNullOrEmpty(nguoiDung.DiaChi) || string.IsNullOrEmpty(nguoiDung.SoDienThoai))
            {
                TempData["Error"] = "Vui lòng cập nhật địa chỉ giao hàng và số điện thoại trước khi thanh toán!";
                return RedirectToAction(nameof(Index));
            }

            // Lưu thông tin giỏ hàng đã chọn vào session để sử dụng ở trang thanh toán
            Session["CartItems"] = cartItems;
            Session["SelectedItems"] = selectedIds;
            Session["PaymentMethod"] = paymentMethod;

            // Lưu phương thức thanh toán
            if (!string.IsNullOrEmpty(paymentMethod))
            {
                Session["PaymentMethod"] = paymentMethod;
            }

            return RedirectToAction("Index", "ThanhToan");
        }

        // GET: GioHangs/ThanhToanTrucTiep - Thanh toán trực tiếp từ giỏ hàng
        [HttpGet]
        public ActionResult ThanhToanTrucTiep(string paymentMethod = "COD")
        {
            try
            {
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Lấy tất cả sản phẩm trong giỏ hàng
                var cartItems = db.GioHangs
                    .Where(g => g.MaNguoiDung == maNguoiDung)
                    .Include(g => g.SanPham)
                    .Include(g => g.SanPham.NguoiBan)
                    .Include(g => g.SanPham.DanhSachAnhSanPham)
                    .ToList();

                if (cartItems.Count == 0)
                {
                    TempData["Error"] = "Giỏ hàng của bạn đang trống!";
                    return RedirectToAction(nameof(Index));
                }

                // Lưu thông tin vào session
                Session["CartItems"] = cartItems;
                Session["SelectedItems"] = cartItems.Select(c => c.MaGioHang).ToList();
                Session["PaymentMethod"] = paymentMethod;

                return RedirectToAction("Index", "ThanhToan");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi ThanhToanTrucTiep: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi xử lý thanh toán. Vui lòng thử lại sau!";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: GioHangs/ChuyenSangThanhToanVNPay - Chuyển đổi sang thanh toán VNPAY trực tiếp
        [HttpGet]
        public ActionResult ChuyenSangThanhToanVNPay()
        {
            try
            {
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Lấy thông tin người dùng
                var nguoiDung = db.NguoiDungs.Find(maNguoiDung);
                if (nguoiDung == null || string.IsNullOrEmpty(nguoiDung.DiaChi) || string.IsNullOrEmpty(nguoiDung.SoDienThoai))
                {
                    TempData["Error"] = "Vui lòng cập nhật địa chỉ giao hàng và số điện thoại trước khi thanh toán!";
                    return RedirectToAction(nameof(Index));
                }

                // Lấy tất cả sản phẩm trong giỏ hàng
                var cartItems = db.GioHangs
                    .Where(g => g.MaNguoiDung == maNguoiDung)
                    .Include(g => g.SanPham)
                    .Include(g => g.SanPham.NguoiBan)
                    .Include(g => g.SanPham.DanhSachAnhSanPham)
                    .ToList();

                if (cartItems.Count == 0)
                {
                    TempData["Error"] = "Giỏ hàng của bạn đang trống!";
                    return RedirectToAction(nameof(Index));
                }

                // Lưu thông tin vào session
                Session["CartItems"] = cartItems;
                Session["SelectedItems"] = cartItems.Select(c => c.MaGioHang).ToList();
                Session["PaymentMethod"] = "VNPAY";

                return RedirectToAction("Index", "ThanhToan");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi ChuyenSangThanhToanVNPay: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi chuyển sang thanh toán VNPAY. Vui lòng thử lại sau!";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: GioHangs/GetCartCount - API lấy số lượng sản phẩm trong giỏ hàng
        [HttpGet]
        public JsonResult GetCartCount()
        {
            int count = 0;

            try
            {
                // Lấy ID người dùng hiện tại
                int maNguoiDung = GetCurrentUserId();

                if (maNguoiDung > 0)
                {
                    // Đếm số lượng mục trong giỏ hàng
                    count = db.GioHangs.Count(g => g.MaNguoiDung == maNguoiDung);

                    // Cập nhật session
                    Session["SoLuongGioHang"] = count;
                }
                else if (Session["SoLuongGioHang"] != null)
                {
                    // Nếu không lấy được ID người dùng, thử lấy từ session
                    count = Convert.ToInt32(Session["SoLuongGioHang"]);
                }
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine("Lỗi GetCartCount: " + ex.Message);
            }

            return Json(new { count = count }, JsonRequestBehavior.AllowGet);
        }

        // POST: GioHangs/CapNhatDiaChi - Cập nhật địa chỉ người dùng
        [HttpPost]
        public ActionResult CapNhatDiaChi(string diaChi, string soDienThoai)
        {
            try
            {
                // Lấy ID người dùng hiện tại
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thực hiện chức năng này!" });
                }

                // Kiểm tra dữ liệu đầu vào
                if (string.IsNullOrEmpty(diaChi))
                {
                    return Json(new { success = false, message = "Địa chỉ không được để trống!" });
                }

                // Kiểm tra số điện thoại
                if (string.IsNullOrEmpty(soDienThoai) || !System.Text.RegularExpressions.Regex.IsMatch(soDienThoai, @"^[0-9]{10}$"))
                {
                    return Json(new { success = false, message = "Số điện thoại không hợp lệ!" });
                }

                // Tìm người dùng trong cơ sở dữ liệu
                var nguoiDung = db.NguoiDungs.Find(maNguoiDung);
                if (nguoiDung == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người dùng!" });
                }

                // Cập nhật thông tin địa chỉ và số điện thoại
                nguoiDung.DiaChi = diaChi;
                nguoiDung.SoDienThoai = soDienThoai;

                // Lưu thay đổi vào cơ sở dữ liệu
                db.Entry(nguoiDung).State = EntityState.Modified;
                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = "Cập nhật địa chỉ và số điện thoại thành công!",
                    diaChi = diaChi,
                    soDienThoai = soDienThoai
                });
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine("Lỗi CapNhatDiaChi: " + ex.Message);
                return Json(new { success = false, message = "Đã xảy ra lỗi khi cập nhật thông tin. Vui lòng thử lại sau!" });
            }
        }

        // Phương thức lấy ID người dùng hiện tại
        private int GetCurrentUserId()
        {
            // Lấy Email người dùng đã đăng nhập
            var userName = User.Identity.Name;

            // Tìm người dùng trong cơ sở dữ liệu theo Email
            var nguoiDung = db.NguoiDungs.FirstOrDefault(n => n.Email == userName);

            if (nguoiDung != null)
            {
                return nguoiDung.MaNguoiDung;
            }

            // Nếu không tìm thấy người dùng, thử lấy từ Session
            if (Session["MaNguoiDung"] != null)
            {
                return Convert.ToInt32(Session["MaNguoiDung"]);
            }

            // Nếu vẫn không có, trả về giá trị mặc định
            return 0; // Trả về 0 để biểu thị không tìm thấy người dùng
        }

        // Phương thức lấy thông tin giỏ hàng cho ViewModel
        private GioHangViewModel GetGioHangViewModel(int maNguoiDung)
        {
            try
            {
                // Debug: Kiểm tra maNguoiDung
                System.Diagnostics.Debug.WriteLine("MaNguoiDung: " + maNguoiDung);

                // Lấy các mục trong giỏ hàng của người dùng
                var gioHangItems = db.GioHangs
                    .Where(g => g.MaNguoiDung == maNguoiDung)
                    .Include(g => g.SanPham)
                    .Include(g => g.SanPham.NguoiBan)
                    .Include(g => g.SanPham.DanhSachAnhSanPham)
                    .ToList();

                // Debug: Kiểm tra số lượng mục trong giỏ hàng
                System.Diagnostics.Debug.WriteLine("Số lượng mục trong giỏ hàng: " + gioHangItems.Count);

                // Chuyển đổi danh sách GioHang thành CartItemViewModel
                var cartItems = new List<CartItemViewModel>();

                foreach (var g in gioHangItems)
                {
                    // Kiểm tra sản phẩm có tồn tại không
                    if (g.SanPham == null)
                    {
                        continue; // Bỏ qua nếu không tìm thấy sản phẩm
                    }

                    // Lấy đường dẫn ảnh đầu tiên hoặc ảnh mặc định
                    string anhSanPham = "/Content/images/no-image.jpeg";
                    if (g.SanPham.DanhSachAnhSanPham != null && g.SanPham.DanhSachAnhSanPham.Any())
                    {
                        anhSanPham = g.SanPham.DanhSachAnhSanPham.First().DuongDanAnh;
                    }

                    // Lấy tên cửa hàng
                    string tenCuaHang = "Medinet Shop";
                    if (g.SanPham.NguoiBan != null)
                    {
                        tenCuaHang = g.SanPham.NguoiBan.TenCuaHang;
                    }

                    cartItems.Add(new CartItemViewModel
                    {
                        MaGioHang = g.MaGioHang,
                        MaSanPham = g.MaSanPham,
                        TenSanPham = g.SanPham.TenSanPham,
                        SoLuong = g.SoLuong,
                        GiaSanPham = g.SanPham.GiaSanPham,
                        ThanhTien = g.SoLuong * g.SanPham.GiaSanPham,
                        AnhSanPham = anhSanPham,
                        TenCuaHang = tenCuaHang
                    });
                }

                // Tính toán tổng tiền
                decimal tongTienHang = cartItems.Sum(i => i.ThanhTien);
                decimal phiVanChuyen = cartItems.Any() ? 30000 : 0; // Phí vận chuyển cố định
                decimal giamGiaVanChuyen = phiVanChuyen > 0 ? -phiVanChuyen : 0; // Giảm giá vận chuyển

                // Lấy giảm giá voucher từ session nếu có
                decimal giamGiaVoucher = 0;
                if (Session["AppliedVoucher"] != null)
                {
                    decimal.TryParse(Session["VoucherDiscount"]?.ToString(), out giamGiaVoucher);
                }

                // Lấy thông tin địa chỉ của người dùng
                var nguoiDung = db.NguoiDungs.Find(maNguoiDung);
                string diaChi = "Chưa có địa chỉ";
                string soDienThoai = "Chưa có số điện thoại";

                if (nguoiDung != null)
                {
                    if (!string.IsNullOrEmpty(nguoiDung.DiaChi))
                    {
                        diaChi = nguoiDung.DiaChi;
                    }

                    if (!string.IsNullOrEmpty(nguoiDung.SoDienThoai))
                    {
                        soDienThoai = nguoiDung.SoDienThoai;
                    }
                }

                // Phân nhóm giỏ hàng theo cửa hàng
                var groupedCartItems = cartItems.GroupBy(i => i.TenCuaHang)
                    .Select(g => new ShopCartGroup
                    {
                        TenCuaHang = g.Key,
                        Items = g.ToList()
                    }).ToList();

                var viewModel = new GioHangViewModel
                {
                    CartItems = cartItems,
                    ShopGroups = groupedCartItems,
                    TongTienHang = tongTienHang,
                    PhiVanChuyen = phiVanChuyen,
                    GiamGiaVanChuyen = giamGiaVanChuyen,
                    GiamGiaVoucher = giamGiaVoucher,
                    TongThanhToan = tongTienHang + phiVanChuyen + giamGiaVanChuyen - giamGiaVoucher,
                    DiaChi = diaChi,
                    SoDienThoai = soDienThoai
                };

                return viewModel;
            }
            catch (Exception ex)
            {
                // Ghi log lỗi
                System.Diagnostics.Debug.WriteLine("Lỗi GetGioHangViewModel: " + ex.Message);

                // Trả về ViewModel rỗng để tránh lỗi
                return new GioHangViewModel
                {
                    CartItems = new List<CartItemViewModel>(),
                    ShopGroups = new List<ShopCartGroup>(),
                    TongTienHang = 0,
                    PhiVanChuyen = 0,
                    GiamGiaVanChuyen = 0,
                    GiamGiaVoucher = 0,
                    TongThanhToan = 0,
                    DiaChi = "Chưa có địa chỉ",
                    SoDienThoai = "Chưa có số điện thoại"
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
    }

    // View Models
    public class CartItemViewModel
    {
        public int MaGioHang { get; set; }
        public int MaSanPham { get; set; }
        public string TenSanPham { get; set; }
        public int SoLuong { get; set; }
        public decimal GiaSanPham { get; set; }
        public decimal ThanhTien { get; set; }
        public string AnhSanPham { get; set; }
        public string TenCuaHang { get; set; }
    }

    public class ShopCartGroup
    {
        public string TenCuaHang { get; set; }
        public List<CartItemViewModel> Items { get; set; }
    }

    public class GioHangViewModel
    {
        public List<CartItemViewModel> CartItems { get; set; }
        public List<ShopCartGroup> ShopGroups { get; set; }
        public decimal TongTienHang { get; set; }
        public decimal PhiVanChuyen { get; set; }
        public decimal GiamGiaVanChuyen { get; set; }
        public decimal GiamGiaVoucher { get; set; }
        public decimal TongThanhToan { get; set; }
        public string DiaChi { get; set; }
        public string GhiChu { get; set; }
        public string SoDienThoai { get; set; }
    }
}