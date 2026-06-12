using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Memory_Policy_Simulator
{
    public abstract class PageReplacementPolicy
    {
        protected int frameSize;
        protected List<PageInfo> frames;
        protected List<SimulationStep> steps;

        public int HitCount { get; protected set; }
        public int FaultCount { get; protected set; }
        public int MigrationCount { get; protected set; }

        public PageReplacementPolicy(int frameSize)
        {
            this.frameSize = frameSize;
            this.frames = new List<PageInfo>();
            this.steps = new List<SimulationStep>();
            this.HitCount = 0;
            this.FaultCount = 0;
            this.MigrationCount = 0;
        }

        public abstract void ProcessReference(char referencedChar, int step);

        public virtual SimulationResult RunSimulation(List<char> referenceString)
        {
            this.frames.Clear();
            this.steps.Clear();
            this.HitCount = 0;
            this.FaultCount = 0;
            this.MigrationCount = 0;

            for (int step = 0; step < referenceString.Count; step++)
            {
                ProcessReference(referenceString[step], step);
            }

            SimulationResult result = new SimulationResult
            {
                AlgorithmName = this.GetType().Name,
                TotalReferences = referenceString.Count,
                HitCount = this.HitCount,
                FaultCount = this.FaultCount,
                MigrationCount = this.MigrationCount,
                FaultRate = (double)this.FaultCount / referenceString.Count * 100.0,
                Steps = this.steps
            };

            return result;
        }

        protected List<char> GetFrameState()
        {
            return frames.Select(p => p.Data).ToList();
        }
    }

    public class FIFOPolicy : PageReplacementPolicy
    {
        private Queue<PageInfo> fifoQueue;

        public FIFOPolicy(int frameSize) : base(frameSize)
        {
            this.fifoQueue = new Queue<PageInfo>();
        }

        public override void ProcessReference(char referencedChar, int step)
        {
            // 1. 현재 페이지가 프레임에 있는지 확인 (HIT)
            PageInfo existingPage = frames.FirstOrDefault(p => p.Data == referencedChar);
            
            SimulationStep stepRecord = new SimulationStep
            {
                StepNumber = step,
                ReferencedChar = referencedChar
            };

            if (existingPage != null)
            {
                HitCount++;
                stepRecord.StatusType = "HIT";
                existingPage.LastAccessStep = step;
                existingPage.AccessCount++;
            }
            else if (frames.Count < frameSize)
            {
                FaultCount++;
                PageInfo newPage = new PageInfo(referencedChar, step);
                frames.Add(newPage);
                fifoQueue.Enqueue(newPage);
                stepRecord.StatusType = "FAULT";
            }
            else
            {
                FaultCount++;
                MigrationCount++;
                PageInfo victimPage = fifoQueue.Dequeue();
                frames.Remove(victimPage);
                
                PageInfo newPage = new PageInfo(referencedChar, step);
                frames.Add(newPage);
                fifoQueue.Enqueue(newPage);
                stepRecord.StatusType = "MIGRATION";
            }

            stepRecord.FrameState = GetFrameState();
            steps.Add(stepRecord);
        }
    }

    public class LRUPolicy : PageReplacementPolicy
    {
        public LRUPolicy(int frameSize) : base(frameSize)
        {
        }

        public override void ProcessReference(char referencedChar, int step)
        {
            PageInfo existingPage = frames.FirstOrDefault(p => p.Data == referencedChar);

            SimulationStep stepRecord = new SimulationStep
            {
                StepNumber = step,
                ReferencedChar = referencedChar
            };

            if (existingPage != null)
            {
                HitCount++;
                stepRecord.StatusType = "HIT";
                existingPage.LastAccessStep = step;
                existingPage.AccessCount++;
            }
            else if (frames.Count < frameSize)
            {
                FaultCount++;
                PageInfo newPage = new PageInfo(referencedChar, step);
                frames.Add(newPage);
                stepRecord.StatusType = "FAULT";
            }
            else
            {
                FaultCount++;
                MigrationCount++;
                
                PageInfo victimPage = frames.OrderBy(p => p.LastAccessStep).First();
                frames.Remove(victimPage);

                PageInfo newPage = new PageInfo(referencedChar, step);
                frames.Add(newPage);
                stepRecord.StatusType = "MIGRATION";
            }

            stepRecord.FrameState = GetFrameState();
            steps.Add(stepRecord);
        }
    }

    public class LFUPolicy : PageReplacementPolicy
    {
        public LFUPolicy(int frameSize) : base(frameSize)
        {
        }

        public override void ProcessReference(char referencedChar, int step)
        {
            PageInfo existingPage = frames.FirstOrDefault(p => p.Data == referencedChar);

            SimulationStep stepRecord = new SimulationStep
            {
                StepNumber = step,
                ReferencedChar = referencedChar
            };

            if (existingPage != null)
            {
                HitCount++;
                stepRecord.StatusType = "HIT";
                existingPage.LastAccessStep = step;
                existingPage.AccessCount++;
            }
            else if (frames.Count < frameSize)
            {
                FaultCount++;
                PageInfo newPage = new PageInfo(referencedChar, step);
                frames.Add(newPage);
                stepRecord.StatusType = "FAULT";
            }
            else
            {
                FaultCount++;
                MigrationCount++;

                PageInfo victimPage = frames.OrderBy(p => p.AccessCount)
                                           .ThenBy(p => p.LastAccessStep)
                                           .First();
                frames.Remove(victimPage);

                PageInfo newPage = new PageInfo(referencedChar, step);
                frames.Add(newPage);
                stepRecord.StatusType = "MIGRATION";
            }

            stepRecord.FrameState = GetFrameState();
            steps.Add(stepRecord);
        }
    }

    public class PARPolicy : PageReplacementPolicy
    {
        private const int W = 10;
        private const double T = 0.15;
        private const double LAMBDA_MIN = 0.2;
        private const double LAMBDA_MAX = 1.0;
        private const double DECAY_RATE = 0.01;

        private List<bool> recentHits;
        private double lambda;
        private double cumulativeSum;
        private double previousHitRate;

        public PARPolicy(int frameSize) : base(frameSize)
        {
            this.recentHits = new List<bool>();
            this.lambda = LAMBDA_MAX;
            this.cumulativeSum = 0.0;
            this.previousHitRate = 0.0;
        }

        public override void ProcessReference(char referencedChar, int step)
        {
            PageInfo existingPage = frames.FirstOrDefault(p => p.Data == referencedChar);

            SimulationStep stepRecord = new SimulationStep
            {
                StepNumber = step,
                ReferencedChar = referencedChar
            };

            bool isHit = false;

            if (existingPage != null)
            {
                HitCount++;
                stepRecord.StatusType = "HIT";
                isHit = true;
                existingPage.LastAccessStep = step;
                existingPage.AccessCount++;
                existingPage.Recency = 1.0;
            }
            else if (frames.Count < frameSize)
            {
                FaultCount++;
                PageInfo newPage = new PageInfo(referencedChar, step);
                frames.Add(newPage);
                stepRecord.StatusType = "FAULT";
                isHit = false;
            }
            else
            {
                FaultCount++;
                MigrationCount++;
                
                PageInfo victimPage = SelectVictimPagePAR(step);
                frames.Remove(victimPage);

                PageInfo newPage = new PageInfo(referencedChar, step);
                frames.Add(newPage);
                stepRecord.StatusType = "MIGRATION";
                isHit = false;
            }

            recentHits.Add(isHit);
            if (recentHits.Count > W)
            {
                recentHits.RemoveAt(0);
            }

            double currentHitRate = recentHits.Count > 0 
                ? (double)recentHits.Count(h => h) / recentHits.Count 
                : 0.0;

            double hitRateDelta = previousHitRate - currentHitRate;
            cumulativeSum += hitRateDelta;

            bool isPhaseChange = false;
            if (cumulativeSum > T)
            {
                isPhaseChange = true;
                lambda = LAMBDA_MAX;
                cumulativeSum = 0.0;
            }
            else
            {
                lambda = Math.Max(lambda - DECAY_RATE, LAMBDA_MIN);
            }

            previousHitRate = currentHitRate;

            stepRecord.Lambda = lambda;
            stepRecord.IsPhaseChange = isPhaseChange;
            stepRecord.FrameState = GetFrameState();
            steps.Add(stepRecord);
        }

        private PageInfo SelectVictimPagePAR(int currentStep)
        {
            double maxRecency = 0;
            double maxFrequency = 0;

            foreach (var page in frames)
            {
                page.Recency = currentStep - page.LastAccessStep;
                page.Frequency = page.AccessCount;
                
                maxRecency = Math.Max(maxRecency, page.Recency);
                maxFrequency = Math.Max(maxFrequency, page.Frequency);
            }

            if (maxRecency == 0) maxRecency = 1;
            if (maxFrequency == 0) maxFrequency = 1;

            double minScore = double.MaxValue;
            PageInfo victimPage = null;

            foreach (var page in frames)
            {
                double recencyNorm = page.Recency / maxRecency;
                double frequencyNorm = page.Frequency / maxFrequency;

                double score = (lambda * recencyNorm) + ((1.0 - lambda) * frequencyNorm);

                if (score < minScore)
                {
                    minScore = score;
                    victimPage = page;
                }
            }

            return victimPage ?? frames[0];
        }
    }
}
