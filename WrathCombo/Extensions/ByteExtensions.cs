using Dalamud.Utility;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;

namespace WrathCombo.Extensions
{
    internal static class ByteExtensions
    {
        internal static string JobAbbreviation(this byte job)
        {
            return Svc.Data.Excel.GetSheet<ClassJob>().GetRow(job).Abbreviation.ToDalamudString().TextValue;
        }
    }
}
