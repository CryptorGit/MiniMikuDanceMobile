using System.Collections.Generic;

namespace MiniMikuDance.IK;

public class Constraint
{
    public int Target { get; }
    public int Effector { get; }
    public float AngleLimit { get; }
    public int Iterations { get; }
    public List<ConstraintJoint> Joints { get; } = new();

    public Constraint(int target, int effector, float angleLimit, int iterations)
    {
        Target = target;
        Effector = effector;
        AngleLimit = angleLimit;
        Iterations = iterations;
    }
}
