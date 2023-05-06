using System;
using System.Text;
using rmb.shared;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using showdown.model;
using Newtonsoft.Json;
namespace showdown.mothership
{
    public class Wheel
    {
        private List<Wedge> wedges = new List<Wedge>();
        private const int WeakCount = 8;
        private const int RegularCount = 12;
        private const int StrongCount = 7;
        private const int SkipCount = 6;

        private int _wonId = 0;

        public Wheel(List<Wedge> wedges)
        {
            this.wedges = wedges;
        }

        private (int, int) GetRange(WheelStrengthState strengthState)
        {
            switch (strengthState)
            {
                case WheelStrengthState.Weak:
                    return (SkipCount, WeakCount + SkipCount);
                case WheelStrengthState.Regular:
                    return (WeakCount + SkipCount, WeakCount + SkipCount + RegularCount);
                case WheelStrengthState.Strong:
                    return (WeakCount + SkipCount + RegularCount,
                        WeakCount + SkipCount + RegularCount + StrongCount);
                default:
                    return (0, wedges.Count);
            }
        }

        private int GetCurrentMaxValue(WheelStrengthState strengthState)
        {
            switch (strengthState)
            {
                case WheelStrengthState.Weak:
                    return WeakCount;
                case WheelStrengthState.Regular:
                    return RegularCount;
                case WheelStrengthState.Strong:
                    return StrongCount;
                default:
                    return 0;
            }
        }

        public Wedge PreDetermineValue(WheelStrengthState strengthState)
        {
            List<Wedge> newWedgesList = new List<Wedge>();
            int minI = GetRange(strengthState).Item1;
            int maxI = GetRange(strengthState).Item2;
            var localWonId = _wonId;
            var newMinId = localWonId - minI;
            if (newMinId > 0)
            {
                for (int i = newMinId; i > 0; i--)
                {
                    wedges.FindAll(x => x.wedgeIndex == i
                    ).ForEach(t => { newWedgesList.Add(t); });
                }
            }
            Util.log.Info("PreDetermineValue 1");
            var addedCounter = newWedgesList.GroupBy(v => v.wedgeIndex).Count();
            if (addedCounter == 0)
            {
                int ts = 24 + newMinId;
                if (ts < 0) ts = 24 + ts;
                for (int i = ts; i > ts - (maxI - minI); i--)
                {
                    if (i > 0)
                        wedges.FindAll(x => x.wedgeIndex == i
                        ).ForEach(t => { newWedgesList.Add(t); });
                }
            }
            Util.log.Info("PreDetermineValue 2");
            addedCounter = newWedgesList.GroupBy(v => v.wedgeIndex).Count();
            if (addedCounter < GetCurrentMaxValue(strengthState) && addedCounter > 0)
            {
                var leftCounter = GetCurrentMaxValue(strengthState) - addedCounter;
                for (int i = 24; i > 24 - leftCounter; i--)
                {
                    wedges.FindAll(x => x.wedgeIndex == i
                    ).ForEach(t => { newWedgesList.Add(t); });
                }
            }
            Util.log.Info($"PreDetermineValue 3 {JsonConvert.SerializeObject(newWedgesList)}");
            var randomizedItem = newWedgesList.RandomElementByWeight(e => e.odds);
            _wonId = randomizedItem.wedgeIndex;
            Util.log.InfoFormat("Congratulation, you Win : {0}, this is wedge {1} and spoke {2}", randomizedItem.value,
                randomizedItem.wedgeIndex, randomizedItem.spokeIndex);
            //Debug.LogFormat("CONGRATULATION. You got this: {0} wedge with: {1} spoke. Your prize is {2}",
            //    randomizedItem.wedgeIndex, randomizedItem.spokeIndex, randomizedItem.value);
            return randomizedItem;
        }
    }

    public static class IEnumerableRandomizedExtensions
    {
        public static T RandomElementByWeight<T>(this IEnumerable<T> sequence, Func<T, double> weightSelector)
        {
            double totalWeight = sequence.Sum(weightSelector);
            // The weight we are after
            double itemWeightIndex = (double)new Random().NextDouble() * totalWeight;
            double currentWeightIndex = 0;

            foreach (var item in from weightedItem in sequence
                                 select new { Value = weightedItem, Weight = weightSelector(weightedItem) })
            {
                currentWeightIndex += item.Weight;

                // If we've hit or passed the weight we are after for this item then it's the one we want....
                if (currentWeightIndex >= itemWeightIndex)
                {
                    var ts = item.Value as Wedge;
                    return item.Value;
                }
            }

            return default;
        }
    }
}
