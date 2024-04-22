using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Web;

namespace WebAssignmentSale.Models
{
    public class RecycleController : Controller
    {
        private readonly ILogger<RecycleController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor; 

        public RecycleController(ILogger<RecycleController> logger, IConfiguration configuration, IHttpContextAccessor contextAccessor)
        {
            _logger = logger;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
        }

        public IActionResult Recycle(string searchQuery, int page = 1)
        {
            //if (HttpContext.Request.Cookies.TryGetValue("LoggedInEmpId", out string loggedInEmpId))
            var session = _contextAccessor.HttpContext.Session;

            if (session.GetString("EmpId") != null)
            {
                try
                {
                    var LoggedInEmpId = session.GetString("EmpId");
                    //int LoggedInEmpId = Convert.ToInt32(loggedInEmpId);
                    int totalItems = GetTotalItemCountRecycle(LoggedInEmpId, searchQuery);
                    int itemsPerPage = 10;
                    int pageCount = (int)Math.Ceiling((double)totalItems / itemsPerPage);

                    ViewBag.SearchQuery = HttpUtility.HtmlDecode(searchQuery);

                    if (page > pageCount)
                    {
                        page = pageCount;
                    }

                    var items = GetAssignRecycleFromDB(LoggedInEmpId, page, itemsPerPage, searchQuery);

                    var viewModel = new PageModel
                    {
                        Items = items,
                        PageCount = pageCount,
                        CurrentPage = page,
                        ItemsPerPage = itemsPerPage,
                        TotalItems = totalItems
                    };

                    return View(viewModel);

                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "เกิดข้อผิดพลาดในการดึงข้อมูล: " + ex.Message);
                    return View(); // หรือ return RedirectToAction("ActionName");
                }
            }
            else
            {
                ModelState.AddModelError("", "ไม่พบค่า LoggedInEmpId ใน Cookie");
                return RedirectToAction("SummarySale", "Home");
            }
        }

        public IActionResult EditRecycleAssign(string searchQuery, int Id, int page = 1)
        {

            var restore = GetAssignById(Id);
            var province = GetProvinceInDB();
            var amphure = GetAmphureById(Id);
            var district = GetDistrictById(Id);
            AddressIdModel address = new AddressIdModel();
            address.AssignmentSales = restore;
            address.Provinces = province;
            address.Amphures = amphure;
            address.Districts = district;

            address.CurrentPage = page;
            ViewBag.SearchQuery = HttpUtility.UrlEncode(searchQuery);

            return View(address);
        }


        public int GetTotalItemCountRecycle(string LoggedInEmpId, string searchQuery)
        {

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            {
                connection.Open();

                // สร้างคำสั่ง SQL เพื่อนับจำนวนรายการทั้งหมด
                string sql = "SELECT COUNT(*) FROM tb_assignment_sale AS a " +
                    "LEFT JOIN tb_employee AS e ON a.emp_id = e.emp_id " +
                    "LEFT JOIN provinces AS p ON a.pro_id = p.id " +
                    "LEFT JOIN amphures AS am ON a.amp_id = am.id " +
                    "LEFT JOIN districts AS d ON a.dis_id = d.id " +
                    $"WHERE a.emp_id = @LoggedInEmpId AND a.ass_status = '0' " +
                    $"AND (a.agency LIKE @SearchQuery " +
                    $"OR a.customer LIKE @SearchQuery OR p.name_th_pro LIKE @SearchQuery " +
                    $"OR am.name_th_amp LIKE @SearchQuery OR d.name_th_dis LIKE @SearchQuery) ";

                // สร้างและประมวลผลคำสั่ง SQL
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");
                command.Parameters.AddWithValue("@LoggedInEmpId", LoggedInEmpId);

                int totalItemCount = (int)(long)command.ExecuteScalar();

                // คืนค่าจำนวนรายการทั้งหมด
                return totalItemCount;
            }
        }



        public List<Province> GetProvinceInDB()
        {
            List<Province> provinces = new List<Province>();

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = "SELECT * FROM provinces";
                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Province pro = new Province
                    {
                        ProId = Convert.ToInt32(reader["id"]),
                        PName = reader["name_th_pro"].ToString()
                    };

                    // และสามารถดึงข้อมูล Position จากตาราง tb_position ได้ตามต้องการ


                    provinces.Add(pro);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching employee data from database.");
            }
            finally
            {
                connection.Close();
            }

