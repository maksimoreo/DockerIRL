using System.Linq;
using UnityEngine;

namespace DockerIrl.ContainerManagement
{
    // TODO: In future, this class should implement IFreeContainerSpotFinder, so that I can use different implementations in different scenes

    /// <summary>
    /// Finds a spot for a container, for free! Just kidding. It just searches for a free spot in world to spawn a container.
    /// </summary>
    public class FreeContainerSpotFinder : MonoBehaviour
    {
        public record Result
        {
            public Vector3 position;
            public Quaternion rotation;
        }

        [Header("Settings")]
        public Vector3 center;
        [Tooltip("Inner radius around the center, where objects will not spawn")]
        public float unspawnableInnerRadius;
        [Tooltip("The size of a prefab. Should be a bit bigger than prefab diagonal length.")]
        public float prefabRadius;

        [Header("References")]
        [SerializeField]
        private ContainerStore containerStore;

        private float prefabDiameter;
        private float sqrPrefabDiameter;
        private float adjustedUnspawnableInnerRadius;

        private void Awake()
        {
            prefabDiameter = prefabRadius * 2;
            sqrPrefabDiameter = Mathf.Pow(prefabDiameter, 2);
            adjustedUnspawnableInnerRadius = unspawnableInnerRadius + prefabRadius;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(center, unspawnableInnerRadius);
        }

        /// <summary>
        /// Finds for a free spot in world to spawn a container. Does not spawn a container.
        /// </summary>
        /// <returns>Suggestion to where to spawn a new container. Might return null.</returns>
        public Result FindFreeSpot()
        {
            if (containerStore.store.Count == 0) return GetVeryFirstSpot();

            float furthestContainerDistance =
                containerStore.store.Max((container) => (container.transform.position - center).magnitude);

            float firstAttemptRadius = Mathf.Max(furthestContainerDistance, unspawnableInnerRadius);
            Result result =
                AttemptToSpawn(attempts: 20, innerRadius: adjustedUnspawnableInnerRadius, outerRadius: firstAttemptRadius);
            if (result != null) return result;

            // Single attempt, it should be guaranteed to spawn
            result = AttemptToSpawn(
                attempts: 1,
                innerRadius: firstAttemptRadius + prefabDiameter,
                outerRadius: firstAttemptRadius + prefabDiameter
            );

            return result;
        }

        private Result GetVeryFirstSpot()
        {
            Vector2 proposedPosition2d = Random.insideUnitCircle.normalized * adjustedUnspawnableInnerRadius;
            Vector3 proposedPosition = new Vector3(proposedPosition2d.x, 0, proposedPosition2d.y);

            return new Result()
            {
                position = proposedPosition,
                rotation = QuaternionLookingAtCenterFrom(proposedPosition)
            };
        }

        private Result AttemptToSpawn(int attempts, float innerRadius, float outerRadius)
        {
            for (int i = 0; i < attempts; i++)
            {
                Vector2 proposedPosition2d = Random.insideUnitCircle.normalized * Random.Range(innerRadius, outerRadius);
                Vector3 proposedPosition = new Vector3(proposedPosition2d.x, 0, proposedPosition2d.y);

                if (IsColliding(proposedPosition)) continue;

                return new Result()
                {
                    position = proposedPosition,
                    rotation = QuaternionLookingAtCenterFrom(proposedPosition)
                };
            }

            return null;
        }

        private bool IsColliding(Vector3 position)
        {
            return containerStore.store
                .Any((container) => (container.transform.position - position).sqrMagnitude < sqrPrefabDiameter);
        }

        private Quaternion QuaternionLookingAtCenterFrom(Vector3 position) => Quaternion.LookRotation(center - position, Vector3.up);
    }
}
