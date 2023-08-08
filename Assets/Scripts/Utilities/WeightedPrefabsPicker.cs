using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DockerIrl.Utils
{
    /// <summary>
    /// Given a list with prefabs and their weights, can randomly pick a prefab. More weight = more chance to pick this prefab.
    /// </summary>
    public class WeightedPrefabsPicker
    {
        /// <summary>
        /// TODO: refactor asap. its probably better to implement a custom Editor for this specific case, but rn im too lazy to learn that.
        /// </summary>
        [System.Serializable]
        public class WeightedPrefabsPickerUnityEditorInput
        {
            public List<WeightedPrefab> weightedPrefabs;
            public WeightedPrefabsPicker ToWeightedPrefabsPicker() => new(weightedPrefabs);
        }

        [System.Serializable]
        public record WeightedPrefab
        {
            public string id;
            public GameObject prefab;
            public float weight;
        }

        private readonly float overallWeight;
        private readonly List<WeightedPrefab> weightedPrefabs;

        public WeightedPrefabsPicker(List<WeightedPrefab> weightedPrefabs)
        {
            if (weightedPrefabs.Count == 0)
            {
                throw new System.ArgumentException("Empty list is not allowed");
            }

            this.weightedPrefabs = weightedPrefabs;

            foreach (var weightedPrefab in weightedPrefabs)
                overallWeight += weightedPrefab.weight;
        }

        public WeightedPrefab Pick()
        {
            float randomNumber = Random.Range(0f, overallWeight);

            float iWeight = 0f;
            for (int i = 0; i < weightedPrefabs.Count; i++)
            {
                var weightedPrefab = weightedPrefabs[i];
                iWeight += weightedPrefab.weight;

                if (randomNumber <= iWeight)
                    return weightedPrefab;
            }

            // Impossible case
            throw new System.Exception();
        }

        public WeightedPrefab FindById(string id)
        {
            // TODO: Use Dictionary for performance
            return weightedPrefabs.FirstOrDefault((weightedPrefab) => weightedPrefab.id == id);
        }
    }
}
