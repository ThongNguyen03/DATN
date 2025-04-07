using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private MedinetDATN db = new MedinetDATN();

        // GET: Admin/UserManagement
        public ActionResult UserManagement(int page = 1, string role = "", string status = "")
        {
            int pageSize = 10; // Số người dùng hiển thị trên mỗi trang
            var query = db.NguoiDungs.AsQueryable();

            // Áp dụng bộ lọc vai trò
            if (!string.IsNullOrEmpty(role))
            {
                query = query.Where(u => u.VaiTro == role);
            }

            // Áp dụng bộ lọc trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Active" || status == "Inactive" || status == "Banned")
                {
                    query = query.Where(u => u.TrangThai == status);
                }
                else if (status == "Upgrade")
                {
                    // Lọc những người đang chờ nâng cấp thành người bán
                    query = query.Where(u => u.VaiTro != "Seller" && u.XetDuyetThanhNguoiBan == true);
                }
            }

            // Tính toán tổng số trang
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Đảm bảo trang hiện tại hợp lệ
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            // Lấy dữ liệu cho trang hiện tại
            var users = query
                .OrderByDescending(u => u.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Truyền dữ liệu phân trang vào ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;

            // Truyền các tham số lọc vào ViewBag để duy trì trạng thái khi phân trang
            ViewBag.Role = role;
            ViewBag.Status = status;

            return View(users);
        }

        // GET: Admin/UserDetails/5
        public ActionResult UserDetails(int id)
        {
            var nguoiDung = db.NguoiDungs.Find(id);
            if (nguoiDung == null)
            {
                return HttpNotFound();
            }

            // Nếu là người bán hoặc đang chờ nâng cấp
            if (nguoiDung.VaiTro == "Seller" || nguoiDung.TrangThai == "Upgrade" || nguoiDung.XetDuyetThanhNguoiBan == true)
            {
                // Tìm thông tin người bán (có thể đã được tạo khi đăng ký nâng cấp)
                var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
                ViewBag.NguoiBan = nguoiBan;

                if (nguoiBan != null)
                {
                    // Lấy danh sách chứng chỉ của người bán
                    var danhSachChungChi = db.AnhChungChis
                        .Where(c => c.MaNguoiBan == nguoiBan.MaNguoiBan)
                        .ToList();

                    // In thông tin về số lượng chứng chỉ tìm thấy để debug
                    System.Diagnostics.Debug.WriteLine("Tìm thấy " + danhSachChungChi.Count + " chứng chỉ cho người bán ID: " + nguoiBan.MaNguoiBan);

                    ViewBag.DanhSachChungChi = danhSachChungChi;
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("Không tìm thấy thông tin người bán cho người dùng ID: " + nguoiDung.MaNguoiDung);
                    // Xử lý khi không có thông tin người bán nhưng đang xin nâng cấp
                    ViewBag.DanhSachChungChi = new List<WebApplication1.Models.AnhChungChi>();
                }
            }

            return View(nguoiDung);
        }

        // POST: Admin/RestoreUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RestoreUser(int id)
        {
            try
            {
                var nguoiDung = db.NguoiDungs.Find(id);
                if (nguoiDung == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Kiểm tra trạng thái hiện tại
                if (nguoiDung.TrangThai != "Inactive")
                {
                    return Json(new { success = false, message = "Người dùng không ở trạng thái bị xóa." });
                }

                // Khôi phục trạng thái Active
                nguoiDung.TrangThai = "Active";
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/LockUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LockUser(int id)
        {
            try
            {
                var nguoiDung = db.NguoiDungs.Find(id);
                if (nguoiDung == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Kiểm tra xem có phải Admin không
                if (nguoiDung.VaiTro == "Admin")
                {
                    return Json(new { success = false, message = "Không thể khóa tài khoản Admin." });
                }

                nguoiDung.TrangThai = "Banned";
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/UnlockUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UnlockUser(int id)
        {
            try
            {
                var nguoiDung = db.NguoiDungs.Find(id);
                if (nguoiDung == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Chỉ mở khóa nếu trạng thái hiện tại là Banned
                if (nguoiDung.TrangThai != "Banned")
                {
                    return Json(new { success = false, message = "Người dùng không ở trạng thái bị khóa." });
                }

                nguoiDung.TrangThai = "Active";
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/UpgradeToSeller/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpgradeToSeller(int id)
        {
            try
            {
                var nguoiDung = db.NguoiDungs.Find(id);
                if (nguoiDung == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }
                // Cập nhật vai trò người dùng
                nguoiDung.VaiTro = "Seller";
                nguoiDung.XetDuyetThanhNguoiBan = false;
                nguoiDung.TrangThai = "Active";
                db.SaveChanges();
                return Json(new { success = true, message = "Nâng cấp thành người bán thành công." });
                //// Kiểm tra xem người dùng có phải là người bán hay không
                //var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
                //if (nguoiBan != null)
                //{
                //    // Cập nhật vai trò người dùng
                //    nguoiDung.VaiTro = "Seller";
                //    nguoiDung.XetDuyetThanhNguoiBan = false;
                //    nguoiDung.TrangThai = "Active";
                //    return Json(new { success = true, message = "Nâng cấp thành người bán thành công." });
                //}
                ////else
                ////{
                ////    // Kiểm tra xem đã có thông tin người bán chưa
                ////    var nguoiban = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);
                ////    if (nguoiban == null)
                ////    {
                ////        // Tạo thông tin người bán mặc định
                ////        nguoiban = new WebApplication1.Models.NguoiBan
                ////        {
                ////            MaNguoiDung = nguoiDung.MaNguoiDung,
                ////            TenCuaHang = nguoiDung.TenNguoiDung + "'s Shop",
                ////            MoTaCuaHang = "Cửa hàng mới",
                ////            DiaChiCuaHang = "Chưa cập nhật",
                ////            SoDienThoaiCuaHang = nguoiDung.SoDienThoai,
                ////            NgayTao = DateTime.Now
                ////        };

                ////        db.NguoiBans.Add(nguoiban);
                ////        return Json(new { success = true, message = "Nâng cấp thành người bán thành công." });

                ////    }
                ////}

                //db.SaveChanges();

                //return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/DeleteUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int id)
        {
            try
            {
                var nguoiDung = db.NguoiDungs.Find(id);
                if (nguoiDung == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Kiểm tra xem có phải Admin không
                if (nguoiDung.VaiTro == "Admin")
                {
                    return Json(new { success = false, message = "Không thể xóa tài khoản Admin." });
                }

                // Đánh dấu là đã xóa thay vì xóa thật sự
                nguoiDung.TrangThai = "Inactive";
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/RejectUpgradeToSeller/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectUpgradeToSeller(int id)
        {
            try
            {
                var nguoiDung = db.NguoiDungs.Find(id);
                if (nguoiDung == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });
                }

                // Kiểm tra xem người dùng có yêu cầu nâng cấp không
                if (nguoiDung.XetDuyetThanhNguoiBan != true)
                {
                    return Json(new { success = false, message = "Người dùng không có yêu cầu nâng cấp." });
                }

                // Tìm thông tin người bán
                var nguoiBan = db.NguoiBans.FirstOrDefault(s => s.MaNguoiDung == nguoiDung.MaNguoiDung);

                if (nguoiBan != null)
                {
                    // Tìm và xóa tất cả chứng chỉ của người bán
                    var danhSachChungChi = db.AnhChungChis.Where(c => c.MaNguoiBan == nguoiBan.MaNguoiBan).ToList();

                    foreach (var chungChi in danhSachChungChi)
                    {
                        // Xóa file ảnh chứng chỉ nếu có
                        if (!string.IsNullOrEmpty(chungChi.DuongDanAnh))
                        {
                            var duongDanFile = Server.MapPath(chungChi.DuongDanAnh);
                            if (System.IO.File.Exists(duongDanFile))
                            {
                                try
                                {
                                    System.IO.File.Delete(duongDanFile);
                                }
                                catch (Exception ex)
                                {
                                    // Ghi log lỗi nếu không xóa được file
                                    System.Diagnostics.Debug.WriteLine("Không thể xóa file: " + ex.Message);
                                }
                            }
                        }

                        // Xóa chứng chỉ khỏi database
                        db.AnhChungChis.Remove(chungChi);
                    }

                    // Xóa thông tin người bán
                    db.NguoiBans.Remove(nguoiBan);
                }


                // Hủy trạng thái chờ nâng cấp
                nguoiDung.XetDuyetThanhNguoiBan = false;

                // Đảm bảo vai trò và trạng thái phù hợp
                if (nguoiDung.VaiTro != "Admin")
                {
                    nguoiDung.VaiTro = "Buyer";
                }

                if (nguoiDung.TrangThai == "Upgrade")
                {
                    nguoiDung.TrangThai = "Active";
                }

                // Đánh dấu đã bị từ chối để hiển thị thông báo
                nguoiDung.BiTuChoiNangCap = true;
                nguoiDung.NgayTuChoiNangCap = DateTime.Now;

                db.SaveChanges();

                return Json(new { success = true, message = "Đã từ chối nâng cấp và xóa dữ liệu liên quan." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // GET: Admin/UserStatistics
        public ActionResult UserStatistics()
        {
            ViewBag.TotalUsers = db.NguoiDungs.Count();
            ViewBag.ActiveUsers = db.NguoiDungs.Count(u => u.TrangThai == "Active");
            ViewBag.InactiveUsers = db.NguoiDungs.Count(u => u.TrangThai == "Inactive");
            ViewBag.LockedUsers = db.NguoiDungs.Count(u => u.TrangThai == "Locked");
            ViewBag.Buyers = db.NguoiDungs.Count(u => u.VaiTro == "Buyer");
            ViewBag.Sellers = db.NguoiDungs.Count(u => u.VaiTro == "Seller");
            ViewBag.Admins = db.NguoiDungs.Count(u => u.VaiTro == "Admin");

            return View();
        }



        // GET: Admin/ProductManagement
        public ActionResult ProductManagement(int page = 1, string category = "", string status = "")
        {
            int pageSize = 10; // Số sản phẩm hiển thị trên mỗi trang
            var query = db.SanPhams.AsQueryable();

            // Áp dụng bộ lọc danh mục
            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(p => p.MaDanhMuc.ToString() == category);
            }

            // Áp dụng bộ lọc trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                if (status == "Đã phê duyệt" || status == "Đang chờ xử lý" || status == "Bị từ chối")
                {
                    query = query.Where(p => p.TrangThai == status);
                }
            }

            // Tính toán tổng số trang
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Đảm bảo trang hiện tại hợp lệ
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            // Lấy dữ liệu cho trang hiện tại
            var products = query
                .OrderByDescending(p => p.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Chuẩn bị thêm dữ liệu cho mỗi sản phẩm
            var danhSachAnh = new Dictionary<int, string>();
            foreach (var product in products)
            {
                // Lấy ảnh đầu tiên của sản phẩm (không quan tâm đến LaChinh)
                var anhDauTien = db.AnhSanPhams.Where(a => a.MaSanPham == product.MaSanPham)
                                               .OrderBy(a => a.MaAnh)
                                               .FirstOrDefault();
                var duongDanAnh = anhDauTien != null ? anhDauTien.DuongDanAnh : "~/Content/Images/default-product.png";
                danhSachAnh[product.MaSanPham] = duongDanAnh;
            }

            ViewBag.DanhSachAnh = danhSachAnh;

            // Truyền dữ liệu phân trang vào ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;

            // Truyền các tham số lọc vào ViewBag để duy trì trạng thái khi phân trang
            ViewBag.Category = category;
            ViewBag.Status = status;

            // Lấy danh sách danh mục để hiển thị trong dropdown lọc
            ViewBag.Categories = db.DanhMucSanPhams.ToList();

            return View(products);
        }

        // GET: Admin/ProductDetails/5
        public ActionResult ProductDetails(int id)
        {
            var sanPham = db.SanPhams.Find(id);
            if (sanPham == null)
            {
                return HttpNotFound();
            }

            // Lấy thông tin danh mục
            var danhMuc = db.DanhMucSanPhams.Find(sanPham.MaDanhMuc);
            ViewBag.DanhMuc = danhMuc;

            // Lấy thông tin người bán
            var nguoiBan = db.NguoiBans.Find(sanPham.MaNguoiBan);
            ViewBag.NguoiBan = nguoiBan;

            // Lấy danh sách ảnh sản phẩm theo thứ tự
            var danhSachAnh = db.AnhSanPhams
                .Where(a => a.MaSanPham == sanPham.MaSanPham)
                .OrderBy(a => a.MaAnh)
                .ToList();
            ViewBag.DanhSachAnh = danhSachAnh;

            return View(sanPham);
        }

        // POST: Admin/ApproveProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApproveProduct(int id)
        {
            try
            {
                var sanPham = db.SanPhams.Find(id);
                if (sanPham == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                }

                // Kiểm tra trạng thái hiện tại
                if (sanPham.TrangThai == "Đã phê duyệt")
                {
                    return Json(new { success = false, message = "Sản phẩm đã được duyệt." });
                }

                // Duyệt sản phẩm
                sanPham.TrangThai = "Đã phê duyệt";
                sanPham.NgayCapNhat = DateTime.Now;
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/RejectProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RejectProduct(int id)
        {
            try
            {
                var sanPham = db.SanPhams.Find(id);
                if (sanPham == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                }

                // Kiểm tra trạng thái hiện tại
                if (sanPham.TrangThai == "Bị từ chối")
                {
                    return Json(new { success = false, message = "Sản phẩm đã bị từ chối." });
                }

                // Từ chối sản phẩm
                sanPham.TrangThai = "Bị từ chối";
                sanPham.NgayCapNhat = DateTime.Now;
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        // POST: Admin/DeleteProduct/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProduct(int id)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var sanPham = db.SanPhams.Find(id);
                    if (sanPham == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy sản phẩm." });
                    }

                    // 1. Xóa hết các dữ liệu liên quan đến sản phẩm này

                    // Xóa các chi tiết đơn hàng liên quan
                    var chiTietDonHangs = db.ChiTietDonHangs.Where(c => c.MaSanPham == id).ToList();
                    db.ChiTietDonHangs.RemoveRange(chiTietDonHangs);

                    // Xóa các mục giỏ hàng liên quan
                    var gioHangs = db.GioHangs.Where(g => g.MaSanPham == id).ToList();
                    db.GioHangs.RemoveRange(gioHangs);

                    // Xóa các bình luận/đánh giá liên quan
                    var danhGias = db.DanhGiaSanPhams.Where(d => d.MaSanPham == id).ToList();
                    db.DanhGiaSanPhams.RemoveRange(danhGias);

                    // Xóa các ảnh sản phẩm
                    var anhSanPhams = db.AnhSanPhams.Where(a => a.MaSanPham == id).ToList();
                    db.AnhSanPhams.RemoveRange(anhSanPhams);

                    // 2. Xóa sản phẩm
                    db.SanPhams.Remove(sanPham);

                    // 3. Lưu thay đổi và commit transaction
                    db.SaveChanges();
                    transaction.Commit();

                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Lỗi: " + ex.Message });
                }
            }
        }



        // GET: Admin/CategoryManagement
        public ActionResult CategoryManagement(int page = 1, string searchTerm = "")
        {
            int pageSize = 10; // Number of categories per page
            var query = db.DanhMucSanPhams.AsQueryable();

            // Apply search filter if provided
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.TenDanhMuc.Contains(searchTerm));
            }

            // Calculate total pages
            int totalItems = query.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Ensure current page is valid
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            // Get data for the current page
            var categories = query
                .OrderByDescending(c => c.NgayTao)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Pass pagination data to ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.PageSize = pageSize;
            ViewBag.SearchTerm = searchTerm;

            // Get product count for each category
            var categoryCounts = new Dictionary<int, int>();
            foreach (var category in categories)
            {
                categoryCounts[category.MaDanhMuc] = db.SanPhams.Count(p => p.MaDanhMuc == category.MaDanhMuc);
            }
            ViewBag.CategoryCounts = categoryCounts;

            return View(categories);
        }

        // GET: Admin/CategoryDetails/5
        public ActionResult CategoryDetails(int id)
        {
            var danhMuc = db.DanhMucSanPhams.Find(id);
            if (danhMuc == null)
            {
                return HttpNotFound();
            }

            // Lấy thông tin danh mục cha nếu có
            if (danhMuc.MaDanhMucCha.HasValue)
            {
                var danhMucCha = db.DanhMucSanPhams.Find(danhMuc.MaDanhMucCha.Value);
                ViewBag.ParentCategory = danhMucCha;
            }

            // Lấy danh sách các danh mục con
            var danhMucCon = db.DanhMucSanPhams.Where(c => c.MaDanhMucCha == id).ToList();
            ViewBag.ChildCategories = danhMucCon;

            // Lấy sản phẩm trong danh mục
            var sanPhamTrongDanhMuc = db.SanPhams.Where(p => p.MaDanhMuc == id).ToList();
            ViewBag.SanPhamTrongDanhMuc = sanPhamTrongDanhMuc;
            ViewBag.SoLuongSanPham = sanPhamTrongDanhMuc.Count();

            return View(danhMuc);
        }

        // GET: Admin/CreateCategory
        public ActionResult CreateCategory()
        {
            // Get list of categories for parent selection
            ViewBag.ParentCategories = db.DanhMucSanPhams.ToList();

            return View();
        }

        // POST: Admin/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCategory([Bind(Include = "TenDanhMuc,MaDanhMucCha")] DanhMucSanPham danhMuc, HttpPostedFileBase categoryImage)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload hình ảnh nếu có
                if (categoryImage != null && categoryImage.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(categoryImage.FileName);
                    string extension = Path.GetExtension(fileName);

                    // Tạo tên file mới để tránh trùng lặp
                    string newFileName = "category_" + Guid.NewGuid().ToString().Substring(0, 8) + extension;

                    // Tạo đường dẫn lưu file
                    string relativePath = "~/Content/Images/Categories/" + newFileName;
                    string physicalPath = Server.MapPath(relativePath);

                    // Đảm bảo thư mục tồn tại
                    string directoryPath = Path.GetDirectoryName(physicalPath);
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    // Lưu file
                    categoryImage.SaveAs(physicalPath);

                    // Cập nhật đường dẫn ảnh cho danh mục
                    danhMuc.AnhDanhMucSanPham = relativePath;
                }

                danhMuc.NgayTao = DateTime.Now;
                db.DanhMucSanPhams.Add(danhMuc);
                db.SaveChanges();

                return RedirectToAction("CategoryManagement");
            }

            // Nếu không thành công, hiển thị lại form với thông báo lỗi
            ViewBag.ParentCategories = db.DanhMucSanPhams.ToList();
            return View(danhMuc);
        }

        // GET: Admin/EditCategory/5
        public ActionResult EditCategory(int id)
        {
            var danhMuc = db.DanhMucSanPhams.Find(id);
            if (danhMuc == null)
            {
                return HttpNotFound();
            }

            // Get all categories except the current one and its children for parent selection
            ViewBag.ParentCategories = db.DanhMucSanPhams
                .Where(c => c.MaDanhMuc != id)
                .ToList();

            return View(danhMuc);
        }

        // POST: Admin/EditCategory/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCategory([Bind(Include = "MaDanhMuc,TenDanhMuc,AnhDanhMucSanPham,MaDanhMucCha,NgayTao")] DanhMucSanPham danhMuc, HttpPostedFileBase categoryImage, bool? removeImage = false)
        {
            if (ModelState.IsValid)
            {
                var existingCategory = db.DanhMucSanPhams.Find(danhMuc.MaDanhMuc);
                if (existingCategory != null)
                {
                    existingCategory.TenDanhMuc = danhMuc.TenDanhMuc;
                    existingCategory.MaDanhMucCha = danhMuc.MaDanhMucCha;

                    // Xử lý ảnh danh mục
                    if (removeImage == true)
                    {
                        // Xóa file ảnh cũ nếu có
                        if (!string.IsNullOrEmpty(existingCategory.AnhDanhMucSanPham))
                        {
                            string physicalPath = Server.MapPath(existingCategory.AnhDanhMucSanPham);
                            if (System.IO.File.Exists(physicalPath))
                            {
                                try
                                {
                                    System.IO.File.Delete(physicalPath);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine("Không thể xóa file: " + ex.Message);
                                    // Ghi log lỗi
                                }
                            }
                        }

                        existingCategory.AnhDanhMucSanPham = null;
                    }

                    // Xử lý upload hình ảnh mới nếu có
                    if (categoryImage != null && categoryImage.ContentLength > 0)
                    {
                        // Xóa file ảnh cũ nếu có
                        if (!string.IsNullOrEmpty(existingCategory.AnhDanhMucSanPham))
                        {
                            string oldPhysicalPath = Server.MapPath(existingCategory.AnhDanhMucSanPham);
                            if (System.IO.File.Exists(oldPhysicalPath))
                            {
                                try
                                {
                                    System.IO.File.Delete(oldPhysicalPath);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine("Không thể xóa file: " + ex.Message);
                                }
                            }
                        }

                        string fileName = Path.GetFileName(categoryImage.FileName);
                        string extension = Path.GetExtension(fileName);

                        // Tạo tên file mới để tránh trùng lặp
                        string newFileName = "category_" + Guid.NewGuid().ToString().Substring(0, 8) + extension;

                        // Tạo đường dẫn lưu file
                        string relativePath = "~/Content/Images/Categories/" + newFileName;
                        string physicalPath = Server.MapPath(relativePath);

                        // Đảm bảo thư mục tồn tại
                        string directoryPath = Path.GetDirectoryName(physicalPath);
                        if (!Directory.Exists(directoryPath))
                        {
                            Directory.CreateDirectory(directoryPath);
                        }

                        // Lưu file
                        categoryImage.SaveAs(physicalPath);

                        // Cập nhật đường dẫn ảnh cho danh mục
                        existingCategory.AnhDanhMucSanPham = relativePath;
                    }

                    db.SaveChanges();
                    return RedirectToAction("CategoryDetails", new { id = danhMuc.MaDanhMuc });
                }
            }

            // If we got this far, something failed, redisplay form
            ViewBag.ParentCategories = db.DanhMucSanPhams
                .Where(c => c.MaDanhMuc != danhMuc.MaDanhMuc)
                .ToList();

            return View(danhMuc);
        }

        // POST: Admin/DeleteCategory/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCategory(int id)
        {
            try
            {
                var danhMuc = db.DanhMucSanPhams.Find(id);
                if (danhMuc == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy danh mục." });
                }

                // Check if there are products in this category
                bool hasProducts = db.SanPhams.Any(p => p.MaDanhMuc == id);
                if (hasProducts)
                {
                    return Json(new { success = false, message = "Không thể xóa danh mục này vì có sản phẩm thuộc danh mục." });
                }

                // Check if there are child categories
                bool hasChildren = db.DanhMucSanPhams.Any(c => c.MaDanhMucCha == id);
                if (hasChildren)
                {
                    return Json(new { success = false, message = "Không thể xóa danh mục này vì có danh mục con." });
                }

                db.DanhMucSanPhams.Remove(danhMuc);
                db.SaveChanges();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost]
        public ActionResult EmptyForm()
        {
            // This is just an empty action to support the CSRF token form
            return new EmptyResult();
        }



        // GET: Admin/QuanLyKyQuy
        public ActionResult QuanLyKyQuy()
        {
            try
            {
                var escrows = db.Escrows
                    .Include(e => e.DonHang)
                    .Include(e => e.NguoiBan)
                    .OrderByDescending(e => e.NgayTao)
                    .ToList();

                return View(escrows);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tải dữ liệu ký quỹ: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải dữ liệu. Vui lòng thử lại sau!";
                return RedirectToAction("Index", "Admin");
            }
        }

        // GET: Admin/ChiTietKyQuy/5
        public ActionResult ChiTietKyQuy(int id)
        {
            try
            {
                var escrow = db.Escrows
                    .Include(e => e.DonHang)
                    .Include(e => e.DonHang.NguoiDung)
                    .Include(e => e.NguoiBan)
                    .Include(e => e.DonHang.ChiTietDonHangs)
                    .Include(e => e.DonHang.ChiTietDonHangs.Select(c => c.SanPham))
                    .FirstOrDefault(e => e.MaKyQuy == id);

                if (escrow == null)
                {
                    return HttpNotFound();
                }

                return View(escrow);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi tải chi tiết ký quỹ: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải thông tin. Vui lòng thử lại sau!";
                return RedirectToAction("QuanLyKyQuy");
            }
        }

        // POST: Admin/GiaiNganKyQuy/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> GiaiNganKyQuy(int id)
        {
            try
            {
                var escrow = db.Escrows.Find(id);
                if (escrow == null)
                {
                    return HttpNotFound();
                }

                if (escrow.TrangThai != "Đang giữ")
                {
                    TempData["Error"] = "Tiền ký quỹ đã được giải ngân hoặc hoàn trả!";
                    return RedirectToAction("ChiTietKyQuy", new { id = id });
                }

                // Cập nhật trạng thái
                escrow.TrangThai = "Đã giải ngân";
                escrow.NgayGiaiNgan = DateTime.Now;

                // Cập nhật đơn hàng
                var donHang = db.DonHangs.Find(escrow.MaDonHang);
                if (donHang != null)
                {
                    donHang.DaGiaiNganChoSeller = true;
                    donHang.TrangThaiDonHang = "Đã hoàn thành";
                    db.Entry(donHang).State = EntityState.Modified;
                }

                db.Entry(escrow).State = EntityState.Modified;
                await db.SaveChangesAsync();

                TempData["Success"] = $"Đã giải ngân {escrow.TienChuyenChoNguoiBan:N0} VNĐ cho người bán thành công!";
                return RedirectToAction("QuanLyKyQuy");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi giải ngân: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi giải ngân. Vui lòng thử lại sau!";
                return RedirectToAction("ChiTietKyQuy", new { id = id });
            }
        }

        // POST: Admin/HoanTienKyQuy/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> HoanTienKyQuy(int id)
        {
            try
            {
                var escrow = db.Escrows.Find(id);
                if (escrow == null)
                {
                    return HttpNotFound();
                }

                if (escrow.TrangThai != "Đang giữ")
                {
                    TempData["Error"] = "Tiền ký quỹ đã được giải ngân hoặc hoàn trả!";
                    return RedirectToAction("ChiTietKyQuy", new { id = id });
                }

                // Cập nhật trạng thái
                escrow.TrangThai = "Đã hoàn tiền";
                escrow.NgayGiaiNgan = DateTime.Now;

                // Cập nhật đơn hàng
                var donHang = db.DonHangs.Find(escrow.MaDonHang);
                if (donHang != null)
                {
                    donHang.TrangThaiDonHang = "Đã hủy";
                    db.Entry(donHang).State = EntityState.Modified;
                }

                db.Entry(escrow).State = EntityState.Modified;
                await db.SaveChangesAsync();

                TempData["Success"] = $"Đã hoàn {escrow.TongTien:N0} VNĐ cho người mua thành công!";
                return RedirectToAction("QuanLyKyQuy");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi hoàn tiền: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi hoàn tiền. Vui lòng thử lại sau!";
                return RedirectToAction("ChiTietKyQuy", new { id = id });
            }
        }

        // GET: Admin/ThongKeDoanhThu
        public ActionResult ThongKeDoanhThu(DateTime? tuNgay, DateTime? denNgay)
        {
            try
            {
                // Xử lý ngày mặc định nếu không có ngày truyền vào
                if (!tuNgay.HasValue)
                {
                    tuNgay = DateTime.Now.AddMonths(-1);
                }

                if (!denNgay.HasValue)
                {
                    denNgay = DateTime.Now;
                }

                // Lấy dữ liệu từ bảng Escrow
                var escrows = db.Escrows
                    .Where(e => e.TrangThai == "Đã giải ngân" &&
                              e.NgayGiaiNgan >= tuNgay.Value &&
                              e.NgayGiaiNgan <= denNgay.Value.AddDays(1))
                    .ToList();

                // Tính toán thống kê
                var tongDoanhThu = escrows.Sum(e => e.TongTien);
                var tongPhiNenTang = escrows.Sum(e => e.PhiNenTang);
                var tongTienChuyenChoNguoiBan = escrows.Sum(e => e.TienChuyenChoNguoiBan);

                // Thống kê theo ngày
                var thongKeTheoNgay = escrows
                    .GroupBy(e => e.NgayGiaiNgan.Value.Date)
                    .Select(g => new ThongKeNgayViewModel
                    {
                        Ngay = g.Key,
                        DoanhThu = g.Sum(e => e.TongTien),
                        PhiNenTang = g.Sum(e => e.PhiNenTang),
                        TienChuyenChoNguoiBan = g.Sum(e => e.TienChuyenChoNguoiBan)
                    })
                    .OrderByDescending(t => t.Ngay)
                    .ToList();

                var viewModel = new ThongKeDoanhThuViewModel
                {
                    TuNgay = tuNgay.Value,
                    DenNgay = denNgay.Value,
                    TongDoanhThu = tongDoanhThu,
                    TongPhiNenTang = tongPhiNenTang,
                    TongTienChuyenChoNguoiBan = tongTienChuyenChoNguoiBan,
                    ThongKeTheoNgay = thongKeTheoNgay
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi thống kê doanh thu: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tạo báo cáo. Vui lòng thử lại sau!";
                return RedirectToAction("QuanLyKyQuy");
            }
        }

        // GET: Admin/ThongKeNguoiBan
        public ActionResult ThongKeNguoiBan(DateTime? tuNgay, DateTime? denNgay)
        {
            try
            {
                // Xử lý ngày mặc định
                if (!tuNgay.HasValue)
                {
                    tuNgay = DateTime.Now.AddMonths(-1);
                }

                if (!denNgay.HasValue)
                {
                    denNgay = DateTime.Now;
                }

                // Lấy dữ liệu từ bảng Escrow
                var escrows = db.Escrows
                    .Where(e => e.TrangThai == "Đã giải ngân" &&
                              e.NgayGiaiNgan >= tuNgay.Value &&
                              e.NgayGiaiNgan <= denNgay.Value.AddDays(1))
                    .Include(e => e.NguoiBan)
                    .ToList();

                // Thống kê theo người bán
                var thongKeTheoNguoiBan = escrows
                    .GroupBy(e => e.MaNguoiBan)
                    .Select(g => new ThongKeNguoiBanViewModel
                    {
                        MaNguoiBan = g.Key,
                        TenCuaHang = g.First().NguoiBan?.TenCuaHang ?? "Không xác định",
                        SoDonHang = g.Count(),
                        TongDoanhThu = g.Sum(e => e.TongTien),
                        TongPhiNenTang = g.Sum(e => e.PhiNenTang),
                        TongTienNhan = g.Sum(e => e.TienChuyenChoNguoiBan)
                    })
                    .OrderByDescending(t => t.TongDoanhThu)
                    .ToList();

                ViewBag.TuNgay = tuNgay.Value;
                ViewBag.DenNgay = denNgay.Value;

                return View(thongKeTheoNguoiBan);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi thống kê theo người bán: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tạo báo cáo. Vui lòng thử lại sau!";
                return RedirectToAction("QuanLyKyQuy");
            }
        }



        // GET: Admin/Dashboard
        public ActionResult Dashboard()
        {
            try
            {
                var viewModel = new DashboardViewModel();

                // Statistics for Users
                viewModel.TotalUsers = db.NguoiDungs.Count();
                viewModel.ActiveUsers = db.NguoiDungs.Count(u => u.TrangThai == "Active");
                viewModel.InactiveUsers = db.NguoiDungs.Count(u => u.TrangThai == "Inactive" || u.TrangThai == "Banned");
                viewModel.PendingUpgradeUsers = db.NguoiDungs.Count(u => u.XetDuyetThanhNguoiBan == true);
                viewModel.Buyers = db.NguoiDungs.Count(u => u.VaiTro == "Buyer");
                viewModel.Sellers = db.NguoiDungs.Count(u => u.VaiTro == "Seller");

                // Statistics for Products
                viewModel.TotalProducts = db.SanPhams.Count();
                viewModel.ApprovedProducts = db.SanPhams.Count(p => p.TrangThai == "Đã phê duyệt");
                viewModel.PendingProducts = db.SanPhams.Count(p => p.TrangThai == "Đang chờ xử lý");
                viewModel.RejectedProducts = db.SanPhams.Count(p => p.TrangThai == "Bị từ chối");

                // Statistics for Orders
                viewModel.TotalOrders = db.DonHangs.Count();
                viewModel.NewOrders = db.DonHangs.Count(o => o.TrangThaiDonHang == "Đang chờ xử lý");
                viewModel.DeliveredOrders = db.DonHangs.Count(o => o.TrangThaiDonHang == "Đã giao" || o.TrangThaiDonHang == "Đã hoàn thành");
                viewModel.CancelledOrders = db.DonHangs.Count(o => o.TrangThaiDonHang == "Đã hủy");

                // Statistics for Orders by Date (for chart)
                DateTime today = DateTime.Now.Date;
                DateTime startDate = today.AddDays(-6); // Last 7 days

                // Calculate the end date explicitly to avoid using AddDays in LINQ
                DateTime endDatePlusOne = today.AddDays(1);

                // Get all orders within the range
                var allOrdersInRange = db.DonHangs
                    .Where(o => o.NgayTao >= startDate && o.NgayTao <= endDatePlusOne)
                    .ToList();

                // Calculate chart data
                var ordersByDate = allOrdersInRange
                    .GroupBy(o => o.NgayTao.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(o => o.TongSoTien)
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                // Generate labels for the last 7 days
                var last7Days = Enumerable.Range(0, 7)
                    .Select(i => today.AddDays(-i))
                    .OrderBy(d => d)
                    .ToList();

                viewModel.OrderChartData = new List<ChartDataViewModel>();

                foreach (var day in last7Days)
                {
                    var dayData = ordersByDate.FirstOrDefault(o => o.Date.Date == day.Date);
                    viewModel.OrderChartData.Add(new ChartDataViewModel
                    {
                        Date = day.ToString("dd/MM"),
                        Count = dayData?.Count ?? 0,
                        Revenue = dayData?.Revenue ?? 0
                    });
                }

                // Doanh thu nên là tổng giá trị đơn hàng đã hoàn thành
                viewModel.TotalRevenue = db.DonHangs
                    .Where(o => o.TrangThaiDonHang == "Đã hoàn thành" || o.TrangThaiDonHang == "Đã giao")
                    .Sum(o => (decimal?)o.TongSoTien) ?? 0;

                // Trong phương thức Dashboard, thêm bộ lọc thời gian cho TotalCommission
                DateTime todays = DateTime.Now.Date;
                DateTime startDates = todays.AddDays(-6); // Last 7 days
                DateTime endDatePlusOnes = todays.AddDays(1);

                // Lấy tất cả các bản ghi Escrow đã giải ngân
                var allEscrows = db.Escrows
                    .Where(e => e.TrangThai == "Đã giải ngân")
                    .ToList();

                // Sau đó thực hiện lọc và tính tổng trong bộ nhớ
                viewModel.TotalCommission = allEscrows
                    .Where(e => e.NgayGiaiNgan >= startDates.Date && e.NgayGiaiNgan <= endDatePlusOnes.Date)
                    .Sum(e => e.PhiNenTang);

                // Recent Orders
                var recentOrders = db.DonHangs
                    .Include(o => o.NguoiDung)
                    .Include(o => o.NguoiBan)
                    .OrderByDescending(o => o.NgayTao)
                    .Take(10)
                    .ToList();

                viewModel.RecentOrders = recentOrders.Select(o => new OrderViewModel
                {
                    OrderId = o.MaDonHang,
                    CustomerName = o.NguoiDung?.TenNguoiDung ?? "Không xác định",
                    TotalAmount = o.TongSoTien,
                    OrderStatus = o.TrangThaiDonHang,
                    OrderDate = o.NgayTao
                }).ToList();

                // Pending Approvals
                var pendingProducts = db.SanPhams
                    .Include(p => p.NguoiBan)
                    .Where(p => p.TrangThai == "Đang chờ xử lý")
                    .OrderByDescending(p => p.NgayTao)
                    .Take(3)
                    .ToList();

                viewModel.PendingProductApprovals = pendingProducts.Select(p => new ProductViewModel
                {
                    ProductId = p.MaSanPham,
                    ProductName = p.TenSanPham,
                    SellerName = p.NguoiBan?.TenCuaHang ?? "Không xác định",
                    SellerId = p.MaNguoiBan
                }).ToList();

                var pendingUpgrades = db.NguoiDungs
                    .Where(u => u.XetDuyetThanhNguoiBan == true)
                    .OrderByDescending(u => u.NgayTao)
                    .Take(3)
                    .ToList();

                viewModel.PendingUpgrades = pendingUpgrades.Select(u => new UserViewModel
                {
                    UserId = u.MaNguoiDung,
                    UserName = u.TenNguoiDung,
                    Email = u.Email
                }).ToList();

                // Top Sellers
                var topSellersData = db.DonHangs
                    .Where(o => o.TrangThaiDonHang == "Đã hoàn thành" || o.TrangThaiDonHang == "Đã xác nhận nhận hàng")
                    .GroupBy(o => o.MaNguoiBan)
                    .Select(g => new
                    {
                        SellerId = g.Key,
                        OrderCount = g.Count(),
                        Revenue = g.Sum(o => o.TongSoTien)
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(5)
                    .ToList();

                viewModel.TopSellers = new List<SellerStatsViewModel>();

                foreach (var item in topSellersData)
                {
                    var seller = db.NguoiBans.FirstOrDefault(n => n.MaNguoiBan == item.SellerId);

                    viewModel.TopSellers.Add(new SellerStatsViewModel
                    {
                        SellerId = item.SellerId,
                        SellerName = seller?.TenCuaHang ?? "Không xác định",
                        OrderCount = item.OrderCount,
                        Revenue = item.Revenue
                    });
                }

                // Recent Transactions
                var recentTransactions = db.GiaoDichs
                    .OrderByDescending(g => g.NgayGiaoDich)
                    .Take(5)
                    .ToList();

                viewModel.RecentTransactions = recentTransactions.Select(t => new TransactionViewModel
                {
                    TransactionId = t.MaGiaoDich,
                    TransactionStatus = t.TrangThaiGiaoDich,
                    Amount = t.TongTien
                }).ToList();

                // Lấy hoạt động gần đây
                viewModel.RecentActivities = new List<ActivityViewModel>();

                // 1. Thêm người dùng mới
                var recentUsers = db.NguoiDungs
                    .OrderByDescending(u => u.NgayTao)
                    .Take(3)
                    .ToList();

                foreach (var user in recentUsers)
                {
                    viewModel.RecentActivities.Add(new ActivityViewModel
                    {
                        Id = user.MaNguoiDung,
                        ActivityType = "NewUser",
                        Message = $"Người dùng mới đăng ký: {user.TenNguoiDung}",
                        ActivityTime = user.NgayTao,
                        IconClass = "fas fa-user", // Dùng Font Awesome thay vì bi-person
                        ColorClass = "bg-primary"
                    });
                }

                // 2. Thêm đơn hàng mới
                var recentOrder = db.DonHangs
                    .OrderByDescending(o => o.NgayTao)
                    .Take(3)
                    .ToList();

                foreach (var order in recentOrders)
                {
                    var customerName = db.NguoiDungs
                        .Where(u => u.MaNguoiDung == order.MaNguoiDung)
                        .Select(u => u.TenNguoiDung)
                        .FirstOrDefault() ?? "Không xác định";

                    viewModel.RecentActivities.Add(new ActivityViewModel
                    {
                        Id = order.MaDonHang,
                        ActivityType = "NewOrder",
                        Message = $"Đơn hàng mới #{order.MaDonHang} từ {customerName}",
                        ActivityTime = order.NgayTao,
                        IconClass = "fas fa-shopping-cart", // Dùng Font Awesome thay vì bi-cart
                        ColorClass = "bg-success"
                    });
                }

                // 3. Thêm sản phẩm mới
                var recentProducts = db.SanPhams
                    .OrderByDescending(p => p.NgayTao)
                    .Take(3)
                    .ToList();

                foreach (var product in recentProducts)
                {
                    var sellerName = db.NguoiBans
                        .Where(s => s.MaNguoiBan == product.MaNguoiBan)
                        .Select(s => s.TenCuaHang)
                        .FirstOrDefault() ?? "Không xác định";

                    viewModel.RecentActivities.Add(new ActivityViewModel
                    {
                        Id = product.MaSanPham,
                        ActivityType = "NewProduct",
                        Message = $"Sản phẩm mới: {product.TenSanPham} từ {sellerName}",
                        ActivityTime = product.NgayTao,
                        IconClass = "fas fa-box", // Dùng Font Awesome thay vì bi-box
                        ColorClass = "bg-warning"
                    });
                }

                // Sắp xếp tất cả hoạt động theo thời gian giảm dần và lấy 5 hoạt động gần nhất
                viewModel.RecentActivities = viewModel.RecentActivities
                    .OrderByDescending(a => a.ActivityTime)
                    .Take(3)
                    .ToList();

                return View(viewModel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi dashboard: " + ex.Message);
                TempData["Error"] = "Đã xảy ra lỗi khi tải dữ liệu dashboard. Vui lòng thử lại sau!";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public JsonResult GetDashboardData(string timeRange)
        {
            try
            {
                DateTime endDate = DateTime.Now.Date;
                DateTime startDate;

                switch (timeRange)
                {
                    case "today":
                        startDate = endDate;
                        break;
                    case "week":
                        startDate = endDate.AddDays(-6);
                        break;
                    case "month":
                        startDate = endDate.AddDays(-29);
                        break;
                    case "all":
                        // Lấy ngày đầu tiên từ cơ sở dữ liệu hoặc một ngày cố định trong quá khứ
                        // Ví dụ: lấy ngày tạo đơn hàng đầu tiên hoặc mặc định 1 năm
                        var firstOrderDate = db.DonHangs.OrderBy(o => o.NgayTao).Select(o => o.NgayTao).FirstOrDefault();

                        // Nếu không có đơn hàng nào, mặc định lấy 1 năm
                        startDate = firstOrderDate != default(DateTime) ? firstOrderDate.Date : endDate.AddYears(-1);
                        break;
                    default:
                        startDate = endDate.AddDays(-6);
                        break;
                }

                // Tính toán endDatePlusOne một lần
                DateTime endDatePlusOne = endDate.AddDays(1);

                // Statistics for Users (không thay đổi theo khoảng thời gian)
                int totalUsers = db.NguoiDungs.Count();
                int activeUsers = db.NguoiDungs.Count(u => u.TrangThai == "Active");
                int inactiveUsers = db.NguoiDungs.Count(u => u.TrangThai == "Inactive" || u.TrangThai == "Banned");
                int pendingUpgradeUsers = db.NguoiDungs.Count(u => u.XetDuyetThanhNguoiBan == true);
                int buyers = db.NguoiDungs.Count(u => u.VaiTro == "Buyer");
                int sellers = db.NguoiDungs.Count(u => u.VaiTro == "Seller");

                // Statistics for Products (không thay đổi theo khoảng thời gian)
                int totalProducts = db.SanPhams.Count();
                int approvedProducts = db.SanPhams.Count(p => p.TrangThai == "Đã phê duyệt");
                int pendingProducts = db.SanPhams.Count(p => p.TrangThai == "Đang chờ xử lý");
                int rejectedProducts = db.SanPhams.Count(p => p.TrangThai == "Bị từ chối");

                // Lấy dữ liệu từ startDate đến endDate
                var allOrdersInRange = db.DonHangs
                    .Where(o => o.NgayTao >= startDate && o.NgayTao <= endDatePlusOne)
                    .ToList();

                // Tính toán dữ liệu theo ngày
                var ordersByDate = allOrdersInRange
                    .GroupBy(o => o.NgayTao.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count(),
                        Revenue = g.Sum(o => o.TongSoTien)
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                // Điều chỉnh tạo danh sách các ngày trong khoảng thời gian
                var dateRange = new List<DateTime>();

                // Với trường hợp "all", có thể có nhiều ngày nên chúng ta cần phân nhóm theo tháng/tuần
                if (timeRange == "all")
                {
                    // Nếu khoảng thời gian dài hơn 90 ngày, nhóm theo tháng
                    if ((endDate - startDate).TotalDays > 90)
                    {
                        var currentDate = new DateTime(startDate.Year, startDate.Month, 1);
                        while (currentDate <= endDate)
                        {
                            dateRange.Add(currentDate);
                            currentDate = currentDate.AddMonths(1);
                        }

                        // Nhóm dữ liệu theo tháng
                        var ordersByMonth = allOrdersInRange
                            .GroupBy(o => new { o.NgayTao.Year, o.NgayTao.Month })
                            .Select(g => new
                            {
                                Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                                Count = g.Count(),
                                Revenue = g.Sum(o => o.TongSoTien)
                            })
                            .OrderBy(x => x.Date)
                            .ToList();

                        ordersByDate = ordersByMonth;
                    }
                    else
                    {
                        // Thêm từng ngày vào khoảng thời gian
                        for (var date = startDate; date <= endDate; date = date.AddDays(1))
                        {
                            dateRange.Add(date);
                        }
                    }
                }
                else
                {
                    // Với các trường hợp khác, sử dụng phương pháp hiện tại
                    for (var date = startDate; date <= endDate; date = date.AddDays(1))
                    {
                        dateRange.Add(date);
                    }
                }

                // Chuẩn bị dữ liệu cho biểu đồ
                var chartLabels = new List<string>();
                var orderData = new List<int>();
                var revenueData = new List<decimal>();

                foreach (var date in dateRange)
                {
                    var dateFormatString = timeRange == "all" && (endDate - startDate).TotalDays > 90
                        ? "MM/yyyy" // Định dạng tháng/năm cho khoảng thời gian dài
                        : "dd/MM"; // Định dạng ngày/tháng cho khoảng thời gian ngắn

                    var dayData = ordersByDate.FirstOrDefault(o =>
                        timeRange == "all" && (endDate - startDate).TotalDays > 90
                            ? o.Date.Month == date.Month && o.Date.Year == date.Year
                            : o.Date.Date == date.Date);

                    chartLabels.Add(date.ToString(dateFormatString));
                    orderData.Add(dayData?.Count ?? 0);
                    revenueData.Add(dayData?.Revenue ?? 0);
                }

                // Statistics for Orders trong khoảng thời gian đã chọn
                int totalOrders = allOrdersInRange.Count;
                int newOrders = allOrdersInRange.Count(o => o.TrangThaiDonHang == "Đang chờ xử lý");
                int deliveredOrders = allOrdersInRange.Count(o => o.TrangThaiDonHang == "Đã giao" || o.TrangThaiDonHang == "Đã hoàn thành");
                int cancelledOrders = allOrdersInRange.Count(o => o.TrangThaiDonHang == "Đã hủy");

                // Tính doanh thu chỉ từ đơn hàng đã hoàn thành/đã giao
                decimal totalRevenue = allOrdersInRange
                    .Where(o => o.TrangThaiDonHang == "Đã hoàn thành" || o.TrangThaiDonHang == "Đã giao")
                    .Sum(o => o.TongSoTien);

                // Lấy tất cả các bản ghi Escrow đã giải ngân
                var allEscrows = db.Escrows
                    .Where(e => e.TrangThai == "Đã giải ngân")
                    .ToList();

                // Sau đó thực hiện lọc và tính tổng trong bộ nhớ
                decimal totalCommission = allEscrows
                    .Where(e => e.NgayGiaiNgan >= startDate.Date && e.NgayGiaiNgan <= endDatePlusOne.Date)
                    .Sum(e => e.PhiNenTang);

                return Json(new
                {
                    // Thống kê người dùng
                    totalUsers,
                    activeUsers,
                    inactiveUsers,
                    pendingUpgradeUsers,
                    buyers,
                    sellers,

                    // Thống kê sản phẩm
                    totalProducts,
                    approvedProducts,
                    pendingProducts,
                    rejectedProducts,

                    // Thống kê đơn hàng
                    totalOrders,
                    newOrders,
                    deliveredOrders,
                    cancelledOrders,

                    // Dữ liệu biểu đồ
                    chartLabels,
                    orderData,
                    revenueData,

                    // Thống kê doanh thu
                    totalRevenue,
                    totalCommission,

                    // Thông tin về khoảng thời gian
                    startDate = startDate.ToString("dd/MM/yyyy"),
                    endDate = endDate.ToString("dd/MM/yyyy"),
                    timeRange
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Lỗi GetDashboardData: " + ex.Message);
                return Json(new { error = true, message = ex.Message }, JsonRequestBehavior.AllowGet);
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



        #region ViewModels
        // Định nghĩa các ViewModel như các lớp lồng nhau trong controller
        public class ActivityViewModel
        {
            public int Id { get; set; }
            public string ActivityType { get; set; } // Loại hoạt động: NewUser, NewOrder, NewProduct, etc.
            public string Message { get; set; } // Mô tả hoạt động
            public DateTime ActivityTime { get; set; } // Thời gian hoạt động
            public string IconClass { get; set; } // Class CSS cho biểu tượng
            public string ColorClass { get; set; } // Class CSS cho màu sắc
        }
        public class DashboardViewModel
        {
            // Thống kê người dùng
            public int TotalUsers { get; set; }
            public int ActiveUsers { get; set; }
            public int InactiveUsers { get; set; }
            public int PendingUpgradeUsers { get; set; }
            public int Buyers { get; set; }
            public int Sellers { get; set; }

            // Thống kê sản phẩm
            public int TotalProducts { get; set; }
            public int ApprovedProducts { get; set; }
            public int PendingProducts { get; set; }
            public int RejectedProducts { get; set; }

            // Thống kê đơn hàng
            public int TotalOrders { get; set; }
            public int NewOrders { get; set; }
            public int DeliveredOrders { get; set; }
            public int CancelledOrders { get; set; }

            // Dữ liệu biểu đồ
            public List<ChartDataViewModel> OrderChartData { get; set; }

            // Thống kê doanh thu
            public decimal TotalRevenue { get; set; }
            public decimal TotalCommission { get; set; }

            // Danh sách các mục
            public List<OrderViewModel> RecentOrders { get; set; }
            public List<ProductViewModel> PendingProductApprovals { get; set; }
            public List<UserViewModel> PendingUpgrades { get; set; }
            public List<SellerStatsViewModel> TopSellers { get; set; }
            public List<TransactionViewModel> RecentTransactions { get; set; }
            public List<ActivityViewModel> RecentActivities { get; set; }
        }

        public class ChartDataViewModel
        {
            public string Date { get; set; }
            public int Count { get; set; }
            public decimal Revenue { get; set; }
        }

        public class OrderViewModel
        {
            public int OrderId { get; set; }
            public string CustomerName { get; set; }
            public decimal TotalAmount { get; set; }
            public string OrderStatus { get; set; }
            public DateTime OrderDate { get; set; }
        }

        public class ProductViewModel
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; }
            public string SellerName { get; set; }
            public int SellerId { get; set; }
        }

        public class UserViewModel
        {
            public int UserId { get; set; }
            public string UserName { get; set; }
            public string Email { get; set; }
        }

        public class SellerStatsViewModel
        {
            public int SellerId { get; set; }
            public string SellerName { get; set; }
            public int OrderCount { get; set; }
            public decimal Revenue { get; set; }
        }

        public class TransactionViewModel
        {
            public int TransactionId { get; set; }
            public string TransactionStatus { get; set; }
            public decimal Amount { get; set; }
        }
        #endregion

    }
}