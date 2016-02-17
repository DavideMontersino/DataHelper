using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nts.DataHelper
{
    public class Error
    {


        public Error(ErrorLevel errorLevel, string message)
        {
            // TODO: Complete member initialization
            this.ErrorLevel = errorLevel;
            this.Message = message;
        }
        public ErrorLevel ErrorLevel { get; set; }
        public string Message { get; set; }


        public string ErrorLevelStr
        {
            get
            {
                return ErrorLevel.ToString();
            }
        }
    }

    public enum ErrorLevel
    {
        INFO = 1,
        WARNING = 2,
        ERROR = 3
    }
}
