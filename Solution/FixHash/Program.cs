using System;

// Generate proper BCrypt hashes for both users
var adminPassword = "admin123";
var systemPassword = "system123"; // Different password for system user

var adminHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
var systemHash = BCrypt.Net.BCrypt.HashPassword(systemPassword);

Console.WriteLine($"Admin hash: {adminHash}");
Console.WriteLine($"Admin length: {adminHash.Length}");
Console.WriteLine($"Admin verification: {BCrypt.Net.BCrypt.Verify(adminPassword, adminHash)}");
Console.WriteLine();

Console.WriteLine($"System hash: {systemHash}");
Console.WriteLine($"System length: {systemHash.Length}");
Console.WriteLine($"System verification: {BCrypt.Net.BCrypt.Verify(systemPassword, systemHash)}");
