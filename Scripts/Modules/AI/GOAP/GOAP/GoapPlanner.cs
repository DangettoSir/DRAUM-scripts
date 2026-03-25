using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IGoapPlanner {
    ActionPlan Plan(GoapAgent agent, HashSet<AgentGoal> goals, AgentGoal mostRecentGoal = null);
}

public class GoapPlanner : IGoapPlanner {
    public ActionPlan Plan(GoapAgent agent, HashSet<AgentGoal> goals, AgentGoal mostRecentGoal = null) {
        List<AgentGoal> orderedGoals = goals
            .Where(g => g.DesiredEffects.Any(b => !b.Evaluate()))
            .OrderByDescending(g => g == mostRecentGoal ? g.Priority - 0.01 : g.Priority)
            .ToList();
        
        foreach (var goal in orderedGoals) {
            Node goalNode = new Node(null, null, goal.DesiredEffects, 0);
            
            if (FindPath(goalNode, agent.actions)) {
                if (goalNode.IsLeafDead) continue;
                
                Stack<AgentAction> actionStack = new Stack<AgentAction>();
                while (goalNode.Leaves.Count > 0) {
                    var cheapestLeaf = goalNode.Leaves.OrderBy(leaf => leaf.Cost).First();
                    goalNode = cheapestLeaf;
                    actionStack.Push(cheapestLeaf.Action);
                }
                
                return new ActionPlan(goal, actionStack, goalNode.Cost);
            }
        }
        
        Debug.LogWarning("No plan found");
        return null;
    }

    // TODO: НАДА СДЕЛАТ более мощный алгоритм поиска, такой как A* или D*
    bool FindPath(Node parent, HashSet<AgentAction> actions) {
        var orderedActions = actions.OrderBy(a => a.Cost);
        
        foreach (var action in orderedActions) {
            var requiredEffects = parent.RequiredEffects;
            
            requiredEffects.RemoveWhere(b => b.Evaluate());
            if (requiredEffects.Count == 0) {
                return true;
            }

            if (action.Effects.Any(requiredEffects.Contains)) {
                var newRequiredEffects = new HashSet<AgentBelief>(requiredEffects);
                newRequiredEffects.ExceptWith(action.Effects);
                newRequiredEffects.UnionWith(action.Preconditions);
                
                var newAvailableActions = new HashSet<AgentAction>(actions);
                newAvailableActions.Remove(action);
                
                var newNode = new Node(parent, action, newRequiredEffects, parent.Cost + action.Cost);
                
                if (FindPath(newNode, newAvailableActions)) {
                    parent.Leaves.Add(newNode);
                    newRequiredEffects.ExceptWith(newNode.Action.Preconditions);
                }
                
                if (newRequiredEffects.Count == 0) {
                    return true;
                }
            }
        }
        
        return parent.Leaves.Count > 0;
    }
}

public class Node {
    public Node Parent { get; }
    public AgentAction Action { get; }
    public HashSet<AgentBelief> RequiredEffects { get; }
    public List<Node> Leaves { get; }
    public float Cost { get; }
    
    public bool IsLeafDead => Leaves.Count == 0 && Action == null;
    
    public Node(Node parent, AgentAction action, HashSet<AgentBelief> effects, float cost) {
        Parent = parent;
        Action = action;
        RequiredEffects = new HashSet<AgentBelief>(effects);
        Leaves = new List<Node>();
        Cost = cost;
    }
}

public class ActionPlan {
    public AgentGoal AgentGoal { get; }
    public Stack<AgentAction> Actions { get; }
    public float TotalCost { get; set; }
    
    public ActionPlan(AgentGoal goal, Stack<AgentAction> actions, float totalCost) {
        AgentGoal = goal;
        Actions = actions;
        TotalCost = totalCost;
    }
}