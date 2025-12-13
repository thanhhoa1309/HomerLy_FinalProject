using Homerly.BusinessObject.Enums;
using Homerly.DataAccess.Entities;
using HomerLy.Business.Interfaces;
using HomerLy.BusinessObject.DTOs.PropertyReportDTOs;
using HomerLy.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HomerLy.Business.Service
{
    public class PropertyReportService : IPropertyReportService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PropertyReportService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PropertyReportResponseDto> CreateReportAsync(Guid tenantId, CreatePropertyReportDto dto)
        {
            // 1. Ki?m tra tenancy có t?n t?i không
            var tenancy = await _unitOfWork.Tenancy.GetByIdAsync(dto.TenancyId);
            if (tenancy == null || tenancy.IsDeleted)
            {
                throw new Exception("Tenancy không t?n t?i");
            }

            // 2. Ki?m tra tenant có ph?i là ng??i thuê trong tenancy này không
            if (tenancy.TenantId != tenantId)
            {
                throw new UnauthorizedAccessException("B?n không có quy?n t?o report cho tenancy này");
            }

            // 3. Ki?m tra property có kh?p v?i tenancy không
            if (tenancy.PropertyId != dto.PropertyId)
            {
                throw new Exception("Property không kh?p v?i tenancy");
            }

            // 4. Ki?m tra property có t?n t?i không
            var property = await _unitOfWork.Property.GetByIdAsync(dto.PropertyId);
            if (property == null || property.IsDeleted)
            {
                throw new Exception("Property không t?n t?i");
            }

            // 5. T?o report
            var report = new PropertyReport
            {
                Id = Guid.NewGuid(),
                PropertyId = dto.PropertyId,
                TenancyId = dto.TenancyId,
                RequestedById = tenantId,
                Title = dto.Title,
                Description = dto.Description,
                Priority = dto.Priority,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = tenantId,
                IsDeleted = false
            };

            await _unitOfWork.PropertyReport.AddAsync(report);
            await _unitOfWork.SaveChangesAsync();

            return await MapToResponseDto(report);
        }

        public async Task<PropertyReportResponseDto?> GetReportByIdAsync(Guid reportId)
        {
            var report = await _unitOfWork.PropertyReport.GetByIdAsync(reportId);

            if (report == null || report.IsDeleted)
            {
                return null;
            }

            return await MapToResponseDto(report);
        }

        public async Task<List<PropertyReportResponseDto>> GetReportsByOwnerAsync(Guid ownerId)
        {
            // L?y t?t c? properties c?a owner
            var ownerProperties = await _unitOfWork.Property.GetQueryable()
                .Where(p => p.OwnerId == ownerId && !p.IsDeleted)
                .Select(p => p.Id)
                .ToListAsync();

            // L?y t?t c? reports c?a các properties này
            var reports = await _unitOfWork.PropertyReport.GetQueryable()
                .Where(r => ownerProperties.Contains(r.PropertyId) && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var responseDtos = new List<PropertyReportResponseDto>();
            foreach (var report in reports)
            {
                responseDtos.Add(await MapToResponseDto(report));
            }

            return responseDtos;
        }

        public async Task<List<PropertyReportResponseDto>> GetReportsByTenantAsync(Guid tenantId)
        {
            var reports = await _unitOfWork.PropertyReport.GetQueryable()
                .Where(r => r.RequestedById == tenantId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var responseDtos = new List<PropertyReportResponseDto>();
            foreach (var report in reports)
            {
                responseDtos.Add(await MapToResponseDto(report));
            }

            return responseDtos;
        }

        public async Task<List<PropertyReportResponseDto>> GetReportsByPropertyAsync(Guid propertyId)
        {
            var reports = await _unitOfWork.PropertyReport.GetQueryable()
                .Where(r => r.PropertyId == propertyId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var responseDtos = new List<PropertyReportResponseDto>();
            foreach (var report in reports)
            {
                responseDtos.Add(await MapToResponseDto(report));
            }

            return responseDtos;
        }

        public async Task<List<PropertyReportResponseDto>> GetReportsByTenancyAsync(Guid tenancyId)
        {
            var reports = await _unitOfWork.PropertyReport.GetQueryable()
                .Where(r => r.TenancyId == tenancyId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var responseDtos = new List<PropertyReportResponseDto>();
            foreach (var report in reports)
            {
                responseDtos.Add(await MapToResponseDto(report));
            }

            return responseDtos;
        }

        public async Task<PropertyReportResponseDto> UpdateReportAsync(Guid reportId, Guid tenantId, UpdatePropertyReportDto dto)
        {
            var report = await _unitOfWork.PropertyReport.GetByIdAsync(reportId);

            if (report == null || report.IsDeleted)
            {
                throw new Exception("Report không t?n t?i");
            }

            // Ki?m tra quy?n: ch? tenant t?o report m?i ???c c?p nh?t
            if (report.RequestedById != tenantId)
            {
                throw new UnauthorizedAccessException("B?n không có quy?n c?p nh?t report này");
            }

            // Không cho phép c?p nh?t n?u ?ã Complete
            if (report.Priority == PriorityStatus.Complete)
            {
                throw new Exception("Không th? c?p nh?t report ?ã hoàn thành");
            }

            // C?p nh?t các tr??ng
            if (!string.IsNullOrWhiteSpace(dto.Title))
                report.Title = dto.Title;

            if (!string.IsNullOrWhiteSpace(dto.Description))
                report.Description = dto.Description;

            if (dto.Priority.HasValue)
                report.Priority = dto.Priority.Value;

            report.UpdatedAt = DateTime.UtcNow;
            report.UpdatedBy = tenantId;

            await _unitOfWork.PropertyReport.Update(report);
            await _unitOfWork.SaveChangesAsync();

            return await MapToResponseDto(report);
        }

        public async Task<PropertyReportResponseDto> UpdateReportStatusAsync(Guid reportId, Guid ownerId, UpdatePropertyReportStatusDto dto)
        {
            var report = await _unitOfWork.PropertyReport.GetByIdAsync(reportId);

            if (report == null || report.IsDeleted)
            {
                throw new Exception("Report không t?n t?i");
            }

            // Ki?m tra quy?n: ch? owner c?a property m?i ???c c?p nh?t status
            var property = await _unitOfWork.Property.GetByIdAsync(report.PropertyId);
            if (property == null || property.IsDeleted)
            {
                throw new Exception("Property không t?n t?i");
            }

            if (property.OwnerId != ownerId)
            {
                throw new UnauthorizedAccessException("B?n không có quy?n c?p nh?t status c?a report này");
            }

            // C?p nh?t priority status
            report.Priority = dto.Priority;
            report.UpdatedAt = DateTime.UtcNow;
            report.UpdatedBy = ownerId;

            await _unitOfWork.PropertyReport.Update(report);
            await _unitOfWork.SaveChangesAsync();

            return await MapToResponseDto(report);
        }

        public async Task<bool> DeleteReportAsync(Guid reportId, Guid userId)
        {
            var report = await _unitOfWork.PropertyReport.GetByIdAsync(reportId);

            if (report == null || report.IsDeleted)
            {
                return false;
            }

            // Ki?m tra quy?n: tenant t?o report ho?c owner c?a property
            var property = await _unitOfWork.Property.GetByIdAsync(report.PropertyId);
            
            if (report.RequestedById != userId && property?.OwnerId != userId)
            {
                throw new UnauthorizedAccessException("B?n không có quy?n xóa report này");
            }

            report.IsDeleted = true;
            report.DeletedAt = DateTime.UtcNow;
            report.DeletedBy = userId;

            await _unitOfWork.PropertyReport.Update(report);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        private async Task<PropertyReportResponseDto> MapToResponseDto(PropertyReport report)
        {
            // L?y thông tin property
            var property = await _unitOfWork.Property.GetByIdAsync(report.PropertyId);

            // L?y thông tin tenant (ng??i request)
            var tenant = await _unitOfWork.Account.GetByIdAsync(report.RequestedById);

            // L?y thông tin owner
            var owner = property != null 
                ? await _unitOfWork.Account.GetByIdAsync(property.OwnerId) 
                : null;

            return new PropertyReportResponseDto
            {
                Id = report.Id,
                PropertyId = report.PropertyId,
                TenancyId = report.TenancyId,
                RequestedById = report.RequestedById,
                Title = report.Title,
                Description = report.Description,
                Priority = report.Priority,
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt,
                IsDeleted = report.IsDeleted,

                PropertyTitle = property?.Title ?? "",
                PropertyAddress = property?.Address ?? "",
                RequestedByName = tenant?.FullName ?? "",
                RequestedByEmail = tenant?.Email ?? "",
                OwnerName = owner?.FullName ?? "",
                OwnerEmail = owner?.Email ?? ""
            };
        }
    }
}
