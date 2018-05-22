using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Query;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using KittyCore;
using Microsoft.ServiceFabric.Data.Collections;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using StatefulService = Microsoft.ServiceFabric.Services.Runtime.StatefulService;

namespace KittyGotBack
{
    /// <summary>
    /// An instance of this class is created for each service replica by the Service Fabric runtime.
    /// </summary>
    internal sealed class KittyGotBack : StatefulService, IKittyBackendService
    {
        private CloudBlobContainer _blobContainer;

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

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var storageConnectionString = Environment.GetEnvironmentVariable("containerConnectionString");

            if (CloudStorageAccount.TryParse(storageConnectionString, out var storageAccount))
            {
                ServiceEventSource.Current.ServiceMessage(Context, "Connected to storage account");
                var operationContext = new OperationContext();
                var client = storageAccount.CreateCloudBlobClient();
                _blobContainer = client.GetContainerReference("kitties");
                if (await _blobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off,
                    new BlobRequestOptions(),
                    operationContext, cancellationToken))
                {
                    var kitties = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("kitties");
                    await kitties.ClearAsync();
                }
                return;
            }

            ServiceEventSource.Current.ServiceMessage(Context, "Could not connect to storage account.  Failure.");
            throw new ArgumentException("Connection String specified is invalid");

        }

        /// <summary>
        /// This is the main entry point for your service replica.
        /// This method executes when this replica of your service becomes primary and has write status.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service replica.</param>
        public async Task<byte[]> GetImageAsync(int width, int height)
        {
            var kitties = await StateManager.GetOrAddAsync<IReliableDictionary<string, string>>("kitties");

            var requestKey = $"{width}x{height}";

            using (var tx = StateManager.CreateTransaction())
            {
                var result = await kitties.TryGetValueAsync(tx, requestKey);

                if (result.HasValue)
                {
                    var blobRef = _blobContainer.GetBlockBlobReference(result.Value);
                    if (!await blobRef.ExistsAsync())
                    {
                        await kitties.TryRemoveAsync(tx, requestKey);
                    }
                    else
                    {
                        await blobRef.FetchAttributesAsync();
                        var imageArray = new byte[blobRef.Properties.Length];
                        await blobRef.DownloadToByteArrayAsync(imageArray, 0);
                        ServiceEventSource.Current.ServiceMessage(Context, $"Found image in Azure cache, returning: {requestKey}.");
                        return imageArray;
                    }

                }


                var client = new HttpClient();


                var get = await client.GetAsync($"http://placekitten.com/{width}/{height}");

                if (!get.IsSuccessStatusCode)
                    return null;

                var bytes = await get.Content.ReadAsByteArrayAsync();

                var newKey = string.Empty;

                using (var sha1 = new SHA1CryptoServiceProvider())
                {
                    newKey = Convert.ToBase64String(sha1.ComputeHash(bytes));
                }

                var newBlob = _blobContainer.GetBlockBlobReference(newKey);

                var addToDictionary = kitties.AddOrUpdateAsync(tx, requestKey, newKey, (key, value) => newKey);
                var addToAzure = newBlob.DeleteIfExistsAsync().ContinueWith(t=> newBlob.UploadFromByteArrayAsync(bytes, 0, bytes.Length));

                await Task.WhenAll(addToAzure, addToDictionary);
                
                await tx.CommitAsync();
                ServiceEventSource.Current.ServiceMessage(Context, $"Had to download image with key {requestKey}, now it lives in Azure under the hash: {newKey}.");
                return bytes;
            }

        }
    }
}
