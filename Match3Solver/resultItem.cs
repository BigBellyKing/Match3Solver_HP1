using System;

namespace Match3Solver
{
    class resultItem
    {
        public string Position { get; set; }
        public int Amount { get; set; }
        public string Direction { get; set; }
        // HP1 Scores
        public int sJoy { get; set; }
        public int sSentiment { get; set; }
        public int sPassion { get; set; }
        public int sRomance { get; set; }
        public int sTalent { get; set; }
        public int sSexuality { get; set; }
        public int sFlirtation { get; set; }
        public int sBrokenHeart { get; set; }
        // Other properties
        public Boolean isVertical { set; get; }
        public int xPos { set; get; }
        public int yPos { set; get; }
        public int StaminaCost { set; get; } // Represents # gems matched in initial move
        public int Chain { set; get; }
        public int Total { get; set; }         // Net Score (Affect - Broken)
        public int TotalWBroken { set; get; }  // Raw Gain (Affect only)

        public resultItem(SolverInterface.Movement input)
        {
            Position = $"[{input.yPos},{input.xPos}]";
            Amount = Math.Abs(input.amount); // Use absolute value for display distance
            Direction = getDirection(input);
            // Assign HP1 scores
            sJoy = input.score.Joy; sSentiment = input.score.Sentiment; sPassion = input.score.Passion; sRomance = input.score.Romance; sTalent = input.score.Talent; sSexuality = input.score.Sexuality; sFlirtation = input.score.Flirtation; sBrokenHeart = input.score.BrokenHeart;
            // Assign other properties
            isVertical = input.isVertical; xPos = input.xPos; yPos = input.yPos;
            // Use score.staminaCost directly now as it represents initial matched count or 1.
            StaminaCost = input.score.staminaCost;
            Chain = input.score.chains;
            // Calculate display scores
            Total = input.score.getTotal(); // Net Score
            TotalWBroken = input.score.getTotalNoBroken(); // Raw Gain
        }
        private string getDirection(SolverInterface.Movement input) { if (!input.isVertical && input.amount > 0) return "⇒"; else if (!input.isVertical && input.amount < 0) return "⇐"; else if (input.isVertical && input.amount > 0) return "⇓"; else if (input.isVertical && input.amount < 0) return "⇑"; return "?"; }
        public override string ToString() { return Direction; }
    }
}