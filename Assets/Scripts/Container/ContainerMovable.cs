using UnityEngine;

namespace DockerIrl
{
    public class ContainerMovable : MovableBehaviour
    {
        public Collider containerCollider;
        public ContainerBehaviour containerBehaviour;

        public override GameObject BeginMove()
        {
            containerCollider.enabled = false;
            containerBehaviour.terminals.ForEach((terminal) =>
            {
                terminal.myCollider.enabled = false;
            });

            return gameObject;
        }

        public override void EndMove()
        {
            containerCollider.enabled = true;
            containerBehaviour.terminals.ForEach((terminal) =>
            {
                terminal.myCollider.enabled = true;
            });
        }

        public override bool CanPlace()
        {
            // TODO: Check collision

            return base.CanPlace();
        }
    }
}
