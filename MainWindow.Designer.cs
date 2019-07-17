namespace LiDARCupDetection
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.Windows.Forms.DataVisualization.Charting.Series series2 = new System.Windows.Forms.DataVisualization.Charting.Series();
            this.pointsChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.coordinatesLabel = new System.Windows.Forms.Label();
            this.pollTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pointsChart)).BeginInit();
            this.SuspendLayout();
            // 
            // pointsChart
            // 
            chartArea1.Name = "ChartArea1";
            this.pointsChart.ChartAreas.Add(chartArea1);
            this.pointsChart.Location = new System.Drawing.Point(12, 12);
            this.pointsChart.Name = "pointsChart";
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
            series1.Color = System.Drawing.Color.Blue;
            series1.Name = "Points";
            series2.ChartArea = "ChartArea1";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
            series2.Color = System.Drawing.Color.Red;
            series2.Name = "Autodetected";
            this.pointsChart.Series.Add(series1);
            this.pointsChart.Series.Add(series2);
            this.pointsChart.Size = new System.Drawing.Size(776, 413);
            this.pointsChart.TabIndex = 0;
            this.pointsChart.Text = "chart1";
            this.pointsChart.MouseMove += new System.Windows.Forms.MouseEventHandler(this.pointsChart_MouseMove);
            this.pointsChart.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.pointsChart_MouseWheel);
            // 
            // coordinatesLabel
            // 
            this.coordinatesLabel.AutoSize = true;
            this.coordinatesLabel.Location = new System.Drawing.Point(12, 428);
            this.coordinatesLabel.Name = "coordinatesLabel";
            this.coordinatesLabel.Size = new System.Drawing.Size(33, 13);
            this.coordinatesLabel.TabIndex = 1;
            this.coordinatesLabel.Text = "X:, Y:";
            // 
            // pollTimer
            // 
            this.pollTimer.Tick += new System.EventHandler(this.pollTimer_Tick);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.coordinatesLabel);
            this.Controls.Add(this.pointsChart);
            this.Name = "MainWindow";
            this.Text = "LiDARCupDetection";
            this.Load += new System.EventHandler(this.MainWindow_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pointsChart)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart pointsChart;
        private System.Windows.Forms.Label coordinatesLabel;
        private System.Windows.Forms.Timer pollTimer;
    }
}