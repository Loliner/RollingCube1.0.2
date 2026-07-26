using UnityEngine;

// Lets a mechanism (elevator, conveyor, etc.) take direct ownership of an
// object's movement, suspending whatever normally drives it.
public interface IExternallyControllable
{
    Transform Transform { get; }
    bool IsExternallyControlled { get; }
    void BeginExternalControl();
    void EndExternalControl();
}
