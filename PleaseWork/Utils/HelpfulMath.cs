﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PleaseWork.Utils
{
    public static class HelpfulMath
    {
        public static int MaxScoreForNotes(int notes)
        {
            if (notes <= 0) return 0;
            if (notes == 1) return 115;
            if (notes < 6) return 115 * (notes * 2 - 1);
            if (notes < 14) return 115 * ((notes - 5) * 4 + 9);
            return 920 * (notes - 14) + 5635;
        }
        public static int NotesForMaxScore(int score)
        {
            if (score <= 0) return 0;
            if (score == 115) return 1;
            if (score < 1495) return (score / 115 + 1) / 2;
            if (score < 5635) return (score / 115 - 9) / 4 + 5;
            return (score - 5635) / 920 + 14;
        }
        public static int MultiplierForNote(int note)
        {
            if (note >= 14) return 8;
            if (note < 2) return 1;
            if (note < 6) return 2;
            if (note < 14) return 4;
            return 8;
        }
        public static int DecreaseMultiplier(int note)
        {
            if (note >= 14) return 6;
            if (note >= 6) return 2;
            return 0;
        }
    }
}