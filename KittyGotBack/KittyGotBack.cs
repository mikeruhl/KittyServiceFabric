using System;
using System.Collections.Generic;
using System.Fabric;
using System.Net.Http;
using System.Threading.Tasks;
using KittyCore;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace KittyGotBack
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class KittyGotBack : StatefulService, IKittyBackendService
    {
        public KittyGotBack(StatefulServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners (e.g., HTTP, Service Remoting, WCF, etc.) for this service replica to handle client or user requests.
        /// </summary>
        /// <remarks>
        /// For more information on service communication, see https://aka.ms/servicefabricservicecommunication
        /// </remarks>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        public async Task<byte[]> GetImageAsync(int width, int height)
        {
            var kitties = await this.StateManager.GetOrAddAsync<IReliableDictionary<string, byte[]>>("kitties");

            var requestKey = $"{width}x{height}";

            using (var tx = StateManager.CreateTransaction())
            {
                var result = await kitties.TryGetValueAsync(tx, requestKey);

                if (result.HasValue)
                    return result.Value;


                var client = new HttpClient();


                var get = await client.GetAsync($"http://placekitten.com/{width}/{height}");

                if (!get.IsSuccessStatusCode)
                    return null;

                var bytes = await get.Content.ReadAsByteArrayAsync();

                await kitties.AddOrUpdateAsync(tx, requestKey, bytes, (key, value) => bytes);

                await tx.CommitAsync();

                return bytes;
            }

        }
    }
}
