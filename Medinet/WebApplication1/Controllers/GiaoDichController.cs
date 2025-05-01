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
        [Authorize]
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
        [Authorize]
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
        [Authorize]
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

            // Tìm đơn hàng liên quan từ mã đơn hàng trong giao dịch
            var donHang = db.DonHangs.Find(giaoDich.MaDonHang);
            if (donHang != null)
            {
                var nguoiban = db.NguoiBans.Find(donHang.MaNguoiBan);
                if(nguoiban != null)
                {
                    // Tạo thông báo cho người bán
                    var thongBaoNguoiBan = new ThongBao
                    {
                        MaNguoiDung = nguoiban.MaNguoiDung, 
                        LoaiThongBao = "GiaoDich",
                        TieuDe = "Trạng thái giao dịch đã thay đổi",
                        TinNhan = $"Giao dịch liên quan đến đơn hàng #{donHang.MaDonHang} đã được cập nhật thành '{trangThai}'.",
                        MucDoQuanTrong = 1, // Thông báo thông thường
                        DuongDanChiTiet = "/GiaoDich/ChiTietGiaoDichNguoiBan/" + giaoDich.MaGiaoDich,
                        NgayTao = DateTime.Now
                    };

                    db.ThongBaos.Add(thongBaoNguoiBan);
                }

            }

            db.SaveChanges();

            return RedirectToAction("Details", new { id = id });
        }

        // GET: GiaoDich/Escrow
        [Authorize]
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
        [Authorize]
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
        [Authorize]
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
        [Authorize]
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
            ViewBag.TotalRutRa = Math.Abs(allData.Where(g => g.LoaiGiaoDich == "Rút tiền" && (g.TrangThai == "Thành công")).Sum(g => (decimal?)g.SoTien) ?? 0);
            ViewBag.TotalThanhToan = allData.Where(g => g.LoaiGiaoDich == "Thanh toán đơn hàng").Sum(g => (decimal?)g.SoTien) ?? 0;

            return View(pagedData);
        }

        // GET: GiaoDich/WalletLogDetails/5
        [Authorize]
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
        [Authorize]
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


        //7/4/2025
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangeWalletLogStatus(int id, string newStatus, string ghiChu)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Nhận yêu cầu: ID={id}, Trạng thái={newStatus}, Ghi chú={ghiChu}");

                // Kiểm tra quyền truy cập - giả sử chỉ admin mới có quyền thay đổi trạng thái
                //if (!User.IsInRole("Admin"))
                //{
                //    System.Diagnostics.Debug.WriteLine("Người dùng không có quyền Admin");
                //    TempData["Error"] = "Bạn không có quyền thực hiện chức năng này!";
                //    return RedirectToAction("WalletLogDetails", new { id = id });
                //}

                // Lấy thông tin ghi chép ví
                System.Diagnostics.Debug.WriteLine($"Đang tìm ghi chép ví với ID={id}");
                var ghiChepVi = await db.GhiChepVis.FindAsync(id);
                if (ghiChepVi == null)
                {
                    System.Diagnostics.Debug.WriteLine($"Không tìm thấy ghi chép ví với ID={id}");
                    return HttpNotFound();
                }
                System.Diagnostics.Debug.WriteLine($"Đã tìm thấy ghi chép ví: ID={id}, TrangThai={ghiChepVi.TrangThai}");

                // Kiểm tra điều kiện thay đổi trạng thái
                if (ghiChepVi.TrangThai != "Đang xử lý" && ghiChepVi.TrangThai != "Chờ xác nhận")
                {
                    System.Diagnostics.Debug.WriteLine($"Không thể thay đổi trạng thái vì giao dịch đã hoàn tất: {ghiChepVi.TrangThai}");
                    TempData["Error"] = "Không thể thay đổi trạng thái của giao dịch đã hoàn tất!";
                    return RedirectToAction("WalletLogDetails", new { id = id });
                }

                // Kiểm tra giá trị trạng thái mới hợp lệ
                if (newStatus != "Thành công" && newStatus != "Không thành công")
                {
                    System.Diagnostics.Debug.WriteLine($"Trạng thái mới không hợp lệ: {newStatus}");
                    TempData["Error"] = "Trạng thái mới không hợp lệ!";
                    return RedirectToAction("WalletLogDetails", new { id = id });
                }

                // Lưu trạng thái cũ để ghi log
                string oldStatus = ghiChepVi.TrangThai;
                System.Diagnostics.Debug.WriteLine($"Trạng thái cũ: {oldStatus}, Trạng thái mới: {newStatus}");

                // Cập nhật trạng thái ghi chép ví hiện tại
                System.Diagnostics.Debug.WriteLine("Đang cập nhật trạng thái ghi chép ví");
                ghiChepVi.TrangThai = newStatus;
                db.Entry(ghiChepVi).State = EntityState.Modified;
                await db.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"Đã cập nhật trạng thái ghi chép ví thành: {ghiChepVi.TrangThai}");

                var nguoiban = db.NguoiBans.Find(ghiChepVi.MaNguoiBan);
                if(nguoiban != null)
                {
                    // Tạo thông báo cho người bán về việc trạng thái ghi chép ví đã thay đổi
                    var thongBaoNguoiBan = new ThongBao
                    {
                        MaNguoiDung = nguoiban.MaNguoiDung,
                        LoaiThongBao = "Vi",
                        TieuDe = $"Trạng thái giao dịch ví đã thay đổi",
                        TinNhan = $"Giao dịch ví #{ghiChepVi.MaGhiChep} của bạn đã được cập nhật thành '{newStatus}'" +
                                (!string.IsNullOrEmpty(ghiChu) ? $". Ghi chú: {ghiChu}" : "."),
                        MucDoQuanTrong = 1, // Thông báo thông thường
                        DuongDanChiTiet = "/GiaoDich/ChiTietGhiChepViNguoiBan/" + ghiChepVi.MaGhiChep,
                        NgayTao = DateTime.Now
                    };
                    db.ThongBaos.Add(thongBaoNguoiBan);
                    System.Diagnostics.Debug.WriteLine($"Đã tạo thông báo cho người bán ID={ghiChepVi.MaNguoiBan}");
                }




                // Xử lý khi giao dịch không thành công
                if (newStatus == "Không thành công")
                {
                    System.Diagnostics.Debug.WriteLine("Xử lý giao dịch không thành công");

                    // Nếu là giao dịch rút tiền, hoàn tiền lại cho người bán
                    if (ghiChepVi.LoaiGiaoDich.Contains("Rút tiền"))
                    {
                        System.Diagnostics.Debug.WriteLine($"Đây là giao dịch rút tiền, MaNguoiBan={ghiChepVi.MaNguoiBan}");
                        var nguoiBan = await db.NguoiBans.FindAsync(ghiChepVi.MaNguoiBan);
                        if (nguoiBan != null)
                        {
                            // Lấy số tiền cần hoàn lại (đổi dấu vì giao dịch rút tiền thường là số âm)
                            decimal refundAmount = Math.Abs(ghiChepVi.SoTien);
                            System.Diagnostics.Debug.WriteLine($"Đã tìm thấy người bán, số dư hiện tại={nguoiBan.SoDuVi}, số tiền hoàn={refundAmount}");

                            // Cập nhật số dư ví
                            nguoiBan.SoDuVi += refundAmount;
                            db.Entry(nguoiBan).State = EntityState.Modified;
                            System.Diagnostics.Debug.WriteLine($"Đã cập nhật số dư ví người bán thành {nguoiBan.SoDuVi}");

                            // Tạo ghi chép hoàn tiền mới
                            var refundRecord = new GhiChepVi
                            {
                                MaNguoiBan = nguoiBan.MaNguoiBan,
                                SoTien = refundAmount, // Số tiền dương (hoàn lại)
                                LoaiGiaoDich = "Hoàn tiền rút",
                                MoTa = $"Hoàn tiền từ giao dịch rút tiền #{ghiChepVi.MaGhiChep} không thành công" +
                                (!string.IsNullOrEmpty(ghiChu) ? $" vì: {ghiChu}" : ""),
                                NgayGiaoDich = DateTime.Now,
                                TrangThai = "Thành công",
                                IP = Request.UserHostAddress
                            };
                            db.GhiChepVis.Add(refundRecord);
                            // Tạo thông báo về việc hoàn tiền
                            var thongBaoHoanTien = new ThongBao
                            {
                                MaNguoiDung = nguoiBan.MaNguoiBan,
                                LoaiThongBao = "Vi",
                                TieuDe = "Hoàn tiền từ giao dịch rút tiền",
                                TinNhan = $"Bạn đã được hoàn {refundAmount:N0}đ từ giao dịch rút tiền #{ghiChepVi.MaGhiChep} không thành công" +
                                        (!string.IsNullOrEmpty(ghiChu) ? $" vì: {ghiChu}" : "."),
                                MucDoQuanTrong = 1, // Thông báo thông thường
                                DuongDanChiTiet = "/GiaoDich/ChiTietGhiChepViNguoiBan/" + ghiChepVi.MaGhiChep,
                                NgayTao = DateTime.Now
                            };
                            db.ThongBaos.Add(thongBaoHoanTien);
                            System.Diagnostics.Debug.WriteLine("Đã tạo thông báo hoàn tiền cho người bán");

                            await db.SaveChangesAsync();
                            System.Diagnostics.Debug.WriteLine("Đã tạo ghi chép hoàn tiền mới");

                            // Ghi log thông tin hoàn tiền
                            System.Diagnostics.Debug.WriteLine($"Đã hoàn {refundAmount:N0}đ cho người bán ID {nguoiBan.MaNguoiBan} từ giao dịch rút tiền không thành công");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Không tìm thấy người bán với ID={ghiChepVi.MaNguoiBan}");
                        }
                    }
                    // Xử lý các loại giao dịch khác khi không thành công nếu cần...
                }
                else if (newStatus == "Thành công")
                {

                    // Nếu cần tạo thông báo đặc biệt cho giao dịch thành công
                    if (ghiChepVi.LoaiGiaoDich.Contains("Nạp tiền"))
                    {
                        var thongBaoNapTien = new ThongBao
                        {
                            MaNguoiDung = nguoiban.MaNguoiDung,
                            LoaiThongBao = "Vi",
                            TieuDe = "Nạp tiền thành công",
                            TinNhan = $"Giao dịch nạp tiền #{ghiChepVi.MaGhiChep} của bạn đã được xác nhận thành công với số tiền {ghiChepVi.SoTien:N0}đ.",
                            MucDoQuanTrong = 1, // Thông báo thông thường
                            DuongDanChiTiet = "/GiaoDich/ChiTietGhiChepViNguoiBan/" + ghiChepVi.MaGhiChep,
                            NgayTao = DateTime.Now
                        };
                        db.ThongBaos.Add(thongBaoNapTien);
                        System.Diagnostics.Debug.WriteLine("Đã tạo thông báo nạp tiền thành công cho người bán");
                    }
                    else if (ghiChepVi.LoaiGiaoDich.Contains("Rút tiền"))
                    {
                        var thongBaoRutTien = new ThongBao
                        {
                            MaNguoiDung = nguoiban.MaNguoiDung,
                            LoaiThongBao = "Vi",
                            TieuDe = "Rút tiền thành công",
                            TinNhan = $"Giao dịch rút tiền #{ghiChepVi.MaGhiChep} của bạn đã được xác nhận thành công với số tiền {Math.Abs(ghiChepVi.SoTien):N0}đ.",
                            MucDoQuanTrong = 1, // Thông báo thông thường
                            DuongDanChiTiet = "/GiaoDich/ChiTietGhiChepViNguoiBan/" + ghiChepVi.MaGhiChep,
                            NgayTao = DateTime.Now
                        };
                        db.ThongBaos.Add(thongBaoRutTien);
                        System.Diagnostics.Debug.WriteLine("Đã tạo thông báo rút tiền thành công cho người bán");
                    }
                }

                await db.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine("Đã lưu tất cả thông báo vào cơ sở dữ liệu");
                // Kiểm tra lại sau khi xử lý
                var checkRecord = await db.GhiChepVis.FindAsync(id);
                System.Diagnostics.Debug.WriteLine($"Kiểm tra sau khi xử lý: ID={id}, TrangThai={checkRecord?.TrangThai ?? "NULL"}");

                TempData["Success"] = $"Đã cập nhật trạng thái ghi chép ví thành {newStatus}!";
                System.Diagnostics.Debug.WriteLine("Đã hoàn thành xử lý, chuyển hướng về trang chi tiết");
                return RedirectToAction("WalletLogDetails", new { id = id });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi cập nhật trạng thái ghi chép ví: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                TempData["Error"] = "Đã xảy ra lỗi khi cập nhật trạng thái ghi chép ví. Vui lòng thử lại sau!";
                return RedirectToAction("WalletLogDetails", new { id = id });
            }
        }
        //7/4/2025


        //9/4/2025

        // GET: GiaoDich/GiaoDichNguoiBan
        [Authorize]
        public ActionResult GiaoDichNguoiBan(DateTime? tuNgay = null, DateTime? denNgay = null, string trangThai = null, string phuongThuc = null, int page = 1)
        {
            int maNguoiBan = LayMaNguoiBanHienTai();
            var nguoiBan = db.NguoiBans.Find(maNguoiBan);

            if (nguoiBan == null)
            {
                return HttpNotFound();
            }

            var viewModel = new NguoiBanGiaoDichViewModel();

            // Thông tin người bán
            viewModel.MaNguoiBan = maNguoiBan;
            viewModel.TenNguoiBan = nguoiBan.TenCuaHang;
            viewModel.SoDuVi = nguoiBan.SoDuVi;

            // Thiết lập bộ lọc
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

            viewModel.TuNgay = tuNgay;
            viewModel.DenNgay = denNgay;
            viewModel.TrangThai = trangThai;
            viewModel.PhuongThuc = phuongThuc;

            // Lấy dữ liệu đã lọc
            var filteredData = db.GiaoDichs
                .Include(g => g.DonHang)
                .Where(g => g.DonHang.MaNguoiBan == maNguoiBan &&
                       g.NgayGiaoDich >= tuNgay && g.NgayGiaoDich <= denNgayKetThuc);

            if (!string.IsNullOrEmpty(trangThai))
            {
                filteredData = filteredData.Where(g => g.TrangThaiGiaoDich == trangThai);
            }

            if (!string.IsNullOrEmpty(phuongThuc))
            {
                filteredData = filteredData.Where(g => g.PhuongThucThanhToan == phuongThuc);
            }

            // Thống kê
            viewModel.TongGiaoDich = filteredData.Count();
            viewModel.GiaoDichHoanThanh = filteredData.Count(g => g.TrangThaiGiaoDich == "Đã hoàn thành");
            viewModel.GiaoDichDangXuLy = filteredData.Count(g => g.TrangThaiGiaoDich == "Đang chờ xử lý");
            viewModel.GiaoDichKhongThanhCong = filteredData.Count(g => g.TrangThaiGiaoDich == "Không thành công");
            viewModel.TongDoanhThu = filteredData.Where(g => g.TrangThaiGiaoDich == "Đã hoàn thành").Sum(g => (decimal?)g.TongTien) ?? 0;

            // Phân trang
            int pageSize = 10;
            int totalItems = filteredData.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Điều chỉnh page hiện tại nếu nằm ngoài phạm vi
            if (page < 1)
                page = 1;
            if (page > totalPages && totalPages > 0)
                page = totalPages;

            viewModel.TatCaGiaoDich = filteredData
                .OrderByDescending(g => g.NgayGiaoDich)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            viewModel.TrangHienTai = page;
            viewModel.TongTrang = totalPages;

            return View(viewModel);
        }

        // GET: GiaoDich/ViNguoiBan
        [Authorize]
        public ActionResult ViNguoiBan(DateTime? tuNgay = null, DateTime? denNgay = null, string loaiGiaoDich = "", int page = 1)
        {
            int maNguoiBan = LayMaNguoiBanHienTai();
            var nguoiBan = db.NguoiBans.Find(maNguoiBan);

            if (nguoiBan == null)
            {
                return HttpNotFound();
            }

            var viewModel = new NguoiBanViTienViewModel();

            // Thông tin người bán
            viewModel.MaNguoiBan = maNguoiBan;
            viewModel.SoDuVi = nguoiBan.SoDuVi;

            // Thiết lập bộ lọc
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

            viewModel.TuNgay = tuNgay;
            viewModel.DenNgay = denNgay;
            viewModel.LoaiGiaoDich = loaiGiaoDich;

            // Lấy dữ liệu đã lọc
            var filteredData = db.GhiChepVis
                .Where(g => g.MaNguoiBan == maNguoiBan &&
                        g.NgayGiaoDich >= tuNgay && g.NgayGiaoDich <= denNgayKetThuc);

            if (!string.IsNullOrEmpty(loaiGiaoDich))
            {
                filteredData = filteredData.Where(g => g.LoaiGiaoDich == loaiGiaoDich);
            }

            // Thống kê
            viewModel.TongGiaoDich = filteredData.Count();
            viewModel.TongNap = filteredData.Where(g => g.LoaiGiaoDich == "Nạp tiền ví" || g.LoaiGiaoDich == "Giải ngân escrow" || g.LoaiGiaoDich == "Hoàn tiền rút")
                                            .Sum(g => (decimal?)g.SoTien) ?? 0;
            viewModel.TongRut = Math.Abs(filteredData.Where(g => g.LoaiGiaoDich == "Rút tiền" && (g.TrangThai == "Thành công" || g.TrangThai == "Đang xử lý"))
                                                  .Sum(g => (decimal?)g.SoTien) ?? 0);

            // Phân trang
            int pageSize = 10;
            int totalItems = filteredData.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Điều chỉnh page hiện tại nếu nằm ngoài phạm vi
            if (page < 1)
                page = 1;
            if (page > totalPages && totalPages > 0)
                page = totalPages;

            viewModel.DanhSachGhiChep = filteredData
                .OrderByDescending(g => g.NgayGiaoDich)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            viewModel.TrangHienTai = page;
            viewModel.TongTrang = totalPages;

            // Tính số tiền tối thiểu phải giữ lại
            var donHangChuaHoanThanh = db.DonHangs
                .Where(d => d.MaNguoiBan == nguoiBan.MaNguoiBan
                    && d.TrangThaiDonHang != "Đã hoàn thành"
                    && d.TrangThaiDonHang != "Đã hủy"
                    && d.PhuongThucThanhToan == "VNPAY")
                .ToList();

            decimal tongTienDonHangChuaHoanThanh = donHangChuaHoanThanh.Sum(d => d.TongSoTien);
            decimal soTienToiThieuPhaiGiuLai = tongTienDonHangChuaHoanThanh * 1.5m;
            ViewBag.SoTienToiThieuPhaiGiuLai = soTienToiThieuPhaiGiuLai;
            return View(viewModel);
        }

        // GET: GiaoDich/ChiTietGiaoDichNguoiBan/5
        [Authorize]
        public ActionResult ChiTietGiaoDichNguoiBan(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int maNguoiBan = LayMaNguoiBanHienTai();

            GiaoDich giaoDich = db.GiaoDichs
                .Include(g => g.DonHang)
                .FirstOrDefault(g => g.MaGiaoDich == id && g.DonHang.MaNguoiBan == maNguoiBan);

            if (giaoDich == null)
            {
                return HttpNotFound();
            }
            ViewBag.MaNguoiBan = maNguoiBan;
            return View(giaoDich);
        }

        // GET: GiaoDich/ChiTietGhiChepViNguoiBan/5
        [Authorize]
        public ActionResult ChiTietGhiChepViNguoiBan(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            int maNguoiBan = LayMaNguoiBanHienTai();

            GhiChepVi ghiChepVi = db.GhiChepVis
                .Include(g => g.DonHang)
                .FirstOrDefault(g => g.MaGhiChep == id && g.MaNguoiBan == maNguoiBan);

            if (ghiChepVi == null)
            {
                return HttpNotFound();
            }
            // Thêm số dư hiện tại vào ViewBag
            var nguoiBan = db.NguoiBans.Find(maNguoiBan);
            ViewBag.SoDuHienTai = nguoiBan != null ? nguoiBan.SoDuVi : 0;
            return View(ghiChepVi);
        }

        // Helper method để lấy ID người bán hiện tại
        private int LayMaNguoiBanHienTai()
        {
            // Lấy username hoặc user id của người đăng nhập hiện tại
            string tenDangNhap = User.Identity.Name;

            // Tìm thông tin người bán dựa vào username
            var nguoiBan = db.NguoiBans.FirstOrDefault(n => n.NguoiDung.Email == tenDangNhap);

            if (nguoiBan == null)
            {
                throw new InvalidOperationException("Không tìm thấy thông tin người bán");
            }

            return nguoiBan.MaNguoiBan;
        }

        private int GetCurrentUserId()
        {
            // Lấy username của người đăng nhập
            string tenDangNhap = User.Identity.Name;

            // Tìm mã người dùng
            var nguoiDung = db.NguoiDungs.FirstOrDefault(n => n.Email == tenDangNhap);

            if (nguoiDung == null)
            {
                throw new InvalidOperationException("Không tìm thấy thông tin người dùng");
            }

            return nguoiDung.MaNguoiDung;
        }
        //9/4/2025
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    //9/4/2025
    public class NguoiBanGiaoDichViewModel
    {
        // Thông tin người bán
        public int MaNguoiBan { get; set; }
        public string TenNguoiBan { get; set; }
        public decimal SoDuVi { get; set; }

        // Thông tin ngân hàng
        public string ThongTinNganHang { get; set; }
        public string SoTaiKhoan { get; set; }
        public string TenChuTaiKhoan { get; set; }

        // Thống kê
        public int TongGiaoDich { get; set; }
        public int GiaoDichHoanThanh { get; set; }
        public int GiaoDichKhongThanhCong { get; set; }
        public int GiaoDichDangXuLy { get; set; }
        public decimal TongDoanhThu { get; set; }
        public int TongKyQuy { get; set; }
        public int KyQuyDangGiu { get; set; }
        public decimal TongPhiNenTang { get; set; }

        // Dữ liệu cho biểu đồ
        public List<string> ThangDoanhThu { get; set; }
        public List<decimal> DoanhThu { get; set; }

        // Danh sách giao dịch
        public List<GiaoDich> GiaoDichGanDay { get; set; }
        public List<GiaoDich> TatCaGiaoDich { get; set; }

        // Dữ liệu phân trang
        public int TongTrang { get; set; }
        public int TrangHienTai { get; set; }

        // Bộ lọc
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public string TrangThai { get; set; }
        public string PhuongThuc { get; set; }
    }

    public class NguoiBanKyQuyViewModel
    {
        public int MaNguoiBan { get; set; }
        public decimal SoDuVi { get; set; }

        // Thống kê
        public int TongKyQuy { get; set; }
        public int DangGiu { get; set; }
        public int DaGiaiNgan { get; set; }
        public int DaHoanTien { get; set; }
        public decimal TongTien { get; set; }
        public decimal TongPhiNenTang { get; set; }

        // Danh sách ký quỹ
        public List<Escrow> DanhSachKyQuy { get; set; }

        // Dữ liệu phân trang
        public int TongTrang { get; set; }
        public int TrangHienTai { get; set; }

        // Bộ lọc
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public string TrangThai { get; set; }
    }

    public class NguoiBanViTienViewModel
    {
        public int MaNguoiBan { get; set; }
        public decimal SoDuVi { get; set; }

        // Thông tin ngân hàng
        public string ThongTinNganHang { get; set; }
        public string SoTaiKhoan { get; set; }
        public string TenChuTaiKhoan { get; set; }

        // Thống kê
        public int TongGiaoDich { get; set; }
        public decimal TongNap { get; set; }
        public decimal TongRut { get; set; }

        // Danh sách ghi chép ví
        public List<GhiChepVi> DanhSachGhiChep { get; set; }

        // Dữ liệu phân trang
        public int TongTrang { get; set; }
        public int TrangHienTai { get; set; }

        // Bộ lọc
        public DateTime? TuNgay { get; set; }
        public DateTime? DenNgay { get; set; }
        public string LoaiGiaoDich { get; set; }
    }
    //9/4/2025
}