using Microsoft.Azure.WebJobs.Host;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace EcTowerCookEastAlert.Tests
{
    public class VerifyNoNANsTests
    {
        string fileWithNAN = @"Assets/CookEastEcTower_Flux_Raw_2017_10_26_0000.dat";
        string fileWithNANOkLocations = @"Assets/CookEastEcTower_Flux_Raw_2017_10_27_1330.dat";
        string fileWithBadDataAtSecondRow = @"Assets/CookEastEcTower_Flux_Raw_2017_10_27_1330_2linesBadCO2_sig.dat";

        [Fact]
        public void Run_HasNan_ThrowsException()
        {
            // Arrange
            var s = new System.IO.FileStream(fileWithNAN, System.IO.FileMode.Open);
            var t = new TraceWriterStub(TraceLevel.Verbose);

            // Act


            foreach (var l in t.Traces)
            {
                Trace.WriteLine(l.ToString());
            }

            Exception ex = Assert.Throws<Exception>(() => VerifyNoNANs.Run(s, s.Name, t));

            Assert.Equal("CookEastEcTower_Flux_Raw_2017_10_26_0000.dat: File contains NAN", ex.Message);
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
            Assert.Equal("CookEastEcTower_Flux_Raw_2017_10_27_1330_2linesBadCO2_sig.dat: CO2_sig_strgth_Min < 0.8, IRGA needs cleaning (0.3260049)", ex.Message);
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
