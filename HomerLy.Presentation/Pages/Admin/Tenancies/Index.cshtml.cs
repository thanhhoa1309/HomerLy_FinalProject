using Homerly.Business.Interfaces;
using Homerly.BusinessObject.DTOs.TenancyDTOs;
using Homerly.BusinessObject.Enums;
using HomerLy.DataAccess.Interfaces;
using HomerLy.DataAccess.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace HomerLy.Presentation.Pages.Admin.Tenancies
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IUnitOfWork _unitOfWork;

        public IndexModel(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            SearchQuery = string.Empty;
        }

        public List<TenancyResponseDto> Tenancies { get; set; } = new();

        [TempData]
        public string SuccessMessage { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SearchQuery { get; set; }

        [BindProperty(SupportsGet = true)]
        public TenancyStatus? Status { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                var query = _unitOfWork.Tenancy.GetQueryable()
                    .Include(t => t.Property)
                    .Include(t => t.Tenant)
                    .Include(t => t.Owner)
                    .AsQueryable();

                if (Status.HasValue)
                {
                    query = query.Where(t => t.Status == Status.Value);
                }

                if (!string.IsNullOrEmpty(SearchQuery))
                {
                    var search = SearchQuery.ToLower();
                    query = query.Where(t => t.Property.Title.ToLower().Contains(search) ||
                                           t.Tenant.FullName.ToLower().Contains(search) ||
                                           t.Owner.FullName.ToLower().Contains(search));
                }

                query = query.OrderByDescending(t => t.CreatedAt);

                var entities = await query.ToListAsync();

                Tenancies = entities.Select(t => new TenancyResponseDto
                {
                    Id = t.Id,
                    PropertyId = t.PropertyId,
                    PropertyTitle = t.Property?.Title ?? "N/A",
                    PropertyAddress = t.Property?.Address ?? "N/A",
                    TenantId = t.TenantId,
                    TenantName = t.Tenant?.FullName ?? "N/A",
                    TenantEmail = t.Tenant?.Email ?? "N/A",
                    OwnerId = t.OwnerId,
                    OwnerName = t.Owner?.FullName ?? "N/A",
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    Status = t.Status,
                    IsTenantConfirmed = t.IsTenantConfirmed,
                    ContractUrl = t.ContractUrl,
                    CreatedAt = t.CreatedAt,
                    ElectricUnitPrice = t.ElectricUnitPrice,
                    WaterUnitPrice = t.WaterUnitPrice
                }).ToList();
            }
            catch (Exception ex)
            {
                ErrorMessage = "Failed to load tenancies: " + ex.Message;
            }
        }

        public async Task<IActionResult> OnPostApproveAsync(Guid id)
        {
            try
            {
                var tenancy = await _unitOfWork.Tenancy.GetByIdAsync(id);
                if (tenancy == null)
                {
                    ErrorMessage = "Tenancy not found";
                    return RedirectToPage();
                }

                if (tenancy.Status != TenancyStatus.pending_confirmation)
                {
                    ErrorMessage = "Tenancy is not in pending status.";
                    return RedirectToPage();
                }

                tenancy.Status = TenancyStatus.active;
                await _unitOfWork.Tenancy.Update(tenancy);
                await _unitOfWork.SaveChangesAsync();

                SuccessMessage = "Tenancy approved successfully";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error approving tenancy: " + ex.Message;
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(Guid id)
        {
            try
            {
                var tenancy = await _unitOfWork.Tenancy.GetByIdAsync(id);
                if (tenancy == null)
                {
                    ErrorMessage = "Tenancy not found";
                    return RedirectToPage();
                }

                if (tenancy.Status == TenancyStatus.cancelled || tenancy.Status == TenancyStatus.expired)
                {
                    ErrorMessage = "Tenancy already inactive.";
                    return RedirectToPage();
                }

                tenancy.Status = TenancyStatus.cancelled;

                // Replicating logic for property status to ensure consistency
                var property = await _unitOfWork.Property.GetByIdAsync(tenancy.PropertyId);
                if (property != null)
                {
                    property.Status = PropertyStatus.available;
                    await _unitOfWork.Property.Update(property);
                }

                await _unitOfWork.Tenancy.Update(tenancy);
                await _unitOfWork.SaveChangesAsync();

                SuccessMessage = "Tenancy rejected (cancelled) successfully";
            }
            catch (Exception ex)
            {
                ErrorMessage = "Error rejecting tenancy: " + ex.Message;
            }
            return RedirectToPage();
        }
    }
}
