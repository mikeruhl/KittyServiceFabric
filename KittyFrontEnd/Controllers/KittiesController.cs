using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using KittyCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using System.Drawing;

namespace KittyFrontEnd.Controllers
{
    [Route("api/[controller]")]
    public class KittiesController : Controller
    {
        [HttpGet("{w}/{h}")]
        public async Task<IActionResult> Get(int w, int h)
        {
            try
            {
                var backend = BackendService();

                HttpContext.Response.ContentType = "image/jpeg";
                var response = await backend.GetImageAsync(w, h);

                if (response == null)
                    return NotFound();

                return File(response, "image/jpeg");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

        }

        private static IKittyBackendService BackendService()
        {
            return ServiceProxy.Create<IKittyBackendService>(new ServiceUriBuilder("KittyGotBack").ToUri(),
                new ServicePartitionKey(0));
        }
    }

    public class ServiceUriBuilder
    {
        public ServiceUriBuilder(string serviceInstance)
        {
            this.ActivationContext = FabricRuntime.GetActivationContext();
            ServiceInstance = serviceInstance;
        }

        public ServiceUriBuilder(ICodePackageActivationContext context, string serviceInstance)
        {
            this.ActivationContext = context;
            this.ServiceInstance = serviceInstance;
        }

        public ServiceUriBuilder(ICodePackageActivationContext context, string applicationInstance, string serviceInstance)
        {
            this.ActivationContext = context;
            this.ApplicationInstance = applicationInstance;
            this.ServiceInstance = serviceInstance;
        }

        /// <summary>
        /// The name of the application instance that contains he service.
        /// </summary>
        public string ApplicationInstance { get; set; }

        /// <summary>
        /// The name of the service instance.
        /// </summary>
        public string ServiceInstance { get; set; }

        /// <summary>
        /// The local activation context
        /// </summary>
        public ICodePackageActivationContext ActivationContext { get; set; }

        public Uri ToUri()
        {
            string applicationInstance = this.ApplicationInstance;

            if (String.IsNullOrEmpty(applicationInstance))
            {
                // the ApplicationName property here automatically prepends "fabric:/" for us
                applicationInstance = this.ActivationContext.ApplicationName.Replace("fabric:/", String.Empty);
            }

            return new Uri("fabric:/" + applicationInstance + "/" + this.ServiceInstance);
        }
    }
}
