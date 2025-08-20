using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace PeplinkSPTool.Controllers
{
    public class PeplinkAuthHandler : ControllerBase
    {
        #region Initialisation
        [HttpGet]
        public IActionResult Index()
        {
            return BadRequest();
        }
        #endregion Initialisation

        /// <summary>
        /// Peplink Auth Reply
        /// </summary>
        [HttpGet("reply")]
        [AllowAnonymous]
        public IActionResult GetAuthReply([FromQuery]string code)
        {
            if( string.IsNullOrEmpty(code) )
            {
                return BadRequest("Peplink did not respond with a valid code");
            }
            
            Program.PeplinkChallengeCode = code;
            return Ok("Peplink authentication successful. You may now close this window.");
        }
    }
}