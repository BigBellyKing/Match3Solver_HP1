// FILE: Match3Solver/resultItem.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Match3Solver
{
    class resultItem
    {
        public string Position { get; set; }
        public int Amount { get; set; }
        public string Direction { get; set; }
        // --- HP1 Score Properties (Stamina Removed) ---
        public int sJoy { get; set; }
        public int sSentiment { get; set; }
        public int sPassion { get; set; }
        public int sRomance { get; set; }
        public int sTalent { get; set; }
        public int sSexuality { get; set; }
        public int sFlirtation { get; set; }
        public int sBrokenHeart { get; set; }
        // --- End Property Change ---
        public Boolean isVertical { set; get; }
        public int xPos { set; get; }
        public int yPos { set; get; }
        public int StaminaCost { set; get; }
        public int Chain { set; get; }
        public int Total { get; set; }
        public int TotalWBroken { set; get; }

        public resultItem(SolverInterface.Movement input)
        {
            Position = $"[{input.yPos},{input.xPos}]"; Amount = Math.Abs(input.amount); Direction = getDirection(input); isVertical = input.isVertical; xPos = input.xPos; yPos = input.yPos; StaminaCost = input.score.staminaCost; Chain = input.score.chains;
            // --- Assign HP1 scores (Stamina Removed) ---
            sJoy = input.score.Joy; sSentiment = input.score.Sentiment; sPassion = input.score.Passion; sRomance = input.score.Romance; sTalent = input.score.Talent; sSexuality = input.score.Sexuality; sFlirtation = input.score.Flirtation; sBrokenHeart = input.score.BrokenHeart;
            // --- End Assignment ---
            Total = input.score.getTotal(); TotalWBroken = input.score.getTotalNoBroken();
        }
        private string getDirection(SolverInterface.Movement input) { if (!input.isVertical && input.amount > 0) return "⇒"; else if (!input.isVertical && input.amount < 0) return "⇐"; else if (input.isVertical && input.amount > 0) return "⇓"; else if (input.isVertical && input.amount < 0) return "⇑"; return "?"; }
        public override string ToString() { return Direction; }
    }
}