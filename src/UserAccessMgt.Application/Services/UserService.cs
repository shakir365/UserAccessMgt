using System.Text.RegularExpressions;
using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.User;
using UserAccessMgt.Application.DTOs.UserSupervisor;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class UserService : IUserService
{
    private const int AllDivisionLevelId = 1;
    private const int OwnDivisionLevelId = 2;
    private const int OwnDistrictLevelId = 3;
    private const int OwnThanaLevelId = 4;
    private const int OwnInstituteLevelId = 5;
    private const int OwnDataLevelId = 6;
    private const int OwnDepartmentsLevelId = 7;
    private static readonly TimeZoneInfo _bdTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        TryGetTimeZoneId("Bangladesh Standard Time", "Asia/Dhaka"));

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;

    public UserService(IUnitOfWork unitOfWork, IPasswordService passwordService)
    {
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
    }

    public Task<ApiResponse<UserDto>> GetByIdAsync(int id)
    {
        var user = _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.Id == id)
            .Select(MapToDtoExpression)
            .FirstOrDefault();

        if (user is null)
            return Task.FromResult(ApiResponse<UserDto>.Fail("User not found", "NOT_FOUND"));

        return Task.FromResult(ApiResponse<UserDto>.Ok(user));
    }

    public Task<ApiResponse<IEnumerable<UserDto>>> GetAllAsync(int instituteId)
    {
        var users = _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.InstituteId == instituteId)
            .Select(MapToDtoExpression)
            .ToList();

        return Task.FromResult(ApiResponse<IEnumerable<UserDto>>.Ok(users.AsEnumerable()));
    }

    public Task<ApiResponse<UserDto>> GetByLoginIdAsync(string loginId, int? requesterInstituteId, bool requesterIsSuperAdmin)
    {
        var normalizedLoginId = loginId.Trim();
        var user = _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.LoginID == normalizedLoginId)
            .Select(MapToDtoExpression)
            .FirstOrDefault();

        if (user is null)
            return Task.FromResult(ApiResponse<UserDto>.Fail("User not found", "NOT_FOUND"));

        if (!requesterIsSuperAdmin && user.InstituteId != requesterInstituteId)
        {
            return Task.FromResult(ApiResponse<UserDto>.Fail(
                "You are not eligible to get the user",
                "INSTITUTE_ACCESS_DENIED"));
        }

        return Task.FromResult(ApiResponse<UserDto>.Ok(user));
    }

    public Task<ApiResponse<UserDto>> GetSupervisorByLoginIdAsync(string loginId)
    {
        if (string.IsNullOrWhiteSpace(loginId))
            return Task.FromResult(ApiResponse<UserDto>.Fail("LoginID is required", "LOGIN_ID_REQUIRED"));

        var normalizedLoginId = loginId.Trim();
        var user = _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.LoginID == normalizedLoginId)
            .Select(u => new
            {
                u.Id,
                u.InstituteId,
                u.RoleId,
                RoleName = u.Role.Name,
                u.Role.UserDataViewLevelID,
                ThanaId = u.Institute.ThanaId,
                DistrictId = u.Institute.Thana == null ? null : (int?)u.Institute.Thana.DistrictId
            })
            .FirstOrDefault();

        if (user is null)
            return Task.FromResult(ApiResponse<UserDto>.Fail("User not found", "NOT_FOUND"));

        var directSupervisor = GetActiveDirectSupervisor(user.Id);
        if (directSupervisor is not null)
            return Task.FromResult(ApiResponse<UserDto>.Ok(directSupervisor));

        var userDataViewLevelId = GetDataViewLevelId(user.UserDataViewLevelID, user.RoleName);
        var supervisorLevelId = GetSupervisorLevelId(userDataViewLevelId);
        if (!supervisorLevelId.HasValue)
        {
            return Task.FromResult(ApiResponse<UserDto>.Fail(
                "Supervisor is not configured for this user data view level",
                "SUPERVISOR_LEVEL_NOT_CONFIGURED"));
        }

        var missingHierarchyError = GetMissingHierarchyError(
            supervisorLevelId.Value,
            user.ThanaId,
            user.DistrictId);

        if (missingHierarchyError is not null)
            return Task.FromResult(ApiResponse<UserDto>.Fail(missingHierarchyError.Value.Message, missingHierarchyError.Value.ErrorCode));

        var supervisorRoleIds = _unitOfWork.Repository<Role>()
            .Query()
            .ToList()
            .Where(r => GetDataViewLevelId(r.UserDataViewLevelID, r.Name) == supervisorLevelId.Value)
            .Select(r => r.Id)
            .ToList();

        if (supervisorRoleIds.Count == 0)
        {
            return Task.FromResult(ApiResponse<UserDto>.Fail(
                "Supervisor role is not configured",
                "SUPERVISOR_ROLE_NOT_CONFIGURED"));
        }

        var supervisors = GetSupervisorQuery(
            user.Id,
            user.InstituteId,
            user.ThanaId,
            user.DistrictId,
            supervisorLevelId.Value,
            supervisorRoleIds);

        var supervisor = supervisors
            .OrderBy(u => u.Id)
            .Select(MapToDtoExpression)
            .FirstOrDefault();

        if (supervisor is null)
            return Task.FromResult(ApiResponse<UserDto>.Fail("Supervisor not found", "SUPERVISOR_NOT_FOUND"));

        return Task.FromResult(ApiResponse<UserDto>.Ok(supervisor));
    }

    public async Task<ApiResponse<UserDirectSupervisorDto>> UserSupervisorSetAsync(UserSupervisorSetRequest request, int? createByUserId)
    {
        if (request.UserID == request.Supervisor_UserID)
        {
            return ApiResponse<UserDirectSupervisorDto>.Fail(
                "User and supervisor cannot be same",
                "SAME_USER_SUPERVISOR");
        }

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.UserID);
        if (user is null)
            return ApiResponse<UserDirectSupervisorDto>.Fail("User not found", "USER_NOT_FOUND");

        var supervisor = await _unitOfWork.Repository<User>().GetByIdAsync(request.Supervisor_UserID);
        if (supervisor is null)
            return ApiResponse<UserDirectSupervisorDto>.Fail("Supervisor user not found", "SUPERVISOR_NOT_FOUND");

        var activeDateFrom = request.ActiveDateFrom.Date;
        var expireDate = request.ExpireDate?.Date;

        if (expireDate.HasValue && expireDate.Value < activeDateFrom)
        {
            return ApiResponse<UserDirectSupervisorDto>.Fail(
                "ExpireDate cannot be earlier than ActiveDateFrom",
                "INVALID_EXPIRE_DATE");
        }

        var userDirectSupervisor = new UserDirectSupervisor
        {
            UserID = request.UserID,
            Supervisor_UserID = request.Supervisor_UserID,
            ActiveDateFrom = activeDateFrom,
            ExpireDate = expireDate,
            CreateDate = GetBangladeshNow(),
            CreateBy_UserID = createByUserId
        };

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var existingRecords = _unitOfWork.Repository<UserDirectSupervisor>()
                .Query()
                .Where(s => s.UserID == request.UserID)
                .ToList();

            foreach (var existingRecord in existingRecords)
            {
                _unitOfWork.Repository<UserDirectSupervisor>().Remove(existingRecord);
            }

            if (existingRecords.Count > 0)
                await _unitOfWork.SaveChangesAsync();

            await _unitOfWork.Repository<UserDirectSupervisor>().AddAsync(userDirectSupervisor);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }

        return ApiResponse<UserDirectSupervisorDto>.Ok(
            GetUserDirectSupervisorDto(userDirectSupervisor.Id),
            "User supervisor set successfully");
    }

    public Task<ApiResponse<UserDirectSupervisorLookupDto>> GetActiveDirectSupervisorByLoginIdAsync(string loginId)
    {
        if (string.IsNullOrWhiteSpace(loginId))
        {
            return Task.FromResult(ApiResponse<UserDirectSupervisorLookupDto>.Fail(
                "LoginID is required",
                "LOGIN_ID_REQUIRED"));
        }

        var normalizedLoginId = loginId.Trim();
        var today = GetBangladeshNow().Date;
        var directSupervisor = _unitOfWork.Repository<UserDirectSupervisor>()
            .Query()
            .Where(s => s.User != null
                && s.User.LoginID == normalizedLoginId
                && s.ActiveDateFrom <= today
                && (!s.ExpireDate.HasValue || s.ExpireDate.Value >= today))
            .OrderBy(s => s.Id)
            .Select(MapToUserDirectSupervisorDtoExpression)
            .FirstOrDefault();

        if (directSupervisor is null)
        {
            return Task.FromResult(ApiResponse<UserDirectSupervisorLookupDto>.Fail(
                "Active direct supervisor configuration not found",
                "DIRECT_SUPERVISOR_NOT_FOUND"));
        }

        var supervisor = _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.Id == directSupervisor.Supervisor_UserID && u.IsActive)
            .Select(MapToDtoExpression)
            .FirstOrDefault();

        if (supervisor is null)
        {
            return Task.FromResult(ApiResponse<UserDirectSupervisorLookupDto>.Fail(
                "Configured supervisor user not found",
                "SUPERVISOR_NOT_FOUND"));
        }

        return Task.FromResult(ApiResponse<UserDirectSupervisorLookupDto>.Ok(
            new UserDirectSupervisorLookupDto
            {
                Configuration = directSupervisor,
                Supervisor = supervisor
            }));
    }

    public async Task<ApiResponse<string>> DeleteUserSupervisorSetAsync(int userId)
    {
        var existingRecords = _unitOfWork.Repository<UserDirectSupervisor>()
            .Query()
            .Where(s => s.UserID == userId)
            .ToList();

        if (existingRecords.Count == 0)
        {
            return ApiResponse<string>.Fail(
                "Direct supervisor configuration not found",
                "DIRECT_SUPERVISOR_NOT_FOUND");
        }

        foreach (var existingRecord in existingRecords)
        {
            _unitOfWork.Repository<UserDirectSupervisor>().Remove(existingRecord);
        }

        await _unitOfWork.SaveChangesAsync();
        return ApiResponse<string>.Ok("Deleted", "Direct supervisor configuration removed successfully");
    }

    public Task<ApiResponse<IEnumerable<UserRoleDto>>> GetRolesAsync()
    {
        var roles = _unitOfWork.Repository<Role>()
            .Query()
            .OrderBy(r => r.Name)
            .Select(r => new UserRoleDto
            {
                Id = r.Id,
                Name = r.Name,
                UserDataViewLevelID = r.UserDataViewLevelID,
                DataViewLevel = r.UserDataViewLevel == null ? null : r.UserDataViewLevel.DataViewLevel,
                RelatedRoleInfo = r.UserDataViewLevel == null ? null : r.UserDataViewLevel.RelatedRoleInfo
            })
            .ToList()
            .AsEnumerable();

        return Task.FromResult(ApiResponse<IEnumerable<UserRoleDto>>.Ok(roles));
    }

    public async Task<ApiResponse<UserDto>> UpdateAsync(int id, UpdateUserRequest request)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user is null)
            return ApiResponse<UserDto>.Fail("User not found", "NOT_FOUND");

        if (request.FirstName is not null) user.FirstName = request.FirstName;
        if (request.LastName is not null) user.LastName = request.LastName;
        if (request.MobileNumber is not null)
        {
            var mobileNumber = request.MobileNumber.Trim();
            if (!Regex.IsMatch(mobileNumber, @"^01[3-9]\d{8}$"))
            {
                return ApiResponse<UserDto>.Fail("MobileNumber must be a valid BD mobile number", "INVALID_MOBILE_NUMBER");
            }

            user.MobileNumber = mobileNumber;
        }
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        if (request.InstituteId.HasValue)
        {
            var institute = await _unitOfWork.Repository<Institute>().GetByIdAsync(request.InstituteId.Value);
            if (institute is null)
                return ApiResponse<UserDto>.Fail("Invalid institute", "INVALID_INSTITUTE");

            user.InstituteId = institute.Id;
        }
        if (request.RoleId.HasValue)
        {
            var role = await _unitOfWork.Repository<Role>().GetByIdAsync(request.RoleId.Value);
            if (role is null)
                return ApiResponse<UserDto>.Fail("Invalid role", "INVALID_ROLE");

            user.RoleId = role.Id;
        }
        if (request.GradeId.HasValue)
        {
            var grade = await _unitOfWork.Repository<Grade>().GetByIdAsync(request.GradeId.Value);
            if (grade is null)
                return ApiResponse<UserDto>.Fail("Invalid grade", "INVALID_GRADE");

            user.GradeId = grade.Id;
        }
        if (request.DesignationId.HasValue)
        {
            var designation = await _unitOfWork.Repository<Designation>().GetByIdAsync(request.DesignationId.Value);
            if (designation is null)
                return ApiResponse<UserDto>.Fail("Invalid designation", "INVALID_DESIGNATION");

            user.DesignationId = designation.Id;
        }

        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<UserDto>.Ok(await GetDtoAsync(user.Id), "User updated successfully");
    }

    public async Task<ApiResponse<string>> ChangeMyPasswordAsync(int userId, ChangeMyPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            return ApiResponse<string>.Fail("New password and confirm password do not match", "PASSWORD_MISMATCH");

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(userId);
        if (user is null)
            return ApiResponse<string>.Fail("User not found", "NOT_FOUND");

        if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            return ApiResponse<string>.Fail("Current password is incorrect", "INVALID_CURRENT_PASSWORD");

        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("Password changed successfully");
    }

    public async Task<ApiResponse<string>> ChangeUserPasswordAsync(int id, ChangeUserPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            return ApiResponse<string>.Fail("New password and confirm password do not match", "PASSWORD_MISMATCH");

        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user is null)
            return ApiResponse<string>.Fail("User not found", "NOT_FOUND");

        user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("Password changed successfully");
    }

    public async Task<ApiResponse<string>> DeactivateAsync(int id)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user is null)
            return ApiResponse<string>.Fail("User not found", "NOT_FOUND");

        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("User deactivated successfully");
    }

    public async Task<ApiResponse<string>> ActivateAsync(int id)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(id);
        if (user is null)
            return ApiResponse<string>.Fail("User not found", "NOT_FOUND");

        user.IsActive = true;
        user.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Repository<User>().Update(user);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("User activated successfully");
    }

    private static readonly System.Linq.Expressions.Expression<Func<User, UserDto>> MapToDtoExpression = user => new UserDto
    {
        Id = user.Id,
        LoginID = user.LoginID,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        MobileNumber = user.MobileNumber,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt,
        InstituteId = user.InstituteId,
        InstituteName = user.Institute == null ? string.Empty : user.Institute.InstituteNameEN,
        RoleId = user.RoleId,
        RoleName = user.Role == null ? string.Empty : user.Role.Name,
        UserDataViewLevelID = user.Role == null ? null : user.Role.UserDataViewLevelID,
        DataViewLevel = user.Role == null || user.Role.UserDataViewLevel == null ? null : user.Role.UserDataViewLevel.DataViewLevel,
        RelatedRoleInfo = user.Role == null || user.Role.UserDataViewLevel == null ? null : user.Role.UserDataViewLevel.RelatedRoleInfo,
        GradeId = user.GradeId,
        GradeName = user.Grade == null ? null : user.Grade.GradeNameEN,
        DesignationId = user.DesignationId,
        DesignationName = user.Designation == null ? null : user.Designation.DesignationNameEN
    };

    private static readonly System.Linq.Expressions.Expression<Func<UserDirectSupervisor, UserDirectSupervisorDto>> MapToUserDirectSupervisorDtoExpression = supervisor => new UserDirectSupervisorDto
    {
        Id = supervisor.Id,
        UserID = supervisor.UserID,
        UserLoginID = supervisor.User == null ? string.Empty : supervisor.User.LoginID,
        Supervisor_UserID = supervisor.Supervisor_UserID,
        SupervisorLoginID = supervisor.SupervisorUser == null ? string.Empty : supervisor.SupervisorUser.LoginID,
        ActiveDateFrom = supervisor.ActiveDateFrom,
        ExpireDate = supervisor.ExpireDate,
        CreateDate = supervisor.CreateDate,
        CreateBy_UserID = supervisor.CreateBy_UserID,
        CreateByLoginID = supervisor.CreateByUser == null ? null : supervisor.CreateByUser.LoginID
    };

    private UserDto? GetActiveDirectSupervisor(int userId)
    {
        var today = GetBangladeshNow().Date;
        var supervisorUserId = _unitOfWork.Repository<UserDirectSupervisor>()
            .Query()
            .Where(s => s.UserID == userId
                && s.ActiveDateFrom <= today
                && (!s.ExpireDate.HasValue || s.ExpireDate.Value >= today))
            .Select(s => (int?)s.Supervisor_UserID)
            .FirstOrDefault();

        if (!supervisorUserId.HasValue)
            return null;

        return _unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.Id == supervisorUserId.Value && u.IsActive)
            .Select(MapToDtoExpression)
            .FirstOrDefault();
    }

    private UserDirectSupervisorDto GetUserDirectSupervisorDto(int id)
        => _unitOfWork.Repository<UserDirectSupervisor>()
            .Query()
            .Where(s => s.Id == id)
            .Select(MapToUserDirectSupervisorDtoExpression)
            .First();

    private static int? GetSupervisorLevelId(int? userDataViewLevelId)
        => userDataViewLevelId switch
        {
            OwnDivisionLevelId => AllDivisionLevelId,
            OwnDistrictLevelId => OwnDivisionLevelId,
            OwnThanaLevelId => OwnDistrictLevelId,
            OwnInstituteLevelId => OwnThanaLevelId,
            OwnDataLevelId => OwnInstituteLevelId,
            OwnDepartmentsLevelId => OwnInstituteLevelId,
            _ => null
        };

    private static int? GetDataViewLevelId(int? userDataViewLevelId, string? roleName)
    {
        if (userDataViewLevelId.HasValue)
            return userDataViewLevelId.Value;

        return roleName?.Trim().Replace(" ", string.Empty).ToUpperInvariant() switch
        {
            "SUPERADMIN" => AllDivisionLevelId,
            "DIVISIONALADMIN" => OwnDivisionLevelId,
            "DISTRICTADMIN" => OwnDistrictLevelId,
            "DISRTICTADMIN" => OwnDistrictLevelId,
            "THANAADMIN" => OwnThanaLevelId,
            "INSTITUTEADMIN" => OwnInstituteLevelId,
            "USER" => OwnDataLevelId,
            "DEPARTMENTALADMIN" => OwnDepartmentsLevelId,
            _ => null
        };
    }

    private static (string Message, string ErrorCode)? GetMissingHierarchyError(
        int supervisorLevelId,
        int? thanaId,
        int? districtId)
        => supervisorLevelId switch
        {
            OwnThanaLevelId when !thanaId.HasValue => ("User institute is not assigned to a thana", "THANA_NOT_CONFIGURED"),
            OwnDistrictLevelId when !districtId.HasValue => ("User thana is not assigned to a district", "DISTRICT_NOT_CONFIGURED"),
            OwnDivisionLevelId when !districtId.HasValue => ("User thana is not assigned to a district", "DISTRICT_NOT_CONFIGURED"),
            _ => null
        };

    private IQueryable<User> GetSupervisorQuery(
        int userId,
        int instituteId,
        int? thanaId,
        int? districtId,
        int supervisorLevelId,
        IReadOnlyCollection<int> supervisorRoleIds)
    {
        var users = _unitOfWork.Repository<User>().Query();
        var institutes = _unitOfWork.Repository<Institute>().Query();
        var roles = _unitOfWork.Repository<Role>().Query();
        var thanas = _unitOfWork.Repository<Thana>().Query();

        return supervisorLevelId switch
        {
            OwnInstituteLevelId => users
                .Where(u => u.Id != userId
                    && u.IsActive
                    && supervisorRoleIds.Contains(u.RoleId)
                    && u.InstituteId == instituteId),

            OwnThanaLevelId =>
                from supervisor in users
                join institute in institutes on supervisor.InstituteId equals institute.Id
                join role in roles on supervisor.RoleId equals role.Id
                join thana in thanas on institute.ThanaId equals thana.ThanaId
                where supervisor.Id != userId
                    && supervisor.IsActive
                    && supervisorRoleIds.Contains(role.Id)
                    && thana.ThanaId == thanaId
                select supervisor,

            OwnDistrictLevelId =>
                from supervisor in users
                join institute in institutes on supervisor.InstituteId equals institute.Id
                join role in roles on supervisor.RoleId equals role.Id
                join thana in thanas on institute.ThanaId equals thana.ThanaId
                where supervisor.Id != userId
                    && supervisor.IsActive
                    && supervisorRoleIds.Contains(role.Id)
                    && thana.DistrictId == districtId
                select supervisor,

            OwnDivisionLevelId =>
                from supervisor in users
                join institute in institutes on supervisor.InstituteId equals institute.Id
                join role in roles on supervisor.RoleId equals role.Id
                join thana in thanas on institute.ThanaId equals thana.ThanaId
                where supervisor.Id != userId
                    && supervisor.IsActive
                    && supervisorRoleIds.Contains(role.Id)
                    && thana.DistrictId == districtId
                select supervisor,

            _ => users
                .Where(u => u.Id != userId
                    && u.IsActive
                    && supervisorRoleIds.Contains(u.RoleId))
        };
    }

    private static string TryGetTimeZoneId(string windowsId, string ianaId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId) is not null ? windowsId : ianaId;
        }
        catch
        {
            return ianaId;
        }
    }

    private static DateTime GetBangladeshNow()
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _bdTimeZone);

    private Task<UserDto> GetDtoAsync(int id)
        => Task.FromResult(_unitOfWork.Repository<User>()
            .Query()
            .Where(u => u.Id == id)
            .Select(MapToDtoExpression)
            .First());
}
