using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Step3_WebApi_Jwt_AzureKV.Models;
using Step3_WebApi_Jwt_AzureKV.Services;
using Step3_WebApi_Jwt_AzureKV.JwtAuthorization;

namespace Step3_WebApi_Jwt_AzureKV.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private ILoginService _loginService;
        private ILogger<LoginController> _logger;

        public LoginController(ILoginService userlogin, ILogger<LoginController> logger)
        {
            _loginService = userlogin;
            _logger = logger;

            _logger.LogInformation("LoginController started");
        }


        //GET: api/Login/LoginUser
        [HttpPost]
        [ProducesResponseType(200, Type = typeof(JwtUserToken))]
        [ProducesResponseType(400, Type = typeof(string))]
        public IActionResult LoginUser([FromBody] UserCredentials userLogins)
        {
            _logger.LogInformation("LoginUser initiated");

            var Token = new JwtUserToken();
            if (_loginService.LoginUser(userLogins.UserName, userLogins.Password, out User user))
            {
                Token = JwtAuthorization.JwtAuthorization.CreateJwtTokenKey(new JwtUserToken()
                {
                    UserRole = user.Roles,
                    UserEmail = user.Email,
                    UserName = user.Name,
                    UserId = user.UserId,
                }, AppConfig.JwtSetting); ;

                _logger.LogInformation("User logged in. Token sent");
                return Ok(Token);
            }

            _logger.LogWarning("wrong user or password");
            return BadRequest($"wrong user or password");
        }

        //GET: api/Login/LoggedInUsers
        [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet]
        [ProducesResponseType(200, Type = typeof(IEnumerable<User>))]
        public IActionResult LoggedInUsers()
        {
            _logger.LogInformation("LoggedInUsers initiated");

            return Ok(_loginService.LoggedinUsers);
        }
    }
}