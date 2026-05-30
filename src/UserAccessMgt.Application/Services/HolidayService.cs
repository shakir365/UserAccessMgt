using UserAccessMgt.Application.DTOs.Common;
using UserAccessMgt.Application.DTOs.Holiday;
using UserAccessMgt.Application.Interfaces;
using UserAccessMgt.Domain.Entities;

namespace UserAccessMgt.Application.Services;

public class HolidayService : IHolidayService
{
    private readonly IUnitOfWork _unitOfWork;

    public HolidayService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ApiResponse<HolidayDto>> CreateAsync(CreateHolidayRequest request)
    {
        var holidayDate = request.HolidayDate.Date;
        var existing = await _unitOfWork.Repository<Holiday>()
            .FirstOrDefaultAsync(h => h.HolidayDate == holidayDate);
        if (existing is not null)
            return ApiResponse<HolidayDto>.Fail("Holiday already exists for this date", "HOLIDAY_EXISTS");

        var holiday = new Holiday
        {
            HolidayName = request.HolidayName.Trim(),
            HolidayDate = holidayDate,
            Description = request.Description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<Holiday>().AddAsync(holiday);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<HolidayDto>.Ok(GetDto(holiday.Id), "Holiday created successfully");
    }

    public Task<ApiResponse<HolidayDto>> GetByIdAsync(int id)
    {
        var dto = _unitOfWork.Repository<Holiday>().Query()
            .Where(h => h.Id == id)
            .Select(ProjectToDto)
            .FirstOrDefault();
        return Task.FromResult(dto is null
            ? ApiResponse<HolidayDto>.Fail("Holiday not found", "NOT_FOUND")
            : ApiResponse<HolidayDto>.Ok(dto));
    }

    public Task<ApiResponse<IEnumerable<HolidayDto>>> GetAllAsync()
        => Task.FromResult(ApiResponse<IEnumerable<HolidayDto>>.Ok(
            _unitOfWork.Repository<Holiday>().Query().Select(ProjectToDto).OrderBy(h => h.HolidayDate).ToList()));

    public async Task<ApiResponse<HolidayDto>> UpdateAsync(int id, UpdateHolidayRequest request)
    {
        var holiday = await _unitOfWork.Repository<Holiday>().GetByIdAsync(id);
        if (holiday is null)
            return ApiResponse<HolidayDto>.Fail("Holiday not found", "NOT_FOUND");

        if (request.HolidayName is not null)
            holiday.HolidayName = request.HolidayName.Trim();
        if (request.HolidayDate.HasValue)
            holiday.HolidayDate = request.HolidayDate.Value.Date;
        if (request.Description is not null)
            holiday.Description = request.Description.Trim();
        if (request.IsActive.HasValue)
            holiday.IsActive = request.IsActive.Value;
        holiday.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Repository<Holiday>().Update(holiday);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<HolidayDto>.Ok(GetDto(holiday.Id), "Holiday updated successfully");
    }

    public async Task<ApiResponse<string>> DeleteAsync(int id)
    {
        var holiday = await _unitOfWork.Repository<Holiday>().GetByIdAsync(id);
        if (holiday is null)
            return ApiResponse<string>.Fail("Holiday not found", "NOT_FOUND");

        _unitOfWork.Repository<Holiday>().Remove(holiday);
        await _unitOfWork.SaveChangesAsync();

        return ApiResponse<string>.Ok("Holiday deleted successfully");
    }

    private static readonly System.Linq.Expressions.Expression<Func<Holiday, HolidayDto>> ProjectToDto = holiday => new HolidayDto
    {
        Id = holiday.Id,
        HolidayName = holiday.HolidayName,
        HolidayDate = holiday.HolidayDate,
        Description = holiday.Description,
        IsActive = holiday.IsActive,
        CreatedAt = holiday.CreatedAt,
        UpdatedAt = holiday.UpdatedAt
    };

    private HolidayDto GetDto(int id)
        => _unitOfWork.Repository<Holiday>().Query().Where(h => h.Id == id).Select(ProjectToDto).First();
}
