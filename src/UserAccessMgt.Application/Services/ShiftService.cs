using System.Globalization;
using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Shift;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class ShiftService : IShiftService
{
    private readonly IUnitOfWork _unitOfWork;

    public ShiftService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<ShiftDto>> CreateAsync(CreateShiftRequest request)
    {
        var startTime = ParseShiftTime(request.StartTime);
        if (startTime is null)
            return ApiResponse<ShiftDto>.Fail("StartTime must be a valid time. Example: 10:00 AM", "INVALID_START_TIME");

        var endTime = ParseShiftTime(request.EndTime);
        if (endTime is null)
            return ApiResponse<ShiftDto>.Fail("EndTime must be a valid time. Example: 02:00 PM", "INVALID_END_TIME");

        var shiftCode = request.ShiftCode.Trim();
        var existing = await _unitOfWork.Repository<Shift>()
            .FirstOrDefaultAsync(s => s.ShiftCode == shiftCode);
        if (existing is not null)
            return ApiResponse<ShiftDto>.Fail("Shift code already exists", "SHIFT_EXISTS");

        var shift = new Shift
        {
            ShiftCode = shiftCode,
            ShiftName = request.ShiftName.Trim(),
            StartTime = startTime.Value,
            EndTime = endTime.Value,
            LateAfterMinutes = request.LateAfterMinutes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Shift>().AddAsync(shift);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<ShiftDto>.Ok(GetDto(shift.Id), "Shift created successfully");
    }

    public Task<ApiResponse<ShiftDto>> GetByIdAsync(int id)
    {
        var dto = _unitOfWork.Repository<Shift>().Query()
            .Where(s => s.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefault();
        return Task.FromResult(dto is null
            ? ApiResponse<ShiftDto>.Fail("Shift not found", "NOT_FOUND")
            : ApiResponse<ShiftDto>.Ok(dto));
    }

    public Task<ApiResponse<IEnumerable<ShiftDto>>> GetAllAsync()
        => Task.FromResult(ApiResponse<IEnumerable<ShiftDto>>.Ok(
            _unitOfWork.Repository<Shift>().Query().Select(ProjectToDto).OrderBy(s => s.StartTime).ToList()));

    public async Task<ApiResponse<ShiftDto>> UpdateAsync(int id, UpdateShiftRequest request)
    {
        var shift = await _unitOfWork.Repository<Shift>().GetByIdAsync(id);
        if (shift is null)
            return ApiResponse<ShiftDto>.Fail("Shift not found", "NOT_FOUND");

        if (request.ShiftName is not null)
            shift.ShiftName = request.ShiftName.Trim();
        if (request.StartTime is not null)
        {
            var startTime = ParseShiftTime(request.StartTime);
            if (startTime is null)
                return ApiResponse<ShiftDto>.Fail("StartTime must be a valid time. Example: 10:00 AM", "INVALID_START_TIME");
            shift.StartTime = startTime.Value;
        }
        if (request.EndTime is not null)
        {
            var endTime = ParseShiftTime(request.EndTime);
            if (endTime is null)
                return ApiResponse<ShiftDto>.Fail("EndTime must be a valid time. Example: 02:00 PM", "INVALID_END_TIME");
            shift.EndTime = endTime.Value;
        }
        if (request.LateAfterMinutes.HasValue)
            shift.LateAfterMinutes = request.LateAfterMinutes.Value;
        if (request.IsActive.HasValue)
            shift.IsActive = request.IsActive.Value;
        shift.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Shift>().Update(shift);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<ShiftDto>.Ok(GetDto(shift.Id), "Shift updated successfully");
    }

    public async Task<ApiResponse<string>> DeleteAsync(int id)
    {
        var shift = await _unitOfWork.Repository<Shift>().GetByIdAsync(id);
        if (shift is null)
            return ApiResponse<string>.Fail("Shift not found", "NOT_FOUND");

        _unitOfWork.Repository<Shift>().Remove(shift);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("Shift deleted successfully");
    }

    private static readonly System.Linq.Expressions.Expression<Func<Shift, ShiftDto>> ProjectToDto = shift => new ShiftDto
    {
        Id = shift.Id,
        ShiftCode = shift.ShiftCode,
        ShiftName = shift.ShiftName,
        StartTime = shift.StartTime,
        EndTime = shift.EndTime,
        LateAfterMinutes = shift.LateAfterMinutes,
        IsActive = shift.IsActive,
        CreatedAt = shift.CreatedAt,
        UpdatedAt = shift.UpdatedAt
    };

    private ShiftDto GetDto(int id)
        => _unitOfWork.Repository<Shift>().Query().Where(s => s.Id == id).Select(ProjectToDto).First();

    private static TimeSpan? ParseShiftTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var text = value.Trim().ToUpperInvariant().Replace(".", string.Empty);
        var hasMeridiem = text.EndsWith(" AM", StringComparison.Ordinal) || text.EndsWith(" PM", StringComparison.Ordinal);
        if (hasMeridiem)
        {
            var timePart = text[..^3].Trim();
            var hourPart = timePart.Split(':')[0];
            if (int.TryParse(hourPart, out var hour) && hour > 12)
                text = timePart;
        }

        var formats = new[]
        {
            "h:mm tt", "hh:mm tt", "h:mm:ss tt", "hh:mm:ss tt",
            "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss"
        };

        if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed.TimeOfDay;

        return TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var time) ? time : null;
    }
}
