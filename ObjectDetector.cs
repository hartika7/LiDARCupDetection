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
using System.Collections.Specialized;

namespace LiDARCupDetection
{
    public class ObjectDetector
    {
        public class ObjectLocation
        {
            public string Id { get; set; }
            public Limits Limits { get; set; }
            public Point Location { get; set; }
            public bool Autodetected { get; set; }
            public bool Active => ActiveTime >= ActiveTimeTol;
            public double ActiveTime { get; set; }
            public double ActiveTimeTol { get; set; }

            public ObjectLocation()
            {
                Autodetected = false;
                ActiveTime = 0;
            }
        }

        public class AutodetectConfiguration
        {
            public Limits Limits { get; set; }
            public double RTol { get; set; }
            public double MoveTol { get; set; }
            public double ActiveTimeTol { get; set; }
        }

        public class Limits
        {
            public double ThMin { get; set; }
            public double ThMax { get; set; }
            public double RMin { get; set; }
            public double RMax { get; set; }
        }

        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private ScannerService _scannerService;
        private Timer _refreshTimer;
        private int _refreshInterval;
        private List<ObjectLocation> _objects { get; set; }
        private AutodetectConfiguration _autodetectConfiguration { get; set; }

        public ObjectDetector(string settingsPostfix, ScannerService scannerService)
        {
            Logger.Debug($"Started");

            _objects = new List<ObjectLocation>();
            _scannerService = scannerService;

            Configure(settingsPostfix);

            if (IsEnabled())
            {
                _refreshTimer = new Timer();
                _refreshTimer.Elapsed += Refresh;
                _refreshTimer.Interval = _refreshInterval;
                _refreshTimer.Start();
            }
        }

        private void Configure(string settingsPostfix)
        {
            Logger.Debug($"Configuring ({settingsPostfix})");

            try
            {
                var settings = (NameValueCollection)ConfigurationManager.GetSection($"ObjectDetectorSettings_{settingsPostfix}").NotNull();

                _refreshInterval = int.Parse(settings["RefreshInterval"].NotNull());
                Logger.Debug($"Using RefreshInterval: {_refreshInterval}");

                var objectsJson = settings["ObjectsJSON"].NotNull();
                Logger.Debug($"Using ObjectsJSON: {objectsJson}");

                _objects = JsonConvert.DeserializeObject<List<ObjectLocation>>(File.ReadAllText(objectsJson));
                Logger.Debug($"Loaded objects: {_objects.ToJSON(false)}");

                var autodetectJson = settings["AutodetectJSON"].NotNull();
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

        public bool IsEnabled()
        {
            // Zero to disable
            return _refreshInterval > 0;
        }

        public bool IsOnline()
        {
            return _scannerService.IsConnected();
        }

        public List<ObjectLocation> GetObjects()
        {
            return _objects;
        }


        public List<ObjectLocation> GetStaticObjects()
        {
            return _objects.Where(v => !v.Autodetected).ToList();
        }

        public List<ObjectLocation> GetActiveObjects()
        {
            return _objects
                .Where(v => v.Active)
                .ToList();
        }

        public List<ObjectLocation> GetAutodetected()
        {
            return _objects
                .Where(v => v.Active && v.Autodetected)
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

        private async void Refresh(object sender, ElapsedEventArgs e)
        {
            var scanResult = await _scannerService.Poll().ConfigureAwait(false);
            var staticObjects = _objects.Where(v => !v.Autodetected).ToList();

            foreach (var obj in staticObjects)
            {
                var objActive = scanResult.Points.Any(v =>
                v.Th >= obj.Limits.ThMin &&
                v.Th <= obj.Limits.ThMax &&
                v.R >= obj.Limits.RMin &&
                v.R <= obj.Limits.RMax);

                if (objActive)
                {
                    obj.ActiveTime += _refreshInterval / 1000.0;
                } else
                {
                    obj.ActiveTime = 0;
                }
            }

            Autodetect(scanResult);
        }

        private void Autodetect(ScanResult scanResult)
        {
            var autodetectPoints = new List<Point>();

            if (scanResult.Points.Count > 0)
            {
                AutodetectNext(scanResult, 0, ref autodetectPoints);
            } else
            {
                _objects.RemoveAll(v => v.Autodetected);
                return;
            }

            var autodetectedObjects = _objects.Where(v => v.Autodetected).ToList();
            foreach (var location in autodetectedObjects)
            {
                try
                {
                    var newLocation = autodetectPoints.Single(v => Distance(location.Location, v) <= _autodetectConfiguration.MoveTol);

                    if (!location.Active)
                    {
                        location.Location = newLocation;
                    }

                    location.ActiveTime += _refreshInterval / 1000.0;
                    autodetectPoints.Remove(newLocation);
                }
                catch
                {
                    _objects.Remove(location);
                }
            }

            autodetectPoints.ForEach(v =>
            {
                _objects.Add(new ObjectLocation()
                {
                    Id = Guid.NewGuid().ToString().ToUpper(),
                    Location = v,
                    Autodetected = true,
                    ActiveTimeTol = _autodetectConfiguration.ActiveTimeTol
                });
            });
        }

        private void AutodetectNext(ScanResult scanResult, int nextIndex, ref List<Point> autodetectPoints)
        {
            int indexStart = -1;
            int indexStop = -1;

            for (int i = nextIndex; i < scanResult.Points.Count; i++)
            {
                var rStart = scanResult.Points.Select(v => v.R).ToList()[i];
                var thStart = scanResult.Points.Select(v => v.Th).ToList()[i];

                if (rStart > _autodetectConfiguration.Limits.RMin &&
                    rStart < _autodetectConfiguration.Limits.RMax &&
                    thStart > _autodetectConfiguration.Limits.ThMin &&
                    thStart < _autodetectConfiguration.Limits.ThMax)
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

                        if ((rStop > rStart + _autodetectConfiguration.RTol || rStop > _autodetectConfiguration.Limits.RMax) &&
                            thStop > _autodetectConfiguration.Limits.ThMin &&
                            thStop < _autodetectConfiguration.Limits.ThMax)
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

        private double Distance(Point pointStart, Point pointEnd)
        {
            return Math.Sqrt(Math.Pow(pointEnd.X - pointStart.X, 2) + Math.Pow(pointEnd.Y - pointStart.Y, 2));
        }
    }
}
