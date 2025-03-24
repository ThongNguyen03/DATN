using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class DonHangController : Controller
    {
        private MedinetDATN db = new MedinetDATN();

        #region Người Mua
        // GET: DonHang/DonHangCuaToi - Cho người mua
        public ActionResult DonHangCuaToi()
        {
            try
            {
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Lấy danh sách đơn hàng của người dùng
                var donHangs = db.DonHangs
                    .Include(d => d.NguoiBan)
                    .Include(d => d.ChiTietDonHangs)
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham))
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham.DanhSachAnhSanPham))
                    .Where(d => d.MaNguoiDung == maNguoiDung)
                    .OrderByDescending(d => d.NgayTao)
                    .ToList();

                // Lấy thông tin escrow
                var escrows = db.Escrows
                    .Where(e => donHangs.Select(d => d.MaDonHang).Contains(e.MaDonHang))
                    .ToList();

                ViewBag.Escrows = escrows;

                return View(donHangs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi xem đơn hàng: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách đơn hàng. Vui lòng thử lại sau!";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: DonHang/ChiTiet/5 - Cho người mua
        public ActionResult ChiTiet(int id)
        {
            try
            {
                var donHang = db.DonHangs
                    .Include(d => d.NguoiBan)
                    .Include(d => d.NguoiDung)
                    .Include(d => d.ChiTietDonHangs)
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham))
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham.DanhSachAnhSanPham))
                    .FirstOrDefault(d => d.MaDonHang == id);

                if (donHang == null)
                {
                    return HttpNotFound();
                }

                // Kiểm tra quyền truy cập
                if (donHang.MaNguoiDung != GetCurrentUserId())
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                // Lấy thông tin escrow nếu có
                var escrow = db.Escrows.FirstOrDefault(e => e.MaDonHang == id);
                ViewBag.Escrow = escrow;

                return View(donHang);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi xem chi tiết đơn hàng: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải thông tin đơn hàng. Vui lòng thử lại sau!";
                return RedirectToAction("DonHangCuaToi");
            }
        }

        // POST: DonHang/XacNhanNhanHang/5 - Cho người mua
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> XacNhanNhanHang(int id)
        {
            try
            {
                var donHang = db.DonHangs.Find(id);
                if (donHang == null)
                {
                    return HttpNotFound();
                }

                // Kiểm tra quyền truy cập
                if (donHang.MaNguoiDung != GetCurrentUserId())
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                // Kiểm tra trạng thái đơn hàng
                if (donHang.TrangThaiDonHang != "Đã giao" && donHang.TrangThaiDonHang != "Đã vận chuyển")
                {
                    TempData["Error"] = "Đơn hàng chưa được giao, không thể xác nhận!";
                    return RedirectToAction("ChiTiet", new { id = id });
                }

                // Cập nhật trạng thái đơn hàng
                donHang.TrangThaiDonHang = "Đã xác nhận nhận hàng";
                donHang.DaXacNhanNhanHang = true;
                donHang.NgayCapNhat = DateTime.Now;
                db.Entry(donHang).State = EntityState.Modified;
                db.SaveChanges();

                // Giải ngân tiền cho người bán
                var escrowService = new EscrowService();
                var result = await escrowService.ReleaseEscrow(id);

                if (result)
                {
                    TempData["Success"] = "Đã xác nhận nhận hàng thành công. Người bán sẽ nhận được thanh toán.";
                }
                else
                {
                    TempData["Warning"] = "Đã xác nhận nhận hàng thành công, nhưng có lỗi khi giải ngân cho người bán.";
                }

                return RedirectToAction("ChiTiet", new { id = id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi xác nhận nhận hàng: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi xác nhận nhận hàng. Vui lòng thử lại sau!";
                return RedirectToAction("ChiTiet", new { id = id });
            }
        }

        // POST: DonHang/HuyDonHang/5 - Cho người mua
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HuyDonHang(int id, string lyDoHuy)
        {
            try
            {
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                var donHang = db.DonHangs.Find(id);
                if (donHang == null)
                {
                    return HttpNotFound();
                }

                // Kiểm tra quyền truy cập
                if (donHang.MaNguoiDung != maNguoiDung)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                // Kiểm tra trạng thái đơn hàng có thể hủy không
                if (donHang.TrangThaiDonHang == "Đã vận chuyển" ||
                    donHang.TrangThaiDonHang == "Đã giao" ||
                    donHang.TrangThaiDonHang == "Đã hủy" ||
                    donHang.TrangThaiDonHang == "Đã xác nhận nhận hàng" ||
                    donHang.TrangThaiDonHang == "Đã hoàn thành" ||
                    donHang.TrangThaiDonHang == "Đã thanh toán")
                {
                    TempData["Error"] = "Không thể hủy đơn hàng ở trạng thái hiện tại!";
                    return RedirectToAction("ChiTiet", new { id = id });
                }

                // Cập nhật trạng thái đơn hàng
                donHang.TrangThaiDonHang = "Đã hủy";
                donHang.LyDoHuy = lyDoHuy;
                donHang.NgayHuy = DateTime.Now;
                donHang.NgayCapNhat = DateTime.Now;

                db.Entry(donHang).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Success"] = "Hủy đơn hàng thành công!";
                return RedirectToAction("ChiTiet", new { id = id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi hủy đơn hàng: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi hủy đơn hàng. Vui lòng thử lại sau!";
                return RedirectToAction("ChiTiet", new { id = id });
            }
        }

        // POST: DonHang/DanhGiaDonHang/5 - Cho người mua
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DanhGiaDonHang(int id, int soDiem, string noiDungDanhGia)
        {
            try
            {
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                var donHang = db.DonHangs.Find(id);
                if (donHang == null)
                {
                    return HttpNotFound();
                }

                // Kiểm tra quyền truy cập
                if (donHang.MaNguoiDung != maNguoiDung)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                // Kiểm tra trạng thái đơn hàng có thể đánh giá không
                if (donHang.TrangThaiDonHang != "Đã xác nhận nhận hàng" &&
                    donHang.TrangThaiDonHang != "Đã hoàn thành" &&
                    donHang.TrangThaiDonHang != "Đã thanh toán")
                {
                    TempData["Error"] = "Chỉ có thể đánh giá đơn hàng đã nhận!";
                    return RedirectToAction("ChiTiet", new { id = id });
                }

                //// Kiểm tra xem đã đánh giá trước đó chưa
                //if (donHang.DaDanhGia)
                //{
                //    TempData["Error"] = "Bạn đã đánh giá đơn hàng này rồi!";
                //    return RedirectToAction("ChiTiet", new { id = id });
                //}

                //// Tạo đánh giá mới
                //var danhGia = new DanhGia
                //{
                //    MaDonHang = id,
                //    MaNguoiDung = maNguoiDung,
                //    MaNguoiBan = donHang.MaNguoiBan,
                //    SoDiem = soDiem,
                //    NoiDung = noiDungDanhGia,
                //    NgayTao = DateTime.Now
                //};

                //db.DanhGias.Add(danhGia);

                //// Cập nhật trạng thái đã đánh giá cho đơn hàng
                //donHang.DaDanhGia = true;
                //donHang.NgayCapNhat = DateTime.Now;
                db.Entry(donHang).State = EntityState.Modified;

                db.SaveChanges();

                TempData["Success"] = "Đánh giá đơn hàng thành công!";
                return RedirectToAction("ChiTiet", new { id = id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi đánh giá đơn hàng: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi đánh giá đơn hàng. Vui lòng thử lại sau!";
                return RedirectToAction("ChiTiet", new { id = id });
            }
        }
        #endregion

        #region Người Bán
        // GET: DonHang/DonHangNguoiMua - Cho người bán
        public ActionResult DonHangNguoiMua()
        {
            try
            {
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Lấy thông tin người bán
                var nguoiBan = db.NguoiBans.FirstOrDefault(n => n.MaNguoiDung == maNguoiDung);
                if (nguoiBan == null)
                {
                    TempData["Error"] = "Bạn không có quyền truy cập trang này!";
                    return RedirectToAction("Index", "Home");
                }

                // Lấy danh sách đơn hàng của các người mua đặt từ người bán này
                var donHangs = db.DonHangs
                    .Include(d => d.NguoiDung)
                    .Include(d => d.ChiTietDonHangs)
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham))
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham.DanhSachAnhSanPham))
                    .Where(d => d.MaNguoiBan == nguoiBan.MaNguoiBan)
                    .OrderByDescending(d => d.NgayTao)
                    .ToList();

                // Lấy thông tin escrow của các đơn hàng
                var escrows = db.Escrows
                    .Where(e => donHangs.Select(d => d.MaDonHang).Contains(e.MaDonHang))
                    .ToList();

                ViewBag.Escrows = escrows;

                return View(donHangs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi xem đơn hàng người mua: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách đơn hàng. Vui lòng thử lại sau!";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: DonHang/ChiTietDonHangNguoiMua/5 - Cho người bán
        public ActionResult ChiTietDonHangNguoiMua(int id)
        {
            try
            {
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Lấy thông tin người bán
                var nguoiBan = db.NguoiBans.FirstOrDefault(n => n.MaNguoiDung == maNguoiDung);
                if (nguoiBan == null)
                {
                    TempData["Error"] = "Bạn không có quyền truy cập trang này!";
                    return RedirectToAction("Index", "Home");
                }

                var donHang = db.DonHangs
                    .Include(d => d.NguoiDung)
                    .Include(d => d.ChiTietDonHangs)
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham))
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham.DanhSachAnhSanPham))
                    .FirstOrDefault(d => d.MaDonHang == id && d.MaNguoiBan == nguoiBan.MaNguoiBan);

                if (donHang == null)
                {
                    return HttpNotFound();
                }

                // Lấy thông tin escrow nếu có
                var escrow = db.Escrows.FirstOrDefault(e => e.MaDonHang == id);
                ViewBag.Escrow = escrow;

                // Danh sách trạng thái đơn hàng để hiển thị trong dropdown
                ViewBag.DanhSachTrangThai = new SelectList(
                    new List<string> {
                        "Đang chờ xử lý",
                        "Đã vận chuyển",
                        "Đã giao",
                        "Đã xác nhận nhận hàng",
                        "Đã hoàn thành",
                        "Đã hủy",
                        "Đã thanh toán"
                    },
                    donHang.TrangThaiDonHang);

                return View(donHang);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi xem chi tiết đơn hàng người mua: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải thông tin đơn hàng. Vui lòng thử lại sau!";
                return RedirectToAction("DonHangNguoiMua");
            }
        }

        // POST: DonHang/CapNhatTrangThaiDonHang - Cho người bán
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CapNhatTrangThaiDonHang(int id, string trangThai)
        {
            try
            {
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Lấy thông tin người bán
                var nguoiBan = db.NguoiBans.FirstOrDefault(n => n.MaNguoiDung == maNguoiDung);
                if (nguoiBan == null)
                {
                    TempData["Error"] = "Bạn không có quyền thực hiện chức năng này!";
                    return RedirectToAction("Index", "Home");
                }

                var donHang = db.DonHangs.Find(id);
                if (donHang == null)
                {
                    return HttpNotFound();
                }

                // Kiểm tra quyền truy cập
                if (donHang.MaNguoiBan != nguoiBan.MaNguoiBan)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                // Cập nhật trạng thái đơn hàng
                donHang.TrangThaiDonHang = trangThai;
                donHang.NgayCapNhat = DateTime.Now;

                // Nếu đơn hàng đã giao, cập nhật ngày giao hàng
                if (trangThai == "Đã giao")
                {
                    donHang.NgayGiaoHang = DateTime.Now;
                }

                db.Entry(donHang).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Success"] = "Cập nhật trạng thái đơn hàng thành công!";
                return RedirectToAction("ChiTietDonHangNguoiMua", new { id = id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi cập nhật trạng thái đơn hàng: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật trạng thái đơn hàng. Vui lòng thử lại sau!";
                return RedirectToAction("ChiTietDonHangNguoiMua", new { id = id });
            }
        }

        // POST: DonHang/HuyDonHangNguoiBan/5 - Cho người bán
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HuyDonHangNguoiBan(int id, string lyDoHuy)
        {
            try
            {
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Lấy thông tin người bán
                var nguoiBan = db.NguoiBans.FirstOrDefault(n => n.MaNguoiDung == maNguoiDung);
                if (nguoiBan == null)
                {
                    TempData["Error"] = "Bạn không có quyền thực hiện chức năng này!";
                    return RedirectToAction("Index", "Home");
                }

                var donHang = db.DonHangs.Find(id);
                if (donHang == null)
                {
                    return HttpNotFound();
                }

                // Kiểm tra quyền truy cập
                if (donHang.MaNguoiBan != nguoiBan.MaNguoiBan)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                // Kiểm tra trạng thái đơn hàng có thể hủy không
                if (donHang.TrangThaiDonHang == "Đã vận chuyển" ||
                    donHang.TrangThaiDonHang == "Đã giao" ||
                    donHang.TrangThaiDonHang == "Đã hủy" ||
                    donHang.TrangThaiDonHang == "Đã xác nhận nhận hàng" ||
                    donHang.TrangThaiDonHang == "Đã hoàn thành" ||
                    donHang.TrangThaiDonHang == "Đã thanh toán")
                {
                    TempData["Error"] = "Không thể hủy đơn hàng ở trạng thái hiện tại!";
                    return RedirectToAction("ChiTietDonHangNguoiMua", new { id = id });
                }

                // Cập nhật trạng thái đơn hàng
                donHang.TrangThaiDonHang = "Đã hủy";
                donHang.LyDoHuy = lyDoHuy;
                donHang.NgayHuy = DateTime.Now;
                donHang.NgayCapNhat = DateTime.Now;

                db.Entry(donHang).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Success"] = "Hủy đơn hàng thành công!";
                return RedirectToAction("ChiTietDonHangNguoiMua", new { id = id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi hủy đơn hàng: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi hủy đơn hàng. Vui lòng thử lại sau!";
                return RedirectToAction("ChiTietDonHangNguoiMua", new { id = id });
            }
        }
        #endregion

        #region Admin
        // GET: DonHang/QuanLyDonHang - Cho Admin
        [Authorize(Roles = "Admin")]
        public ActionResult QuanLyDonHang(string trangThai = "", int maNguoiBan = 0, int maNguoiDung = 0, DateTime? tuNgay = null, DateTime? denNgay = null)
        {
            try
            {
                // Lấy danh sách đơn hàng với các điều kiện lọc
                var query = db.DonHangs
                    .Include(d => d.NguoiBan)
                    .Include(d => d.NguoiDung)
                    .Include(d => d.ChiTietDonHangs)
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham))
                    .AsQueryable();

                // Áp dụng các điều kiện lọc
                if (!string.IsNullOrEmpty(trangThai))
                {
                    query = query.Where(d => d.TrangThaiDonHang == trangThai);
                }

                if (maNguoiBan > 0)
                {
                    query = query.Where(d => d.MaNguoiBan == maNguoiBan);
                }

                if (maNguoiDung > 0)
                {
                    query = query.Where(d => d.MaNguoiDung == maNguoiDung);
                }

                if (tuNgay.HasValue)
                {
                    query = query.Where(d => d.NgayTao >= tuNgay.Value);
                }

                if (denNgay.HasValue)
                {
                    query = query.Where(d => d.NgayTao <= denNgay.Value.AddDays(1));
                }

                // Sắp xếp và lấy kết quả
                var donHangs = query.OrderByDescending(d => d.NgayTao).ToList();

                // Lấy danh sách trạng thái đơn hàng để hiển thị trong dropdown
                ViewBag.DanhSachTrangThai = new SelectList(
                    new List<string> {
                "", "Đang chờ xử lý",
                "Đã vận chuyển",
                "Đã giao",
                "Đã xác nhận nhận hàng",
                "Đã hoàn thành",
                "Đã hủy",
                "Đã thanh toán"
                    },
                    trangThai);

                // Lấy danh sách người bán để hiển thị trong dropdown
                ViewBag.DanhSachNguoiBan = new SelectList(
                    db.NguoiBans.Select(n => new { n.MaNguoiBan, TenHienThi = n.TenCuaHang }).ToList(),
                    "MaNguoiBan", "TenHienThi", maNguoiBan);

                // Lấy danh sách người dùng để hiển thị trong dropdown
                ViewBag.DanhSachNguoiDung = new SelectList(
                    db.NguoiDungs.Select(n => new { n.MaNguoiDung, TenHienThi = n.TenNguoiDung + " (" + n.Email + ")" }).ToList(),
                    "MaNguoiDung", "TenHienThi", maNguoiDung);

                // Lưu các tham số lọc vào ViewBag để sử dụng trong view
                ViewBag.TrangThai = trangThai;
                ViewBag.MaNguoiBan = maNguoiBan;
                ViewBag.MaNguoiDung = maNguoiDung;
                ViewBag.TuNgay = tuNgay;
                ViewBag.DenNgay = denNgay;

                return View(donHangs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi quản lý đơn hàng: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải danh sách đơn hàng. Vui lòng thử lại sau!";
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: DonHang/ChiTietAdmin/5 - Cho Admin
        [Authorize(Roles = "Admin")]
        public ActionResult ChiTietAdmin(int id)
        {
            try
            {
                var donHang = db.DonHangs
                    .Include(d => d.NguoiBan)
                    .Include(d => d.NguoiDung)
                    .Include(d => d.ChiTietDonHangs)
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham))
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham.DanhSachAnhSanPham))
                    .FirstOrDefault(d => d.MaDonHang == id);

                if (donHang == null)
                {
                    return HttpNotFound();
                }

                // Lấy thông tin escrow nếu có
                var escrow = db.Escrows.FirstOrDefault(e => e.MaDonHang == id);
                ViewBag.Escrow = escrow;

                // Danh sách trạng thái đơn hàng để hiển thị trong dropdown
                ViewBag.DanhSachTrangThai = new SelectList(
                    new List<string> {
                "Đang chờ xử lý",
                "Đã vận chuyển",
                "Đã giao",
                "Đã xác nhận nhận hàng",
                "Đã hoàn thành",
                "Đã hủy",
                "Đã thanh toán"
                    },
                    donHang.TrangThaiDonHang);

                return View(donHang);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi xem chi tiết đơn hàng: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải thông tin đơn hàng. Vui lòng thử lại sau!";
                return RedirectToAction("QuanLyDonHang");
            }
        }

        // POST: DonHang/CapNhatTrangThaiDonHangAdmin - Cho Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult CapNhatTrangThaiDonHangAdmin(int id, string trangThai)
        {
            try
            {
                var donHang = db.DonHangs.Find(id);
                if (donHang == null)
                {
                    return HttpNotFound();
                }

                // Cập nhật trạng thái đơn hàng
                donHang.TrangThaiDonHang = trangThai;
                donHang.NgayCapNhat = DateTime.Now;

                // Nếu đơn hàng đã giao, cập nhật ngày giao hàng
                if (trangThai == "Đã giao")
                {
                    donHang.NgayGiaoHang = DateTime.Now;
                }

                db.Entry(donHang).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Success"] = "Cập nhật trạng thái đơn hàng thành công!";
                return RedirectToAction("ChiTietAdmin", new { id = id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi cập nhật trạng thái đơn hàng: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật trạng thái đơn hàng. Vui lòng thử lại sau!";
                return RedirectToAction("ChiTietAdmin", new { id = id });
            }
        }

        // POST: DonHang/HuyDonHangAdmin/5 - Cho Admin
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult HuyDonHangAdmin(int id, string lyDoHuy)
        {
            try
            {
                var donHang = db.DonHangs.Find(id);
                if (donHang == null)
                {
                    return HttpNotFound();
                }

                // Kiểm tra trạng thái đơn hàng có thể hủy không
                if (donHang.TrangThaiDonHang == "Đã xác nhận nhận hàng" ||
                    donHang.TrangThaiDonHang == "Đã hoàn thành" ||
                    donHang.TrangThaiDonHang == "Đã thanh toán")
                {
                    TempData["Error"] = "Không thể hủy đơn hàng ở trạng thái hiện tại!";
                    return RedirectToAction("ChiTietAdmin", new { id = id });
                }

                // Cập nhật trạng thái đơn hàng
                donHang.TrangThaiDonHang = "Đã hủy";
                donHang.LyDoHuy = "Bị hủy bởi Admin: " + lyDoHuy;
                donHang.NgayHuy = DateTime.Now;
                donHang.NgayCapNhat = DateTime.Now;

                db.Entry(donHang).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Success"] = "Hủy đơn hàng thành công!";
                return RedirectToAction("ChiTietAdmin", new { id = id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi hủy đơn hàng: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi hủy đơn hàng. Vui lòng thử lại sau!";
                return RedirectToAction("ChiTietAdmin", new { id = id });
            }
        }

        // GET: DonHang/ThongKeDonHang - Cho Admin
        [Authorize(Roles = "Admin")]
        public ActionResult ThongKeDonHang(DateTime? tuNgay = null, DateTime? denNgay = null)
        {
            try
            {
                // Mặc định là thống kê trong 30 ngày gần nhất nếu không có tham số
                if (!tuNgay.HasValue)
                {
                    tuNgay = DateTime.Now.AddDays(-30);
                }

                if (!denNgay.HasValue)
                {
                    denNgay = DateTime.Now;
                }

                // Lấy tất cả đơn hàng trong khoảng thời gian
                var donHangs = db.DonHangs
                    .Where(d => d.NgayTao >= tuNgay.Value && d.NgayTao <= denNgay.Value.AddDays(1))
                    .ToList();

                // Thống kê theo trạng thái
                var thongKeTheoTrangThai = donHangs
                    .GroupBy(d => d.TrangThaiDonHang)
                    .Select(g => new { TrangThai = g.Key, SoLuong = g.Count() })
                    .ToList();
                ViewBag.ThongKeTheoTrangThai = thongKeTheoTrangThai;

                // Thống kê doanh thu
                var doanhThu = donHangs
                    .Where(d => d.TrangThaiDonHang == "Đã xác nhận nhận hàng" ||
                                d.TrangThaiDonHang == "Đã hoàn thành" ||
                                d.TrangThaiDonHang == "Đã thanh toán")
                    .Sum(d => d.TongSoTien);
                ViewBag.TongDoanhThu = doanhThu;

                // Thống kê số lượng đơn hàng theo ngày
                var thongKeTheoNgay = donHangs
                    .GroupBy(d => d.NgayTao.Date)
                    .Select(g => new { Ngay = g.Key, SoLuong = g.Count() })
                    .OrderBy(item => item.Ngay)
                    .ToList();
                ViewBag.ThongKeTheoNgay = thongKeTheoNgay;

                // Lấy top 5 người mua có nhiều đơn hàng nhất
                var topNguoiMua = donHangs
                    .GroupBy(d => d.MaNguoiDung)
                    .Select(g => new {
                        MaNguoiDung = g.Key,
                        SoLuong = g.Count(),
                        TongTien = g.Sum(d => d.TongSoTien)
                    })
                    .OrderByDescending(item => item.SoLuong)
                    .Take(5)
                    .ToList();

                // Lấy thông tin chi tiết của người mua
                var maNguoiDungs = topNguoiMua.Select(t => t.MaNguoiDung).ToList();
                var nguoiDungs = db.NguoiDungs.Where(n => maNguoiDungs.Contains(n.MaNguoiDung)).ToList();

                var topNguoiMuaChiTiet = topNguoiMua.Select(t => new {
                    NguoiDung = nguoiDungs.FirstOrDefault(n => n.MaNguoiDung == t.MaNguoiDung),
                    SoLuong = t.SoLuong,
                    TongTien = t.TongTien
                }).ToList();

                ViewBag.TopNguoiMua = topNguoiMuaChiTiet;

                // Lấy top 5 người bán có nhiều đơn hàng nhất
                var topNguoiBan = donHangs
                    .GroupBy(d => d.MaNguoiBan)
                    .Select(g => new {
                        MaNguoiBan = g.Key,
                        SoLuong = g.Count(),
                        TongTien = g.Sum(d => d.TongSoTien)
                    })
                    .OrderByDescending(item => item.SoLuong)
                    .Take(5)
                    .ToList();

                // Lấy thông tin chi tiết của người bán
                var maNguoiBans = topNguoiBan.Select(t => t.MaNguoiBan).ToList();
                var nguoiBans = db.NguoiBans.Where(n => maNguoiBans.Contains(n.MaNguoiBan)).ToList();

                var topNguoiBanChiTiet = topNguoiBan.Select(t => new {
                    NguoiBan = nguoiBans.FirstOrDefault(n => n.MaNguoiBan == t.MaNguoiBan),
                    SoLuong = t.SoLuong,
                    TongTien = t.TongTien
                }).ToList();

                ViewBag.TopNguoiBan = topNguoiBanChiTiet;

                // Lưu tham số lọc vào ViewBag
                ViewBag.TuNgay = tuNgay;
                ViewBag.DenNgay = denNgay;

                return View();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi thống kê đơn hàng: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải dữ liệu thống kê. Vui lòng thử lại sau!";
                return RedirectToAction("QuanLyDonHang");
            }
        }

        // GET: DonHang/XuatBaoCao - Cho Admin
        [Authorize(Roles = "Admin")]
        public ActionResult XuatBaoCao(DateTime tuNgay, DateTime denNgay)
        {
            try
            {
                // Lấy dữ liệu đơn hàng trong khoảng thời gian
                var donHangs = db.DonHangs
                    .Include(d => d.NguoiBan)
                    .Include(d => d.NguoiDung)
                    .Where(d => d.NgayTao >= tuNgay && d.NgayTao <= denNgay.AddDays(1))
                    .OrderByDescending(d => d.NgayTao)
                    .ToList();

                // Tạo dữ liệu báo cáo
                var doanhThu = donHangs
                    .Where(d => d.TrangThaiDonHang == "Đã xác nhận nhận hàng" ||
                                d.TrangThaiDonHang == "Đã hoàn thành" ||
                                d.TrangThaiDonHang == "Đã thanh toán")
                    .Sum(d => d.TongSoTien);

                var soLuongDonHang = donHangs.Count;
                var soLuongDonThanhCong = donHangs
                    .Count(d => d.TrangThaiDonHang == "Đã xác nhận nhận hàng" ||
                                d.TrangThaiDonHang == "Đã hoàn thành" ||
                                d.TrangThaiDonHang == "Đã thanh toán");
                var soLuongDonHuy = donHangs.Count(d => d.TrangThaiDonHang == "Đã hủy");

                var thongKeTheoNguoiBan = donHangs
                    .GroupBy(d => d.MaNguoiBan)
                    .Select(g => new {
                        NguoiBan = g.First().NguoiBan,
                        SoLuongDon = g.Count(),
                        DoanhThu = g.Where(d => d.TrangThaiDonHang == "Đã xác nhận nhận hàng" ||
                                             d.TrangThaiDonHang == "Đã hoàn thành" ||
                                             d.TrangThaiDonHang == "Đã thanh toán")
                                     .Sum(d => d.TongSoTien)
                    })
                    .OrderByDescending(x => x.DoanhThu)
                    .ToList();

                // Đưa dữ liệu vào ViewBag
                ViewBag.TuNgay = tuNgay;
                ViewBag.DenNgay = denNgay;
                ViewBag.DoanhThu = doanhThu;
                ViewBag.SoLuongDonHang = soLuongDonHang;
                ViewBag.SoLuongDonThanhCong = soLuongDonThanhCong;
                ViewBag.SoLuongDonHuy = soLuongDonHuy;
                ViewBag.ThongKeTheoNguoiBan = thongKeTheoNguoiBan;

                return View(donHangs);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi xuất báo cáo: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi xuất báo cáo. Vui lòng thử lại sau!";
                return RedirectToAction("ThongKeDonHang");
            }
        }
        #endregion

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
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}