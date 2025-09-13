using System;

// Test the actual hash from database
var actualHash = "$2a$11$.gYR1V/nyTae4Q9KDlLgxekEp2yNi9j8Ik/hsixB15GEyAAuomuJa";
var password = "admin123";

Console.WriteLine($"Stored hash: {actualHash}");
Console.WriteLine($"Hash length: {actualHash.Length}");
Console.WriteLine($"Testing password '{password}': {BCrypt.Net.BCrypt.Verify(password, actualHash)}");

// Generate a new one for comparison
var newHash = BCrypt.Net.BCrypt.HashPassword(password);
Console.WriteLine($"\nNew hash: {newHash}");
Console.WriteLine($"New hash length: {newHash.Length}");
Console.WriteLine($"New hash verification: {BCrypt.Net.BCrypt.Verify(password, newHash)}");