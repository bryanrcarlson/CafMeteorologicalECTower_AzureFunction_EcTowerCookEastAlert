using Nsar.EcTowerCookEastAlert;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nsar.EcTowerCookEastAlert
{
    public class AlertInformation : AlertBasic
    {
        public AlertInformation(
            string filename, 
            string message) : base(filename, message)
        {
        }

        public override string ToString()
        {
            string s = $"[INFO] {base.ToString()}";
            return s;
        }
    }
}
