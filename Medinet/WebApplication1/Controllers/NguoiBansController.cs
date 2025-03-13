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
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
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
        public ActionResult QuanLySanPham(int? id)
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

            // Lấy danh sách sản phẩm của người bán kèm theo danh sách ảnh
            var sanPhams = db.SanPhams
                .Include(s => s.DanhMucSanPham)
                .Include(s => s.DanhSachAnhSanPham)  // Include thêm danh sách ảnh
                .Where(s => s.MaNguoiBan == id)
                .ToList();

            // Đặt thông tin người bán vào ViewBag để hiển thị
            ViewBag.MaNguoiBan = id;
            ViewBag.TenCuaHang = nguoiBan.TenCuaHang;

            return View(sanPhams);
        }

        // GET: NguoiBans/ThemSanPham/5
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

            return View(new SanPham { MaNguoiBan = (int)id, NgayTao = DateTime.Now, TrangThai = "Đang chờ duyệt" });
        }

        // POST: NguoiBans/ThemSanPham
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemSanPham([Bind(Include = "MaSanPham,MaNguoiBan,MaDanhMuc,TenSanPham,MoTaSanPham,GiaSanPham,SoLuongTonKho,TrangThai,ThongTin,SoLuotMua,ThuongHieu,DoiTuongSuDung,HuongDanSuDung,KhoiLuong")] SanPham sanPham, HttpPostedFileBase[] anhSanPham)
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
                            var path = Path.Combine(Server.MapPath("~/Content/Images/Products"), uniqueFileName);

                            // Lưu file ảnh
                            anh.SaveAs(path);

                            // Tạo bản ghi trong bảng AnhSanPham
                            var anhSP = new AnhSanPham
                            {
                                MaSanPham = sanPham.MaSanPham,
                                DuongDanAnh = "/Content/Images/Products/" + uniqueFileName
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
        public ActionResult ChiTietSanPham(int? id)
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

        // GET: NguoiBans/SuaSanPham/5
        public ActionResult SuaSanPham(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            SanPham sanPham = db.SanPhams.Find(id);
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

            // Chuẩn bị dữ liệu cho form
            ViewBag.MaNguoiBan = sanPham.MaNguoiBan;
            NguoiBan nguoiBan = db.NguoiBans.Find(sanPham.MaNguoiBan);
            ViewBag.TenCuaHang = nguoiBan?.TenCuaHang;
            ViewBag.MaDanhMuc = new SelectList(db.DanhMucSanPhams, "MaDanhMuc", "TenDanhMuc", sanPham.MaDanhMuc);

            return View(viewModel);
        }

        // POST: NguoiBans/SuaSanPham/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SuaSanPham([Bind(Include = "MaSanPham,MaNguoiBan,MaDanhMuc,TenSanPham,MoTaSanPham,GiaSanPham,SoLuongTonKho,TrangThai,ThongTin,SoLuotMua,ThuongHieu,DoiTuongSuDung,HuongDanSuDung,KhoiLuong,AnhSanPham")] SanPham sanPham, HttpPostedFileBase[] anhSanPham)
        {
            if (ModelState.IsValid)
            {
                // Cập nhật ngày cập nhật
                sanPham.NgayCapNhat = DateTime.Now;

                // Cập nhật vào cơ sở dữ liệu
                db.Entry(sanPham).State = EntityState.Modified;
                db.SaveChanges();

                // Xử lý upload nhiều ảnh mới
                if (anhSanPham != null && anhSanPham.Length > 0)
                {
                    foreach (var anh in anhSanPham)
                    {
                        if (anh != null && anh.ContentLength > 0)
                        {
                            // Tạo tên file duy nhất
                            var fileName = Path.GetFileName(anh.FileName);
                            var uniqueFileName = DateTime.Now.Ticks + "_" + fileName;
                            var path = Path.Combine(Server.MapPath("~/Content/Images/Products"), uniqueFileName);

                            // Lưu file ảnh
                            anh.SaveAs(path);

                            // Tạo bản ghi trong bảng AnhSanPham
                            var anhSP = new AnhSanPham
                            {
                                MaSanPham = sanPham.MaSanPham,
                                DuongDanAnh = "/Content/Images/Products/" + uniqueFileName
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

            // Lấy lại danh sách ảnh
            var danhSachAnh = db.AnhSanPhams.Where(a => a.MaSanPham == sanPham.MaSanPham).ToList();

            // Tạo ViewModel
            var viewModel = new SanPhamViewModel
            {
                SanPham = sanPham,
                DanhSachAnh = danhSachAnh
            };

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