            return provinces;
        }

        public List<Amphure> GetAmphureById(int id)
        {
            List<Amphure> amphures = new List<Amphure>();

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = $"SELECT a.*,am.* FROM tb_assignment_sale AS a JOIN Amphures AS am ON a.pro_id = am.province_id WHERE a.ass_sale_id = {id}";
                MySqlCommand command = new MySqlCommand(sqlSelect, connection);

                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Amphure amphure = new Amphure
                    {
                        AmpId = Convert.ToInt32(reader["id"]),
                        AName = reader["name_th_amp"].ToString(),
                        ProId = Convert.ToInt32(reader["province_id"])
                    };

                    amphures.Add(amphure);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching amphure data from database.");
            }
            finally
            {
                connection.Close();
            }

            // Return the Partial View with the amphures data
            return amphures;
        }

        public List<District> GetDistrictById(int id)
        {
            List<District> districts = new List<District>();

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = $"SELECT a.*,d.* FROM tb_assignment_sale AS a JOIN districts AS d ON a.amp_id = d.amphure_id WHERE a.ass_sale_id = {id}";
                MySqlCommand command = new MySqlCommand(sqlSelect, connection);

                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    District district = new District
                    {
                        DisId = Convert.ToInt32(reader["id"]),
                        DName = reader["name_th_dis"].ToString(),
                        AmpId = Convert.ToInt32(reader["amp_id"])
                    };

                    districts.Add(district);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching amphure data from database.");
            }
            finally
            {
                connection.Close();
            }

            // Return the Partial View with the amphures data
            return districts;
        }

        private IEnumerable<AssignmentSale> GetAssignRecycleFromDB(string LoggedInEmpId, int page, int itemsPerPage, string searchQuery)
        {
            List<AssignmentSale> assignment = new List<AssignmentSale>();

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                int offset = (page - 1) * itemsPerPage;                // คำนวณ offset สำหรับการดึงข้อมูลหน้าปัจจุบัน

                string sqlSelect = "SELECT a.*, p.*, am.*, d.* FROM tb_assignment_sale AS a " +
                   "LEFT JOIN provinces AS p ON a.pro_id = p.id " +
                   "LEFT JOIN amphures AS am ON a.amp_id = am.id " +
                   "LEFT JOIN districts AS d ON a.dis_id = d.id " +
                   "WHERE a.emp_id = @LoggedInEmpId AND a.ass_status = '0' " +
                   "AND (a.agency LIKE @SearchQuery OR a.customer LIKE @SearchQuery " +
                   "OR a.Department LIKE @SearchQuery OR p.name_th_pro LIKE @SearchQuery " +
                   "OR am.name_th_amp LIKE @SearchQuery OR d.name_th_dis LIKE @SearchQuery) " +
                   " ORDER BY a.last_update_date DESC " +
                   "LIMIT @ItemsPerPage OFFSET @Offset ";

                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                command.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");
                command.Parameters.AddWithValue("@LoggedInEmpId", LoggedInEmpId);
                command.Parameters.AddWithValue("@ItemsPerPage", itemsPerPage);
                command.Parameters.AddWithValue("@Offset", offset);

                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    AssignmentSale ass = new AssignmentSale
                    {
                        AssignSaleId = Convert.ToInt32(reader["ass_sale_id"]),
                        Agency = reader["agency"].ToString(),
                        ProId = Convert.ToInt32(reader["pro_id"]),
                        AmpId = Convert.ToInt32(reader["amp_id"]),
                        DisId = Convert.ToInt32(reader["dis_id"]),
                        Customer = reader["customer"].ToString(),
                        Department = reader["department"].ToString(),
                        PhoneNumber = reader["phone_number"].ToString(),
                        LineId = reader["lineId"].ToString(),
                        Annotation = reader["annotation"].ToString(),
                        CreateDate = (DateTime)reader["create_date"],

                        PName = reader["name_th_pro"].ToString(),
                        AName = reader["name_th_amp"].ToString(),
                        DName = reader["name_th_dis"].ToString()

                    };



                    assignment.Add(ass);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching employee data from database.");
            }
            finally
            {
                connection.Close();
            }

            return assignment;
        }

        private AssignmentSale GetAssignById(int AssignSaleId)
        {
            AssignmentSale assign = null;

            string connectionString = _configuration.GetConnectionString("connectionStr");
            using MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = $"SELECT * FROM tb_assignment_sale WHERE ass_sale_id = {AssignSaleId}";
                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                using MySqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    assign = new AssignmentSale
                    {
                        AssignSaleId = Convert.ToInt32(reader["ass_sale_id"]),
                        Agency = reader["agency"].ToString(),
                        ProId = Convert.ToInt32(reader["pro_id"]),
                        AmpId = Convert.ToInt32(reader["amp_id"]),
                        DisId = Convert.ToInt32(reader["dis_id"]),
                        Customer = reader["customer"].ToString(),
                        Department = reader["department"].ToString(),
                        PhoneNumber = reader["phone_number"].ToString(),
                        LineId = reader["lineId"].ToString(),
                        Annotation = reader["annotation"].ToString(),


                        CreateDate = (DateTime)reader["create_date"]

                    };

                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching employee data from database.");
            }
            finally
            {
                connection.Close();
            }

            return assign;
        }

        [HttpPost]
        public IActionResult UpdateRecycleAssign(AssignmentSale updatedAssign)
        {
            string connectionString = _configuration.GetConnectionString("connectionStr");
            using MySqlConnection connection = new MySqlConnection(connectionString);

            if (string.IsNullOrWhiteSpace(updatedAssign.AssStatus))
            {
                updatedAssign.AssStatus = "0";
            }

            string sqlUpdate = "UPDATE tb_assignment_sale SET " +
                               "ass_status = @ass_status " +
                               "WHERE ass_sale_id = @ass_sale_id";

            using MySqlCommand command = new MySqlCommand(sqlUpdate, connection);
            command.Parameters.AddWithValue("@ass_status", updatedAssign.AssStatus);
            command.Parameters.AddWithValue("@ass_sale_id", updatedAssign.AssignSaleId);

            try
            {
                connection.Open();
                command.ExecuteNonQuery();
                TempData["SuccessMessage"] = "Assignment updated successfully!";
                return RedirectToAction("Recycle", "Recycle");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating assignment: " + ex.Message;
                return RedirectToAction("Recycle", "Recycle");
            }
            finally
            {
                connection.Close();
            }
        }


    }
}
