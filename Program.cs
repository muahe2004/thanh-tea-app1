using Npgsql;
using System.Security.Cryptography;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    try
    {
        // PostgreSQL
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        Console.WriteLine("PostgreSQL connection test succeeded.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"PostgreSQL connection test failed: {ex.Message}");
    }
}
else
{
    Console.WriteLine("DefaultConnection is missing.");
}

/*
// MySQL
// using MySqlConnector;
//
// var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
// if (!string.IsNullOrWhiteSpace(connectionString))
// {
//     try
//     {
//         await using var connection = new MySqlConnection(connectionString);
//         await connection.OpenAsync();
//
//         Console.WriteLine("MySQL connection test succeeded.");
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine($"MySQL connection test failed: {ex.Message}");
//     }
// }
// else
// {
//     Console.WriteLine("DefaultConnection is missing.");
// }
*/

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapPost("/api/login", async (HttpContext context) =>
{
    var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Json(new { success = false, message = "DefaultConnection is missing." }, statusCode: 500);
    }

    var request = await JsonSerializer.DeserializeAsync<LoginRequest>(
        context.Request.Body,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    if (request is null ||
        string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.Password) ||
        string.IsNullOrWhiteSpace(request.Role))
    {
        return Results.Json(new { success = false, message = "Email, password and role are required." }, statusCode: 400);
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await using var command = new NpgsqlCommand(
        @"SELECT id, full_name, email, phone_number, role, avatar_url, date_of_birth, gender, address, created_at, updated_at, status, password
          FROM users
          WHERE (email = @login
             OR phone_number = @login)
            AND role = @role
            AND status = TRUE
          LIMIT 1;",
        connection);

    command.Parameters.AddWithValue("login", request.Email);
    command.Parameters.AddWithValue("role", request.Role);

    await using var reader = await command.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
    {
        return Results.Json(new { success = false, message = "Invalid credentials." }, statusCode: 401);
    }

    var user = new UserDto(
        reader.GetString(0),
        reader.GetString(1),
        reader.GetString(2),
        reader.IsDBNull(3) ? null : reader.GetString(3),
        reader.GetString(4),
        reader.IsDBNull(5) ? null : reader.GetString(5),
        reader.IsDBNull(6) ? null : reader.GetDateTime(6),
        reader.IsDBNull(7) ? null : reader.GetString(7),
        reader.IsDBNull(8) ? null : reader.GetString(8),
        reader.GetDateTime(9),
        reader.IsDBNull(10) ? null : reader.GetDateTime(10),
        reader.GetBoolean(11),
        reader.GetString(12));

    if (!PasswordHasher.Verify(request.Password, user.Password))
    {
        return Results.Json(new { success = false, message = "Invalid credentials." }, statusCode: 401);
    }

    context.Response.Cookies.Append(
        "user_session",
        user.Id,
        new CookieOptions
        {
            HttpOnly = false,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

    return Results.Json(new
    {
        success = true,
        message = "Login successful.",
        user = new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.Role,
            user.AvatarUrl,
            user.DateOfBirth,
            user.Gender,
            user.Address,
            user.CreatedAt,
            user.UpdatedAt,
            user.Status
        }
    });
});

app.MapPost("/api/register", async (HttpContext context) =>
{
    var connectionString = app.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        return Results.Json(new { success = false, message = "DefaultConnection is missing." }, statusCode: 500);
    }

    var request = await JsonSerializer.DeserializeAsync<RegisterRequest>(
        context.Request.Body,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    if (request is null ||
        string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.FullName) ||
        string.IsNullOrWhiteSpace(request.PhoneNumber) ||
        string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.Json(new { success = false, message = "Required fields are missing." }, statusCode: 400);
    }

    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();

    await using (var checkCommand = new NpgsqlCommand(
        @"SELECT 1
          FROM users
          WHERE email = @email
          LIMIT 1;",
        connection))
    {
        checkCommand.Parameters.AddWithValue("email", request.Email);
        var exists = await checkCommand.ExecuteScalarAsync();
        if (exists is not null)
        {
            return Results.Json(new { success = false, message = "Email already exists." }, statusCode: 409);
        }
    }

    await using (var phoneCheckCommand = new NpgsqlCommand(
        @"SELECT 1
          FROM users
          WHERE phone_number = @phone_number
          LIMIT 1;",
        connection))
    {
        phoneCheckCommand.Parameters.AddWithValue("phone_number", request.PhoneNumber);
        var phoneExists = await phoneCheckCommand.ExecuteScalarAsync();
        if (phoneExists is not null)
        {
            return Results.Json(new { success = false, message = "Phone number already exists." }, statusCode: 409);
        }
    }

    var now = DateTime.UtcNow;
    var userId = Guid.NewGuid().ToString();
    var passwordHash = PasswordHasher.Hash(request.Password);

    await using (var insertCommand = new NpgsqlCommand(
        @"INSERT INTO users (
            id,
            full_name,
            email,
            phone_number,
            password,
            role,
            avatar_url,
            date_of_birth,
            gender,
            address,
            status,
            created_at,
            updated_at
        ) VALUES (
            @id,
            @full_name,
            @email,
            @phone_number,
            @password,
            @role,
            NULL,
            NULL,
            NULL,
            NULL,
            TRUE,
            @created_at,
            @updated_at
        );",
        connection))
    {
        insertCommand.Parameters.AddWithValue("id", userId);
        insertCommand.Parameters.AddWithValue("full_name", request.FullName);
        insertCommand.Parameters.AddWithValue("email", request.Email);
        insertCommand.Parameters.AddWithValue("phone_number", request.PhoneNumber);
        insertCommand.Parameters.AddWithValue("password", passwordHash);
        insertCommand.Parameters.AddWithValue("role", "student");
        insertCommand.Parameters.AddWithValue("created_at", now);
        insertCommand.Parameters.AddWithValue("updated_at", now);

        await insertCommand.ExecuteNonQueryAsync();
    }

    return Results.Json(new
    {
        success = true,
        message = "Register successful."
    });
});

// Map Razor Pages (đây là dòng chính)
app.MapRazorPages();

app.Run();

record LoginRequest(string Email, string Password, string Role);

record RegisterRequest(string Email, string FullName, string PhoneNumber, string Password);

record UserDto(
    string Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string Role,
    string? AvatarUrl,
    DateTime? DateOfBirth,
    string? Gender,
    string? Address,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool Status,
    string Password);

static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        return $"pbkdf2_sha256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != "pbkdf2_sha256")
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expectedKey = Convert.FromBase64String(parts[3]);

        var actualKey = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedKey.Length);

        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
