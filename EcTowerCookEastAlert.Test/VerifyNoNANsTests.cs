using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace EcTowerCookEastAlert.Tests
{
    public class VerifyNoNANsTests
    {
        string fileWithBadNAN = @"Assets/CookEastEcTower_Flux_Raw_2017_11_03_1300_badNAN.dat";
        string fileWithNANOkLocations = @"Assets/CookEastEcTower_Flux_Raw_2017_11_03_1300_okNAN.dat";
        string fileWithBadDataAtSecondRow = @"Assets/CookEastEcTower_Flux_Raw_2017_11_03_1300_2linesBadCO2.dat";
        string fileWithBadDataAtSecondRowAndNAN = @"Assets/CookEastEcTower_Flux_Raw_2017_11_03_1300_2linesBadCO2BadNAN.dat";

        [Fact]
        public void Run_HasNan_ThrowsException()
        {
            // Arrange
            var s = new System.IO.FileStream(fileWithBadNAN, System.IO.FileMode.Open);
            var t = new TraceWriterStub(TraceLevel.Verbose);

            // Act


            foreach (var l in t.Traces)
            {
                Trace.WriteLine(l.ToString());
            }

            Exception ex = Assert.Throws<Exception>(() => VerifyNoNANs.Run(s, s.Name, t));

            Assert.Equal("[WARNING] CookEastEcTower_Flux_Raw_2017_11_03_1300_badNAN.dat: File contains NAN.", ex.Message);
        }

        [Fact]
        public void Run_HasNanAtOkLocations_NoException()
        {
            // Arrange
            var s = new System.IO.FileStream(fileWithNANOkLocations, System.IO.FileMode.Open);
            var t = new TraceWriterStub(TraceLevel.Verbose);

            // Act
            VerifyNoNANs.Run(s, s.Name, t);

            // Assert
        }

        [Fact]
        public void Run_HasBadValueAtSecondRow_ThrowsException()
        {
            // Arrange
            var s = new System.IO.FileStream(fileWithBadDataAtSecondRow, System.IO.FileMode.Open);
            var t = new TraceWriterStub(TraceLevel.Verbose);

            // Act
            Exception ex = Assert.Throws<Exception>(() => VerifyNoNANs.Run(s, s.Name, t));

            // Assert
            Assert.Equal("[WARNING] CookEastEcTower_Flux_Raw_2017_11_03_1300_2linesBadCO2.dat: CO2_sig_strgth_Min < 0.8 (0.689).", ex.Message);
        }

        [Fact]
        public void Run_HasBadValueAtSecondRowAndNAN_ThrowsException()
        {
            // Arrange
            var s = new System.IO.FileStream(fileWithBadDataAtSecondRowAndNAN, System.IO.FileMode.Open);
            var t = new TraceWriterStub(TraceLevel.Verbose);

            // Act
            Exception ex = Assert.Throws<Exception>(() => VerifyNoNANs.Run(s, s.Name, t));

            // Assert
            Assert.Equal("[WARNING] CookEastEcTower_Flux_Raw_2017_11_03_1300_2linesBadCO2BadNAN.dat: File contains NAN.[WARNING] CookEastEcTower_Flux_Raw_2017_11_03_1300_2linesBadCO2BadNAN.dat: CO2_sig_strgth_Min < 0.8 (0.689).", ex.Message);
        }
    }

    public class TraceWriterStub : TraceWriter
    {
        protected TraceLevel _level;
        protected List<TraceEvent> _traces;

        public TraceWriterStub(TraceLevel level) : base(level)
        {
            _level = level;
            _traces = new List<TraceEvent>();
        }

        public override void Trace(TraceEvent traceEvent)
        {
            _traces.Add(traceEvent);
        }

        public List<TraceEvent> Traces => _traces;
    }
}
