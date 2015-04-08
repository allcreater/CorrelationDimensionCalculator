using Microsoft.Win32;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;

using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.DataSources;

namespace CorrelationDimensionCalculator
{

    public abstract class BaseConverter : System.Windows.Markup.MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
    [ValueConversion(typeof(int), typeof(double))]
    public class IntToDoubleConverter : BaseConverter, IValueConverter
    {
        public double AdditiveValue { get; set; }

        public IntToDoubleConverter()
        {
            AdditiveValue = 0.0;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int srcValue = (int)value;
            return (double)srcValue + AdditiveValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ViewModel ViewModel { get; private set; }

        private IList<Point>[] m_CDresults;
        private IList<LineGraph> m_CDlineGraphs = new List<LineGraph>();
        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new ViewModel();
            DataContext = ViewModel;

            ViewModel.PseudophaseSpaceChanged += ViewModel_PseudophaseSpaceChanged;
        }

        private void ViewModel_PseudophaseSpaceChanged (object sender, ViewModel.CurveEventArgs e)
        {
            plotterPPS.RemoveUserElements();
            plotterPPS.AddLineGraph(new ObservableDataSource<Point>(e.Curve));
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void FileOpen(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog() { Filter = "Text files|*.txt|All files|*.*" };
            if (ofd.ShowDialog() == true)
            {
                ViewModel.ReadFile(ofd.FileName);

                labelFileInfo.Content = String.Format("Name: {0}\nRaw size: {1} bytes\nLength: {2} records",
                    System.IO.Path.GetFileName(ofd.FileName),
                    new FileInfo(ofd.FileName).Length,
                    ViewModel.RawData.Count
                    );
            }
        }

        private void ComputeCD(object sender, RoutedEventArgs e)
        {
            if (ViewModel.RawData == null)
                return;

            plotterCD.RemoveUserElements();
            m_CDlineGraphs.Clear();
            m_CDresults = new List<Point>[ViewModel.NumberOfDimensions];
            for (int n = 0; n < ViewModel.NumberOfDimensions; n++)
            {
                m_CDresults[n] = ViewModel.ComputeCorrelationalDimensionCurve(n + 2).ToList();
                var lineGraph = plotterCD.AddLineGraph(new ObservableDataSource<Point>(m_CDresults[n]), 1);
                m_CDlineGraphs.Add(lineGraph);
            }
            
            tabControlMain.SelectedIndex = 1;

            plotterKE.RemoveUserElements();
            for (int curveIndexA = 0, curveIndexB = 1; curveIndexB < ViewModel.NumberOfDimensions; curveIndexA++, curveIndexB++)
            {
                double firstCommonElement = Math.Max(m_CDresults[curveIndexA].First().X, m_CDresults[curveIndexB].First().X);
                int i = m_CDresults[curveIndexA].IndexOf(m_CDresults[curveIndexA].First((p) => (p.X == firstCommonElement)));
                int j = m_CDresults[curveIndexB].IndexOf(m_CDresults[curveIndexB].First((p) => (p.X == firstCommonElement)));

                var intersection = new ObservableDataSource<Point>();
                for (; i < m_CDresults[curveIndexA].Count && j < m_CDresults[curveIndexB].Count && m_CDresults[curveIndexA][i].X == m_CDresults[curveIndexB][j].X; i++, j++)
                    intersection.AppendAsync(Dispatcher, new Point(m_CDresults[curveIndexA][i].X, m_CDresults[curveIndexA][i].Y - m_CDresults[curveIndexB][j].Y));

                plotterKE.AddLineGraph(intersection, 1, String.Format("KE {0}-{1}", curveIndexA, curveIndexB));
            }
        }

        private void ComputeHurst(object sender, RoutedEventArgs e)
        {
            if (ViewModel.RawData == null)
                return;

            MessageBox.Show(String.Format("Hurst exponent is {0}", ViewModel.ComputeHurstExponent()), "Computation result");
        }

        private void plotterCD_MouseMove(object sender, MouseEventArgs e)
        {
            var mousePos = Mouse.GetPosition(plotterCD.Viewport);
            if (!plotterCD.Viewport.Output.Contains(mousePos) || m_CDresults == null)
                return;

            var pos = mousePos.ScreenToData(plotterCD.Viewport.Transform);


            for (int n = 0; n < m_CDresults.Length; ++n)
            {
                var pointList = m_CDresults[n];

                var index = pointList.Count(p => p.X <= pos.X);

                if (index == 0 || index == pointList.Count)
                    continue;

                double dYdX = (pointList[index].Y - pointList[index - 1].Y) / (pointList[index].X - pointList[index - 1].X);
                m_CDlineGraphs[n].Description = new PenDescription(String.Format("[{0}D]: {1:0.000}", n + 2, dYdX));
            }
        }

    }
}
