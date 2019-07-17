using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using static LiDARCupDetection.ScannerService;
using Newtonsoft.Json;
using System.IO;

namespace LiDARCupDetection
{
    public class ObjectDetector
    {
        public class ObjectLocation
        {
            public string Id { get; set; }
            public double ThMin { get; set; }
            public double ThMax { get; set; }
            public double RMin { get; set; }
            public double RMax { get; set; }
            public bool Active => ActiveTime >= ActiveTimeTol;
            public double ActiveTime { get; set; }
            public double ActiveTimeTol { get; set; }

            public ObjectLocation()
            {
                ActiveTime = 0;
            }
        }

        public class AutodetectLocation
        {
            public string Id { get; set; }
            public Point Point { get; set; }
            public bool Active => ActiveTime >= _activeTimeTol;
            public double ActiveTime { get; set; }
            private double _activeTimeTol { get; set; }

            public AutodetectLocation(string id, Point point, double activeTimeTol)
            {
                Id = id;
                Point = point;
                ActiveTime = 0;
                _activeTimeTol = activeTimeTol;
            }
        }

        public class AutodetectConfiguration
        {
            public double ThMin { get; set; }
            public double ThMax { get; set; }
            public double RMin { get; set; }
            public double RMax { get; set; }
            public double RTol { get; set; }
            public double MoveTol { get; set; }
            public double ActiveTimeTol { get; set; }
        }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static ScannerService _scannerService;
        private Timer _refreshTimer;
        private static int _refreshInterval;
        private static List<ObjectLocation> _objects { get; set; }
        private static List<AutodetectLocation> _autodetectLocations { get; set; }
        private static AutodetectConfiguration _autodetectConfiguration { get; set; }

        public ObjectDetector(ScannerService scannerService = null)
        {
            Logger.Debug($"Started");

            _scannerService = scannerService ?? new ScannerService();
            _objects = new List<ObjectLocation>();
            _autodetectLocations = new List<AutodetectLocation>();

            Configure();

            _refreshTimer = new Timer();
            _refreshTimer.Elapsed += Refresh;
            _refreshTimer.Interval = _refreshInterval;
            _refreshTimer.Start();
        }

        private void Configure()
        {
            Logger.Debug($"Configuring");

            try
            {
                _refreshInterval = int.Parse(ConfigurationManager.AppSettings["RefreshInterval"].NotNull());
                Logger.Debug($"Using RefreshInterval: {_refreshInterval}");

                var objectsJson = ConfigurationManager.AppSettings["ObjectsJSON"].NotNull();
                Logger.Debug($"Using ObjectsJSON: {objectsJson}");

                _objects = JsonConvert.DeserializeObject<List<ObjectLocation>>(File.ReadAllText(objectsJson));
                Logger.Debug($"Loaded objects: {_objects.ToJSON(false)}");

                var autodetectJson = ConfigurationManager.AppSettings["AutodetectJSON"].NotNull();
                Logger.Debug($"Using AutodetectJSON: {autodetectJson}");

                _autodetectConfiguration = JsonConvert.DeserializeObject<AutodetectConfiguration>(File.ReadAllText(autodetectJson));
                Logger.Debug($"Loaded autodetect configuration: {_autodetectConfiguration.ToJSON(false)}");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Configuration failed");
                throw;
            }
        }

        public async Task<ScanResult> GetScan()
        {
            return await _scannerService.Poll().ConfigureAwait(false);
        }

        public bool IsOnline()
        {
            return _scannerService.IsConnected();
        }

        public List<ObjectLocation> GetObjects()
        {
            return _objects;
        }

        public List<ObjectLocation> GetActiveObjects()
        {
            return _objects
                .Where(v => v.Active)
                .ToList();
        }

        public List<AutodetectLocation> GetAutodetected()
        {
            return _autodetectLocations
                .Where(v => v.Active)
                .ToList();
        }

        public AutodetectConfiguration GetAutodetectConfiguration()
        {
            return _autodetectConfiguration;
        }

