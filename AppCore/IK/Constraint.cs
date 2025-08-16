using System.Collections.Generic;

namespace MiniMikuDance.IK;

public class Constraint
{
    public int Target { get; }
    public int Effector { get; }
    public int Iterations { get; }
    public float AngleLimit { get; }
    public List<ConstraintJoint> Joints { get; } = new();

    public Constraint(int target, int effector, int iterations, float angleLimit)
    {
        Target = target;
        Effector = effector;
        Iterations = iterations;
        AngleLimit = angleLimit;
    }
}
