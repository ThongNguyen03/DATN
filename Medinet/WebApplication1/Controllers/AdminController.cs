using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
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