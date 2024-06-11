using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.JSInterop;
using WebUI.Extensions;
using WebUI.Services;
using WebUI.Shared;
using Dialog = M2P.WebUI.Dialogs.Dialog;

namespace WebUI.Pages.AllocationPlans;

public partial class ViewEngineTwoPlansDialog : Dialog
{
    #region PARAMETERS
    [Parameter] public Guid AllocationPlanId { get; set; } = default!;
    #endregion PARAMETERS

    #region INJECTS
    [Inject] private IODataService ODataService { get; init; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; init; } = default!;
    [Inject] private IStringLocalizer<Resource> ResourceExt { get; init; } = default!;
    #endregion INJECTS

    #region PROPS
    private bool loading = true;
    private bool IsLoading
    {
        get => loading;
        set
        {
            loading = value;
            StateHasChanged();
        }
    }

    private bool loadingDownload = false;
    private bool IsLoadingDownload
    {
        get => loadingDownload;
        set
        {
            loadingDownload = value;
            StateHasChanged();
        }
    }

    private IEnumerable<ProductionPlan> ProductionPlans { get; set; } = Enumerable.Empty<ProductionPlan>();
    #endregion PROPS

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        await QueryDataAsync();
    }

    private async Task QueryDataAsync()
    {
        IsLoading = true;

        if (AllocationPlanId != Guid.Empty)
        {
            var executor = new ProductionPlansQueryExecutor(ODataService);
            ProductionPlans = await executor.ForEngineTwo(AllocationPlanId, true);
        }

        IsLoading = false;
    }

    private string GetRise(ProductionPlan productionPlan, Guid shiftId, DateTime date)
    {
        return productionPlan.GetRiseDayPercentage(shiftId, date);
    }

    private string GetTeamName(ProductionPlan productionPlan, Guid shiftId, DateTime date)
    {
        var name = productionPlan.GetTeamName(shiftId, date);
        return string.IsNullOrEmpty(name) ? "-" : name;
    }

    private async Task OnDownloadRequested()
    {
        IsLoadingDownload = true;
        await MFGUtils.ShareExecution();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add();
        using (var stream = new MemoryStream())
        {
            //First header
            for (int i = 1; i < 6; i++)
            {
                worksheet.Cell(1, i).Value = string.Empty;
                worksheet.Column(i).AdjustToContents();
            }
            //Second header
            worksheet.Cell(1, 1).Value = Resource.Line;
            worksheet.Cell(1, 2).Value = Resource.Product;
            worksheet.Cell(1, 3).Value = Resource.Layout;
            worksheet.Cell(1, 4).Value = Resource.PriorityAbbreviation;
            worksheet.Cell(1, 5).Value = Resource.Shift;

            var col = 6;
            var dates = ProductionPlans.GetDates();
            foreach (var date in dates)
            {
                var cell = worksheet.Cell(1, col);
                cell.Value = date;
                worksheet.Range(cell.Address, cell.CellRight().Address).Merge();
                col += 2;
            }
            await MFGUtils.ShareExecution();

            var row = 2;
            foreach (var productionPlan in ProductionPlans)
            {
                var shifts = productionPlan.GetShifts();
                var shiftCount = shifts.Count();

                var lineCell = worksheet.Cell(row, 1);
                var productCell = worksheet.Cell(row, 2);
                var layoutCell = worksheet.Cell(row, 3);
                var priorityCell = worksheet.Cell(row, 4);

                lineCell.Value = productionPlan.ProductionLineWithAlias();
                productCell.Value = productionPlan.Product.Name;
                layoutCell.Value = productionPlan.Layout.Name;
                if (productionPlan.Priority != 0)
                {
                    priorityCell.Value = productionPlan.Priority;
                }

                foreach (var shift in shifts)
                {
                    worksheet.Cell(row, 5).Value = shift.Name;

                    col = 6;
                    foreach (var date in dates)
                    {
                        var productionDays = productionPlan.GetPlanDay(date, shift.Id);
                        foreach (var productionDay in productionDays)
                        {
                            var rise = productionPlan.GetRiseDayPercentage(shift.Id, date);
                            worksheet.Cell(row, col).SetValue(rise);
                            col++;

                            var team = GetTeamName(productionPlan, shift.Id, date);
                            worksheet.Cell(row, col).SetValue(team);
                            col++;
                        }
                        await MFGUtils.ShareExecution(10);
                    }
                    row++;
                }

                MergeCell(worksheet, lineCell, shiftCount - 1);
                MergeCell(worksheet, productCell, shiftCount - 1);
                MergeCell(worksheet, layoutCell, shiftCount - 1);
                MergeCell(worksheet, priorityCell, shiftCount - 1);

                await MFGUtils.ShareExecution();
            }
            worksheet.ColumnsUsed().AdjustToContents();
            worksheet.Row(1).CellsUsed().Style.Fill.BackgroundColor = XLColor.BlueGray;
            worksheet.Row(1).CellsUsed().Style.Font.FontColor = XLColor.White;
            worksheet.ColumnsUsed().Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            await MFGUtils.ShareExecution();

            await Task.Run(() =>
            {
                workbook.SaveAs(stream);
            });
            await MFGUtils.ShareExecution();

            var fileName = "SecondaryEngineTemplate.xlsx";
            var mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            var base64String = Convert.ToBase64String(stream.ToArray());
            var dataUrl = $"data:{mimeType};base64,{base64String}";
            await JSRuntime.InvokeVoidAsync("downloadFile", dataUrl, fileName);
        }

        IsLoadingDownload = false;
    }

    private void MergeCell(IXLWorksheet worksheet, IXLCell cell, int step)
    {
        worksheet
            .Range(cell.Address, cell.CellBelow(step).Address)
            .Merge()
            .Style
            .Alignment
            .SetVertical(XLAlignmentVerticalValues.Center);

        cell.WorksheetColumn().AdjustToContents();
    }

    public async static Task ShowAsync(IDialogService DialogService, Guid AllocationPlanId)
    {
        var parameters = new DialogParameters<ViewEngineTwoPlansDialog>
        {
            { x => x.AllocationPlanId, AllocationPlanId }
        };
        var options = new DialogOptions() { CloseButton = true, FullWidth = true, MaxWidth = MaxWidth.Large };
        var dialog = await DialogService.ShowAsync<ViewEngineTwoPlansDialog>(Resource.AllocationPlan, parameters, options);
        await dialog.Result;
    }
}