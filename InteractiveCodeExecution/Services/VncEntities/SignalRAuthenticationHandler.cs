using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Security;
using MarcusW.VncClient;

namespace InteractiveCodeExecution.Services.VncEntities
{
    public class SignalRAuthenticationHandler : IAuthenticationHandler
    {
        public async Task<TInput> ProvideAuthenticationInputAsync<TInput>(RfbConnection connection, ISecurityType securityType, IAuthenticationInputRequest<TInput> request)
        where TInput : class, IAuthenticationInput
        {
            await Task.Yield();
            if (typeof(TInput) == typeof(PasswordAuthenticationInput))
            {
                string password = "No password Needed";

                return (TInput)Convert.ChangeType(new PasswordAuthenticationInput(password), typeof(TInput));
            }

            throw new InvalidOperationException($"The authentication input \"{typeof(TInput).FullName}\" request is not supported by this authentication handler.");
        }
    }
}
