using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class GiaoDichController : Controller
    {
        private MedinetDATN db = new MedinetDATN();

        // GET: GiaoDich
        public ActionResult Index(DateTime? tuNgay = null, DateTime? denNgay = null, string trangThai = null, string phuongThuc = null, int page = 1)
        {
            // Thiết lập ngày mặc định nếu không có
            if (!tuNgay.HasValue)
            {
                tuNgay = DateTime.Now.AddMonths(-1);
            }
            if (!denNgay.HasValue)
            {
                denNgay = DateTime.Now;
            }

            // Điều chỉnh denNgay để bao gồm cả ngày cuối
            DateTime denNgayKetThuc = denNgay.Value.Date.AddDays(1).AddSeconds(-1);

            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");
            ViewBag.TrangThai = trangThai;
            ViewBag.PhuongThuc = phuongThuc;

            // Lấy dữ liệu đã lọc cho phân trang
            var filteredData = db.GiaoDichs
                .Include(g => g.DonHang)
                .Where(g => g.NgayGiaoDich >= tuNgay && g.NgayGiaoDich <= denNgayKetThuc);

            if (!string.IsNullOrEmpty(trangThai))
            {
                filteredData = filteredData.Where(g => g.TrangThaiGiaoDich == trangThai);
            }

            if (!string.IsNullOrEmpty(phuongThuc))
            {
                filteredData = filteredData.Where(g => g.PhuongThucThanhToan == phuongThuc);
            }

            // Phân trang
            int pageSize = 10;
            int totalItems = filteredData.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Điều chỉnh page hiện tại nếu nằm ngoài phạm vi
            if (page < 1)
                page = 1;
            if (page > totalPages && totalPages > 0)
                page = totalPages;

            var pagedData = filteredData
                .OrderByDescending(g => g.NgayGiaoDich)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            return View(pagedData);
        }

        // GET: GiaoDich/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GiaoDich giaoDich = db.GiaoDichs.Include(g => g.DonHang).FirstOrDefault(g => g.MaGiaoDich == id);
            if (giaoDich == null)
            {
                return HttpNotFound();
            }
            return View(giaoDich);
        }

        // POST: GiaoDich/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStatus(int id, string trangThai)
        {
            GiaoDich giaoDich = db.GiaoDichs.Find(id);
            if (giaoDich == null)
            {
                return HttpNotFound();
            }

            giaoDich.TrangThaiGiaoDich = trangThai;
            db.SaveChanges();

            return RedirectToAction("Details", new { id = id });
        }

        // GET: GiaoDich/Escrow
        public ActionResult Escrow(DateTime? tuNgay = null, DateTime? denNgay = null, string trangThai = null, int page = 1)
        {
            // Thiết lập ngày mặc định nếu không có
            if (!tuNgay.HasValue)
            {
                tuNgay = DateTime.Now.AddMonths(-1);
            }
            if (!denNgay.HasValue)
            {
                denNgay = DateTime.Now;
            }

            // Điều chỉnh denNgay để bao gồm cả ngày cuối
            DateTime denNgayKetThuc = denNgay.Value.Date.AddDays(1).AddSeconds(-1);

            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");
            ViewBag.TrangThai = trangThai;

            // Lấy dữ liệu đã lọc cho phân trang
            var filteredData = db.Escrows
                .Include(e => e.DonHang)
                .Include(e => e.NguoiBan)
                .Where(e => e.NgayTao >= tuNgay && e.NgayTao <= denNgayKetThuc);

            if (!string.IsNullOrEmpty(trangThai))
            {
                filteredData = filteredData.Where(e => e.TrangThai == trangThai);
            }

            // Phân trang
            int pageSize = 10;
            int totalItems = filteredData.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Điều chỉnh page hiện tại nếu nằm ngoài phạm vi
            if (page < 1)
                page = 1;
            if (page > totalPages && totalPages > 0)
                page = totalPages;

            var pagedData = filteredData
                .OrderByDescending(e => e.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            // Tính tổng cho toàn bộ dữ liệu không phụ thuộc vào bộ lọc
            var allData = db.Escrows;
            ViewBag.TongEscrow = allData.Count();
            ViewBag.DangGiu = allData.Count(e => e.TrangThai == "Đang giữ");
            ViewBag.DaGiaiNgan = allData.Count(e => e.TrangThai == "Đã giải ngân");
            ViewBag.DaHoanTien = allData.Count(e => e.TrangThai == "Đã hoàn tiền");

            return View(pagedData);
        }

        // GET: GiaoDich/EscrowDetails/5
        public ActionResult EscrowDetails(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Escrow escrow = db.Escrows.Include(e => e.DonHang).Include(e => e.NguoiBan).FirstOrDefault(e => e.MaKyQuy == id);
            if (escrow == null)
            {
                return HttpNotFound();
            }
            return View(escrow);
        }

        // POST: GiaoDich/UpdateEscrowStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateEscrowStatus(int id, string trangThai)
        {
            Escrow escrow = db.Escrows.Find(id);
            if (escrow == null)
            {
                return HttpNotFound();
            }

            escrow.TrangThai = trangThai;

            if (trangThai == "Đã giải ngân")
            {
                escrow.NgayGiaiNgan = DateTime.Now;

                // Thêm ghi chép ví cho người bán
                var ghiChepVi = new GhiChepVi
                {
                    MaNguoiBan = escrow.MaNguoiBan,
                    SoTien = escrow.TienChuyenChoNguoiBan,
                    LoaiGiaoDich = "Giải ngân escrow",
                    MoTa = $"Giải ngân đơn hàng #{escrow.MaDonHang}",
                    NgayGiaoDich = DateTime.Now,
                    TrangThai = "Thành công",
                    MaDonHang = escrow.MaDonHang
                };
                db.GhiChepVis.Add(ghiChepVi);
            }

            db.SaveChanges();

            return RedirectToAction("EscrowDetails", new { id = id });
        }

        // GET: GiaoDich/WalletLogs
        public ActionResult WalletLogs(DateTime? tuNgay = null, DateTime? denNgay = null, string loaiGiaoDich = "", int page = 1)
        {
            // Thiết lập ngày mặc định nếu không có
            if (!tuNgay.HasValue)
            {
                tuNgay = DateTime.Now.AddMonths(-1);
            }
            if (!denNgay.HasValue)
            {
                denNgay = DateTime.Now;
            }
            // Điều chỉnh denNgay để bao gồm cả ngày cuối
            DateTime denNgayKetThuc = denNgay.Value.Date.AddDays(1).AddSeconds(-1);
            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");
            ViewBag.LoaiGiaoDich = loaiGiaoDich;

            // Lấy dữ liệu đã lọc cho phân trang
            var filteredData = db.GhiChepVis
                .Where(g => g.NgayGiaoDich >= tuNgay && g.NgayGiaoDich <= denNgay);

            if (!string.IsNullOrEmpty(loaiGiaoDich))
            {
                filteredData = filteredData.Where(g => g.LoaiGiaoDich == loaiGiaoDich);
            }

            // Phân trang
            int pageSize = 10;
            var pagedData = filteredData.OrderByDescending(g => g.NgayGiaoDich)
                                     .Skip((page - 1) * pageSize)
                                     .Take(pageSize)
                                     .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)filteredData.Count() / pageSize);
            ViewBag.TotalItems = filteredData.Count();

            // Tính tổng cho toàn bộ dữ liệu không phụ thuộc vào bộ lọc
            var allData = db.GhiChepVis;
            ViewBag.TotalGiaoDich = allData.Count();
            ViewBag.TotalNapVao = allData.Where(g => g.LoaiGiaoDich == "Nạp tiền ví").Sum(g => (decimal?)g.SoTien) ?? 0;
            ViewBag.TotalRutRa = Math.Abs(allData.Where(g => g.LoaiGiaoDich == "Rút tiền").Sum(g => (decimal?)g.SoTien) ?? 0);
            ViewBag.TotalThanhToan = allData.Where(g => g.LoaiGiaoDich == "Thanh toán đơn hàng").Sum(g => (decimal?)g.SoTien) ?? 0;

            return View(pagedData);
        }

        // GET: GiaoDich/WalletLogDetails/5
        public ActionResult WalletLogDetails(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            GhiChepVi ghiChepVi = db.GhiChepVis.Include(g => g.NguoiBan).Include(g => g.DonHang).FirstOrDefault(g => g.MaGhiChep == id);
            if (ghiChepVi == null)
            {
                return HttpNotFound();
            }
            return View(ghiChepVi);
        }

        // GET: GiaoDich/Dashboard
        public ActionResult Dashboard()
        {
            // Thống kê tổng quan
            ViewBag.TongGiaoDich = db.GiaoDichs.Count();
            ViewBag.GiaoDichHoanThanh = db.GiaoDichs.Count(g => g.TrangThaiGiaoDich == "Đã hoàn thành");
            ViewBag.GiaoDichDangXuLy = db.GiaoDichs.Count(g => g.TrangThaiGiaoDich == "Đang chờ xử lý");
            ViewBag.GiaoDichThatBai = db.GiaoDichs.Count(g => g.TrangThaiGiaoDich == "Không thành công");

            ViewBag.TongDoanhThu = db.GiaoDichs.Where(g => g.TrangThaiGiaoDich == "Đã hoàn thành").Sum(g => (decimal?)g.TongTien) ?? 0;
            ViewBag.TongEscrow = db.Escrows.Count();
            ViewBag.EscrowDangGiu = db.Escrows.Count(e => e.TrangThai == "Đang giữ");
            ViewBag.TongPhiNenTang = db.Escrows.Where(e => e.TrangThai != "Đã hoàn tiền").Sum(e => (decimal?)e.PhiNenTang) ?? 0;

            // Thống kê giao dịch 7 ngày gần đây
            var today = DateTime.Now.Date;
            var last7Days = Enumerable.Range(0, 7).Select(i => today.AddDays(-i)).Reverse().ToList();

            // Sau đó sửa đổi các truy vấn của bạn:
            var giaoDichTheoNgay = last7Days.Select(date => new
            {
                Ngay = date.ToString("dd/MM"),
                TongGiaoDich = db.GiaoDichs.Count(g => DbFunctions.TruncateTime(g.NgayGiaoDich) == date),
                DoanhThu = db.GiaoDichs.Where(g => DbFunctions.TruncateTime(g.NgayGiaoDich) == date && g.TrangThaiGiaoDich == "Đã hoàn thành").Sum(g => (decimal?)g.TongTien) ?? 0
            }).ToList();

            ViewBag.NgayThongKe = string.Join(",", giaoDichTheoNgay.Select(g => $"'{g.Ngay}'"));
            ViewBag.SoGiaoDich = string.Join(",", giaoDichTheoNgay.Select(g => g.TongGiaoDich));
            ViewBag.DoanhThu = string.Join(",", giaoDichTheoNgay.Select(g => g.DoanhThu));

            // Giao dịch gần đây
            ViewBag.GiaoDichGanDay = db.GiaoDichs.Include(g => g.DonHang)
                .OrderByDescending(g => g.NgayGiaoDich)
                .Take(5)
                .ToList();

            return View();
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