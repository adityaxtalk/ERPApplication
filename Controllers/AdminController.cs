using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPApplication.Controllers
{

    [Authorize(Roles ="Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return Ok("You have accesses the Admin Controller");
        }
    }
}
