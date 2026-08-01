using System.Data;
using Microsoft.Data.SqlClient;
using MyApp.Api.Data;
using MyApp.Api.DTOs;
using MyApp.Api.Repository.Interfaces;

namespace MyApp.Api.Repository.Implementations;

public class ProfileRepository : IProfileRepository
{
    private readonly SqlDataAccess _db;

    public ProfileRepository(SqlDataAccess db)
    {
        _db = db;
    }

    public Task<ProfileDto?> GetProfileAsync(int userId) =>
        _db.ExecuteReaderSingleAsync("dbo.sp_GetProfile",
            cmd => SqlDataAccess.AddParam(cmd, "@UserId", userId),
            MapProfile);

    public Task UpdateProfileAsync(int userId, UpdateProfileDto request, int modifiedBy) =>
        _db.ExecuteNonQueryAsync("dbo.sp_UpdateProfile", cmd =>
        {
            SqlDataAccess.AddParam(cmd, "@UserId", userId);
            SqlDataAccess.AddParam(cmd, "@FullName", request.FullName);
            SqlDataAccess.AddParam(cmd, "@Email", request.Email);
            SqlDataAccess.AddParam(cmd, "@Phone", request.Phone);
            SqlDataAccess.AddParam(cmd, "@Designation", request.Designation);
            SqlDataAccess.AddParam(cmd, "@Department", request.Department);
            SqlDataAccess.AddParam(cmd, "@ProfilePicturePath", request.ProfilePicturePath);
            SqlDataAccess.AddParam(cmd, "@ModifiedBy", modifiedBy);
        });

    private static ProfileDto MapProfile(SqlDataReader reader) => new()
    {
        UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
        EmployeeNumber = reader.GetNullableString("EmployeeNumber"),
        Username = reader.GetString(reader.GetOrdinal("Username")),
        FullName = reader.GetString(reader.GetOrdinal("FullName")),
        Email = reader.GetString(reader.GetOrdinal("Email")),
        Phone = reader.GetNullableString("Phone"),
        Designation = reader.GetNullableString("Designation"),
        Department = reader.GetNullableString("Department"),
        ProfilePicturePath = reader.GetNullableString("ProfilePicturePath"),
        RoleId = reader.GetInt32(reader.GetOrdinal("RoleId")),
        RoleName = reader.GetString(reader.GetOrdinal("RoleName")),
        LastLogin = reader.GetNullableDateTime("LastLogin"),
        Theme = reader.GetNullableString("Theme") ?? "light",
        SidebarCollapsed = reader.GetBoolean(reader.GetOrdinal("SidebarCollapsed")),
        Language = reader.GetNullableString("Language") ?? "en",
        DashboardLayout = reader.GetNullableString("DashboardLayout"),
        LandingPage = reader.GetNullableString("LandingPage")
    };
}
