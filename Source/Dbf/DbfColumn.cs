
namespace FastShapefile.Dbf
{
    public class DbfColumn
    {
        #region - Fields -

        private string _decimalFormat = null;

        #endregion

        #region - Properties -

        public string Name { get; internal set; }
        public DbfColumnType Type { get; internal set; }
        public int DataAddress { get; internal set; }
        public byte Length { get; internal set; }
        public byte DecimalCount { get; internal set; }

        internal string DecimalFormat
        {
            get { return _decimalFormat ?? (_decimalFormat = "F" + DecimalCount); }
        }

        #endregion
    }
}
