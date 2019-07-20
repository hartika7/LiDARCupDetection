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
            this.objectsChart = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.coordinatesLabel = new System.Windows.Forms.Label();
            this.refreshTimer = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.objectsChart)).BeginInit();
            this.SuspendLayout();
            // 
            // objectsChart
            // 
            chartArea1.Name = "ChartArea1";
            this.objectsChart.ChartAreas.Add(chartArea1);
            this.objectsChart.Location = new System.Drawing.Point(16, 15);
            this.objectsChart.Margin = new System.Windows.Forms.Padding(4);
            this.objectsChart.Name = "objectsChart";
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.FastLine;
            series1.Color = System.Drawing.Color.Blue;
            series1.Name = "Points";
            series2.ChartArea = "ChartArea1";
            series2.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
            series2.Color = System.Drawing.Color.Red;
            series2.Name = "Autodetected";
            this.objectsChart.Series.Add(series1);
            this.objectsChart.Series.Add(series2);
            this.objectsChart.Size = new System.Drawing.Size(1035, 508);
            this.objectsChart.TabIndex = 0;
            this.objectsChart.Text = "chart1";
            this.objectsChart.MouseMove += new System.Windows.Forms.MouseEventHandler(this.objectsChart_MouseMove);
            this.objectsChart.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.objectsChart_MouseWheel);
            // 
            // coordinatesLabel
            // 
            this.coordinatesLabel.AutoSize = true;
            this.coordinatesLabel.Location = new System.Drawing.Point(16, 527);
            this.coordinatesLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.coordinatesLabel.Name = "coordinatesLabel";
            this.coordinatesLabel.Size = new System.Drawing.Size(42, 17);
            this.coordinatesLabel.TabIndex = 1;
            this.coordinatesLabel.Text = "X:, Y:";
            // 
            // refreshTimer
            // 
            this.refreshTimer.Interval = 500;
            this.refreshTimer.Tick += new System.EventHandler(this.refreshTimer_Tick);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1067, 554);
            this.Controls.Add(this.coordinatesLabel);
            this.Controls.Add(this.objectsChart);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "MainWindow";
            this.Text = "LiDARCupDetection";
            this.Load += new System.EventHandler(this.MainWindow_Load);
            ((System.ComponentModel.ISupportInitialize)(this.objectsChart)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart objectsChart;
        private System.Windows.Forms.Label coordinatesLabel;
        private System.Windows.Forms.Timer refreshTimer;
    }
}