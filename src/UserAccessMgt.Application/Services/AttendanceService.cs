using UserAccessMgt.Application.DTOs.Attendance;
using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;
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
        TimeZoneInfo.ConvertTimeFromUtc(BangladeshNow, _bdTimeZone);

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

        var existing = await _unitOfWork.Repository<Attendance>()
            .FirstOrDefaultAsync(a => a.UserId == request.UserId && a.Date.Date == request.Date.Date);
        if (existing is not null)
        {
            existing.CheckOutTime = BangladeshNow;
            existing.CheckOutLatitude = request.CheckOutLatitude;
            existing.CheckOutLongitude = request.CheckOutLongitude;
            _unitOfWork.Repository<Attendance>().Update(existing);
            await _unitOfWork.SaveChangesAsync();
            return ApiResponse<AttendanceDto>.Ok(MapToDto(existing), "Check-out updated successfully");
        }

        var now = BangladeshNow;
        var attendance = new Attendance
        {
            UserId = request.UserId,
            Date = request.Date.Date,
            CheckInTime = request.CheckInTime ?? now,
            CheckInLatitude = request.CheckInLatitude,
            CheckInLongitude = request.CheckInLongitude,
            CheckOutTime = request.CheckOutTime ?? now,
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

        return ApiResponse<AttendanceDto>.Ok(MapToDto(attendance), "Attendance recorded successfully");
    }

    public async Task<ApiResponse<AttendanceDto>> GetByIdAsync(int id)
    {
        var attendance = await _unitOfWork.Repository<Attendance>().GetByIdAsync(id);
        if (attendance is null)
            return ApiResponse<AttendanceDto>.Fail("Attendance not found", "NOT_FOUND");

        return ApiResponse<AttendanceDto>.Ok(MapToDto(attendance));
    }

    public async Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByUserAsync(int userId)
    {
        var records = await _unitOfWork.Repository<Attendance>()
            .FindAsync(a => a.UserId == userId);
        return ApiResponse<IEnumerable<AttendanceDto>>.Ok(records.Select(MapToDto));
    }

    public async Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByDateAsync(DateTime date)
    {
        var records = await _unitOfWork.Repository<Attendance>()
            .FindAsync(a => a.Date.Date == date.Date);
        return ApiResponse<IEnumerable<AttendanceDto>>.Ok(records.Select(MapToDto));
    }

    public async Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByInstituteAsync(int instituteId)
    {
        var records = await _unitOfWork.Repository<Attendance>()
            .FindAsync(a => a.InstituteId == instituteId);
        return ApiResponse<IEnumerable<AttendanceDto>>.Ok(records.Select(MapToDto));
    }

    public async Task<ApiResponse<IEnumerable<AttendanceDto>>> GetByUserAndDateRangeAsync(int userId, DateTime from, DateTime to)
    {
        var records = await _unitOfWork.Repository<Attendance>()
            .FindAsync(a => a.UserId == userId && a.Date >= from.Date && a.Date <= to.Date);
        return ApiResponse<IEnumerable<AttendanceDto>>.Ok(records.Select(MapToDto));
    }

    public async Task<ApiResponse<AttendanceDto>> UpdateAsync(int id, UpdateAttendanceRequest request)
    {
        var attendance = await _unitOfWork.Repository<Attendance>().GetByIdAsync(id);
        if (attendance is null)
            return ApiResponse<AttendanceDto>.Fail("Attendance not found", "NOT_FOUND");

        attendance.CheckOutTime = BangladeshNow;
        if (request.CheckOutLatitude.HasValue) attendance.CheckOutLatitude = request.CheckOutLatitude;
        if (request.CheckOutLongitude.HasValue) attendance.CheckOutLongitude = request.CheckOutLongitude;
        if (request.CheckInLatitude.HasValue) attendance.CheckInLatitude = request.CheckInLatitude;
        if (request.CheckInLongitude.HasValue) attendance.CheckInLongitude = request.CheckInLongitude;
        if (request.Status is not null) attendance.Status = request.Status;
        if (request.Notes is not null) attendance.Notes = request.Notes;

        _unitOfWork.Repository<Attendance>().Update(attendance);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<AttendanceDto>.Ok(MapToDto(attendance), "Check-out updated successfully");
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

    private static AttendanceDto MapToDto(Attendance attendance) => new()
    {
        Id = attendance.Id,
        UserId = attendance.UserId,
        UserName = attendance.User?.Username ?? string.Empty,
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
        InstituteName = attendance.Institute?.Name ?? string.Empty,
        SubmittedByUserId = attendance.SubmittedByUserId,
        SubmittedByUserName = attendance.SubmittedByUser?.Username ?? string.Empty
    };
}
