using BIT.Data.Sync;
using BIT.Data.Sync.AspNetCore.Controllers;
using BIT.Data.Sync.Client;
using BIT.Data.Sync.Server;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.Serialization.Json;
using System.Text;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace XafMT2.WebApi.API
{

    [ApiController]
    [Route("[controller]")]
    public class SyncController : SyncControllerBase
    {
        public SyncController(ILogger<SyncControllerBase> logger, ISyncServer SyncServer) : base(logger, SyncServer)
        {
        }

        public override Task<string> Fetch(string startIndex, string identity)
        {
            return base.Fetch(startIndex, identity);
        }

        public async override Task<string> Push()
        {
            string NodeId = GetHeader("NodeId");

            return await base.Push();
        }

        public override async Task<IActionResult> RegisterNode()
        {
            return await base.RegisterNode();
        }
    }

}
