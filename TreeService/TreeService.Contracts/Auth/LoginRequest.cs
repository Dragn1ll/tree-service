namespace TreeService.Contracts.Auth;

public record LoginRequest(
    string Username, 
    string Password
    );