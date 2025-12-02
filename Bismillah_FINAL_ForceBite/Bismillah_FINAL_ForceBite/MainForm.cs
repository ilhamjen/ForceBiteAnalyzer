using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Collections.Generic;

// Namespace disesuaikan dengan nama proyek Anda
namespace Bismillah_FINAL_ForceBite
{
    // Custom Complex number implementation to replace System.Numerics.Complex
    public struct ComplexNumber
    {
        public double Real { get; set; }
        public double Imaginary { get; set; }

        public ComplexNumber(double real, double imaginary)
        {
            Real = real;
            Imaginary = imaginary;
        }

        public double Magnitude => Math.Sqrt(Real * Real + Imaginary * Imaginary);
        public double Phase => Math.Atan2(Imaginary, Real);

        public static ComplexNumber FromPolarCoordinates(double magnitude, double phase)
        {
            return new ComplexNumber(magnitude * Math.Cos(phase), magnitude * Math.Sin(phase));
        }

        public static ComplexNumber operator +(ComplexNumber a, ComplexNumber b)
        {
            return new ComplexNumber(a.Real + b.Real, a.Imaginary + b.Imaginary);
        }

        public static ComplexNumber operator -(ComplexNumber a, ComplexNumber b)
        {
            return new ComplexNumber(a.Real - b.Real, a.Imaginary - b.Imaginary);
        }

        public static ComplexNumber operator *(ComplexNumber a, ComplexNumber b)
        {
            return new ComplexNumber(
                a.Real * b.Real - a.Imaginary * b.Imaginary,
                a.Real * b.Imaginary + a.Imaginary * b.Real
            );
        }

        public static ComplexNumber operator /(ComplexNumber a, ComplexNumber b)
        {
            double denominator = b.Real * b.Real + b.Imaginary * b.Imaginary;
            return new ComplexNumber(
                (a.Real * b.Real + a.Imaginary * b.Imaginary) / denominator,
                (a.Imaginary * b.Real - a.Real * b.Imaginary) / denominator
            );
        }

        public static ComplexNumber operator *(ComplexNumber a, double b)
        {
            return new ComplexNumber(a.Real * b, a.Imaginary * b);
        }

        public static ComplexNumber Conjugate(ComplexNumber a)
        {
            return new ComplexNumber(a.Real, -a.Imaginary);
        }

        public static readonly ComplexNumber Zero = new ComplexNumber(0, 0);
    }

    // Helper class to define and manage sensor parameters
    public class SensorParameterConfig
    {
        public string Name { get; set; }
        public double CurrentValue { get; set; }
        public int TrackBarMin { get; set; }
        public int TrackBarMax { get; set; }
        public int TrackBarDefault { get; set; }
        public double ScalingFactor { get; set; }
        public string ValueFormat { get; set; }

        public Label ParameterLabel { get; set; }
        public TrackBar ParameterTrackBar { get; set; }

        public SensorParameterConfig(string name, int tbMin, int tbMax, int tbDefault, double scalingFactor, string valueFormat)
        {
            Name = name;
            TrackBarMin = tbMin;
            TrackBarMax = tbMax;
            TrackBarDefault = tbDefault;
            ScalingFactor = scalingFactor;
            CurrentValue = tbDefault * scalingFactor;
            ValueFormat = valueFormat;
        }

        public void UpdateValueFromTrackBar()
        {
            CurrentValue = ParameterTrackBar.Value * ScalingFactor;
            ParameterLabel.Text = $"{Name}: {CurrentValue.ToString(ValueFormat)}";
        }
    }

    public partial class MainForm : Form
    {
        private Random random = new Random();
        private const double SampleRate = 1000.0;
        private const double SimulationDuration = 1.0;
        private int NumSamples = (int)(SampleRate * SimulationDuration);

        // --- Deklarasi Kontrol UI Umum ---
        private TableLayoutPanel tableLayoutPanelMain;
        private TableLayoutPanel scrollableSensorContainer;

        // GroupBoxes untuk 5 sensor
        private GroupBox groupBoxGyro;
        private GroupBox groupBoxTemperature;
        private GroupBox groupBoxForce;
        private GroupBox groupBoxVibration;
        private GroupBox groupBoxImage;
        private GroupBox groupBoxSystemPlots;

        // Labels Rumus
        private Label lblGyroFormula;
        private Label lblTemperatureFormula;
        private Label lblForceFormula;
        private Label lblVibrationFormula;
        private Label lblImageFormula;

        // Labels for S-Domain and Z-Domain formulas
        private Label lblSDomainFormula;
        private Label lblZDomainFormula;

