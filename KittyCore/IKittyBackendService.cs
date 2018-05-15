using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;

namespace KittyCore
{
    public interface IKittyBackendService : IService
    {
        Task<byte[]> GetImageAsync(int width, int height);
    }
}
