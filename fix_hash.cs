using System;

// Generate a proper BCrypt hash for admin
var password = "admin123";
var hash = BCrypt.Net.BCrypt.HashPassword(password);

Console.WriteLine($"Generated hash: {hash}");
Console.WriteLine($"Hash length: {hash.Length}");
Console.WriteLine($"Verification: {BCrypt.Net.BCrypt.Verify(password, hash)}");
Console.WriteLine();

// Prepare SQL with proper escaping
Console.WriteLine("SQL Update command:");
Console.WriteLine($"UPDATE [auth].[Users] SET PasswordHash = '{hash}' WHERE Username = 'admin';");