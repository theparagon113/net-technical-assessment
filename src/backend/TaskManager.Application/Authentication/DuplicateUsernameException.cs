namespace TaskManager.Application.Authentication;

public sealed class DuplicateUsernameException() : Exception("Username is already registered.");
