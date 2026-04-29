using System.Threading.Tasks;

namespace gearOps.Application.Interfaces;

public interface IEmailService
{
    Task SendOtpEmailAsync(string email, string otp);
}
