using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApiSecurityExample2.Controllers.v1
{

    /// <summary>
    /// Generic form of collection URI is: https://{domain}[/api]/{capability}/{facility}/{versionNo}/{collectionResource}
    /// </summary>
    [Route("CompaniesHouse/Register/v1/Companies")]
    public class CompanyController : Controller
    {
        /// <summary>
        /// Returns a list of CompaniesHouse Companies.
        /// </summary>
        // GET: api/ResourceTypes
        [HttpGet()]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client, VaryByHeader = "Accept")]
        [Produces("application/json", "application/xml")]
        public IActionResult Get()
        {
            var draftJson = new
            {
                total = 1,
                page = 1,
                records = 1,
                rows = new[]
                {
                    new{
                        id = 8,
                        cell = new[]
                        {
                            "data1",
                            "data2",
                            "9/13/2010",
                        }
                    }
                },
            };

            return Json(draftJson);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [NonAction]
        protected IActionResult GetImpl()
        {
            try
            {
                return null;
            }
            catch (Exception e)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
            }
        }
    }
}
