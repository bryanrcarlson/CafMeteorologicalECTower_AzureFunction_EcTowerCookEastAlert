using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Linq;
using System.Text;

namespace EcTowerCookEastAlert
{
    public static class VerifyNoNANs
    {
        

        [FunctionName("VerifyNoNANs")]
        public static void Run(
            [BlobTrigger("ectower-cookeast/raw/Flux/{name}", 
            Connection = "CookEastFluxConnectionString")] Stream inBlob, 
            string name, 
            TraceWriter log)
        {
            // Column numbers of csv file
            int COL_NUM_CO2_sig_strgth_Min = 63;
            int COL_NUM_H2O_sig_strgth_Min = 64;
            int COL_NUM_batt_volt_Avg = 240;
            int COL_NUM_sonic_samples_Tot = 155;
            int COL_NUM_CO2_samples_Tot = 182;
            int COL_NUM_H2O_samples_Tot = 183;
            int COL_NUM_Precipitation_Tot = 73;
            int COL_NUM_door_is_open_Hst = 238;
            int[] COL_with_NAN_OK = { 83, 84, 95, 96, 97, 98, 99, 100 };

            // Alert types
            const string INFO = "[INFO]";
            const string WARNING = "[WARNING]";
            const string ERROR = "[ERROR]";

            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {inBlob.Length} Bytes");
            
            // Ignore files that are not .dat files
            if (Path.GetExtension(name) != ".dat") return;

            StringBuilder alertString = new StringBuilder();

            using (var sr = new StreamReader(inBlob, true))
            {
                // Skip first four lines (the header and meta data)
                int count = 0;
                while (sr.ReadLine() != null && count < 3)
                {
                    count++;
                }

                string line;
                while((line = sr.ReadLine()) != null)
                {
                    // Split by comma (it's assumed to be a csv) to do futher tests
                    string[] values = line.Split(',');

                    // Make sure length of data is intended
                    if (values.Length != 245)
                    {
                        //throw new Exception($"{Path.GetFileName(name)}: Files does not contain 245 values ({values.Length})");
                        alertString.Append($"{ERROR} {Path.GetFileName(name)}: Files does not contain 245 values ({values.Length}).");
                    }

                    // Check for NAN where they shouldn't be
                    for(int i = 0; i < values.Length; i++)
                    {
                        if (values[i] == "\"NAN\"" && !COL_with_NAN_OK.Contains(i))
                        {
                            //throw new Exception($"{Path.GetFileName(name)}: File contains NAN");
                            alertString.Append($"{WARNING} {Path.GetFileName(name)}: File contains NAN.");
                        }
                    }

                    // Extract values we care about
                    double CO2_sig_strgth_Min = Convert.ToDouble(values[COL_NUM_CO2_sig_strgth_Min]);
                    double H2O_sig_strgth_Min = Convert.ToDouble(values[COL_NUM_H2O_sig_strgth_Min]);
                    double batt_volt_Avg =      Convert.ToDouble(values[COL_NUM_batt_volt_Avg]);
                    double sonic_samples_Tot =  Convert.ToDouble(values[COL_NUM_sonic_samples_Tot]);
                    double CO2_samples_Tot =    Convert.ToDouble(values[COL_NUM_CO2_samples_Tot]);
                    double H2O_samples_Tot =    Convert.ToDouble(values[COL_NUM_H2O_samples_Tot]);
                    double Precipitation_Tot =  Convert.ToDouble(values[COL_NUM_Precipitation_Tot]);
                    double door_is_open_Hst =   Convert.ToDouble(values[COL_NUM_door_is_open_Hst]);

                    if (CO2_sig_strgth_Min < 0.8 && Precipitation_Tot == 0)
                        alertString.Append($"{WARNING} {Path.GetFileName(name)}: CO2_sig_strgth_Min < 0.8, IRGA needs cleaning ({CO2_sig_strgth_Min}).");
                        //throw new Exception($"{Path.GetFileName(name)}: CO2_sig_strgth_Min < 0.8, IRGA needs cleaning ({CO2_sig_strgth_Min})");

                    if (H2O_sig_strgth_Min < 0.8 && Precipitation_Tot == 0)
                        alertString.Append($"{WARNING} {Path.GetFileName(name)}: H2O_sig_strgth_Min < 0.8, IRGA needs cleaning ({H2O_sig_strgth_Min}).");
                        //throw new Exception($"{Path.GetFileName(name)}: H2O_sig_strgth_Min < 0.8, IRGA needs cleaning ({H2O_sig_strgth_Min})");

                    if (batt_volt_Avg < 11)
                        alertString.Append($"{ERROR} {Path.GetFileName(name)}: batt_volt_Avg < 11, Solar panels blocked/batteries not recharging ({batt_volt_Avg}).");
                        //throw new Exception($"{Path.GetFileName(name)}: batt_volt_Avg < 11, Solar panels blocked/batteries not recharging ({batt_volt_Avg})");

                    if (sonic_samples_Tot < 13500)
                        alertString.Append($"{WARNING} {Path.GetFileName(name)}: sonic_samples_Tot < 13500, Too many scans being skipped ({sonic_samples_Tot}).");
                        //throw new Exception($"{Path.GetFileName(name)}: sonic_samples_Tot < 13500, Too many scans being skipped ({sonic_samples_Tot})");

                    if (CO2_samples_Tot < 13500)
                        alertString.Append($"{WARNING} {Path.GetFileName(name)}: CO2_samples_Tot < 13500, Too many scans being skipped ({CO2_samples_Tot}).");
                        //throw new Exception($"{Path.GetFileName(name)}: CO2_samples_Tot < 13500, Too many scans being skipped ({CO2_samples_Tot})");

                    if (H2O_samples_Tot < 13500)
                        alertString.Append($"{WARNING} {Path.GetFileName(name)}: H2O_samples_Tot < 13500, Too many scans being skipped ({H2O_samples_Tot}).");
                        //throw new Exception($"{Path.GetFileName(name)}: H2O_samples_Tot < 13500, Too many scans being skipped ({H2O_samples_Tot})");

                    if (door_is_open_Hst > 0)
                        alertString.Append($"{INFO} {Path.GetFileName(name)}: door_is_open_Hst > 0, Door to enclosure open ({door_is_open_Hst}).");
                        //throw new Exception($"{Path.GetFileName(name)}: door_is_open_Hst > 0, Door to enclosure open ({door_is_open_Hst})");
                }
            }

            if (alertString.Length > 0)
                throw new Exception(alertString.ToString());
        }
    }
}
