using HomerLy.DataAccess.Interfaces;
using HomerLy.DataAccess.Utils;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace HomerLy.DataAccess.Commons
{
    public class ClaimsService : IClaimsService
    {
        public ClaimsService(IHttpContextAccessor httpContextAccessor)
        {
            // Lấy ClaimsIdentity
            var identity = httpContextAccessor.HttpContext?.User?.Identity as ClaimsIdentity;

            var extractedId = AuthenTools.GetCurrentUserId(identity);
            if (Guid.TryParse(extractedId, out var parsedId))
                GetCurrentUserId = parsedId;
            else
                GetCurrentUserId = Guid.Empty;

            IpAddress = httpContextAccessor?.HttpContext?.Connection?.RemoteIpAddress?.ToString();
        }

        public Guid GetCurrentUserId { get; }

        public string? IpAddress { get; }
    }
}
