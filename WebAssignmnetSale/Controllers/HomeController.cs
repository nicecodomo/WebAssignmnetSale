using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using System.Diagnostics;
using System.Web;
using WebAssignmentSale.Models;

namespace WebAssignmentSale.Controllers
{
    [CheckSessionFilter]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _contextAccessor;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, IHttpContextAccessor contextAccessor)
        {
            _logger = logger;
            _configuration = configuration;
            _contextAccessor = contextAccessor;
        }

        //Start -> View
        public IActionResult SummarySale(string searchQuery, string searchDateQuery, int page = 1)
        {
            //check session
            var session = _contextAccessor.HttpContext.Session;


            var LoggedInEmpId = session.GetString("EmpId");
            var LoggedInLogId = session.GetString("LogId");
            var LoggedInUserName = session.GetString("UserName");
            var LoggedInPosStatus = session.GetString("PosStatus");

            try
            {
                ViewBag.SearchQuery = HttpUtility.HtmlDecode(searchQuery);
                ViewBag.SearchDateQuery = HttpUtility.HtmlDecode(searchDateQuery);

                int totalItems = GetTotalItemCountSummary(LoggedInEmpId, searchQuery, searchDateQuery);
                int itemsPerPage = 10; // ¶éÒäÁèä´éÃÑº¤èÒ pageSize ÁÒãËéãªé¤èÒàÃÔèÁµé¹à»ç¹ 10
                int pageCount = (int)Math.Ceiling((double)totalItems / itemsPerPage);

                if (page > pageCount)
                {
                    page = pageCount;
                }

                var items = GetAssignFromDB(LoggedInEmpId, page, itemsPerPage, searchQuery, searchDateQuery);

                var viewModel = new Models.PageModel
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
                ViewBag.Error = "An error occurred : " + ex.Message;
                return View();
            }

        }

        public IActionResult Report(string searchQuery, string searchDateQuery, int page = 1)
        {

            try
            {
                ViewBag.SearchQuery = HttpUtility.HtmlDecode(searchQuery);
                ViewBag.SearchDateQuery = HttpUtility.HtmlDecode(searchDateQuery);

                int totalItems = GetTotalItemCountReport(searchQuery, searchDateQuery);
                int itemsPerPage = 10;
                int pageCount = (int)Math.Ceiling((double)totalItems / itemsPerPage);

                if (page > pageCount)
                {
                    page = pageCount;
                }


                var items = GetReportFromDB(searchQuery, page, itemsPerPage, searchDateQuery);

                var viewModel = new Models.PageModel
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
                ModelState.AddModelError("", "An error occurred : " + ex.Message);
                ViewBag.Error = "An error occurred : " + ex.Message;
                return View();
            }

        }

        public IActionResult InsertAssignment()
        {

            //start method
            var province = GetProvinceInDB();
            var amphure = GetAmphureInDB();
            var district = GetDistrictInDB();
            var address = new AddressModel();
            address.Provinces = province;
            address.Amphures = amphure;
            address.Districts = district;
            return View(address);

        }

        public IActionResult EditAssign(int AssignSaleId, string searchQuery, string searchDateQuery, int page = 1)
        {

            //start method
            var sales = GetAssignById(AssignSaleId);
            var province = GetProvinceInDB();
            var amphure = GetAmphureById(AssignSaleId);
            var district = GetDistrictById(AssignSaleId);
            AddressIdModel address = new AddressIdModel();
            address.AssignmentSales = sales;
            address.Provinces = province;
            address.Amphures = amphure;
            address.Districts = district;
            address.CurrentPage = page;

            ViewBag.SearchQuery = HttpUtility.UrlEncode(searchQuery);
            ViewBag.SearchDateQuery = HttpUtility.UrlEncode(searchDateQuery);

            return View(address);

        }

        public IActionResult EditReportAssign(int AssignSaleId, string searchQuery, string searchDateQuery, int page = 1)
        {

            //start method
            var sales = GetAssignById(AssignSaleId);
            var province = GetProvinceInDB();
            var amphure = GetAmphureById(AssignSaleId);
            var district = GetDistrictById(AssignSaleId);
            AddressIdModel address = new AddressIdModel();
            address.AssignmentSales = sales;
            address.Provinces = province;
            address.Amphures = amphure;
            address.Districts = district;
            address.CurrentPage = page;

            ViewBag.SearchQuery = HttpUtility.UrlEncode(searchQuery);
            ViewBag.SearchDateQuery = HttpUtility.UrlEncode(searchDateQuery);

            return View(address);

        }

        public IActionResult EditEmployee(int EmpId)
        {

            //start method
            var employee = GetEmployeeById(EmpId);
            var pos = GetPosFromDB();
            PositionViewModel nice = new PositionViewModel();
            nice.Positions = pos;
            nice.Employee = employee;


            if (employee == null)
            {
                return RedirectToAction("Summary", "Home");
            }
            return View(nice);

        }

        public IActionResult Setting(int EmpId)
        {

            // start method
            var salesmanager = GetSalesById(EmpId);
            return View(salesmanager);

        }
        //End -> View


        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]









        //Start -> Get total count
        public int GetTotalItemCountSummary(string LoggedInEmpId, string searchQuery, string searchDateQuery)
        {

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            {
                connection.Open();

                // ÊÃéÒ§¤ÓÊÑè§ SQL à¾×èÍ¹Ñº¨Ó¹Ç¹ÃÒÂ¡ÒÃ·Ñé§ËÁ´
                string sql = "SELECT COUNT(*) FROM tb_assignment_sale AS a " +
                    "LEFT JOIN tb_employee AS e ON a.emp_id = e.emp_id " +
                    "LEFT JOIN provinces AS p ON a.pro_id = p.id " +
                    "LEFT JOIN amphures AS am ON a.amp_id = am.id " +
                    "LEFT JOIN districts AS d ON a.dis_id = d.id " +
                    $"WHERE a.emp_id = @LoggedInEmpId AND a.ass_status != '0' AND (a.agency LIKE @SearchQuery " +
                    "OR p.name_th_pro LIKE @SearchQuery OR am.name_th_amp LIKE @SearchQuery OR d.name_th_dis LIKE @SearchQuery " +
                    $"OR a.customer LIKE @SearchQuery) AND (a.create_date LIKE @SearchDateQuery) ";

                // ÊÃéÒ§áÅÐ»ÃÐÁÇÅ¼Å¤ÓÊÑè§ SQL
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");
                command.Parameters.AddWithValue("@SearchDateQuery", "%" + searchDateQuery + "%");
                command.Parameters.AddWithValue("@LoggedInEmpId", LoggedInEmpId);

                int totalItemCount = (int)(long)command.ExecuteScalar();

                // ¤×¹¤èÒ¨Ó¹Ç¹ÃÒÂ¡ÒÃ·Ñé§ËÁ´
                return totalItemCount;
            }
        }

        public int GetTotalItemCountReport(string searchQuery, string searchDateQuery)
        {

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            {
                connection.Open();

                // ÊÃéÒ§¤ÓÊÑè§ SQL à¾×èÍ¹Ñº¨Ó¹Ç¹ÃÒÂ¡ÒÃ·Ñé§ËÁ´
                string sql = "SELECT COUNT(*) FROM tb_assignment_sale AS a " +
                    "LEFT JOIN tb_employee AS e ON a.emp_id = e.emp_id " +
                    "LEFT JOIN tb_login AS l ON e.emp_id = l.emp_id " +
                    "LEFT JOIN provinces AS p ON a.pro_id = p.id " +
                    "LEFT JOIN amphures AS am ON a.amp_id = am.id " +
                    "LEFT JOIN districts AS d ON a.dis_id = d.id " +
                    "WHERE a.ass_status != '0' AND (a.agency LIKE @SearchQuery " +
                    "OR p.name_th_pro LIKE @SearchQuery OR am.name_th_amp LIKE @SearchQuery OR d.name_th_dis LIKE @SearchQuery " +
                    "OR a.customer LIKE @SearchQuery OR l.Username LIKE @SearchQuery) " +
                    "AND (a.create_date LIKE @SearchDateQuery) ";

                // ÊÃéÒ§áÅÐ»ÃÐÁÇÅ¼Å¤ÓÊÑè§ SQL
                MySqlCommand command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");
                command.Parameters.AddWithValue("@SearchDateQuery", "%" + searchDateQuery + "%");

                int totalItemCount = (int)(long)command.ExecuteScalar();

                // ¤×¹¤èÒ¨Ó¹Ç¹ÃÒÂ¡ÒÃ·Ñé§ËÁ´
                return totalItemCount;
            }
        }
        //End -> Get total count



        //Start -> Get Address
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

                    // áÅÐÊÒÁÒÃ¶´Ö§¢éÍÁÙÅ Position ¨Ò¡µÒÃÒ§ tb_position ä´éµÒÁµéÍ§¡ÒÃ


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

        public List<Amphure> GetAmphureInDB()
        {
            List<Amphure> amphures = new List<Amphure>();

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = "SELECT * FROM amphures";
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

                    // áÅÐÊÒÁÒÃ¶´Ö§¢éÍÁÙÅ Position ¨Ò¡µÒÃÒ§ tb_position ä´éµÒÁµéÍ§¡ÒÃ


                    amphures.Add(amphure);
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

            return amphures;
        }

        public List<District> GetDistrictInDB()
        {
            List<District> district = new List<District>();

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = "SELECT * FROM districts";
                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    District districts = new District
                    {
                        DisId = Convert.ToInt32(reader["id"]),
                        DName = reader["name_th_dis"].ToString(),
                        ZipCode = Convert.ToInt32(reader["zip_code"]),
                        AmpId = Convert.ToInt32(reader["amphure_id"])
                    };

                    // áÅÐÊÒÁÒÃ¶´Ö§¢éÍÁÙÅ Position ¨Ò¡µÒÃÒ§ tb_position ä´éµÒÁµéÍ§¡ÒÃ


                    district.Add(districts);
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

            return district;
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

        public IActionResult GetAmphureByAjaxInDB(int proId)
        {
            List<Amphure> amphures = new List<Amphure>();

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = "SELECT * FROM amphures WHERE province_id = @proId";
                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                command.Parameters.AddWithValue("@proId", proId);

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
            return PartialView("_AmphurePartial", amphures);
        }

        public IActionResult GetDistrictByAjaxInDB(int ampId)
        {
            List<District> districts = new List<District>();

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = "SELECT * FROM districts WHERE amphure_id = @ampId";
                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                command.Parameters.AddWithValue("@ampId", ampId);

                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    District district = new District
                    {
                        DisId = Convert.ToInt32(reader["id"]),
                        DName = reader["name_th_dis"].ToString(),
                        ZipCode = Convert.ToInt32(reader["zip_code"]),
                        AmpId = Convert.ToInt32(reader["amphure_id"])
                    };

                    // áÅÐÊÒÁÒÃ¶´Ö§¢éÍÁÙÅ Position ¨Ò¡µÒÃÒ§ tb_position ä´éµÒÁµéÍ§¡ÒÃ

                    districts.Add(district);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while fetching district data from database.");
            }
            finally
            {
                connection.Close();
            }

            return PartialView("_DistrictPartial", districts);
        }
        //End -> Get Address



        //Start -> Get Location
        [HttpGet]
        public IActionResult GetLocationDetails(int assignSaleId)
        {
            try
            {
                // ´Ö§¢éÍÁÙÅ¨Ò¡°Ò¹¢éÍÁÙÅµÒÁ assignSaleId
                var locationDetails = GetLocationDetailsFromDatabase(assignSaleId);

                if (locationDetails != null)
                {
                    return Json(locationDetails);
                }
                else
                {
                    return Json(null); // ËÃ×Í¤×¹¤èÒµÒÁ·Õè¤Ø³µéÍ§¡ÒÃ
                }
            }
            catch (Exception ex)
            {
                // ËÃ×Íãªé Serilog, NLog, ËÃ×ÍÇÔ¸Õ¡ÒÃ log Í×è¹ æ µÒÁ·Õè¤Ø³ãªé
                Console.WriteLine($"Error fetching location details: {ex.Message}");
                return Json(null);
            }
        }

        public AssignmentSale GetLocationDetailsFromDatabase(int assignSaleId)
        {
            try
            {
                // ¹Ó¤èÒ assignSaleId ä» query ¢éÍÁÙÅ¨Ò¡°Ò¹¢éÍÁÙÅ (ã¹·Õè¹ÕéÊÁÁµÔÇèÒ¤Ø³¨Ðãªé ADO.NET)

                // µÑÇÍÂèÒ§â¤é´ ADO.NET
                using (MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("connectionStr")))
                {
                    connection.Open();

                    string query = "SELECT ass_sale_id, latitude, longitude FROM tb_assignment_sale WHERE ass_sale_id = @AssignSaleId";
                    MySqlCommand cmd = new MySqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@AssignSaleId", assignSaleId);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            double latitude = reader.GetDouble("Latitude");
                            double longitude = reader.GetDouble("Longitude");

                            // ÊÃéÒ§ LocationDetailsModel ËÃ×Íãªé model ·Õè¤Ø³¡ÓË¹´àÍ§
                            var locationDetails = new AssignmentSale
                            {
                                Latitude = latitude,
                                Longitude = longitude,
                                AssignSaleId = Convert.ToInt32(reader["ass_sale_id"])
                            };

                            return locationDetails;
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // ã¹¡Ã³Õ·ÕèÁÕ¢éÍ¼Ô´¾ÅÒ´ã¹¡ÒÃàª×èÍÁµèÍ¡Ñº°Ò¹¢éÍÁÙÅ
                Console.WriteLine($"Error fetching location details from database: {ex.Message}");
                throw; // ÊÒÁÒÃ¶¨Ñ´¡ÒÃä´éµèÍä»¶éÒ¤Ø³µéÍ§¡ÒÃ
            }
        }
        //End -> Get location



        //Start -> Get Assignment & Sales from DB
        private IEnumerable<AssignmentSale> GetAssignFromDB(string LoggedInEmpId, int page, int itemsPerPage, string searchQuery, string searchDateQuery)
        {
            List<AssignmentSale> assignment = new List<AssignmentSale>();

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                int offset = (page - 1) * itemsPerPage; // ¤Ó¹Ç³ offset ÊÓËÃÑº¡ÒÃ´Ö§¢éÍÁÙÅË¹éÒ»Ñ¨¨ØºÑ¹

                // ¡ÓË¹´¤ÓÊÑè§ SQL ¾ÃéÍÁàÃÕÂ§ÅÓ´Ñº¢éÍÁÙÅµÒÁ¤ÍÅÑÁ¹ì·Õè¡ÓË¹´
                string sqlSelect = @"
            SELECT a.*, p.*, am.*, d.*
            FROM tb_assignment_sale AS a
            LEFT JOIN provinces AS p ON a.pro_id = p.id
            LEFT JOIN amphures AS am ON a.amp_id = am.id
            LEFT JOIN districts AS d ON a.dis_id = d.id
            WHERE a.emp_id = @LoggedInEmpId AND a.ass_status != '0' AND 
                  (a.agency LIKE @SearchQuery OR p.name_th_pro LIKE @SearchQuery OR d.name_th_dis LIKE @SearchQuery
            OR am.name_th_amp LIKE @SearchQuery OR a.customer LIKE @SearchQuery) AND (a.create_date LIKE @SearchDateQuery)
            ORDER BY a.create_date DESC";


                sqlSelect += " LIMIT @ItemsPerPage OFFSET @Offset";

                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                command.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");
                command.Parameters.AddWithValue("@SearchDateQuery", "%" + searchDateQuery + "%");
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
                        PhoneNumber = reader["phone_number"].ToString(),
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


        private IEnumerable<AssignmentSale> GetReportFromDB(string searchQuery, int page, int itemsPerPage, string searchDateQuery)
        {
            List<AssignmentSale> assignment = new List<AssignmentSale>();

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                int offset = (page - 1) * itemsPerPage; // ¤Ó¹Ç³ offset ÊÓËÃÑº¡ÒÃ´Ö§¢éÍÁÙÅË¹éÒ»Ñ¨¨ØºÑ¹

                string sqlSelect = "SELECT a.*,e.*,l.username,p.*,am.*,d.* FROM tb_assignment_sale AS a " +
                    "LEFT JOIN tb_employee AS e ON a.emp_id = e.emp_id " +
                    "LEFT JOIN tb_login AS l ON e.emp_id = l.emp_id " +
                    "LEFT JOIN provinces AS p ON a.pro_id = p.id " +
                    "LEFT JOIN amphures AS am ON a.amp_id = am.id " +
                    "LEFT JOIN districts AS d ON a.dis_id = d.id " +
                    "WHERE a.ass_status != '0' AND (a.agency LIKE @SearchQuery " +
                    "OR p.name_th_pro LIKE @SearchQuery OR am.name_th_amp LIKE @SearchQuery " +
                    "OR d.name_th_dis LIKE @SearchQuery OR a.customer LIKE @SearchQuery OR l.username LIKE @SearchQuery) " +
                    "AND (a.create_date LIKE @SearchDateQuery) " +
                    " ORDER BY a.create_date DESC " +
                    "LIMIT @ItemsPerPage OFFSET @Offset";



                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                command.Parameters.AddWithValue("@SearchQuery", "%" + searchQuery + "%");
                command.Parameters.AddWithValue("@SearchDateQuery", "%" + searchDateQuery + "%");
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
                        EmpId = Convert.ToInt32(reader["emp_id"]),
                        Username = reader["username"].ToString(),
                        CreateDate = (DateTime)reader["create_date"],
                        PName = reader["name_th_pro"].ToString(),
                        AName = reader["name_th_amp"].ToString(),
                        DName = reader["name_th_dis"].ToString()

                    };

                    if (ass.EmpId == 0)
                    {
                        ass.Username = "Admin";
                    }

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
        //Start -> Get Assignment & Sales from DB



        //Start -> Get Assignment & Sales by ID
        private AssignmentSale GetAssignById(int AssignSaleId)
        {
            AssignmentSale assignment = null;

            string connectionString = _configuration.GetConnectionString("connectionStr");
            using MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = $"SELECT a.*,l1.username AS create_username, l2.username AS last_username " +
                    $"FROM tb_assignment_sale AS a " +
                    $"LEFT JOIN tb_login AS l1 ON a.create_by = l1.emp_id " +
                    $"LEFT JOIN tb_login AS l2 ON a.last_update_by = l2.emp_id " +
                    $"WHERE ass_sale_id = {AssignSaleId}";
                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                using MySqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    assignment = new AssignmentSale
                    {
                        CreateBy = reader["create_by"].ToString(),
                        CreateDate = Convert.ToDateTime(reader["create_date"]),
                        LastUpdateBy = reader["last_update_by"].ToString(),
                        LastUpdateDate = Convert.ToDateTime(reader["last_update_date"]),
                        AssignSaleId = Convert.ToInt32(reader["ass_sale_id"]),
                        Agency = reader["agency"].ToString(),
                        ProId = Convert.ToInt32(reader["pro_id"]),
                        AmpId = Convert.ToInt32(reader["amp_id"]),
                        DisId = Convert.ToInt32(reader["dis_id"]),
                        Customer = reader["customer"].ToString(),
                        Department = reader["department"].ToString(),
                        PhoneNumber = reader["phone_number"].ToString(),
                        LineId = reader["lineid"].ToString(),
                        Annotation = reader["annotation"].ToString(),
                        EmpId = Convert.ToInt32(reader["emp_id"]),
                        CreateByUsername = reader["create_username"].ToString(),
                        LastByUsername = reader["last_username"].ToString(),

                        Latitude = Convert.ToDouble(reader["latitude"].ToString()),
                        Longitude = Convert.ToDouble(reader["longitude"].ToString())
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

            return assignment;
        }

        private Employee GetSalesById(int EmpId)
        {
            Employee employee = null;

            string connectionString = _configuration.GetConnectionString("connectionStr");
            using MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = $"SELECT e.*,l.* FROM tb_employee AS e " +
                    $"LEFT JOIN tb_login AS l ON l.emp_id = e.emp_id " +
                    $"WHERE e.emp_id = {EmpId} ";

                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                using MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    employee = new Employee
                    {
                        CreateDate = Convert.ToDateTime(reader["create_date"]),
                        LastUpdateDate = Convert.ToDateTime(reader["last_update_date"]),
                        EmpId = Convert.ToInt32(reader["emp_id"]),
                        EmpName = reader["emp_name"].ToString(),
                        DateOfBirth = Convert.ToDateTime(reader["date_of_birth"]),
                        Address = reader["address"].ToString(),
                        Email = reader["email"].ToString(),
                        PhoneNumber = reader["phone_number"].ToString(),
                        Salary = Convert.ToDecimal(reader["salary"]),
                        StartDate = Convert.ToDateTime(reader["start_date"]),
                        EndDate = Convert.ToDateTime(reader["end_date"]),
                        PosId = Convert.ToInt32(reader["pos_id"]),
                        Username = reader["username"].ToString(),
                        Password = reader["password"].ToString()

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

            return employee;
        }
        //End -> Get Assignment & Sales by ID


        //Start -> Insert, Update Assignment & Sales
        [HttpPost]
        public IActionResult InsertAssign(AssignmentSale InsertAssign)
        {

            // µÃÇ¨ÊÍºÇèÒ Latitude áÅÐ Longitude äÁèÇèÒ§
            if (InsertAssign.Latitude == 0.0 || InsertAssign.Longitude == 0.0)
            {
                TempData["ErrorMessage"] = "à¡Ô´¢éÍ¼Ô´¾ÅÒ´ã¹¡ÒÃà¾ÔèÁ¾¹Ñ¡§Ò¹: µéÍ§ÃÐºØ¤èÒÅÐµÔ¨Ù´áÅÐÅÍ§¨Ô¨Ù´";
                return RedirectToAction("SummarySale", "Home");
            }


            string connectionString = _configuration.GetConnectionString("connectionStr");
            using MySqlConnection connection = new MySqlConnection(connectionString);

            string sqlInsert = "INSERT INTO tb_assignment_sale (agency, pro_id, amp_id, dis_id, customer, department, phone_number, lineid, " +
                               "annotation, emp_id, create_by, last_update_by, latitude, longitude )" +
                               "VALUES (@agency, @pro_id, @amp_id, @dis_id, @customer, @department, @phone_number, @lineid, @annotation," +
                               " @emp_id, @create_by, @last_update_by, @latitude, @longitude)";

            using MySqlCommand command = new MySqlCommand(sqlInsert, connection);
            command.Parameters.AddWithValue("@agency", InsertAssign.Agency);
            command.Parameters.AddWithValue("@pro_id", InsertAssign.ProId);
            command.Parameters.AddWithValue("@amp_id", InsertAssign.AmpId);
            command.Parameters.AddWithValue("@dis_id", InsertAssign.DisId);
            command.Parameters.AddWithValue("@customer", InsertAssign.Customer);
            command.Parameters.AddWithValue("@department", InsertAssign.Department);
            command.Parameters.AddWithValue("@phone_number", InsertAssign.PhoneNumber);
            command.Parameters.AddWithValue("@lineid", InsertAssign.LineId);
            command.Parameters.AddWithValue("@annotation", InsertAssign.Annotation);
            command.Parameters.AddWithValue("@emp_id", InsertAssign.EmpId);
            command.Parameters.AddWithValue("@create_by", InsertAssign.CreateBy);
            command.Parameters.AddWithValue("@last_update_by", InsertAssign.LastUpdateBy);
            command.Parameters.AddWithValue("@latitude", InsertAssign.Latitude);
            command.Parameters.AddWithValue("@longitude", InsertAssign.Longitude);


            try
            {
                connection.Open();
                command.ExecuteNonQuery();

                TempData["SuccessMessage"] = "Employee added successfully!";
                return RedirectToAction("SummarySale", "Home");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error adding employee: " + ex.Message;
                return RedirectToAction("SummarySale", "Home");
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpPost]
        public IActionResult UpdateAssign(AssignmentSale updatedAssign)
        {
            string connectionString = _configuration.GetConnectionString("connectionStr");
            using MySqlConnection connection = new MySqlConnection(connectionString);

            if (string.IsNullOrWhiteSpace(updatedAssign.AssStatus))
            {
                updatedAssign.AssStatus = "1";
            }

            string sqlUpdate = "UPDATE tb_assignment_sale SET " +
                               "agency = @agency, " +
                               "pro_id = @pro_id, " +
                               "amp_id = @amp_id, " +
                               "dis_id = @dis_id, " +
                               "customer = @customer, " +
                               "department = @department, " +
                               "phone_number = @phone_number, " +
                               "lineid = @lineid, " +
                               "annotation = @annotation, " +
                               "ass_status = @ass_status, " +
                               "latitude = @latitude, " +
                               "longitude = @longitude, " +
                               "last_update_by = @last_update_by " +
                               "WHERE ass_sale_id = @ass_sale_id";

            using MySqlCommand command = new MySqlCommand(sqlUpdate, connection);
            command.Parameters.AddWithValue("@agency", updatedAssign.Agency);
            command.Parameters.AddWithValue("@pro_id", updatedAssign.ProId);
            command.Parameters.AddWithValue("@amp_id", updatedAssign.AmpId);
            command.Parameters.AddWithValue("@dis_id", updatedAssign.DisId);
            command.Parameters.AddWithValue("@customer", updatedAssign.Customer);
            command.Parameters.AddWithValue("@department", updatedAssign.Department);
            command.Parameters.AddWithValue("@phone_number", updatedAssign.PhoneNumber);
            command.Parameters.AddWithValue("@lineid", updatedAssign.LineId);
            command.Parameters.AddWithValue("@annotation", updatedAssign.Annotation);
            command.Parameters.AddWithValue("@ass_status", updatedAssign.AssStatus);
            command.Parameters.AddWithValue("@latitude", updatedAssign.Latitude);
            command.Parameters.AddWithValue("@longitude", updatedAssign.Longitude);
            command.Parameters.AddWithValue("@last_update_by", updatedAssign.LastUpdateBy);
            command.Parameters.AddWithValue("@ass_sale_id", updatedAssign.AssignSaleId);

            try
            {
                connection.Open();
                command.ExecuteNonQuery();
                TempData["SuccessMessage"] = "Assignment updated successfully!";
                return RedirectToAction("SummarySale", "Home");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating assignment: " + ex.Message;
                return RedirectToAction("SummarySale", "Home");
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpPost]
        public IActionResult UpdateReportAssign(AssignmentSale updatedAssign)
        {
            string connectionString = _configuration.GetConnectionString("connectionStr");
            using MySqlConnection connection = new MySqlConnection(connectionString);

            if (string.IsNullOrWhiteSpace(updatedAssign.AssStatus))
            {
                updatedAssign.AssStatus = "1";
            }

            string sqlUpdate = "UPDATE tb_assignment_sale SET " +
                               "agency = @agency, " +
                               "pro_id = @pro_id, " +
                               "amp_id = @amp_id, " +
                               "dis_id = @dis_id, " +
                               "customer = @customer, " +
                               "department = @department, " +
                               "phone_number = @phone_number, " +
                               "lineid = @lineid, " +
                               "annotation = @annotation, " +
                               "ass_status = @ass_status, " +
                               "latitude = @latitude, " +
                               "longitude = @longitude, " +
                               "last_update_by = @last_update_by " +
                               "WHERE ass_sale_id = @ass_sale_id";

            using MySqlCommand command = new MySqlCommand(sqlUpdate, connection);
            command.Parameters.AddWithValue("@agency", updatedAssign.Agency);
            command.Parameters.AddWithValue("@pro_id", updatedAssign.ProId);
            command.Parameters.AddWithValue("@amp_id", updatedAssign.AmpId);
            command.Parameters.AddWithValue("@dis_id", updatedAssign.DisId);
            command.Parameters.AddWithValue("@customer", updatedAssign.Customer);
            command.Parameters.AddWithValue("@department", updatedAssign.Department);
            command.Parameters.AddWithValue("@phone_number", updatedAssign.PhoneNumber);
            command.Parameters.AddWithValue("@lineid", updatedAssign.LineId);
            command.Parameters.AddWithValue("@annotation", updatedAssign.Annotation);
            command.Parameters.AddWithValue("@ass_status", updatedAssign.AssStatus);
            command.Parameters.AddWithValue("@latitude", updatedAssign.Latitude);
            command.Parameters.AddWithValue("@longitude", updatedAssign.Longitude);
            command.Parameters.AddWithValue("@last_update_by", updatedAssign.LastUpdateBy);
            command.Parameters.AddWithValue("@ass_sale_id", updatedAssign.AssignSaleId);

            try
            {
                connection.Open();
                command.ExecuteNonQuery();
                TempData["SuccessMessage"] = "Assignment updated successfully!";
                return RedirectToAction("Report", "Home");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating assignment: " + ex.Message;
                return RedirectToAction("Report", "Home");
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpPost]
        public IActionResult UpdateSalesUserInDB(Login updateSalesUser)
        {
            string connectionString = _configuration.GetConnectionString("connectionStr");

            using MySqlConnection connection = new MySqlConnection(connectionString);

            string sqlUpdate = "UPDATE tb_login SET emp_id = @emp_id , password = @password " +
                               //"last_update_by = @Last_update_by, last_update_date = NOW() " +
                               "WHERE emp_id = @emp_id";

            using MySqlCommand command = new MySqlCommand(sqlUpdate, connection);
            command.Parameters.AddWithValue("@emp_id", updateSalesUser.EmpId);
            command.Parameters.AddWithValue("@password", updateSalesUser.Password);

            try
            {
                connection.Open();
                command.ExecuteNonQuery();
                TempData["SuccessMessage"] = "Employee updated successfully!";
                return RedirectToAction("SummarySale", "Home");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating employee: " + ex.Message;
                return RedirectToAction("SummarySale", "Home");
            }
            finally
            {
                connection.Close();
            }
        }
        //End -> Insert, Update Assignment & Sales



        //Start -> Position permission
        public IActionResult UpdatePosPermissionsInDB(Position updatePosPermissions)
        {
            string connectionString = _configuration.GetConnectionString("connectionStr");

            using MySqlConnection connection = new MySqlConnection(connectionString);

            string sqlUpdate = "UPDATE tb_position SET pos_id = @pos_id , pos_permissions_assign = @pos_permissions_assign " +
                               "WHERE pos_id = @pos_id";

            if (string.IsNullOrWhiteSpace(updatePosPermissions.PosPermissions))
            {
                updatePosPermissions.PosPermissions = "0";
            }

            using MySqlCommand command = new MySqlCommand(sqlUpdate, connection);
            command.Parameters.AddWithValue("@pos_id", updatePosPermissions.PosId);
            command.Parameters.AddWithValue("@pos_permissions_assign", updatePosPermissions.PosPermissions);

            try
            {
                connection.Open();
                command.ExecuteNonQuery();
                TempData["SuccessMessage"] = "Permissions updated successfully!";
                return RedirectToAction("Permissions", "Home");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating Permissions: " + ex.Message;
                return RedirectToAction("Permissions", "Home");
            }
            finally
            {
                connection.Close();
            }
        }

        //End -> Position permission















        //Not use
        private IEnumerable<Employee> GetSalesFromDB()
        {
            List<Employee> employee = new List<Employee>();

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = "SELECT e.*,p.pos_name FROM tb_employee as e JOIN tb_position as p ON e.pos_id = p.pos_id WHERE p.pos_name = 'sales' AND e.username IS NULL OR e.username = '' ";
                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Employee emp = new Employee
                    {
                        EmpId = Convert.ToInt32(reader["emp_id"]),
                        EmpName = reader["emp_name"].ToString(),
                        DateOfBirth = Convert.ToDateTime(reader["date_of_birth"]),
                        Address = reader["address"].ToString(),
                        Email = reader["email"].ToString(),
                        PhoneNumber = reader["phone_number"].ToString(),
                        CreateDate = (DateTime)reader["create_date"],

                        StartDate = (DateTime)reader["start_date"],

                        PosName = reader["pos_name"].ToString()

                    };



                    employee.Add(emp);
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

            return employee;
        }
        private IEnumerable<Employee> GetSalesUserFromDB()
        {
            List<Employee> employee = new List<Employee>();

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = "SELECT e.*,p.pos_name FROM tb_employee as e JOIN tb_position as p ON e.pos_id = p.pos_id " +
                    "WHERE p.pos_name = 'sales' AND e.username != NULL OR p.pos_name = 'sales' AND e.username != '' ";
                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Employee emp = new Employee
                    {
                        EmpId = Convert.ToInt32(reader["emp_id"]),
                        EmpName = reader["emp_name"].ToString(),
                        DateOfBirth = Convert.ToDateTime(reader["date_of_birth"]),
                        Address = reader["address"].ToString(),
                        Email = reader["email"].ToString(),
                        PhoneNumber = reader["phone_number"].ToString(),
                        CreateDate = (DateTime)reader["create_date"],

                        StartDate = (DateTime)reader["start_date"],

                        PosName = reader["pos_name"].ToString()

                    };



                    employee.Add(emp);
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

            return employee;
        }
        [HttpPost]
        public IActionResult InsertSalesUserInDB(Employee employee)
        {

            string connectionString = _configuration.GetConnectionString("connectionStr");
            using MySqlConnection connection = new MySqlConnection(connectionString);

            string sqlInsert = "UPDATE tb_employee SET username = @username , password = @password WHERE emp_id = @emp_id";

            using MySqlCommand command = new MySqlCommand(sqlInsert, connection);
            command.Parameters.AddWithValue("@emp_id", employee.EmpId);
            command.Parameters.AddWithValue("@username", employee.Username);
            command.Parameters.AddWithValue("@password", employee.Password);

            try
            {
                connection.Open();
                command.ExecuteNonQuery();
                TempData["SuccessMessage"] = "Sales User added successfully!";
                return RedirectToAction("Account", "Home");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error adding Sales User: " + ex.Message;
                return RedirectToAction("Account", "Home");
            }
            finally
            {
                connection.Close();
            }
        }
        //Not use



        //Not use employee
        [HttpPost]
        public IActionResult InsertEmp(Employee InsertEmp)
        {

            string connectionString = _configuration.GetConnectionString("connectionStr");
            using MySqlConnection connection = new MySqlConnection(connectionString);

            string sqlInsert = "INSERT INTO tb_employee (emp_name, date_of_birth, address, email, phone_number, salary, start_date, end_date, create_by, last_update_by, pos_id) " +
                               "VALUES (@emp_name, @date_of_birth, @address, @email, @phone_number, @salary, @start_date, @end_date, @create_by, @last_update_by, @pos_id)";

            using MySqlCommand command = new MySqlCommand(sqlInsert, connection);
            command.Parameters.AddWithValue("@emp_name", InsertEmp.EmpName);
            command.Parameters.AddWithValue("@date_of_birth", InsertEmp.DateOfBirth);
            command.Parameters.AddWithValue("@address", InsertEmp.Address);
            command.Parameters.AddWithValue("@email", InsertEmp.Email);
            command.Parameters.AddWithValue("@phone_number", InsertEmp.PhoneNumber);
            command.Parameters.AddWithValue("@salary", InsertEmp.Salary);
            command.Parameters.AddWithValue("@start_date", InsertEmp.StartDate);
            command.Parameters.AddWithValue("@end_date", InsertEmp.EndDate.ToString("yyyy-MM-dd HH:mm:ss") ?? DBNull.Value.ToString());
            command.Parameters.AddWithValue("@create_by", InsertEmp.CreateBy);
            command.Parameters.AddWithValue("@last_update_by", InsertEmp.LastUpdateBy);
            command.Parameters.AddWithValue("@pos_id", InsertEmp.PosId);

            try
            {
                connection.Open();
                command.ExecuteNonQuery();
                TempData["SuccessMessage"] = "Employee added successfully!";
                return RedirectToAction("Summary", "Home");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error adding employee: " + ex.Message;
                return RedirectToAction("Summary", "Home");
            }
            finally
            {
                connection.Close();
            }
        }
        [HttpPost]
        public IActionResult UpdateEmpInDB(Employee updatedEmployee)
        {
            string connectionString = _configuration.GetConnectionString("connectionStr");

            using MySqlConnection connection = new MySqlConnection(connectionString);

            string sqlUpdate = "UPDATE tb_employee SET emp_name = @emp_name, date_of_birth = @date_of_birth, address = @address, " +
                               "email = @email, phone_number = @phone_number, salary = @salary, start_date = @start_date, " +
                               "end_date = @end_date, pos_id = @pos_id, last_update_by = @Last_update_by, last_update_date = NOW() " +
                               "WHERE emp_id = @emp_id";

            using MySqlCommand command = new MySqlCommand(sqlUpdate, connection);
            command.Parameters.AddWithValue("@emp_id", updatedEmployee.EmpId);
            command.Parameters.AddWithValue("@emp_name", updatedEmployee.EmpName);
            command.Parameters.AddWithValue("@date_of_birth", updatedEmployee.DateOfBirth);
            command.Parameters.AddWithValue("@address", updatedEmployee.Address);
            command.Parameters.AddWithValue("@email", updatedEmployee.Email);
            command.Parameters.AddWithValue("@phone_number", updatedEmployee.PhoneNumber);
            command.Parameters.AddWithValue("@salary", updatedEmployee.Salary);
            command.Parameters.AddWithValue("@start_date", updatedEmployee.StartDate);
            command.Parameters.AddWithValue("@end_date", updatedEmployee.EndDate);
            command.Parameters.AddWithValue("@pos_id", updatedEmployee.PosId);
            command.Parameters.AddWithValue("@last_update_by", updatedEmployee.LastUpdateBy);
            command.Parameters.AddWithValue("@last_update_date", updatedEmployee.LastUpdateDate);

            try
            {
                connection.Open();
                command.ExecuteNonQuery();
                TempData["SuccessMessage"] = "Employee updated successfully!";
                return RedirectToAction("Summary", "Home");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating employee: " + ex.Message;
                return RedirectToAction("Summary", "Home");
            }
            finally
            {
                connection.Close();
            }
        }
        private IEnumerable<Employee> GetEmpFromDB()
        {
            List<Employee> employee = new List<Employee>();

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = "SELECT e.*,p.pos_name FROM tb_employee as e JOIN tb_position as p ON e.pos_id = p.pos_id";
                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Employee emp = new Employee
                    {
                        EmpId = Convert.ToInt32(reader["emp_id"]),
                        EmpName = reader["emp_name"].ToString(),
                        DateOfBirth = Convert.ToDateTime(reader["date_of_birth"]),
                        Address = reader["address"].ToString(),
                        Email = reader["email"].ToString(),
                        PhoneNumber = reader["phone_number"].ToString(),
                        CreateDate = (DateTime)reader["create_date"],

                        StartDate = (DateTime)reader["start_date"],

                        PosName = reader["pos_name"].ToString()

                    };



                    employee.Add(emp);
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

            return employee;
        }
        private List<Position> GetPosFromDB()
        {
            List<Position> positions = new List<Position>();

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = "SELECT * FROM tb_position";
                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                MySqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Position pos = new Position
                    {
                        PosId = Convert.ToInt32(reader["pos_id"]),
                        PosName = reader["pos_name"].ToString(),
                        PosPermissions = reader["pos_permissions_assign"].ToString()
                    };

                    // áÅÐÊÒÁÒÃ¶´Ö§¢éÍÁÙÅ Position ¨Ò¡µÒÃÒ§ tb_position ä´éµÒÁµéÍ§¡ÒÃ


                    positions.Add(pos);
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

            return positions;
        }
        private Employee GetEmployeeById(int EmpId)
        {
            Employee employee = null;

            string connectionString = _configuration.GetConnectionString("connectionStr");
            using MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = $"SELECT * FROM tb_employee WHERE emp_id = {EmpId}";
                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                using MySqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    employee = new Employee
                    {
                        CreateBy = reader["create_by"].ToString(),
                        CreateDate = Convert.ToDateTime(reader["create_date"]),
                        LastUpdateBy = reader["last_update_by"].ToString(),
                        LastUpdateDate = Convert.ToDateTime(reader["last_update_date"]),
                        EmpId = Convert.ToInt32(reader["emp_id"]),
                        EmpName = reader["emp_name"].ToString(),
                        DateOfBirth = Convert.ToDateTime(reader["date_of_birth"]),
                        Address = reader["address"].ToString(),
                        Email = reader["email"].ToString(),
                        PhoneNumber = reader["phone_number"].ToString(),
                        Salary = Convert.ToDecimal(reader["salary"]),
                        StartDate = Convert.ToDateTime(reader["start_date"]),
                        EndDate = Convert.ToDateTime(reader["end_date"]),
                        PosId = Convert.ToInt32(reader["pos_id"])
                    };

                }
                if (employee != null && employee.CreateBy != null && employee.CreateBy != "")
                {
                    EmployeeCreateBy employeecreateby = GetEmployeeByCreateFromDB(employee.CreateBy);
                    employee.CreateByUsername = employeecreateby.Username;

                    employee.LastByUsername = GetEmployeeByLastUpdateFromDB(employee.LastUpdateBy);

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

            return employee;
        }
        private Position GetPosById(int Id)
        {
            Position position = null;

            string connectionString = _configuration.GetConnectionString("connectionStr");
            using MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = $"SELECT * FROM tb_position WHERE pos_id = {Id}";
                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                using MySqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    position = new Position
                    {
                        PosId = Convert.ToInt32(reader["pos_id"]),
                        PosName = reader["pos_name"].ToString(),
                        PosPermissions = reader["pos_permissions_assign"].ToString()

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

            return position;
        }
        private EmployeeCreateBy GetEmployeeByCreateFromDB(string createby)
        {
            EmployeeCreateBy employee_createby = null;

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = $"SELECT * FROM tb_employee WHERE emp_id = {createby}";
                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                MySqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    employee_createby = new EmployeeCreateBy
                    {

                        Username = reader["username"].ToString(),
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

            return employee_createby;
        }
        private string GetEmployeeByLastUpdateFromDB(string lastupdateby)
        {
            Employee employee_lastupdateby = null;

            string connectionString = _configuration.GetConnectionString("connectionStr");
            MySqlConnection connection = new MySqlConnection(connectionString);

            try
            {
                connection.Open();

                string sqlSelect = $"SELECT * FROM tb_employee WHERE emp_id = {lastupdateby}";
                MySqlCommand command = new MySqlCommand(sqlSelect, connection);
                MySqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    employee_lastupdateby = new Employee
                    {
                        Username = reader["username"].ToString()
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

            return employee_lastupdateby.Username;
        }
        //Not use employee





    }

}

