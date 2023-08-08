using UnityEngine;

namespace DockerIrl
{
    public class MovableBehaviour : MonoBehaviour
    {
        // Should return "ghost" GameObject, usually itself
        public virtual GameObject BeginMove() => gameObject;

        // Previously returned "ghost" will have updated transform
        public virtual void EndMove() { }

        // Should return if object can be placed where "ghost" currently is
        public virtual bool CanPlace() => true;

        // Two options to implement this class:
        // 1. Easy:
        //    BeginMove() returns itself (not overriden).
        //    Also, can return root GO, if this component is assigned to a nested mesh/collider GO.
        //    No need to override EndMove()
        // 2. A bit more complex:
        //    BeginMove() returns "ghost" GO.
        //    In this case u should override EndMove() to teleport real GO to ghost GO,
        //    and hide "ghost" GO.
    }
}
