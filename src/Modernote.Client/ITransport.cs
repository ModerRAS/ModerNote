using System.Threading.Tasks;
using Modernote.Protocol;

namespace Modernote.Client;

public interface ITransport
{
    Task<ApiResponse> SendAsync(ApiRequest request);
}
