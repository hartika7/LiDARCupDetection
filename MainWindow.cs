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

namespace LiDARCupDetection
{
    public partial class MainWindow : Form
    {
        private ObjectDetector _objectDetector;

        public MainWindow(ObjectDetector objectDetector)
        {
            InitializeComponent();

            _objectDetector = objectDetector;
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            InitPointsChart();
            pollTimer.Start();
        }

        private void InitPointsChart()
        {
            pointsChart.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            pointsChart.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            pointsChart.ChartAreas[0].AxisX.ScaleView.Zoomable = true;
            pointsChart.ChartAreas[0].AxisY.ScaleView.Zoomable = true;
            pointsChart.ChartAreas[0].AxisX.Minimum = -1000;
            pointsChart.ChartAreas[0].AxisX.Maximum = 1000;
            pointsChart.ChartAreas[0].AxisY.Minimum = 0;
            pointsChart.ChartAreas[0].AxisY.Maximum = 2000;
        }

        private async void pollTimer_Tick(object sender, EventArgs e)
        {
            pointsChart.Series["Points"].Points.Clear();
            var scanResult = await _objectDetector.GetScan();
            scanResult.Points.ForEach(v => pointsChart.Series["Points"].Points.AddXY(v.X, v.Y));

            pointsChart.Series["Autodetected"].Points.Clear();
            var autodetected = _objectDetector.GetAutodetected();
            autodetected.ForEach(v => pointsChart.Series["Autodetected"].Points.AddXY(v.Point.X, v.Point.Y));
        }

        private void pointsChart_MouseMove(object sender, MouseEventArgs e)
        {
            var cursor = e.Location;
            var hits = pointsChart.HitTest(cursor.X, cursor.Y, false, ChartElementType.PlottingArea);

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

        private void pointsChart_MouseWheel(object sender, MouseEventArgs e)
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
                    pointsChart.ChartAreas[0].AxisX.LabelStyle.Enabled = true;
                    pointsChart.ChartAreas[0].AxisY.LabelStyle.Enabled = true;
                }
                else if (e.Delta > 0) // Scrolled up.
                {
                    pointsChart.ChartAreas[0].AxisX.LabelStyle.Enabled = false;
                    pointsChart.ChartAreas[0].AxisY.LabelStyle.Enabled = false;

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
