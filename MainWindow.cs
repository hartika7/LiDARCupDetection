using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Point = LiDARCupDetection.ScannerService.Point;

namespace LiDARCupDetection
{
    public partial class MainWindow : Form
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private ObjectDetector _objectDetector;

        public MainWindow(ObjectDetector objectDetector)
        {
            InitializeComponent();

            _objectDetector = objectDetector;
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            InitObjectsChart();
            refreshTimer.Start();
        }

        private void InitObjectsChart()
        {
            objectsChart.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            objectsChart.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            objectsChart.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            objectsChart.ChartAreas[0].AxisY.ScaleView.Zoomable = true;
            objectsChart.ChartAreas[0].AxisX.Minimum = -1000;
            objectsChart.ChartAreas[0].AxisX.Maximum = 1000;
            objectsChart.ChartAreas[0].AxisY.Minimum = 0;
            objectsChart.ChartAreas[0].AxisY.Maximum = 2000;

            DrawStaticObjects();
        }

        private async void refreshTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                objectsChart.Series["Points"].Points.Clear();
                var scanResult = await _objectDetector.GetScan();
                scanResult.Points.ForEach(v => objectsChart.Series["Points"].Points.AddXY(v.X, v.Y));

                objectsChart.Series["Autodetected"].Points.Clear();
                var autodetected = _objectDetector.GetAutodetected();
                autodetected.ForEach(v => {
                    var pointIndex = objectsChart.Series["Autodetected"].Points.AddXY(v.Point.X, v.Point.Y);
                    objectsChart.Series["Autodetected"].Points[pointIndex].Label = v.Id;
                });

                UpdateStaticObjects();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error refreshing GUI");
            }
        }

        private void DrawStaticObjects()
        {
            foreach (var obj in _objectDetector.GetObjects())
            {
                var chartSeries = objectsChart.Series.Add(obj.Id);
                chartSeries.ChartType = SeriesChartType.Line;
                chartSeries.Color = Color.Green;

                var pointBL = new Point(obj.ThMin, obj.RMin);
                var pointBR = new Point(obj.ThMax, obj.RMin);
                var pointTR = new Point(obj.ThMax, obj.RMax);
                var pointTL = new Point(obj.ThMin, obj.RMax);
                GetArcPoints(pointBL.Th, pointBR.Th, pointBL.R).ForEach(v => chartSeries.Points.AddXY(v.X, v.Y));
                GetArcPoints(pointTR.Th, pointTL.Th, pointTR.R).ForEach(v => chartSeries.Points.AddXY(v.X, v.Y));
                chartSeries.Points.AddXY(pointBL.X, pointBL.Y);

                chartSeries.Points[0].Label = obj.Id;
            }
        }

        private void UpdateStaticObjects()
        {
            foreach (var obj in _objectDetector.GetObjects())
            {
                var chartSeries = objectsChart.Series[obj.Id];
                chartSeries.Color = obj.Active ? Color.Red : Color.Green;
            }
        }

        private List<Point> GetArcPoints(double thMin, double thMax, double r, int count = 100)
        {
            var points = new List<Point>();

            for (int i = 0; i <= count; i++)
            {
                var th = thMin + (thMax - thMin) / count * i;
                points.Add(new Point(th, r));
            }

            return points;
        }

        private void objectsChart_MouseMove(object sender, MouseEventArgs e)
        {
            var cursor = e.Location;
            var hits = objectsChart.HitTest(cursor.X, cursor.Y, false, ChartElementType.PlottingArea);

            foreach (var hit in hits)
            {
                if (hit.ChartElementType == ChartElementType.PlottingArea)
                {
                    var xVal = hit.ChartArea.AxisX.PixelPositionToValue(cursor.X);
                    var yVal = hit.ChartArea.AxisY.PixelPositionToValue(cursor.Y);

                    coordinatesLabel.Text = $"X: {Math.Round(xVal, 3)}, Y: {Math.Round(yVal, 3)}";
                }
            }
        }

        private void objectsChart_MouseWheel(object sender, MouseEventArgs e)
        {
            var chart = (Chart)sender;
            var xAxis = chart.ChartAreas[0].AxisX;
            var yAxis = chart.ChartAreas[0].AxisY;

            try
            {
                if (e.Delta < 0) // Scrolled down.
                {
                    xAxis.ScaleView.ZoomReset();
                    yAxis.ScaleView.ZoomReset();
                    objectsChart.ChartAreas[0].AxisX.LabelStyle.Enabled = true;
                    objectsChart.ChartAreas[0].AxisY.LabelStyle.Enabled = true;
                }
                else if (e.Delta > 0) // Scrolled up.
                {
                    objectsChart.ChartAreas[0].AxisX.LabelStyle.Enabled = false;
                    objectsChart.ChartAreas[0].AxisY.LabelStyle.Enabled = false;

                    var xMin = xAxis.ScaleView.ViewMinimum;
                    var xMax = xAxis.ScaleView.ViewMaximum;
                    var yMin = yAxis.ScaleView.ViewMinimum;
                    var yMax = yAxis.ScaleView.ViewMaximum;

                    var posXStart = xAxis.PixelPositionToValue(e.Location.X) - (xMax - xMin) / 4;
                    var posXFinish = xAxis.PixelPositionToValue(e.Location.X) + (xMax - xMin) / 4;
                    var posYStart = yAxis.PixelPositionToValue(e.Location.Y) - (yMax - yMin) / 4;
                    var posYFinish = yAxis.PixelPositionToValue(e.Location.Y) + (yMax - yMin) / 4;

                    xAxis.ScaleView.Zoom(posXStart, posXFinish);
                    yAxis.ScaleView.Zoom(posYStart, posYFinish);
                }
            }
            catch { }
        }
    }
}
