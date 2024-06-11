using ClosedXML.Excel;
using System.ComponentModel.DataAnnotations;
using WebApi.Extensions;
using WebApi.Resquests;
using WebUI.Shared;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/allocationplan")]
    public class AllocationPlanController : ControllerBase
    {
        [HttpGet("rise")]
        public async Task<IActionResult> GetRise(
            [FromServices] IAllocationPlanService service,
            [FromQuery(Name = "teamProductId")] Guid teamProductId
            )
        {
            var rise = await service.GetCandidateRise(teamProductId);
            if (rise is not null)
            {
                return Ok(rise);
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> SaveAllocationPlanAsync(
            [FromServices] IAllocationPlanService service,
            [Required][FromHeader(Name = "site")] Guid flexSite,
            [FromHeader(Name = "company")] Guid? companyId,
            [FromHeader(Name = "building")] Guid? buildingId,
            [FromHeader(Name = "customer")] Guid? customerId,
            [FromHeader(Name = "division")] Guid? divisionId,
            [FromHeader(Name = "area")] Guid? areaId,
            [FromBody] CreateAllocationPlanModel model)
        {
            var productionPlans = new HashSet<ProductionPlan>();
            foreach (var pp in model.productionPlans)
            {
                productionPlans.Add(pp.ToProductionPlan());
            }

            if (model.id == Guid.Empty)
            {
                Result<AllocationPlan> createResult = await service.CreateAsync(
                    flexSite, companyId, buildingId, customerId, divisionId, areaId,
                    model.startDate, model.endDate, AllocationPlanType.FromName(model.type),
                    AllocationPlanEngine.FromName(model.engine), productionPlans);
                if (createResult.Valid)
                {
                    return Ok(createResult.Data);
                }
                return BadRequest(createResult.Errors);
            }
            else
            {
                Result updateResult = await service.UpdateAsync(model.id, flexSite,
                    model.startDate, model.endDate, AllocationPlanType.FromName(model.type),
                    AllocationPlanEngine.FromName(model.engine), productionPlans, model.reason! , model.user!) ;
                if (updateResult.Valid)
                {
                    return Ok();
                }
                return BadRequest(updateResult.Errors);
            }
        }

        [HttpPut("clone")]
        public async Task<IActionResult> CloneAllocationPlanAsync(
           [FromServices] IAllocationPlanService service,
           [FromBody] GuidModel model)
        {
            Result createResult = await service.CloneAsync(model.id);
            if (createResult.Valid)
            {
                return Ok();
            }
            return BadRequest(createResult.Errors);
        }

        [HttpPut("validate")]
        public async Task<IActionResult> ValidateAllocationPlanAsync(
           [FromServices] IAllocationPlanService service,
           [FromBody] GuidModel model)
        {
            Result createResult = await service.ValidateAsync(model.id);
            if (createResult.Valid)
            {
                return Ok();
            }
            return BadRequest(createResult.Errors);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAllocationPlanAsync(
            [FromServices] IAllocationPlanService service,
            [FromRoute] Guid id)
        {
            Result createResult = await service.DeleteAsync(id, false);
            if (createResult.Valid)
            {
                return Ok();
            }
            return BadRequest(createResult.Errors);
        }

        [HttpPost("allocate")]
        public async Task<IActionResult> AllocateEngineTwoTeams(IFormFile file
            , [FromServices] IAllocationPlanService service
            , [FromServices] IMFGAllocationDataProvider dataProvider
            , [Required][FromHeader(Name = "utf")] int utf)
        {
            var productionPlans = new List<ProductionPlan>();
            var errorMessages = new List<Error>();
            var teamsAllocated = new HashSet<Team>();

            try
            {
                using (var stream = file.OpenReadStream())
                {
                    using var workbook = new XLWorkbook(stream);
                    var worksheet = workbook.Worksheet(1);
                    var rowCount = worksheet.RowsUsed().Count();
                    var siteEntity = await dataProvider.Sites.Where(s => s.Id == site).FirstOrDefaultAsync();
                    if (siteEntity == default)
                    {
                        errorMessages.Add(new Error("Allocation", "SiteNotFound"));
                        return BadRequest(errorMessages);
                    }
                    var dataShifts = dataProvider.Shifts.Where(s => s.SiteId == siteEntity.Id).ToArray();
                    var allocationplan = AllocationPlan.CreateEmpty();
                    if (rowCount < 3)
                    {
                        errorMessages.Add(new Error("Allocation", "NoDataToRead"));
                        return BadRequest(errorMessages);
                    }

                    for (int i = 2; i < rowCount; i++)
                    {
                        var id = Guid.NewGuid();
                        var row = worksheet.Row(i + 1);

                        var col = 1;
                        var lineCell = row.Cell(col).Value.GetText();
                        var lineTokens = lineCell.Split('_');
                        var lineName = lineTokens[0];
                        var lineExists = await dataProvider.ProductionLines.Where(pl => pl.Name.ToUpper() == lineName.ToUpper()).FirstOrDefaultAsync();
                        if (lineExists == null)
                        {
                            errorMessages.Add(new Error("LineNotFound", lineName));
                            return BadRequest(errorMessages);
                        }

                        col++;
                        var productName = row.Cell(col).Value.GetText();
                        var productExists = await dataProvider.Products
                            .Where(pr => pr.Name.ToUpper() == productName.ToUpper())
                            .FirstOrDefaultAsync();
                        if (productExists == null)
                        {
                            errorMessages.Add(new Error("ProductNotFound", productName));
                            return BadRequest(errorMessages);
                        }

                        col++;
                        var layoutName = row.Cell(col).Value.GetText();
                        var layout = await dataProvider.Layouts
                            .Include(x => x.Product)
                            .Include(x => x.ProductionLine)
                            .Where(l => 
                                l.Name.ToUpper() == layoutName.ToUpper() && 
                                l.Product.Name.ToUpper() == productName.ToUpper() && 
                                l.ProductionLine.Name.ToUpper() == lineName.ToUpper())
                            .FirstOrDefaultAsync();
                        if (layout == null)
                        {
                            errorMessages.Add(new Error("LayoutNotFoundByProductAndLine", string.Concat(layoutName, " / ", productName, " / ", lineName)));
                            return BadRequest(errorMessages);
                        }

                        col++;
                        if (!row.Cell(col).IsEmpty() && !row.Cell(col).Value.IsNumber)
                        {
                            errorMessages.Add(new Error("PriorityNotNumber", "'" + row.Cell(col).Value.ToString() + "'"));
                            return BadRequest(errorMessages);
                        }
                        int priority = row.Cell(col).IsEmpty() 
                            ? 0 
                            : (int)row.Cell(col).Value.GetNumber();

                        col++;
                        var resultPlanDays = GetProductionPlanDays(worksheet, row, dataShifts, utf);
                        if (!resultPlanDays.Valid)
                        {
                            errorMessages.AddRange(resultPlanDays.Errors);
                            return BadRequest(errorMessages);
                        }
                        var productionDays = resultPlanDays.Data;
                        var productionPlanQuantity = productionDays.Sum(p => p.Quantity);

                        productionPlans.Add(new ProductionPlan()
                        {
                            AllocationPlanId = allocationplan.Id,
                            Id = id,
                            ProductId = layout.Product.Id,
                            Product = layout.Product,
                            ProductionDays = productionDays,
                            ProductionLineId = layout.ProductionLine.Id,
                            ProductionLine = layout.ProductionLine.Clone(),
                            Quantity = productionPlanQuantity,
                            Alias = lineTokens.Length > 1 ? lineTokens[1]: string.Empty,
                            Velocity = 0,
                            LayoutId = layout.Id,
                            Layout = layout,
                            Priority = priority
                        });
                    }
                }

                var result = await service.AllocateEngineTwoTeamAsync(productionPlans);
                if (!result.Valid)
                {
                    errorMessages.AddRange(result.Errors);
                    return BadRequest(errorMessages);
                }

                return Ok(result.Data);
            }
            catch (Exception ex)
            {
                errorMessages.Add(new Error("UnexpectedError", ex.Message));
                return BadRequest(errorMessages);
            }
        }

        private DataResult<ProductionDay> GetProductionPlanDays(IXLWorksheet worksheet, IXLRow row, IEnumerable<Shift> shifts, int utf)
        {
            var dateHeader = worksheet.Row(1);
            var header = worksheet.Row(2);
            List<ProductionDay> producionDays = new List<ProductionDay>();
            var index = 5;
            var dateIndex = 5;
            var cell = row.Cell(index);
            while (!cell.IsEmpty())
            {
                var dateCol = dateHeader.Cell(dateIndex);
                var shiftCount = 0;
                while (!dateCol.IsEmpty())
                {
                    if (!dateCol.Value.IsDateTime)
                    {
                        return M2P.Results.Result.Fail("InvalidDate", dateCol.Value.ToString());
                    }

                    var date = dateCol.Value.GetDateTime().Date;
                    var adjustedServerDateTime = DateTime.Now.ToUtfDateTime(utf);
                    if (date < adjustedServerDateTime.Date)
                    {
                        return M2P.Results.Result.Fail("DateMustBeGraterOrEqualToday", date.Date.ToShortDateString());
                    }
                    if (producionDays.Any(x => x.Date == date))
                    {
                        return M2P.Results.Result.Fail("DateIsRepeated", date.Date.ToShortDateString());
                    }

                    //if (date.DayOfWeek == DayOfWeek.Sunday)
                    //{
                    //    return Result.Fail("DateIsSunday", date.Date.ToShortDateString());
                    //}

                    shiftCount = dateCol.MergedRange().ColumnCount();

                    for (int i = 0; i < shiftCount; i++)
                    {
                        var shiftNameCell = header.Cell(index).Value;
                        var shiftName = shiftNameCell.IsNumber
                            ? shiftNameCell.GetNumber().ToString()
                            : shiftNameCell.GetText();

                        var shift = shifts
                            .Where(s => s.Name.ToUpper() == shiftName.ToUpper())
                            .FirstOrDefault();
                        if (shift != default)
                        {

                            producionDays.Add(new ProductionDay()
                            {
                                Date = date,
                                Quantity = cell.Value.IsNumber ? (int)cell.Value.GetNumber() : 0,
                                ShiftId = shift.Id,
                                Shift = shift
                            }); ;
                        }
                        index++;
                        cell = row.Cell(index);
                    }
                    dateIndex += shiftCount;
                    dateCol = dateHeader.Cell(dateIndex);

                }
            }

            return M2P.Results.Result.Ok(producionDays, producionDays.Count);
        }

        [HttpPost("allocate/teams")]
        public async Task<IActionResult> AllocateEngineOneTeams([FromServices] IAllocationPlanService service,
            [FromBody] AllocateProductionPlansModel model)
        {
            var list = new HashSet<ProductionPlan>();
            foreach (var pp in model.productionPlans)
            {
                list.Add(pp.ToProductionPlan());
            }

            var result = await service.AllocateEngineOneTeamAsync(model.StartDate, list);

            var productionPlans = new HashSet<ProductionPlan>();
            if (result.Data != null)
            {
                foreach (var pp in result.Data)
                {
                    if (pp != null)
                    {
                        productionPlans.Add(pp);
                    }
                }
            }
            return Ok(productionPlans);
        }

        [HttpPost("allocate/productionplan")]
        public async Task<IActionResult> AllocateProductionPlan(
            [FromServices] IAllocationPlanService service,
            [FromBody] AllocateProductionPlanModel model)
        {
            return Result(await service.AllocateProductionPlanAsync(model.StartDate,
                    model.productionPlan.ToProductionPlan()));
        }

        [HttpPost("UploadProductionPlan")]
        public async Task<IActionResult> UploadProductionLine(IFormFile file
            , [FromServices] IAllocationPlanService service
            , [FromHeader(Name = "customer")] Guid flexCustomer
            , [FromHeader(Name = "site")] Guid siteId)
        {
            var productionPlans = new List<ProductionPlan>();
            using (var stream = file.OpenReadStream())
            {
                using var workbook = new XLWorkbook(stream);
                var worksheet = workbook.Worksheet(1);

                var rowCount = worksheet.RowsUsed().Count();

                var errorMessages = new List<Error>();

                for (int i = 2; i <= rowCount; i++)
                {
                    var row = worksheet.Row(i);

                    try
                    {
                        var lineName = row.Cell(1).Value.GetText();
                        var productName = row.Cell(2).Value.GetText();
                        var layoutName = row.Cell(3).Value.GetText();
                        var shiftName = row.Cell(4).Value.IsText
                            ? row.Cell(4).Value.GetText()
                            : row.Cell(4).Value.GetNumber().ToString();
                        var quantity = (int)row.Cell(5).Value.GetNumber();
                        var velocity = (int)row.Cell(6).Value.GetNumber();

                        var result = await service.BuildAsync(lineName, productName, layoutName, shiftName, siteId, quantity, velocity, flexCustomer);
                        if (result.Valid && result.Data != null)
                        {
                            productionPlans.Add(result.Data);
                        }
                        else
                        {
                            errorMessages.AddRange(result.Errors);
                        }
                    }
                    catch (InvalidCastException)
                    {
                        errorMessages.Add(new Error("UnableToGetDataInLine", i.ToString()));
                    }
                }

                if (errorMessages.Any())
                {
                    return BadRequest(errorMessages);
                }
            }

            return Ok(productionPlans);
        }
    }

    #region MODELS

    public record AllocateProductionPlansModel(
        [Required] DateTime StartDate,
        [Required] List<ProductionPlanModel> productionPlans);

    public record AllocateProductionPlanModel(
        [Required] DateTime StartDate,
        [Required] ProductionPlanModel productionPlan);

    public record GuidModel([Required] Guid id);

    public record DownloadPrimaryEngineModel([Required] string[] Columns);


    public record DownloadSecondaryEngineModel(
        [Required] string Line,
        [Required] string Product,
        [Required] string Layout,
        [Required] string Priority,

        [Required] string CommentPlanDay,
        [Required] string CommentLine,
        [Required] string CommentProduct,
        [Required] string CommentLayout,
        [Required] string CommentPriority,
        [Required] string CommentShift);

    #endregion
}
