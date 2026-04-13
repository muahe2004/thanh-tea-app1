using Npgsql;

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

// Map Razor Pages (đây là dòng chính)
app.MapRazorPages();

app.Run();
