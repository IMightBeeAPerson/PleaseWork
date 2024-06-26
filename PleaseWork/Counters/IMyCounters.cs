﻿using PleaseWork.Utils;
using TMPro;

namespace PleaseWork.Counters
{
    internal interface IMyCounters
    {
        void SetupData(MapSelection map);
        void ReinitCounter(TMP_Text display); //same difficulty, modifier, and map
        void ReinitCounter(TMP_Text display, float passRating, float accRating, float techRating); //same map, different modifiers
        void ReinitCounter(TMP_Text display, MapSelection map); //same map, different difficulty/mode
        void UpdateCounter(float acc, int notes, int badNotes, float fcPercent);

        string Name { get; }
    }
}
