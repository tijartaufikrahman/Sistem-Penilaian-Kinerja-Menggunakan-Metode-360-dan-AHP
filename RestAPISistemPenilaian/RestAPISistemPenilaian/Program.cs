using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RestAPISistemPenilaian.Data;
using RestAPISistemPenilaian.Interfaces;
using RestAPISistemPenilaian.Models.Entities;
using RestAPISistemPenilaian.Repositories;
using RestAPISistemPenilaian.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure CORS for Frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// SQL Server Database DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication Configuration
var rawKey = builder.Configuration["Jwt:Key"] ?? "SistemPenilaian360AHP_SuperSecretSecurityKey_2026_SecureAuthentication!";
var keyBytes = Encoding.UTF8.GetBytes(rawKey);
var safeKeyBytes = keyBytes.Length >= 32 ? keyBytes : SHA256.HashData(keyBytes);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(safeKeyBytes),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "SistemPenilaianAPI",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "SistemPenilaianClient",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5)
    };
});

// Register Repositories & Services
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IMenuRoleRepository, MenuRoleRepository>();
builder.Services.AddScoped<ISistemPenilaianService, SistemPenilaianService>();

// Configure Swagger/OpenAPI with unique schema IDs & JWT Bearer Support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName ?? type.Name);
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "Masukkan token JWT dengan format: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Auto-seed default users and initial sample evaluations
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        // Pastikan users terisi
        if (!context.Users.Any())
        {
            var users = new List<User>
            {
                new User
                {
                    KodeKaryawan = "HRD-001",
                    NamaLengkap = "Administrator HRD",
                    Email = "hrd@sistempenilaian.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    RoleId = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    KodeKaryawan = "KRY-001",
                    NamaLengkap = "Budi Santoso",
                    Email = "budi@sistempenilaian.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Karyawan123!"),
                    RoleId = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    KodeKaryawan = "KRY-002",
                    NamaLengkap = "Siti Rahma",
                    Email = "siti@sistempenilaian.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Karyawan123!"),
                    RoleId = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    KodeKaryawan = "KRY-003",
                    NamaLengkap = "Ahmad Fauzi",
                    Email = "ahmad@sistempenilaian.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Karyawan123!"),
                    RoleId = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    KodeKaryawan = "KRY-004",
                    NamaLengkap = "Dewi Lestari",
                    Email = "dewi@sistempenilaian.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Karyawan123!"),
                    RoleId = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    KodeKaryawan = "KRY-005",
                    NamaLengkap = "Rian Pratama",
                    Email = "rian@sistempenilaian.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Karyawan123!"),
                    RoleId = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            };
            context.Users.AddRange(users);
            context.SaveChanges();

            // Seed sample 360 evaluations matching our documented simulation
            var budi = context.Users.First(u => u.Email == "budi@sistempenilaian.com");
            var siti = context.Users.First(u => u.Email == "siti@sistempenilaian.com");
            var ahmad = context.Users.First(u => u.Email == "ahmad@sistempenilaian.com");
            var dewi = context.Users.First(u => u.Email == "dewi@sistempenilaian.com");
            var rian = context.Users.First(u => u.Email == "rian@sistempenilaian.com");
            var hrd = context.Users.First(u => u.Email == "hrd@sistempenilaian.com");

            var sampleEvaluations = new List<Penilaian360>
            {
                // Evaluasi Budi Santoso (Nilai tinggi di waktu & tanggung jawab)
                new Penilaian360 { KodePenilaian = "EVL-2026-001", UserId = budi.Id, PenilaiUserId = hrd.Id, TipePenilai = "Atasan", BobotPersen = 50.0m, kerjasama_tim = 85, ketepatan_waktu = 92, tanggung_jawab = 90, inisiatif = 80, keterlibatan_kerja = 88, interaksi_kolega = 78, tindak_lanjut = 90, StatusPengisian = "Selesai" },
                new Penilaian360 { KodePenilaian = "EVL-2026-002", UserId = budi.Id, PenilaiUserId = siti.Id, TipePenilai = "Rekan", BobotPersen = 30.0m, kerjasama_tim = 85, ketepatan_waktu = 92, tanggung_jawab = 90, inisiatif = 80, keterlibatan_kerja = 88, interaksi_kolega = 78, tindak_lanjut = 90, StatusPengisian = "Selesai" },
                new Penilaian360 { KodePenilaian = "EVL-2026-003", UserId = budi.Id, PenilaiUserId = budi.Id, TipePenilai = "Self", BobotPersen = 20.0m, kerjasama_tim = 85, ketepatan_waktu = 92, tanggung_jawab = 90, inisiatif = 80, keterlibatan_kerja = 88, interaksi_kolega = 78, tindak_lanjut = 90, StatusPengisian = "Selesai" },

                // Evaluasi Siti Rahma
                new Penilaian360 { KodePenilaian = "EVL-2026-004", UserId = siti.Id, PenilaiUserId = hrd.Id, TipePenilai = "Atasan", BobotPersen = 50.0m, kerjasama_tim = 88, ketepatan_waktu = 85, tanggung_jawab = 85, inisiatif = 85, keterlibatan_kerja = 80, interaksi_kolega = 90, tindak_lanjut = 82, StatusPengisian = "Selesai" },
                new Penilaian360 { KodePenilaian = "EVL-2026-005", UserId = siti.Id, PenilaiUserId = budi.Id, TipePenilai = "Rekan", BobotPersen = 30.0m, kerjasama_tim = 88, ketepatan_waktu = 85, tanggung_jawab = 85, inisiatif = 85, keterlibatan_kerja = 80, interaksi_kolega = 90, tindak_lanjut = 82, StatusPengisian = "Selesai" },
                new Penilaian360 { KodePenilaian = "EVL-2026-006", UserId = siti.Id, PenilaiUserId = siti.Id, TipePenilai = "Self", BobotPersen = 20.0m, kerjasama_tim = 88, ketepatan_waktu = 85, tanggung_jawab = 85, inisiatif = 85, keterlibatan_kerja = 80, interaksi_kolega = 90, tindak_lanjut = 82, StatusPengisian = "Selesai" },

                // Evaluasi Ahmad Fauzi (Tinggi di interaksi, sedang di waktu)
                new Penilaian360 { KodePenilaian = "EVL-2026-007", UserId = ahmad.Id, PenilaiUserId = hrd.Id, TipePenilai = "Atasan", BobotPersen = 50.0m, kerjasama_tim = 90, ketepatan_waktu = 70, tanggung_jawab = 75, inisiatif = 78, keterlibatan_kerja = 82, interaksi_kolega = 95, tindak_lanjut = 70, StatusPengisian = "Selesai" },
                new Penilaian360 { KodePenilaian = "EVL-2026-008", UserId = ahmad.Id, PenilaiUserId = rian.Id, TipePenilai = "Rekan", BobotPersen = 30.0m, kerjasama_tim = 90, ketepatan_waktu = 70, tanggung_jawab = 75, inisiatif = 78, keterlibatan_kerja = 82, interaksi_kolega = 95, tindak_lanjut = 70, StatusPengisian = "Selesai" },
                new Penilaian360 { KodePenilaian = "EVL-2026-009", UserId = ahmad.Id, PenilaiUserId = ahmad.Id, TipePenilai = "Self", BobotPersen = 20.0m, kerjasama_tim = 90, ketepatan_waktu = 70, tanggung_jawab = 75, inisiatif = 78, keterlibatan_kerja = 82, interaksi_kolega = 95, tindak_lanjut = 70, StatusPengisian = "Selesai" },

                // Evaluasi Dewi Lestari
                new Penilaian360 { KodePenilaian = "EVL-2026-010", UserId = dewi.Id, PenilaiUserId = hrd.Id, TipePenilai = "Atasan", BobotPersen = 50.0m, kerjasama_tim = 75, ketepatan_waktu = 65, tanggung_jawab = 70, inisiatif = 60, keterlibatan_kerja = 70, interaksi_kolega = 80, tindak_lanjut = 65, StatusPengisian = "Selesai" },

                // Evaluasi Rian Pratama
                new Penilaian360 { KodePenilaian = "EVL-2026-011", UserId = rian.Id, PenilaiUserId = hrd.Id, TipePenilai = "Atasan", BobotPersen = 50.0m, kerjasama_tim = 60, ketepatan_waktu = 50, tanggung_jawab = 55, inisiatif = 50, keterlibatan_kerja = 60, interaksi_kolega = 65, tindak_lanjut = 55, StatusPengisian = "Selesai" }
            };
            context.Penilaian360s.AddRange(sampleEvaluations);
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seeding data: {ex.Message}");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
