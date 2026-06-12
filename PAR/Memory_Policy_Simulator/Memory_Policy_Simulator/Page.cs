using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memory_Policy_Simulator
{
    public class PageInfo
    {
        public char Data { get; set; }
        public int LastAccessStep { get; set; }
        public int AccessCount { get; set; }
        public double Recency { get; set; }
        public double Frequency { get; set; }
        public double Lambda { get; set; }
        public bool IsPhaseChange { get; set; }

        public PageInfo(char data, int step)
        {
            this.Data = data;
            this.LastAccessStep = step;
            this.AccessCount = 1;
            this.Recency = 0;
            this.Frequency = 0;
            this.Lambda = 1.0;
            this.IsPhaseChange = false;
        }
    }

    public class SimulationStep
    {
        public int StepNumber { get; set; }
        public char ReferencedChar { get; set; }
        public List<char> FrameState { get; set; }
        public string StatusType { get; set; }
        public double? Lambda { get; set; }
        public bool? IsPhaseChange { get; set; }

        public SimulationStep()
        {
            this.FrameState = new List<char>();
        }
    }

    public class SimulationResult
    {
        public string AlgorithmName { get; set; }
        public int TotalReferences { get; set; }
        public int HitCount { get; set; }
        public int FaultCount { get; set; }
        public int MigrationCount { get; set; }
        public double FaultRate { get; set; }
        public long ExecutionTimeMs { get; set; }
        public List<SimulationStep> Steps { get; set; }

        public SimulationResult()
        {
            this.Steps = new List<SimulationStep>();
        }
    }
}
