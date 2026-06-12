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
using System.Diagnostics;

namespace Memory_Policy_Simulator
{
    public partial class Form1 : Form
    {
        Graphics g;
        PictureBox pbPlaceHolder;
        Bitmap bResultImage;

        public Form1()
        {
            InitializeComponent();
            this.pbPlaceHolder = new PictureBox();
            this.bResultImage = new Bitmap(2048, 2048);
            this.pbPlaceHolder.Size = new Size(2048, 2048);
            g = Graphics.FromImage(this.bResultImage);
            pbPlaceHolder.Image = this.bResultImage;
            this.pImage.Controls.Add(this.pbPlaceHolder);
            this.tbConsole.Multiline = true;
            this.tbConsole.ScrollBars = ScrollBars.Vertical;
        }

        private void DrawBase(Core core, int windowSize, int dataLength)
        {
            var psudoQueue = new Queue<char>();

            g.Clear(Color.Black);

            for ( int i = 0; i < dataLength; i++ )
            {
                int psudoCursor = core.pageHistory[i].loc;
                char data = core.pageHistory[i].data;
                Page.STATUS status = core.pageHistory[i].status;

                switch ( status )
                {
                    case Page.STATUS.PAGEFAULT:
                        psudoQueue.Enqueue(data);
                        break;
                    case Page.STATUS.MIGRATION:
                        psudoQueue.Dequeue();
                        psudoQueue.Enqueue(data);
                        break;
                }

                for ( int j = 0; j <= windowSize; j++)
                {
                    if (j == 0)
                    {
                        DrawGridText(i, j, data);
                    }
                    else
                    {
                        DrawGrid(i, j);
                    }
                }

                DrawGridHighlight(i, psudoCursor, status);
                int depth = 1;

                foreach ( char t in psudoQueue )
                {
                    DrawGridText(i, depth++, t);
                }
            }
        }


        private void DrawGrid(int x, int y)
        {
            int gridSize = 30;
            int gridSpace = 5;
            int gridBaseX = x * gridSize;
            int gridBaseY = y * gridSize;

            g.DrawRectangle(new Pen(Color.White), new Rectangle(
                gridBaseX + (x * gridSpace),
                gridBaseY,
                gridSize,
                gridSize
                ));
        }

        private void DrawGridHighlight(int x, int y, Page.STATUS status)
        {
            int gridSize = 30;
            int gridSpace = 5;
            int gridBaseX = x * gridSize;
            int gridBaseY = y * gridSize;

            SolidBrush highlighter = new SolidBrush(Color.LimeGreen);

            switch (status)
            {
                case Page.STATUS.HIT:
                    break;
                case Page.STATUS.MIGRATION:
                    highlighter.Color = Color.Purple;
                    break;
                case Page.STATUS.PAGEFAULT:
                    highlighter.Color = Color.Red;
                    break;
            }

            g.FillRectangle(highlighter, new Rectangle(
                gridBaseX + (x * gridSpace),
                gridBaseY,
                gridSize,
                gridSize
                ));
        }

        private void DrawGridText(int x, int y, char value)
        {
            int gridSize = 30;
            int gridSpace = 5;
            int gridBaseX = x * gridSize;
            int gridBaseY = y * gridSize;

            g.DrawString(
                value.ToString(), 
                new Font(FontFamily.GenericMonospace, 8), 
                new SolidBrush(Color.White), 
                new PointF(
                    gridBaseX + (x * gridSpace) + gridSize / 3,
                    gridBaseY + gridSize / 4));
        }

        private void btnOperate_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this.tbQueryString.Text) || 
                    string.IsNullOrWhiteSpace(this.tbWindowSize.Text))
                {
                    MessageBox.Show("참조 문자열과 프레임 크기를 입력하세요.", "입력 오류");
                    return;
                }

                string referenceString = this.tbQueryString.Text.Trim();
                int frameSize = int.Parse(this.tbWindowSize.Text);
                string selectedPolicy = this.comboBox1.SelectedItem.ToString();

                if (frameSize <= 0 || frameSize > 100)
                {
                    MessageBox.Show("프레임 크기는 1~100 사이여야 합니다.", "입력 오류");
                    return;
                }

                SimulationResult result = RunSimulation(referenceString, frameSize, selectedPolicy);

                DisplaySimulationResult(result);

                string[] policies = { "FIFO", "LRU", "LFU", "PAR" };
                List<SimulationResult> allResults = new List<SimulationResult>();

                foreach (string policy in policies)
                {
                    SimulationResult res = RunSimulation(referenceString, frameSize, policy);
                    allResults.Add(res);
                }

                DisplayComparisonChart(allResults);

                this.lbPageFaultRatio.Text = 
                    $"FIFO: {allResults[0].FaultRate:F2}% | " +
                    $"LRU: {allResults[1].FaultRate:F2}% | " +
                    $"LFU: {allResults[2].FaultRate:F2}% | " +
                    $"PAR: {allResults[3].FaultRate:F2}%";
            }
            catch (FormatException)
            {
                MessageBox.Show("프레임 크기는 숫자로 입력하세요.", "입력 오류");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"오류 발생: {ex.Message}\n{ex.StackTrace}", "실행 오류");
            }
        }

        private void pbPlaceHolder_Paint(object sender, PaintEventArgs e)
        {
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void tbWindowSize_KeyDown(object sender, KeyEventArgs e)
        {

        }

        private void tbWindowSize_KeyPress(object sender, KeyPressEventArgs e)
        {
                if (!(Char.IsDigit(e.KeyChar)) && e.KeyChar != 8)
                {
                    e.Handled = true;
                }
        }

        private void btnRand_Click(object sender, EventArgs e)
        {
            Random rd = new Random();

            int count = rd.Next(5, 50);
            StringBuilder sb = new StringBuilder();

            for ( int i = 0; i < count; i++ )
            {
                sb.Append((char)rd.Next(65, 90));
            }

            this.tbQueryString.Text = sb.ToString();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            bResultImage.Save("./result.jpg");
        }

        public SimulationResult RunSimulation(string referenceString, int frameSize, string policyType)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<char> refList = referenceString.ToList();

            PageReplacementPolicy policy = null;

            switch (policyType.ToUpper())
            {
                case "FIFO":
                    policy = new FIFOPolicy(frameSize);
                    break;
                case "LRU":
                    policy = new LRUPolicy(frameSize);
                    break;
                case "LFU":
                    policy = new LFUPolicy(frameSize);
                    break;
                case "PAR":
                    policy = new PARPolicy(frameSize);
                    break;
                default:
                    throw new ArgumentException($"알 수 없는 정책: {policyType}");
            }

            SimulationResult result = policy.RunSimulation(refList);

            stopwatch.Stop();
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;

            return result;
        }

        private void DisplayComparisonChart(List<SimulationResult> results)
        {
            chart1.Series.Clear();

            Series resultChartContent = chart1.Series.Add("통계");
            resultChartContent.ChartType = SeriesChartType.Bar;
            resultChartContent.IsVisibleInLegend = true;

            foreach (var result in results)
            {
                int faultRate = (int)result.FaultRate;
                resultChartContent.Points.AddXY(result.AlgorithmName, faultRate);
                resultChartContent.Points.Last().LegendText = 
                    $"{result.AlgorithmName} - Fault: {result.FaultCount}, Rate: {result.FaultRate:F2}%";
            }

            chart1.ChartAreas[0].AxisY.Title = "Page Fault Rate (%)";
            chart1.ChartAreas[0].AxisX.Title = "정책";
        }
    }
}
