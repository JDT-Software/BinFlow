using ProductionTracker.Models;

namespace ProductionTracker.Services
{
    public interface IProductionService
    {
        // Existing methods from your current implementation
        Task<List<ShiftReport>> GetAllShiftReportsAsync();
        Task<List<ShiftSummary>> GetShiftSummariesAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<ShiftReport?> GetShiftReportByIdAsync(string id);
        Task<List<ShiftReport>> GetShiftReportsByDateRangeAsync(DateTime fromDate, DateTime toDate);
        Task<List<ShiftReport>> GetShiftReportsByLineManagerAsync(string lineManager);
        Task<ShiftReport> CreateShiftReportAsync(CreateShiftReportDto dto);
        Task<bool> UpdateShiftReportAsync(string id, ShiftReport shiftReport);
        Task<bool> DeleteShiftReportAsync(string id);
        Task<List<ProductionMetrics>> GetProductionMetricsAsync(DateTime fromDate, DateTime toDate);
        Task<bool> AddBinTippingEntryAsync(string shiftReportId, BinTipping binTipping);
        Task<bool> UpdateBinTippingEntryAsync(string shiftReportId, int entryIndex, BinTipping binTipping);

        // New methods for integration with Stock and Automation modules
        Task<List<HourlyEntry>> GetHourlyEntriesByDateRangeAsync(DateTime fromDate, DateTime toDate);
        Task<List<HourlyEntry>> GetHourlyEntriesByShiftAsync(string shift, DateTime date);
        Task<HourlyEntry?> GetHourlyEntryByIdAsync(string id);
        Task<string> CreateHourlyEntryAsync(HourlyEntry entry);
        Task<bool> UpdateHourlyEntryAsync(string id, HourlyEntry entry);
        Task<bool> DeleteHourlyEntryAsync(string id);

        // Enhanced dashboard integration
        Task<ProductionMetrics> GetProductionMetricsForDateAsync(DateTime date);
        Task<List<ProductionMetrics>> GetProductionTrendsAsync(DateTime fromDate, DateTime toDate);
        Task<Dictionary<string, double>> GetProductionEfficiencyByManagerAsync(DateTime fromDate, DateTime toDate);
        Task<List<ShiftReport>> GetTopPerformingShiftsAsync(int count = 10);
        Task<List<ShiftReport>> GetShiftReportsByEfficiencyRangeAsync(double minEfficiency, double maxEfficiency);

        // Shift analysis methods
        Task<double> GetAverageEfficiencyByShiftTypeAsync(string shiftType, DateTime fromDate, DateTime toDate);
        Task<Dictionary<string, int>> GetDowntimeAnalysisAsync(DateTime fromDate, DateTime toDate);
        Task<List<ProductionMetrics>> GetLineManagerPerformanceAsync(string lineManager, DateTime fromDate, DateTime toDate);

        // Data export and reporting
        Task<byte[]> ExportShiftReportsToExcelAsync(DateTime fromDate, DateTime toDate);
        Task<byte[]> ExportProductionMetricsToExcelAsync(DateTime fromDate, DateTime toDate);
        
        // Integration support methods
        Task<bool> ShiftReportExistsAsync(DateTime date, string shift, string lineManager);
        Task<List<string>> GetActiveLineManagersAsync();
        Task<Dictionary<DateTime, int>> GetProductionVolumeByDateAsync(DateTime fromDate, DateTime toDate);
    }
}