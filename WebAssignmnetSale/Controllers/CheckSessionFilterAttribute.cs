 using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;

namespace WebAssignmentSale.Controllers
{
    public class CheckSessionFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;
            if (session.GetString("UserName") == null)
            {
                var controller = context.Controller as Controller;
                if (controller != null)
                {
                    controller.TempData["SessionError"] = "Not found session";
                }


                context.Result = new RedirectToActionResult("Login", "Login", null);
            }
        }
    }

}