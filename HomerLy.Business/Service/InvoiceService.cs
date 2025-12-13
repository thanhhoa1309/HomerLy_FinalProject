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
            // 1. L?y thông tin tenancy
            var tenancy = await _unitOfWork.Tenancy.FirstOrDefaultAsync(
                t => t.Id == dto.TenancyId && !t.IsDeleted);

            if (tenancy == null)
            {
                throw new Exception("Tenancy không t?n t?i");
            }

            // 2. Ki?m tra owner có quy?n t?o invoice cho tenancy này không
            var property = await _unitOfWork.Property.GetByIdAsync(tenancy.PropertyId);

            if (property == null || property.IsDeleted)
            {
                throw new Exception("Property không t?n t?i");
            }

            if (property.OwnerId != ownerId)
            {
                throw new UnauthorizedAccessException("B?n không có quy?n t?o invoice cho tenancy này");
            }

            // 3. L?y utility reading c? nh?t c?a property (old index)
            var lastUtilityReading = await _unitOfWork.UtilityReading.GetQueryable()
                .Where(u => u.PropertyId == tenancy.PropertyId && u.TenancyId == tenancy.Id && !u.IsDeleted)
                .OrderByDescending(u => u.ReadingDate)
                .FirstOrDefaultAsync();

            int electricOldIndex = lastUtilityReading?.ElectricNewIndex ?? 0;
            int waterOldIndex = lastUtilityReading?.WaterNewIndex ?? 0;

            // 4. Validate new index ph?i l?n h?n old index
            if (dto.ElectricNewIndex < electricOldIndex)
            {
                throw new Exception($"Ch? s? ?i?n m?i ({dto.ElectricNewIndex}) ph?i l?n h?n ho?c b?ng ch? s? c? ({electricOldIndex})");
            }

            if (dto.WaterNewIndex < waterOldIndex)
            {
                throw new Exception($"Ch? s? n??c m?i ({dto.WaterNewIndex}) ph?i l?n h?n ho?c b?ng ch? s? c? ({waterOldIndex})");
            }

            // 5. Tính toán chi phí
            decimal electricCost = (dto.ElectricNewIndex - electricOldIndex) * dto.ElectricUnitPrice;
            decimal waterCost = (dto.WaterNewIndex - waterOldIndex) * dto.WaterUnitPrice;
            decimal totalAmount = property.MonthlyPrice + electricCost + waterCost + dto.OtherFees;

            // 6. T?o utility reading m?i
            var utilityReading = new UtilityReading
            {
                Id = Guid.NewGuid(),
                PropertyId = tenancy.PropertyId,
                TenancyId = tenancy.Id,
                ElectricOldIndex = electricOldIndex,
                ElectricNewIndex = dto.ElectricNewIndex,
                WaterOldIndex = waterOldIndex,
                WaterNewIndex = dto.WaterNewIndex,
                ReadingDate = DateTime.UtcNow,
                IsCharged = false,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = ownerId,
                CreatedById = ownerId,
                IsDeleted = false
            };

            await _unitOfWork.UtilityReading.AddAsync(utilityReading);

            // 7. T?o invoice
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
                Status = InvoiceStatus.pending,

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
                .Where(i => i.TenantId == tenantId && !i.IsDeleted)
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
                throw new Exception("Invoice không t?n t?i");
            }

            if (invoice.OwnerId != ownerId)
            {
                throw new UnauthorizedAccessException("B?n không có quy?n c?p nh?t invoice này");
            }

            if (invoice.Status != InvoiceStatus.draft && invoice.Status != InvoiceStatus.pending)
            {
                throw new Exception("Ch? có th? c?p nh?t invoice ? tr?ng thái draft ho?c pending");
            }

            // C?p nh?t các field n?u có
            if (dto.BillingPeriodStart.HasValue)
                invoice.BillingPeriodStart = dto.BillingPeriodStart.Value;

            if (dto.BillingPeriodEnd.HasValue)
                invoice.BillingPeriodEnd = dto.BillingPeriodEnd.Value;

            if (dto.DueDate.HasValue)
                invoice.DueDate = dto.DueDate.Value;

            if (dto.OtherFees.HasValue)
                invoice.OtherFees = dto.OtherFees.Value;

            // N?u c?p nh?t ch? s? ho?c ??n giá, c?n tính l?i
            bool needRecalculate = false;

            if (dto.ElectricNewIndex.HasValue)
            {
                if (dto.ElectricNewIndex.Value < invoice.ElectricOldIndex)
                {
                    throw new Exception($"Ch? s? ?i?n m?i ({dto.ElectricNewIndex.Value}) ph?i l?n h?n ho?c b?ng ch? s? c? ({invoice.ElectricOldIndex})");
                }
                invoice.ElectricNewIndex = dto.ElectricNewIndex.Value;
                needRecalculate = true;
            }

            if (dto.WaterNewIndex.HasValue)
            {
                if (dto.WaterNewIndex.Value < invoice.WaterOldIndex)
                {
                    throw new Exception($"Ch? s? n??c m?i ({dto.WaterNewIndex.Value}) ph?i l?n h?n ho?c b?ng ch? s? c? ({invoice.WaterOldIndex})");
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

            // Tính l?i cost và total
            if (needRecalculate || dto.OtherFees.HasValue)
            {
                invoice.ElectricCost = (invoice.ElectricNewIndex - invoice.ElectricOldIndex) * invoice.ElectricUnitPrice;
                invoice.WaterCost = (invoice.WaterNewIndex - invoice.WaterOldIndex) * invoice.WaterUnitPrice;
                invoice.TotalAmount = invoice.MonthlyRentPrice + invoice.ElectricCost + invoice.WaterCost + invoice.OtherFees;

                // C?p nh?t utility reading n?u c?n
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
                throw new Exception("Invoice không t?n t?i");
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
                throw new UnauthorizedAccessException("B?n không có quy?n xóa invoice này");
            }

            if (invoice.Status != InvoiceStatus.draft)
            {
                throw new Exception("Ch? có th? xóa invoice ? tr?ng thái draft");
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
                throw new Exception("Invoice không t?n t?i");
            }

            if (invoice.TenantId != tenantId)
            {
                throw new UnauthorizedAccessException("B?n không có quy?n thanh toán invoice này");
            }

            if (invoice.Status == InvoiceStatus.paid)
            {
                throw new Exception("Invoice ?ã ???c thanh toán");
            }

            if (invoice.Status == InvoiceStatus.cancelled)
            {
                throw new Exception("Invoice ?ã b? h?y");
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
            // L?y thông tin property
            var property = await _unitOfWork.Property.GetByIdAsync(invoice.PropertyId);

            // L?y thông tin tenant
            var tenant = await _unitOfWork.Account.GetByIdAsync(invoice.TenantId);

            // L?y thông tin owner
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
