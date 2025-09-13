// Simple script to generate BCrypt hashes for test users
// Run with: dotnet script generate_hashes.cs

using System;

class Program
{
    static void Main()
    {
        // For admin123
        var adminSalt = BCrypt.Net.BCrypt.GenerateSalt();
        var adminHash = BCrypt.Net.BCrypt.HashPassword("admin123", adminSalt);
        
        Console.WriteLine($"Admin password 'admin123':");
        Console.WriteLine($"Salt: {adminSalt}");
        Console.WriteLine($"Hash: {adminHash}");
        Console.WriteLine();
        
        // For password123  
        var userSalt = BCrypt.Net.BCrypt.GenerateSalt();
        var userHash = BCrypt.Net.BCrypt.HashPassword("password123", userSalt);
        
        Console.WriteLine($"User password 'password123':");
        Console.WriteLine($"Salt: {userSalt}");
        Console.WriteLine($"Hash: {userHash}");
        Console.WriteLine();
        
        // Verify they work
        Console.WriteLine($"Admin verification: {BCrypt.Net.BCrypt.Verify("admin123", adminHash)}");
        Console.WriteLine($"User verification: {BCrypt.Net.BCrypt.Verify("password123", userHash)}");
    }
}