using System.Globalization;
using System.Security.Claims;

namespace TaskManager.SharedApi;

internal static class CurrentIdentity
{
    public static bool TryGetUserId(ClaimsPrincipal? principal, out int userId)
    {
        userId = 0;
        var subjects = principal?.FindAll("sub").ToArray();
        return subjects is { Length: 1 }
            && int.TryParse(subjects[0].Value, NumberStyles.None, CultureInfo.InvariantCulture, out userId)
            && userId > 0;
    }

    // Authorized actions run after OnTokenValidated has checked the subject.
    public static int UserId(ClaimsPrincipal principal) => TryGetUserId(principal, out var id)
        ? id : throw new InvalidOperationException("Validated identity is required.");
}
