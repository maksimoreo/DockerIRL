using System;
using System.Collections.Generic;
using System.Linq;

namespace DockerIrl.Utils
{
    public interface IHasId
    {
        public string id { get; set; }
    }

    /// <summary>
    /// Container class for random generic methods
    /// </summary>
    public static class X
    {
        /// <summary>
        /// Splits given lists of items firstList and secondList into three lists: (1) items that appear in both
        /// firstList and secondList, (2) items that appear only in firstList and (3) items that appear only in
        /// secondList. This implementation might call a match function for the same pair of items twice.
        /// </summary>
        /// <typeparam name="TFirstItem"></typeparam>
        /// <typeparam name="TSecondItem"></typeparam>
        /// <param name="firstList"></param>
        /// <param name="secondList"></param>
        /// <param name="match"></param>
        /// <returns></returns>
        public static (
            IList<(TFirstItem firstItem, TSecondItem secondItem)> matchedPairs,
            IList<TFirstItem> unmatchedFirstListItems,
            IList<TSecondItem> unmatchedSecondListItems
            ) PartitionLists<TFirstItem, TSecondItem>(
            IList<TFirstItem> firstList,
            IList<TSecondItem> secondList,
            Func<TFirstItem, TSecondItem, bool> match
        )
        {
            var matchedPairs = new List<(TFirstItem firstItem, TSecondItem secondItem)>();
            var unmatchedFirstListItems = new List<TFirstItem>();
            var unmatchedSecondListItems = new List<TSecondItem>();

            foreach (var firstListItem in firstList)
            {
                var matchingSecondListItem = secondList.FirstOrDefault((secondListItemWithIndex) => match(firstListItem, secondListItemWithIndex));

                if (matchingSecondListItem == null)
                {
                    unmatchedFirstListItems.Add(firstListItem);
                    continue;
                }

                matchedPairs.Add((firstListItem, matchingSecondListItem));
            }

            foreach (var secondListItem in secondList)
            {
                var matchingFirstListItem = firstList.FirstOrDefault((firstListItem) => match(firstListItem, secondListItem));

                if (matchingFirstListItem == null)
                {
                    unmatchedSecondListItems.Add(secondListItem);
                    continue;
                }
            }

            return (matchedPairs, unmatchedFirstListItems, unmatchedSecondListItems);
        }

        public static bool IdMatch(IHasId a, IHasId b) => a.id == b.id;

        public static string ToStringList<T>(IEnumerable<T> items)
        {
            return $"[{string.Join(", ", items)}]";
        }

        public static string ToIdStringList(IEnumerable<IHasId> items)
        {
            return ToStringList(items.Select((item) => item.id));
        }

        public static string GenerateUUID()
        {
            return Guid.NewGuid().ToString();
        }

        public static IEnumerable<T> FindDuplicates<T>(IEnumerable<T> items)
        {
            return items.GroupBy((item) => item).Where((group) => group.Count() > 1).Select((group) => group.Key);
        }

        public static T RandomSample<T>(IList<T> items)
        {
            return items[UnityEngine.Random.Range(0, items.Count())];
        }

        public static string FormatStringTemplate(string template, Dictionary<string, string> parameters)
        {
            // TODO: Maybe use template engine like mustache?

            return parameters.Aggregate(template, (template, parameter) => template.Replace($"{{{{{parameter.Key}}}}}", parameter.Value));
        }
    }
}
