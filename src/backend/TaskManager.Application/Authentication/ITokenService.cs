using TaskManager.Domain;

namespace TaskManager.Application.Authentication;

public interface ITokenService
{
    AccessToken Create(User user);
}
