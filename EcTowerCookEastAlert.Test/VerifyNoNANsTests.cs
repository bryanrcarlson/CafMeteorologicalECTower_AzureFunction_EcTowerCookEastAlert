using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace Nsar.EcTowerCookEastAlert.Tests
{
    public class VerifyNoNANsTests
    {
        string fileWithBadNAN = @"Assets/CookEastEcTower_Flux_Raw_2017_11_03_1300_badNAN.dat";
        string fileWithNANOkLocations = @"Assets/CookEastEcTower_Flux_Raw_2017_11_03_1300_okNAN.dat";
        string fileWithBadDataAtSecondRow = @"Assets/CookEastEcTower_Flux_Raw_2017_11_03_1300_2linesBadCO2.dat";
        string fileWithBadDataAtSecondRowAndNAN = @"Assets/CookEastEcTower_Flux_Raw_2017_11_03_1300_2linesBadCO2BadNAN.dat";

        [Fact]
        public void Run_HasNan_CreatesAlert()
        {
            // Arrange
            var s = new System.IO.FileStream(fileWithBadNAN, System.IO.FileMode.Open);
            var t = new TraceWriterStub(TraceLevel.Verbose);
            string expected = "[WARNING] CookEastEcTower_Flux_Raw_2017_11_03_1300_badNAN.dat: File contains NAN.";

            // Act
            VerifyNoNANs.Run(s, s.Name, t);
            string actual = t.ToString();

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Run_HasNanAtOkLocations_NoAlert()
        {
            // Arrange
            var s = new System.IO.FileStream(fileWithNANOkLocations, System.IO.FileMode.Open);
            var t = new TraceWriterStub(TraceLevel.Verbose);

            // Act
            VerifyNoNANs.Run(s, s.Name, t);

            // Assert
        }

        [Fact]
        public void Run_HasBadValueAtSecondRow_CreatesAlert()
        {
            // Arrange
            var s = new System.IO.FileStream(fileWithBadDataAtSecondRow, System.IO.FileMode.Open);
            var t = new TraceWriterStub(TraceLevel.Verbose);
            string expected = "[WARNING] CookEastEcTower_Flux_Raw_2017_11_03_1300_2linesBadCO2.dat: CO2_sig_strgth_Min < 0.8 (0.689).";
            
            // Act
            VerifyNoNANs.Run(s, s.Name, t);
            string actual = t.ToString();

            // Assert
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void Run_HasBadValueAtSecondRowAndNAN_CreatesAlert()
        {
            // Arrange
            var s = new System.IO.FileStream(fileWithBadDataAtSecondRowAndNAN, System.IO.FileMode.Open);
            var t = new TraceWriterStub(TraceLevel.Verbose);
            string expected = "[WARNING] CookEastEcTower_Flux_Raw_2017_11_03_1300_2linesBadCO2BadNAN.dat: File contains NAN.\r\n[WARNING] CookEastEcTower_Flux_Raw_2017_11_03_1300_2linesBadCO2BadNAN.dat: CO2_sig_strgth_Min < 0.8 (0.689).";

            // Act
            VerifyNoNANs.Run(s, s.Name, t);
            string actual = t.ToString();

            // Assert
            Assert.Equal(expected, actual);
        }
    }

    public class TraceWriterStub : TraceWriter
    {
        protected TraceLevel _level;
        protected List<TraceEvent> _traces;
        public string TraceString { get; set; }

        public TraceWriterStub(TraceLevel level) : base(level)
        {
            _level = level;
            _traces = new List<TraceEvent>();
        }

        public override void Trace(TraceEvent traceEvent)
        {
            _traces.Add(traceEvent);
            TraceString = traceEvent.Message;
        }

        public override string ToString()
        {
            return TraceString;
        }

        public List<TraceEvent> Traces => _traces;
    }
}
