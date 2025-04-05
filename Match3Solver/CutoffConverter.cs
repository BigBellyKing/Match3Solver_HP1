// FILE: Match3Solver/CutoffConverter.cs
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Match3Solver
{
    // --- MODIFICATION: Ensure class is public ---
    public class CutoffConverter : System.Windows.Data.IValueConverter
    // --- END MODIFICATION ---
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Ensure value is convertible to int before casting
            if (value is int intValue)
            {
                return intValue > Cutoff;
            }
            return false; // Default if conversion fails
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public int Cutoff { get; set; }
    }
}