        public bool IsActive(string id)
        {
            var obj = _objects.SingleOrDefault(v => v.Id == id);

            if (obj != null)
            {
                return obj.Active;
            } else
            {
                Logger.Warn($"Not found: {id}");
                return false;
            }
        }

        private static async void Refresh(object sender, ElapsedEventArgs e)
        {
            var scanResult = await _scannerService.Poll().ConfigureAwait(false);

            foreach (var obj in _objects)
            {
                var objActive = scanResult.Points.Any(v =>
                v.Th >= obj.ThMin &&
                v.Th <= obj.ThMax &&
                v.R >= obj.RMin &&
                v.R <= obj.RMax);

                if (objActive)
                {
                    obj.ActiveTime += _refreshInterval / 1000;
                } else
                {
                    obj.ActiveTime = 0;
                }
            }

            Autodetect(scanResult);
        }

        private static void Autodetect(ScanResult scanResult)
        {
            var autodetectPoints = new List<Point>();

            if (scanResult.Points.Count > 0)
            {
                AutodetectNext(scanResult, 0, ref autodetectPoints);
            } else
            {
                _autodetectLocations.Clear();
                return;
            }

            foreach (var location in _autodetectLocations)
            {
                try
                {
                    var newLocation = autodetectPoints.Single(v => Distance(location.Point, v) <= _autodetectConfiguration.MoveTol);

                    if (!location.Active)
                    {
                        location.Point = newLocation;
                    }

                    location.ActiveTime += _refreshInterval;
                    autodetectPoints.Remove(newLocation);
                }
                catch
                {
                    _autodetectLocations.Remove(location);
                }
            }

            autodetectPoints.ForEach(v =>
            {
                _autodetectLocations.Add(new AutodetectLocation(
                    Guid.NewGuid().ToString().ToUpper(), 
                    v, 
                    _autodetectConfiguration.ActiveTimeTol));
            });
        }

        private static void AutodetectNext(ScanResult scanResult, int nextIndex, ref List<Point> autodetectPoints)
        {
            int indexStart = -1;
            int indexStop = -1;

            for (int i = nextIndex; i < scanResult.Points.Count; i++)
            {
                var rStart = scanResult.Points.Select(v => v.R).ToList()[i];
                var thStart = scanResult.Points.Select(v => v.Th).ToList()[i];

                if (rStart > _autodetectConfiguration.RMin &&
                    rStart < _autodetectConfiguration.RMax &&
                    thStart > _autodetectConfiguration.ThMin &&
                    thStart < _autodetectConfiguration.ThMax)
                {
                    indexStart = i + 1;

                    if (indexStart > scanResult.Points.Count - 3)
                    {
                        return;
                    }

                    for (int j = indexStart + 1; j < scanResult.Points.Count; j++)
                    {
                        var rStop = scanResult.Points.Select(v => v.R).ToList()[j];
                        var thStop = scanResult.Points.Select(v => v.Th).ToList()[j];

                        if ((rStop > rStart + _autodetectConfiguration.RTol || rStop > _autodetectConfiguration.RMax) &&
                            thStop > _autodetectConfiguration.ThMin &&
                            thStop < _autodetectConfiguration.ThMax)
                        {
                            indexStop = j - 2;
                            break;
                        }
                    }

                    break;
                }
            }

            if (indexStart != -1 && indexStop != -1)
            {
                var thAvg = (scanResult.Points[indexStart].Th + scanResult.Points[indexStop].Th) / 2;
                var rAvg = (scanResult.Points[indexStart].R + scanResult.Points[indexStop].R) / 2;
                autodetectPoints.Add(new Point(thAvg, rAvg));

                AutodetectNext(scanResult, indexStop + 3, ref autodetectPoints);
            }
        }

        private static double Distance(Point pointStart, Point pointEnd)
        {
            return Math.Sqrt(Math.Pow(pointEnd.X - pointStart.X, 2) + Math.Pow(pointEnd.Y - pointStart.Y, 2));
        }
    }
}
