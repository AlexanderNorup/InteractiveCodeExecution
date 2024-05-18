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
                // TODO: Set VNC_PASSWORD environment variable to random 8-char string when creating VNC containers
                // Then fetch that value and set it here. 
                string password = "12345678";

                return (TInput)Convert.ChangeType(new PasswordAuthenticationInput(password), typeof(TInput));
            }

            throw new InvalidOperationException($"The authentication input \"{typeof(TInput).FullName}\" request is not supported by this authentication handler.");
        }
    }
}
