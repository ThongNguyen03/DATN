using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using WebApplication1.Models;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Web.UI;
using System.Globalization;
using System.Text;

namespace WebApplication1.Controllers
{
    // ViewModel cho trang Home
    public class HomeViewModel
    {
        public List<DanhMucSanPham> DanhMucSanPhams { get; set; }
        public List<SanPham> SanPhamDeXuats { get; set; }
    }

    // ViewModel cho trang danh sách sản phẩm
    public class ListSanPhamViewModel
    {
        public List<SanPham> SanPhams { get; set; }
        public List<DanhMucSanPham> DanhMucSanPhams { get; set; }
        public int? MaDanhMuc { get; set; }
        public string TenDanhMuc { get; set; }
        public decimal? GiaMin { get; set; }
        public decimal? GiaMax { get; set; }
        public string TimKiem { get; set; }
        public string ThuongHieu { get; set; }
    }

    public class HomeController : Controller
    {
        private MedinetDATN db = new MedinetDATN();
        private string connectionString = "Data Source=DESKTOP-C6TH3H0;Initial Catalog=MediNet;Integrated Security=True";

        // GET: Home/Index - Trang chủ
        //[Authorize]
        public ActionResult Index(int page = 1, int pageSize = 12)
        {
            // Lấy danh sách danh mục sản phẩm
            var danhMucSanPhams = db.DanhMucSanPhams.ToList();

            // Truy vấn sản phẩm đã phê duyệt
            var sanPhamQuery = db.SanPhams.Where(s => s.TrangThai == "Đã phê duyệt")
                                          .OrderByDescending(s => s.SoLuotMua);

            // Tính tổng số sản phẩm và số trang
            int totalItems = sanPhamQuery.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Lấy sản phẩm cho trang hiện tại
            var sanPhamDeXuats = sanPhamQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Tạo ViewModel
            var viewModel = new HomeViewModel
            {
                DanhMucSanPhams = danhMucSanPhams,
                SanPhamDeXuats = sanPhamDeXuats
            };

            // Truyền thông tin phân trang vào ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            return View(viewModel);
        }

