using MongoDB.Driver;
using MongoDB.Bson;
using ProductionTracker.Models;
using Microsoft.Extensions.Options;
using System.Security.Authentication;

namespace ProductionTracker.Services
{
    // Configuration class
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string ShiftReportsCollectionName { get; set; } = "shift_reports";
        public string HourlyEntriesCollectionName { get; set; } = "hourly_entries";
    }

    // MongoDB service implementation
    public class ProductionService : IProductionService
    {
    private IMongoCollection<ShiftReport>? _shiftReports;
    private IMongoCollection<HourlyEntry>? _hourlyEntries;
    private bool _initAttempted;
    private bool _initSucceeded;
    private string _initError = string.Empty;

        public ProductionService(IOptions<MongoDbSettings> settings)
        {
            _settings = settings.Value;
        }

        private readonly MongoDbSettings _settings;

        private void TryInitialize()
        {
            if (_initAttempted) return;
            _initAttempted = true;

            if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
            {
                _initError = "Mongo connection string missing (MongoDbSettings__ConnectionString). Running in degraded mode.";
                Console.WriteLine("⚠️ " + _initError);
                return;
            }
            try
            {
                var url = new MongoUrl(_settings.ConnectionString);
                var cs = MongoClientSettings.FromUrl(url);
                cs.SslSettings = new SslSettings { EnabledSslProtocols = SslProtocols.Tls12 };
                cs.ServerSelectionTimeout = TimeSpan.FromSeconds(15);
                cs.ConnectTimeout = TimeSpan.FromSeconds(15);
                cs.SocketTimeout = TimeSpan.FromSeconds(30);
                var client = new MongoClient(cs);
                var database = client.GetDatabase(_settings.DatabaseName);
                _shiftReports = database.GetCollection<ShiftReport>(_settings.ShiftReportsCollectionName);
                _hourlyEntries = database.GetCollection<HourlyEntry>(_settings.HourlyEntriesCollectionName);
                _initSucceeded = true;
                Console.WriteLine($"✅ Mongo initialized (lazy). Host(s): {string.Join(',', url.Servers.Select(s => s.Host))}");
            }
            catch (Exception ex)
            {
                _initError = ex.Message;
                Console.WriteLine($"❌ Mongo lazy init failed: {ex.Message}\n{ex}");
            }
        }

        private bool EnsureReady()
        {
            if (!_initAttempted) TryInitialize();
            if (_initSucceeded) return true;
            return false;
        }

        // Existing methods from your current implementation
        public async Task<List<ShiftReport>> GetAllShiftReportsAsync()
        {
            if (!EnsureReady()) return new List<ShiftReport>();
            return await _shiftReports!
                .Find(_ => true)
                .Sort(Builders<ShiftReport>.Sort.Descending(x => x.Date))
                .ToListAsync();
        }

        public async Task<List<ShiftSummary>> GetShiftSummariesAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            var filterBuilder = Builders<ShiftReport>.Filter;
            var filter = filterBuilder.Empty;

            if (fromDate.HasValue)
                filter &= filterBuilder.Gte(x => x.Date, fromDate.Value);
            
            if (toDate.HasValue)
                filter &= filterBuilder.Lte(x => x.Date, toDate.Value);

            var projection = Builders<ShiftReport>.Projection
                .Include(x => x.Id)
                .Include(x => x.Date)
                .Include(x => x.LineManager)
                .Include(x => x.Shift)
                .Include(x => x.TotalTipped)
                .Include(x => x.AverageWeight)
                .Include(x => x.TotalDowntime);

            if (!EnsureReady()) return new List<ShiftSummary>();
            var results = await _shiftReports!
                .Find(filter)
                .Project(projection)
                .Sort(Builders<ShiftReport>.Sort.Descending(x => x.Date))
                .ToListAsync();

            return results.Select(doc => new ShiftSummary
            {
                Id = doc["_id"].AsObjectId.ToString(),
                Date = doc["date"].ToLocalTime(), // FIXED: Changed from ToUniversalTime() to ToLocalTime()
                LineManager = doc["lineManager"].AsString,
                Shift = doc.Contains("shift") ? doc["shift"].AsString : "",
                TotalTipped = doc["totalTipped"].AsInt32,
                AverageWeight = doc["averageWeight"].AsDouble,
                TotalDowntime = doc["totalDowntime"].AsInt32,
                Efficiency = CalculateNewEfficiency(doc["totalTipped"].AsInt32, doc) // Updated calculation
            }).ToList();
        }

        public async Task<ShiftReport?> GetShiftReportByIdAsync(string id)
        {
            if (!EnsureReady()) return null;
            return await _shiftReports!
                .Find(x => x.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ShiftReport>> GetShiftReportsByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            if (!EnsureReady()) return new List<ShiftReport>();
            return await _shiftReports!
                .Find(x => x.Date >= fromDate && x.Date <= toDate)
                .Sort(Builders<ShiftReport>.Sort.Descending(x => x.Date))
                .ToListAsync();
        }

        public async Task<List<ShiftReport>> GetShiftReportsByLineManagerAsync(string lineManager)
        {
            if (!EnsureReady()) return new List<ShiftReport>();
            return await _shiftReports!
                .Find(x => x.LineManager == lineManager)
                .Sort(Builders<ShiftReport>.Sort.Descending(x => x.Date))
                .ToListAsync();
        }

        public async Task<ShiftReport> CreateShiftReportAsync(CreateShiftReportDto dto)
        {
            // Add comprehensive debug logging to track date handling
            Console.WriteLine($"📅 Backend: Received date for shift report: {dto.Date:yyyy-MM-dd HH:mm:ss} (Kind: {dto.Date.Kind})");
            Console.WriteLine($"📅 Backend: Received date ticks: {dto.Date.Ticks}");
            Console.WriteLine($"📅 Backend: Current server timezone: {TimeZoneInfo.Local.Id}");
            
            // Force the date to be treated as local and only keep the date part
            var localDate = new DateTime(dto.Date.Year, dto.Date.Month, dto.Date.Day, 0, 0, 0, DateTimeKind.Local);
            Console.WriteLine($"📅 Backend: Forced local date: {localDate:yyyy-MM-dd HH:mm:ss} (Kind: {localDate.Kind})");
            
            var shiftReport = new ShiftReport
            {
                Date = localDate, // Use the forced local date
                LineManager = dto.LineManager,
                Shift = dto.Shift,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            Console.WriteLine($"📅 Backend: About to save date: {shiftReport.Date:yyyy-MM-dd HH:mm:ss} (Kind: {shiftReport.Date.Kind})");

            if (!EnsureReady()) return shiftReport; // no-op degraded
            await _shiftReports!.InsertOneAsync(shiftReport);
            
            // Immediately read back what was saved to see if MongoDB converted it
            var savedReport = await GetShiftReportByIdAsync(shiftReport.Id);
            if (savedReport != null)
            {
                Console.WriteLine($"📅 Backend: MongoDB saved date as: {savedReport.Date:yyyy-MM-dd HH:mm:ss} (Kind: {savedReport.Date.Kind})");
            }
            
            Console.WriteLine($"📅 Backend: Saved shift report with ID: {shiftReport.Id}");
            
            return shiftReport;
        }

        public async Task<bool> UpdateShiftReportAsync(string id, ShiftReport shiftReport)
        {
            Console.WriteLine($"📅 Backend: Updating report {id} with date: {shiftReport.Date:yyyy-MM-dd HH:mm:ss} (Kind: {shiftReport.Date.Kind})");
            
            shiftReport.UpdatedAt = DateTime.UtcNow;
            
            // Recalculate totals
            RecalculateTotals(shiftReport);

            if (!EnsureReady()) return false;
            var result = await _shiftReports!.ReplaceOneAsync(x => x.Id == id, shiftReport);
            
            Console.WriteLine($"📅 Backend: Update result - Modified count: {result.ModifiedCount}");
            
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteShiftReportAsync(string id)
        {
            if (!EnsureReady()) return false;
            var result = await _shiftReports!.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<List<ProductionMetrics>> GetProductionMetricsAsync(DateTime fromDate, DateTime toDate)
        {
            var pipeline = new[]
            {
                new BsonDocument("$match", new BsonDocument
                {
                    { "date", new BsonDocument
                        {
                            { "$gte", fromDate },
                            { "$lte", toDate }
                        }
                    }
                }),
                new BsonDocument("$project", new BsonDocument
                {
                    { "date", 1 },
                    { "lineManager", 1 },
                    { "totalTipped", 1 },
                    { "averageWeight", 1 },
                    { "totalDowntime", 1 },
                    { "efficiency", new BsonDocument("$multiply", new BsonArray
                        {
                            new BsonDocument("$divide", new BsonArray
                            {
                                new BsonDocument("$subtract", new BsonArray { 480, "$totalDowntime" }), // 8 hour shift = 480 minutes
                                480
                            }),
                            100
                        })
                    }
                })
            };

            if (!EnsureReady()) return new List<ProductionMetrics>();
            var results = await _shiftReports!.Aggregate<BsonDocument>(pipeline).ToListAsync();
            
            return results.Select(doc => new ProductionMetrics
            {
                Date = doc["date"].ToLocalTime(), // FIXED: Changed from ToUniversalTime() to ToLocalTime()
                TotalBinsTipped = doc["totalTipped"].AsInt32,
                AverageWeight = doc["averageWeight"].AsDouble,
                TotalDowntime = doc["totalDowntime"].AsInt32,
                EfficiencyPercentage = doc["efficiency"].AsDouble,
                LineManager = doc["lineManager"].AsString
            }).ToList();
        }

        public async Task<bool> AddBinTippingEntryAsync(string shiftReportId, BinTipping binTipping)
        {
            Console.WriteLine($"📅 Backend: Adding bin tipping entry to report {shiftReportId}");
            Console.WriteLine($"📅 Backend: Entry time: {binTipping.Time}");
            
            var update = Builders<ShiftReport>.Update
                .Push(x => x.BinTippings, binTipping)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            if (!EnsureReady()) return false;
            var result = await _shiftReports!.UpdateOneAsync(x => x.Id == shiftReportId, update);
            
            if (result.ModifiedCount > 0)
            {
                Console.WriteLine($"📅 Backend: Successfully added bin tipping entry");
                
                // Recalculate totals
                var shiftReport = await GetShiftReportByIdAsync(shiftReportId);
                if (shiftReport != null)
                {
                    RecalculateTotals(shiftReport);
                    await UpdateShiftReportAsync(shiftReportId, shiftReport);
                }
            }
            else
            {
                Console.WriteLine($"📅 Backend: Failed to add bin tipping entry");
            }

            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateBinTippingEntryAsync(string shiftReportId, int entryIndex, BinTipping binTipping)
        {
            var update = Builders<ShiftReport>.Update
                .Set(x => x.BinTippings[entryIndex], binTipping)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            if (!EnsureReady()) return false;
            var result = await _shiftReports!.UpdateOneAsync(x => x.Id == shiftReportId, update);

            if (result.ModifiedCount > 0)
            {
                // Recalculate totals
                var shiftReport = await GetShiftReportByIdAsync(shiftReportId);
                if (shiftReport != null)
                {
                    RecalculateTotals(shiftReport);
                    await UpdateShiftReportAsync(shiftReportId, shiftReport);
                }
            }

            return result.ModifiedCount > 0;
        }

        // New methods for integration with Stock and Automation modules
        public async Task<List<HourlyEntry>> GetHourlyEntriesByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _hourlyEntries
                .Find(entry => entry.Date >= fromDate && entry.Date <= toDate)
                .Sort(Builders<HourlyEntry>.Sort.Descending(x => x.Date).Descending(x => x.Time))
                .ToListAsync();
        }

        public async Task<List<HourlyEntry>> GetHourlyEntriesByShiftAsync(string shift, DateTime date)
        {
            return await _hourlyEntries
                .Find(entry => entry.Shift == shift && entry.Date.Date == date.Date)
                .Sort(Builders<HourlyEntry>.Sort.Ascending(x => x.Time))
                .ToListAsync();
        }

        public async Task<HourlyEntry?> GetHourlyEntryByIdAsync(string id)
        {
            return await _hourlyEntries
                .Find(entry => entry.Id == id)
                .FirstOrDefaultAsync();
        }

        public async Task<string> CreateHourlyEntryAsync(HourlyEntry entry)
        {
            entry.CreatedAt = DateTime.UtcNow;
            if (!EnsureReady()) return entry.Id; // degraded
            await _hourlyEntries!.InsertOneAsync(entry);
            return entry.Id;
        }

        public async Task<bool> UpdateHourlyEntryAsync(string id, HourlyEntry entry)
        {
            if (!EnsureReady()) return false;
            var result = await _hourlyEntries!.ReplaceOneAsync(e => e.Id == id, entry);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteHourlyEntryAsync(string id)
        {
            if (!EnsureReady()) return false;
            var result = await _hourlyEntries!.DeleteOneAsync(e => e.Id == id);
            return result.DeletedCount > 0;
        }

        // Enhanced dashboard integration
        public async Task<ProductionMetrics> GetProductionMetricsForDateAsync(DateTime date)
        {
            var reports = await GetShiftReportsByDateRangeAsync(date.Date, date.Date);
            
            if (!reports.Any())
                return new ProductionMetrics { Date = date };

            return new ProductionMetrics
            {
                Date = date,
                TotalBinsTipped = reports.Sum(r => r.TotalTipped),
                AverageWeight = reports.Average(r => r.AverageWeight),
                TotalDowntime = reports.Sum(r => r.TotalDowntime),
                EfficiencyPercentage = reports.Average(r => CalculateEfficiencyForReport(r)),
                LineManager = reports.FirstOrDefault()?.LineManager ?? ""
            };
        }

        public async Task<List<ProductionMetrics>> GetProductionTrendsAsync(DateTime fromDate, DateTime toDate)
        {
            return await GetProductionMetricsAsync(fromDate, toDate);
        }

        public async Task<Dictionary<string, double>> GetProductionEfficiencyByManagerAsync(DateTime fromDate, DateTime toDate)
        {
            var reports = await GetShiftReportsByDateRangeAsync(fromDate, toDate);
            
            return reports
                .GroupBy(r => r.LineManager)
                .ToDictionary(
                    group => group.Key,
                    group => group.Average(r => CalculateEfficiencyForReport(r))
                );
        }

        public async Task<List<ShiftReport>> GetTopPerformingShiftsAsync(int count = 10)
        {
            var reports = await GetAllShiftReportsAsync();
            return reports
                .OrderByDescending(r => CalculateEfficiencyForReport(r))
                .Take(count)
                .ToList();
        }

        public async Task<List<ShiftReport>> GetShiftReportsByEfficiencyRangeAsync(double minEfficiency, double maxEfficiency)
        {
            var reports = await GetAllShiftReportsAsync();
            return reports
                .Where(r => {
                    var efficiency = CalculateEfficiencyForReport(r);
                    return efficiency >= minEfficiency && efficiency <= maxEfficiency;
                })
                .ToList();
        }

        // Shift analysis methods
        public async Task<double> GetAverageEfficiencyByShiftTypeAsync(string shiftType, DateTime fromDate, DateTime toDate)
        {
            var reports = await GetShiftReportsByDateRangeAsync(fromDate, toDate);
            var shiftReports = reports.Where(r => r.Shift == shiftType).ToList();
            
            return shiftReports.Any() 
                ? shiftReports.Average(r => CalculateEfficiencyForReport(r))
                : 0;
        }

        public async Task<Dictionary<string, int>> GetDowntimeAnalysisAsync(DateTime fromDate, DateTime toDate)
        {
            var reports = await GetShiftReportsByDateRangeAsync(fromDate, toDate);
            var downtimeCategories = new Dictionary<string, int>();

            foreach (var report in reports)
            {
                if (report.BinTippings != null)
                {
                    foreach (var entry in report.BinTippings.Where(bt => bt.DownTime > 0))
                    {
                        var reason = string.IsNullOrEmpty(entry.ReasonForNotAchievingTarget) 
                            ? "Unspecified" 
                            : entry.ReasonForNotAchievingTarget;

                        if (downtimeCategories.ContainsKey(reason))
                            downtimeCategories[reason] += entry.DownTime;
                        else
                            downtimeCategories[reason] = entry.DownTime;
                    }
                }
            }

            return downtimeCategories;
        }

        public async Task<List<ProductionMetrics>> GetLineManagerPerformanceAsync(string lineManager, DateTime fromDate, DateTime toDate)
        {
            var reports = await GetShiftReportsByLineManagerAsync(lineManager);
            var filteredReports = reports.Where(r => r.Date >= fromDate && r.Date <= toDate).ToList();

            return filteredReports.Select(r => new ProductionMetrics
            {
                Date = r.Date,
                TotalBinsTipped = r.TotalTipped,
                AverageWeight = r.AverageWeight,
                TotalDowntime = r.TotalDowntime,
                EfficiencyPercentage = CalculateEfficiencyForReport(r),
                LineManager = r.LineManager
            }).ToList();
        }

        // Data export and reporting - stub implementations
        public async Task<byte[]> ExportShiftReportsToExcelAsync(DateTime fromDate, DateTime toDate)
        {
            // TODO: Implement Excel export using EPPlus or similar library
            await Task.CompletedTask;
            return Array.Empty<byte>();
        }

        public async Task<byte[]> ExportProductionMetricsToExcelAsync(DateTime fromDate, DateTime toDate)
        {
            // TODO: Implement Excel export using EPPlus or similar library
            await Task.CompletedTask;
            return Array.Empty<byte>();
        }

        // Integration support methods
        public async Task<bool> ShiftReportExistsAsync(DateTime date, string shift, string lineManager)
        {
            var report = await _shiftReports
                .Find(r => r.Date.Date == date.Date && r.Shift == shift && r.LineManager == lineManager)
                .FirstOrDefaultAsync();
            
            return report != null;
        }

        public async Task<List<string>> GetActiveLineManagersAsync()
        {
            var reports = await _shiftReports
                .Find(_ => true)
                .Project(r => r.LineManager)
                .ToListAsync();

            return reports.Distinct().OrderBy(manager => manager).ToList();
        }

        public async Task<Dictionary<DateTime, int>> GetProductionVolumeByDateAsync(DateTime fromDate, DateTime toDate)
        {
            var reports = await GetShiftReportsByDateRangeAsync(fromDate, toDate);
            
            return reports
                .GroupBy(r => r.Date.Date)
                .ToDictionary(
                    group => group.Key,
                    group => group.Sum(r => r.TotalTipped)
                );
        }

        // Private helper methods
        private void RecalculateTotals(ShiftReport shiftReport)
        {
            var workingEntries = shiftReport.BinTippings.Where(x => !x.IsLunchBreak).ToList();
            
            shiftReport.TotalTipped = workingEntries.Sum(x => x.BinsTipped);
            shiftReport.AverageWeight = workingEntries.Any() 
                ? workingEntries.Average(x => x.AverageBinWeight) 
                : 0;
            shiftReport.TotalDowntime = shiftReport.BinTippings.Sum(x => x.DownTime);
        }

        // Updated efficiency calculation: bins per hour vs target
        private double CalculateEfficiency(int totalBins, int workingEntries, int targetBinsPerHour = 65)
        {
            if (workingEntries <= 0) return 0;
            var binsPerHour = (double)totalBins / workingEntries;
            return Math.Min(100, (binsPerHour / targetBinsPerHour) * 100);
        }

        // Helper method for calculating bins per hour from BsonDocument
        private double CalculateNewEfficiency(int totalBins, BsonDocument doc)
        {
            // Try to get the number of working entries from binTippings array
            if (doc.Contains("binTippings") && doc["binTippings"].IsBsonArray)
            {
                var binTippings = doc["binTippings"].AsBsonArray;
                var workingEntries = binTippings.Count(entry => 
                    entry.IsBsonDocument && 
                    (!entry.AsBsonDocument.Contains("isLunchBreak") || !entry.AsBsonDocument["isLunchBreak"].AsBoolean));
                
                if (workingEntries > 0)
                {
                    return CalculateEfficiency(totalBins, workingEntries);
                }
            }
            
            // Fallback: assume reasonable number of entries based on total bins
            var estimatedEntries = Math.Max(1, totalBins / 50); // Rough estimate
            return CalculateEfficiency(totalBins, estimatedEntries);
        }

        // Helper method for calculating efficiency from ShiftReport
        private double CalculateEfficiencyForReport(ShiftReport report)
        {
            var workingEntries = report.BinTippings?.Where(bt => !bt.IsLunchBreak).Count() ?? 0;
            return CalculateEfficiency(report.TotalTipped, Math.Max(1, workingEntries));
        }
    }
}