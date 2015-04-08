using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Text.RegularExpressions;
using System.IO;
using System.Windows;
using System.Globalization;

namespace CorrelationDimensionCalculator
{
    public class ViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        private int m_nod = 2, m_dataShift = 2, m_dataColumn = 1;
        private List<List<double>> m_rawData;

        public int NumberOfDimensions { get { return m_nod; } set { m_nod = value; UpdateModel(); OnPropertyChanged("NumberOfDimensions"); } }
        public int DataShift { get { return m_dataShift; } set { m_dataShift = value; UpdateModel(); OnPropertyChanged("DataShift"); } }

        public int ActiveDataColumn { get { return m_dataColumn; } set { m_dataColumn = value; UpdateModel(); OnPropertyChanged("ActiveDataColumn"); } }
        public int NumberOfDataColumns { get { return (m_rawData == null) ? 0 : m_rawData.Count; } }
        public IReadOnlyList<double> RawData { get; private set; }

        protected PseudoPhaseSpace Model { get; private set; }

        public ViewModel ()
        {
        }

        private IEnumerable<Point> Get2DPseudophaseSpaceAttractor()
        {
            for (int i = 0; i < RawData.Count; ++i)
            {
                int j = (i + DataShift) % RawData.Count;
                yield return new Point(RawData[i], RawData[j]);
            }
        }

        public IEnumerable<Point> ComputeCorrelationalDimensionCurve(int N)
        {
            return Model.ComputeCorrelation(N);
        }

        public double ComputeHurstExponent()
        {
            return Model.ComputeHurstExponent();
        }

        private void UpdateModel()
        {
            if (m_rawData == null || m_rawData.Count == 0 || m_dataColumn >= NumberOfDataColumns)
                return;

            RawData = m_rawData[m_dataColumn].AsReadOnly();

            Model = new PseudoPhaseSpace(RawData, DataShift);
            OnPseudophaseSpaceChanged(Get2DPseudophaseSpaceAttractor());
        }

        private static Regex deserealizeRegex = new Regex("([^\t\n ])+", RegexOptions.IgnoreCase);
        public void ReadFile(string filename)
        {
            m_rawData = null;

            using (var reader = new StreamReader(filename))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    //parsing
                    MatchCollection matches = deserealizeRegex.Matches(line);


                    if (m_rawData == null)
                    {
                        m_rawData = new List<List<double>>(matches.Count);
                        for (int i = 0; i < matches.Count; i++)
                            m_rawData.Add(new List<double>());
                        OnPropertyChanged("NumberOfDataColumns");

                        if (ActiveDataColumn >= NumberOfDataColumns)
                            ActiveDataColumn = NumberOfDataColumns - 1;
                    }

                    for (int i = 0; i < matches.Count; i++)
                    {
                        double value = double.Parse(matches[i].Value.Replace(',', '.'), NumberStyles.Float, NumberFormatInfo.InvariantInfo);
                        m_rawData[i].Add(value);
                    }
                }
            }

            UpdateModel();
        }

        public class CurveEventArgs : EventArgs
        {
            public IEnumerable<Point> Curve { get; private set; }

            public CurveEventArgs(IEnumerable<Point> curve) { Curve = curve; }
        }

        public event EventHandler<CurveEventArgs> PseudophaseSpaceChanged;

        private void OnPseudophaseSpaceChanged(IEnumerable<Point> curve)
        {
            if (PseudophaseSpaceChanged != null)
                PseudophaseSpaceChanged(this, new CurveEventArgs(curve));
        }

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }
}
