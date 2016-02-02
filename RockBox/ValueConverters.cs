using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RockBox
{
    [ValueConversion(typeof(object), typeof(string))]
    public class ConcatenateFieldsMultiValueConverter : IMultiValueConverter
    {
        public object Convert(
                    object[] values,
                    Type targetType,
                    object parameter,
                    System.Globalization.CultureInfo culture
                 )
        {
            string strDelimiter;
            StringBuilder sb = new StringBuilder();

            if (parameter != null)
            {
                //Use the passed delimiter.
                strDelimiter = parameter.ToString();
            }
            else
            {
                //Use the default delimiter.
                strDelimiter = ", ";
            }

            //Concatenate all fields
            foreach (object value in values)
            {
                if (value != null && value.ToString().Trim().Length > 0)
                {
                    if (sb.Length > 0) sb.Append(strDelimiter);
                    sb.Append(value.ToString());
                }
            }

            return sb.ToString();
        }

        public object[] ConvertBack(
                    object value,
                    Type[] targetTypes,
                    object parameter,
                    System.Globalization.CultureInfo culture
              )
        {
            throw new NotImplementedException("ConcatenateFieldsMultiValueConverter cannot convert back (bug)!");
        }

    }
}
