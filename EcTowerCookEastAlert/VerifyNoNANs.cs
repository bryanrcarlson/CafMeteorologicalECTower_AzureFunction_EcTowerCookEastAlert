using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System;
using System.Linq;
using System.Text;
using TweetSharp;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Configuration;
using System.Collections.Generic;

namespace Nsar.EcTowerCookEastAlert
{
    public static class VerifyNoNANs
    {
        [FunctionName("VerifyNoNANs")]
        public static void Run(
            [BlobTrigger("ectower-cookeast/raw/Flux/{name}", 
            Connection = "CookEastFluxConnectionString")] System.IO.Stream inBlob, 
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
            
            // Ignore files that are not .dat files
            if (Path.GetExtension(name) != ".dat") return;

            //StringBuilder alertString = new StringBuilder();
            List<IAlertMessage> alerts = new List<IAlertMessage>();

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
                        //alertString.Append($"{ERROR} {Path.GetFileName(name)}: Files does not contain 245 values ({values.Length}).");
                        alerts.Add(
                            new AlertError(Path.GetFileName(name), 
                            $"Files does not contain 245 values ({values.Length})"));
                    }

                    // Check for NAN where they shouldn't be
                    for(int i = 0; i < values.Length; i++)
                    {
                        if (values[i] == "\"NAN\"" && !COL_with_NAN_OK.Contains(i))
                        {
                            //throw new Exception($"{Path.GetFileName(name)}: File contains NAN");
                            //alertString.Append($"{WARNING} {Path.GetFileName(name)}: File contains NAN.");
                            alerts.Add(
                                new AlertWarning(Path.GetFileName(name),
                                $"File contains NAN"));
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
                        alerts.Add(
                                new AlertWarning(Path.GetFileName(name),
                                $"CO2_sig_strgth_Min < 0.8 ({CO2_sig_strgth_Min})"));

                    if (H2O_sig_strgth_Min < 0.8 && Precipitation_Tot == 0)
                        alerts.Add(
                                new AlertWarning(Path.GetFileName(name),
                                $"H2O_sig_strgth_Min < 0.8 ({H2O_sig_strgth_Min})"));

                    //if (batt_volt_Avg < 12.5 && batt_volt_Avg >= 12.1)
                    //    alerts.Add(
                    //            new AlertInformation(Path.GetFileName(name),
                    //            $"batt_volt_Avg low ({batt_volt_Avg})"));
                    //
                    //if (batt_volt_Avg < 12.1 && batt_volt_Avg >= 11.6)
                    //    alerts.Add(
                    //            new AlertWarning(Path.GetFileName(name),
                    //            $"batt_volt_Avg low ({batt_volt_Avg})"));

                    if (batt_volt_Avg < 11.6)
                        alerts.Add(
                                new AlertError(Path.GetFileName(name),
                                $"batt_volt_Avg < 11.6 ({batt_volt_Avg})"));

                    if (sonic_samples_Tot < 13500)
                        alerts.Add(
                                new AlertWarning(Path.GetFileName(name),
                                $"sonic_samples_Tot < 13500 ({sonic_samples_Tot})"));

                    if (CO2_samples_Tot < 13500)
                        alerts.Add(
                                new AlertWarning(Path.GetFileName(name),
                                $"CO2_samples_Tot < 13500 ({CO2_samples_Tot})"));

                    if (H2O_samples_Tot < 13500)
                        alerts.Add(
                                new AlertWarning(Path.GetFileName(name),
                                $"H2O_samples_Tot < 13500 ({H2O_samples_Tot})"));

                    if (door_is_open_Hst > 0)
                        alerts.Add(
                                new AlertInformation(Path.GetFileName(name),
                                $"door_is_open_Hst > 0 ({door_is_open_Hst})"));
                }
            }

            if (alerts.Count > 0)
            {
                StringBuilder alertString = new StringBuilder();
                foreach(IAlertMessage alert in alerts)
                {
                    alertString.AppendLine(alert.ToString());
                }
                // Remove newline from end
                alertString.Length = alertString.Length-2;

                //log.Trace(new TraceEvent(TraceLevel.Off, alertString.ToString()));
                LogAlert(alertString.ToString(), log);

                TweetAlert(alertString.ToString());

                //authenticating with Twitter
                //var _consumerKey = ConfigurationManager.AppSettings["TwitterConsumerKey"];
                //var _consumerSecret = ConfigurationManager.AppSettings["TwitterConsumerSecret"];
                //var _accessToken = ConfigurationManager.AppSettings["TwitterAccessToken"];
                //var _accessTokenSecret = ConfigurationManager.AppSettings["TwitterAccessTokenSecret"];
                //var service = new TwitterService(_consumerKey, _consumerSecret);
                //service.AuthenticateWith(_accessToken, _accessTokenSecret);
                //
                //TwitterStatus result = service.SendTweet(new SendTweetOptions
                //{
                //    Status = alertString.ToString()
                //});
            }
        }

        [Conditional("DEBUG")]
        private static void LogAlert(string alertMessage, TraceWriter log)
        {
            log.Trace(new TraceEvent(TraceLevel.Off, alertMessage));
        }

        [Conditional("RELEASE")]
        private static void TweetAlert(string alertMessage)
        {
            var _consumerKey = ConfigurationManager.AppSettings["TwitterConsumerKey"];
            var _consumerSecret = ConfigurationManager.AppSettings["TwitterConsumerSecret"];
            var _accessToken = ConfigurationManager.AppSettings["TwitterAccessToken"];
            var _accessTokenSecret = ConfigurationManager.AppSettings["TwitterAccessTokenSecret"];
            var service = new TwitterService(_consumerKey, _consumerSecret);
            service.AuthenticateWith(_accessToken, _accessTokenSecret);

            TwitterStatus result = service.SendTweet(new SendTweetOptions
            {
                Status = alertMessage
            });
        }
    }
}
