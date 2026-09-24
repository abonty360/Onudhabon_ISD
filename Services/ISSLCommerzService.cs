using Onudhabon_ISD.Models;

namespace Onudhabon_ISD.Services
{
    public interface ISSLCommerzService
    {
        Task<SSLCommerzInitResponse?> InitiatePaymentAsync(Donation donation, string hostUrl);
        Task<SSLCommerzValidationResponse?> ValidatePaymentAsync(string valId);
    }
}
