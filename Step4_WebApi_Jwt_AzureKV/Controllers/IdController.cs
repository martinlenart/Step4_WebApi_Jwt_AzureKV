﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Step3_WebApi_Jwt_AzureKV.Models;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Step3_WebApi_Jwt_AzureKV.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class IdController : Controller
    {
        private ILogger<IdController> _logger;

        //GET /id
        [HttpGet()]
        [ProducesResponseType(200, Type = typeof(WebApiID))]
        public IActionResult Get()
        {
            var wai = new WebApiID();
            wai.Version += $"  - {AppConfig.SecretMessage} - {AppConfig.CurrentDbConnectionString}";

            return Ok(wai);
        }

        public IdController(ILogger<IdController> logger)
        {
            _logger = logger;
            _logger.LogInformation($"IdController started");
        }
    }
}

