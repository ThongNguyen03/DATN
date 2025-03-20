using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.IO;
using WebApplication1.Models;
using System.Globalization;

namespace WebApplication1.Controllers
{
    public class NguoiBansController : Controller
    {
        private MedinetDATN db = new MedinetDATN();

        // GET: NguoiBans
        public ActionResult Index()
        {
            var nguoiBans = db.NguoiBans.Include(n => n.NguoiDung);
            return View(nguoiBans.ToList());
        }

        // GET: NguoiBans/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NguoiBan nguoiBan = db.NguoiBans.Find(id);
            if (nguoiBan == null)
            {
                return HttpNotFound();
            }
            return View(nguoiBan);
        }

        // GET: NguoiBans/Create
        public ActionResult Create()
        {
            ViewBag.MaNguoiDung = new SelectList(db.NguoiDungs, "MaNguoiDung", "TenNguoiDung");
            return View();
        }

        // POST: NguoiBans/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaNguoiBan,MaNguoiDung,TenCuaHang,MoTaCuaHang,DiaChiCuaHang,SoDienThoaiCuaHang,NgayTao")] NguoiBan nguoiBan)
        {
            if (ModelState.IsValid)
            {
                db.NguoiBans.Add(nguoiBan);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.MaNguoiDung = new SelectList(db.NguoiDungs, "MaNguoiDung", "TenNguoiDung", nguoiBan.MaNguoiDung);
            return View(nguoiBan);
        }

        // GET: NguoiBans/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NguoiBan nguoiBan = db.NguoiBans.Find(id);
            if (nguoiBan == null)
            {
                return HttpNotFound();
            }
            ViewBag.MaNguoiDung = new SelectList(db.NguoiDungs, "MaNguoiDung", "TenNguoiDung", nguoiBan.MaNguoiDung);
            return View(nguoiBan);
        }

        // POST: NguoiBans/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaNguoiBan,MaNguoiDung,TenCuaHang,MoTaCuaHang,DiaChiCuaHang,SoDienThoaiCuaHang,NgayTao")] NguoiBan nguoiBan)
        {
            if (ModelState.IsValid)
            {
                db.Entry(nguoiBan).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.MaNguoiDung = new SelectList(db.NguoiDungs, "MaNguoiDung", "TenNguoiDung", nguoiBan.MaNguoiDung);
            return View(nguoiBan);
        }

        // GET: NguoiBans/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NguoiBan nguoiBan = db.NguoiBans.Find(id);
            if (nguoiBan == null)
            {
                return HttpNotFound();
            }
            return View(nguoiBan);
        }

        // POST: NguoiBans/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            NguoiBan nguoiBan = db.NguoiBans.Find(id);
            db.NguoiBans.Remove(nguoiBan);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: NguoiBans/ThongTinNguoiBan/5
        public ActionResult ThongTinNguoiBan(int? id, int? page = 1, string sort = "newest", int pageSize = 9)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Lấy thông tin người bán
            var nguoiBan = db.NguoiBans
                .Include(s => s.NguoiDung)
                .Include(s => s.DanhSachChungChi)
                .FirstOrDefault(s => s.MaNguoiBan == id);

            if (nguoiBan == null)
            {
                return HttpNotFound();
            }

            // Lấy danh sách sản phẩm của người bán
            var sanPhamQuery = db.SanPhams
                .Include(s => s.DanhSachAnhSanPham)
                .Include(s => s.DanhMucSanPham)
                .Where(s => s.MaNguoiBan == id && s.TrangThai == "Đã phê duyệt");

            // Áp dụng sắp xếp
            switch (sort)
            {
                case "priceAsc":
                    sanPhamQuery = sanPhamQuery.OrderBy(s => s.GiaSanPham);
                    break;
                case "priceDesc":
                    sanPhamQuery = sanPhamQuery.OrderByDescending(s => s.GiaSanPham);
                    break;
                default: // "newest"
                    sanPhamQuery = sanPhamQuery.OrderByDescending(s => s.NgayTao);
                    break;
            }

            int totalItems = sanPhamQuery.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            int pageNumber = (page ?? 1);