        // GET: Home/SanPham - Trang danh sách sản phẩm
        // Phần HomeController - SanPham action
        public ActionResult SanPham(int? maDanhMuc, string timKiem, decimal? giaMin, decimal? giaMax,
                                  string thuongHieu, int page = 1, int pageSize = 9)
        {
            // Lấy query ban đầu
            var sanPhamQuery = db.SanPhams.Where(s => s.TrangThai == "Đã phê duyệt");

            // Lọc theo danh mục
            if (maDanhMuc.HasValue)
            {
                sanPhamQuery = sanPhamQuery.Where(s => s.MaDanhMuc == maDanhMuc.Value);
                ViewBag.TenDanhMuc = db.DanhMucSanPhams.Find(maDanhMuc.Value)?.TenDanhMuc;
            }

            // Lọc theo tên sản phẩm
            if (!string.IsNullOrEmpty(timKiem))
            {
                sanPhamQuery = sanPhamQuery.Where(s => s.TenSanPham.Contains(timKiem));
            }

            // Lọc theo giá
            if (giaMin.HasValue)
            {
                sanPhamQuery = sanPhamQuery.Where(s => s.GiaSanPham >= giaMin.Value);
            }

            if (giaMax.HasValue)
            {
                sanPhamQuery = sanPhamQuery.Where(s => s.GiaSanPham <= giaMax.Value);
            }

            // Lọc theo thương hiệu
            if (!string.IsNullOrEmpty(thuongHieu))
            {
                sanPhamQuery = sanPhamQuery.Where(s => s.ThuongHieu == thuongHieu);
            }

            // Tính tổng số sản phẩm và số trang
            int totalItems = sanPhamQuery.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Lấy sản phẩm cho trang hiện tại
            var sanPhams = sanPhamQuery
                .OrderByDescending(s => s.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Lấy danh sách danh mục để hiển thị trong bộ lọc
            var danhMucSanPhams = db.DanhMucSanPhams.ToList();

            // Lấy danh sách thương hiệu theo danh mục được chọn
            var danhSachThuongHieuQuery = db.SanPhams
                .Where(s => s.TrangThai == "Đã phê duyệt" && !string.IsNullOrEmpty(s.ThuongHieu));

            // Nếu có chọn danh mục, chỉ lấy thương hiệu trong danh mục đó
            if (maDanhMuc.HasValue)
            {
                danhSachThuongHieuQuery = danhSachThuongHieuQuery.Where(s => s.MaDanhMuc == maDanhMuc.Value);
            }

            var danhSachThuongHieu = danhSachThuongHieuQuery
                .Select(s => s.ThuongHieu)
                .Distinct()
                .ToList();

            // Tạo ViewModel
            var viewModel = new ListSanPhamViewModel
            {
                SanPhams = sanPhams,
                DanhMucSanPhams = danhMucSanPhams,
                MaDanhMuc = maDanhMuc,
                TimKiem = timKiem,
                GiaMin = giaMin,
                GiaMax = giaMax,
                TenDanhMuc = ViewBag.TenDanhMuc,
                ThuongHieu = thuongHieu
            };

            // Truyền thông tin phân trang và bộ lọc vào ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.DanhSachThuongHieu = danhSachThuongHieu;

            return View(viewModel);
        }

        // GET: Home/ChiTiet - Trang chi tiết sản phẩm
        // Cập nhật phương thức ChiTiet trong HomeController
        public ActionResult ChiTiet(int id, int? page)
        {
            var sanPham = db.SanPhams
                .Include(s => s.AnhSanPhams)
                .Include(s => s.NguoiBan)
                .Include(s => s.DanhMucSanPham)
                .FirstOrDefault(s => s.MaSanPham == id && s.TrangThai == "Đã phê duyệt");

            if (sanPham == null)
            {
                return HttpNotFound();
            }

            // Lấy TẤT CẢ sản phẩm cùng danh mục (trừ sản phẩm hiện tại) đã được phê duyệt
            // và sắp xếp theo số lượt mua giảm dần
            var sanPhamLienQuan = db.SanPhams
                .Where(s => s.MaDanhMuc == sanPham.MaDanhMuc &&
                       s.MaSanPham != sanPham.MaSanPham &&
                       s.TrangThai == "Đã phê duyệt")
                .OrderByDescending(s => s.SoLuotMua)
                .ToList();
            //1/4/2025 thêm int? page
            // Lấy danh sách đánh giá cho sản phẩm
            var danhGias = db.DanhGiaSanPhams
                .Where(dg => dg.MaSanPham == id)
                .OrderByDescending(dg => dg.NgayTao)
                .Include(dg => dg.NguoiDung)
                .Include(dg => dg.DonHang)
                .ToList();

            // Tính điểm trung bình
            ViewBag.DiemTrungBinh = danhGias.Any()
                ? Math.Round(danhGias.Average(dg => dg.DanhGia ?? 0), 1)
                : 0;

            // Tổng số đánh giá
            ViewBag.TongDanhGia = danhGias.Count;

            // Thống kê đánh giá theo sao - an toàn hơn
            ViewBag.ThongKeDanhGia = new Dictionary<int, int>
            {
                { 1, danhGias.Count(dg => dg.DanhGia == 1) },
                { 2, danhGias.Count(dg => dg.DanhGia == 2) },
                { 3, danhGias.Count(dg => dg.DanhGia == 3) },
                { 4, danhGias.Count(dg => dg.DanhGia == 4) },
                { 5, danhGias.Count(dg => dg.DanhGia == 5) }
            };

            // Phân trang
            int pageSize = 5;
            int pageNumber = (page ?? 1);
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = (int)Math.Ceiling((double)danhGias.Count / pageSize);

            ViewBag.DanhSachDanhGia = danhGias
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
            //1/4/2025
            ViewBag.SanPhamLienQuan = sanPhamLienQuan;

            return View(sanPham);
        }

        // POST: Home/ThemVaoGio - Thêm sản phẩm vào giỏ hàng
        [HttpPost]
        [Authorize]
        public ActionResult ThemVaoGio(int maSanPham, int soLuong)
        {
            // Kiểm tra sản phẩm tồn tại
            var sanPham = db.SanPhams.Find(maSanPham);
            if (sanPham == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            // Kiểm tra số lượng còn hàng
            if (sanPham.SoLuongTonKho < soLuong)
            {
                return Json(new { success = false, message = "Số lượng sản phẩm không đủ" });
            }

            // Lấy thông tin người dùng hiện tại
            var userName = User.Identity.Name;
            var nguoiDung = db.NguoiDungs.FirstOrDefault(n => n.Email == userName);

            if (nguoiDung == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin người dùng" });
            }

            // Kiểm tra giỏ hàng hiện tại của người dùng
            var gioHang = db.GioHangs.FirstOrDefault(g => g.MaNguoiDung == nguoiDung.MaNguoiDung && g.MaSanPham == maSanPham);

            if (gioHang != null)
            {
                // Cập nhật số lượng nếu sản phẩm đã có trong giỏ
                gioHang.SoLuong += soLuong;
            }
            else
            {
                // Thêm mới vào giỏ hàng
                gioHang = new GioHang
                {
                    MaNguoiDung = nguoiDung.MaNguoiDung,
                    MaSanPham = maSanPham,
                    SoLuong = soLuong,
                    NgayTao = DateTime.Now
                };
                db.GioHangs.Add(gioHang);
            }

            db.SaveChanges();

            // Cập nhật số lượng sản phẩm khác nhau trong giỏ hàng
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();
                string cartQuery = @"
            SELECT COUNT(*) as SoLoaiSanPham
            FROM GioHang
            WHERE MaNguoiDung = @MaNguoiDung";

                SqlCommand cartCmd = new SqlCommand(cartQuery, con);
                cartCmd.Parameters.AddWithValue("@MaNguoiDung", nguoiDung.MaNguoiDung);
                int soLoaiSanPham = (int)cartCmd.ExecuteScalar();

                // Lưu vào session để hiển thị trên giao diện
                Session["SoLuongGioHang"] = soLoaiSanPham;

                // Trả về kết quả thành công cùng với số lượng sản phẩm để cập nhật UI
                return Json(new
                {
                    success = true,
                    message = "Đã thêm sản phẩm vào giỏ hàng",
                    cartItemCount = soLoaiSanPham
                });
            }
        }

        // Thêm action mới vào HomeController
        [HttpGet]
        public JsonResult GetThuongHieuTheoDanhMuc(int maDanhMuc)
        {
            var danhSachThuongHieu = db.SanPhams
                .Where(s => s.TrangThai == "Đã phê duyệt" && s.MaDanhMuc == maDanhMuc && !string.IsNullOrEmpty(s.ThuongHieu))
                .Select(s => s.ThuongHieu)
                .Distinct()
                .ToList();

            return Json(danhSachThuongHieu, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetCartCount()
        {
            int count = 0;
            if (Session["SoLuongGioHang"] != null)
            {
                count = Convert.ToInt32(Session["SoLuongGioHang"]);
            }

            return Json(new { count = count }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetSearchSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term))
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            // Tìm kiếm sản phẩm theo từ khóa (chỉ dùng Contains tiêu chuẩn)
            // Sửa phần lấy đường dẫn ảnh trong GetSearchSuggestions
            var productQuery = db.SanPhams
                .Where(s => s.TrangThai == "Đã phê duyệt" && s.TenSanPham.Contains(term))
                .Select(s => new {
                    value = s.TenSanPham,
                    id = s.MaSanPham,
                    image = s.AnhSanPhams.Any() ?
                        (s.AnhSanPhams.FirstOrDefault().DuongDanAnh.StartsWith("/") ?
                            s.AnhSanPhams.FirstOrDefault().DuongDanAnh :
                            "/" + s.AnhSanPhams.FirstOrDefault().DuongDanAnh)
                        : null,
                    price = s.GiaSanPham
                })
                .ToList();

            // Chuẩn hóa từ khóa tìm kiếm cho phần lọc trong bộ nhớ
            string normalizedTerm = RemoveDiacritics(term.ToLower());

            // Lọc thêm kết quả trong bộ nhớ nếu cần 
            var suggestions = productQuery
                .Where(s => RemoveDiacritics(s.value.ToLower()).Contains(normalizedTerm))
                .Take(5)
                .ToList();

            // Tìm kiếm danh mục theo từ khóa (chỉ dùng Contains tiêu chuẩn)
            var categoryQuery = db.DanhMucSanPhams
                .Where(c => c.TenDanhMuc.Contains(term))
                .Select(c => new {
                    value = "Danh mục: " + c.TenDanhMuc,
                    id = c.MaDanhMuc,
                    isCategory = true
                })
                .ToList();

            // Lọc thêm kết quả danh mục trong bộ nhớ
            var categoryTerms = categoryQuery
                .Where(c => RemoveDiacritics(c.value.ToLower()).Contains(normalizedTerm))
                .Take(3)
                .ToList();

            // Kết hợp kết quả
            var productResults = suggestions.Select(s => new {
                label = s.value,
                value = s.value,
                id = s.id,
                image = s.image,
                price = (decimal?)s.price,
                isCategory = false
            });

            var categoryResults = categoryTerms.Select(c => new {
                label = c.value,
                value = c.value,
                id = c.id,
                image = (string)null,
                price = (decimal?)null,
                isCategory = c.isCategory
            });

            var results = productResults.Concat(categoryResults).ToList();
            return Json(results, JsonRequestBehavior.AllowGet);
        }

        // Hàm loại bỏ dấu tiếng Việt để tìm kiếm tốt hơn
        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            // Thay thế đ/Đ
            return sb.ToString()
                .Normalize(NormalizationForm.FormC)
                .Replace('đ', 'd').Replace('Đ', 'D');
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