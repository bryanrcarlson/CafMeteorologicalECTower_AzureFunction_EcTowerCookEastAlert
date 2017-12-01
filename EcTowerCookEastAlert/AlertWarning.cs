using Nsar.EcTowerCookEastAlert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nsar.EcTowerCookEastAlert
{
    public class AlertWarning : AlertBasic
    {
        public AlertWarning(
            string filename, 
            string message) : base(filename, message)
        {
        }

        public override string ToString()
        {
            string s = $"[WARNING] {base.ToString()}";
            return s;
        }
    }
}
