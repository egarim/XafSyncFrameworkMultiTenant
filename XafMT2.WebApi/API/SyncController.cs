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


            PushOperationResponse pushOperationResponse = new PushOperationResponse();
            try
            {
                string NodeId = GetHeader("NodeId");
                StreamReader streamReader = new StreamReader(base.Request.Body);
                string s = await streamReader.ReadToEndAsync();
                using MemoryStream ms = new MemoryStream(Encoding.Unicode.GetBytes(s));
                DataContractJsonSerializer dataContractJsonSerializer = new DataContractJsonSerializer(typeof(List<Delta>));
                List<Delta> Deltas = (List<Delta>)dataContractJsonSerializer.ReadObject((Stream)ms);
                await _SyncServer.SaveDeltasAsync(NodeId, Deltas, default(CancellationToken));
                string message = $"Push to node:{NodeId}{Environment.NewLine}Deltas Received:{Deltas.Count}{Environment.NewLine}Identity:{Deltas.FirstOrDefault()?.Identity}";
                _logger.LogInformation(message);
                pushOperationResponse.Success = true;
                pushOperationResponse.Message = message;
            }
            catch (NodeNotFoundException ex)
            {
                _logger.LogError(ex, "An argument null exception occurred.");
                pushOperationResponse.Success = false;
                pushOperationResponse.Message = ex.Message;
            }
            catch (ArgumentNullException exception)
            {
                _logger.LogError(exception, "An argument null exception occurred.");
            }
            catch (InvalidOperationException exception2)
            {
                _logger.LogError(exception2, "An invalid operation exception occurred.");
            }
            catch (Exception exception3)
            {
                _logger.LogError(exception3, "An unknown exception occurred.");
            }

            DataContractJsonSerializer dataContractJsonSerializer2 = new DataContractJsonSerializer(typeof(PushOperationResponse));
            MemoryStream memoryStream = new MemoryStream();
            dataContractJsonSerializer2.WriteObject((Stream)memoryStream, (object?)pushOperationResponse);
            memoryStream.Position = 0L;
            StreamReader streamReader2 = new StreamReader(memoryStream);
            return streamReader2.ReadToEnd();

            //  return base.Push();
        }

        public override Task<IActionResult> RegisterNode()
        {
            return base.RegisterNode();
        }
    }

}
