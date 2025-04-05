// FILE: Match3Solver/SolverInterface.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media; // Required for Color struct if used elsewhere, not directly here

namespace Match3Solver
{
    public interface SolverInterface
    {
        public struct Score
        {
            // --- HP1 Tile Scores (7 types + Broken Heart) ---
            public int Joy;         // 0: Bell Icon
            public int Sentiment;   // 1: Teardrop
            public int Passion;     // 2: Heart
            public int Romance;     // 3: Orange Lips?
            public int Talent;      // 4: Blue Star?
            public int Sexuality;   // 5: Red Whip?
            public int Flirtation;  // 6: Green Diamond?
            public int BrokenHeart; // 7: Purple Broken Heart
            // --- End HP1 Tiles ---

            public int staminaCost;
            public int chains;
            public Boolean wasChanged;

            public Score(int whyDoINeedThis = 0)
            {
                // --- Initialize HP1 fields ---
                this.Joy = 0; this.Sentiment = 0; this.Passion = 0; this.Romance = 0;
                this.Talent = 0; this.Sexuality = 0; this.Flirtation = 0; this.BrokenHeart = 0;
                // --- End Init ---
                this.staminaCost = 0; this.chains = 0; this.wasChanged = false;
            }

            // --- Updated addScoreFromValue ---
            public void addScoreFromValue(int value)
            {
                switch (value % 10) // Modulo 10 useful if marked tiles (value > 9) are passed
                {
                    case 0: this.Joy++; break;
                    case 1: this.Sentiment++; break;
                    case 2: this.Passion++; break;
                    case 3: this.Romance++; break;
                    case 4: this.Talent++; break;
                    case 5: this.Sexuality++; break;
                    case 6: this.Flirtation++; break;
                    case 7: this.BrokenHeart++; break;
                    default: return; // Unknown tile index
                }
                this.wasChanged = true;
            }
            // --- End Update ---

            public void resetWasChanged() { this.wasChanged = false; }

            // --- Updated hasScore ---
            public Boolean hasScore() { return (Joy + Sentiment + Passion + Romance + Talent + Sexuality + Flirtation + BrokenHeart) > 0; }
            // --- End Update ---

            // --- Updated getTotal ---
            public int getTotal() { return (Joy + Sentiment + Passion + Romance + Talent + Sexuality + Flirtation) - BrokenHeart; }
            // --- End Update ---

            // --- Updated getTotalNoBroken ---
            public int getTotalNoBroken() { return (Joy + Sentiment + Passion + Romance + Talent + Sexuality + Flirtation); }
            // --- End Update ---
        }

        public struct Movement
        {
            public int xPos; public int yPos; public Boolean isVertical; public int amount; public Score score; public int boardHash;
            public Movement(int xPos, int yPos, Boolean isVertical, int amount, Score score, int boardhash) { this.xPos = xPos; this.yPos = yPos; this.isVertical = isVertical; this.amount = amount; this.score = score; this.boardHash = boardhash; }
        }
    }
}