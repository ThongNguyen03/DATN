using iTextSharp.text.pdf;
using iTextSharp.text;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using WebApplication1.Models;
using WebApplication1.Services;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Configuration;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Data.Entity.Infrastructure;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class DonHangController : Controller
    {
        private MedinetDATN db = new MedinetDATN();

        #region Người Mua
        // GET: DonHang/DonHangCuaToi - Cho người mua
        public ActionResult DonHangCuaToi(string trangThai, string ngayDat, string sapXep)
        {
            try
            {
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Lấy thông tin người dùng để hiển thị trong sidebar
                var nguoiDung = db.NguoiDungs.Find(maNguoiDung);
                if (nguoiDung != null)
                {
                    ViewBag.HoTen = nguoiDung.TenNguoiDung;
                    ViewBag.AnhDaiDien = nguoiDung.AnhDaiDien;
                }

                // Lấy danh sách đơn hàng của người dùng
                var query = db.DonHangs
                    .Where(d => d.MaNguoiDung == maNguoiDung);

                // Lọc theo trạng thái
                if (!string.IsNullOrEmpty(trangThai))
                {
                    query = query.Where(d => d.TrangThaiDonHang == trangThai);
                    ViewBag.TrangThai = trangThai;
                }

                // Lọc theo ngày đặt
                if (!string.IsNullOrEmpty(ngayDat) && int.TryParse(ngayDat, out int soNgay))
                {
                    var ngayBatDau = DateTime.Now.AddDays(-soNgay);
                    query = query.Where(d => d.NgayTao >= ngayBatDau);
                    ViewBag.NgayDat = ngayDat;
                }

                // Sắp xếp
                switch (sapXep)
                {
                    case "cu-nhat":
                        query = query.OrderBy(d => d.NgayTao);
                        break;
                    case "gia-cao":
                        query = query.OrderByDescending(d => d.TongSoTien);
                        break;
                    case "gia-thap":
                        query = query.OrderBy(d => d.TongSoTien);
                        break;
                    default: // moi-nhat là mặc định
                        query = query.OrderByDescending(d => d.NgayTao);
                        break;
                }
                ViewBag.SapXep = string.IsNullOrEmpty(sapXep) ? "moi-nhat" : sapXep;

                // Thực hiện truy vấn và Include các đối tượng liên quan sau khi đã lọc và sắp xếp
                var donHangs = query
                    .Include(d => d.NguoiBan)
                    .Include(d => d.ChiTietDonHangs)
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham))
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham.AnhSanPhams))
                    .ToList();

                // Lấy danh sách mã đơn hàng
                var maDonHangs = donHangs.Select(d => d.MaDonHang).ToList();

                // Lấy thông tin escrow
                var escrows = db.Escrows
                    .Where(e => maDonHangs.Contains(e.MaDonHang))
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
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham.AnhSanPhams))
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

                // Cập nhật trạng thái giao dịch thông qua SQL
                string updateGiaoDichSql = @"
                UPDATE GiaoDich 
                SET TrangThaiGiaoDich = N'Đã hoàn thành' 
                WHERE MaDonHang = @MaDonHang";

                await db.Database.ExecuteSqlCommandAsync(
                    updateGiaoDichSql,
                    new SqlParameter("@MaDonHang", id)
                );


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

        [HttpPost]
        [ValidateAntiForgeryToken]

        //1/4/2025
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
                // Kiểm tra xem đã đánh giá trước đó chưa
                if (donHang.DaDanhGia)
                {
                    TempData["Error"] = "Bạn đã đánh giá đơn hàng này rồi!";
                    return RedirectToAction("ChiTiet", new { id = id });
                }

                // Danh sách các đánh giá sẽ được tạo
                var danhGias = new List<DanhGiaSanPham>();

                // Tạo đánh giá cho từng sản phẩm trong đơn hàng
                foreach (var chiTietDonHang in donHang.ChiTietDonHangs)
                {
                    var danhGia = new DanhGiaSanPham
                    {
                        MaNguoiDung = maNguoiDung,
                        MaSanPham = chiTietDonHang.MaSanPham,
                        MaDonHang = id,
                        MaNguoiBan = donHang.MaNguoiBan,
                        DanhGia = soDiem,
                        BinhLuan = noiDungDanhGia,
                        DaDanhGia = true
                    };
                    danhGias.Add(danhGia);
                }

                // Thêm tất cả các đánh giá
                db.DanhGiaSanPhams.AddRange(danhGias);

                // Cập nhật trạng thái đã đánh giá cho đơn hàng
                donHang.DaDanhGia = true;
                donHang.NgayCapNhat = DateTime.Now;

                try
                {
                    db.SaveChanges();
                    TempData["Success"] = "Đánh giá đơn hàng thành công!";
                    return RedirectToAction("ChiTiet", new { id = id });
                }
                catch (DbUpdateException ex)
                {
                    // Ghi log chi tiết ngoại lệ
                    var innerException = ex.InnerException;
                    while (innerException != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Ngoại lệ bên trong: " + innerException.Message);
                        System.Diagnostics.Debug.WriteLine("Ngăn xếp lỗi: " + innerException.StackTrace);
                        innerException = innerException.InnerException;
                    }

                    // Thay thế phần validate trước đó
                    TempData["Error"] = "Đã xảy ra lỗi khi đánh giá đơn hàng. Chi tiết: " + ex.Message;

                    // Ghi log thêm thông tin về ngoại lệ
                    System.Diagnostics.Debug.WriteLine("Chi tiết lỗi DbUpdateException: " + ex.Message);

                    return RedirectToAction("ChiTiet", new { id = id });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi đánh giá đơn hàng: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi đánh giá đơn hàng. Vui lòng thử lại sau!";
                return RedirectToAction("ChiTiet", new { id = id });
            }
        }
        //1/4/2025
        public FileResult XuatHoaDonPdf(int maDonHang)
        {
            try
            {
                // Lấy thông tin đơn hàng
                var donHang = db.DonHangs
                    .Include(d => d.NguoiDung)
                    .Include(d => d.NguoiBan)
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham))
                    .FirstOrDefault(d => d.MaDonHang == maDonHang);

                if (donHang == null)
                {
                    // Xử lý trường hợp không tìm thấy đơn hàng
                    return null;
                }

                // Kiểm tra quyền truy cập
                int currentUserId = GetCurrentUserId();
                if (donHang.MaNguoiDung != currentUserId)
                {
                    // Người dùng không có quyền truy cập đơn hàng này
                    return null;
                }

                // Tạo file PDF
                using (MemoryStream ms = new MemoryStream())
                {
                    // Tạo document
                    Document document = new Document(PageSize.A4, 25, 25, 25, 25);
                    PdfWriter writer = PdfWriter.GetInstance(document, ms);
                    document.Open();

                    // Thêm font chữ tiếng Việt
                    string fontPath = HostingEnvironment.MapPath("~/fonts/arial.ttf");
                    if (fontPath == null)
                    {
                        // Nếu không tìm thấy font, sử dụng font mặc định
                        fontPath = HostingEnvironment.MapPath("~/Content/fonts/arial.ttf");
                    }

                    // Nếu vẫn không tìm thấy, sử dụng font Times-Roman mặc định
                    BaseFont baseFont;
                    try
                    {
                        baseFont = BaseFont.CreateFont(fontPath, BaseFont.IDENTITY_H, BaseFont.EMBEDDED);
                    }
                    catch
                    {
                        baseFont = BaseFont.CreateFont(BaseFont.TIMES_ROMAN, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                    }

                    // Tạo các font
                    Font fontTitle = new Font(baseFont, 16, Font.BOLD);
                    Font fontHeader = new Font(baseFont, 12, Font.BOLD);
                    Font fontNormal = new Font(baseFont, 10, Font.NORMAL);
                    Font fontBold = new Font(baseFont, 10, Font.BOLD);

                    // Thêm tiêu đề
                    Paragraph title = new Paragraph("HÓA ĐƠN CHI TIẾT", fontTitle);
                    title.Alignment = Element.ALIGN_CENTER;
                    title.SpacingAfter = 20;
                    document.Add(title);

                    // Thêm thông tin đơn hàng
                    PdfPTable infoTable = new PdfPTable(2);
                    infoTable.WidthPercentage = 100;
                    infoTable.SetWidths(new float[] { 1f, 2f });
                    infoTable.SpacingAfter = 20;

                    infoTable.AddCell(CreateCell("Mã đơn hàng:", fontBold, Element.ALIGN_LEFT));
                    infoTable.AddCell(CreateCell(donHang.MaDonHang.ToString(), fontNormal, Element.ALIGN_LEFT));

                    infoTable.AddCell(CreateCell("Ngày đặt hàng:", fontBold, Element.ALIGN_LEFT));
                    infoTable.AddCell(CreateCell(donHang.NgayTao.ToString("dd/MM/yyyy HH:mm"), fontNormal, Element.ALIGN_LEFT));

                    infoTable.AddCell(CreateCell("Trạng thái:", fontBold, Element.ALIGN_LEFT));
                    infoTable.AddCell(CreateCell(donHang.TrangThaiDonHang, fontNormal, Element.ALIGN_LEFT));

                    infoTable.AddCell(CreateCell("Phương thức thanh toán:", fontBold, Element.ALIGN_LEFT));
                    infoTable.AddCell(CreateCell(donHang.PhuongThucThanhToan, fontNormal, Element.ALIGN_LEFT));

                    document.Add(infoTable);

                    // Thông tin người mua và người bán
                    PdfPTable contactTable = new PdfPTable(2);
                    contactTable.WidthPercentage = 100;
                    contactTable.SpacingAfter = 20;

                    // Thông tin người mua
                    PdfPCell buyerCell = new PdfPCell();
                    buyerCell.Border = Rectangle.BOX;
                    buyerCell.Padding = 10;

                    Paragraph buyerHeader = new Paragraph("Thông tin người mua", fontHeader);
                    buyerHeader.SpacingAfter = 10;
                    buyerCell.AddElement(buyerHeader);

                    Paragraph buyerName = new Paragraph("Họ tên: " + donHang.NguoiDung.TenNguoiDung, fontNormal);
                    buyerCell.AddElement(buyerName);

                    Paragraph buyerPhone = new Paragraph("Số điện thoại: " + donHang.NguoiDung.SoDienThoai, fontNormal);
                    buyerCell.AddElement(buyerPhone);

                    Paragraph buyerAddress = new Paragraph("Địa chỉ: " + donHang.NguoiDung.DiaChi, fontNormal);
                    buyerCell.AddElement(buyerAddress);

                    contactTable.AddCell(buyerCell);

                    // Thông tin người bán
                    PdfPCell sellerCell = new PdfPCell();
                    sellerCell.Border = Rectangle.BOX;
                    sellerCell.Padding = 10;

                    Paragraph sellerHeader = new Paragraph("Thông tin người bán", fontHeader);
                    sellerHeader.SpacingAfter = 10;
                    sellerCell.AddElement(sellerHeader);

                    Paragraph sellerName = new Paragraph("Tên cửa hàng: " + donHang.NguoiBan.TenCuaHang, fontNormal);
                    sellerCell.AddElement(sellerName);

                    Paragraph sellerPhone = new Paragraph("Số điện thoại: " + donHang.NguoiBan.SoDienThoaiCuaHang, fontNormal);
                    sellerCell.AddElement(sellerPhone);

                    Paragraph sellerAddress = new Paragraph("Địa chỉ: " + donHang.NguoiBan.DiaChiCuaHang, fontNormal);
                    sellerCell.AddElement(sellerAddress);

                    contactTable.AddCell(sellerCell);
                    document.Add(contactTable);

                    // Bảng chi tiết sản phẩm
                    Paragraph productHeader = new Paragraph("Chi tiết sản phẩm", fontHeader);
                    productHeader.SpacingAfter = 10;
                    document.Add(productHeader);

                    PdfPTable productTable = new PdfPTable(5);
                    productTable.WidthPercentage = 100;
                    productTable.SetWidths(new float[] { 0.5f, 2f, 0.7f, 1f, 1f });
                    productTable.SpacingAfter = 20;

                    // Thêm header cho bảng sản phẩm
                    productTable.AddCell(CreateCell("STT", fontBold, Element.ALIGN_CENTER));
                    productTable.AddCell(CreateCell("Tên sản phẩm", fontBold, Element.ALIGN_CENTER));
                    productTable.AddCell(CreateCell("Số lượng", fontBold, Element.ALIGN_CENTER));
                    productTable.AddCell(CreateCell("Đơn giá", fontBold, Element.ALIGN_CENTER));
                    productTable.AddCell(CreateCell("Thành tiền", fontBold, Element.ALIGN_CENTER));

                    // Thêm chi tiết sản phẩm
                    int stt = 1;
                    foreach (var item in donHang.ChiTietDonHangs)
                    {
                        productTable.AddCell(CreateCell(stt.ToString(), fontNormal, Element.ALIGN_CENTER));
                        productTable.AddCell(CreateCell(item.SanPham.TenSanPham, fontNormal, Element.ALIGN_LEFT));
                        productTable.AddCell(CreateCell(item.SoLuong.ToString(), fontNormal, Element.ALIGN_CENTER));
                        productTable.AddCell(CreateCell(String.Format("{0:N0} VNĐ", item.Gia), fontNormal, Element.ALIGN_RIGHT));
                        productTable.AddCell(CreateCell(String.Format("{0:N0} VNĐ", item.SoLuong * item.Gia), fontNormal, Element.ALIGN_RIGHT));
                        stt++;
                    }

                    document.Add(productTable);

                    // Bảng tổng tiền
                    PdfPTable totalTable = new PdfPTable(2);
                    totalTable.WidthPercentage = 100;
                    totalTable.SetWidths(new float[] { 4f, 1f });

                    totalTable.AddCell(CreateCell("Tổng tiền hàng:", fontBold, Element.ALIGN_RIGHT));
                    totalTable.AddCell(CreateCell(String.Format("{0:N0} VNĐ", donHang.TongSoTien), fontBold, Element.ALIGN_RIGHT));

                    document.Add(totalTable);

                    // Thêm chữ ký số và ghi chú
                    Paragraph note = new Paragraph("\nGhi chú: Hóa đơn này đã được xuất tự động từ hệ thống Medinet.", fontNormal);
                    note.Alignment = Element.ALIGN_LEFT;
                    document.Add(note);

                    // Thêm thời gian xuất
                    Paragraph exportDate = new Paragraph("\nNgày xuất hóa đơn: " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), fontNormal);
                    exportDate.Alignment = Element.ALIGN_RIGHT;
                    document.Add(exportDate);

                    document.Close();
                    writer.Close();

                    // Trả về file PDF
                    return File(ms.ToArray(), "application/pdf", "HoaDon_" + donHang.MaDonHang + ".pdf");
                }
            }
            catch (Exception ex)
            {
                // Log lỗi
                System.Diagnostics.Debug.WriteLine("Lỗi xuất PDF: " + ex.Message);
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine("Inner exception: " + ex.InnerException.Message);
                }

                // Chuyển hướng về trang chi tiết với thông báo lỗi
                TempData["Error"] = "Đã xảy ra lỗi khi xuất hóa đơn!";
                return null;
            }
        }

        // Phương thức hỗ trợ tạo cell trong PDF
        private PdfPCell CreateCell(string text, Font font, int alignment)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font));
            cell.HorizontalAlignment = alignment;
            cell.Padding = 5;
            return cell;
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

                ViewBag.MaNguoiBan = nguoiBan.MaNguoiBan;

                // Lấy danh sách đơn hàng của các người mua đặt từ người bán này - phương pháp tối ưu hóa
                var maNguoiBan = nguoiBan.MaNguoiBan;
                var donHangs = db.DonHangs
                    .Where(d => d.MaNguoiBan == maNguoiBan)
                    .ToList();

                // Sau khi có danh sách đơn hàng cơ bản, mới lấy thêm thông tin chi tiết
                if (donHangs.Any())
                {
                    // Lấy danh sách ID đơn hàng
                    var donHangIds = donHangs.Select(d => d.MaDonHang).ToList();

                    // Lấy thông tin người dùng
                    var nguoiDungIds = donHangs.Select(d => d.MaNguoiDung).Distinct().ToList();
                    var nguoiDungs = db.NguoiDungs.Where(n => nguoiDungIds.Contains(n.MaNguoiDung)).ToList();

                    // Lấy chi tiết đơn hàng
                    var chiTietDonHangs = db.ChiTietDonHangs
                        .Where(c => donHangIds.Contains(c.MaDonHang))
                        .Include(c => c.SanPham)
                        .Include(c => c.SanPham.AnhSanPhams)
                        .ToList();

                    // Lấy thông tin escrow
                    var escrows = db.Escrows
                        .Where(e => donHangIds.Contains(e.MaDonHang))
                        .ToList();

                    ViewBag.Escrows = escrows;

                    // Gán các đối tượng liên quan vào đơn hàng
                    foreach (var donHang in donHangs)
                    {
                        donHang.NguoiDung = nguoiDungs.FirstOrDefault(n => n.MaNguoiDung == donHang.MaNguoiDung);
                        donHang.ChiTietDonHangs = chiTietDonHangs.Where(c => c.MaDonHang == donHang.MaDonHang).ToList();
                    }
                }

                return View(donHangs.OrderByDescending(d => d.NgayTao).ToList());
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

                ViewBag.MaNguoiBan = nguoiBan.MaNguoiBan;

                // Lấy thông tin đơn hàng cơ bản
                var donHang = db.DonHangs.FirstOrDefault(d => d.MaDonHang == id && d.MaNguoiBan == nguoiBan.MaNguoiBan);

                if (donHang == null)
                {
                    return HttpNotFound();
                }

                // Lấy thông tin người dùng
                donHang.NguoiDung = db.NguoiDungs.FirstOrDefault(n => n.MaNguoiDung == donHang.MaNguoiDung);

                // Lấy chi tiết đơn hàng
                donHang.ChiTietDonHangs = db.ChiTietDonHangs
                    .Where(c => c.MaDonHang == id)
                    .ToList();

                // Lấy thông tin sản phẩm và ảnh sản phẩm
                foreach (var chiTiet in donHang.ChiTietDonHangs)
                {
                    chiTiet.SanPham = db.SanPhams.FirstOrDefault(s => s.MaSanPham == chiTiet.MaSanPham);
                    if (chiTiet.SanPham != null)
                    {
                        chiTiet.SanPham.AnhSanPhams = db.AnhSanPhams
                            .Where(a => a.MaSanPham == chiTiet.MaSanPham)
                            .ToList();
                    }
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
        public async Task<ActionResult> CapNhatTrangThaiDonHang(int id, string trangThai, HttpPostedFileBase anhXacNhan)
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

                //30/3/2025
                // Nếu trạng thái mới là "Đã giao", yêu cầu người bán phải tải lên ảnh xác nhận
                if (trangThai == "Đã giao")
                {
                    // Kiểm tra và lưu ảnh xác nhận
                    if (anhXacNhan == null || anhXacNhan.ContentLength == 0)
                    {
                        TempData["Error"] = "Vui lòng tải lên ảnh xác nhận giao hàng!";
                        return RedirectToAction("ChiTietDonHangNguoiMua", new { id = id });
                    }

                    // Xử lý lưu tệp hình ảnh
                    string fileName = Path.GetFileNameWithoutExtension(anhXacNhan.FileName)
                        + "_" + DateTime.Now.ToString("yyyyMMddHHmmss")
                        + Path.GetExtension(anhXacNhan.FileName);

                    string uploadPath = Server.MapPath("~/Content/images/confirmations/");

                    // Đảm bảo thư mục tồn tại
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    string filePath = Path.Combine(uploadPath, fileName);
                    anhXacNhan.SaveAs(filePath);

                    // Lưu đường dẫn ảnh vào đơn hàng
                    donHang.AnhXacNhanGiaoHang = "/Content/images/confirmations/" + fileName;
                }
                //30/3/2025
                //28/03/2025 thêm async Task<ActionResult> vào trước action
                // Đối với các chuyển đổi trạng thái yêu cầu đặt cọc, kiểm tra số dư
                if ((donHang.TrangThaiDonHang == "Đang chờ xử lý" || donHang.TrangThaiDonHang == "Đã thanh toán") &&
                    trangThai == "Đã vận chuyển" &&
                    donHang.PhuongThucThanhToan == "COD")
                {
                    // Kiểm tra số dư đặt cọc
                    var escrowService = new EscrowService();
                    var ketQua = await escrowService.KiemTraSoDuNguoiBanChiTietAsync(nguoiBan.MaNguoiBan, donHang.TongSoTien);
                    if (!ketQua.DuTien)
                    {
                        decimal soDuHienTai = nguoiBan.SoDuVi;
                        decimal soTienThieu = ketQua.SoTienYeuCau - soDuHienTai;
                        TempData["Error"] = $"Không đủ số dư để đặt cọc. Bạn cần nạp thêm ít nhất {String.Format("{0:N0}", soTienThieu)} VNĐ để xử lý đơn hàng này.";
                        return RedirectToAction("ChiTietDonHangNguoiMua", new { id = id });
                    }

                    // Xử lý đặt cọc
                    bool datCocThanhCong = await escrowService.XuLyDatCocDonHangAsync(id);
                    if (!datCocThanhCong)
                    {
                        TempData["Error"] = "Có lỗi xảy ra khi xử lý đặt cọc. Vui lòng thử lại sau.";
                        return RedirectToAction("ChiTietDonHangNguoiMua", new { id = id });
                    }
                }
                //28/03/2025

                // Cập nhật trạng thái đơn hàng
                donHang.TrangThaiDonHang = trangThai;
                donHang.NgayCapNhat = DateTime.Now;

                // Nếu đơn hàng đã giao, cập nhật ngày giao hàng
                if (trangThai == "Đã giao")
                {
                    donHang.NgayGiaoHang = DateTime.Now;
                    //30/3/2025
                    donHang.ThoiGianTuDongXacNhan = DateTime.Now.AddDays(3);
                    //30/3/2025
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> HuyDonHangNguoiBan(int id, string lyDoHuy)
        {
            try
            {
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Lấy thông tin người bán
                var nguoiBan = await db.NguoiBans.FirstOrDefaultAsync(n => n.MaNguoiDung == maNguoiDung);
                if (nguoiBan == null)
                {
                    TempData["Error"] = "Bạn không có quyền thực hiện chức năng này!";
                    return RedirectToAction("Index", "Home");
                }

                var donHang = await db.DonHangs.FindAsync(id);
                if (donHang == null)
                {
                    return HttpNotFound();
                }

                // Kiểm tra quyền truy cập
                if (donHang.MaNguoiBan != nguoiBan.MaNguoiBan)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                // THÊM ĐIỀU KIỆN: Chỉ hủy được đơn hàng thanh toán COD
                if (donHang.PhuongThucThanhToan != "COD")
                {
                    TempData["Error"] = "Chỉ có thể hủy đơn hàng thanh toán bằng phương thức COD!";
                    return RedirectToAction("ChiTietDonHangNguoiMua", new { id = id });
                }

                // Kiểm tra trạng thái đơn hàng có thể hủy không
                // Chỉ cho phép hủy khi đơn hàng chưa vận chuyển
                if (donHang.TrangThaiDonHang != "Đang chờ xử lý" &&
                    donHang.TrangThaiDonHang != "Chờ thanh toán")
                {
                    TempData["Error"] = "Không thể hủy đơn hàng ở trạng thái hiện tại!";
                    return RedirectToAction("ChiTietDonHangNguoiMua", new { id = id });
                }

                // Cập nhật trạng thái đơn hàng
                donHang.TrangThaiDonHang = "Đã hủy";
                donHang.LyDoHuy = "Bị hủy bởi người bán: " + lyDoHuy; // Thêm thông tin người hủy
                donHang.NgayHuy = DateTime.Now;
                donHang.NgayCapNhat = DateTime.Now;

                db.Entry(donHang).State = EntityState.Modified;
                await db.SaveChangesAsync();

                // Hoàn tiền đặt cọc cho người bán
                var escrowService = new EscrowService();
                await escrowService.ProcessOrderCancellationAsync(id, lyDoHuy);

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


        // Cập nhật phương thức HuyDonHang để sử dụng async/await
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> HuyDonHang(int id, string lyDoHuy)
        {
            try
            {
                int maNguoiDung = GetCurrentUserId();
                if (maNguoiDung <= 0)
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                var donHang = await db.DonHangs.FindAsync(id);
                if (donHang == null)
                {
                    return HttpNotFound();
                }

                // Kiểm tra quyền truy cập
                if (donHang.MaNguoiDung != maNguoiDung)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                // THÊM ĐIỀU KIỆN: Chỉ hủy được đơn hàng thanh toán COD
                if (donHang.PhuongThucThanhToan != "COD")
                {
                    TempData["Error"] = "Chỉ có thể hủy đơn hàng thanh toán bằng phương thức COD!";
                    return RedirectToAction("ChiTiet", new { id = id });
                }

                // Kiểm tra trạng thái đơn hàng có thể hủy không
                // Chỉ cho phép hủy khi đơn hàng chưa vận chuyển
                if (donHang.TrangThaiDonHang != "Đang chờ xử lý" &&
                    donHang.TrangThaiDonHang != "Chờ thanh toán")
                {
                    TempData["Error"] = "Không thể hủy đơn hàng ở trạng thái hiện tại!";
                    return RedirectToAction("ChiTiet", new { id = id });
                }

                // Cập nhật trạng thái đơn hàng
                donHang.TrangThaiDonHang = "Đã hủy";
                donHang.LyDoHuy = "Bị hủy bởi người mua: " + lyDoHuy; // Thêm thông tin người hủy
                donHang.NgayHuy = DateTime.Now;
                donHang.NgayCapNhat = DateTime.Now;

                db.Entry(donHang).State = EntityState.Modified;
                await db.SaveChangesAsync();

                // Xử lý hoàn tiền đặt cọc nếu cần
                var escrowService = new EscrowService();
                await escrowService.ProcessOrderCancellationAsync(id, lyDoHuy);

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

        // Cập nhật phương thức HuyDonHangAdmin để sử dụng async/await
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<ActionResult> HuyDonHangAdmin(int id, string lyDoHuy)
        {
            try
            {
                var donHang = await db.DonHangs.FindAsync(id);
                if (donHang == null)
                {
                    return HttpNotFound();
                }

                // THÊM ĐIỀU KIỆN: Chỉ hủy được đơn hàng thanh toán COD
                if (donHang.PhuongThucThanhToan != "COD")
                {
                    TempData["Error"] = "Chỉ có thể hủy đơn hàng thanh toán bằng phương thức COD!";
                    return RedirectToAction("ChiTietAdmin", new { id = id });
                }

                // Kiểm tra trạng thái đơn hàng có thể hủy không
                // Chỉ cho phép hủy khi đơn hàng chưa vận chuyển
                if (donHang.TrangThaiDonHang != "Đang chờ xử lý" &&
                    donHang.TrangThaiDonHang != "Chờ thanh toán")
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
                await db.SaveChangesAsync();

                // Xử lý hoàn tiền đặt cọc nếu cần
                var escrowService = new EscrowService();
                await escrowService.ProcessOrderCancellationAsync(id, lyDoHuy);

                TempData["Success"] = "Hủy đơn hàng thành công!";
                return RedirectToAction("QuanLyDonHang");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi hủy đơn hàng: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi hủy đơn hàng. Vui lòng thử lại sau!";
                return RedirectToAction("QuanLyDonHang");
            }
        }

        //27/03/2025

        #endregion




        #region Admin
        // GET: DonHang/QuanLyDonHang - Cho Admin
        [Authorize]
        public ActionResult QuanLyDonHang(string trangThai = "", int maNguoiBan = 0, int maNguoiDung = 0,
                                            DateTime? tuNgay = null, DateTime? denNgay = null, string sortOrder = "date_desc", int page = 1)
        {
            try
            {
                int pageSize = 10; // Số lượng đơn hàng trên mỗi trang

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

                // Sắp xếp theo các tiêu chí
                switch (sortOrder)
                {
                    case "date_asc":
                        query = query.OrderBy(d => d.NgayTao);
                        break;
                    case "price_desc":
                        query = query.OrderByDescending(d => d.TongSoTien);
                        break;
                    case "price_asc":
                        query = query.OrderBy(d => d.TongSoTien);
                        break;
                    default: // date_desc là mặc định
                        query = query.OrderByDescending(d => d.NgayTao);
                        break;
                }

                // Tính toán phân trang
                int totalItems = query.Count();
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                // Đảm bảo trang hiện tại không vượt quá tổng số trang
                if (page > totalPages && totalPages > 0)
                {
                    page = totalPages;
                }
                else if (page < 1)
                {
                    page = 1;
                }

                // Lấy đơn hàng theo trang
                var donHangs = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Lấy danh sách trạng thái đơn hàng để hiển thị trong dropdown
                ViewBag.DanhSachTrangThai = new SelectList(
                    new List<string> {
                "", "Đang chờ xử lý",
                "Chờ thanh toán",
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
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.SortOrder = sortOrder;

                // Lưu các thông tin sắp xếp
                ViewBag.DateSortParam = sortOrder == "date_desc" ? "date_asc" : "date_desc";
                ViewBag.PriceSortParam = sortOrder == "price_desc" ? "price_asc" : "price_desc";

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
        [Authorize]
        public ActionResult ChiTietAdmin(int id)
        {
            try
            {
                var donHang = db.DonHangs
                    .Include(d => d.NguoiBan)
                    .Include(d => d.NguoiDung)
                    .Include(d => d.ChiTietDonHangs)
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham))
                    .Include(d => d.ChiTietDonHangs.Select(c => c.SanPham.AnhSanPhams))
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
        [Authorize]
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

        [Authorize]
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