using ClosedXML.Excel;

namespace WebUI.Extensions
{
    public static class ClosedXMLExtension
    {
        public static IXLCell CreateAutoSizedComment(this IXLCell cell, string comment)
        {
            cell.CreateComment().AddText(comment);
            cell.GetComment().Style.Size.SetAutomaticSize();
            return cell;
        }

        public static IXLRange SetAllBorders(this IXLRange range)
        {
            range.Style.Border.SetBottomBorder(XLBorderStyleValues.Thin);
            range.Style.Border.SetTopBorder(XLBorderStyleValues.Thin);
            range.Style.Border.SetTopBorder(XLBorderStyleValues.Thin);
            range.Style.Border.SetRightBorder(XLBorderStyleValues.Thin);
            return range;
        }

        public static IXLCell SetAllBorders(this IXLCell cell)
        {
            cell.Style.Border.SetBottomBorder(XLBorderStyleValues.Thin);
            cell.Style.Border.SetTopBorder(XLBorderStyleValues.Thin);
            cell.Style.Border.SetTopBorder(XLBorderStyleValues.Thin);
            cell.Style.Border.SetRightBorder(XLBorderStyleValues.Thin);
            return cell;
        }
    }
}
