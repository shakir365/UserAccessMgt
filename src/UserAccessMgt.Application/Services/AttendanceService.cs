using UserAccessMgt.Application.DTOs.Attendance;
using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;
    private static readonly string[] ValidStatuses = ["Present", "Absent", "Late", "OnLeave"];
    private static readonly TimeZoneInfo _bdTimeZone = TimeZoneInfo.FindSystemTimeZoneById(
        TryGetTimeZoneId("Bangladesh Standard Time", "Asia/Dhaka"));

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

    private static DateTime BangladeshNow =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _bdTimeZone);

    public AttendanceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<AttendanceDto>> CreateAsync(CreateAttendanceRequest request, int submittedByUserId)
    {
        var user = await _unitOfWork.Repository<User>().GetByIdAsync(request.UserId);
        if (user is null)
            return ApiResponse<AttendanceDto>.Fail("User not found", "NOT_FOUND");

        var institute = await _unitOfWork.Repository<Institute>().GetByIdAsync(request.InstituteId);
        if (institute is null)
            return ApiResponse<AttendanceDto>.Fail("Institute not found", "INSTITUTE_NOT_FOUND");

        if (user.InstituteId != request.InstituteId)
            return ApiResponse<AttendanceDto>.Fail("User does not belong to this institute", "INSTITUTE_MISMATCH");

        if (!ValidStatuses.Contains(request.Status))
            return ApiResponse<AttendanceDto>.Fail("Invalid attendance status", "INVALID_STATUS");

        var attendanceDate = request.Date.Date;
        var existing = await _unitOfWork.Repository<Attendance>()
            .FirstOrDefaultAsync(a => a.UserId == request.UserId && a.Date >= attendanceDate && a.Date < attendanceDate.AddDays(1));
        if (existing is not null)
        {
            existing.CheckOutTime = request.CheckOutTime ?? BangladeshNow;
            existing.CheckOutLatitude = request.CheckOutLatitude;
            existing.CheckOutLongitude = request.CheckOutLongitude;
            _unitOfWork.Repository<Attendance>().Update(existing);
            await _unitOfWork.SaveChangesAsync();
            return ApiResponse<AttendanceDto>.Ok(GetDto(existing.Id), "Check-out updated successfully");
        }

        var now = BangladeshNow;
        var attendance = new Attendance
        {
            UserId = request.UserId,
            Date = attendanceDate,
            CheckInTime = request.CheckInTime ?? now,
            CheckInLatitude = request.CheckInLatitude,
            CheckInLongitude = request.CheckInLongitude,
            CheckOutTime = request.CheckOutTime,
            CheckOutLatitude = request.CheckOutLatitude,
            CheckOutLongitude = request.CheckOutLongitude,
            Status = request.Status,
            Notes = request.Notes,
            InstituteId = request.InstituteId,
            SubmittedByUserId = submittedByUserId,
            CreatedAt = now
        };

        await _unitOfWork.Repository<Attendance>().AddAsync(attendance);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<AttendanceDto>.Ok(GetDto(attendance.Id), "Attendance recorded successfully");
    }

    public Task<ApiResponse<AttendanceDto>> GetByIdAsync(int id)
    {
        var attendance = _unitOfWork.Repository<Attendance>()
            .Query()
            .Where(a => a.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefault();
        if (attendance is null)
            return Task.FromResult(ApiResponse<AttendanceDto>.Fail("Attendance not found", "NOT_FOUND"));

        return Task.FromResult(ApiResponse<AttendanceDto>.Ok(attendance));
    }

    public Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByUserAsync(int userId)
    {
        var records = _unitOfWork.Repository<Attendance>()
            .Query()
            .Where(a => a.UserId == userId)
            .Select(ProjectToDto)
            .ToList();
        return Task.FromResult(ApiResponse<IEnumerable<AttendanceDto>>.Ok(records));
    }

    public Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByDateAsync(DateTime date)
    {
        var attendanceDate = date.Date;
        var records = _unitOfWork.Repository<Attendance>()
            .Query()
            .Where(a => a.Date >= attendanceDate && a.Date < attendanceDate.AddDays(1))
            .Select(ProjectToDto)
            .ToList();
        return Task.FromResult(ApiResponse<IEnumerable<AttendanceDto>>.Ok(records));
    }

    public Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByInstituteAsync(int instituteId)
    {
        var records = _unitOfWork.Repository<Attendance>()
            .Query()
            .Where(a => a.InstituteId == instituteId)
            .Select(ProjectToDto)
            .ToList();
        return Task.FromResult(ApiResponse<IEnumerable<AttendanceDto>>.Ok(records));
    }

    public Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByUserAndDateRangeAsync(int userId, DateTime from, DateTime to)
    {
        if (from.Date > to.Date)
            return Task.FromResult(ApiResponse<IEnumerable<AttendanceDto>>.Fail("From date cannot be after to date", "INVALID_DATES"));

        var fromDate = from.Date;
        var toDateExclusive = to.Date.AddDays(1);
        var records = _unitOfWork.Repository<Attendance>()
            .Query()
            .Where(a => a.UserId == userId && a.Date >= fromDate && a.Date < toDateExclusive)
            .Select(ProjectToDto)
            .ToList();
        return Task.FromResult(ApiResponse<IEnumerable<AttendanceDto>>.Ok(records));
    }

    public async Task<ApiResponse<AttendanceDto>> UpdateAsync(int id, UpdateAttendanceRequest request)
    {
        var attendance = await _unitOfWork.Repository<Attendance>().GetByIdAsync(id);
        if (attendance is null)
            return ApiResponse<AttendanceDto>.Fail("Attendance not found", "NOT_FOUND");

        if (request.CheckInTime.HasValue) attendance.CheckInTime = request.CheckInTime;
        if (request.CheckOutTime.HasValue) attendance.CheckOutTime = request.CheckOutTime;
        else if (request.CheckOutLatitude.HasValue || request.CheckOutLongitude.HasValue) attendance.CheckOutTime = BangladeshNow;
        if (request.CheckOutLatitude.HasValue) attendance.CheckOutLatitude = request.CheckOutLatitude;
        if (request.CheckOutLongitude.HasValue) attendance.CheckOutLongitude = request.CheckOutLongitude;
        if (request.CheckInLatitude.HasValue) attendance.CheckInLatitude = request.CheckInLatitude;
        if (request.CheckInLongitude.HasValue) attendance.CheckInLongitude = request.CheckInLongitude;
        if (request.Status is not null)
        {
            if (!ValidStatuses.Contains(request.Status))
                return ApiResponse<AttendanceDto>.Fail("Invalid attendance status", "INVALID_STATUS");

            attendance.Status = request.Status;
        }
        if (request.Notes is not null) attendance.Notes = request.Notes;

        _unitOfWork.Repository<Attendance>().Update(attendance);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<AttendanceDto>.Ok(GetDto(attendance.Id), "Attendance updated successfully");
    }

    public async Task<ApiResponse<string>> DeleteAsync(int id)
    {
        var attendance = await _unitOfWork.Repository<Attendance>().GetByIdAsync(id);
        if (attendance is null)
            return ApiResponse<string>.Fail("Attendance not found", "NOT_FOUND");

        _unitOfWork.Repository<Attendance>().Remove(attendance);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("Attendance deleted successfully");
    }

    private static readonly System.Linq.Expressions.Expression<Func<Attendance, AttendanceDto>> ProjectToDto = attendance => new AttendanceDto
    {
        Id = attendance.Id,
        UserId = attendance.UserId,
        UserName = attendance.User == null ? string.Empty : attendance.User.Username,
        Date = attendance.Date,
        CheckInTime = attendance.CheckInTime,
        CheckInLatitude = attendance.CheckInLatitude,
        CheckInLongitude = attendance.CheckInLongitude,
        CheckOutTime = attendance.CheckOutTime,
        CheckOutLatitude = attendance.CheckOutLatitude,
        CheckOutLongitude = attendance.CheckOutLongitude,
        Status = attendance.Status,
        Notes = attendance.Notes,
        InstituteId = attendance.InstituteId,
        InstituteName = attendance.Institute == null ? string.Empty : attendance.Institute.Name,
        SubmittedByUserId = attendance.SubmittedByUserId,
        SubmittedByUserName = attendance.SubmittedByUser == null ? string.Empty : attendance.SubmittedByUser.Username
    };

    private AttendanceDto GetDto(int id)
        => _unitOfWork.Repository<Attendance>()
            .Query()
            .Where(a => a.Id == id)
            .Select(ProjectToDto)
            .First();
}
