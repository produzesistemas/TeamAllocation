using System.ComponentModel.DataAnnotations;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/teams")]
    public sealed class TeamController : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateTeamAsync(
            [FromServices] ITeamService service,
            [FromBody] CreateTeamModel model)
        {
            var products = new HashSet<TeamProduct>();
            foreach (var product in model.products)
            {
                IEnumerable<TeamWorkstation> workstations = product.TeamWorkstations.Select(w=>new TeamWorkstation()
                {
                    Id = w.id,
                    TeamProductId = product.id,
                    WorkstationId = w.workstation.Id,
                    TeamWorkstationHCs = w.teamWorkstationHCs.Select(o => new TeamWorkstationHC()
                    {
                        HCId = o.HCId,
                        TeamWorkstationId = w.id,
                    }).ToList()
                }).ToList();
                products.Add(new TeamProduct() { Id = product.id, Category = product.category, TeamId = model.id, ProductId = product.productId, LayoutId = product.layoutId,TeamWorkstations = workstations});
            }

            if (model.id == Guid.Empty)
            {
                return Result(await service.CreateAsync(
                    flexSite, companyId, buildingId, customerId, divisionId, areaId,
                    model.description, model.responsibleId, model.shiftId,products));
            }
            else
            {
                return Result(await service.UpdateAsync(
                    model.id, model.description, model.responsibleId, model.shiftId, products, 
                    model.reason!, model.user!));
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteTeam(
            [FromServices] ITeamService service,
            [FromRoute] Guid id)
        {
            Result createResult = await service.DeleteAsync(id, false);
            if (createResult.Valid)
            {
                return Ok();
            }
            return BadRequest(createResult.Errors);
        }

        public record CreateTeamModel([Required] Guid id, [Required] string description, [Required] Guid responsibleId, [Required] Guid shiftId, [Required] List<TeamProductModel> products, string? reason = "", string? user = null);
        public record TeamProductModel([Required] Guid id, [Required] Guid productId, [Required] Guid teamId, [Required] Guid layoutId, string category, List<TeamWorkstationModel> TeamWorkstations);
        public record TeamWorkstationModel([Required] Guid id, [Required] Guid TeamProductId, [Required] Workstation workstation, List<TeamWorkstationHCModel> teamWorkstationHCs);
        public record TeamWorkstationHCModel([Required] Guid id, [Required] Guid TeamWorkstationId, [Required] Guid HCId);
    }
}