        // Charts untuk setiap sensor
        private Chart chartGyroTime;
        private Chart chartGyroFreq;
        private Chart chartTemperatureTime;
        private Chart chartTemperatureFreq;
        private Chart chartForceTime;
        private Chart chartForceFreq;
        private Chart chartVibrationTime;
        private Chart chartVibrationFreq;
        private Chart chartImageTime;
        private Chart chartImageFreq;

        // Charts Sistem
        private Chart chartSDomain;
        private Chart chartZDomain;
        // PictureBox untuk gambar 3D di atas S-Domain
        private PictureBox pictureBox3DModel;

        // [REMOVED] PictureBox pictureBoxPZPlot removed here

        // List parameter untuk setiap sensor
        private List<SensorParameterConfig> _gyroParams;
        private List<SensorParameterConfig> _temperatureParams;
        private List<SensorParameterConfig> _forceParams;
        private List<SensorParameterConfig> _vibrationParams;
        private List<SensorParameterConfig> _imageParams;

        public MainForm()
        {
            InitializeComponent();
            SetupUIAndChartsProgrammatically();

            UpdateGyroSensor();
            UpdateTemperatureSensor();
            UpdateForceSensor();
            UpdateVibrationSensor();
            UpdateImageSensor();
            UpdateSystemPlots();
        }

        private void InitializeComponent()
        {
            this.Text = "Forcebite Analyzer - 5 Sensor Dental Analysis System";
            this.Size = new Size(1800, 1000);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AutoScroll = false;
        }

        private void SetupUIAndChartsProgrammatically()
        {
            // --- 1. TableLayoutPanel Utama ---
            tableLayoutPanelMain = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(5)
            };

