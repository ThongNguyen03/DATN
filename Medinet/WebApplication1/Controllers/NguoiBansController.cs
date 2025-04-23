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
        [Authorize]
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
        [Authorize]
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
        [Authorize]
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
        [Authorize]
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
        [Authorize]
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
        [Authorize]
        public ActionResult ThongTinNguoiBan(int? id, int? page = 1, string sort = "newest", int pageSize = 9)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Lấy thông tin người bán
            var nguoiBan = db.NguoiBans
                .Include(s => s.NguoiDung)
                .Include(s => s.AnhChungChis)
                .FirstOrDefault(s => s.MaNguoiBan == id);

            if (nguoiBan == null)
            {
                return HttpNotFound();
            }

            // Lấy danh sách sản phẩm của người bán
            var sanPhamQuery = db.SanPhams
                .Include(s => s.AnhSanPhams)
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
                DanhSachChungChi = nguoiBan.AnhChungChis.ToList(),
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
        [Authorize]
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
                .Include(s => s.AnhSanPhams)
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
                case "stockLow": // Thêm tùy chọn mới này
                    sanPhams = sanPhams.OrderBy(s => s.SoLuongTonKho == 0 ? 0 :
                                                 s.SoLuongTonKho <= 5 ? 1 : 2)
                                      .ThenByDescending(s => s.NgayTao);
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
        [Authorize]
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
        [Authorize]
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

                var nguoiban = db.NguoiBans.Find(sanPham.MaNguoiBan);
                if(nguoiban != null)
                {
                    // Tạo thông báo cho người bán về việc thêm sản phẩm thành công
                    var thongBaoNguoiBan = new ThongBao
                    {
                        MaNguoiDung = nguoiban.MaNguoiDung,
                        LoaiThongBao = "SanPham",
                        TieuDe = "Thêm sản phẩm mới thành công",
                        TinNhan = $"Bạn đã thêm sản phẩm '{sanPham.TenSanPham}' vào cửa hàng thành công. Sản phẩm sẽ được quản trị viên phê duyệt.",
                        MucDoQuanTrong = 1, // Thông báo thông thường
                        DuongDanChiTiet = "/NguoiBans/ChiTietSanPham/" + sanPham.MaSanPham,
                        NgayTao = DateTime.Now
                    };
                    db.ThongBaos.Add(thongBaoNguoiBan);
                    db.SaveChanges();
                }


                // Tạo thông báo cho admin về yêu cầu trở thành người bán
                var adminList = db.NguoiDungs.Where(n => n.VaiTro == "Admin").ToList();
                if (adminList != null && adminList.Count > 0)
                {
                    foreach (var admin in adminList)
                    {
                        var thongBaoAdmin = new ThongBao
                        {
                            MaNguoiDung = admin.MaNguoiDung, // Giả sử có trường MaQuanTriVien
                            LoaiThongBao = "SanPham",
                            TieuDe = "Có sản phẩm mới được thêm vào",
                            TinNhan = $"Người bán ID: {sanPham.MaNguoiBan} đã thêm sản phẩm mới: '{sanPham.TenSanPham}'. Vui lòng kiểm tra .",
                            MucDoQuanTrong = 1, // Thông báo thông thường
                            DuongDanChiTiet = "/Admin/ProductManagement" ,
                            NgayTao = DateTime.Now
                        };
                        db.ThongBaos.Add(thongBaoAdmin);
                    }
                    db.SaveChanges();
                }
                // Thêm thông báo thành công vào TempData (cho thông báo UI)
                TempData["Success"] = $"Sản phẩm '{sanPham.TenSanPham}' đã được thêm thành công.";
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
        [Authorize]
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
                .Include(s => s.AnhSanPhams)
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
        [Authorize]
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

        [Authorize]
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

                    var nguoiban = db.NguoiBans.Find(sanPham.MaNguoiBan);
                    if (nguoiban != null)
                    {
                        // Tạo thông báo cho người bán về việc cập nhật sản phẩm thành công
                        var thongBaoNguoiBan = new ThongBao
                        {
                            MaNguoiDung = nguoiban.MaNguoiDung,
                            LoaiThongBao = "SanPham",
                            TieuDe = "Cập nhật sản phẩm thành công",
                            TinNhan = $"Bạn đã cập nhật thông tin sản phẩm '{sanPham.TenSanPham}' thành công",
                            MucDoQuanTrong = 1, // Thông báo thông thường
                            DuongDanChiTiet = "/NguoiBans/ChiTietSanPham/" + sanPham.MaSanPham,
                            NgayTao = DateTime.Now
                        };
                        db.ThongBaos.Add(thongBaoNguoiBan);
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
        [Authorize]
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
        [Authorize]
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
                        var nguoiban = db.NguoiBans.Find(maNguoiBan);
                        if(nguoiban != null)
                        {
                            // Tạo thông báo cho người bán về việc xóa sản phẩm
                            var thongBaoNguoiBan = new ThongBao
                            {
                                MaNguoiDung = nguoiban.MaNguoiDung,
                                LoaiThongBao = "SanPham",
                                TieuDe = "Xóa sản phẩm thành công",
                                TinNhan = $"Bạn đã xóa sản phẩm '{sanPham.TenSanPham}' khỏi cửa hàng của mình. Tất cả dữ liệu liên quan đến sản phẩm này đã được xóa khỏi hệ thống.",
                                MucDoQuanTrong = 1, // Thông báo thông thường
                                DuongDanChiTiet = "/NguoiBans/QuanLySanPham/" + maNguoiBan,
                                NgayTao = DateTime.Now
                            };
                            db.ThongBaos.Add(thongBaoNguoiBan);
                        }


                        // Tạo thông báo cho admin về yêu cầu trở thành người bán
                        var adminList = db.NguoiDungs.Where(n => n.VaiTro == "Admin").ToList();
                        if (adminList != null && adminList.Count > 0)
                        {
                            foreach (var admin in adminList)
                            {
                                var thongBaoAdmin = new ThongBao
                                {
                                    MaNguoiDung = admin.MaNguoiDung, // Giả sử có trường MaQuanTriVien
                                    LoaiThongBao = "SanPham",
                                    TieuDe = "Sản phẩm đã bị xóa",
                                    TinNhan = $"Người bán (ID: {maNguoiBan}) đã xóa sản phẩm '{sanPham.TenSanPham}' (ID: {id}) khỏi hệ thống.",
                                    MucDoQuanTrong = 1, // Thông báo thông thường
                                    DuongDanChiTiet = "/Admin/ProductManagement",
                                    NgayTao = DateTime.Now
                                };
                                db.ThongBaos.Add(thongBaoAdmin);
                            }
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

        //27/03/2025
        // Thêm phương thức quản lý tài khoản vào NguoiBansController.cs
        [Authorize]
        public ActionResult QuanLyTaiKhoan(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // Lấy thông tin người bán
            var nguoiBan = db.NguoiBans.Find(id);
            if (nguoiBan == null)
            {
                return HttpNotFound();
            }

            // Kiểm tra quyền truy cập
            int maNguoiDung = GetCurrentUserId();
            if (nguoiBan.MaNguoiDung != maNguoiDung)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            }

            // Lấy danh sách ghi chép ví
            var ghiChepVis = db.GhiChepVis
                .Where(g => g.MaNguoiBan == id)
                .OrderByDescending(g => g.NgayGiaoDich)
                .Take(10)
                .ToList();

            // Lấy thông tin đơn hàng
            var donHangs = db.DonHangs
                .Where(d => d.MaNguoiBan == id)
                .ToList();

            // Lấy thông tin sản phẩm
            var sanPhams = db.SanPhams
                .Where(s => s.MaNguoiBan == id)
                .ToList();

            // Thay đổi cách thức truy vấn
            var sanPhamBanChay = (from ct in db.ChiTietDonHangs
                                  join sp in db.SanPhams on ct.MaSanPham equals sp.MaSanPham
                                  join dh in db.DonHangs on ct.MaDonHang equals dh.MaDonHang
                                  where sp.MaNguoiBan == id &&
                                        (dh.TrangThaiDonHang == "Đã xác nhận nhận hàng" ||
                                         dh.TrangThaiDonHang == "Đã hoàn thành" ||
                                         dh.TrangThaiDonHang == "Đã thanh toán")
                                  group ct by new { sp.MaSanPham, sp.TenSanPham } into g
                                  select new
                                  {
                                      MaSanPham = g.Key.MaSanPham,
                                      TenSanPham = g.Key.TenSanPham,
                                      SoLuongBan = g.Sum(x => x.SoLuong),
                                      TongTien = g.Sum(x => x.SoLuong * x.Gia)
                                  })
                                 .OrderByDescending(x => x.SoLuongBan)
                                 .Take(5)
                                 .ToList();

            // Sau đó cập nhật AnhDaiDien riêng
            var result = sanPhamBanChay.Select(sp => new SanPhamBanChayViewModel
            {
                MaSanPham = sp.MaSanPham,
                TenSanPham = sp.TenSanPham,
                SoLuongBan = sp.SoLuongBan,
                TongTien = sp.TongTien,
                AnhDaiDien = db.AnhSanPhams
                    .Where(a => a.MaSanPham == sp.MaSanPham)
                    .Select(a => a.DuongDanAnh)
                    .FirstOrDefault() ?? "/Content/images/no-image.jpeg"
            }).ToList();

            ViewBag.SanPhamBanChay = result;
            // Tính toán các thống kê
            var tongDoanhThu = donHangs
                .Where(d => 
                           d.TrangThaiDonHang == "Đã hoàn thành" 
                           )
                .Sum(d => d.TongSoTien);

            var tongLoiNhuan = tongDoanhThu - (tongDoanhThu * 0.1m); // Lợi nhuận sau khi trừ phí 10%

            var tongSanPham = sanPhams.Count;
            var tongDonHang = donHangs.Count;
            var donHangDaGiao = donHangs.Count(d => d.TrangThaiDonHang == "Đã hoàn thành");
            var donHangDangVanChuyen = donHangs.Count(d => d.TrangThaiDonHang == "Đã vận chuyển");
            var donHangChoXuLy = donHangs.Count(d => d.TrangThaiDonHang == "Đang chờ xử lý");
            var donHangDaHuy = donHangs.Count(d => d.TrangThaiDonHang == "Đã hủy");

            // Thống kê doanh thu theo tháng trong năm hiện tại
            var namHienTai = DateTime.Now.Year;
            var doanhThuTheoThang = new decimal[12];
            var thangDoanhThu = new string[12];

            for (int i = 0; i < 12; i++)
            {
                var thang = i + 1;
                doanhThuTheoThang[i] = donHangs
                    .Where(d => d.NgayTao.Month == thang &&
                                d.NgayTao.Year == namHienTai &&
                                (d.TrangThaiDonHang == "Đã xác nhận nhận hàng" ||
                                 d.TrangThaiDonHang == "Đã hoàn thành" ||
                                 d.TrangThaiDonHang == "Đã thanh toán"))
                    .Sum(d => d.TongSoTien);

                thangDoanhThu[i] = "Tháng " + thang;
            }

            // Tính toán tỷ lệ đặt cọc và phí nền tảng
            decimal tyLeDatCoc = 10; // 10%
            decimal tyLePhiNenTang = 10; // 10%

            // Đưa dữ liệu vào ViewBag
            ViewBag.GhiChepVis = ghiChepVis;
            //ViewBag.SanPhamBanChay = sanPhamBanChay;
            ViewBag.TongDoanhThu = tongDoanhThu;
            ViewBag.TongLoiNhuan = tongLoiNhuan;
            ViewBag.TongSanPham = tongSanPham;
            ViewBag.TongDonHang = tongDonHang;
            ViewBag.DonHangDaGiao = donHangDaGiao;
            ViewBag.DonHangDangVanChuyen = donHangDangVanChuyen;
            ViewBag.DonHangChoXuLy = donHangChoXuLy;
            ViewBag.DonHangDaHuy = donHangDaHuy;
            ViewBag.DoanhThu = doanhThuTheoThang;
            ViewBag.ThangDoanhThu = thangDoanhThu;
            ViewBag.TyLeDatCoc = tyLeDatCoc;
            ViewBag.TyLePhiNenTang = tyLePhiNenTang;

            return View(nguoiBan);
        }


        // GET: NguoiBans/NapTien
        [Authorize]
        public ActionResult NapTien()
        {
            int maNguoiDung = GetCurrentUserId();
            if (maNguoiDung <= 0)
            {
                return RedirectToAction("DangNhap", "DangNhap");
            }

            var nguoiBan = db.NguoiBans.FirstOrDefault(n => n.MaNguoiDung == maNguoiDung);
            if (nguoiBan == null)
            {
                TempData["Error"] = "Bạn không phải là người bán!";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.MaNguoiBan = nguoiBan.MaNguoiBan;
            ViewBag.SoDuHienTai = nguoiBan.SoDuVi;

            return View();
        }

        // POST: NguoiBans/NapTienVi
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NapTienVi(decimal soTien, string phuongThucThanhToan)
        {
            try
            {
                // Kiểm tra quyền truy cập
                int maNguoiDung = GetCurrentUserId();
                var nguoiBan = db.NguoiBans.FirstOrDefault(n => n.MaNguoiDung == maNguoiDung);
                if (nguoiBan == null)
                {
                    TempData["Error"] = "Bạn không phải là người bán!";
                    return RedirectToAction("Index", "Home");
                }

                int maNguoiBan = nguoiBan.MaNguoiBan;

                // Kiểm tra số tiền
                if (soTien <= 0)
                {
                    TempData["Error"] = "Số tiền nạp phải lớn hơn 0!";
                    return RedirectToAction("NapTien");
                }

                // Nếu phương thức thanh toán là VNPAY, chuyển hướng đến trang thanh toán VNPAY
                if (phuongThucThanhToan == "VNPAY")
                {
                    return RedirectToAction("TaoThanhToanVNPayNapTien", "ThanhToan", new { maNguoiBan = maNguoiBan, soTien = soTien });
                }

                // Nếu là phương thức chuyển khoản ngân hàng
                if (phuongThucThanhToan == "ChuyenKhoan")
                {
                    // Tạo ghi chép ví với trạng thái "Chờ xác nhận"
                    var ghiChep = new GhiChepVi
                    {
                        MaNguoiBan = maNguoiBan,
                        SoTien = soTien,
                        LoaiGiaoDich = "Nạp tiền",
                        MoTa = $"Nạp tiền vào ví qua chuyển khoản ngân hàng",
                        NgayGiaoDich = DateTime.Now,
                        TrangThai = "Chờ xác nhận"
                    };
                    db.GhiChepVis.Add(ghiChep);
                    db.SaveChanges();

                    TempData["Success"] = "Yêu cầu nạp tiền đã được ghi nhận. Vui lòng chuyển khoản theo hướng dẫn để hoàn tất!";
                    return RedirectToAction("HuongDanChuyenKhoan", new { maGiaoDich = ghiChep.MaGiaoDichNgoai });
                }

                // Nếu là phương thức tiền mặt hoặc khác (cần admin phê duyệt)
                var ghiChepKhac = new GhiChepVi
                {
                    MaNguoiBan = maNguoiBan,
                    SoTien = soTien,
                    LoaiGiaoDich = "Nạp tiền",
                    MoTa = $"Nạp tiền vào ví qua {phuongThucThanhToan}",
                    NgayGiaoDich = DateTime.Now,
                    TrangThai = "Chờ xác nhận"
                };
                db.GhiChepVis.Add(ghiChepKhac);

                // Tạo thông báo cho người bán về yêu cầu nạp tiền
                var thongBaoNguoiBan = new ThongBao
                {
                    MaNguoiDung = nguoiBan.MaNguoiDung,
                    LoaiThongBao = "Vi",
                    TieuDe = "Yêu cầu nạp tiền thành công",
                    TinNhan = $"Yêu cầu nạp {soTien:N0} VNĐ vào ví thông qua chuyển khoản ngân hàng đã thành công",
                    MucDoQuanTrong = 1, // Thông báo thông thường
                    DuongDanChiTiet = "/GiaoDich/ViNguoiBan" ,
                    NgayTao = DateTime.Now
                };
                db.ThongBaos.Add(thongBaoNguoiBan);
                db.SaveChanges();

                TempData["Success"] = "Yêu cầu nạp tiền đã được ghi nhận. Quản trị viên sẽ xác nhận trong thời gian sớm nhất!";
                return RedirectToAction("NapTien");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("NapTien");
            }
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RutTienVi(int maNguoiBan, decimal soTien, string thongTinTaiKhoan)
        {
            try
            {
                // Kiểm tra quyền truy cập
                int maNguoiDung = GetCurrentUserId();
                var nguoiBan = db.NguoiBans.Find(maNguoiBan);
                if (nguoiBan == null || nguoiBan.MaNguoiDung != maNguoiDung)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
                }

                // Kiểm tra số tiền
                if (soTien <= 0)
                {
                    TempData["Error"] = "Số tiền rút phải lớn hơn 0!";
                    return RedirectToAction("QuanLyTaiKhoan", new { id = maNguoiBan });
                }

                // Kiểm tra số dư
                if (nguoiBan.SoDuVi < soTien)
                {
                    TempData["Error"] = "Số dư ví không đủ để thực hiện rút tiền!";
                    return RedirectToAction("QuanLyTaiKhoan", new { id = maNguoiBan });
                }

                // Tạo ghi chép ví
                var ghiChep = new GhiChepVi
                {
                    MaNguoiBan = maNguoiBan,
                    SoTien = -soTien, // Số tiền âm vì là rút ra
                    LoaiGiaoDich = "Rút tiền",
                    MoTa = $"Rút tiền về tài khoản: {thongTinTaiKhoan}",
                    NgayGiaoDich = DateTime.Now,
                    TrangThai = "Đang xử lý" // Trạng thái ban đầu là đang xử lý, sau đó cần có quá trình xác nhận
                };
                db.GhiChepVis.Add(ghiChep);

                // Tạo thông báo cho người bán về yêu cầu rút tiền
                var thongBaoNguoiBan = new ThongBao
                {
                    MaNguoiDung = nguoiBan.MaNguoiDung,
                    LoaiThongBao = "Vi",
                    TieuDe = "Yêu cầu rút tiền đã được ghi nhận",
                    TinNhan = $"Yêu cầu rút {soTien:N0} VNĐ từ ví về tài khoản {thongTinTaiKhoan} đã được ghi nhận. Tiền sẽ được chuyển vào tài khoản của bạn trong vòng 24h làm việc.",
                    MucDoQuanTrong = 1, // Thông báo thông thường
                    DuongDanChiTiet = "/GiaoDich/ViNguoiBan",
                    NgayTao = DateTime.Now
                };
                db.ThongBaos.Add(thongBaoNguoiBan);

                // Tạo thông báo cho admin về yêu cầu trở thành người bán
                var adminList = db.NguoiDungs.Where(n => n.VaiTro == "Admin").ToList();
                if (adminList != null && adminList.Count > 0)
                {
                    foreach (var admin in adminList)
                    {
                        var thongBaoAdmin = new ThongBao
                        {
                            MaNguoiDung = admin.MaNguoiDung, // Giả sử có trường MaQuanTriVien
                            LoaiThongBao = "Vi",
                            TieuDe = "Yêu cầu rút tiền mới cần xử lý",
                            TinNhan = $"Người bán (ID: {maNguoiBan}) đã yêu cầu rút {soTien:N0} VNĐ về tài khoản {thongTinTaiKhoan}. Vui lòng kiểm tra và xử lý yêu cầu này.",
                            MucDoQuanTrong = 2, // Thông báo quan trọng cần xử lý
                            DuongDanChiTiet = "/GiaoDich/WalletLogs" ,
                            NgayTao = DateTime.Now
                        };
                        db.ThongBaos.Add(thongBaoAdmin);
                    }
                }


                // Cập nhật số dư ví
                nguoiBan.SoDuVi -= soTien;
                db.Entry(nguoiBan).State = EntityState.Modified;

                db.SaveChanges();

                TempData["Success"] = "Yêu cầu rút tiền đã được ghi nhận. Tiền sẽ được chuyển vào tài khoản của bạn trong vòng 24h làm việc!";
                return RedirectToAction("QuanLyTaiKhoan", new { id = maNguoiBan });
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Đã xảy ra lỗi: " + ex.Message;
                return RedirectToAction("QuanLyTaiKhoan", new { id = maNguoiBan });
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

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult TraLoiDanhGia(int maDanhGia, string noiDungTraLoi)
        {
            try
            {
                // Lấy thông tin người dùng hiện tại
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

                // Kiểm tra đánh giá tồn tại
                var danhGia = db.DanhGiaSanPhams.Find(maDanhGia);
                if (danhGia == null)
                {
                    TempData["Error"] = "Không tìm thấy đánh giá!";
                    return RedirectToAction("QuanLyDanhGia", new { id = nguoiBan.MaNguoiBan });
                }

                // Kiểm tra xem đánh giá có thuộc sản phẩm của người bán này không
                var sanPham = db.SanPhams.Find(danhGia.MaSanPham);
                if (sanPham == null || sanPham.MaNguoiBan != nguoiBan.MaNguoiBan)
                {
                    TempData["Error"] = "Bạn không có quyền trả lời đánh giá này!";
                    return RedirectToAction("QuanLyDanhGia", new { id = nguoiBan.MaNguoiBan });
                }

                // Kiểm tra nội dung trả lời
                if (string.IsNullOrWhiteSpace(noiDungTraLoi))
                {
                    TempData["Error"] = "Nội dung trả lời không được để trống!";
                    return RedirectToAction("QuanLyDanhGia", new { id = nguoiBan.MaNguoiBan });
                }

                // Kiểm tra xem đã có phản hồi chưa, nếu có thì cập nhật
                var phanHoi = db.PhanHoiDanhGias.FirstOrDefault(p => p.MaDanhGiaSanPham == maDanhGia);
                if (phanHoi != null)
                {
                    // Cập nhật phản hồi hiện có
                    phanHoi.NoiDungPhanHoi = noiDungTraLoi;
                    phanHoi.NgayTao = DateTime.Now;
                    db.Entry(phanHoi).State = EntityState.Modified;
                }
                else
                {
                    // Tạo mới phản hồi
                    phanHoi = new PhanHoiDanhGia
                    {
                        MaDanhGiaSanPham = maDanhGia,
                        MaNguoiBan = nguoiBan.MaNguoiBan,
                        NoiDungPhanHoi = noiDungTraLoi,
                        NgayTao = DateTime.Now
                    };
                    db.PhanHoiDanhGias.Add(phanHoi);
                }

                // Lưu thay đổi
                db.SaveChanges();

                // Tạo thông báo cho người bán
                var thongBaoNguoiBan = new ThongBao
                {
                    MaNguoiDung = maNguoiDung,
                    LoaiThongBao = "DanhGia",
                    TieuDe = "Đã trả lời đánh giá thành công",
                    TinNhan = $"Bạn đã trả lời đánh giá cho sản phẩm {sanPham.TenSanPham} thành công.",
                    MucDoQuanTrong = 1, // Thông báo thông thường
                    DuongDanChiTiet = "/NguoiBans/QuanLyDanhGia/" + nguoiBan.MaNguoiBan,
                    NgayTao = DateTime.Now
                };
                db.ThongBaos.Add(thongBaoNguoiBan);

                // Tạo thông báo cho người mua về việc đánh giá đã được trả lời
                var thongBaoNguoiMua = new ThongBao
                {
                    MaNguoiDung = danhGia.MaNguoiDung,
                    LoaiThongBao = "DanhGia",
                    TieuDe = "Đánh giá của bạn đã được trả lời",
                    TinNhan = $"Người bán đã trả lời đánh giá của bạn về sản phẩm {sanPham.TenSanPham}.",
                    MucDoQuanTrong = 2, // Thông báo quan trọng
                    DuongDanChiTiet = "/Home/ChiTiet/" + danhGia.MaSanPham,
                    NgayTao = DateTime.Now
                };
                db.ThongBaos.Add(thongBaoNguoiMua);

                db.SaveChanges();

                TempData["Success"] = "Đã trả lời đánh giá thành công!";
                return RedirectToAction("QuanLyDanhGia", new { id = nguoiBan.MaNguoiBan });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi trả lời đánh giá: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi trả lời đánh giá. Vui lòng thử lại sau!";
                return RedirectToAction("QuanLyDanhGia", new { id = db.NguoiBans.FirstOrDefault(n => n.MaNguoiDung == GetCurrentUserId())?.MaNguoiBan });
            }
        }

        [Authorize]
        public ActionResult QuanLyDanhGia(int? id, int? page, string searchTerm = "",
    string statusFilter = "", string sortOrder = "newest", DateTime? startDate = null, DateTime? endDate = null)
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

            // Lấy danh sách sản phẩm của người bán
            var sanPhamIds = db.SanPhams
                .Where(s => s.MaNguoiBan == id)
                .Select(s => s.MaSanPham)
                .ToList();

            // Lấy danh sách đánh giá cho các sản phẩm của người bán
            var danhGiaQuery = db.DanhGiaSanPhams
                .Include(d => d.SanPham)
                .Include(d => d.NguoiDung)
                .Where(d => sanPhamIds.Contains(d.MaSanPham) && d.DaDanhGia == true);

            // Áp dụng bộ lọc tìm kiếm nếu có
            if (!string.IsNullOrEmpty(searchTerm))
            {
                danhGiaQuery = danhGiaQuery.Where(d =>
                    d.BinhLuan.Contains(searchTerm) ||
                    d.SanPham.TenSanPham.Contains(searchTerm) ||
                    d.NguoiDung.TenNguoiDung.Contains(searchTerm));
            }

            // Áp dụng bộ lọc số sao nếu có
            if (!string.IsNullOrEmpty(statusFilter))
            {
                if (statusFilter == "replied")
                {
                    var danhGiaDaTraLoiIds = db.PhanHoiDanhGias.Select(p => p.MaDanhGiaSanPham).ToList();
                    danhGiaQuery = danhGiaQuery.Where(d => danhGiaDaTraLoiIds.Contains(d.MaDanhGiaSanPham));
                }
                else if (statusFilter == "waiting")
                {
                    var danhGiaDaTraLoiIds = db.PhanHoiDanhGias.Select(p => p.MaDanhGiaSanPham).ToList();
                    danhGiaQuery = danhGiaQuery.Where(d => !danhGiaDaTraLoiIds.Contains(d.MaDanhGiaSanPham));
                }
                else
                {
                    int soSao;
                    if (int.TryParse(statusFilter, out soSao))
                    {
                        danhGiaQuery = danhGiaQuery.Where(d => d.DanhGia == soSao);
                    }
                }
            }

            // Áp dụng bộ lọc ngày tháng
            if (startDate.HasValue)
            {
                danhGiaQuery = danhGiaQuery.Where(d => d.NgayTao >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                DateTime adjustedEndDate = endDate.Value.AddDays(1).AddSeconds(-1);
                danhGiaQuery = danhGiaQuery.Where(d => d.NgayTao <= adjustedEndDate);
            }

            // Áp dụng sắp xếp
            switch (sortOrder)
            {
                case "oldest":
                    danhGiaQuery = danhGiaQuery.OrderBy(d => d.NgayTao);
                    break;
                case "ratingAsc":
                    danhGiaQuery = danhGiaQuery.OrderBy(d => d.DanhGia);
                    break;
                case "ratingDesc":
                    danhGiaQuery = danhGiaQuery.OrderByDescending(d => d.DanhGia);
                    break;
                case "productAsc":
                    danhGiaQuery = danhGiaQuery.OrderBy(d => d.SanPham.TenSanPham);
                    break;
                case "productDesc":
                    danhGiaQuery = danhGiaQuery.OrderByDescending(d => d.SanPham.TenSanPham);
                    break;
                default: // newest
                    danhGiaQuery = danhGiaQuery.OrderByDescending(d => d.NgayTao);
                    break;
            }

            // Đếm tổng số đánh giá sau khi lọc
            int totalItems = danhGiaQuery.Count();

            // Tính tổng số trang
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Đảm bảo trang hiện tại không vượt quá tổng số trang
            if (pageNumber > totalPages && totalPages > 0)
            {
                pageNumber = 1;
                return RedirectToAction("QuanLyDanhGia", new
                {
                    id = id,
                    page = pageNumber,
                    searchTerm = searchTerm,
                    statusFilter = statusFilter,
                    sortOrder = sortOrder,
                    startDate = startDate,
                    endDate = endDate
                });
            }

            // Lấy đánh giá theo trang
            var danhGiaTrang = danhGiaQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Lấy danh sách ID đánh giá
            var danhGiaIds = danhGiaTrang.Select(d => d.MaDanhGiaSanPham).ToList();

            // Lấy thông tin phản hồi từ người bán
            var phanHoiList = db.PhanHoiDanhGias
                .Where(p => danhGiaIds.Contains(p.MaDanhGiaSanPham))
                .ToList();

            // Tính điểm đánh giá trung bình
            double? trungBinhDanhGia = null;
            if (totalItems > 0)
            {
                trungBinhDanhGia = danhGiaQuery.Average(d => (double)d.DanhGia);
            }

            // Thống kê số lượng đánh giá theo số sao
            var thongKeDanhGia = danhGiaQuery
                .GroupBy(d => d.DanhGia)
                .Select(g => new { SoSao = g.Key, SoLuong = g.Count() })
                .ToDictionary(x => x.SoSao, x => x.SoLuong);

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
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.TrungBinhDanhGia = trungBinhDanhGia;
            ViewBag.ThongKeDanhGia = thongKeDanhGia;
            ViewBag.PhanHoiList = phanHoiList;

            return View(danhGiaTrang);
        }
        //21/4/2025
        public ActionResult KiemTraSanPhamHetHang()
        {
            try
            {
                // Chỉ thực hiện nếu người dùng đã đăng nhập và là người bán
                if (!User.Identity.IsAuthenticated || Session["VaiTro"] as string != "Seller")
                {
                    return Json(new { success = false, message = "Không có quyền truy cập" }, JsonRequestBehavior.AllowGet);
                }

                // Lấy thông tin người bán
                int maNguoiDung = GetCurrentUserId();
                var nguoiBan = db.NguoiBans.FirstOrDefault(n => n.MaNguoiDung == maNguoiDung);
                if (nguoiBan == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin người bán" }, JsonRequestBehavior.AllowGet);
                }

                // Lấy danh sách sản phẩm đã hết hàng
                var sanPhamHetHang = db.SanPhams
                    .Where(s => s.MaNguoiBan == nguoiBan.MaNguoiBan && s.SoLuongTonKho == 0 && s.TrangThai == "Đã phê duyệt")
                    .ToList();

                // Lấy danh sách sản phẩm sắp hết hàng
                var sanPhamGanHet = db.SanPhams
                    .Where(s => s.MaNguoiBan == nguoiBan.MaNguoiBan && s.SoLuongTonKho > 0 && s.SoLuongTonKho <= 5 && s.TrangThai == "Đã phê duyệt")
                    .ToList();

                // Tạo thông báo cho sản phẩm hết hàng
                foreach (var sanPham in sanPhamHetHang)
                {
                    // Kiểm tra xem đã gửi thông báo cho sản phẩm này trong 24h qua chưa
                    // Tính toán thời gian trước khi sử dụng trong truy vấn
                    DateTime ngayHomQua = DateTime.Now.AddHours(-24);

                    // Sử dụng biến trong truy vấn
                    var daGuiThongBao = db.ThongBaos
                        .Any(tb => tb.MaNguoiDung == maNguoiDung
                                && tb.LoaiThongBao == "SanPham"
                                && tb.TieuDe.Contains("hết hàng")
                                && tb.TinNhan.Contains(sanPham.MaSanPham.ToString())
                                && tb.NgayTao >= ngayHomQua);
                    if (!daGuiThongBao)
                    {
                        var thongBao = new ThongBao
                        {
                            MaNguoiDung = maNguoiDung,
                            LoaiThongBao = "SanPham",
                            TieuDe = "Sản phẩm hết hàng",
                            TinNhan = $"Sản phẩm {sanPham.TenSanPham} (Mã: {sanPham.MaSanPham}) đã hết hàng. Vui lòng cập nhật số lượng để tiếp tục bán hàng.",
                            MucDoQuanTrong = 2, // Quan trọng
                            DuongDanChiTiet = $"/NguoiBans/QuanLySanPham/{nguoiBan.MaNguoiBan}",
                            NgayTao = DateTime.Now,
                            TrangThai = "Chưa đọc"
                        };

                        db.ThongBaos.Add(thongBao);
                    }
                }

                // Tạo thông báo cho sản phẩm sắp hết hàng
                foreach (var sanPham in sanPhamGanHet)
                {
                    // Kiểm tra xem đã gửi thông báo cho sản phẩm này trong 24h qua chưa
                    // Tính toán thời gian trước khi sử dụng trong truy vấn
                    DateTime ngayHomQua = DateTime.Now.AddHours(-24);

                    // Sử dụng biến trong truy vấn
                    var daGuiThongBao = db.ThongBaos
                        .Any(tb => tb.MaNguoiDung == maNguoiDung
                                && tb.LoaiThongBao == "SanPham"
                                && tb.TieuDe.Contains("hết hàng")
                                && tb.TinNhan.Contains(sanPham.MaSanPham.ToString())
                                && tb.NgayTao >= ngayHomQua);
                    if (!daGuiThongBao)
                    {
                        var thongBao = new ThongBao
                        {
                            MaNguoiDung = maNguoiDung,
                            LoaiThongBao = "SanPham",
                            TieuDe = "Sản phẩm sắp hết hàng",
                            TinNhan = $"Sản phẩm {sanPham.TenSanPham} (Mã: {sanPham.MaSanPham}) chỉ còn {sanPham.SoLuongTonKho} sản phẩm. Vui lòng cập nhật số lượng kịp thời.",
                            MucDoQuanTrong = 1, // Thông thường
                            DuongDanChiTiet = $"/NguoiBans/QuanLySanPham/{nguoiBan.MaNguoiBan}",
                            NgayTao = DateTime.Now,
                            TrangThai = "Chưa đọc"
                        };

                        db.ThongBaos.Add(thongBao);
                    }
                }

                db.SaveChanges();

                return Json(new
                {
                    success = true,
                    hetHang = sanPhamHetHang.Count,
                    ganHet = sanPhamGanHet.Count
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
        //21/4/2025
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

        // Tạo một lớp rõ ràng thay vì sử dụng kiểu ẩn danh
        public class SanPhamBanChayViewModel
        {
            public int MaSanPham { get; set; }
            public string TenSanPham { get; set; }
            public int SoLuongBan { get; set; }
            public decimal TongTien { get; set; }
            public string AnhDaiDien { get; set; }
        }

}