            // Lấy sản phẩm theo trang
            var sanPhams = sanPhamQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Tạo ViewModel với dữ liệu người bán và sản phẩm
            var viewModel = new NguoiBanViewModel
            {
                NguoiDung = nguoiBan.NguoiDung,
                NguoiBan = nguoiBan,
                DanhSachChungChi = nguoiBan.DanhSachChungChi.ToList(),
                DanhSachSanPham = sanPhams
            };

            // Thông tin phân trang và sắp xếp
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentSort = sort;

            return View(viewModel);
        }

        // GET: NguoiBans/QuanLySanPham/5
        // Thêm vào đầu phương thức QuanLySanPham trong NguoiBansController
        public ActionResult QuanLySanPham(int? id, int? page, string searchTerm = "", string statusFilter = "", string sortOrder = "newest")
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Xác thực người bán tồn tại
            NguoiBan nguoiBan = db.NguoiBans.Find(id);
            if (nguoiBan == null)
            {
                return HttpNotFound();
            }

            // Thiết lập phân trang
            int pageSize = 10;
            int pageNumber = (page ?? 1);

            // Lấy danh sách sản phẩm của người bán kèm theo danh sách ảnh
            var sanPhams = db.SanPhams
                .Include(s => s.DanhMucSanPham)
                .Include(s => s.DanhSachAnhSanPham)
                .Where(s => s.MaNguoiBan == id);

            // Áp dụng bộ lọc tìm kiếm nếu có
            if (!string.IsNullOrEmpty(searchTerm))
            {
                sanPhams = sanPhams.Where(s => s.TenSanPham.Contains(searchTerm));
            }

            // Áp dụng bộ lọc trạng thái nếu có
            if (!string.IsNullOrEmpty(statusFilter))
            {
                sanPhams = sanPhams.Where(s => s.TrangThai == statusFilter);
            }

            // Áp dụng sắp xếp
            switch (sortOrder)
            {
                case "oldest":
                    sanPhams = sanPhams.OrderBy(s => s.NgayTao);
                    break;
                case "nameAsc":
                    sanPhams = sanPhams.OrderBy(s => s.TenSanPham);
                    break;
                case "nameDesc":
                    sanPhams = sanPhams.OrderByDescending(s => s.TenSanPham);
                    break;
                case "priceAsc":
                    sanPhams = sanPhams.OrderBy(s => s.GiaSanPham);
                    break;
                case "priceDesc":
                    sanPhams = sanPhams.OrderByDescending(s => s.GiaSanPham);
                    break;
                default: // newest
                    sanPhams = sanPhams.OrderByDescending(s => s.NgayTao);
                    break;
            }

            // Đếm tổng số sản phẩm sau khi lọc
            int totalItems = sanPhams.Count();

            // Tính tổng số trang
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Đảm bảo trang hiện tại không vượt quá tổng số trang
            if (pageNumber > totalPages && totalPages > 0)
            {
                pageNumber = 1; // Reset về trang 1 nếu trang hiện tại không hợp lệ
                return RedirectToAction("QuanLySanPham", new
                {
                    id = id,
                    page = pageNumber,
                    searchTerm = searchTerm,
                    statusFilter = statusFilter,
                    sortOrder = sortOrder
                });
            }

            // Lấy sản phẩm theo trang
            var sanPhamsTrang = sanPhams
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Đặt thông tin người bán và phân trang vào ViewBag để hiển thị
            ViewBag.MaNguoiBan = id;
            ViewBag.TenCuaHang = nguoiBan.TenCuaHang;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.HasPreviousPage = pageNumber > 1;
            ViewBag.HasNextPage = pageNumber < totalPages;
            ViewBag.PageSize = pageSize;

            // Lưu các giá trị filter để sử dụng trong view
            ViewBag.SearchTerm = searchTerm;
            ViewBag.StatusFilter = statusFilter;
            ViewBag.SortOrder = sortOrder;

            return View(sanPhamsTrang);
        }

        // GET: NguoiBans/ThemSanPham/5
        // Update this part in the ThemSanPham action
        public ActionResult ThemSanPham(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Xác thực người bán tồn tại
            NguoiBan nguoiBan = db.NguoiBans.Find(id);
            if (nguoiBan == null)
            {
                return HttpNotFound();
            }

            // Đặt thông tin người bán vào ViewBag để hiển thị
            ViewBag.MaNguoiBan = id;
            ViewBag.TenCuaHang = nguoiBan.TenCuaHang;

            // Chuẩn bị danh sách danh mục cho dropdown
            ViewBag.MaDanhMuc = new SelectList(db.DanhMucSanPhams, "MaDanhMuc", "TenDanhMuc");

            // Make sure to set the default status according to your DB schema
            return View(new SanPham
            {
                MaNguoiBan = (int)id,
                NgayTao = DateTime.Now,
                TrangThai = "Đang chờ xử lý" // Changed from "Đang chờ duyệt" to match schema
            });
        }

        // POST: NguoiBans/ThemSanPham
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemSanPham([Bind(Include = "MaSanPham,MaNguoiBan,MaDanhMuc,TenSanPham,MoTaSanPham,GiaSanPham,SoLuongTonKho,TrangThai,ThongTin,SoLuotMua,ThuongHieu,SoLuongMoiHop,ThanhPhan,DoiTuongSuDung,HuongDanSuDung,KhoiLuong,BaoQuan")] SanPham sanPham, HttpPostedFileBase[] anhSanPham)
        {
            if (ModelState.IsValid)
            {
                // Thiết lập ngày tạo và cập nhật
                sanPham.NgayTao = DateTime.Now;
                sanPham.NgayCapNhat = DateTime.Now;

                // Thêm sản phẩm vào cơ sở dữ liệu
                db.SanPhams.Add(sanPham);
                db.SaveChanges();

                // Xử lý upload nhiều ảnh
                if (anhSanPham != null && anhSanPham.Length > 0)
                {
                    foreach (var anh in anhSanPham)
                    {
                        if (anh != null && anh.ContentLength > 0)
                        {
                            // Tạo tên file duy nhất
                            var fileName = Path.GetFileName(anh.FileName);
                            var uniqueFileName = DateTime.Now.Ticks + "_" + fileName;
                            var path = Path.Combine(Server.MapPath("~/Content/images/Products"), uniqueFileName);

                            // Lưu file ảnh
                            anh.SaveAs(path);

                            // Tạo bản ghi trong bảng AnhSanPham
                            var anhSP = new AnhSanPham
                            {
                                MaSanPham = sanPham.MaSanPham,
                                DuongDanAnh = "/Content/images/Products/" + uniqueFileName
                            };

                            db.AnhSanPhams.Add(anhSP);
                        }
                    }

                    // Lưu các ảnh vào cơ sở dữ liệu
                    db.SaveChanges();
                }

                return RedirectToAction("QuanLySanPham", new { id = sanPham.MaNguoiBan });
            }

            // Nếu ModelState không hợp lệ, chuẩn bị lại dữ liệu cho form
            ViewBag.MaNguoiBan = sanPham.MaNguoiBan;
            NguoiBan nguoiBan = db.NguoiBans.Find(sanPham.MaNguoiBan);
            ViewBag.TenCuaHang = nguoiBan?.TenCuaHang;
            ViewBag.MaDanhMuc = new SelectList(db.DanhMucSanPhams, "MaDanhMuc", "TenDanhMuc", sanPham.MaDanhMuc);

            return View(sanPham);
        }

        // GET: NguoiBans/ChiTietSanPham/5
        public ActionResult ChiTietSanPham(int? id, int? trangDanhGia = 1)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Lấy thông tin sản phẩm kèm theo các thông tin liên quan
            SanPham sanPham = db.SanPhams
                .Include(s => s.DanhMucSanPham)
                .Include(s => s.NguoiBan)
                .FirstOrDefault(s => s.MaSanPham == id);

            if (sanPham == null)
            {
                return HttpNotFound();
            }

            // Lấy danh sách ảnh của sản phẩm
            var danhSachAnh = db.AnhSanPhams.Where(a => a.MaSanPham == id).ToList();

            // Tạo ViewModel
            var viewModel = new SanPhamViewModel
            {
                SanPham = sanPham,
                DanhSachAnh = danhSachAnh
            };

            // Lấy sản phẩm liên quan (cùng danh mục, không bao gồm sản phẩm hiện tại)
            var sanPhamLienQuan = db.SanPhams
                .Include(s => s.NguoiBan)
                .Include(s => s.DanhSachAnhSanPham)
                .Where(s => s.MaDanhMuc == sanPham.MaDanhMuc  && s.TrangThai == "Đã phê duyệt")
                .Take(4)
                .ToList();

            ViewBag.SanPhamLienQuan = sanPhamLienQuan;
            ViewBag.MaNguoiBan = sanPham.MaNguoiBan;
            ViewBag.TenCuaHang = sanPham.NguoiBan.TenCuaHang;

            // Xử lý đánh giá sản phẩm
            int pageSize = 5; // Số lượng đánh giá mỗi trang
            int pageNumber = (trangDanhGia ?? 1);

            // Lấy danh sách đánh giá của sản phẩm
            var danhSachDanhGia = db.DanhGiaSanPhams
                .Include(d => d.NguoiDung)
                .Where(d => d.MaSanPham == id)
                .OrderByDescending(d => d.NgayTao)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Đếm tổng số đánh giá
            int totalReviews = db.DanhGiaSanPhams.Where(d => d.MaSanPham == id).Count();
            int totalPages = (int)Math.Ceiling(totalReviews / (double)pageSize);

            // Tính điểm đánh giá trung bình
            double? averageRating = null;
            if (totalReviews > 0)
            {
                averageRating = db.DanhGiaSanPhams.Where(d => d.MaSanPham == id).Average(d => (double)d.DanhGia);
            }

            // Tính phần trăm cho từng số sao
            var ratingStats = db.DanhGiaSanPhams
                .Where(d => d.MaSanPham == id)
                .GroupBy(d => d.DanhGia)
                .Select(g => new { DiemDanhGia = g.Key, SoLuong = g.Count() })
                .ToDictionary(x => x.DiemDanhGia, x => x.SoLuong);

            Dictionary<int, double> ratingPercentages = new Dictionary<int, double>();
            for (int i = 1; i <= 5; i++)
            {
                int count = ratingStats.ContainsKey(i) ? ratingStats[i] : 0;
                double percentage = totalReviews > 0 ? Math.Round((double)count / totalReviews * 100, 1) : 0;
                ratingPercentages[i] = percentage;
            }

            // Truyền dữ liệu vào ViewBag
            ViewBag.DanhSachDanhGia = danhSachDanhGia;
            ViewBag.TongSoDanhGia = totalReviews;
            ViewBag.DiemDanhGiaTrungBinh = averageRating;
            ViewBag.PhanTramDanhGia = ratingPercentages;
            ViewBag.TrangHienTaiDanhGia = pageNumber;
            ViewBag.TongSoTrangDanhGia = totalPages;

            return View(viewModel);
        }

        // GET: NguoiBans/SuaSanPham/5
        public ActionResult SuaSanPham(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Lấy thông tin sản phẩm từ database
            SanPham sanPham = db.SanPhams.Find(id);
            if (sanPham == null)
            {
                return HttpNotFound();
            }

            // Lấy danh sách ảnh của sản phẩm
            var danhSachAnh = db.AnhSanPhams.Where(a => a.MaSanPham == id).ToList();

            // Lấy thông tin người bán
            var nguoiBan = db.NguoiBans.Find(sanPham.MaNguoiBan);

            // Chuẩn bị dữ liệu cho dropdown danh mục
            // Đảm bảo chọn đúng danh mục hiện tại
            ViewBag.MaDanhMuc = new SelectList(db.DanhMucSanPhams, "MaDanhMuc", "TenDanhMuc", sanPham.MaDanhMuc);
            ViewBag.MaNguoiBan = sanPham.MaNguoiBan;
            ViewBag.TenCuaHang = nguoiBan?.TenCuaHang;
            // Lưu trạng thái hiện tại vào ViewBag để hiển thị thông tin
            ViewBag.TrangThaiSanPham = sanPham.TrangThai;

            // Làm tròn giá để hiển thị
            ViewBag.GiaSanPhamHienThi = (long)Math.Round(sanPham.GiaSanPham);
            ViewBag.GiaGocHienThi = (long)Math.Round(sanPham.GiaSanPham / 1.1m);

            // Tạo ViewModel
            var viewModel = new SanPhamViewModel
            {
                SanPham = sanPham,
                DanhSachAnh = danhSachAnh
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaSanPham(SanPhamViewModel viewModel, HttpPostedFileBase[] anhSanPham)
        {
            // Lấy sản phẩm từ database bằng DbSet.Find() trực tiếp
            var sanPham = db.SanPhams.Find(viewModel.SanPham.MaSanPham);
            if (sanPham == null)
            {
                return HttpNotFound();
            }

            // Lưu lại MaDanhMuc hiện tại
            int maDanhMucHienTai = sanPham.MaDanhMuc;

            try
            {
                // Lưu các giá trị không đổi
                var ngayTao = sanPham.NgayTao;
                var soLuotMua = sanPham.SoLuotMua;
                var trangThai = sanPham.TrangThai;

                // Cập nhật TỪNG TRƯỜNG một cách thủ công, tránh lỗi
                sanPham.TenSanPham = viewModel.SanPham.TenSanPham;
                sanPham.MoTaSanPham = viewModel.SanPham.MoTaSanPham;

                // Xử lý GiaSanPham đặc biệt
                string giaSanPhamStr = Request.Form["GiaSanPham"];
                giaSanPhamStr = giaSanPhamStr.Replace(".", "").Replace(",", ".");
                Console.WriteLine("Giá từ form: " + giaSanPhamStr);
                decimal giaSanPham;
                if (decimal.TryParse(giaSanPhamStr, NumberStyles.Any, CultureInfo.InvariantCulture, out giaSanPham))
                {
                    Console.WriteLine("Giá sau parse: " + giaSanPham);

                    // Đảm bảo giá trị trong phạm vi hợp lý
                    if (giaSanPham > 100000) // Ngưỡng cao bất thường
                    {
                        giaSanPham = giaSanPham / 100;
                        Console.WriteLine("Giá sau điều chỉnh: " + giaSanPham);
                    }

                    sanPham.GiaSanPham = giaSanPham;
                }

                // Kiểm tra danh mục mới
                int maDanhMucMoi = viewModel.SanPham.MaDanhMuc;
                var danhMucTonTai = db.DanhMucSanPhams.Any(d => d.MaDanhMuc == maDanhMucMoi);

                // Chỉ cập nhật nếu danh mục mới tồn tại
                if (danhMucTonTai)
                {
                    sanPham.MaDanhMuc = maDanhMucMoi;
                }

                // Đảm bảo các giá trị cơ bản
                sanPham.SoLuongTonKho = Math.Max(0, viewModel.SanPham.SoLuongTonKho);
                sanPham.ThuongHieu = viewModel.SanPham.ThuongHieu ?? "Khác";
                sanPham.SoLuongMoiHop = Math.Max(0, viewModel.SanPham.SoLuongMoiHop);
                sanPham.ThanhPhan = viewModel.SanPham.ThanhPhan;
                sanPham.DoiTuongSuDung = viewModel.SanPham.DoiTuongSuDung;
                sanPham.HuongDanSuDung = viewModel.SanPham.HuongDanSuDung;
                sanPham.KhoiLuong = Math.Max(1, viewModel.SanPham.KhoiLuong);
                sanPham.BaoQuan = viewModel.SanPham.BaoQuan;

                // Khôi phục các giá trị đã lưu
                sanPham.NgayTao = ngayTao;
                sanPham.SoLuotMua = soLuotMua;
                sanPham.TrangThai = trangThai;
                sanPham.NgayCapNhat = DateTime.Now;

                // TRỰC TIẾP THỰC HIỆN LỆNH SQL ĐỂ CẬP NHẬT
                db.Database.ExecuteSqlCommand(
                    "UPDATE SanPham SET MaDanhMuc = @p0, TenSanPham = @p1, MoTaSanPham = @p2, " +
                    "GiaSanPham = @p3, SoLuongTonKho = @p4, ThuongHieu = @p5, SoLuongMoiHop = @p6, " +
                    "ThanhPhan = @p7, DoiTuongSuDung = @p8, HuongDanSuDung = @p9, KhoiLuong = @p10, " +
                    "BaoQuan = @p11, NgayCapNhat = @p12 WHERE MaSanPham = @p13",
                    sanPham.MaDanhMuc, sanPham.TenSanPham, sanPham.MoTaSanPham,
                    sanPham.GiaSanPham, sanPham.SoLuongTonKho, sanPham.ThuongHieu, sanPham.SoLuongMoiHop,
                    sanPham.ThanhPhan, sanPham.DoiTuongSuDung, sanPham.HuongDanSuDung, sanPham.KhoiLuong,
                    sanPham.BaoQuan, sanPham.NgayCapNhat, sanPham.MaSanPham);

                // Xử lý upload ảnh mới (nếu có)
                if (anhSanPham != null && anhSanPham.Length > 0)
                {
                    foreach (var anh in anhSanPham)
                    {
                        if (anh != null && anh.ContentLength > 0)
                        {
                            // Tạo tên file duy nhất
                            var fileName = Path.GetFileName(anh.FileName);
                            var uniqueFileName = DateTime.Now.Ticks + "_" + fileName;
                            var path = Path.Combine(Server.MapPath("~/Content/images/Products"), uniqueFileName);

                            // Lưu file ảnh
                            anh.SaveAs(path);

                            // Tạo bản ghi trong bảng AnhSanPham
                            var anhSP = new AnhSanPham
                            {
                                MaSanPham = sanPham.MaSanPham,
                                DuongDanAnh = "/Content/images/Products/" + uniqueFileName
                            };

                            db.AnhSanPhams.Add(anhSP);
                        }
                    }

                    // Lưu các ảnh vào cơ sở dữ liệu
                    db.SaveChanges();
                }

                TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";
                return RedirectToAction("QuanLySanPham", new { id = sanPham.MaNguoiBan });
            }
            catch (Exception ex)
            {
                // Ghi log chi tiết
                string errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage = ex.InnerException.Message;
                }

                System.Diagnostics.Debug.WriteLine("Error: " + errorMessage);
                ModelState.AddModelError("", "Lỗi khi cập nhật sản phẩm: " + errorMessage);
            }

            // Nếu có lỗi, chuẩn bị lại dữ liệu cho view
            ViewBag.MaNguoiBan = viewModel.SanPham.MaNguoiBan;
            NguoiBan nguoiBan = db.NguoiBans.Find(viewModel.SanPham.MaNguoiBan);
            ViewBag.TenCuaHang = nguoiBan?.TenCuaHang;
            ViewBag.MaDanhMuc = new SelectList(db.DanhMucSanPhams, "MaDanhMuc", "TenDanhMuc", viewModel.SanPham.MaDanhMuc);

            // Làm tròn giá để hiển thị
            ViewBag.GiaSanPhamHienThi = (long)Math.Round(viewModel.SanPham.GiaSanPham);
            ViewBag.GiaGocHienThi = (long)Math.Round(viewModel.SanPham.GiaSanPham / 1.1m);

            // Lấy lại danh sách ảnh
            var danhSachAnh = db.AnhSanPhams.Where(a => a.MaSanPham == viewModel.SanPham.MaSanPham).ToList();
            viewModel.DanhSachAnh = danhSachAnh;

            return View(viewModel);
        }

        // GET: NguoiBans/XoaSanPham/5
        public ActionResult XoaSanPham(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Lấy thông tin sản phẩm từ database kèm thông tin liên quan
            SanPham sanPham = db.SanPhams
                .Include(s => s.DanhMucSanPham)
                .Include(s => s.NguoiBan)
                .FirstOrDefault(s => s.MaSanPham == id);

            // Kiểm tra và log trạng thái
            string trangThai = sanPham?.TrangThai;
            System.Diagnostics.Debug.WriteLine("Trạng thái sản phẩm: " + trangThai);

            if (sanPham == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra quyền truy cập - nếu người dùng hiện tại không phải là chủ sản phẩm
            if (Session["MaNguoiDung"] != null)
            {
                int maNguoiDung = (int)Session["MaNguoiDung"];
                var nguoiBan = db.NguoiBans.FirstOrDefault(n => n.MaNguoiDung == maNguoiDung);

                if (nguoiBan == null || nguoiBan.MaNguoiBan != sanPham.MaNguoiBan)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền xóa sản phẩm này!";
                    return RedirectToAction("QuanLySanPham", new { id = nguoiBan?.MaNguoiBan });
                }
            }
            else
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            // Lấy danh sách ảnh của sản phẩm
            var danhSachAnh = db.AnhSanPhams.Where(a => a.MaSanPham == id).ToList();

            // Kiểm tra sản phẩm đã có trong đơn hàng chưa
            bool coTrongDonHang = db.ChiTietDonHangs.Any(c => c.MaSanPham == id);

            // Tạo một bản sao của đối tượng sản phẩm để tránh vấn đề với Entity Framework
            var sanPhamClone = new SanPham
            {
                MaSanPham = sanPham.MaSanPham,
                MaNguoiBan = sanPham.MaNguoiBan,
                MaDanhMuc = sanPham.MaDanhMuc,
                TenSanPham = sanPham.TenSanPham,
                MoTaSanPham = sanPham.MoTaSanPham,
                GiaSanPham = sanPham.GiaSanPham,
                SoLuongTonKho = sanPham.SoLuongTonKho,
                TrangThai = sanPham.TrangThai, // Đảm bảo trạng thái được sao chép
                SoLuotMua = sanPham.SoLuotMua,
                ThuongHieu = sanPham.ThuongHieu,
                SoLuongMoiHop = sanPham.SoLuongMoiHop,
                ThanhPhan = sanPham.ThanhPhan,
                DoiTuongSuDung = sanPham.DoiTuongSuDung,
                HuongDanSuDung = sanPham.HuongDanSuDung,
                KhoiLuong = sanPham.KhoiLuong,
                BaoQuan = sanPham.BaoQuan,
                NgayTao = sanPham.NgayTao,
                NgayCapNhat = sanPham.NgayCapNhat,
                DanhMucSanPham = sanPham.DanhMucSanPham,
                NguoiBan = sanPham.NguoiBan
            };

            // Tạo ViewModel với bản sao mới
            var viewModel = new SanPhamViewModel
            {
                SanPham = sanPhamClone,
                DanhSachAnh = danhSachAnh
            };

            ViewBag.MaNguoiBan = sanPham.MaNguoiBan;
            ViewBag.TenCuaHang = sanPham.NguoiBan.TenCuaHang;
            ViewBag.CoTrongDonHang = coTrongDonHang;

            // Đảm bảo trạng thái không bị null
            if (string.IsNullOrEmpty(sanPham.TrangThai))
            {
                sanPham.TrangThai = "Chưa xác định";
            }

            // Truyền trạng thái trực tiếp qua ViewBag để tránh vấn đề với model binding
            ViewBag.TrangThaiSanPham = sanPham.TrangThai;

            // Truy vấn trực tiếp trạng thái từ database để đảm bảo
            var trangThaiTrucTiep = db.Database.SqlQuery<string>(
                "SELECT TrangThai FROM SanPham WHERE MaSanPham = @p0",
                id).FirstOrDefault();

            ViewBag.TrangThaiTrucTiep = trangThaiTrucTiep;

            return View(viewModel);
        }

        // POST: NguoiBans/XoaSanPham/5
        [HttpPost, ActionName("XoaSanPham")]
        [ValidateAntiForgeryToken]
        public ActionResult XoaSanPhamConfirmed(int id)
        {
            try
            {
                SanPham sanPham = db.SanPhams.Find(id);

                if (sanPham == null)
                {
                    return HttpNotFound();
                }

                int maNguoiBan = sanPham.MaNguoiBan;

                // Kiểm tra quyền truy cập - nếu người dùng hiện tại không phải là chủ sản phẩm
                if (Session["MaNguoiDung"] != null)
                {
                    int maNguoiDung = (int)Session["MaNguoiDung"];
                    var nguoiBan = db.NguoiBans.FirstOrDefault(n => n.MaNguoiDung == maNguoiDung);

                    if (nguoiBan == null || nguoiBan.MaNguoiBan != sanPham.MaNguoiBan)
                    {
                        TempData["ErrorMessage"] = "Bạn không có quyền xóa sản phẩm này!";
                        return RedirectToAction("QuanLySanPham", new { id = nguoiBan?.MaNguoiBan });
                    }
                }
                else
                {
                    return RedirectToAction("DangNhap", "DangNhap");
                }

                // Kiểm tra sản phẩm có trong đơn hàng không
                bool coTrongDonHang = db.ChiTietDonHangs.Any(c => c.MaSanPham == id);

                if (coTrongDonHang)
                {
                    TempData["ErrorMessage"] = "Không thể xóa sản phẩm đã có trong đơn hàng!";
                    return RedirectToAction("QuanLySanPham", new { id = maNguoiBan });
                }

                // Bắt đầu transaction để đảm bảo tính toàn vẹn dữ liệu
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        // Xóa tất cả các ảnh liên quan đến sản phẩm
                        var danhSachAnh = db.AnhSanPhams.Where(a => a.MaSanPham == id).ToList();
                        foreach (var anh in danhSachAnh)
                        {
                            // Xóa file ảnh từ thư mục nếu cần
                            string duongDanAnh = anh.DuongDanAnh;
                            if (!string.IsNullOrEmpty(duongDanAnh) && duongDanAnh.StartsWith("/Content/images/Products/"))
                            {
                                string duongDanDay = Server.MapPath("~" + duongDanAnh);
                                if (System.IO.File.Exists(duongDanDay))
                                {
                                    try
                                    {
                                        System.IO.File.Delete(duongDanDay);
                                    }
                                    catch
                                    {
                                        // Ghi log lỗi nếu cần
                                    }
                                }
                            }

                            db.AnhSanPhams.Remove(anh);
                        }

                        // Xóa đánh giá sản phẩm nếu có
                        var danhGia = db.DanhGiaSanPhams.Where(d => d.MaSanPham == id).ToList();
                        foreach (var dg in danhGia)
                        {
                            db.DanhGiaSanPhams.Remove(dg);
                        }

                        // Xóa giỏ hàng có sản phẩm này nếu có
                        var gioHang = db.GioHangs.Where(g => g.MaSanPham == id).ToList();
                        foreach (var gh in gioHang)
                        {
                            db.GioHangs.Remove(gh);
                        }

                        // Xóa sản phẩm
                        db.SanPhams.Remove(sanPham);
                        db.SaveChanges();

                        // Commit transaction nếu mọi thứ thành công
                        transaction.Commit();

                        TempData["SuccessMessage"] = "Xóa sản phẩm thành công!";
                    }
                    catch (Exception ex)
                    {
                        // Rollback lại nếu có lỗi
                        transaction.Rollback();
                        TempData["ErrorMessage"] = "Lỗi khi xóa sản phẩm: " + ex.Message;
                    }
                }

                return RedirectToAction("QuanLySanPham", new { id = maNguoiBan });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("QuanLySanPham", new { id = Session["MaNguoiBan"] });
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
        public class NguoiBanViewModel
        {
            public NguoiDung NguoiDung { get; set; }
            public NguoiBan NguoiBan { get; set; }
            public List<AnhChungChi> DanhSachChungChi { get; set; }
            public List<SanPham> DanhSachSanPham { get; set; }
        }

        public class SanPhamViewModel
        {
            public SanPham SanPham { get; set; }
            public List<AnhSanPham> DanhSachAnh { get; set; }
        }
}