            // Kolom Kiri (Sensor): 80%, Kolom Kanan (Sistem): 20%
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80F));
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            this.Controls.Add(tableLayoutPanelMain);

            // --- 1.1. Container Scrollable untuk Sensor ---
            scrollableSensorContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                AutoScroll = true,
                Padding = new Padding(0, 0, 20, 0)
            };
            tableLayoutPanelMain.Controls.Add(scrollableSensorContainer, 0, 0);

            // --- 2. Inisialisasi Parameter untuk 5 Sensor ---
            _gyroParams = new List<SensorParameterConfig>
            {
                new SensorParameterConfig("Sensitivity", 100, 500, 250, 1.0, "F0"),
                new SensorParameterConfig("Bias", -100, 100, 0, 0.1, "F1"),
                new SensorParameterConfig("Noise", 0, 100, 10, 0.01, "F2"),
                new SensorParameterConfig("Rotation Event", 0, 100, 20, 0.01, "F2")
            };

            _temperatureParams = new List<SensorParameterConfig>
            {
                new SensorParameterConfig("Base Temp", 200, 400, 250, 0.1, "F1"),
                new SensorParameterConfig("Fluctuation", 0, 100, 10, 0.1, "F1"),
                new SensorParameterConfig("LSB Value", 1, 100, 16, 0.001, "F3"),
                new SensorParameterConfig("Noise", 0, 50, 5, 0.01, "F2")
            };

            _forceParams = new List<SensorParameterConfig>
            {
                new SensorParameterConfig("V_REF", 1, 50, 10, 0.1, "F1"),
                new SensorParameterConfig("Gain", 100, 1000, 500, 1.0, "F0"),
                new SensorParameterConfig("Spike Chance", 0, 100, 15, 0.01, "F2"),
                new SensorParameterConfig("Spike Magnitude", 0, 100, 30, 0.1, "F1")
            };

            _vibrationParams = new List<SensorParameterConfig>
            {
                new SensorParameterConfig("Amplitude", 1, 100, 30, 1.0, "F0"),
                new SensorParameterConfig("Base Frequency", 1, 50, 20, 0.1, "F1"),
                new SensorParameterConfig("Frequency Var", 0, 100, 15, 0.1, "F1"),
                new SensorParameterConfig("Scale Factor", 1, 100, 10, 0.001, "F3")
            };

            _imageParams = new List<SensorParameterConfig>
            {
                new SensorParameterConfig("Irradiance", 1, 100, 50, 0.1, "F1"),
                new SensorParameterConfig("Exposure Time", 1, 100, 10, 0.01, "F2"),
                new SensorParameterConfig("Quantum Eff", 10, 100, 70, 0.01, "F2"),
                new SensorParameterConfig("Digital Gain", 1, 50, 10, 0.1, "F1"),
                new SensorParameterConfig("Conversion Gain", 1, 100, 50, 0.01, "F2")
            };

            // --- 3. Setup 5 Sensor GroupBoxes ---
            SetupSensorGroupBox(scrollableSensorContainer, out groupBoxGyro, out lblGyroFormula, _gyroParams,
                out chartGyroTime, out chartGyroFreq, "Gyroscope (RAK12025 / I3G4250D)", tbGyro_Scroll);

            SetupSensorGroupBox(scrollableSensorContainer, out groupBoxTemperature, out lblTemperatureFormula, _temperatureParams,
                out chartTemperatureTime, out chartTemperatureFreq, "Temperature Sensor (DS18B20)", tbTemperature_Scroll);

            SetupSensorGroupBox(scrollableSensorContainer, out groupBoxForce, out lblForceFormula, _forceParams,
                out chartForceTime, out chartForceFreq, "Force Sensor (FlexiForce A201)", tbForce_Scroll);

            SetupSensorGroupBox(scrollableSensorContainer, out groupBoxVibration, out lblVibrationFormula, _vibrationParams,
                out chartVibrationTime, out chartVibrationFreq, "Vibration Sensor (IO-Link RMS)", tbVibration_Scroll);

            SetupSensorGroupBox(scrollableSensorContainer, out groupBoxImage, out lblImageFormula, _imageParams,
                out chartImageTime, out chartImageFreq, "Image Sensor (Mira220 Global Shutter)", tbImage_Scroll);

            // --- 4. Setup System Plots GroupBox (Kolom Kanan Utama) ---
            groupBoxSystemPlots = new GroupBox
            {
                Text = "System Poles & Zeros",
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            tableLayoutPanelMain.Controls.Add(groupBoxSystemPlots, 1, 0);

            // [CHANGED] RowCount menjadi 5 (sebelumnya 6, karena gambar pz dihapus)
            // Baris 0: Gambar 3D
            // Baris 1: Rumus S
            // Baris 2: S-Domain Chart
            // Baris 3: Rumus Z
            // Baris 4: Z-Domain Chart
            TableLayoutPanel systemChartsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 1,
                Padding = new Padding(5)
            };
            systemChartsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 140F)); // Baris 0: Gambar 3D
            systemChartsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));  // Baris 1: Rumus S-Domain
            systemChartsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));   // Baris 2: S-Domain chart
            systemChartsPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));  // Baris 3: Rumus Z-Domain
            systemChartsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));   // Baris 4: Z-Domain chart
            groupBoxSystemPlots.Controls.Add(systemChartsPanel);

            // PictureBox untuk gambar 3D di Baris 0
            pictureBox3DModel = new PictureBox
            {
                BackColor = Color.LightGray,
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Fill,
                Margin = new Padding(5)
            };
            systemChartsPanel.Controls.Add(pictureBox3DModel, 0, 0);

            // [REMOVED] Bagian setup pictureBoxPZPlot dihapus sesuai permintaan

            // Label untuk rumus S-Domain (di Baris 1)
            lblSDomainFormula = new Label
            {
                Text = "H(s) Formula:",
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(5, 5, 5, 0),
                Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Regular)
            };
            systemChartsPanel.Controls.Add(lblSDomainFormula, 0, 1);

            chartSDomain = new Chart { Dock = DockStyle.Fill };
            ConfigureChart(chartSDomain, "S-Domain (Poles & Zeros)", "Real Axis", "Imaginary Axis");
            systemChartsPanel.Controls.Add(chartSDomain, 0, 2);

            // Label untuk rumus Z-Domain (di Baris 3)
            lblZDomainFormula = new Label
            {
                Text = "H(z) Formula:",
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(5, 5, 5, 0),
                Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Regular)
            };
            systemChartsPanel.Controls.Add(lblZDomainFormula, 0, 3);

            chartZDomain = new Chart { Dock = DockStyle.Fill };
            ConfigureChart(chartZDomain, "Z-Domain (Poles & Zeros)", "Real Axis", "Imaginary Axis");
            systemChartsPanel.Controls.Add(chartZDomain, 0, 4);

            ConfigureSystemChartScales();

            // --- Pemanggilan Resource Gambar ---
            // 1. Gambar SPS_2
            try
            {
                var imageResource = Bismillah_FINAL_ForceBite.Properties.Resources.SPS_2;
                if (imageResource != null)
                {
                    pictureBox3DModel.Image = imageResource;
                }
                else
                {
                    MessageBox.Show("Resource gambar 'SPS_2' ditemukan tetapi kosong.", "Resource Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Gambar 'SPS_2' tidak ditemukan di resources. Pastikan Anda telah menambahkannya.", "Error Resource", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // [REMOVED] Bagian loading Resource PolesZerosCombined dihapus
        }

        private void ConfigureSystemChartScales()
        {
            // --- S-Domain Chart Configuration ---
            ChartArea sDomainArea = chartSDomain.ChartAreas[0];
            sDomainArea.AxisX.Minimum = -3;
            sDomainArea.AxisX.Maximum = 3;
            sDomainArea.AxisY.Minimum = -3;
            sDomainArea.AxisY.Maximum = 3;
            sDomainArea.AxisX.Interval = 1;
            sDomainArea.AxisY.Interval = 1;
            sDomainArea.AxisX.MajorGrid.LineColor = Color.LightGray;
            sDomainArea.AxisY.MajorGrid.LineColor = Color.LightGray;
            sDomainArea.AxisX.MinorGrid.Enabled = true;
            sDomainArea.AxisY.MinorGrid.Enabled = true;
            sDomainArea.AxisX.MinorGrid.LineColor = Color.LightGray;
            sDomainArea.AxisY.MinorGrid.LineColor = Color.LightGray;

            // Make S-Domain chart proportional
            sDomainArea.AxisX.ScaleView.Zoomable = false;
            sDomainArea.AxisY.ScaleView.Zoomable = false;

            sDomainArea.InnerPlotPosition.Auto = false;
            sDomainArea.InnerPlotPosition.Width = 80;
            sDomainArea.InnerPlotPosition.Height = 80;
            sDomainArea.InnerPlotPosition.X = 10;
            sDomainArea.InnerPlotPosition.Y = 10;


            // --- Z-Domain Chart Configuration ---
            ChartArea zDomainArea = chartZDomain.ChartAreas[0];
            zDomainArea.AxisX.Minimum = -1.5;
            zDomainArea.AxisX.Maximum = 1.5;
            zDomainArea.AxisY.Minimum = -1.5;
            zDomainArea.AxisY.Maximum = 1.5;
            zDomainArea.AxisX.Interval = 0.5;
            zDomainArea.AxisY.Interval = 0.5;
            zDomainArea.AxisX.MajorGrid.LineColor = Color.LightGray;
            zDomainArea.AxisY.MajorGrid.LineColor = Color.LightGray;
            zDomainArea.AxisX.MinorGrid.Enabled = true;
            zDomainArea.AxisY.MinorGrid.Enabled = true;
            zDomainArea.AxisX.MinorGrid.LineColor = Color.LightGray;
            zDomainArea.AxisY.MinorGrid.LineColor = Color.LightGray;

            // Make Z-Domain chart proportional
            zDomainArea.AxisX.ScaleView.Zoomable = false;
            zDomainArea.AxisY.ScaleView.Zoomable = false;

            zDomainArea.InnerPlotPosition.Auto = false;
            zDomainArea.InnerPlotPosition.Width = 80;
            zDomainArea.InnerPlotPosition.Height = 80;
            zDomainArea.InnerPlotPosition.X = 10;
            zDomainArea.InnerPlotPosition.Y = 10;
        }

        // --- Metode Layout Sensor ---
        private void SetupSensorGroupBox(TableLayoutPanel parentPanel, out GroupBox groupBox, out Label formulaLabel,
                                         List<SensorParameterConfig> paramConfigs,
                                         out Chart chartTime, out Chart chartFreq,
                                         string groupText, EventHandler scrollHandler)
        {
            groupBox = new GroupBox
            {
                Text = groupText,
                Dock = DockStyle.Top,
                Padding = new Padding(5)
            };
            parentPanel.Controls.Add(groupBox);

            // 1. Main Layout di dalam GroupBox: 2 Kolom (Kiri: Kontrol, Kanan: Grafik)
            TableLayoutPanel splitPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0)
            };
            splitPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380F));
            splitPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            groupBox.Controls.Add(splitPanel);

            // 2. Panel Kiri: Kontrol (Rumus + Slider)
            TableLayoutPanel leftControlPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1 + paramConfigs.Count + 1,
                Padding = new Padding(0)
            };
            splitPanel.Controls.Add(leftControlPanel, 0, 0);

            leftControlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            leftControlPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // --- BARIS 0: RUMUS ---
            int formulaHeight = 80;
            leftControlPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, formulaHeight));

            formulaLabel = new Label
            {
                Text = "Rumus:",
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(0, 5, 0, 0)
            };
            leftControlPanel.Controls.Add(formulaLabel, 0, 0);
            leftControlPanel.SetColumnSpan(formulaLabel, 2);

            // --- BARIS 1..N: PARAMETER ---
            int paramHeight = 40;
            int currentRow = 1;
            foreach (var param in paramConfigs)
            {
                leftControlPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, paramHeight));

                // Label Parameter
                param.ParameterLabel = new Label
                {
                    Text = $"{param.Name}: ...",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Margin = new Padding(0, 0, 5, 0)
                };
                leftControlPanel.Controls.Add(param.ParameterLabel, 0, currentRow);

                // TrackBar Parameter
                param.ParameterTrackBar = new TrackBar
                {
                    Minimum = param.TrackBarMin,
                    Maximum = param.TrackBarMax,
                    SmallChange = 1,
                    LargeChange = Math.Max(1, (param.TrackBarMax - param.TrackBarMin) / 10),
                    TickFrequency = Math.Max(1, (param.TrackBarMax - param.TrackBarMin) / 10),
                    Value = param.TrackBarDefault,
                    Dock = DockStyle.Fill,
                    Margin = new Padding(0)
                };
                param.ParameterTrackBar.Scroll += scrollHandler;
                leftControlPanel.Controls.Add(param.ParameterTrackBar, 1, currentRow);

                param.UpdateValueFromTrackBar();
                currentRow++;
            }
            leftControlPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // 3. Panel Kanan: Grafik (Berdampingan)
            TableLayoutPanel rightChartPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0)
            };
            splitPanel.Controls.Add(rightChartPanel, 1, 0);

            rightChartPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            rightChartPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            chartTime = new Chart { Dock = DockStyle.Fill, Margin = new Padding(5) };
            ConfigureChart(chartTime, $"{groupText} - Time Domain", "Time (s)", "Amplitude");
            rightChartPanel.Controls.Add(chartTime, 0, 0);

            chartFreq = new Chart { Dock = DockStyle.Fill, Margin = new Padding(5) };
            ConfigureChart(chartFreq, $"{groupText} - Frequency Domain", "Frequency (Hz)", "Magnitude");
            rightChartPanel.Controls.Add(chartFreq, 1, 0);

            // 4. Menghitung Tinggi GroupBox
            int contentInputHeight = 80 + (paramConfigs.Count * 40) + 20;
            int desiredChartHeight = 350;
            int finalHeight = Math.Max(contentInputHeight, desiredChartHeight);
            groupBox.Height = finalHeight;
        }

        // --- Metode Standard Chart/FFT/Noise ---
        private void ConfigureChart(Chart chart, string title, string xAxisTitle, string yAxisTitle)
        {
            chart.Series.Clear();
            chart.ChartAreas.Clear();
            chart.Titles.Clear();

            ChartArea chartArea = new ChartArea();
            chart.ChartAreas.Add(chartArea);

            chart.Titles.Add(title);
            chart.ChartAreas[0].AxisX.Title = xAxisTitle;
            chart.ChartAreas[0].AxisY.Title = yAxisTitle;
            chart.ChartAreas[0].AxisX.LabelStyle.Format = "{F2}";
            chart.ChartAreas[0].AxisY.LabelStyle.Format = "{F2}";
            chart.ChartAreas[0].AxisX.IsStartedFromZero = false;
            chart.ChartAreas[0].AxisY.IsStartedFromZero = false;

            Series series = new Series
            {
                Name = "Data",
                ChartType = SeriesChartType.Line,
                Color = Color.DodgerBlue,
                XValueType = ChartValueType.Double,
                YValueType = ChartValueType.Double
            };
            chart.Series.Add(series);
        }

        private void UpdatePlot(Chart chart, double[] xData, double[] yData, string seriesName = "Data", SeriesChartType chartType = SeriesChartType.Line, Color? color = null)
        {
            if (chart.InvokeRequired)
            {
                chart.Invoke(new MethodInvoker(delegate { UpdatePlot(chart, xData, yData, seriesName, chartType, color); }));
                return;
            }
            if (!chart.ChartAreas.Any()) chart.ChartAreas.Add(new ChartArea());

            Series series;
            if (chart.Series.Any(s => s.Name == seriesName))
            {
                series = chart.Series[seriesName];
                series.Points.Clear();
            }
            else
            {
                series = new Series(seriesName);
                chart.Series.Add(series);
            }

            series.ChartType = chartType;
            series.Color = color ?? Color.DodgerBlue;

            for (int i = 0; i < xData.Length; i++) series.Points.AddXY(xData[i], yData[i]);

            chart.ChartAreas[0].RecalculateAxesScale();
            chart.Invalidate();
        }

        private (double[] frequencies, double[] magnitudes) CalculateFFT(double[] timeDomainData)
        {
            int N = timeDomainData.Length;
            int N_fft = 1;
            while (N_fft < N) N_fft <<= 1;

            ComplexNumber[] complexData = new ComplexNumber[N_fft];
            for (int i = 0; i < N; i++) complexData[i] = new ComplexNumber(timeDomainData[i], 0);
            for (int i = N; i < N_fft; i++) complexData[i] = ComplexNumber.Zero;

            // Simple FFT implementation
            FFT(complexData, N_fft);

            double[] frequencies = new double[N_fft / 2];
            double[] magnitudes = new double[N_fft / 2];

            for (int i = 0; i < N_fft / 2; i++)
            {
                frequencies[i] = i * SampleRate / N_fft;
                magnitudes[i] = complexData[i].Magnitude / N_fft;
            }
            return (frequencies, magnitudes);
        }

        private void FFT(ComplexNumber[] data, int n)
        {
            if (n <= 1) return;

            // Split into even and odd
            ComplexNumber[] even = new ComplexNumber[n / 2];
            ComplexNumber[] odd = new ComplexNumber[n / 2];

            for (int i = 0; i < n / 2; i++)
            {
                even[i] = data[2 * i];
                odd[i] = data[2 * i + 1];
            }

            // Recursive FFT
            FFT(even, n / 2);
            FFT(odd, n / 2);

            // Combine
            for (int k = 0; k < n / 2; k++)
            {
                double angle = -2 * Math.PI * k / n;
                ComplexNumber t = new ComplexNumber(Math.Cos(angle), Math.Sin(angle)) * odd[k];

                data[k] = even[k] + t;
                data[k + n / 2] = even[k] - t;
            }
        }

        private double GenerateNoise(double amplitude) => amplitude * (random.NextDouble() * 2 - 1);

        // --- Update Logic untuk 5 Sensor ---
        private void tbGyro_Scroll(object sender, EventArgs e) => UpdateGyroSensor();
        private void UpdateGyroSensor()
        {
            foreach (var param in _gyroParams) param.UpdateValueFromTrackBar();

            double sensitivity = _gyroParams.First(p => p.Name == "Sensitivity").CurrentValue;
            double bias = _gyroParams.First(p => p.Name == "Bias").CurrentValue;
            double noise = _gyroParams.First(p => p.Name == "Noise").CurrentValue;
            double rotationChance = _gyroParams.First(p => p.Name == "Rotation Event").CurrentValue;

            lblGyroFormula.Text = "Rumus: ω_terukur (dps) = (D_Gyro - B) / S\n" +
                                   $"S = {sensitivity:F0} LSB/dps, B = {bias:F1} dps\n" +
                                   $"D_Gyro = S × ω + B";

            double[] timeData = new double[NumSamples];
            double[] angularVelocity = new double[NumSamples];
            double timeStep = SimulationDuration / NumSamples;

            double currentVelocity = 0;
            for (int i = 0; i < NumSamples; i++)
            {
                double t = i * timeStep;

                // Simulate rotation events
                if (random.NextDouble() < rotationChance / 100.0)
                {
                    currentVelocity = (random.NextDouble() - 0.5) * 50;
                }
                else
                {
                    currentVelocity *= 0.95;
                }

                double digitalOutput = sensitivity * currentVelocity + bias;
                angularVelocity[i] = digitalOutput + GenerateNoise(noise);
                timeData[i] = t;
            }
            UpdatePlot(chartGyroTime, timeData, angularVelocity);
            var fft = CalculateFFT(angularVelocity);
            UpdatePlot(chartGyroFreq, fft.frequencies, fft.magnitudes);
        }

        private void tbTemperature_Scroll(object sender, EventArgs e) => UpdateTemperatureSensor();
        private void UpdateTemperatureSensor()
        {
            foreach (var param in _temperatureParams) param.UpdateValueFromTrackBar();

            double baseTemp = _temperatureParams.First(p => p.Name == "Base Temp").CurrentValue;
            double fluctuation = _temperatureParams.First(p => p.Name == "Fluctuation").CurrentValue;
            double lsbValue = _temperatureParams.First(p => p.Name == "LSB Value").CurrentValue;
            double noise = _temperatureParams.First(p => p.Name == "Noise").CurrentValue;

            lblTemperatureFormula.Text = "Rumus: T(°C) = D_T × LSB\n" +
                                         $"LSB = {lsbValue:F3} °C/LSB\n" +
                                         $"Base Temp = {baseTemp:F1}°C, Fluctuation = {fluctuation:F1}°C";

            double[] timeData = new double[NumSamples];
            double[] temperature = new double[NumSamples];
            double timeStep = SimulationDuration / NumSamples;

            for (int i = 0; i < NumSamples; i++)
            {
                double t = i * timeStep;
                double tempVariation = fluctuation * Math.Sin(2 * Math.PI * 0.1 * t);
                double currentTemp = baseTemp + tempVariation;
                double digitalOutput = currentTemp / lsbValue;
                temperature[i] = digitalOutput + GenerateNoise(noise);
                timeData[i] = t;
            }
            UpdatePlot(chartTemperatureTime, timeData, temperature);
            var fft = CalculateFFT(temperature);
            UpdatePlot(chartTemperatureFreq, fft.frequencies, fft.magnitudes);
        }

        private void tbForce_Scroll(object sender, EventArgs e) => UpdateForceSensor();
        private void UpdateForceSensor()
        {
            foreach (var param in _forceParams) param.UpdateValueFromTrackBar();

            double vref = _forceParams.First(p => p.Name == "V_REF").CurrentValue;
            double gain = _forceParams.First(p => p.Name == "Gain").CurrentValue;
            double spikeChance = _forceParams.First(p => p.Name == "Spike Chance").CurrentValue;
            double spikeMagnitude = _forceParams.First(p => p.Name == "Spike Magnitude").CurrentValue;

            lblForceFormula.Text = "Rumus: V_OUT = -V_REF × (R_F / R_S(F))\n" +
                                   $"V_REF = {vref:F1}V, Gain = {gain:F0}\n" +
                                   $"R_F/R_S ≈ {gain:F0}, F ∝ 1/R_S";

            double[] timeData = new double[NumSamples];
            double[] voltageOut = new double[NumSamples];
            double timeStep = SimulationDuration / NumSamples;

            for (int i = 0; i < NumSamples; i++)
            {
                double t = i * timeStep;
                double baseForce = Math.Abs(Math.Sin(2 * Math.PI * 2 * t));

                if (random.NextDouble() < spikeChance / 100.0)
                {
                    baseForce += random.NextDouble() * spikeMagnitude / 10.0;
                }

                double resistance = 1.0 / (baseForce + 0.1);
                double vout = -vref * gain * resistance;
                voltageOut[i] = vout;
                timeData[i] = t;
            }
            UpdatePlot(chartForceTime, timeData, voltageOut);
            var fft = CalculateFFT(voltageOut);
            UpdatePlot(chartForceFreq, fft.frequencies, fft.magnitudes);
        }

        private void tbVibration_Scroll(object sender, EventArgs e) => UpdateVibrationSensor();
        private void UpdateVibrationSensor()
        {
            foreach (var param in _vibrationParams) param.UpdateValueFromTrackBar();

            double amplitude = _vibrationParams.First(p => p.Name == "Amplitude").CurrentValue;
            double baseFreq = _vibrationParams.First(p => p.Name == "Base Frequency").CurrentValue;
            double freqVar = _vibrationParams.First(p => p.Name == "Frequency Var").CurrentValue;
            double scaleFactor = _vibrationParams.First(p => p.Name == "Scale Factor").CurrentValue;

            lblVibrationFormula.Text = "Rumus: v_RMS,i (mm/s) = D_i × K_v\n" +
                                       $"K_v = {scaleFactor:F3} mm/s/LSB\n" +
                                       $"Amplitude = {amplitude:F0} mm/s, Base Freq = {baseFreq:F1} Hz";

            double[] timeData = new double[NumSamples];
            double[] vibration = new double[NumSamples];
            double timeStep = SimulationDuration / NumSamples;

            for (int i = 0; i < NumSamples; i++)
            {
                double t = i * timeStep;
                double frequency = baseFreq + freqVar * Math.Sin(2 * Math.PI * 0.5 * t);
                double vibrationSignal = amplitude * Math.Sin(2 * Math.PI * frequency * t);
                double digitalOutput = vibrationSignal / scaleFactor;
                vibration[i] = digitalOutput;
                timeData[i] = t;
            }
            UpdatePlot(chartVibrationTime, timeData, vibration);
            var fft = CalculateFFT(vibration);
            UpdatePlot(chartVibrationFreq, fft.frequencies, fft.magnitudes);
        }

        private void tbImage_Scroll(object sender, EventArgs e) => UpdateImageSensor();
        private void UpdateImageSensor()
        {
            foreach (var param in _imageParams) param.UpdateValueFromTrackBar();

            double irradiance = _imageParams.First(p => p.Name == "Irradiance").CurrentValue;
            double exposureTime = _imageParams.First(p => p.Name == "Exposure Time").CurrentValue;
            double quantumEff = _imageParams.First(p => p.Name == "Quantum Eff").CurrentValue;
            double digitalGain = _imageParams.First(p => p.Name == "Digital Gain").CurrentValue;
            double conversionGain = _imageParams.First(p => p.Name == "Conversion Gain").CurrentValue;

            lblImageFormula.Text = "Rumus: D_Pixel ∝ E × T_exp × QE\n" +
                                   $"D_Pixel ≈ Gain_Digital × (E × T_exp × QE × CG) + Offset\n" +
                                   $"E = {irradiance:F1}, T_exp = {exposureTime:F2}s\n" +
                                   $"QE = {quantumEff:F2}, CG = {conversionGain:F2}";

            double[] timeData = new double[NumSamples];
            double[] pixelValue = new double[NumSamples];
            double timeStep = SimulationDuration / NumSamples;

            for (int i = 0; i < NumSamples; i++)
            {
                double t = i * timeStep;
                double lightVariation = 0.2 * Math.Sin(2 * Math.PI * 1.0 * t);
                double currentIrradiance = irradiance * (1 + lightVariation);

                double pixel = digitalGain * (currentIrradiance * exposureTime * quantumEff * conversionGain) + 10;
                pixelValue[i] = pixel;
                timeData[i] = t;
            }
            UpdatePlot(chartImageTime, timeData, pixelValue);
            var fft = CalculateFFT(pixelValue);
            UpdatePlot(chartImageFreq, fft.frequencies, fft.magnitudes);
        }

        private void UpdateSystemPlots()
        {
            // Poles and Zeros for S-Domain (Continuous)
            ComplexNumber[] sPoles = {
                new ComplexNumber(-0.5, 1.5),
                new ComplexNumber(-0.5, -1.5),
                new ComplexNumber(-1.0, 0)
            };
            ComplexNumber[] sZeros = {
                new ComplexNumber(0, 0),
                new ComplexNumber(-0.8, 0)
            };
            PlotPolesAndZeros(chartSDomain, sPoles, sZeros, false);
            UpdateSDomainFormula(sPoles, sZeros);

            // Poles and Zeros for Z-Domain (Discrete)
            ComplexNumber[] zPoles = {
                new ComplexNumber(0.8, 0.4),
                new ComplexNumber(0.8, -0.4),
                new ComplexNumber(0.5, 0)
            };
            ComplexNumber[] zZeros = {
                new ComplexNumber(0.1, 0),
                new ComplexNumber(-0.5, 0)
            };
            PlotPolesAndZeros(chartZDomain, zPoles, zZeros, true);
            UpdateZDomainFormula(zPoles, zZeros);
        }

        private void UpdateSDomainFormula(ComplexNumber[] poles, ComplexNumber[] zeros)
        {
            // K (gain) diasumsikan 1 untuk penyederhanaan tampilan rumus
            string numSimplified = "s(s + 0.8)"; // From s(s - 0)(s - (-0.8))
            string denSimplified = "(s^2 + s + 2.5)(s + 1.0)"; // From (s + 0.5 - j1.5)(s + 0.5 + j1.5)(s + 1.0)

            lblSDomainFormula.Text = "H(s) = K * Numerator / Denominator\n" +
                                     $"Numerator: {numSimplified}\n" +
                                     $"Denominator: {denSimplified}";
        }

        private void UpdateZDomainFormula(ComplexNumber[] poles, ComplexNumber[] zeros)
        {
            // K' (gain) diasumsikan 1 untuk penyederhanaan tampilan rumus
            string numSimplified = "(z - 0.1)(z + 0.5)";
            string denSimplified = "(z^2 - 1.6z + 0.8)(z - 0.5)";

            lblZDomainFormula.Text = "H(z) = K' * Numerator / Denominator\n" +
                                     $"Numerator: {numSimplified}\n" +
                                     $"Denominator: {denSimplified}";
        }


        private void PlotPolesAndZeros(Chart chart, ComplexNumber[] poles, ComplexNumber[] zeros, bool isZ)
        {
            if (chart.InvokeRequired)
            {
                chart.Invoke(new MethodInvoker(() => PlotPolesAndZeros(chart, poles, zeros, isZ)));
                return;
            }
            chart.Series.Clear();

            // Plot poles as red X
            var pSeries = new Series("Poles")
            {
                ChartType = SeriesChartType.Point,
                MarkerStyle = MarkerStyle.Cross,
                MarkerSize = 10,
                Color = Color.Red
            };
            foreach (var p in poles) pSeries.Points.AddXY(p.Real, p.Imaginary);
            chart.Series.Add(pSeries);

            // Plot zeros as green circles
            var zSeries = new Series("Zeros")
            {
                ChartType = SeriesChartType.Point,
                MarkerStyle = MarkerStyle.Circle,
                MarkerSize = 8,
                Color = Color.Green
            };
            foreach (var z in zeros) zSeries.Points.AddXY(z.Real, z.Imaginary);
            chart.Series.Add(zSeries);

            // Draw unit circle for Z-domain
            if (isZ)
            {
                var circle = new Series("Unit Circle")
                {
                    ChartType = SeriesChartType.Line,
                    Color = Color.Gray,
                    BorderWidth = 1
                };
                for (int i = 0; i <= 360; i += 5)
                {
                    double angle = i * Math.PI / 180;
                    circle.Points.AddXY(Math.Cos(angle), Math.Sin(angle));
                }
                chart.Series.Add(circle);
            }
            chart.ChartAreas[0].RecalculateAxesScale();
        }
    }
}