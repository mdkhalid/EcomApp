namespace EcomApi.Services;

public static class LockoutPolicy
{
    public const int MaxFailedAttempts = 5;
    public const int LockoutMinutes = 15;
}
