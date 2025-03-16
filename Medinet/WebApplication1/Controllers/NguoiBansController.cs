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
        public ActionResult ThongTinNguoiBan(int? id)
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

            // Tạo ViewModel với dữ liệu người bán
            var viewModel = new NguoiBanViewModel
            {
                NguoiDung = nguoiBan.NguoiDung,
                NguoiBan = nguoiBan,
                DanhSachChungChi = nguoiBan.DanhSachChungChi.ToList()
            };

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

            ViewBag.MaNguoiBan = sanPham.MaNguoiBan;
            ViewBag.TenCuaHang = sanPham.NguoiBan.TenCuaHang;

            return View(viewModel);
        }

        // POST: NguoiBans/XoaSanPham/5
        [HttpPost, ActionName("XoaSanPham")]
        [ValidateAntiForgeryToken]
        public ActionResult XoaSanPhamConfirmed(int id)
        {
            SanPham sanPham = db.SanPhams.Find(id);
            int maNguoiBan = sanPham.MaNguoiBan;

            // Xóa tất cả các ảnh liên quan đến sản phẩm
            var danhSachAnh = db.AnhSanPhams.Where(a => a.MaSanPham == id).ToList();
            foreach (var anh in danhSachAnh)
            {
                db.AnhSanPhams.Remove(anh);
            }

            // Xóa sản phẩm
            db.SanPhams.Remove(sanPham);
            db.SaveChanges();

            return RedirectToAction("QuanLySanPham", new { id = maNguoiBan });
        }

        // GET: NguoiBans/XoaAnhSanPham/5
        public ActionResult XoaAnhSanPham(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            AnhSanPham anhSanPham = db.AnhSanPhams
                .Include(a => a.SanPham)
                .FirstOrDefault(a => a.MaAnh == id);

            if (anhSanPham == null)
            {
                return HttpNotFound();
            }

            return View(anhSanPham);
        }

        // POST: NguoiBans/XoaAnhSanPham/5
        [HttpPost, ActionName("XoaAnhSanPham")]
        [ValidateAntiForgeryToken]
        public ActionResult XoaAnhSanPhamConfirmed(int id)
        {
            AnhSanPham anhSanPham = db.AnhSanPhams.Find(id);
            int maSanPham = anhSanPham.MaSanPham;

            db.AnhSanPhams.Remove(anhSanPham);
            db.SaveChanges();

            return RedirectToAction("SuaSanPham", new { id = maSanPham });
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
        }

        public class SanPhamViewModel
        {
            public SanPham SanPham { get; set; }
            public List<AnhSanPham> DanhSachAnh { get; set; }
        }
}