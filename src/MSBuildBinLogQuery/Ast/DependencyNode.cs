using Microsoft.Build.Logging.Query.Graph;
using Microsoft.Build.Logging.Query.Result;

namespace Microsoft.Build.Logging.Query.Ast
{
    public abstract class DependencyNode<TParent, TGraphNode, TAstNode> : ConstraintNode<TParent, TAstNode>
        where TParent : class, IQueryResult
        where TGraphNode : IDirectedAcyclicGraphNode<TGraphNode>
        where TAstNode : IAstNode
    {
        public DependencyNodeType Type { get; }

        public DependencyNode(TAstNode value, DependencyNodeType type) : base(value)
        {
            Type = type;
        }

        protected bool Equals(DependencyNode<TParent, TGraphNode, TAstNode> other)
        {
            return base.Equals(other) &&
                   Type == other.Type;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}