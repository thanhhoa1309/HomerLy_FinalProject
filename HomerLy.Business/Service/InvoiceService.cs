using HomerLy.Business.Interfaces;
using HomerLy.BusinessObject.DTOs.InvoiceDTOs;
using HomerLy.BusinessObject.Enums;
using HomerLy.DataAccess.Entities;
using HomerLy.DataAccess.Interfaces;
using Homerly.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomerLy.Business.Service
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IUnitOfWork _unitOfWork;

        public InvoiceService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<InvoiceResponseDto> CreateInvoiceAsync(Guid ownerId, CreateInvoiceDto dto)
        {
            // 1. Lấy thông tin tenancy
            var tenancy = await _unitOfWork.Tenancy.FirstOrDefaultAsync(
                t => t.Id == dto.TenancyId && !t.IsDeleted);

            if (tenancy == null)
            {
                throw new Exception("Tenancy không tồn tại");
            }

            // 2. Kiểm tra owner có quyền tạo invoice cho tenancy này không
            var property = await _unitOfWork.Property.GetByIdAsync(tenancy.PropertyId);

            if (property == null || property.IsDeleted)
            {
                throw new Exception("Property không tồn tại");
            }

            if (property.OwnerId != ownerId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền tạo invoice cho tenancy này");
            }

            // 3. Lấy utility reading để lấy thông tin (KHÔNG TẠO MỚI)
            var utilityReading = await _unitOfWork.UtilityReading.GetQueryable()
                .Where(u => u.PropertyId == tenancy.PropertyId && u.TenancyId == tenancy.Id && !u.IsDeleted)
                .OrderByDescending(u => u.ReadingDate)
                .FirstOrDefaultAsync();

            if (utilityReading == null)
            {
                throw new Exception("Không tìm thấy utility reading. Vui lòng tạo utility reading trước khi tạo invoice.");
            }

            int electricOldIndex = utilityReading.ElectricOldIndex;
            int waterOldIndex = utilityReading.WaterOldIndex;

            // 4. Validate new index phải lớn hơn old index
            if (dto.ElectricNewIndex < electricOldIndex)
            {
                throw new Exception($"Chỉ số điện mới ({dto.ElectricNewIndex}) phải lớn hơn hoặc bằng chỉ số cũ ({electricOldIndex})");
            }

            if (dto.WaterNewIndex < waterOldIndex)
            {
                throw new Exception($"Chỉ số nước mới ({dto.WaterNewIndex}) phải lớn hơn hoặc bằng chỉ số cũ ({waterOldIndex})");
            }

            // 5. Kiểm tra xem utility reading đã có invoice chưa
            var existingInvoice = await _unitOfWork.Invoice.FirstOrDefaultAsync(
                i => i.UtilityReadingId == utilityReading.Id && !i.IsDeleted);

            if (existingInvoice != null)
            {
                throw new Exception("Utility reading này đã có invoice. Vui lòng sử dụng invoice hiện có hoặc tạo utility reading mới.");
            }

            // 6. Tính toán chi phí
            decimal electricCost = (dto.ElectricNewIndex - electricOldIndex) * dto.ElectricUnitPrice;
            decimal waterCost = (dto.WaterNewIndex - waterOldIndex) * dto.WaterUnitPrice;
            decimal totalAmount = property.MonthlyPrice + electricCost + waterCost + dto.OtherFees;

            // 7. Tạo invoice với status là DRAFT
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                PropertyId = tenancy.PropertyId,
                TenancyId = tenancy.Id,
                TenantId = tenancy.TenantId,
                OwnerId = ownerId,
                UtilityReadingId = utilityReading.Id,

                BillingPeriodStart = dto.BillingPeriodStart,
                BillingPeriodEnd = dto.BillingPeriodEnd,
                DueDate = dto.DueDate,
                Status = InvoiceStatus.draft, // Changed to draft

                MonthlyRentPrice = property.MonthlyPrice,

                ElectricOldIndex = electricOldIndex,
                ElectricNewIndex = dto.ElectricNewIndex,
                ElectricUnitPrice = dto.ElectricUnitPrice,
                ElectricCost = electricCost,

                WaterOldIndex = waterOldIndex,
                WaterNewIndex = dto.WaterNewIndex,
                WaterUnitPrice = dto.WaterUnitPrice,
                WaterCost = waterCost,

                OtherFees = dto.OtherFees,
                TotalAmount = totalAmount,

                CreatedAt = DateTime.UtcNow,
                CreatedBy = ownerId,
                IsDeleted = false
            };

            await _unitOfWork.Invoice.AddAsync(invoice);
            await _unitOfWork.SaveChangesAsync();

            return await MapToResponseDto(invoice);
        }

        public async Task<InvoiceResponseDto?> GetInvoiceByIdAsync(Guid invoiceId)
        {
            var invoice = await _unitOfWork.Invoice.GetByIdAsync(invoiceId);

            if (invoice == null || invoice.IsDeleted)
            {
                return null;
            }

            return await MapToResponseDto(invoice);
        }

        public async Task<List<InvoiceResponseDto>> GetInvoicesByOwnerAsync(Guid ownerId)
        {
            var invoices = await _unitOfWork.Invoice.GetQueryable()
                .Where(i => i.OwnerId == ownerId && !i.IsDeleted)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var responseDtos = new List<InvoiceResponseDto>();
            foreach (var item in invoices)
            {
                responseDtos.Add(await MapToResponseDto(item));
            }

            return responseDtos;
        }

        public async Task<List<InvoiceResponseDto>> GetInvoicesByTenantAsync(Guid tenantId)
        {
            var invoices = await _unitOfWork.Invoice.GetQueryable()
                .Where(i => i.TenantId == tenantId && !i.IsDeleted && i.Status != InvoiceStatus.draft) // Don't show draft to tenants
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var responseDtos = new List<InvoiceResponseDto>();
            foreach (var item in invoices)
            {
                responseDtos.Add(await MapToResponseDto(item));
            }

            return responseDtos;
        }

        public async Task<List<InvoiceResponseDto>> GetInvoicesByTenancyAsync(Guid tenancyId)
        {
            var invoices = await _unitOfWork.Invoice.GetQueryable()
                .Where(i => i.TenancyId == tenancyId && !i.IsDeleted)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var responseDtos = new List<InvoiceResponseDto>();
            foreach (var item in invoices)
            {
                responseDtos.Add(await MapToResponseDto(item));
            }

            return responseDtos;
        }

        public async Task<InvoiceResponseDto> UpdateInvoiceAsync(Guid invoiceId, Guid ownerId, UpdateInvoiceDto dto)
        {
            var invoice = await _unitOfWork.Invoice.GetByIdAsync(invoiceId);

            if (invoice == null || invoice.IsDeleted)
            {
                throw new Exception("Invoice không tồn tại");
            }

            if (invoice.OwnerId != ownerId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền cập nhật invoice này");
            }

            if (invoice.Status != InvoiceStatus.draft && invoice.Status != InvoiceStatus.pending)
            {
                throw new Exception("Chỉ có thể cập nhật invoice ở trạng thái draft hoặc pending");
            }

            // Cập nhật các field nếu có
            if (dto.BillingPeriodStart.HasValue)
                invoice.BillingPeriodStart = dto.BillingPeriodStart.Value;

            if (dto.BillingPeriodEnd.HasValue)
                invoice.BillingPeriodEnd = dto.BillingPeriodEnd.Value;

            if (dto.DueDate.HasValue)
                invoice.DueDate = dto.DueDate.Value;

            if (dto.OtherFees.HasValue)
                invoice.OtherFees = dto.OtherFees.Value;

            // Nếu cập nhật chỉ số hoặc đơn giá, cần tính lại
            bool needRecalculate = false;

            if (dto.ElectricNewIndex.HasValue)
            {
                if (dto.ElectricNewIndex.Value < invoice.ElectricOldIndex)
                {
                    throw new Exception($"Chỉ số điện mới ({dto.ElectricNewIndex.Value}) phải lớn hơn hoặc bằng chỉ số cũ ({invoice.ElectricOldIndex})");
                }
                invoice.ElectricNewIndex = dto.ElectricNewIndex.Value;
                needRecalculate = true;
            }

            if (dto.WaterNewIndex.HasValue)
            {
                if (dto.WaterNewIndex.Value < invoice.WaterOldIndex)
                {
                    throw new Exception($"Chỉ số nước mới ({dto.WaterNewIndex.Value}) phải lớn hơn hoặc bằng chỉ số cũ ({invoice.WaterOldIndex})");
                }
                invoice.WaterNewIndex = dto.WaterNewIndex.Value;
                needRecalculate = true;
            }

            if (dto.ElectricUnitPrice.HasValue)
            {
                invoice.ElectricUnitPrice = dto.ElectricUnitPrice.Value;
                needRecalculate = true;
            }

            if (dto.WaterUnitPrice.HasValue)
            {
                invoice.WaterUnitPrice = dto.WaterUnitPrice.Value;
                needRecalculate = true;
            }

            // Tính lại cost và total
            if (needRecalculate || dto.OtherFees.HasValue)
            {
                invoice.ElectricCost = (invoice.ElectricNewIndex - invoice.ElectricOldIndex) * invoice.ElectricUnitPrice;
                invoice.WaterCost = (invoice.WaterNewIndex - invoice.WaterOldIndex) * invoice.WaterUnitPrice;
                invoice.TotalAmount = invoice.MonthlyRentPrice + invoice.ElectricCost + invoice.WaterCost + invoice.OtherFees;

                // Cập nhật utility reading nếu cần
                var utilityReading = await _unitOfWork.UtilityReading.GetByIdAsync(invoice.UtilityReadingId);

                if (utilityReading != null && !utilityReading.IsDeleted)
                {
                    if (dto.ElectricNewIndex.HasValue)
                        utilityReading.ElectricNewIndex = dto.ElectricNewIndex.Value;
                    if (dto.WaterNewIndex.HasValue)
                        utilityReading.WaterNewIndex = dto.WaterNewIndex.Value;

                    utilityReading.UpdatedAt = DateTime.UtcNow;
                    utilityReading.UpdatedBy = ownerId;

                    _unitOfWork.UtilityReading.Update(utilityReading);
                }
            }

            invoice.UpdatedAt = DateTime.UtcNow;
            invoice.UpdatedBy = ownerId;

            _unitOfWork.Invoice.Update(invoice);
            await _unitOfWork.SaveChangesAsync();

            return await MapToResponseDto(invoice);
        }

        public async Task<InvoiceResponseDto> UpdateInvoiceStatusAsync(Guid invoiceId, UpdateInvoiceStatusDto dto)
        {
            var invoice = await _unitOfWork.Invoice.GetByIdAsync(invoiceId);

            if (invoice == null || invoice.IsDeleted)
            {
                throw new Exception("Invoice không tồn tại");
            }

            invoice.Status = dto.Status;

            if (dto.PaymentDate.HasValue)
            {
                invoice.PaymentDate = dto.PaymentDate.Value;
            }

            if (dto.Status == InvoiceStatus.paid && !invoice.PaymentDate.HasValue)
            {
                invoice.PaymentDate = DateTime.UtcNow;
            }

            invoice.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Invoice.Update(invoice);
            await _unitOfWork.SaveChangesAsync();

            return await MapToResponseDto(invoice);
        }

        public async Task<bool> DeleteInvoiceAsync(Guid invoiceId, Guid ownerId)
        {
            var invoice = await _unitOfWork.Invoice.GetByIdAsync(invoiceId);

            if (invoice == null || invoice.IsDeleted)
            {
                return false;
            }

            if (invoice.OwnerId != ownerId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xóa invoice này");
            }

            if (invoice.Status != InvoiceStatus.draft)
            {
                throw new Exception("Chỉ có thể xóa invoice ở trạng thái draft");
            }

            invoice.IsDeleted = true;
            invoice.DeletedAt = DateTime.UtcNow;
            invoice.DeletedBy = ownerId;

            _unitOfWork.Invoice.Update(invoice);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task UpdateOverdueInvoicesAsync()
        {
            var overdueInvoices = await _unitOfWork.Invoice.GetQueryable()
                .Where(i => !i.IsDeleted
                    && i.Status == InvoiceStatus.pending
                    && i.DueDate < DateTime.UtcNow)
                .ToListAsync();

            foreach (var invoice in overdueInvoices)
            {
                invoice.Status = InvoiceStatus.overdue;
                invoice.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.Invoice.Update(invoice);
            }

            if (overdueInvoices.Any())
            {
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<InvoiceResponseDto> PayInvoiceAsync(Guid invoiceId, Guid tenantId)
        {
            var invoice = await _unitOfWork.Invoice.GetByIdAsync(invoiceId);

            if (invoice == null || invoice.IsDeleted)
            {
                throw new Exception("Invoice không tồn tại");
            }

            if (invoice.TenantId != tenantId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền thanh toán invoice này");
            }

            if (invoice.Status == InvoiceStatus.paid)
            {
                throw new Exception("Invoice đã được thanh toán");
            }

            if (invoice.Status == InvoiceStatus.cancelled || invoice.Status == InvoiceStatus.draft)
            {
                throw new Exception("Không thể thanh toán invoice này");
            }

            invoice.Status = InvoiceStatus.paid;
            invoice.PaymentDate = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;
            invoice.UpdatedBy = tenantId;

            _unitOfWork.Invoice.Update(invoice);
            await _unitOfWork.SaveChangesAsync();

            return await MapToResponseDto(invoice);
        }

        private async Task<InvoiceResponseDto> MapToResponseDto(Invoice invoice)
        {
            // Lấy thông tin property
            var property = await _unitOfWork.Property.GetByIdAsync(invoice.PropertyId);

            // Lấy thông tin tenant
            var tenant = await _unitOfWork.Account.GetByIdAsync(invoice.TenantId);

            // Lấy thông tin owner
            var owner = await _unitOfWork.Account.GetByIdAsync(invoice.OwnerId);

            return new InvoiceResponseDto
            {
                Id = invoice.Id,
                PropertyId = invoice.PropertyId,
                TenancyId = invoice.TenancyId,
                TenantId = invoice.TenantId,
                OwnerId = invoice.OwnerId,
                UtilityReadingId = invoice.UtilityReadingId,

                BillingPeriodStart = invoice.BillingPeriodStart,
                BillingPeriodEnd = invoice.BillingPeriodEnd,
                DueDate = invoice.DueDate,
                Status = invoice.Status,
                PaymentDate = invoice.PaymentDate,

                MonthlyRentPrice = invoice.MonthlyRentPrice,

                ElectricOldIndex = invoice.ElectricOldIndex,
                ElectricNewIndex = invoice.ElectricNewIndex,
                ElectricUnitPrice = invoice.ElectricUnitPrice,
                ElectricCost = invoice.ElectricCost,

                WaterOldIndex = invoice.WaterOldIndex,
                WaterNewIndex = invoice.WaterNewIndex,
                WaterUnitPrice = invoice.WaterUnitPrice,
                WaterCost = invoice.WaterCost,

                OtherFees = invoice.OtherFees,
                TotalAmount = invoice.TotalAmount,

                CreatedAt = invoice.CreatedAt,
                UpdatedAt = invoice.UpdatedAt,

                PropertyTitle = property?.Title ?? "",
                PropertyAddress = property?.Address ?? "",
                TenantName = tenant?.FullName ?? "",
                OwnerName = owner?.FullName ?? ""
            };
        }
    }
}
