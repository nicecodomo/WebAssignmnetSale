using Microsoft.AspNetCore.Mvc;
using System.Text;
using MySql.Data.MySqlClient;
using Microsoft.AspNetCore.Authorization;
using WebAssignmentSale.Models;
using Microsoft.AspNetCore.Session;
using Microsoft.AspNetCore.Http;


namespace WebAssignmentSale.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<LoginController> _logger;
        private readonly IConfiguration _configuration;
        //private readonly ISession _session;
        private readonly IHttpContextAccessor _contextAccessor;

        public LoginController(ILogger<LoginController> logger, IConfiguration configuration
            , IHttpContextAccessor contextAccessor)
        {
            _logger = logger;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            //_session = httpContextAccessor.HttpContext.Session;
        }

        [HttpGet]
        public IActionResult Login()
        {
            var model = new Login();

            // ตรวจสอบคุกกี้
            if (Request.Cookies.TryGetValue("RememberMe", out string rememberMeValue))
            {
                var values = rememberMeValue.Split('|');
                if (values.Length == 2)
                {
                    ViewBag.RememberMeUsername = values[0];
                    ViewBag.RememberMePassword = values[1];
                }
            }

            //alert error session
            if (TempData.ContainsKey("SessionError"))
            {
                ViewData["SessionError"] = TempData["SessionError"].ToString();
            }

            return View(model);
        }


        [HttpPost]
        public IActionResult Login(Login login)
        {
            try
            {
                using (MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("connectionStr")))
                {
                    
                    connection.Open();

                    string sqlquery = "SELECT  a.*,l.*,p.*,e.*,po.* FROM tb_access AS a " +
                        "LEFT JOIN tb_login AS l ON a.log_id = l.log_id  " +
                        "LEFT JOIN tb_program AS p ON a.pg_id = p.pg_id  " +
                        "LEFT JOIN tb_employee AS e ON e.emp_id = l.emp_id  " +
                        "LEFT JOIN tb_position AS po ON po.pos_id = e.pos_id  " +
                        "WHERE l.Username = @Username AND l.Password = @Password AND a.pg_id = '3' AND a.acc_status = '1' ";


                    var cmd = new MySqlCommand(sqlquery, connection);

                    cmd.Parameters.AddWithValue("@Username", login.Username);
                    cmd.Parameters.AddWithValue("@Password", login.Password);
                    cmd.Parameters.AddWithValue("@EmpId", login.EmpId);
                    cmd.Parameters.AddWithValue("@PosPermissions", login.PosPermissions);

                    var reader = cmd.ExecuteReader();

                    if (reader.Read())
                    {

                        if (login.RememberMe)
                        {
                            Response.Cookies.Append("RememberMe", $"{login.Username}|{login.Password}", new 
                                CookieOptions
                            {
                                Expires = DateTime.UtcNow.AddDays(1),
                                HttpOnly = true,
                                Secure = true,
                                SameSite = SameSiteMode.None
                            });
                        }

                        //นำข้อมูลใส่ใน cookie เพื่อนำไปใช้ที่อื่น
                        //HttpContext.Response.Cookies.Append("LoggedInEmpId", Convert.ToInt32(reader["emp_id"]).ToString());
                        //HttpContext.Response.Cookies.Append("LoggedInUsername", login.Username);
                        //HttpContext.Response.Cookies.Append("LoggedInPosPermissions", reader["pos_permissions_manage"].ToString());


                        // สร้าง Session แบบแบ่งประเภทข้อมูล โดยเก็บข้อมูลที่จะนำไปใช้ใน HomeController
                        //var session = _contextAccessor.HttpContext.Session;
                        //session.SetInt32("EmpId", Convert.ToInt32(reader["emp_id"]));
                        //session.SetInt32("LogId", Convert.ToInt32(reader["log_id"]));
                        //session.SetString("UserName", reader["username"].ToString());
                        //session.SetString("PosStatus", Convert.ToInt32(reader["pos_permissions_manage"]).ToString());



                        //นำค่าจากฐานข้อมูล ใส่ใน KEY Example แบบประเภท string ทั้งหมด

                        var session = _contextAccessor.HttpContext.Session;
                        session.SetString("EmpId", reader["emp_id"].ToString());
                        session.SetString("LogId", reader["log_id"].ToString());
                        session.SetString("UserName", reader["username"].ToString());
                        session.SetString("PosStatus", reader["pos_permissions_manage"].ToString());

                        //นำค่าจาก Model ใส่ใน KEY
                        //session.SetInt32("EmpId", login.EmpId);
                        //session.SetInt32("LogId", login.LogId);
                        //session.SetString("UserName", login.Username);
                        //session.SetString("PosPermissions", login.PosPermissions);

                        return RedirectToAction("SummarySale", "Home");
                    }
                    else
                    {
                        ViewData["LoginError"] = "<script>Swal.fire('Invalid Login Attempt', 'Please check your username and password', 'error')</script>";
                        ViewBag.AlertError = "ชื่อผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง";
                        //TempData["LoginError"] = "ชื่อผู้ใช้งานหรือรหัสผ่านไม่ถูกต้อง";
                        //ModelState.AddModelError("", "ไม่พบชื่อผู้ใช้งานหรือรหัสผ่าน");

                        return View(login);

                    }
                }
            }
            catch (Exception ex)
            {
                ViewData["LoginError"] = "<script>Swal.fire('An error occurred.', 'An error occurred logging in.', 'error')</script>";
                ViewBag.AlertError = "Error : " + ex.Message;
                //ModelState.AddModelError(string.Empty, "An error occurred logging in.");
                return View();
            }
        }

        // Method สำหรับ logout
        public IActionResult Logout()
        {
            try
            {
                // ลบ session ทั้งหมด
                _contextAccessor.HttpContext.Session.Clear();

                // Redirect กลับไปยังหน้า login
                return RedirectToAction("Login", "Login");
            }
            catch (Exception ex)
            {
                // กรณีเกิดข้อผิดพลาด
                ViewBag.Error = "Error : " + ex.Message;
                return View("Error"); // สามารถเลือกที่จะส่งไปยังหน้า error หรือหน้าอื่นที่เหมาะสม
            }
        }


    }
}
